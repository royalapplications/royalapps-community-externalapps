using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class ExecutableResolverTests
{
    [Fact]
    public void Resolve_ReturnsOriginalValue_WhenExecutableAlreadyHasDirectory()
    {
        var resolver = new ExecutableResolver();
        var executable = @"C:\Windows\System32\notepad.exe";

        var resolved = resolver.Resolve(executable, environmentVariables: null);

        Assert.Equal(executable, resolved);
    }

    [Fact]
    public void Resolve_FindsExecutableUsingCustomPath()
    {
        var resolver = new ExecutableResolver();
        var testDirectory = Path.Combine(Path.GetTempPath(), "externalapps-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        var executablePath = Path.Combine(testDirectory, "demo-tool.cmd");
        File.WriteAllText(executablePath, "@echo off");

        try
        {
            var resolved = resolver.Resolve(
                "demo-tool",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PATH"] = testDirectory,
                    ["PATHEXT"] = ".cmd"
                });

            Assert.Equal(executablePath, resolved, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void Resolve_ReturnsOriginalValue_WhenExecutableCannotBeResolved()
    {
        var resolver = new ExecutableResolver();

        var resolved = resolver.Resolve(
            "definitely-not-a-real-command",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["PATH"] = Path.GetTempPath()
            });

        Assert.Equal("definitely-not-a-real-command", resolved);
    }
}
