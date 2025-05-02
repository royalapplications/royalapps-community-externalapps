using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

internal static class Program
{
    internal static readonly StringWriter ConsoleOutput = new();

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Console.SetOut(ConsoleOutput);

        using var host = new HostBuilder()
            .ConfigureLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "HH:mm:ss.fff ";
                    options.IncludeScopes = true;
                })
            ).Build();

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        ExternalApps.Initialize();
        Application.Run(new FormMain(loggerFactory));
        ExternalApps.Cleanup();

    }
}
