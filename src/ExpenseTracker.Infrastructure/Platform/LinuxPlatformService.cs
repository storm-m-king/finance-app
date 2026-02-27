using System.Diagnostics;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Platform;

public class LinuxPlatformService : IPlatformService
{
    public void CopyToClipboard(string text)
    {
        var psi = new ProcessStartInfo("xclip", "-selection clipboard")
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
        var dir = File.Exists(filePath)
            ? Path.GetDirectoryName(filePath) ?? filePath
            : filePath;
        Process.Start("xdg-open", dir);
    }
}
