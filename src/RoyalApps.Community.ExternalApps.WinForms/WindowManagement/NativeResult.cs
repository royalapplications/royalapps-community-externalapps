namespace RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

using System;

/// <summary>
/// Encapsulate the result for native operation.
/// </summary>
public class NativeResult
{
    /// <summary>
    /// Gets a successful result.
    /// </summary>
    public static readonly NativeResult Success = new(true);

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeResult"/> class.
    /// </summary>
    /// <param name="succeeded">The status of the result.</param>
    /// <param name="exception">An optional <see cref="Exception"/> for a failed result.</param>
    private NativeResult(bool succeeded, Exception? exception = null)
    {
        Succeeded = succeeded;
        Exception = exception;
    }

    /// <summary>
    /// Gets the <see cref="Exception"/> of the failed result, if present.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="exception">An optional <see cref="Exception"/> that caused the failure.</param>
    /// <returns>A <see cref="NativeResult"/>.</returns>
    public static NativeResult Fail(Exception? exception = null) => new(false, exception);
}