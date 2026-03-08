using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoyalApps.Community.ExternalApps.WinForms.Launching;

internal sealed class ExecutableResolver
{
    public string Resolve(string executable, IEnumerable<KeyValuePair<string, string>>? environmentVariables)
    {
        if (string.IsNullOrWhiteSpace(executable))
            throw new ArgumentException("Executable must not be empty.", nameof(executable));

        if (HasDirectoryComponent(executable) || Path.IsPathRooted(executable) || environmentVariables == null)
            return executable;

        var environment = environmentVariables as KeyValuePair<string, string>[] ?? environmentVariables.ToArray();
        var pathEntries = GetPathEntries(environment);
        var pathExtEntries = GetPathExtensions(environment);

        foreach (var pathEntry in pathEntries)
        {
            foreach (var candidate in EnumerateCandidates(pathEntry, executable, pathExtEntries))
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return executable;
    }

    private static bool HasDirectoryComponent(string executable)
    {
        return executable.Contains(Path.DirectorySeparatorChar) || executable.Contains(Path.AltDirectorySeparatorChar);
    }

    private static IEnumerable<string> GetPathEntries(IEnumerable<KeyValuePair<string, string>>? environmentVariables)
    {
        var pathValue = TryGetEnvironmentValue(environmentVariables, "PATH") ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string[] GetPathExtensions(IEnumerable<KeyValuePair<string, string>>? environmentVariables)
    {
        var pathExtValue = TryGetEnvironmentValue(environmentVariables, "PATHEXT") ?? Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrWhiteSpace(pathExtValue))
            return [".exe", ".cmd", ".bat", ".com"];

        return pathExtValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateCandidates(string pathEntry, string executable, string[] pathExtEntries)
    {
        yield return Path.Combine(pathEntry, executable);

        if (Path.HasExtension(executable))
            yield break;

        foreach (var extension in pathExtEntries)
            yield return Path.Combine(pathEntry, executable + extension);
    }

    private static string? TryGetEnvironmentValue(IEnumerable<KeyValuePair<string, string>>? environmentVariables, string key)
    {
        if (environmentVariables == null)
            return null;

        foreach (var pair in environmentVariables)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }
}
