using System;
using System.Collections.Generic;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class EnvironmentVariableExpanderTests
{
    [Fact]
    public void Expand_ReplacesVariablesFromCustomEnvironment()
    {
        var expander = new EnvironmentVariableExpander();

        var expanded = expander.Expand(
            @"%TOOLS_ROOT%\demo.exe",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TOOLS_ROOT"] = @"C:\Tools"
            });

        Assert.Equal(@"C:\Tools\demo.exe", expanded);
    }

    [Fact]
    public void Expand_PreservesUnknownVariables()
    {
        var expander = new EnvironmentVariableExpander();

        var expanded = expander.Expand(@"%DOES_NOT_EXIST%\demo.exe", environmentVariables: null);

        Assert.Equal(@"%DOES_NOT_EXIST%\demo.exe", expanded);
    }

    [Fact]
    public void Expand_UsesProcessEnvironmentWhenCustomEnvironmentDoesNotOverride()
    {
        var variableName = "ROYALAPPS_EXTERNALAPPS_TEST_EXPAND";
        Environment.SetEnvironmentVariable(variableName, @"C:\ProcessValue");

        try
        {
            var expander = new EnvironmentVariableExpander();

            var expanded = expander.Expand($@"%{variableName}%\demo.exe", environmentVariables: null);

            Assert.Equal(@"C:\ProcessValue\demo.exe", expanded);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
