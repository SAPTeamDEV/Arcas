using System.Drawing;
using System.Windows.Forms;

namespace Arcas
{
    public static class SetupDesign
    {
        // Color scheme
        public static readonly Color PrimaryColor = Color.FromArgb(0, 120, 212);        // Windows blue
        public static readonly Color SecondaryColor = Color.FromArgb(32, 32, 32);       // Dark gray
        public static readonly Color AccentColor = Color.FromArgb(16, 110, 190);        // Darker blue
        public static readonly Color SuccessColor = Color.FromArgb(16, 124, 16);        // Green
        public static readonly Color WarningColor = Color.FromArgb(255, 185, 0);        // Orange
        public static readonly Color ErrorColor = Color.FromArgb(196, 43, 28);          // Red
        
        public static readonly Color BackgroundColor = SystemColors.Window;             // White
        public static readonly Color SurfaceColor = Color.FromArgb(250, 250, 250);     // Light gray
        public static readonly Color BorderColor = Color.FromArgb(204, 204, 204);      // Gray border
        
        public static readonly Color TextPrimary = Color.FromArgb(32, 32, 32);         // Dark text
        public static readonly Color TextSecondary = Color.FromArgb(96, 96, 96);       // Gray text
        public static readonly Color TextMuted = Color.FromArgb(128, 128, 128);        // Muted text

        // Typography
        public static readonly Font TitleFont = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font HeadingFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font ButtonFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        public static readonly Font CaptionFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        // Spacing
        public static readonly Padding StandardPadding = new Padding(16);
        public static readonly Padding CompactPadding = new Padding(8);
        public static readonly Padding LargePadding = new Padding(24);

        // Helper methods
        public static Label CreateTitleLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = TitleFont,
                ForeColor = TextPrimary,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 8)
            };
        }

        public static Label CreateHeadingLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = HeadingFont,
                ForeColor = TextPrimary,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 4)
            };
        }

        public static Label CreateBodyLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = BodyFont,
                ForeColor = TextPrimary,
                AutoSize = true
            };
        }

        public static Label CreateCaptionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = CaptionFont,
                ForeColor = TextSecondary,
                AutoSize = true
            };
        }

        public static Panel CreateCard()
        {
            return new Panel
            {
                BackColor = BackgroundColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = StandardPadding
            };
        }

        public static Panel CreateSurface()
        {
            return new Panel
            {
                BackColor = SurfaceColor,
                Padding = CompactPadding
            };
        }

        public static void StyleButton(Button button)
        {
            button.Font = ButtonFont;
            button.UseVisualStyleBackColor = true;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.FlatStyle = FlatStyle.Standard;
        }
    }
}