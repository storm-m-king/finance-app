using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Pty.Net;

namespace ExpenseTracker.UI.Features.Dashboard;

/// <summary>
/// Singleton service that owns the PTY connection and terminal emulator state.
/// Survives view recreation during navigation.
/// </summary>
public class TerminalSession
{
    private IPtyConnection? _pty;
    private CancellationTokenSource? _readCts;

    public TerminalEmulator Emulator { get; private set; }
    public bool IsConnected => _pty != null;
    public int Columns { get; private set; } = 80;
    public int Rows { get; private set; } = 24;

    /// <summary>
    /// Fired on the UI thread when the display should be repainted.
    /// </summary>
    public event Action? DisplayChanged;

    public TerminalSession()
    {
        Emulator = new TerminalEmulator(Columns, Rows);
        Emulator.DisplayChanged += () => DisplayChanged?.Invoke();
        Emulator.SendData += data => WriteToPty(data);
    }

    public async Task ConnectAsync(string workingDirectory, string? startupCommand = null)
    {
        if (IsConnected) return;

        var isWindows = OperatingSystem.IsWindows();
        var options = new PtyOptions
        {
            App = isWindows ? "cmd.exe" : "/bin/bash",
            Cwd = workingDirectory,
            Rows = Rows,
            Cols = Columns,
            CommandLine = isWindows ? Array.Empty<string>() : new[] { "--login" },
            Environment = new Dictionary<string, string>
            {
                ["TERM"] = "xterm-256color",
                ["COLORTERM"] = "truecolor"
            }
        };

        _pty = await PtyProvider.SpawnAsync(options, CancellationToken.None);
        _readCts = new CancellationTokenSource();
        _ = ReadPtyLoopAsync(_readCts.Token);

        if (!string.IsNullOrEmpty(startupCommand))
        {
            await Task.Delay(300);
            WriteToPty(Encoding.UTF8.GetBytes(startupCommand + "\r"));
        }
    }

    public void WriteToPty(byte[] data)
    {
        if (_pty == null) return;
        try
        {
            _pty.WriterStream.Write(data, 0, data.Length);
            _pty.WriterStream.Flush();
        }
        catch { /* ignore write errors */ }
    }

    public void Resize(int columns, int rows)
    {
        if (columns == Columns && rows == Rows) return;
        Columns = columns;
        Rows = rows;
        Emulator.Resize(columns, rows);
        _pty?.Resize(columns, rows);
    }

    public void Disconnect()
    {
        _readCts?.Cancel();
        try { _pty?.Kill(); } catch { }
        _pty?.Dispose();
        _pty = null;
    }

    private async Task ReadPtyLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && _pty != null)
            {
                int bytesRead;
                try
                {
                    bytesRead = await _pty.ReaderStream.ReadAsync(buffer, 0, buffer.Length, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch { break; }

                if (bytesRead == 0) break;

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Emulator.ProcessData(data, 0, data.Length);
                });
            }
        }
        catch { /* stream closed */ }
    }
}
