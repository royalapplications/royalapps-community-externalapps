using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Discovery;

internal sealed class ProcessMetadataProvider
{
    private readonly ILogger<ProcessMetadataProvider> _logger;

    public ProcessMetadataProvider(ILogger<ProcessMetadataProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Dictionary<int, string> LoadCommandLines()
    {
        var commandLines = new Dictionary<int, string>();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process");
            foreach (var result in searcher.Get().OfType<ManagementObject>())
            {
                var processIdValue = result["ProcessId"]?.ToString();
                if (!int.TryParse(processIdValue, out var processId))
                    continue;

                commandLines[processId] = result["CommandLine"]?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Loading process command lines failed");
        }

        return commandLines;
    }

    public ProcessSnapshot? TryGetSnapshot(int processId, IReadOnlyDictionary<int, string> commandLines)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            string executablePath;
            try
            {
                executablePath = process.MainModule?.FileName ?? string.Empty;
            }
            catch (Win32Exception ex)
            {
                _logger.LogDebug(ex, "Cannot read executable path for process id {ProcessId}", processId);
                executablePath = string.Empty;
            }

            return new ProcessSnapshot(
                processId,
                process.ProcessName,
                executablePath,
                commandLines.GetValueOrDefault(processId, string.Empty));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Loading process metadata failed for process id {ProcessId}", processId);
            return null;
        }
    }
}
