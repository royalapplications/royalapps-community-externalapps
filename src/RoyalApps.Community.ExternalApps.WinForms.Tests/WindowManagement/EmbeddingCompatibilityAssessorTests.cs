using Xunit;

namespace RoyalApps.Community.ExternalApps.WinForms.Tests.WindowManagement;

public sealed class EmbeddingCompatibilityAssessorTests
{
    [Fact]
    public void Assess_MarksPackagedExecutableAsPreferringExternalHosting()
    {
        var candidate = CreateCandidate(
            executablePath: @"C:\Program Files\WindowsApps\Contoso.Notepad_1.0.0.0_x64__8wekyb3d8bbwe\Notepad.exe");
        var assessor = new EmbeddingCompatibilityAssessor();

        var assessment = assessor.Assess(candidate);

        Assert.True(assessment.PrefersExternalHosting);
        Assert.Contains("WindowsApps", assessment.Warning);
    }

    [Fact]
    public void Assess_MarksKnownModernWindowClassAsPreferringExternalHosting()
    {
        var candidate = CreateCandidate(className: "WinUIDesktopWin32WindowClass");
        var assessor = new EmbeddingCompatibilityAssessor();

        var assessment = assessor.Assess(candidate);

        Assert.True(assessment.PrefersExternalHosting);
        Assert.Contains("WinUIDesktopWin32WindowClass", assessment.Warning);
    }

    [Fact]
    public void Assess_LeavesClassicWin32WindowUntouched()
    {
        var candidate = CreateCandidate(
            executablePath: @"C:\Windows\System32\write.exe",
            className: "WordPadClass");
        var assessor = new EmbeddingCompatibilityAssessor();

        var assessment = assessor.Assess(candidate);

        Assert.False(assessment.PrefersExternalHosting);
        Assert.Equal(string.Empty, assessment.Warning);
    }

    private static ExternalWindowCandidate CreateCandidate(
        string executablePath = @"C:\Test\TestProcess.exe",
        string className = "TestWindowClass",
        string commandLine = "\"C:\\Test\\TestProcess.exe\"")
    {
        return new ExternalWindowCandidate
        {
            WindowHandle = (nint)0x7001,
            ProcessId = 7001,
            ProcessName = "TestProcess",
            ExecutablePath = executablePath,
            CommandLine = commandLine,
            ClassName = className,
            WindowTitle = "Test Window",
            IsVisible = true,
            IsTopLevel = true,
            PrefersExternalHosting = false,
            EmbeddingCompatibilityWarning = string.Empty
        };
    }
}
