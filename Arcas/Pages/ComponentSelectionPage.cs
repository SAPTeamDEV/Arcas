using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Arcas
{
    public class ComponentSelectionPage : SetupPage
    {
        private CheckedListBox componentsListBox;
        private Label descriptionLabel;
        private Label spaceLabel;
        private SplitContainer splitContainer;
        private const int MIN_PANEL_WIDTH = 200;
        private const int MAX_PANEL_WIDTH = 400;

        public override string Title => "Select Components";
        public override string Subtitle => "Choose which features of Arcas you want to install";

        public string[] SelectedComponents => 
            componentsListBox?.CheckedItems.Cast<ComponentItem>().Select(c => c.Name).ToArray() ?? new string[0];

        public override Control CreateContent()
        {
            var panel = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor
            };

            // Instruction label
            var instructionLabel = SetupDesign.CreateBodyLabel(
                "Double-click components to select/deselect them. Single-click to view descriptions.");
            instructionLabel.Dock = DockStyle.Top;
            instructionLabel.Height = 40;
            instructionLabel.TextAlign = ContentAlignment.TopLeft;
            instructionLabel.Padding = new Padding(0, 0, 0, 15);

            // Main content using split container
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 250,
                FixedPanel = FixedPanel.Panel1,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SetupDesign.BorderColor
            };

            // Components panel
            var componentsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor,
                Padding = new Padding(1)
            };

            componentsListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                Font = SetupDesign.BodyFont,
                CheckOnClick = false, // Disable click to check - we'll use double-click
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                BackColor = SetupDesign.BackgroundColor,
                ForeColor = SetupDesign.TextPrimary
            };

            componentsPanel.Controls.Add(componentsListBox);
            splitContainer.Panel1.Controls.Add(componentsPanel);

            // Description panel
            var descriptionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SetupDesign.BackgroundColor,
                Padding = SetupDesign.CompactPadding
            };

            // Space label
            spaceLabel = SetupDesign.CreateHeadingLabel("Space required: 50 MB");
            spaceLabel.Dock = DockStyle.Top;
            spaceLabel.Height = 30;
            spaceLabel.ForeColor = SetupDesign.PrimaryColor;
            spaceLabel.TextAlign = ContentAlignment.MiddleLeft;

            // Description area
            var descContainer = SetupDesign.CreateCard();
            descContainer.Dock = DockStyle.Fill;
            descContainer.Margin = new Padding(0, 5, 0, 0);

            descriptionLabel = SetupDesign.CreateBodyLabel("Select a component to see its description.");
            descriptionLabel.Dock = DockStyle.Fill;
            descriptionLabel.AutoSize = false;
            descriptionLabel.TextAlign = ContentAlignment.TopLeft;
            descriptionLabel.Font = SetupDesign.BodyFont;
            descriptionLabel.ForeColor = SetupDesign.TextMuted;

            descContainer.Controls.Add(descriptionLabel);

            descriptionPanel.Controls.Add(descContainer);
            descriptionPanel.Controls.Add(spaceLabel);

            splitContainer.Panel2.Controls.Add(descriptionPanel);

            // Load components from configuration
            var availableComponents = SetupConfigurationManager.GetAvailableComponents();
            var componentItems = new List<ComponentItem>();

            foreach (var component in availableComponents)
            {
                var componentItem = new ComponentItem(
                    component.Name,
                    component.Description,
                    component.DefaultSelected,
                    component.Required,
                    component.Id // Pass the ID for new system
                );
                componentItems.Add(componentItem);
            }

            foreach (var component in componentItems)
            {
                var index = componentsListBox.Items.Add(component);
                componentsListBox.SetItemChecked(index, component.IsChecked);
                
                // Make required items non-toggleable
                if (component.IsRequired)
                {
                    componentsListBox.SetItemCheckState(index, CheckState.Checked);
                }
            }

            // Auto-resize panel based on longest component name
            ResizePanelToFitContent();

            // Event handlers
            componentsListBox.ItemCheck += ComponentsListBox_ItemCheck;
            componentsListBox.SelectedIndexChanged += ComponentsListBox_SelectedIndexChanged;
            componentsListBox.MouseDoubleClick += ComponentsListBox_MouseDoubleClick;

            // Add all components to main panel
            panel.Controls.Add(splitContainer);
            panel.Controls.Add(instructionLabel);

            // Initial description and space calculation
            UpdateSpaceCalculation();

            return panel;
        }

        private void ResizePanelToFitContent()
        {
            if (componentsListBox?.Items.Count == 0) return;

            using (var g = componentsListBox.CreateGraphics())
            {
                var maxWidth = 0;
                foreach (ComponentItem item in componentsListBox.Items)
                {
                    var textSize = g.MeasureString(item.Name, componentsListBox.Font);
                    var width = (int)textSize.Width + 50; // Add space for checkbox and padding
                    maxWidth = Math.Max(maxWidth, width);
                }

                // Apply constraints
                maxWidth = Math.Max(MIN_PANEL_WIDTH, Math.Min(MAX_PANEL_WIDTH, maxWidth));
                
                if (splitContainer != null)
                {
                    splitContainer.SplitterDistance = maxWidth;
                }
            }
        }

        private void ComponentsListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var index = componentsListBox.IndexFromPoint(e.Location);
            if (index >= 0 && index < componentsListBox.Items.Count)
            {
                var item = componentsListBox.Items[index] as ComponentItem;
                if (item?.IsRequired != true) // Don't allow toggling required items
                {
                    var currentState = componentsListBox.GetItemChecked(index);
                    componentsListBox.SetItemChecked(index, !currentState);
                    UpdateSpaceCalculation();
                }
            }
        }

        private void ComponentsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var item = componentsListBox.Items[e.Index] as ComponentItem;
            if (item?.IsRequired == true && e.NewValue == CheckState.Unchecked)
            {
                MessageBox.Show($"The '{item.Name}' component is required and cannot be deselected.", 
                    "Required Component", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.NewValue = CheckState.Checked;
            }
        }

        private void ComponentsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (componentsListBox.SelectedItem is ComponentItem item)
            {
                descriptionLabel.Text = $"{item.Name}\n\n{item.Description}";
                descriptionLabel.Font = SetupDesign.BodyFont;
                descriptionLabel.ForeColor = SetupDesign.TextPrimary;
            }
            else
            {
                descriptionLabel.Text = "Select a component to see its description.";
                descriptionLabel.Font = new Font(SetupDesign.BodyFont, FontStyle.Italic);
                descriptionLabel.ForeColor = SetupDesign.TextMuted;
            }
        }

        private void UpdateSpaceCalculation()
        {
            var selectedCount = componentsListBox?.CheckedItems.Count ?? 0;
            
            // Calculate actual space from configuration
            long totalBytes = 0;
            var availableComponents = SetupConfigurationManager.GetAvailableComponents();
            
            if (componentsListBox != null)
            {
                for (int i = 0; i < componentsListBox.Items.Count; i++)
                {
                    if (componentsListBox.GetItemChecked(i) && componentsListBox.Items[i] is ComponentItem item)
                    {
                        var component = availableComponents.FirstOrDefault(c => c.Name == item.Name);
                        if (component != null)
                        {
                            totalBytes += component.SizeBytes;
                        }
                    }
                }
            }
            
            var totalMB = totalBytes / (1024 * 1024);
            if (totalMB == 0) totalMB = 25; // Minimum space
            
            if (spaceLabel != null)
            {
                spaceLabel.Text = $"Space required: {totalMB} MB ({selectedCount} components selected)";
                
                if (selectedCount == 0)
                {
                    spaceLabel.ForeColor = SetupDesign.ErrorColor;
                }
                else
                {
                    spaceLabel.ForeColor = SetupDesign.PrimaryColor;
                }
            }
        }

        public override bool ValidatePage()
        {
            if (componentsListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show("You must select at least one component to install.", "Component Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
    }

    public class ComponentItem
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsRequired { get; }
        public bool IsChecked { get; }
        public string Id { get; } // New: Component ID for new system

        public ComponentItem(string name, string description, bool isChecked, bool isRequired = false, string id = "")
        {
            Name = name;
            Description = description;
            IsChecked = isChecked;
            IsRequired = isRequired;
            Id = id;
        }

        public override string ToString() => Name;
    }
}