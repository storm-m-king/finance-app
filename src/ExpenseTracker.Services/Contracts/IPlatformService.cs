namespace ExpenseTracker.Services.Contracts;

/// <summary>
/// Abstracts platform-specific operations (clipboard, file explorer, etc.)
/// so consuming code doesn't need OS-detection logic.
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Copies the given text to the system clipboard.
    /// </summary>
    void CopyToClipboard(string text);

    /// <summary>
    /// Opens the OS file explorer to the directory containing the specified file.
    /// If the file exists, it is selected/highlighted.
    /// </summary>
    void RevealFileInExplorer(string filePath);
}
