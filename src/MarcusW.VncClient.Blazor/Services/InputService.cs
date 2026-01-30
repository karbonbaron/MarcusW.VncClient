using Microsoft.AspNetCore.Components.Web;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;

namespace MarcusW.VncClient.Blazor.Services
{
    /// <summary>
    /// Implementation of VNC input handling service
    /// </summary>
    public class InputService : IInputService
    {
        public async Task HandleMouseDownAsync(MouseEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter)
        {
            if (connection?.ConnectionState == ConnectionState.Connected)
            {
                var buttons = GetMouseButtons(e);
                var position = await coordinateConverter(e.OffsetX, e.OffsetY);
                connection.EnqueueMessage(new PointerEventMessage(position, buttons));
            }
        }

        public async Task HandleMouseUpAsync(MouseEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter)
        {
            if (connection?.ConnectionState == ConnectionState.Connected)
            {
                var position = await coordinateConverter(e.OffsetX, e.OffsetY);
                connection.EnqueueMessage(new PointerEventMessage(position, MouseButtons.None));
            }
        }

        public async Task HandleMouseMoveAsync(MouseEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter)
        {
            if (connection?.ConnectionState == ConnectionState.Connected)
            {
                // Important: Track button state during move for text selection and drag operations
                var buttons = GetMouseButtons(e);
                var position = await coordinateConverter(e.OffsetX, e.OffsetY);
                connection.EnqueueMessage(new PointerEventMessage(position, buttons));
            }
        }

        public async Task HandleMouseWheelAsync(WheelEventArgs e, RfbConnection? connection, Func<double, double, Task<Position>> coordinateConverter)
        {
            if (connection?.ConnectionState == ConnectionState.Connected)
            {
                var position = await coordinateConverter(e.OffsetX, e.OffsetY);
                var button = e.DeltaY > 0 ? MouseButtons.WheelUp : MouseButtons.WheelDown;
                
                // Send wheel press and release
                connection.EnqueueMessage(new PointerEventMessage(position, button));
                connection.EnqueueMessage(new PointerEventMessage(position, MouseButtons.None));
            }
        }

        public async Task HandleKeyDownAsync(KeyboardEventArgs e, RfbConnection? connection, Func<Task> exitFullscreenAction, bool isFullscreen)
        {
            // Handle ESC key to exit fullscreen (unless Ctrl+Shift+Esc for Task Manager)
            if (e.Key == "Escape" && isFullscreen && !(e.CtrlKey && e.ShiftKey))
            {
                await exitFullscreenAction();
                return;
            }

            if (connection?.ConnectionState == ConnectionState.Connected)
            {
                // Special key combinations are intercepted by JavaScript (preventDefault)
                // to prevent browser default behavior, but still forwarded here to VNC.
                // Intercepted combinations include:
                // - Alt+F4 (close window)
                // - Alt+Tab (window switching)
                // - Ctrl+Alt+Delete (security screen)
                // - Ctrl+Shift+Esc (task manager)
                // - Super/Windows key combinations
                // - And others defined in VncView.razor JavaScript
                
                var keySymbol = ConvertToKeySymbol(e);
                connection.EnqueueMessage(new KeyEventMessage(true, keySymbol));
            }
        }

        public Task HandleKeyUpAsync(KeyboardEventArgs e, RfbConnection? connection, bool isFullscreen)
        {
            // Skip ESC key up if we're handling fullscreen (unless Ctrl+Shift+Esc for Task Manager)
            if (e.Key == "Escape" && isFullscreen && !(e.CtrlKey && e.ShiftKey))
            {
                return Task.CompletedTask;
            }

            if (connection?.ConnectionState == ConnectionState.Connected)
            {
                var keySymbol = ConvertToKeySymbol(e);
                connection.EnqueueMessage(new KeyEventMessage(false, keySymbol));
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Converts browser MouseEventArgs.Buttons to VNC MouseButtons enum
        /// Browser uses bitmask: 1=Left, 2=Right, 4=Middle
        /// This is critical for proper text selection and drag operations
        /// </summary>
        private static MouseButtons GetMouseButtons(MouseEventArgs e)
        {
            var buttons = MouseButtons.None;

            // Use bitwise AND operations to check which buttons are pressed
            // This allows multiple buttons to be pressed simultaneously
            if ((e.Buttons & 1) == 1) buttons |= MouseButtons.Left;   // Left mouse button
            if ((e.Buttons & 2) == 2) buttons |= MouseButtons.Right;  // Right mouse button  
            if ((e.Buttons & 4) == 4) buttons |= MouseButtons.Middle; // Middle mouse button/wheel

            return buttons;
        }

        /// <summary>
        /// Converts browser KeyboardEventArgs.Key to VNC KeySymbol
        /// Handles both special keys and character input with proper case sensitivity
        /// </summary>
        private static KeySymbol ConvertToKeySymbol(KeyboardEventArgs e)
        {
            // Handle special keys first
            return e.Key switch
            {
                // Basic control keys
                "Backspace" => KeySymbol.BackSpace,
                "Tab" => KeySymbol.Tab,
                "Enter" => KeySymbol.Return,
                "Escape" => KeySymbol.Escape,
                "Delete" => KeySymbol.Delete,
                "Insert" => KeySymbol.Insert,
                
                // Navigation keys
                "ArrowLeft" => KeySymbol.Left,
                "ArrowUp" => KeySymbol.Up,
                "ArrowRight" => KeySymbol.Right,
                "ArrowDown" => KeySymbol.Down,
                "Home" => KeySymbol.Home,
                "End" => KeySymbol.End,
                "PageUp" => KeySymbol.Page_Up,
                "PageDown" => KeySymbol.Page_Down,
                
                // Function keys
                "F1" => KeySymbol.F1,
                "F2" => KeySymbol.F2,
                "F3" => KeySymbol.F3,
                "F4" => KeySymbol.F4,
                "F5" => KeySymbol.F5,
                "F6" => KeySymbol.F6,
                "F7" => KeySymbol.F7,
                "F8" => KeySymbol.F8,
                "F9" => KeySymbol.F9,
                "F10" => KeySymbol.F10,
                "F11" => KeySymbol.F11,
                "F12" => KeySymbol.F12,
                
                // Modifier keys
                "Shift" => KeySymbol.Shift_L,
                "ShiftLeft" => KeySymbol.Shift_L,
                "ShiftRight" => KeySymbol.Shift_R,
                "Control" => KeySymbol.Control_L,
                "ControlLeft" => KeySymbol.Control_L,
                "ControlRight" => KeySymbol.Control_R,
                "Alt" => KeySymbol.Alt_L,
                "AltLeft" => KeySymbol.Alt_L,
                "AltRight" => KeySymbol.Alt_R,
                "Meta" => KeySymbol.Super_L,
                "MetaLeft" => KeySymbol.Super_L,
                "MetaRight" => KeySymbol.Super_R,
                
                // Advanced keys
                "CapsLock" => KeySymbol.Caps_Lock,
                "NumLock" => KeySymbol.Num_Lock,
                "ScrollLock" => KeySymbol.Scroll_Lock,
                "Pause" => KeySymbol.Pause,
                "PrintScreen" => KeySymbol.Print,
                
                // Space (common case)
                " " => KeySymbol.space,
                
                // Character keys and symbols - delegate to character handler
                _ => ConvertCharacterToKeySymbol(e.Key)
            };
        }

        /// <summary>
        /// Converts a single character to VNC KeySymbol
        /// Preserves case sensitivity - critical for proper Shift+letter handling
        /// </summary>
        private static KeySymbol ConvertCharacterToKeySymbol(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length != 1)
                return KeySymbol.VoidSymbol;

            var c = key[0];
            
            // Handle numbers (0-9)
            if (c >= '0' && c <= '9')
                return (KeySymbol)c;
            
            // Handle letters - PRESERVE CASE for proper Shift handling!
            // When user presses Shift+A, browser sends "A", we should send KeySymbol.A
            // When user presses just 'a', browser sends "a", we should send KeySymbol.a
            if (c >= 'A' && c <= 'Z')
                return (KeySymbol)c; // Keep uppercase as-is
            
            if (c >= 'a' && c <= 'z')
                return (KeySymbol)c; // Keep lowercase as-is
            
            // Handle common symbols
            return c switch
            {
                '!' => KeySymbol.exclam,
                '@' => KeySymbol.at,
                '#' => KeySymbol.numbersign,
                '$' => KeySymbol.dollar,
                '%' => KeySymbol.percent,
                '^' => KeySymbol.asciicircum,
                '&' => KeySymbol.ampersand,
                '*' => KeySymbol.asterisk,
                '(' => KeySymbol.parenleft,
                ')' => KeySymbol.parenright,
                '-' => KeySymbol.minus,
                '_' => KeySymbol.underscore,
                '=' => KeySymbol.equal,
                '+' => KeySymbol.plus,
                '[' => KeySymbol.bracketleft,
                ']' => KeySymbol.bracketright,
                '{' => KeySymbol.braceleft,
                '}' => KeySymbol.braceright,
                '\\' => KeySymbol.backslash,
                '|' => KeySymbol.bar,
                ';' => KeySymbol.semicolon,
                ':' => KeySymbol.colon,
                '\'' => KeySymbol.apostrophe,
                '"' => KeySymbol.quotedbl,
                ',' => KeySymbol.comma,
                '.' => KeySymbol.period,
                '/' => KeySymbol.slash,
                '?' => KeySymbol.question,
                '`' => KeySymbol.grave,
                '~' => KeySymbol.asciitilde,
                '<' => KeySymbol.less,
                '>' => KeySymbol.greater,
                _ => (KeySymbol)c
            };
        }
    }
}
