using System.Diagnostics;

namespace DesktopFlowCLI.Tests;

public class CliTests
{
    [Fact]
    public async Task HelpCommand_ReturnsSuccess()
    {
        var root = GetRepositoryRoot();
        var projectPath = Path.Combine(root, "DesktopFlowCLI", "DesktopFlowCLI.csproj");

        using var process = new Process
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
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await process.WaitForExitAsync(cancellation.Token);

        var output = await outputTask;
        var error = await errorTask;

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("Commands:", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("list", output, StringComparison.OrdinalIgnoreCase);
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
