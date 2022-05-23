using DevExpress.XtraBars;

using Microsoft.Extensions.Logging;

using RoyalApps.Community.ExternalApps.WinForms.WindowManagement;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoyalApps.Community.ExternalApps.WinForms.DxDemo
{
    public partial class RibbonForm1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RibbonForm1> _logger;

        private readonly Timer _logWriterTimer = new();

        public RibbonForm1(ILoggerFactory loggerFactory)
        {
            InitializeComponent();

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<RibbonForm1>();

            _logWriterTimer.Tick += (_, _) =>
            {
                var text = Program.ConsoleOutput.GetStringBuilder().ToString();
                if (text.Length == TextBoxLog.TextLength)
                    return;
                TextBoxLog.Text = text;
                TextBoxLog.SelectionStart = TextBoxLog.TextLength;
                TextBoxLog.ScrollToCaret();
            };
            _logWriterTimer.Interval = 500;
            _logWriterTimer.Enabled = true;

        }

        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            AddApplication(new ExternalAppConfiguration { Executable = barEditItem1.EditValue.ToString() });
        }

        private void AddApplication(ExternalAppConfiguration externalAppConfiguration)
        {
            if (string.IsNullOrWhiteSpace(externalAppConfiguration.Executable))
            {
                _logger.LogWarning("No executable specified");
                return;
            }
            if (!File.Exists(externalAppConfiguration.Executable))
            {
                _logger.LogError("Executable not found: {Command}", externalAppConfiguration.Executable);
                return;
            }

            Program.ConsoleOutput.GetStringBuilder().Clear();

            externalAppConfiguration.EmbedMethod = EmbedMethod.Window;

            var fileInfo = new FileInfo(externalAppConfiguration.Executable!);
            var caption = fileInfo.Name;

            var externalApp = new ExternalAppHost
            {
                Dock = DockStyle.Fill,
                LoggerFactory = _loggerFactory,
            };

            var doc = (DevExpress.XtraBars.Docking2010.Views.Tabbed.Document)tabbedView1.AddDocument(externalApp, caption);

            _logger.LogInformation("Starting application: {Command}", externalAppConfiguration.Executable);
            externalApp.Start(externalAppConfiguration);
        }

    }
}