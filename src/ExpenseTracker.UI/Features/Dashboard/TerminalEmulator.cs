using System;
using System.Collections.Generic;
using System.Text;

namespace ExpenseTracker.UI.Features.Dashboard;

/// <summary>
/// A single cell in the terminal character grid.
/// </summary>
public struct TerminalCell
{
    public char Character;
    public uint Foreground; // ARGB, 0 = use default
    public uint Background; // ARGB, 0 = use default
    public bool Bold;
    public bool Underline;
    public bool Inverse;

    public static TerminalCell Empty => new() { Character = ' ' };
}

/// <summary>
/// A span of text with uniform visual attributes, for efficient rendering.
/// </summary>
public readonly record struct TerminalSpan(string Text, uint Foreground, uint Background, bool Bold, bool Underline);

/// <summary>
/// VT100/xterm terminal emulator with full CSI, SGR, alternate buffer, and scroll region support.
/// </summary>
public class TerminalEmulator
{
    public int Columns { get; private set; }
    public int Rows { get; private set; }
    public int CursorRow => _cursorRow;
    public int CursorCol => _cursorCol;
    public bool CursorVisible { get; private set; } = true;
    public bool ApplicationCursorKeys { get; private set; }
    public string? WindowTitle { get; private set; }

    public const uint DefaultFg = 0xFFD4D4D4;
    public const uint DefaultBg = 0xFF0C0C0C;

    private TerminalCell[,] _mainBuffer;
    private TerminalCell[,] _altBuffer;
    private TerminalCell[,] _activeBuffer;
    private readonly List<TerminalCell[]> _scrollback = new();
    public int MaxScrollback { get; set; } = 5000;
    public int ScrollbackCount => _scrollback.Count;

    private int _cursorRow, _cursorCol;
    private int _savedCursorRow, _savedCursorCol;
    private uint _savedFg, _savedBg;
    private bool _savedBold, _savedUnderline, _savedInverse;
    private int _scrollTop, _scrollBottom;
    private bool _useAltBuffer;
    private bool _autoWrap = true;
    private bool _wrapPending; // defer wrap until next character

    // Current text attributes
    private uint _currentFg;
    private uint _currentBg;
    private bool _bold, _underline, _inverse;

    // Parser state
    private enum ParserState { Ground, Escape, Csi, Osc, OscEsc }
    private ParserState _state = ParserState.Ground;
    private readonly StringBuilder _csiParams = new();
    private readonly StringBuilder _oscString = new();

    // UTF-8 decoding
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();

    // Events
    public event Action? DisplayChanged;
    public event Action<string>? TitleChanged;
    public event Action<byte[]>? SendData;

    // ANSI 16-color palette
    private static readonly uint[] Ansi16 =
    {
        0xFF000000, 0xFFCC0000, 0xFF4E9A06, 0xFFC4A000,
        0xFF3465A4, 0xFF75507B, 0xFF06989A, 0xFFD3D7CF,
        0xFF555753, 0xFFEF2929, 0xFF8AE234, 0xFFFCE94F,
        0xFF729FCF, 0xFFAD7FA8, 0xFF34E2E2, 0xFFEEEEEC
    };

    // Full 256-color palette (lazily built)
    private static uint[]? _palette256;
    private static uint[] Palette256 => _palette256 ??= Build256Palette();

    public TerminalEmulator(int columns = 80, int rows = 24)
    {
        Columns = columns;
        Rows = rows;
        _scrollTop = 0;
        _scrollBottom = rows - 1;
        _mainBuffer = CreateBuffer(rows, columns);
        _altBuffer = CreateBuffer(rows, columns);
        _activeBuffer = _mainBuffer;
    }

    public void Resize(int columns, int rows)
    {
        if (columns == Columns && rows == Rows) return;
        var newMain = CreateBuffer(rows, columns);
        var newAlt = CreateBuffer(rows, columns);
        CopyBuffer(_mainBuffer, newMain, Math.Min(Rows, rows), Math.Min(Columns, columns));
        CopyBuffer(_altBuffer, newAlt, Math.Min(Rows, rows), Math.Min(Columns, columns));
        _mainBuffer = newMain;
        _altBuffer = newAlt;
        _activeBuffer = _useAltBuffer ? _altBuffer : _mainBuffer;
        Columns = columns;
        Rows = rows;
        _scrollTop = 0;
        _scrollBottom = rows - 1;
        _cursorRow = Math.Min(_cursorRow, rows - 1);
        _cursorCol = Math.Min(_cursorCol, columns - 1);
    }

    public void ProcessData(byte[] data, int offset, int count)
    {
        var chars = new char[count * 2];
        int charCount = _decoder.GetChars(data, offset, count, chars, 0);
        for (int i = 0; i < charCount; i++)
            ProcessChar(chars[i]);
        DisplayChanged?.Invoke();
    }

    public TerminalCell GetCell(int row, int col) => _activeBuffer[row, col];

    /// <summary>
    /// Gets a scrollback line (0 = most recent scrollback line).
    /// </summary>
    public TerminalCell[]? GetScrollbackLine(int index)
    {
        if (index < 0 || index >= _scrollback.Count) return null;
        return _scrollback[_scrollback.Count - 1 - index];
    }

    /// <summary>
    /// Returns grouped spans for a scrollback line, for efficient rendering.
    /// Index 0 = most recent scrollback line.
    /// </summary>
    public List<TerminalSpan> GetScrollbackRowSpans(int index)
    {
        var spans = new List<TerminalSpan>();
        var line = GetScrollbackLine(index);
        if (line == null) return spans;

        var sb = new StringBuilder();
        uint curFg = ResolveScrollbackColor(line[0], true);
        uint curBg = ResolveScrollbackColor(line[0], false);
        bool curBold = line[0].Bold;
        bool curUl = line[0].Underline;

        for (int col = 0; col < line.Length; col++)
        {
            var cell = line[col];
            uint fg = ResolveScrollbackColor(cell, true);
            uint bg = ResolveScrollbackColor(cell, false);

            if (fg != curFg || bg != curBg || cell.Bold != curBold || cell.Underline != curUl)
            {
                if (sb.Length > 0)
                    spans.Add(new TerminalSpan(sb.ToString(), curFg, curBg, curBold, curUl));
                sb.Clear();
                curFg = fg;
                curBg = bg;
                curBold = cell.Bold;
                curUl = cell.Underline;
            }
            sb.Append(cell.Character == '\0' ? ' ' : cell.Character);
        }

        if (sb.Length > 0)
            spans.Add(new TerminalSpan(sb.ToString(), curFg, curBg, curBold, curUl));

        return spans;
    }

    private uint ResolveScrollbackColor(TerminalCell cell, bool isForeground)
    {
        uint fg = cell.Foreground == 0 ? DefaultFg : cell.Foreground;
        uint bg = cell.Background == 0 ? DefaultBg : cell.Background;
        if (cell.Inverse)
            (fg, bg) = (bg, fg);
        return isForeground ? fg : bg;
    }

    /// <summary>
    /// Returns grouped spans for a visible row, for efficient rendering.
    /// </summary>
    public List<TerminalSpan> GetRowSpans(int row)
    {
        var spans = new List<TerminalSpan>();
        if (row < 0 || row >= Rows) return spans;

        var sb = new StringBuilder();
        uint curFg = ResolveColor(_activeBuffer[row, 0], true);
        uint curBg = ResolveColor(_activeBuffer[row, 0], false);
        bool curBold = _activeBuffer[row, 0].Bold;
        bool curUl = _activeBuffer[row, 0].Underline;

        for (int col = 0; col < Columns; col++)
        {
            var cell = _activeBuffer[row, col];
            uint fg = ResolveColor(cell, true);
            uint bg = ResolveColor(cell, false);

            if (fg != curFg || bg != curBg || cell.Bold != curBold || cell.Underline != curUl)
            {
                if (sb.Length > 0)
                    spans.Add(new TerminalSpan(sb.ToString(), curFg, curBg, curBold, curUl));
                sb.Clear();
                curFg = fg;
                curBg = bg;
                curBold = cell.Bold;
                curUl = cell.Underline;
            }
            sb.Append(cell.Character == '\0' ? ' ' : cell.Character);
        }

        if (sb.Length > 0)
            spans.Add(new TerminalSpan(sb.ToString(), curFg, curBg, curBold, curUl));

        return spans;
    }

    private uint ResolveColor(TerminalCell cell, bool isForeground)
    {
        uint fg = cell.Foreground == 0 ? DefaultFg : cell.Foreground;
        uint bg = cell.Background == 0 ? DefaultBg : cell.Background;
        if (cell.Inverse)
            (fg, bg) = (bg, fg);
        return isForeground ? fg : bg;
    }

    #region Character Processing

    private void ProcessChar(char c)
    {
        switch (_state)
        {
            case ParserState.Ground:
                ProcessGround(c);
                break;
            case ParserState.Escape:
                ProcessEscape(c);
                break;
            case ParserState.Csi:
                ProcessCsi(c);
                break;
            case ParserState.Osc:
                ProcessOsc(c);
                break;
            case ParserState.OscEsc:
                if (c == '\\')
                    FinishOsc();
                else
                {
                    _state = ParserState.Ground;
                    ProcessChar(c);
                }
                break;
        }
    }

    private void ProcessGround(char c)
    {
        switch (c)
        {
            case '\x1B': // ESC
                _state = ParserState.Escape;
                break;
            case '\r': // CR
                _cursorCol = 0;
                _wrapPending = false;
                break;
            case '\n': // LF
            case '\x0B': // VT
            case '\x0C': // FF
                LineFeed();
                break;
            case '\t': // TAB
                _cursorCol = Math.Min((_cursorCol / 8 + 1) * 8, Columns - 1);
                _wrapPending = false;
                break;
            case '\b': // BS
                if (_cursorCol > 0) _cursorCol--;
                _wrapPending = false;
                break;
            case '\a': // BEL
                break;
            case '\x0E': // SO (shift out)
            case '\x0F': // SI (shift in)
                break;
            default:
                if (c >= ' ')
                    PutChar(c);
                break;
        }
    }

    private void ProcessEscape(char c)
    {
        switch (c)
        {
            case '[':
                _state = ParserState.Csi;
                _csiParams.Clear();
                break;
            case ']':
                _state = ParserState.Osc;
                _oscString.Clear();
                break;
            case '7': // DECSC - Save cursor
                SaveCursor();
                _state = ParserState.Ground;
                break;
            case '8': // DECRC - Restore cursor
                RestoreCursor();
                _state = ParserState.Ground;
                break;
            case 'M': // RI - Reverse Index
                ReverseIndex();
                _state = ParserState.Ground;
                break;
            case 'D': // IND - Index (line feed)
                LineFeed();
                _state = ParserState.Ground;
                break;
            case 'E': // NEL - Next Line
                _cursorCol = 0;
                LineFeed();
                _state = ParserState.Ground;
                break;
            case 'c': // RIS - Full Reset
                Reset();
                _state = ParserState.Ground;
                break;
            case '(': case ')': case '*': case '+':
                // Character set designation - ignore next char
                _state = ParserState.Ground; // simplified: skip
                break;
            case '=': case '>':
                // Keypad mode
                _state = ParserState.Ground;
                break;
            default:
                _state = ParserState.Ground;
                break;
        }
    }

    private void ProcessCsi(char c)
    {
        if (c >= '0' && c <= '9' || c == ';' || c == '?' || c == '>' || c == '!' || c == '"' || c == ' ' || c == '\'')
        {
            _csiParams.Append(c);
        }
        else if (c >= '@' && c <= '~')
        {
            HandleCsi(c);
            _state = ParserState.Ground;
        }
        else
        {
            // Unexpected character, abort CSI
            _state = ParserState.Ground;
        }
    }

    private void ProcessOsc(char c)
    {
        if (c == '\x07') // BEL terminates OSC
        {
            FinishOsc();
        }
        else if (c == '\x1B')
        {
            _state = ParserState.OscEsc; // might be ESC\ (ST)
        }
        else
        {
            _oscString.Append(c);
        }
    }

    private void FinishOsc()
    {
        var osc = _oscString.ToString();
        var semi = osc.IndexOf(';');
        if (semi >= 0 && int.TryParse(osc[..semi], out int code))
        {
            var value = osc[(semi + 1)..];
            if (code is 0 or 2)
            {
                WindowTitle = value;
                TitleChanged?.Invoke(value);
            }
        }
        _state = ParserState.Ground;
    }

    #endregion

    #region CSI Handling

    private void HandleCsi(char terminator)
    {
        var paramStr = _csiParams.ToString();

        // Private mode sequences (ESC[?...)
        if (paramStr.StartsWith('?'))
        {
            HandlePrivateMode(paramStr[1..], terminator);
            return;
        }

        var args = ParseParams(paramStr);

        switch (terminator)
        {
            case 'A': // CUU - Cursor Up
                _cursorRow = Math.Max(_cursorRow - Math.Max(args[0], 1), 0);
                _wrapPending = false;
                break;
            case 'B': // CUD - Cursor Down
                _cursorRow = Math.Min(_cursorRow + Math.Max(args[0], 1), Rows - 1);
                _wrapPending = false;
                break;
            case 'C': // CUF - Cursor Forward
                _cursorCol = Math.Min(_cursorCol + Math.Max(args[0], 1), Columns - 1);
                _wrapPending = false;
                break;
            case 'D': // CUB - Cursor Backward
                _cursorCol = Math.Max(_cursorCol - Math.Max(args[0], 1), 0);
                _wrapPending = false;
                break;
            case 'E': // CNL - Cursor Next Line
                _cursorCol = 0;
                _cursorRow = Math.Min(_cursorRow + Math.Max(args[0], 1), Rows - 1);
                break;
            case 'F': // CPL - Cursor Previous Line
                _cursorCol = 0;
                _cursorRow = Math.Max(_cursorRow - Math.Max(args[0], 1), 0);
                break;
            case 'G': // CHA - Cursor Character Absolute
                _cursorCol = Math.Clamp((args[0] > 0 ? args[0] : 1) - 1, 0, Columns - 1);
                _wrapPending = false;
                break;
            case 'H': // CUP - Cursor Position
            case 'f': // HVP
                _cursorRow = Math.Clamp((args[0] > 0 ? args[0] : 1) - 1, 0, Rows - 1);
                _cursorCol = Math.Clamp((args.Length > 1 && args[1] > 0 ? args[1] : 1) - 1, 0, Columns - 1);
                _wrapPending = false;
                break;
            case 'J': // ED - Erase in Display
                EraseDisplay(args[0]);
                break;
            case 'K': // EL - Erase in Line
                EraseLine(args[0]);
                break;
            case 'L': // IL - Insert Lines
                InsertLines(Math.Max(args[0], 1));
                break;
            case 'M': // DL - Delete Lines
                DeleteLines(Math.Max(args[0], 1));
                break;
            case 'P': // DCH - Delete Characters
                DeleteChars(Math.Max(args[0], 1));
                break;
            case '@': // ICH - Insert Characters
                InsertChars(Math.Max(args[0], 1));
                break;
            case 'S': // SU - Scroll Up
                ScrollUp(Math.Max(args[0], 1));
                break;
            case 'T': // SD - Scroll Down
                ScrollDown(Math.Max(args[0], 1));
                break;
            case 'X': // ECH - Erase Character
                EraseChars(Math.Max(args[0], 1));
                break;
            case 'd': // VPA - Vertical Position Absolute
                _cursorRow = Math.Clamp((args[0] > 0 ? args[0] : 1) - 1, 0, Rows - 1);
                _wrapPending = false;
                break;
            case 'm': // SGR - Select Graphic Rendition
                HandleSgr(args);
                break;
            case 'n': // DSR - Device Status Report
                if (args[0] == 6)
                    SendData?.Invoke(Encoding.ASCII.GetBytes($"\x1B[{_cursorRow + 1};{_cursorCol + 1}R"));
                break;
            case 'r': // DECSTBM - Set Scrolling Region
                _scrollTop = Math.Clamp((args[0] > 0 ? args[0] : 1) - 1, 0, Rows - 1);
                _scrollBottom = Math.Clamp((args.Length > 1 && args[1] > 0 ? args[1] : Rows) - 1, 0, Rows - 1);
                _cursorRow = 0;
                _cursorCol = 0;
                break;
            case 's': // SCP - Save Cursor Position
                SaveCursor();
                break;
            case 'u': // RCP - Restore Cursor Position
                RestoreCursor();
                break;
            case 'c': // DA - Device Attributes
                SendData?.Invoke(Encoding.ASCII.GetBytes("\x1B[?1;2c"));
                break;
            case 't': // Window manipulation (ignore)
                break;
            case 'l': // Reset mode (non-private)
            case 'h': // Set mode (non-private)
                break;
        }
    }

    private void HandlePrivateMode(string paramStr, char terminator)
    {
        var args = ParseParams(paramStr);
        bool set = terminator == 'h';

        foreach (var code in args)
        {
            switch (code)
            {
                case 1: // DECCKM - Application Cursor Keys
                    ApplicationCursorKeys = set;
                    break;
                case 7: // DECAWM - Auto-Wrap
                    _autoWrap = set;
                    break;
                case 12: // Cursor blink (ignore)
                    break;
                case 25: // DECTCEM - Show/Hide Cursor
                    CursorVisible = set;
                    break;
                case 47: // Alternate screen buffer (save/restore)
                case 1047:
                    if (set) SwitchToAltBuffer();
                    else SwitchToMainBuffer();
                    break;
                case 1048:
                    if (set) SaveCursor();
                    else RestoreCursor();
                    break;
                case 1049: // Alternate screen buffer + save/restore cursor
                    if (set) { SaveCursor(); SwitchToAltBuffer(); }
                    else { SwitchToMainBuffer(); RestoreCursor(); }
                    break;
                case 2004: // Bracketed paste mode (acknowledge but ignore)
                    break;
            }
        }
    }

    private void HandleSgr(int[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && args[0] == 0))
        {
            ResetAttributes();
            return;
        }

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case 0: ResetAttributes(); break;
                case 1: _bold = true; break;
                case 3: break; // italic (not tracked separately)
                case 4: _underline = true; break;
                case 7: _inverse = true; break;
                case 22: _bold = false; break;
                case 23: break; // not italic
                case 24: _underline = false; break;
                case 27: _inverse = false; break;
                case >= 30 and <= 37:
                    _currentFg = Ansi16[args[i] - 30 + (_bold ? 8 : 0)];
                    break;
                case 38: // Extended foreground
                    i = ParseExtendedColor(args, i, out _currentFg);
                    break;
                case 39: _currentFg = 0; break; // default fg
                case >= 40 and <= 47:
                    _currentBg = Ansi16[args[i] - 40];
                    break;
                case 48: // Extended background
                    i = ParseExtendedColor(args, i, out _currentBg);
                    break;
                case 49: _currentBg = 0; break; // default bg
                case >= 90 and <= 97:
                    _currentFg = Ansi16[args[i] - 90 + 8];
                    break;
                case >= 100 and <= 107:
                    _currentBg = Ansi16[args[i] - 100 + 8];
                    break;
            }
        }
    }

    private static int ParseExtendedColor(int[] args, int i, out uint color)
    {
        color = 0;
        if (i + 1 >= args.Length) return i;

        if (args[i + 1] == 5 && i + 2 < args.Length)
        {
            // 256-color: ESC[38;5;Nm
            int idx = Math.Clamp(args[i + 2], 0, 255);
            color = Palette256[idx];
            return i + 2;
        }

        if (args[i + 1] == 2 && i + 4 < args.Length)
        {
            // True color: ESC[38;2;R;G;Bm
            int r = Math.Clamp(args[i + 2], 0, 255);
            int g = Math.Clamp(args[i + 3], 0, 255);
            int b = Math.Clamp(args[i + 4], 0, 255);
            color = 0xFF000000 | (uint)(r << 16) | (uint)(g << 8) | (uint)b;
            return i + 4;
        }

        return i;
    }

    #endregion

    #region Buffer Operations

    private void PutChar(char c)
    {
        if (_wrapPending)
        {
            _cursorCol = 0;
            LineFeed();
            _wrapPending = false;
        }

        if (_cursorRow >= 0 && _cursorRow < Rows && _cursorCol >= 0 && _cursorCol < Columns)
        {
            _activeBuffer[_cursorRow, _cursorCol] = new TerminalCell
            {
                Character = c,
                Foreground = _currentFg,
                Background = _currentBg,
                Bold = _bold,
                Underline = _underline,
                Inverse = _inverse
            };
        }

        if (_cursorCol < Columns - 1)
            _cursorCol++;
        else if (_autoWrap)
            _wrapPending = true;
    }

    private void LineFeed()
    {
        _wrapPending = false;
        if (_cursorRow == _scrollBottom)
            ScrollUp(1);
        else if (_cursorRow < Rows - 1)
            _cursorRow++;
    }

    private void ReverseIndex()
    {
        if (_cursorRow == _scrollTop)
            ScrollDown(1);
        else if (_cursorRow > 0)
            _cursorRow--;
    }

    private void ScrollUp(int lines)
    {
        for (int n = 0; n < lines; n++)
        {
            // Save top line to scrollback (main buffer only, not in alt)
            if (!_useAltBuffer && _scrollTop == 0)
            {
                var saved = new TerminalCell[Columns];
                for (int c = 0; c < Columns; c++)
                    saved[c] = _activeBuffer[_scrollTop, c];
                _scrollback.Add(saved);
                if (_scrollback.Count > MaxScrollback)
                    _scrollback.RemoveAt(0);
            }

            // Shift rows up within scroll region
            for (int r = _scrollTop; r < _scrollBottom; r++)
                for (int c = 0; c < Columns; c++)
                    _activeBuffer[r, c] = _activeBuffer[r + 1, c];

            // Clear bottom row
            for (int c = 0; c < Columns; c++)
                _activeBuffer[_scrollBottom, c] = TerminalCell.Empty;
        }
    }

    private void ScrollDown(int lines)
    {
        for (int n = 0; n < lines; n++)
        {
            for (int r = _scrollBottom; r > _scrollTop; r--)
                for (int c = 0; c < Columns; c++)
                    _activeBuffer[r, c] = _activeBuffer[r - 1, c];

            for (int c = 0; c < Columns; c++)
                _activeBuffer[_scrollTop, c] = TerminalCell.Empty;
        }
    }

    private void EraseDisplay(int mode)
    {
        switch (mode)
        {
            case 0: // Below
                EraseLine(0);
                for (int r = _cursorRow + 1; r < Rows; r++)
                    ClearRow(r);
                break;
            case 1: // Above
                for (int c = 0; c <= _cursorCol && c < Columns; c++)
                    _activeBuffer[_cursorRow, c] = TerminalCell.Empty;
                for (int r = 0; r < _cursorRow; r++)
                    ClearRow(r);
                break;
            case 2: // All
                for (int r = 0; r < Rows; r++)
                    ClearRow(r);
                break;
            case 3: // All + scrollback
                for (int r = 0; r < Rows; r++)
                    ClearRow(r);
                _scrollback.Clear();
                break;
        }
    }

    private void EraseLine(int mode)
    {
        switch (mode)
        {
            case 0: // Right
                for (int c = _cursorCol; c < Columns; c++)
                    _activeBuffer[_cursorRow, c] = TerminalCell.Empty;
                break;
            case 1: // Left
                for (int c = 0; c <= _cursorCol && c < Columns; c++)
                    _activeBuffer[_cursorRow, c] = TerminalCell.Empty;
                break;
            case 2: // All
                ClearRow(_cursorRow);
                break;
        }
    }

    private void EraseChars(int count)
    {
        for (int i = 0; i < count && _cursorCol + i < Columns; i++)
            _activeBuffer[_cursorRow, _cursorCol + i] = TerminalCell.Empty;
    }

    private void InsertLines(int count)
    {
        if (_cursorRow < _scrollTop || _cursorRow > _scrollBottom) return;
        for (int n = 0; n < count; n++)
        {
            for (int r = _scrollBottom; r > _cursorRow; r--)
                for (int c = 0; c < Columns; c++)
                    _activeBuffer[r, c] = _activeBuffer[r - 1, c];
            ClearRow(_cursorRow);
        }
    }

    private void DeleteLines(int count)
    {
        if (_cursorRow < _scrollTop || _cursorRow > _scrollBottom) return;
        for (int n = 0; n < count; n++)
        {
            for (int r = _cursorRow; r < _scrollBottom; r++)
                for (int c = 0; c < Columns; c++)
                    _activeBuffer[r, c] = _activeBuffer[r + 1, c];
            ClearRow(_scrollBottom);
        }
    }

    private void InsertChars(int count)
    {
        for (int c = Columns - 1; c >= _cursorCol + count; c--)
            _activeBuffer[_cursorRow, c] = _activeBuffer[_cursorRow, c - count];
        for (int i = 0; i < count && _cursorCol + i < Columns; i++)
            _activeBuffer[_cursorRow, _cursorCol + i] = TerminalCell.Empty;
    }

    private void DeleteChars(int count)
    {
        for (int c = _cursorCol; c + count < Columns; c++)
            _activeBuffer[_cursorRow, c] = _activeBuffer[_cursorRow, c + count];
        for (int c = Math.Max(_cursorCol, Columns - count); c < Columns; c++)
            _activeBuffer[_cursorRow, c] = TerminalCell.Empty;
    }

    private void SwitchToAltBuffer()
    {
        if (_useAltBuffer) return;
        _useAltBuffer = true;
        _activeBuffer = _altBuffer;
        ClearActiveBuffer();
    }

    private void SwitchToMainBuffer()
    {
        if (!_useAltBuffer) return;
        _useAltBuffer = false;
        _activeBuffer = _mainBuffer;
    }

    private void SaveCursor()
    {
        _savedCursorRow = _cursorRow;
        _savedCursorCol = _cursorCol;
        _savedFg = _currentFg;
        _savedBg = _currentBg;
        _savedBold = _bold;
        _savedUnderline = _underline;
        _savedInverse = _inverse;
    }

    private void RestoreCursor()
    {
        _cursorRow = Math.Clamp(_savedCursorRow, 0, Rows - 1);
        _cursorCol = Math.Clamp(_savedCursorCol, 0, Columns - 1);
        _currentFg = _savedFg;
        _currentBg = _savedBg;
        _bold = _savedBold;
        _underline = _savedUnderline;
        _inverse = _savedInverse;
        _wrapPending = false;
    }

    private void ResetAttributes()
    {
        _currentFg = 0;
        _currentBg = 0;
        _bold = false;
        _underline = false;
        _inverse = false;
    }

    private void Reset()
    {
        ResetAttributes();
        _cursorRow = 0;
        _cursorCol = 0;
        _scrollTop = 0;
        _scrollBottom = Rows - 1;
        CursorVisible = true;
        ApplicationCursorKeys = false;
        _autoWrap = true;
        _wrapPending = false;
        _useAltBuffer = false;
        _activeBuffer = _mainBuffer;
        ClearActiveBuffer();
        _scrollback.Clear();
    }

    #endregion

    #region Helpers

    private void ClearRow(int row)
    {
        for (int c = 0; c < Columns; c++)
            _activeBuffer[row, c] = TerminalCell.Empty;
    }

    private void ClearActiveBuffer()
    {
        for (int r = 0; r < Rows; r++)
            ClearRow(r);
    }

    private static TerminalCell[,] CreateBuffer(int rows, int cols)
    {
        var buf = new TerminalCell[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                buf[r, c] = TerminalCell.Empty;
        return buf;
    }

    private static void CopyBuffer(TerminalCell[,] src, TerminalCell[,] dst, int rows, int cols)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                dst[r, c] = src[r, c];
    }

    private static int[] ParseParams(string s)
    {
        if (string.IsNullOrEmpty(s)) return new[] { 0 };
        var parts = s.Split(';');
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            int.TryParse(parts[i], out result[i]);
        return result;
    }

    private static uint[] Build256Palette()
    {
        var p = new uint[256];
        Array.Copy(Ansi16, p, 16);
        // 16-231: 6×6×6 color cube
        for (int i = 0; i < 216; i++)
        {
            int r = i / 36 == 0 ? 0 : 55 + (i / 36) * 40;
            int g = (i / 6) % 6 == 0 ? 0 : 55 + ((i / 6) % 6) * 40;
            int b = i % 6 == 0 ? 0 : 55 + (i % 6) * 40;
            p[16 + i] = 0xFF000000 | (uint)(r << 16) | (uint)(g << 8) | (uint)b;
        }
        // 232-255: grayscale
        for (int i = 0; i < 24; i++)
        {
            int v = 8 + i * 10;
            p[232 + i] = 0xFF000000 | (uint)(v << 16) | (uint)(v << 8) | (uint)v;
        }
        return p;
    }

    #endregion
}
