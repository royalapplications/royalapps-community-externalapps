using System;

namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

/// <summary>
/// 
/// </summary>
public class NativeResult
{
    /// <summary>
    /// 
    /// </summary>
    public Exception? Exception { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="success"></param>
    /// <param name="exception"></param>
    public NativeResult(bool success, Exception? exception = null)
    {
        Success = success;
        Exception = exception;
    }
}