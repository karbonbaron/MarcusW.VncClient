using System;
using System.Buffers.Binary;
using System.IO;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A Stream wrapper that provides AES-EAX encryption and decryption for RA2 security type.
    /// </summary>
    /// <remarks>
    /// Each message is framed as: [2-byte length][encrypted message][16-byte MAC]
    /// The 2-byte length is associated data, and a 16-byte little-endian counter is the nonce.
    /// </remarks>
    internal class AesEaxStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly byte[] _clientSessionKey;
        private readonly byte[] _serverSessionKey;
        private ulong _writeCounter;
        private ulong _readCounter;
        private const int MacSize = 16;
        private const int NonceSize = 16;
        
        // Read buffer for incoming messages
        private byte[] _readBuffer = Array.Empty<byte>();
        private int _readPosition;
        private int _readLength;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public AesEaxStream(Stream baseStream, byte[] clientSessionKey, byte[] serverSessionKey)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _clientSessionKey = clientSessionKey ?? throw new ArgumentNullException(nameof(clientSessionKey));
            _serverSessionKey = serverSessionKey ?? throw new ArgumentNullException(nameof(serverSessionKey));

            if (clientSessionKey.Length != 16)
                throw new ArgumentException("Client session key must be 16 bytes", nameof(clientSessionKey));
            if (serverSessionKey.Length != 16)
                throw new ArgumentException("Server session key must be 16 bytes", nameof(serverSessionKey));

            _writeCounter = 0;
            _readCounter = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            // If we have data in the read buffer, return it
            if (_readPosition < _readLength)
            {
                int available = _readLength - _readPosition;
                int toCopy = Math.Min(count, available);
                Array.Copy(_readBuffer, _readPosition, buffer, offset, toCopy);
                _readPosition += toCopy;
                return toCopy;
            }

            // Need to read and decrypt a new message
            _readPosition = 0;
            _readLength = 0;

            // Read message length (2 bytes, big-endian)
            Span<byte> lengthBuffer = stackalloc byte[2];
            int lengthRead = 0;
            while (lengthRead < 2)
            {
                int read = _baseStream.Read(lengthBuffer.Slice(lengthRead));
                if (read == 0)
                    return 0; // End of stream
                lengthRead += read;
            }

            ushort messageLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);

            // Read encrypted message + MAC
            int ciphertextLength = messageLength + MacSize;
            byte[] ciphertext = new byte[ciphertextLength];
            int totalRead = 0;
            while (totalRead < ciphertextLength)
            {
                int read = _baseStream.Read(ciphertext, totalRead, ciphertextLength - totalRead);
                if (read == 0)
                    throw new EndOfStreamException("Unexpected end of stream while reading AES-EAX message");
                totalRead += read;
            }

            // Decrypt with AES-EAX
            _readBuffer = DecryptMessage(ciphertext, lengthBuffer.ToArray(), _serverSessionKey, _readCounter);
            _readLength = _readBuffer.Length;
            _readPosition = 0;
            _readCounter++;

            // Return as much as requested
            int bytesToReturn = Math.Min(count, _readLength);
            Array.Copy(_readBuffer, 0, buffer, offset, bytesToReturn);
            _readPosition = bytesToReturn;
            return bytesToReturn;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            // Extract the data to encrypt
            byte[] plaintext = new byte[count];
            Array.Copy(buffer, offset, plaintext, 0, count);

            // Encrypt with AES-EAX
            byte[] lengthBuffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)count);

            byte[] ciphertext = EncryptMessage(plaintext, lengthBuffer, _clientSessionKey, _writeCounter);
            _writeCounter++;

            // Write: [2-byte length][encrypted message + MAC]
            _baseStream.Write(lengthBuffer, 0, 2);
            _baseStream.Write(ciphertext, 0, ciphertext.Length);
        }

        private static byte[] EncryptMessage(byte[] plaintext, byte[] associatedData, byte[] key, ulong counter)
        {
            // Create nonce from counter (16 bytes, little-endian)
            byte[] nonce = new byte[NonceSize];
            BinaryPrimitives.WriteUInt64LittleEndian(nonce, counter);

            // Initialize EAX cipher
            var cipher = new EaxBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), MacSize * 8, nonce, associatedData));

            // Encrypt
            byte[] ciphertext = new byte[cipher.GetOutputSize(plaintext.Length)];
            int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertext, 0);
            cipher.DoFinal(ciphertext, len);

            return ciphertext;
        }

        private static byte[] DecryptMessage(byte[] ciphertext, byte[] associatedData, byte[] key, ulong counter)
        {
            // Create nonce from counter (16 bytes, little-endian)
            byte[] nonce = new byte[NonceSize];
            BinaryPrimitives.WriteUInt64LittleEndian(nonce, counter);

            // Initialize EAX cipher
            var cipher = new EaxBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), MacSize * 8, nonce, associatedData));

            // Decrypt
            byte[] plaintext = new byte[cipher.GetOutputSize(ciphertext.Length)];
            int len = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
            try
            {
                len += cipher.DoFinal(plaintext, len);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("AES-EAX decryption failed - MAC verification error", ex);
            }

            // Trim to actual length
            if (len != plaintext.Length)
                Array.Resize(ref plaintext, len);

            return plaintext;
        }

        public override void Flush() => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Don't dispose the base stream - it's managed by the base transport
                Array.Clear(_clientSessionKey, 0, _clientSessionKey.Length);
                Array.Clear(_serverSessionKey, 0, _serverSessionKey.Length);
                Array.Clear(_readBuffer, 0, _readBuffer.Length);
            }
            base.Dispose(disposing);
        }
    }
}
