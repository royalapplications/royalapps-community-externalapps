using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using DevExpress.Utils;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RoyalApps.Community.ExternalApps.WinForms.DxDemo;

internal static class Program
{
    internal static readonly StringWriter ConsoleOutput = new();

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Console.SetOut(ConsoleOutput);

        using var host = new HostBuilder()
            .ConfigureLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "HH:mm:ss.fff ";
                    options.IncludeScopes = true;
                })
            ).Build();

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        ExternalApps.Initialize();

        WindowsFormsSettings.AllowArrowDragIndicators = true;
        WindowsFormsSettings.AllowAutoFilterConditionChange = DefaultBoolean.True;
        WindowsFormsSettings.AllowAutoScale = DefaultBoolean.True;
        WindowsFormsSettings.AllowDefaultSvgImages = DefaultBoolean.True;
        WindowsFormsSettings.AllowDpiScale = true;
        WindowsFormsSettings.AllowOverpanApplicationWindow = DefaultBoolean.True;
        WindowsFormsSettings.AllowRoundedWindowCorners = DefaultBoolean.True;
        WindowsFormsSettings.AllowWindowGhosting = true;
        WindowsFormsSettings.AutoCorrectForeColor = DefaultBoolean.True;
        WindowsFormsSettings.ColumnAutoFilterMode = ColumnAutoFilterMode.Default;
        WindowsFormsSettings.ColumnFilterPopupMode = ColumnFilterPopupMode.Default;
        WindowsFormsSettings.CustomizationFormSnapMode = DevExpress.Utils.Controls.SnapMode.All;
        WindowsFormsSettings.DefaultAllowHtmlDraw = true;
        WindowsFormsSettings.DefaultFont = SystemFonts.MessageBoxFont;
        WindowsFormsSettings.DefaultMenuFont = SystemFonts.MenuFont;
        WindowsFormsSettings.DragScrollThumbBeyondControlMode = DragScrollThumbBeyondControlMode.Default;
        WindowsFormsSettings.DockingViewStyle = DevExpress.XtraBars.Docking2010.Views.DockingViewStyle.Light;
        WindowsFormsSettings.FocusRectStyle = DevExpress.Utils.Paint.DXDashStyle.Default;
        WindowsFormsSettings.InplaceEditorUpdateMode = InplaceEditorUpdateMode.Immediate;
        WindowsFormsSettings.PopupMenuStyle = DevExpress.XtraEditors.Controls.PopupMenuStyle.Classic;
        WindowsFormsSettings.PopupShadowStyle = PopupShadowStyle.Default;
        WindowsFormsSettings.ShowTouchScrollBarOnMouseMove = true;
        WindowsFormsSettings.SmartMouseWheelProcessing = true;
        WindowsFormsSettings.SvgImageRenderingMode = SvgImageRenderingMode.HighQuality;
        WindowsFormsSettings.UseAdvancedTextEdit = DefaultBoolean.True;
        WindowsFormsSettings.UseDXDialogs = DefaultBoolean.True;

        if (WindowsFormsSettings.OptimizeRemoteConnectionPerformance != DefaultBoolean.False)
        {
            WindowsFormsSettings.FormThickBorder = true;
            WindowsFormsSettings.ThickBorderWidth = 1;
        }

        WindowsFormsSettings.AllowRibbonFormGlass = DefaultBoolean.False;

        WindowsFormsSettings.EnableFormSkins();
        WindowsFormsSettings.EnableMdiFormSkins();


        Application.Run(new RibbonForm1(loggerFactory));
        ExternalApps.Cleanup();
    }
}