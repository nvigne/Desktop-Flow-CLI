using System.Diagnostics;

namespace DesktopFlowCLI.Tests;

public class CliTests
{
    [Fact]
    public void HelpCommand_ReturnsSuccess()
    {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, "DesktopFlowCLI", "DesktopFlowCLI.csproj");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" -- --help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        var exited = process.WaitForExit(60000);

        Assert.True(exited, "CLI process did not exit within 60 seconds.");
        Assert.Equal(0, process.ExitCode);
        Assert.Contains("list all desktop flows", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unhandled exception", error, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Desktop-Flow-CLI.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
