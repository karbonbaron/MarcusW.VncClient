using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns without waiting for it being sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        public bool EnqueueMessage<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return false;

            connection.MessageSender.EnqueueMessage(message, cancellationToken);
            return true;
        }

        /// <summary>
        /// Enqueues a framebuffer update request with throttling based on <see cref="ConnectParameters.FramebufferUpdateDelay"/>.
        /// </summary>
        /// <param name="rectangle">The rectangle to request an update for.</param>
        /// <param name="incremental">Whether this is an incremental update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        public bool EnqueueFramebufferUpdateRequest(Rectangle rectangle, bool incremental, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return false;

            connection.MessageSender.EnqueueFramebufferUpdateRequest(rectangle, incremental, cancellationToken);
            return true;
        }

        /// <summary>
        /// Enqueues a framebuffer update request with a specific delay, ignoring global throttle settings.
        /// </summary>
        /// <param name="rectangle">The rectangle to request an update for.</param>
        /// <param name="incremental">Whether this is an incremental update request.</param>
        /// <param name="delay">The delay before sending the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True, if the message was queued, otherwise false.</returns>
        public bool EnqueueFramebufferUpdateRequestDelayed(Rectangle rectangle, bool incremental, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return false;

            connection.MessageSender.EnqueueFramebufferUpdateRequestDelayed(rectangle, incremental, delay, cancellationToken);
            return true;
        }

        /// <summary>
        /// Adds the <paramref name="message"/> to the send queue and returns a <see cref="Task"/> that completes when the message was sent.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TMessageType">The type of the message.</typeparam>
        /// <remarks>Please ensure the outgoing message type is marked as being supported by both sides before sending it. See <see cref="RfbConnection.UsedMessageTypes"/>.</remarks>
        public Task SendMessageAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RfbConnectionContext? connection = _activeConnection;
            if (connection?.MessageSender == null)
                return Task.CompletedTask;

            return connection.MessageSender.SendMessageAndWaitAsync(message, cancellationToken);
        }
    }
}
