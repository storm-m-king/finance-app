using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace ExpenseTracker.UI.Features.Dashboard;

/// <summary>
/// Custom Avalonia control that renders a terminal session and handles keyboard I/O.
/// Does not own the session — the session survives view recreation.
/// </summary>
public class TerminalControl : Control
{
    private TerminalSession? _session;

    // Font metrics
    private readonly Typeface _typeface;
    private readonly Typeface _typefaceBold;
    private const double FontSize = 13;
    private double _cellWidth;
    private double _cellHeight;

    // Cursor blink
    private readonly DispatcherTimer _cursorTimer;
    private bool _cursorBlinkOn = true;

    // Scroll offset (for scrollback)
    private int _scrollOffset;

    public TerminalControl()
    {
        Focusable = true;
        ClipToBounds = true;

        _typeface = new Typeface("Cascadia Mono, Consolas, Courier New, monospace");
        _typefaceBold = new Typeface("Cascadia Mono, Consolas, Courier New, monospace", FontStyle.Normal, FontWeight.Bold);

        MeasureCellSize();

        _cursorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _cursorTimer.Tick += (_, _) =>
        {
            _cursorBlinkOn = !_cursorBlinkOn;
            InvalidateVisual();
        };
        _cursorTimer.Start();
    }

    /// <summary>
    /// Attaches this control to a terminal session for rendering and input.
    /// </summary>
    public void Attach(TerminalSession session)
    {
        if (_session != null)
            _session.DisplayChanged -= OnDisplayChanged;

        _session = session;
        _session.DisplayChanged += OnDisplayChanged;
        _scrollOffset = 0;
        InvalidateVisual();
    }

    /// <summary>
    /// Detaches from the session without killing it.
    /// </summary>
    public void Detach()
    {
        if (_session != null)
            _session.DisplayChanged -= OnDisplayChanged;
        _cursorTimer.Stop();
    }

    #region Rendering

    private void MeasureCellSize()
    {
        var ft = new FormattedText(
            "M",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            _typeface,
            FontSize,
            Brushes.White);
        _cellWidth = ft.Width;
        _cellHeight = ft.Height;
    }

    private void OnDisplayChanged()
    {
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int newCols = Math.Max(10, (int)(finalSize.Width / _cellWidth));
        int newRows = Math.Max(4, (int)(finalSize.Height / _cellHeight));
        _session?.Resize(newCols, newRows);
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var bgBrush = new SolidColorBrush(Color.FromUInt32(TerminalEmulator.DefaultBg));
        context.FillRectangle(bgBrush, new Rect(Bounds.Size));

        if (_session == null) return;

        var emulator = _session.Emulator;
        var brushCache = new Dictionary<uint, IBrush>();
        IBrush GetBrush(uint argb)
        {
            if (!brushCache.TryGetValue(argb, out var b))
            {
                b = new SolidColorBrush(Color.FromUInt32(argb));
                brushCache[argb] = b;
            }
            return b;
        }

        int scrollback = emulator.ScrollbackCount;
        int rows = emulator.Rows;

        for (int row = 0; row < rows; row++)
        {
            double y = row * _cellHeight;
            int virtualRow = scrollback - _scrollOffset + row;
            List<TerminalSpan> spans;

            if (virtualRow < 0)
                continue;
            else if (virtualRow < scrollback)
                spans = emulator.GetScrollbackRowSpans(scrollback - 1 - virtualRow);
            else
                spans = emulator.GetRowSpans(virtualRow - scrollback);

            double x = 0;
            foreach (var span in spans)
            {
                double spanWidth = span.Text.Length * _cellWidth;

                if (span.Background != TerminalEmulator.DefaultBg)
                {
                    context.FillRectangle(GetBrush(span.Background),
                        new Rect(x, y, spanWidth, _cellHeight));
                }

                var typeface = span.Bold ? _typefaceBold : _typeface;
                var ft = new FormattedText(
                    span.Text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    FontSize,
                    GetBrush(span.Foreground));

                context.DrawText(ft, new Point(x, y));
                x += spanWidth;
            }
        }

        // Cursor (only when viewing live screen)
        if (emulator.CursorVisible && _cursorBlinkOn && _scrollOffset == 0)
        {
            double cx = emulator.CursorCol * _cellWidth;
            double cy = emulator.CursorRow * _cellHeight;
            var cursorBrush = new SolidColorBrush(Color.FromArgb(180, 200, 200, 200));
            context.FillRectangle(cursorBrush, new Rect(cx, cy, _cellWidth, _cellHeight));
        }

        // Scroll indicator
        if (_scrollOffset > 0)
        {
            var indicatorBrush = new SolidColorBrush(Color.FromArgb(180, 91, 155, 213));
            var text = new FormattedText(
                $"↑ {_scrollOffset} lines above",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _typeface,
                11,
                indicatorBrush);
            context.DrawText(text, new Point(Bounds.Width - text.Width - 8, 4));
        }
    }

    #endregion

    #region Keyboard Input

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_session == null) return;

        // Handle Ctrl+V paste before TranslateKey
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.V)
        {
            _ = PasteFromClipboardAsync();
            e.Handled = true;
            return;
        }

        var data = TranslateKey(e);
        if (data != null)
        {
            _session.WriteToPty(data);
            _scrollOffset = 0;
            _cursorBlinkOn = true;
            e.Handled = true;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (_session == null || string.IsNullOrEmpty(e.Text)) return;

        _session.WriteToPty(Encoding.UTF8.GetBytes(e.Text));
        _scrollOffset = 0;
        _cursorBlinkOn = true;
        e.Handled = true;
    }

    private async Task PasteFromClipboardAsync()
    {
        if (_session == null) return;

        try
        {
            string? text = await ReadClipboardTextAsync();
            if (string.IsNullOrEmpty(text)) return;

            // Normalize line endings to \r for the terminal
            text = text.Replace("\r\n", "\r").Replace('\n', '\r');

            var bytes = Encoding.UTF8.GetBytes(text);
            _session.WriteToPty(bytes);
            _scrollOffset = 0;
            _cursorBlinkOn = true;
        }
        catch { /* clipboard read failed — silently ignore */ }
    }

    private static async Task<string?> ReadClipboardTextAsync()
    {
        string cmd, args;
        if (OperatingSystem.IsWindows())
        {
            cmd = "powershell";
            args = "-NoProfile -Command Get-Clipboard";
        }
        else if (OperatingSystem.IsMacOS())
        {
            cmd = "pbpaste";
            args = "";
        }
        else
        {
            cmd = "xclip";
            args = "-selection clipboard -o";
        }

        var psi = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var proc = Process.Start(psi);
        if (proc == null) return null;

        var result = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return result;
    }

    private byte[]? TranslateKey(KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        string csi = _session!.Emulator.ApplicationCursorKeys ? "O" : "[";

        if (ctrl && e.Key >= Key.A && e.Key <= Key.Z)
            return new[] { (byte)(e.Key - Key.A + 1) };

        return e.Key switch
        {
            Key.Enter => "\r"u8.ToArray(),
            Key.Escape => "\x1B"u8.ToArray(),
            Key.Tab => shift ? Encoding.ASCII.GetBytes("\x1B[Z") : "\t"u8.ToArray(),
            Key.Back => new byte[] { 0x7F },
            Key.Delete => Encoding.ASCII.GetBytes("\x1B[3~"),
            Key.Up => Encoding.ASCII.GetBytes($"\x1B{csi}A"),
            Key.Down => Encoding.ASCII.GetBytes($"\x1B{csi}B"),
            Key.Right => Encoding.ASCII.GetBytes($"\x1B{csi}C"),
            Key.Left => Encoding.ASCII.GetBytes($"\x1B{csi}D"),
            Key.Home => Encoding.ASCII.GetBytes($"\x1B{csi}H"),
            Key.End => Encoding.ASCII.GetBytes($"\x1B{csi}F"),
            Key.PageUp => Encoding.ASCII.GetBytes("\x1B[5~"),
            Key.PageDown => Encoding.ASCII.GetBytes("\x1B[6~"),
            Key.Insert => Encoding.ASCII.GetBytes("\x1B[2~"),
            Key.F1 => Encoding.ASCII.GetBytes("\x1BOP"),
            Key.F2 => Encoding.ASCII.GetBytes("\x1BOQ"),
            Key.F3 => Encoding.ASCII.GetBytes("\x1BOR"),
            Key.F4 => Encoding.ASCII.GetBytes("\x1BOS"),
            Key.F5 => Encoding.ASCII.GetBytes("\x1B[15~"),
            Key.F6 => Encoding.ASCII.GetBytes("\x1B[17~"),
            Key.F7 => Encoding.ASCII.GetBytes("\x1B[18~"),
            Key.F8 => Encoding.ASCII.GetBytes("\x1B[19~"),
            Key.F9 => Encoding.ASCII.GetBytes("\x1B[20~"),
            Key.F10 => Encoding.ASCII.GetBytes("\x1B[21~"),
            Key.F11 => Encoding.ASCII.GetBytes("\x1B[23~"),
            Key.F12 => Encoding.ASCII.GetBytes("\x1B[24~"),
            _ => null
        };
    }

    #endregion

    #region Mouse Wheel (scroll)

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_session == null) return;

        int delta = e.Delta.Y > 0 ? -3 : 3;
        _scrollOffset = Math.Clamp(_scrollOffset + delta, 0, _session.Emulator.ScrollbackCount);
        InvalidateVisual();
        e.Handled = true;
    }

    #endregion
}
