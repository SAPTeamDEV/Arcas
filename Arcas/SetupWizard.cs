using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Arcas
{
    public partial class SetupWizard : Form
    {
        private Panel headerPanel;
        private Panel contentPanel;
        private Panel buttonPanel;
        private Button backButton;
        private Button nextButton;
        private Button cancelButton;
        private Label titleLabel;
        private Label subtitleLabel;
        private PictureBox headerIcon;

        private List<SetupPage> pages;
        private int currentPageIndex;
        private SetupConfiguration configuration;
        private System.Windows.Forms.Timer progressCheckTimer;
        private System.Windows.Forms.Timer buttonUpdateTimer;

        public SetupWizard()
        {
            InitializeComponent();
            
            // Initialize the new configuration system
            try
            {
                // This will load configuration and initialize state
                var definition = SetupConfigurationManager.Definition;
                var state = SetupConfigurationManager.State;
                
                SetupConfigurationManager.Log(SetupLogLevel.Info, $"Loaded setup configuration for {definition.Application.Name} v{definition.Application.Version}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load setup configuration: {ex.Message}", "Configuration Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            InitializePages();
            InitializeTimers();
            ShowPage(0);
        }

        private void InitializePages()
        {
            var enabledPages = SetupConfigurationManager.GetEnabledPages();
            pages = new List<SetupPage>();

            foreach (var pageDefinition in enabledPages)
            {
                SetupPage? page = pageDefinition.PageType.ToLowerInvariant() switch
                {
                    "welcome" => new WelcomePage(),
                    "license" => SetupConfigurationManager.Definition.License != null ? new LicenseAgreementPage() : null,
                    "directory" => new InstallationDirectoryPage(),
                    "components" => new ComponentSelectionPage(),
                    "progress" => new InstallationProgressPage(),
                    "completion" => new CompletionPage(),
                    _ => null
                };

                if (page != null)
                {
                    pages.Add(page);
                }
            }

            // If no pages defined or license is null, use default flow
            if (pages.Count == 0)
            {
                pages = new List<SetupPage>
                {
                    new WelcomePage(),
                    new InstallationDirectoryPage(),
                    new ComponentSelectionPage(),
                    new InstallationProgressPage(),
                    new CompletionPage()
                };

                // Add license page only if license is defined
                if (SetupConfigurationManager.Definition.License != null)
                {
                    pages.Insert(1, new LicenseAgreementPage());
                }
            }
        }

        private void InitializeTimers()
        {
            progressCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = 500,
                Enabled = false
            };
            progressCheckTimer.Tick += ProgressCheckTimer_Tick;

            // Timer to periodically update button states (for dynamic pages like license agreement)
            buttonUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 200,
                Enabled = true
            };
            buttonUpdateTimer.Tick += ButtonUpdateTimer_Tick;
        }

        private void ProgressCheckTimer_Tick(object sender, EventArgs e)
        {
            if (pages[currentPageIndex] is InstallationProgressPage progressPage && progressPage.InstallationComplete)
            {
                progressCheckTimer.Enabled = false;
                nextButton.Enabled = true;
                nextButton.Text = "Next >";
                cancelButton.Enabled = true;
            }
        }

        private void ButtonUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Continuously update button states for pages that have dynamic CanGoNext properties
            UpdateButtonStates();
        }

        private void ShowPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= pages.Count)
                return;

            currentPageIndex = pageIndex;
            var page = pages[pageIndex];

            // Clear current content
            contentPanel.Controls.Clear();

            // Set header information
            titleLabel.Text = page.Title;
            subtitleLabel.Text = page.Subtitle;

            // Add page content
            var pageControl = page.CreateContent();
            pageControl.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(pageControl);

            // Special handling for installation progress page
            if (page is InstallationProgressPage)
            {
                progressCheckTimer.Enabled = true;
                buttonUpdateTimer.Enabled = false; // Don't need continuous updates during installation
            }
            else
            {
                progressCheckTimer.Enabled = false;
                buttonUpdateTimer.Enabled = true; // Enable for interactive pages
            }

            // Update button states
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var page = pages[currentPageIndex];

            backButton.Enabled = currentPageIndex > 0 && page.CanGoBack;
            nextButton.Enabled = page.CanGoNext;
            
            if (currentPageIndex == pages.Count - 1)
            {
                nextButton.Text = "Finish";
            }
            else
            {
                nextButton.Text = "Next >";
            }

            // Special handling for installation page
            if (pages[currentPageIndex] is InstallationProgressPage)
            {
                backButton.Enabled = false;
                nextButton.Enabled = false;
                cancelButton.Enabled = false;
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            if (currentPageIndex > 0)
            {
                ShowPage(currentPageIndex - 1);
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            var page = pages[currentPageIndex];
            
            if (!page.ValidatePage())
            {
                return; // Validation method will show appropriate error message
            }

            // Save configuration before proceeding
            SaveCurrentPageConfiguration();

            if (currentPageIndex == pages.Count - 1)
            {
                // Finish button clicked
                SaveFinalConfiguration();
                
                // Check if user wants to launch the application
                if (pages[currentPageIndex] is CompletionPage completionPage && completionPage.ShouldLaunchApplication)
                {
                    // This could launch the main application
                    // For now, we'll just close with OK
                }
                
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowPage(currentPageIndex + 1);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to cancel the setup?", "Setup", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void SaveCurrentPageConfiguration()
        {
            var state = SetupConfigurationManager.State;
            
            switch (pages[currentPageIndex])
            {
                case LicenseAgreementPage:
                    state.LicenseAccepted = true; // If we get past validation, it's accepted
                    SetupConfigurationManager.Log(SetupLogLevel.Info, "License accepted by user");
                    break;
                    
                case InstallationDirectoryPage dirPage:
                    state.InstallationPath = dirPage.InstallationPath;
                    SetupConfigurationManager.Log(SetupLogLevel.Info, $"Installation path set to: {state.InstallationPath}");
                    break;
                    
                case ComponentSelectionPage componentPage:
                    state.SelectedComponentIds.Clear();
                    foreach (var componentName in componentPage.SelectedComponents)
                    {
                        // Find component ID by name (for backward compatibility)
                        var component = SetupConfigurationManager.GetAvailableComponents()
                            .FirstOrDefault(c => c.Name == componentName);
                        if (component != null)
                        {
                            state.SelectedComponentIds.Add(component.Id);
                        }
                    }
                    SetupConfigurationManager.Log(SetupLogLevel.Info, $"Selected components: {string.Join(", ", state.SelectedComponentIds)}");
                    break;
            }
        }

        private void SaveFinalConfiguration()
        {
            var state = SetupConfigurationManager.State;
            state.InstallationCompleted = true;
            SetupConfigurationManager.Log(SetupLogLevel.Info, "Setup wizard completed successfully");
            // Note: State is not persisted to disk as it's runtime only
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                progressCheckTimer?.Dispose();
                buttonUpdateTimer?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}