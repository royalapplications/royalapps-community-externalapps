using System;

namespace RoyalApps.Community.ExternalApps.WinForms.Embedding;

internal sealed class EmbeddingFailedException : InvalidOperationException
{
    public EmbeddingFailedException(string message, int? nativeErrorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        NativeErrorCode = nativeErrorCode;
    }

    public int? NativeErrorCode { get; }
}
