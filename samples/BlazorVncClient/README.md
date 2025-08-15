# Blazor VNC Client Sample

This is a sample Blazor application that demonstrates how to use the MarcusW.VncClient library in a web environment using Blazor Server.

## Features

- **Multi-Protocol VNC Support**: Connect to VNC servers using RFB protocol with support for multiple security types
- **Smart Authentication System**: 
  - **VNC Authentication** (Security Type 2): Password-only authentication for traditional VNC servers
  - **VeNCrypt Authentication** (Security Type 19): Username + password authentication for modern secure VNC servers
  - **Automatic Protocol Detection**: Correctly prompts for appropriate credentials based on server security type
  - **Pre-filled Credentials**: Optional username/password fields to avoid prompts
  - **Modal Authentication Dialogs**: Professional Bootstrap-based dialogs instead of browser prompts
- **Real-time VNC Display**: HTML5 Canvas-based rendering with threading-safe updates
- **Full Input Support**: Mouse and keyboard input forwarding with comprehensive key mapping
- **Connection Management**: Status monitoring, error handling, and graceful disconnection
- **Modern Bootstrap UI**: Responsive design with real-time connection information display

## How to Run

1. Navigate to the BlazorVncClient directory:
   ```bash
   cd samples/BlazorVncClient
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to the URL shown in the console (typically `https://localhost:5001` or `http://localhost:5000`)

4. Navigate to the "Vnc" page using the navigation menu

## How to Use

1. **Connection Setup:**
   - Enter the VNC server host (IP address or hostname)
   - Enter the VNC server port (default is 5900)
   - Optionally enter a password if you know it in advance

2. **Connect:**
   - Click the "Connect" button
   - **Automatic Authentication**: The app automatically detects the server's security type and prompts accordingly:
     - **VNC Auth**: Shows password-only dialog
     - **VeNCrypt**: Shows username + password dialog
     - **Pre-filled**: Uses form values if provided
   - **Authentication Dialog Features**:
     - Security type identification in dialog
     - Enter key to confirm, Escape to cancel
     - Professional modal design

3. **Interact:**
   - Once connected, you can interact with the remote desktop using mouse and keyboard
   - The connection information panel shows:
     - Connection status and protocol version
     - Security type used for authentication
     - Desktop name and framebuffer size
     - Active encoding types
     - Error messages if connection fails

## Architecture

The application is built using:

- **Blazor Server** for the web UI framework
- **MarcusW.VncClient.Blazor** for VNC client functionality 
- **JavaScript Interop** for canvas rendering and browser integration
- **Dependency Injection** for service management
- **ReactiveUI** for view model and command patterns

## Key Components

- `Vnc.razor` - Main VNC client page with connection UI
- `VncView.razor` - VNC display component with canvas rendering
- `ConnectionManager` - Handles VNC connection lifecycle
- `InteractiveAuthenticationHandler` - Manages VNC authentication
- `JsInterop.cs` / `JsInterop.js` - Browser integration for canvas operations

## Comparison with Avalonia Sample

This Blazor implementation provides similar functionality to the Avalonia sample but adapted for web browsers:

- **Rendering:** Uses HTML5 Canvas instead of native UI controls
- **Input:** Browser event handling instead of native input events  
- **Authentication:** Browser-based prompts instead of modal dialogs
- **Deployment:** Web-based instead of desktop application

## Limitations

- Canvas rendering is basic and may not support all advanced VNC features
- Performance may be lower than native desktop applications
- Requires continuous connection to the Blazor server
- Browser security restrictions may limit some functionality

## Development Notes

The implementation demonstrates how to:
- Integrate the VNC client library with Blazor components
- Handle asynchronous VNC operations in a web context
- Bridge between .NET and JavaScript for canvas operations
- Manage connection state and user interaction in a web UI
