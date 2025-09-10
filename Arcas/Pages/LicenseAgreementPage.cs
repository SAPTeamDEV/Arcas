using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Arcas
{
    public class LicenseAgreementPage : SetupPage
    {
        private RadioButton acceptRadio;
        private RadioButton declineRadio;

        public override string Title => SetupConfigurationManager.Definition.License?.Title ?? "License Agreement";
        public override string Subtitle => "Please review the license terms before installing " + SetupConfigurationManager.Definition.Application.Name;
        public override bool CanGoNext => acceptRadio?.Checked ?? false;

        public override Control CreateContent()
        {
            var panel = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor
            };

            var licenseInfo = SetupConfigurationManager.Definition.License;
            if (licenseInfo == null)
            {
                // Should not happen if page is shown, but handle gracefully
                var errorLabel = SetupDesign.CreateBodyLabel("License information not available.");
                panel.Controls.Add(errorLabel);
                return panel;
            }

            // Instruction label
            var instructionLabel = SetupDesign.CreateBodyLabel(
                "Please read the following license agreement. You must accept the terms of this agreement before continuing with the installation.");
            instructionLabel.Dock = DockStyle.Top;
            instructionLabel.Height = 40;
            instructionLabel.TextAlign = ContentAlignment.TopLeft;
            instructionLabel.Padding = new Padding(0, 0, 0, 10);

            // License text box with proper styling
            var licenseTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Text = GetLicenseText(),
                BackColor = SetupDesign.BackgroundColor,
                ForeColor = SetupDesign.TextPrimary,
                BorderStyle = BorderStyle.Fixed3D
            };

            // Bottom panel for radio buttons
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = SetupDesign.SurfaceColor,
                Padding = SetupDesign.StandardPadding
            };

            // Accept radio button
            acceptRadio = new RadioButton
            {
                Text = licenseInfo.AcceptText,
                Font = SetupDesign.BodyFont,
                ForeColor = SetupDesign.SuccessColor,
                UseVisualStyleBackColor = true,
                AutoSize = true,
                Location = new Point(0, 5)
            };

            // Decline radio button
            declineRadio = new RadioButton
            {
                Text = licenseInfo.DeclineText,
                Font = SetupDesign.BodyFont,
                ForeColor = SetupDesign.ErrorColor,
                Checked = true,
                UseVisualStyleBackColor = true,
                AutoSize = true,
                Location = new Point(0, 30)
            };

            // Event handlers
            acceptRadio.CheckedChanged += OnRadioButtonChanged;
            declineRadio.CheckedChanged += OnRadioButtonChanged;

            bottomPanel.Controls.Add(declineRadio);
            bottomPanel.Controls.Add(acceptRadio);

            panel.Controls.Add(licenseTextBox);
            panel.Controls.Add(bottomPanel);
            panel.Controls.Add(instructionLabel);

            return panel;
        }

        private void OnRadioButtonChanged(object sender, EventArgs e)
        {
            // This will trigger the wizard to update the Next button state
        }

        public override bool ValidatePage()
        {
            if (!acceptRadio.Checked)
            {
                MessageBox.Show("You must accept the license agreement to continue with the installation.", 
                    "License Agreement Required", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private string GetLicenseText()
        {
            var licenseInfo = SetupConfigurationManager.Definition.License;
            if (licenseInfo == null) return "";

            // First try to load from file if specified
            if (!string.IsNullOrEmpty(licenseInfo.TextFilePath))
            {
                try
                {
                    var filePath = SetupConfigurationManager.ExpandVariables(licenseInfo.TextFilePath);
                    if (File.Exists(filePath))
                    {
                        return File.ReadAllText(filePath);
                    }
                }
                catch (Exception ex)
                {
                    SetupConfigurationManager.Log(SetupLogLevel.Warning, $"Failed to load license file: {ex.Message}");
                }
            }

            // Fall back to embedded text
            return licenseInfo.Text;
        }
    }
}