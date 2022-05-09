using System;

namespace RoyalApps.Community.ExternalApps.WinForms;

public class NativeResult
{
    public Exception? Exception { get; set; }
    public bool Success { get; set; }

    public NativeResult(bool success, Exception? exception = null)
    {
        Success = success;
        Exception = exception;
    }
}