# MarcusW.VncClient.Blazor

A Blazor component library for VNC client functionality, providing high-performance VNC rendering and input handling in web applications.

## Quick Start

### 1. Add Package Reference

```xml
<PackageReference Include="MarcusW.VncClient.Blazor" Version="1.0.0" />
```

### 2. Register Services

In your `Program.cs`:

```csharp
using MarcusW.VncClient.Blazor.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add VNC client services with default configuration
builder.Services.AddVncClientServices();

// Or with custom configuration
builder.Services.AddVncClientServices(options =>
{
    options.EnableDirtyRectangleRendering = true;
    options.MaxDirtyRectangles = 50;
    options.EnableFramebufferCaching = true;
    options.DefaultCanvasSize = new Size(800, 600);
    options.DefaultDpi = 96.0;
});
```

### 3. Use the VNC Component

In your Blazor component:

```razor
@page "/vnc"
@using MarcusW.VncClient.Blazor

<h3>VNC Client</h3>

<VncView Connection="@_connection" 
         OnFullscreenChange="@OnFullscreenChanged" />

@code {
    private RfbConnection? _connection;
    
    private async Task OnFullscreenChanged(bool isFullscreen)
    {
        // Handle fullscreen state changes
        Console.WriteLine($"Fullscreen: {isFullscreen}");
    }
}
```

## Features

### 🚀 High Performance
- **Dirty Rectangle Rendering**: Only renders changed areas for optimal performance
- **Buffer Pooling**: Reuses memory buffers to reduce garbage collection
- **Optimized Pixel Conversion**: Efficient conversion from VNC formats to web-compatible RGBA

### 🖱️ Full Input Support
- **Mouse Events**: Click, drag, wheel scrolling with accurate coordinate mapping
- **Keyboard Input**: Complete keyboard support including special keys and shortcuts
- **Touch Support**: Works on mobile devices and tablets

### 🖥️ Display Features
- **Fullscreen Mode**: Immersive fullscreen experience with ESC key support
- **Responsive Design**: Automatic scaling and responsive layout
- **Multiple Pixel Formats**: Support for various VNC pixel formats (16-bit, 32-bit)

### 🔧 Easy Integration
- **Dependency Injection**: Full DI support with service registration
- **Configuration Options**: Customizable behavior through options pattern
- **Event-Driven**: Reactive programming with event callbacks

## Architecture

The library is built with a clean, service-oriented architecture:

```
MarcusW.VncClient.Blazor
├── Components/
│   └── VncView.razor              # Main VNC display component
├── Services/
│   ├── IFramebufferService        # Framebuffer management
│   ├── IInputService              # Input event handling
│   ├── IRenderingService          # Pixel conversion & rendering
│   └── IFullscreenService         # Fullscreen functionality
├── Extensions/
│   └── ServiceCollectionExtensions # DI registration helpers
└── Adapters/
    └── Rendering/                 # Canvas and framebuffer adapters
```

## Configuration Options

```csharp
public class VncClientOptions
{
    /// <summary>Default pixel format when none specified</summary>
    public PixelFormat? DefaultPixelFormat { get; set; }
    
    /// <summary>Enable dirty rectangle rendering optimization</summary>
    public bool EnableDirtyRectangleRendering { get; set; } = true;
    
    /// <summary>Max dirty rectangles before fallback to full render</summary>
    public int MaxDirtyRectangles { get; set; } = 50;
    
    /// <summary>Enable framebuffer caching for better performance</summary>
    public bool EnableFramebufferCaching { get; set; } = true;
    
    /// <summary>Default canvas size when none specified</summary>
    public Size DefaultCanvasSize { get; set; } = new Size(800, 600);
    
    /// <summary>Default DPI settings for rendering</summary>
    public double DefaultDpi { get; set; } = 96.0;
}
```

## Advanced Usage

### Custom Service Implementation

You can replace any service with your own implementation:

```csharp
// Register custom rendering service
builder.Services.AddVncClientServices();
builder.Services.AddScoped<IRenderingService, MyCustomRenderingService>();
```

### Event Handling

```csharp
// Handle fullscreen changes
<VncView Connection="@_connection" 
         OnFullscreenChange="@((isFullscreen) => HandleFullscreenChange(isFullscreen))" />
```

### Direct Service Usage

```csharp
@inject IFramebufferService FramebufferService
@inject IRenderingService RenderingService

@code {
    private async Task CustomRendering()
    {
        var framebufferData = FramebufferService.GetFramebufferData();
        var dirtyRects = FramebufferService.GetDirtyRectangles();
        
        if (dirtyRects.Count > 0)
        {
            await RenderingService.RenderDirtyRectanglesAsync(
                "myCanvas", framebufferData, size, format, dirtyRects);
        }
    }
}
```

## Best Practices

1. **Service Lifetime**: Use `AddScoped` for Blazor Server, `AddTransient` for Blazor WebAssembly
2. **Memory Management**: Services automatically handle buffer cleanup and disposal
3. **Performance**: Enable dirty rectangle rendering for better performance on large displays
4. **Configuration**: Tune `MaxDirtyRectangles` based on your expected update patterns

## Requirements

- .NET 6.0 or later
- Blazor Server or Blazor WebAssembly
- Modern browser with HTML5 Canvas support

## License

This library is licensed under the same terms as the main MarcusW.VncClient project.
