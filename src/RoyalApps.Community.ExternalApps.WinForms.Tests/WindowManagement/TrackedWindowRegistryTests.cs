using System;
using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class TrackedWindowRegistryTests
{
    [Fact]
    public void Register_MarksWindowAsTracked()
    {
        var registry = new TrackedWindowRegistry();

        var added = registry.Register((nint)0x1010);

        Assert.True(added);
        Assert.True(registry.IsTracked((nint)0x1010));
    }

    [Fact]
    public void Register_ReturnsFalse_ForDuplicateWindow()
    {
        var registry = new TrackedWindowRegistry();
        registry.Register((nint)0x2020);

        var added = registry.Register((nint)0x2020);

        Assert.False(added);
    }

    [Fact]
    public void Unregister_RemovesTrackedWindow()
    {
        var registry = new TrackedWindowRegistry();
        registry.Register((nint)0x3030);

        registry.Unregister((nint)0x3030);

        Assert.False(registry.IsTracked((nint)0x3030));
    }
}
