using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.Extensions;

internal static class LoggerExtensions
{
    public static void WithCallerInfo(
        this ILogger logger, 
        Action<ILogger> action, 
        [CallerMemberName] string memberName = "", 
        [CallerFilePath] string sourceFilePath = "", 
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (logger.BeginScope("T:{Thread} MemberName: {MemberName}, SourceFile: {SourceFile}, LineNumber: {LineNumber}", 
                    Thread.CurrentThread.IsBackground ? $"#{Thread.CurrentThread.ManagedThreadId}" : "UI", memberName, sourceFilePath, sourceLineNumber))
        {
            action.Invoke(logger);
        } 
    }
}