using System.ComponentModel;
using System.Diagnostics;

namespace TeamTalk.AppHost;

// Aspire falls back to another installed runtime (e.g. Podman) when Docker is down, which silently
// starts a fresh, empty Postgres. Fail fast instead so a missing prerequisite is never masked.
internal static class ContainerRuntimePreflight
{
    private const string RuntimeEnvironmentVariable = "ASPIRE_CONTAINER_RUNTIME";
    private const string DefaultRuntime = "docker";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static void EnsureRuntimeIsRunning()
    {
        var runtime = Environment.GetEnvironmentVariable(RuntimeEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(runtime))
        {
            runtime = DefaultRuntime;
        }

        var startInfo = new ProcessStartInfo(runtime, "info")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Could not start '{runtime}'.");

            var errorTask = process.StandardError.ReadToEndAsync();

            _ = process.StandardOutput.ReadToEndAsync();

            if (process.WaitForExit(Timeout) is false)
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidOperationException(
                    $"Container runtime '{runtime}' did not respond within {Timeout.TotalSeconds:0} seconds. "
                    + "Make sure it is running, then start the AppHost again.");
            }

            if (process.ExitCode is not 0)
            {
                var error = errorTask.GetAwaiter().GetResult().Trim();
                throw new InvalidOperationException(
                    $"Container runtime '{runtime}' is not running. Start it, then start the AppHost again. {error}".TrimEnd());
            }
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                $"Container runtime '{runtime}' was not found. Install it and make sure it is on your PATH.",
                exception);
        }
    }
}
