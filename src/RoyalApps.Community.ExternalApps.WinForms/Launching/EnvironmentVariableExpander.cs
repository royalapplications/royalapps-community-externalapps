using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RoyalApps.Community.ExternalApps.WinForms.Launching;

internal sealed partial class EnvironmentVariableExpander
{
    public string? Expand(string? value, IEnumerable<KeyValuePair<string, string>>? environmentVariables)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return EnvironmentVariablePattern().Replace(
            value,
            match =>
            {
                var variableName = match.Groups[1].Value;
                var resolvedValue = TryGetEnvironmentValue(environmentVariables, variableName)
                                    ?? Environment.GetEnvironmentVariable(variableName);
                return resolvedValue ?? match.Value;
            });
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

    [GeneratedRegex("%([^%]+)%", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentVariablePattern();
}
