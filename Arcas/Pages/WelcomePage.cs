using System.Drawing;
using System.Windows.Forms;

namespace Arcas
{
    public class WelcomePage : SetupPage
    {
        public override string Title => "Welcome to " + SetupConfigurationManager.Definition.Application.Name + " Setup";
        public override string Subtitle => "This wizard will install " + SetupConfigurationManager.Definition.Application.Name + " on your computer";
        public override bool CanGoBack => false;

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

            // Welcome message
            var welcomeLabel = SetupDesign.CreateTitleLabel("Welcome to the " + SetupConfigurationManager.Definition.Application.Name + " Setup Wizard");
            welcomeLabel.Dock = DockStyle.Top;
            welcomeLabel.Height = 40;
            welcomeLabel.TextAlign = ContentAlignment.MiddleLeft;

            // Description with better formatting
            var appInfo = SetupConfigurationManager.Definition.Application;
            var descriptionLabel = SetupDesign.CreateBodyLabel(
                $"This wizard will guide you through the installation of {appInfo.Name}.\n\n" +
                (!string.IsNullOrEmpty(appInfo.Description) ? $"{appInfo.Description}\n\n" : "") +
                "It is recommended that you close all other applications before continuing.\n\n" +
                "Click Next to continue, or Cancel to exit Setup.");
            descriptionLabel.Dock = DockStyle.Top;
            descriptionLabel.Height = 120;
            descriptionLabel.TextAlign = ContentAlignment.TopLeft;
            descriptionLabel.AutoSize = false;

            // Application logo/branding section - removed bordered rectangle
            var brandingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor,
                Padding = new Padding(0, 40, 0, 0)
            };

            // Simple logo without bordered container
            var logoLabel = new Label
            {
                Text = appInfo.Name.ToUpperInvariant(),
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = SetupDesign.PrimaryColor,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Version info
            var versionLabel = SetupDesign.CreateCaptionLabel($"Version {appInfo.Version}");
            versionLabel.ForeColor = SetupDesign.TextMuted;
            versionLabel.TextAlign = ContentAlignment.MiddleCenter;
            versionLabel.AutoSize = true;

            brandingPanel.Controls.Add(versionLabel);
            brandingPanel.Controls.Add(logoLabel);

            contentPanel.Controls.Add(brandingPanel);
            contentPanel.Controls.Add(descriptionLabel);
            contentPanel.Controls.Add(welcomeLabel);

            panel.Controls.Add(contentPanel);

            return panel;
        }
    }
}