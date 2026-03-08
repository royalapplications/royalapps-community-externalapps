using System.IO;
using System.Text;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

internal sealed class ThreadSafeStringWriter : TextWriter
{
    private readonly StringBuilder _builder = new();
    private readonly object _syncRoot = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        lock (_syncRoot)
        {
            _builder.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (value == null)
            return;

        lock (_syncRoot)
        {
            _builder.Append(value);
        }
    }

    public override void WriteLine(string? value)
    {
        lock (_syncRoot)
        {
            _builder.AppendLine(value);
        }
    }

    public string Snapshot()
    {
        lock (_syncRoot)
        {
            return _builder.ToString();
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _builder.Clear();
        }
    }
}
