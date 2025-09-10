using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Arcas
{
    public class InstallationDirectoryPage : SetupPage
    {
        private TextBox installPathTextBox;
        private Button browseButton;
        private Label spaceLabel;

        public override string Title => "Select Installation Folder";
        public override string Subtitle => "Choose the folder in which to install Arcas";

        public string InstallationPath => installPathTextBox?.Text ?? GetDefaultInstallPath();

        public override Control CreateContent()
        {
            var panel = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor
            };

            // Instruction label
            var instructionLabel = SetupDesign.CreateBodyLabel(
                "Setup will install " + SetupConfigurationManager.Definition.Application.Name + 
                " in the following folder. To install in a different folder, click Browse and select another folder.");
            instructionLabel.Dock = DockStyle.Top;
            instructionLabel.Height = 50;
            instructionLabel.TextAlign = ContentAlignment.TopLeft;
            instructionLabel.Padding = new Padding(0, 0, 0, 20);

            // Path selection panel
            var pathPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = SetupDesign.BackgroundColor
            };

            var pathLabel = SetupDesign.CreateBodyLabel("Install " + SetupConfigurationManager.Definition.Application.Name + " in:");
            pathLabel.Dock = DockStyle.Top;
            pathLabel.Height = 25;
            pathLabel.TextAlign = ContentAlignment.BottomLeft;

            var pathInputPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(0, 5, 0, 0),
                BackColor = SetupDesign.BackgroundColor
            };

            installPathTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = SetupDesign.BodyFont,
                Text = SetupConfigurationManager.State.InstallationPath,
                BackColor = SetupDesign.BackgroundColor,
                ForeColor = SetupDesign.TextPrimary,
                BorderStyle = BorderStyle.Fixed3D
            };

            browseButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 100,
                Text = "Browse...",
                UseVisualStyleBackColor = true
            };

            SetupDesign.StyleButton(browseButton);
            browseButton.Click += BrowseButton_Click;
            installPathTextBox.TextChanged += InstallPathTextBox_TextChanged;

            pathInputPanel.Controls.Add(installPathTextBox);
            pathInputPanel.Controls.Add(browseButton);

            pathPanel.Controls.Add(pathInputPanel);
            pathPanel.Controls.Add(pathLabel);

            // Space information
            var settings = SetupConfigurationManager.GetEffectiveSettings();
            var requiredSpaceMB = settings.MinimumDiskSpace / (1024 * 1024);
            
            spaceLabel = SetupDesign.CreateBodyLabel($"Space required: {requiredSpaceMB} MB\nSpace available: Calculating...");
            spaceLabel.Dock = DockStyle.Top;
            spaceLabel.Height = 60;
            spaceLabel.AutoSize = false;
            spaceLabel.Padding = new Padding(0, 20, 0, 0);
            spaceLabel.ForeColor = SetupDesign.PrimaryColor;

            #if DEBUG
            // Add dry run option in debug builds
            var debugPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = SetupDesign.BackgroundColor,
                Padding = new Padding(0, 15, 0, 0)
            };

            var dryRunCheckBox = new CheckBox
            {
                Text = "Dry Run (Debug Mode - Simulate Installation)",
                Font = SetupDesign.BodyFont,
                ForeColor = SetupDesign.ErrorColor,
                AutoSize = true,
                UseVisualStyleBackColor = true,
                Checked = SetupConfigurationManager.State.IsDryRun
            };

            dryRunCheckBox.CheckedChanged += (s, e) =>
            {
                SetupConfigurationManager.State.IsDryRun = dryRunCheckBox.Checked;
                SetupConfigurationManager.Log(SetupLogLevel.Info, $"Dry run mode: {dryRunCheckBox.Checked}");
            };

            debugPanel.Controls.Add(dryRunCheckBox);

            // Add components to main panel
            panel.Controls.Add(debugPanel);
            panel.Controls.Add(spaceLabel);
            panel.Controls.Add(pathPanel);
            panel.Controls.Add(instructionLabel);
            #else
            // Add components to main panel (release build)
            panel.Controls.Add(spaceLabel);
            panel.Controls.Add(pathPanel);
            panel.Controls.Add(instructionLabel);
            #endif

            // Update space information initially
            UpdateSpaceInformation();

            return panel;
        }

        private void InstallPathTextBox_TextChanged(object sender, EventArgs e)
        {
            // Update the state
            SetupConfigurationManager.State.InstallationPath = installPathTextBox.Text;
            UpdateSpaceInformation();
        }

        private void UpdateSpaceInformation()
        {
            try
            {
                var path = InstallationPath;
                if (!string.IsNullOrEmpty(path))
                {
                    var rootPath = Path.GetPathRoot(path);
                    if (!string.IsNullOrEmpty(rootPath))
                    {
                        var drive = new DriveInfo(rootPath);
                        var availableGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        var totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        
                        spaceLabel.Text = $"Space required: 50 MB\n" +
                                        $"Space available: {availableGB:F1} GB of {totalGB:F1} GB";
                        
                        if (availableGB < 0.1) // Less than 100MB
                        {
                            spaceLabel.ForeColor = SetupDesign.ErrorColor;
                            spaceLabel.Text += "\n⚠️ Insufficient space!";
                        }
                        else
                        {
                            spaceLabel.ForeColor = SetupDesign.SuccessColor;
                            spaceLabel.Text += "\n✓ Sufficient space available";
                        }
                    }
                }
            }
            catch (Exception)
            {
                spaceLabel.Text = "Space required: 50 MB\nSpace available: Unable to calculate";
                spaceLabel.ForeColor = SetupDesign.TextMuted;
            }
        }

        public override bool ValidatePage()
        {
            var path = installPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Please specify an installation directory.", "Installation Directory", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                // Validate the path format
                var directory = new DirectoryInfo(path);
                
                if (!directory.Root.Exists)
                {
                    MessageBox.Show("The specified drive does not exist.", "Installation Directory", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Check available space
                var drive = new DriveInfo(directory.Root.FullName);
                if (drive.AvailableFreeSpace < 50 * 1024 * 1024) // 50MB
                {
                    MessageBox.Show("Insufficient disk space. At least 50 MB is required.", "Installation Directory", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                // Test write permissions by creating a temporary file
                var testPath = Path.Combine(directory.Root.FullName, $"arcas_test_{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(testPath, "test");
                    File.Delete(testPath);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("You do not have permission to write to the specified directory or drive.", "Installation Directory", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid installation directory: {ex.Message}", "Installation Directory", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder where you want to install Arcas:",
                SelectedPath = Path.GetDirectoryName(installPathTextBox.Text) ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                installPathTextBox.Text = Path.Combine(dialog.SelectedPath, "Arcas");
            }
        }

        private string GetDefaultInstallPath()
        {
            return SetupConfigurationManager.GetDefaultInstallPath();
        }
    }
}