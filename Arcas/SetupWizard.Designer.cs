namespace Arcas
{
    partial class SetupWizard
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this.headerPanel = new Panel();
            this.contentPanel = new Panel();
            this.buttonPanel = new Panel();
            this.backButton = new Button();
            this.nextButton = new Button();
            this.cancelButton = new Button();
            this.titleLabel = new Label();
            this.subtitleLabel = new Label();
            this.headerIcon = new PictureBox();

            this.SuspendLayout();

            // SetupWizard - Modern design with proper sizing
            this.AutoScaleDimensions = new SizeF(8F, 20F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Arcas Setup";
            this.BackColor = SetupDesign.SurfaceColor;

            // Header Panel
            this.headerPanel.BackColor = SetupDesign.BackgroundColor;
            this.headerPanel.Dock = DockStyle.Top;
            this.headerPanel.Height = 80;
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Controls.Add(this.subtitleLabel);
            this.headerPanel.Controls.Add(this.headerIcon);

            // Title Label
            this.titleLabel.Font = SetupDesign.HeadingFont;
            this.titleLabel.ForeColor = SetupDesign.TextPrimary;
            this.titleLabel.AutoSize = true;
            this.titleLabel.Location = new Point(80, 18);
            this.titleLabel.Text = "Welcome to Setup";

            // Subtitle Label
            this.subtitleLabel.Font = SetupDesign.BodyFont;
            this.subtitleLabel.ForeColor = SetupDesign.TextSecondary;
            this.subtitleLabel.AutoSize = true;
            this.subtitleLabel.Location = new Point(80, 42);
            this.subtitleLabel.Text = "This wizard will guide you through the installation";

            // Header Icon
            this.headerIcon.Location = new Point(20, 16);
            this.headerIcon.Size = new Size(48, 48);
            this.headerIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            this.headerIcon.BackColor = SetupDesign.PrimaryColor;

            // Content Panel
            this.contentPanel.BackColor = SetupDesign.BackgroundColor;
            this.contentPanel.Dock = DockStyle.Fill;
            this.contentPanel.Padding = SetupDesign.LargePadding;

            // Button Panel
            this.buttonPanel.Dock = DockStyle.Bottom;
            this.buttonPanel.Height = 60;
            this.buttonPanel.BackColor = SetupDesign.SurfaceColor;
            this.buttonPanel.Padding = new Padding(16, 10, 16, 10);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Controls.Add(this.nextButton);
            this.buttonPanel.Controls.Add(this.backButton);

            // Back Button - Properly centered text and positioned
            this.backButton.Location = new Point(490, 17);
            this.backButton.Size = new Size(75, 28);
            this.backButton.Text = "< Back";
            this.backButton.TextAlign = ContentAlignment.MiddleCenter;
            this.backButton.UseVisualStyleBackColor = true;
            this.backButton.Click += new EventHandler(this.BackButton_Click);
            SetupDesign.StyleButton(this.backButton);

            // Next Button
            this.nextButton.Location = new Point(570, 17);
            this.nextButton.Size = new Size(75, 28);
            this.nextButton.Text = "Next >";
            this.nextButton.TextAlign = ContentAlignment.MiddleCenter;
            this.nextButton.UseVisualStyleBackColor = true;
            this.nextButton.Click += new EventHandler(this.NextButton_Click);
            SetupDesign.StyleButton(this.nextButton);

            // Cancel Button
            this.cancelButton.Location = new Point(660, 17);
            this.cancelButton.Size = new Size(75, 28);
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextAlign = ContentAlignment.MiddleCenter;
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new EventHandler(this.CancelButton_Click);
            SetupDesign.StyleButton(this.cancelButton);

            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.buttonPanel);

            this.ResumeLayout(false);
        }
    }
}