using System.Diagnostics;
using ExpenseTracker.Services.Contracts;

namespace ExpenseTracker.Infrastructure.Platform;

public class MacPlatformService : IPlatformService
{
    public void CopyToClipboard(string text)
    {
        var psi = new ProcessStartInfo("pbcopy")
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
            Process.Start("open", $"-R \"{filePath}\"");
        else
        {
            var dir = Path.GetDirectoryName(filePath) ?? filePath;
            Process.Start("open", dir);
        }
    }
}
