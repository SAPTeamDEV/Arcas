using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Arcas
{
    public class InstallationProgressPage : SetupPage
    {
        private ProgressBar progressBar;
        private Label statusLabel;
        private Label detailLabel;
        private BackgroundWorker installWorker;

        public override string Title => "Installing";
        public override string Subtitle => "Please wait while Setup installs " + SetupConfigurationManager.Definition.Application.Name + " on your computer";
        public override bool CanGoBack => false;
        public override bool CanGoNext => false;

        public bool InstallationComplete { get; private set; }

        public override Control CreateContent()
        {
            var panel = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor,
                Padding = new Padding(0, 20, 0, 0)
            };

            var appName = SetupConfigurationManager.Definition.Application.Name;
            var instructionLabel = SetupDesign.CreateBodyLabel($"Setup is installing {appName}. This may take several minutes.");
            instructionLabel.Dock = DockStyle.Top;
            instructionLabel.Height = 35;
            instructionLabel.TextAlign = ContentAlignment.TopLeft;
            instructionLabel.Padding = new Padding(0, 0, 0, 15);

            statusLabel = SetupDesign.CreateHeadingLabel("Preparing installation...");
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Height = 35;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = SetupDesign.PrimaryColor;

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 28,
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Margin = new Padding(0, 5, 0, 5)
            };

            detailLabel = SetupDesign.CreateCaptionLabel("");
            detailLabel.Dock = DockStyle.Top;
            detailLabel.Height = 25;
            detailLabel.ForeColor = SetupDesign.TextSecondary;
            detailLabel.TextAlign = ContentAlignment.MiddleLeft;
            detailLabel.Padding = new Padding(0, 0, 0, 15);

            // Log panel with proper styling
            var logPanel = SetupDesign.CreateCard();
            logPanel.Dock = DockStyle.Fill;
            logPanel.Padding = SetupDesign.CompactPadding;

            var logLabel = SetupDesign.CreateCaptionLabel("Installation Log:");
            logLabel.Dock = DockStyle.Top;
            logLabel.Height = 20;
            logLabel.Font = new Font(SetupDesign.CaptionFont, FontStyle.Bold);
            logLabel.TextAlign = ContentAlignment.BottomLeft;

            var logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                BackColor = SetupDesign.BackgroundColor,
                ForeColor = SetupDesign.TextPrimary,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                Margin = new Padding(0, 2, 0, 0)
            };

            logPanel.Controls.Add(logTextBox);
            logPanel.Controls.Add(logLabel);

            panel.Controls.Add(logPanel);
            panel.Controls.Add(detailLabel);
            panel.Controls.Add(progressBar);
            panel.Controls.Add(statusLabel);
            panel.Controls.Add(instructionLabel);

            // Start installation process
            StartInstallation(logTextBox);

            return panel;
        }

        private void StartInstallation(TextBox logTextBox)
        {
            installWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };

            installWorker.DoWork += async (s, e) =>
            {
                try
                {
                    // Use the new command execution system
                    var progress = new Progress<SetupProgressInfo>(info =>
                    {
                        installWorker.ReportProgress(info.Percentage, new InstallProgress
                        {
                            StatusText = info.Operation,
                            DetailText = info.Detail ?? $"{info.Percentage}% complete",
                            LogMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {info.Operation}"
                        });
                    });

                    var executor = new SetupCommandExecutor(SetupConfigurationManager.State, progress);
                    var success = await executor.ExecuteInstallationAsync();

                    e.Result = success;
                }
                catch (Exception ex)
                {
                    SetupConfigurationManager.Log(SetupLogLevel.Critical, $"Installation failed: {ex.Message}", exception: ex);
                    e.Result = false;
                }
            };

            installWorker.ProgressChanged += (s, e) =>
            {
                var progress = e.UserState as InstallProgress;
                progressBar.Value = e.ProgressPercentage;
                statusLabel.Text = progress?.StatusText ?? "";
                detailLabel.Text = progress?.DetailText ?? "";
                
                if (!string.IsNullOrEmpty(progress?.LogMessage))
                {
                    logTextBox.AppendText(progress.LogMessage + Environment.NewLine);
                    logTextBox.ScrollToCaret();
                }
            };

            installWorker.RunWorkerCompleted += (s, e) =>
            {
                var success = e.Result as bool? ?? false;
                InstallationComplete = true;
                statusLabel.ForeColor = success ? SetupDesign.SuccessColor : SetupDesign.ErrorColor;
                
                if (success)
                {
                    statusLabel.Text = "Installation completed successfully!";
                    logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ✓ Installation completed successfully!" + Environment.NewLine);
                }
                else
                {
                    statusLabel.Text = "Installation failed!";
                    logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] ✗ Installation failed. Check the log for details." + Environment.NewLine);
                }
                
                logTextBox.ScrollToCaret();
            };

            installWorker.RunWorkerAsync();
        }

        private class InstallProgress
        {
            public string StatusText { get; set; } = "";
            public string DetailText { get; set; } = "";
            public string LogMessage { get; set; } = "";
        }
    }
}