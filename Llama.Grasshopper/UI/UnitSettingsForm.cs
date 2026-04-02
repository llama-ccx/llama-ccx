using System;
using Eto.Forms;
using Eto.Drawing;
using Llama.Core.Units;

namespace Llama.UI
{
    public class UnitSettingsForm : Form
    {
        private DropDown lengthDropDown;
        private DropDown forceDropDown;
        private DropDown massDropDown;
        private DropDown temperatureDropDown;

        private Label stressLabel;
        private Label densityLabel;
        private Label momentLabel;
        private Label springLabel;
        private Label accelLabel;

        public UnitSettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Unit Settings";
            Maximizable = false;
            Minimizable = false;
            Resizable = false;
            Topmost = true;
            Padding = new Padding(16);
            BackgroundColor = Colors.White;

            Content = CreateMainLayout();

            Shown += (s, e) =>
            {
                var screen = Screen.FromPoint(PointFromScreen(new PointF(0, 0))) ?? Screen.PrimaryScreen;
                var bounds = screen.WorkingArea;
                Location = new Point(
                    (int)(bounds.Center.X - Width / 2),
                    (int)(bounds.Center.Y - Height / 2));
            };
        }

        private Control CreateMainLayout()
        {
            // ── Left panel: dropdowns + buttons ──
            var leftPanel = new TableLayout { Spacing = new Size(8, 8) };

            leftPanel.Rows.Add(CreateDropdownRow("Length", out lengthDropDown, typeof(LengthUnit), (int)UnitSystem.Length));
            leftPanel.Rows.Add(CreateDropdownRow("Force", out forceDropDown, typeof(ForceUnit), (int)UnitSystem.Force));
            leftPanel.Rows.Add(CreateDropdownRow("Mass", out massDropDown, typeof(MassUnit), (int)UnitSystem.Mass));
            leftPanel.Rows.Add(CreateDropdownRow("Temperature", out temperatureDropDown, typeof(TemperatureUnit), (int)UnitSystem.Temperature));

            // Buttons directly under dropdowns
            leftPanel.Rows.Add(CreateButtonRow());

            // ── Right panel: derived units ──
            var rightPanel = new TableLayout { Spacing = new Size(8, 6) };

            rightPanel.Rows.Add(new TableRow(new Label
            {
                Text = "Derived Units",
                Font = SystemFonts.Bold(10),
                TextColor = Colors.Black
            }));

            rightPanel.Rows.Add(CreateDerivedRow("Stress", out stressLabel));
            rightPanel.Rows.Add(CreateDerivedRow("Density", out densityLabel));
            rightPanel.Rows.Add(CreateDerivedRow("Moment", out momentLabel));
            rightPanel.Rows.Add(CreateDerivedRow("Stiffness", out springLabel));
            rightPanel.Rows.Add(CreateDerivedRow("Acceleration", out accelLabel));

            UpdatePreview();

            // ── Combine left and right ──
            var main = new TableLayout { Spacing = new Size(20, 0) };
            var row = new TableRow();
            row.Cells.Add(new TableCell(leftPanel));
            row.Cells.Add(new TableCell(rightPanel));
            main.Rows.Add(row);

            return main;
        }

        private TableRow CreateDropdownRow(string label, out DropDown dropdown, Type enumType, int selectedIndex)
        {
            dropdown = new DropDown();
            foreach (var name in Enum.GetNames(enumType))
                dropdown.Items.Add(name);
            dropdown.SelectedIndex = selectedIndex;
            dropdown.SelectedIndexChanged += (s, e) => UpdatePreview();

            var row = new TableRow();
            row.Cells.Add(new TableCell(new Label
            {
                Text = label,
                Font = SystemFonts.Bold(10),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 90
            }));
            row.Cells.Add(new TableCell(dropdown, true));
            return row;
        }

        private TableRow CreateDerivedRow(string label, out Label valueLabel)
        {
            valueLabel = new Label
            {
                Font = new Font(SystemFont.Default, 10),
                TextColor = Colors.DimGray,
                VerticalAlignment = VerticalAlignment.Center
            };

            var row = new TableRow();
            row.Cells.Add(new TableCell(new Label
            {
                Text = label,
                Font = new Font(SystemFont.Default, 10),
                TextColor = Colors.DimGray,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 90
            }));
            row.Cells.Add(new TableCell(valueLabel, true));
            return row;
        }

        private void UpdatePreview()
        {
            var l = ((LengthUnit)lengthDropDown.SelectedIndex).ToString();
            var f = ((ForceUnit)forceDropDown.SelectedIndex).ToString();
            var m = ((MassUnit)massDropDown.SelectedIndex).ToString();

            stressLabel.Text = $"{f}/{l}\u00B2";
            densityLabel.Text = $"{m}/{l}\u00B3";
            momentLabel.Text = $"{f}\u00B7{l}";
            springLabel.Text = $"{f}/{l}";
            accelLabel.Text = $"{l}/s\u00B2";
        }

        private TableRow CreateButtonRow()
        {
            var saveBtn = new Button
            {
                Text = "Save",
                MinimumSize = new Size(80, 28)
            };
            saveBtn.Click += OnSaveClicked;

            var cancelBtn = new Button
            {
                Text = "Cancel",
                MinimumSize = new Size(80, 28)
            };
            cancelBtn.Click += (s, e) => Close();

            var btnLayout = new TableLayout { Spacing = new Size(8, 0) };
            var row = new TableRow();
            row.Cells.Add(new TableCell(saveBtn, true));
            row.Cells.Add(new TableCell(cancelBtn, true));
            btnLayout.Rows.Add(row);

            return new TableRow(btnLayout);
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            var length = (LengthUnit)lengthDropDown.SelectedIndex;
            var force = (ForceUnit)forceDropDown.SelectedIndex;
            var mass = (MassUnit)massDropDown.SelectedIndex;
            var temperature = (TemperatureUnit)temperatureDropDown.SelectedIndex;

            UnitSystem.Apply(length, force, mass, temperature);
            UnitSettings.Save();
            Close();
        }

        public static void ShowForm()
        {
            UnitSettings.Load();
            var form = new UnitSettingsForm();
            form.Show();
        }
    }
}
