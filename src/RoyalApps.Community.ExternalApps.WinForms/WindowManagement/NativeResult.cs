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
    public static readonly NativeResult Success = new(true);
    
    /// <summary>
    /// 
    /// </summary>
    public static NativeResult Fail(Exception? exception = null) => new(false, exception);
    
    /// <summary>
    /// 
    /// </summary>
    public Exception? Exception { get; }
    /// <summary>
    /// 
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="succeeded"></param>
    /// <param name="exception"></param>
    private NativeResult(bool succeeded, Exception? exception = null)
    {
        Succeeded = succeeded;
        Exception = exception;
    }
}