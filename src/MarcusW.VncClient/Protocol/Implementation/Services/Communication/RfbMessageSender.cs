using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <summary>
    /// A background thread that sends queued messages and provides methods to add messages to the send queue.
    /// </summary>
    public class RfbMessageSender : BackgroundThread, IRfbMessageSender
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;
        private readonly ILogger<RfbMessageSender> _logger;

        private readonly BlockingCollection<QueueItem> _queue = new BlockingCollection<QueueItem>(new ConcurrentQueue<QueueItem>());

        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbMessageSender"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbMessageSender(RfbConnectionContext context) : base("RFB Message Sender")
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbMessageSender>();

            // Log failure events from background thread base
            Failed += (sender, args) => _logger.LogWarning(args.Exception, "Send loop failed.");
        }

        /// <inheritdoc />
        public void StartSendLoop()
        {
            // Removed debug logging for production use
            Start();
        }

        /// <inheritdoc />
        public Task StopSendLoopAsync()
        {
            // Removed debug logging for production use
            return StopAndWaitAsync();
        }

        /// <inheritdoc />
        public void EnqueueInitialMessages(CancellationToken cancellationToken = default)
        {
            // Removed initial message enqueueing debug logging for production use

            // WAYVNC COMPATIBILITY: Use TIER 1-4 encoding set for maximum compatibility
            var minimalEncodingTypes = GetMinimalEncodingTypes();
            var encodingTypesToUse = minimalEncodingTypes.ToImmutableHashSet();
            
            // Removed debug logging for production use

            // Send SetPixelFormat first for maximum compatibility
            var setPixelFormatMessage = new Protocol.Implementation.MessageTypes.Outgoing.SetPixelFormatMessage(_state.RemoteFramebufferFormat);
            EnqueueMessage(setPixelFormatMessage, cancellationToken);

            // Send initial SetEncodings
            var setEncodingsMessage = new SetEncodingsMessage(encodingTypesToUse);
            EnqueueMessage(setEncodingsMessage, cancellationToken);

            // Send incremental framebuffer update request for compatibility
            var updateRect = new Rectangle(Position.Origin, _state.RemoteFramebufferSize);
            var framebufferRequest = new FramebufferUpdateRequestMessage(true, updateRect); 
            EnqueueMessage(framebufferRequest, cancellationToken);
            
            // Brief delay to prevent server flooding
            Thread.Sleep(200);
            
            // Removed debug logging for production use
        }
        
        /// <summary>
        /// Gets encoding types for optimal WayVNC compatibility (TIER 1-4 default, configurable TIER 5)
        /// </summary>
        private IEnumerable<IEncodingType> GetMinimalEncodingTypes()
        {
            // TODO: Add configuration option to enable TIER 5 features for known compatible servers
            const bool enableAdvancedFeatures = false; // Could be configurable in the future
            
            return BuildEncodingTypeSet(includeTier5: enableAdvancedFeatures);
        }


        /// <summary>
        /// Builds an optimized set of encoding types based on tier configuration
        /// </summary>
        private IEnumerable<IEncodingType> BuildEncodingTypeSet(bool includeTier5 = false)
        {
            var encodingTypes = new List<IEncodingType>();
            
            // TIER 1: Essential encodings (required for basic VNC operation)
            AddEncodingIfExists(encodingTypes, "Raw");        // Required by VNC spec
            AddEncodingIfExists(encodingTypes, "CopyRect");   // Basic, widely supported
            AddEncodingIfExists(encodingTypes, "DesktopSize"); // Critical for framebuffer handling
            
            // TIER 2: Common frame encodings (safe compression)
            AddEncodingIfExists(encodingTypes, "ZRLE");       // Efficient compression
            AddEncodingIfExists(encodingTypes, "LastRect");   // End-of-update marker
            
            // TIER 3: Advanced frame encodings
            AddEncodingIfExists(encodingTypes, "ZLib");       // Basic compression
            AddEncodingIfExists(encodingTypes, "Tight");      // Advanced (if TurboJPEG available)
            
            // TIER 4: Quality control pseudo encodings
            AddJpegQualityEncodingsIfExists(encodingTypes);   // Smart JPEG quality control
            
            // TIER 5: Advanced protocol extensions (optional, may cause issues with newer servers)
            if (includeTier5)
            {
                AddEncodingIfExists(encodingTypes, "Fence");              // Advanced synchronization
                AddEncodingIfExists(encodingTypes, "ContinuousUpdates");  // Real-time streaming
                AddEncodingIfExists(encodingTypes, "ExtendedDesktopSize"); // Multi-screen support
            }
            
            // Removed debug logging for production use
            
            return encodingTypes;
        }
        
        /// <summary>
        /// Helper method to safely add encoding types by name
        /// </summary>
        private void AddEncodingIfExists(List<IEncodingType> encodingTypes, string name)
        {
            var encodingType = _context.SupportedEncodingTypes?.FirstOrDefault(et => et.Name == name);
            if (encodingType != null)
            {
                encodingTypes.Add(encodingType);
            }
        }
        
        /// <summary>
        /// Adds JPEG quality control pseudo encodings for advanced image optimization
        /// </summary>
        private void AddJpegQualityEncodingsIfExists(List<IEncodingType> encodingTypes)
        {
            if (_context.SupportedEncodingTypes == null) return;
            
            var jpegEncodings = _context.SupportedEncodingTypes
                .Where(et => et.Name.StartsWith("JPEG Quality Level", StringComparison.Ordinal) || 
                            et.Name.StartsWith("JPEG Fine-Grained Quality Level", StringComparison.Ordinal) || 
                            et.Name.StartsWith("JPEG Subsampling Level", StringComparison.Ordinal))
                .ToList();
                
            encodingTypes.AddRange(jpegEncodings);
        }

        /// <inheritdoc />
        public void EnqueueMessage<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbMessageSender));

            cancellationToken.ThrowIfCancellationRequested();

            TMessageType messageType = GetAndCheckMessageType<TMessageType>();

            // Add message to queue
            _queue.Add(new QueueItem(message, messageType), cancellationToken);
        }

        /// <inheritdoc />
        public void SendMessageAndWait<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            // ReSharper disable once AsyncConverter.AsyncWait
            SendMessageAndWaitAsync(message, cancellationToken).Wait(cancellationToken);
        }

        /// <inheritdoc />
        public Task SendMessageAndWaitAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbMessageSender));

            cancellationToken.ThrowIfCancellationRequested();

            TMessageType messageType = GetAndCheckMessageType<TMessageType>();

            // Create a completion source and ensure that completing the task won't block our send-loop.
            var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Add message to queue
            _queue.Add(new QueueItem(message, messageType, completionSource), cancellationToken);

            return completionSource.Task;
        }

        // This method will not catch exceptions so the BackgroundThread base class will receive them,
        // raise a "Failure" and trigger a reconnect.
        protected override void ThreadWorker(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_context.Transport != null, "_context.Transport != null");
                ITransport transport = _context.Transport;

                // Iterate over all queued items (will block if the queue is empty)
                foreach (QueueItem queueItem in _queue.GetConsumingEnumerable(cancellationToken))
                {
                    IOutgoingMessage<IOutgoingMessageType> message = queueItem.Message;
                    IOutgoingMessageType messageType = queueItem.MessageType;

                  // Removed verbose per-message debug logging for production use

                    try
                    {
                        // Write message to transport stream
                        messageType.WriteToTransport(message, transport, cancellationToken);
                      
                        queueItem.CompletionSource?.SetResult(null);
                      
                      // Add small delay between critical messages to prevent overwhelming the server
                      if (messageType.Name == "SetEncodings" || messageType.Name == "FramebufferUpdateRequest")
                      {
                          Thread.Sleep(100);
                      }
                    }
                    catch (Exception ex)
                    {
                        // If something went wrong during sending, tell the waiting tasks about it (so for example the GUI doesn't wait forever).
                        queueItem.CompletionSource?.TrySetException(ex);

                        // Send-thread should still fail
                        throw;
                    }
                }
            }
            catch
            {
                // When the loop was canceled or failed, cancel all remaining queue items
                SetQueueCancelled();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                SetQueueCancelled();
                _queue.Dispose();
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        private TMessageType GetAndCheckMessageType<TMessageType>() where TMessageType : class, IOutgoingMessageType
        {
            Debug.Assert(_context.SupportedMessageTypes != null, "_context.SupportedMessageTypes != null");

            TMessageType? messageType = _context.SupportedMessageTypes.OfType<TMessageType>().FirstOrDefault();
            if (messageType == null)
                throw new InvalidOperationException($"Could not find {typeof(TMessageType).Name} in supported message types collection.");

            if (!_state.UsedMessageTypes.Contains(messageType))
                throw new InvalidOperationException($"The message type {messageType.Name} must not be sent before checking for server-side support and marking it as used.");

            return messageType;
        }

        private void SetQueueCancelled()
        {
            _queue.CompleteAdding();
            foreach (QueueItem queueItem in _queue)
                queueItem.CompletionSource?.TrySetCanceled();
        }

        private class QueueItem
        {
            public IOutgoingMessage<IOutgoingMessageType> Message { get; }

            public IOutgoingMessageType MessageType { get; }

            public TaskCompletionSource<object?>? CompletionSource { get; }

            public QueueItem(IOutgoingMessage<IOutgoingMessageType> message, IOutgoingMessageType messageType, TaskCompletionSource<object?>? completionSource = null)
            {
                Message = message;
                MessageType = messageType;
                CompletionSource = completionSource;
            }
        }
    }
}
