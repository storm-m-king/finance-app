using System.Diagnostics;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Platform;

public class WindowsPlatformService : IPlatformService
{
    public void CopyToClipboard(string text)
    {
        var psi = new ProcessStartInfo("clip.exe")
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process != null)
        {
            process.StandardInput.Write(text);
            process.StandardInput.Close();
        }
    }

    public void RevealFileInExplorer(string filePath)
    {
        if (File.Exists(filePath))
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        else
        {
            var dir = Path.GetDirectoryName(filePath) ?? filePath;
            Process.Start("explorer.exe", dir);
        }
    }
}
