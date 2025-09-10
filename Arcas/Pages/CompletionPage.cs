using System.Drawing;
using System.Windows.Forms;

namespace Arcas
{
    public class CompletionPage : SetupPage
    {
        private CheckBox launchCheckBox;

        public override string Title => "Completing the " + SetupConfigurationManager.Definition.Application.Name + " Setup Wizard";
        public override string Subtitle => "Setup has finished installing " + SetupConfigurationManager.Definition.Application.Name + " on your computer";
        public override bool CanGoBack => false;

        public bool ShouldLaunchApplication => launchCheckBox?.Checked ?? false;

        public override Control CreateContent()
        {
            var panel = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor
            };

            // Create main content container
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 20, 0, 0),
                BackColor = SetupDesign.BackgroundColor
            };

            // Success message
            var appName = SetupConfigurationManager.Definition.Application.Name;
            var successLabel = SetupDesign.CreateTitleLabel($"Setup has successfully installed {appName} on your computer.");
            successLabel.Dock = DockStyle.Top;
            successLabel.Height = 40;
            successLabel.ForeColor = SetupDesign.SuccessColor;
            successLabel.TextAlign = ContentAlignment.MiddleLeft;

            // Instructions
            var instructionLabel = SetupDesign.CreateBodyLabel("Click Finish to exit Setup.");
            instructionLabel.Dock = DockStyle.Top;
            instructionLabel.Height = 30;
            instructionLabel.TextAlign = ContentAlignment.TopLeft;
            instructionLabel.Padding = new Padding(0, 10, 0, 0);

            // Options panel
            var optionsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = SetupDesign.BackgroundColor,
                Padding = new Padding(0, 20, 0, 0)
            };

            launchCheckBox = new CheckBox
            {
                Text = $"Launch {appName} now",
                Font = SetupDesign.BodyFont,
                ForeColor = SetupDesign.TextPrimary,
                Checked = true,
                AutoSize = true,
                UseVisualStyleBackColor = true
            };

            optionsPanel.Controls.Add(launchCheckBox);

            // Installation summary panel
            var summaryPanel = SetupDesign.CreateCard();
            summaryPanel.Dock = DockStyle.Fill;
            summaryPanel.Margin = new Padding(0, 20, 0, 0);

            var summaryTitle = SetupDesign.CreateHeadingLabel("Installation Summary");
            summaryTitle.Dock = DockStyle.Top;
            summaryTitle.Height = 30;

            var summaryContent = SetupDesign.CreateBodyLabel(
                "✓ Application installed successfully\n" +
                "✓ Components configured\n" +
                "✓ Shortcuts created\n" +
                "✓ Ready to use");
            summaryContent.Dock = DockStyle.Fill;
            summaryContent.AutoSize = false;
            summaryContent.ForeColor = SetupDesign.TextSecondary;
            summaryContent.Padding = new Padding(0, 5, 0, 0);

            summaryPanel.Controls.Add(summaryContent);
            summaryPanel.Controls.Add(summaryTitle);

            contentPanel.Controls.Add(summaryPanel);
            contentPanel.Controls.Add(optionsPanel);
            contentPanel.Controls.Add(instructionLabel);
            contentPanel.Controls.Add(successLabel);

            panel.Controls.Add(contentPanel);

            return panel;
        }
    }
}