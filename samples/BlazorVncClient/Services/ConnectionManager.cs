using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient;
using MarcusW.VncClient.Blazor.Adapters.Logging;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace BlazorVncClient.Services
{
    public class ConnectionManager
    {
        private readonly InteractiveAuthenticationHandler _interactiveAuthenticationHandler;
        private readonly VncClient _vncClient;

        public ConnectionManager(InteractiveAuthenticationHandler interactiveAuthenticationHandler, ILoggerFactory? loggerFactory = null)
        {
            _interactiveAuthenticationHandler = interactiveAuthenticationHandler ?? throw new ArgumentNullException(nameof(interactiveAuthenticationHandler));

            // Create and populate default logger factory for logging to Blazor logging sinks
            if (loggerFactory == null)
            {
                var factory = new LoggerFactory();
                factory.AddProvider(new BlazorLoggerProvider());
                _vncClient = new VncClient(factory);
            }
            else
            {
                _vncClient = new VncClient(loggerFactory);
            }
        }

        public Task<RfbConnection> ConnectAsync(ConnectParameters parameters, CancellationToken cancellationToken = default)
        {
            parameters.AuthenticationHandler = _interactiveAuthenticationHandler;
            
            // Enable rectangle-based updates for better performance
            parameters.RenderFlags |= RenderFlags.UpdateByRectangle;

            // Uncomment for debugging/visualization purposes
            //parameters.RenderFlags |= RenderFlags.VisualizeRectangles;

            return _vncClient.ConnectAsync(parameters, cancellationToken);
        }
    }
}
