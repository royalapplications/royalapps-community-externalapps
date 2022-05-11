using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;

namespace RoyalApps.Community.ExternalApps.WinForms;

internal static class LoggerExtensions
{
    public static void WithCallerInfo(
        this ILogger logger, 
        Action<ILogger> action, 
        [CallerMemberName] string memberName = "", 
        [CallerFilePath] string sourceFilePath = "", 
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        using (logger.BeginScope("MemberName: {MemberName}, SourceFile: {SourceFile}, LineNumber: {LineNumber}", 
                   memberName, sourceFilePath, sourceLineNumber))
        {
            action.Invoke(logger);
        } 
    }
}