using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Eto.Forms;
using Eto.Drawing;

namespace Llama.UI
{
    public class LicenseManagementForm : Form
    {
        private ListBox licenseListBox;
        private Label statusLabel;
        private Button addLicenseButton;
        private Button removeLicenseButton;
        private Button buyLicenseButton;

        private string licenseFilePath;
        private List<Llama.Core.License.User> currentLicenses;

        public LicenseManagementForm()
        {
            InitializeComponent();
            LoadLicenses();
        }

        private void InitializeComponent()
        {
            Title = "Llama - License Management";
            Maximizable = false;
            Minimizable = true;
            Resizable = true;
            Topmost = true;
            Padding = new Padding(20);
            BackgroundColor = Colors.White;

            AutoSize = true;
            MinimumSize = new Size(480, 400);

            // Initialize license file path
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string llamaFolder = Path.GetDirectoryName(assemblyLocation);
            licenseFilePath = Path.Combine(llamaFolder, "data.bin");

            // Create main layout
            Content = CreateMainLayout();

            // Center the form on screen
            Shown += (sender, e) =>
            {
                var screen = Screen.FromPoint(PointFromScreen(new PointF(0, 0))) ?? Screen.PrimaryScreen;
                var screenBounds = screen.WorkingArea;
                Location = new Point(
                    (int)(screenBounds.Center.X - Width / 2),
                    (int)(screenBounds.Center.Y - Height / 2));
            };
        }

        private Control CreateMainLayout()
        {
            var layout = new TableLayout
            {
                Spacing = new Size(10, 10)
            };

            layout.Rows.Add(CreateHeaderSection());

            var listRow = new TableRow();
            listRow.Cells.Add(CreateLicenseListSection());
            layout.Rows.Add(listRow);

            layout.Rows.Add(CreateStatusSection());
            layout.Rows.Add(CreateButtonSection());

            return layout;
        }

        private Control CreateHeaderSection()
        {
            var headerLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            var titleLabel = new Label
            {
                Text = "License Management",
                Font = SystemFonts.Bold(16),
                TextAlignment = TextAlignment.Center,
                TextColor = Colors.DarkBlue
            };
            headerLayout.Items.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text = "Manage your Llama licenses",
                Font = new Font(SystemFont.Default, 10),
                TextAlignment = TextAlignment.Center,
                TextColor = Colors.Gray
            };
            headerLayout.Items.Add(subtitleLabel);

            return headerLayout;
        }

        private Control CreateLicenseListSection()
        {
            var listLayout = new TableLayout
            {
                Spacing = new Size(10, 10)
            };

            var labelRow = new TableRow();
            var listLabel = new Label
            {
                Text = "Current Licenses:",
                Font = SystemFonts.Bold(12)
            };
            labelRow.Cells.Add(listLabel);
            listLayout.Rows.Add(labelRow);

            var listRow = new TableRow();
            licenseListBox = new ListBox
            {
                Size = new Size(400, 150),
                BackgroundColor = Colors.LightGrey,
            };
            licenseListBox.SelectedIndexChanged += OnLicenseSelectionChanged;
            var listCell = new TableCell(licenseListBox);
            listCell.ScaleWidth = true;
            listRow.Cells.Add(listCell);
            listLayout.Rows.Add(listRow);

            return listLayout;
        }

        private Control CreateStatusSection()
        {
            var statusLayout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
            };

            statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font(SystemFont.Default, 10),
                TextColor = Colors.DarkGreen
            };
            statusLayout.Items.Add(statusLabel);

            var pathLabel = new Label
            {
                Text = $"License file: {licenseFilePath}",
                Font = new Font(SystemFont.Default, 8),
                TextColor = Colors.Gray
            };
            statusLayout.Items.Add(pathLabel);

            return statusLayout;
        }

        private Control CreateButtonSection()
        {
            var buttonLayout = new TableLayout
            {
                Spacing = new Size(10, 10)
            };

            var row = new TableRow();

            var addButtonCell = new TableCell(CreateAddLicenseButton());
            addButtonCell.ScaleWidth = true;
            row.Cells.Add(addButtonCell);

            var removeButtonCell = new TableCell(CreateRemoveLicenseButton());
            removeButtonCell.ScaleWidth = true;
            row.Cells.Add(removeButtonCell);

            var buyButtonCell = new TableCell(CreateBuyLicenseButton());
            buyButtonCell.ScaleWidth = true;
            row.Cells.Add(buyButtonCell);

            buttonLayout.Rows.Add(row);

            return buttonLayout;
        }

        private Button CreateAddLicenseButton()
        {
            addLicenseButton = new Button
            {
                Text = "Add License",
                BackgroundColor = Colors.LightGreen,
                TextColor = Colors.Black,
                MinimumSize = new Size(100, 35)
            };

            addLicenseButton.Click += OnAddLicenseClicked;
            return addLicenseButton;
        }

        private Button CreateRemoveLicenseButton()
        {
            removeLicenseButton = new Button
            {
                Text = "Remove License",
                BackgroundColor = Color.FromArgb(240, 128, 128),
                TextColor = Colors.Black,
                MinimumSize = new Size(100, 35)
            };

            removeLicenseButton.Click += OnRemoveLicenseClicked;
            return removeLicenseButton;
        }

        private Button CreateBuyLicenseButton()
        {
            buyLicenseButton = new Button
            {
                Text = "Buy License",
                BackgroundColor = Color.FromArgb(0, 51, 102),
                TextColor = Colors.Black,
                MinimumSize = new Size(100, 35)
            };

            buyLicenseButton.Click += OnBuyLicenseClicked;
            return buyLicenseButton;
        }

        private void LoadLicenses()
        {
            try
            {
                licenseListBox.Items.Clear();
                currentLicenses = new List<Llama.Core.License.User>();

                if (File.Exists(licenseFilePath))
                {
                    currentLicenses = Llama.Core.License.License.DeserializeBinary(licenseFilePath);

                    foreach (var license in currentLicenses)
                    {
                        string displayText = $"{license.user_name} - Expires: {license.expiring_date:yyyy-MM-dd}";
                        if (license.expiring_date < DateTime.Now)
                        {
                            displayText += " (EXPIRED)";
                        }
                        licenseListBox.Items.Add(displayText);
                    }

                    UpdateStatus($"Loaded {currentLicenses.Count} license(s)", Colors.DarkGreen);
                }
                else
                {
                    UpdateStatus("No license file found", Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading licenses: {ex.Message}", Colors.Red);
            }
        }

        private void OnLicenseSelectionChanged(object sender, EventArgs e)
        {
        }

        private void OnAddLicenseClicked(object sender, EventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select License File";
                openFileDialog.Filters.Add(new FileFilter("License files", "*.bin"));
                openFileDialog.Filters.Add(new FileFilter("All files", "*.*"));

                if (openFileDialog.ShowDialog(this) == DialogResult.Ok)
                {
                    string selectedFilePath = openFileDialog.FileName;
                    string targetDirectory = Path.GetDirectoryName(licenseFilePath);

                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(selectedFilePath, licenseFilePath, true);

                    if (Llama.Core.License.License.IsValid)
                    {
                        UpdateStatus("License added successfully!", Colors.DarkGreen);
                        LoadLicenses();
                    }
                    else
                    {
                        UpdateStatus("License file added but appears to be invalid or expired", Colors.Orange);
                        LoadLicenses();
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error adding license: {ex.Message}", Colors.Red);
            }
        }

        private void OnRemoveLicenseClicked(object sender, EventArgs e)
        {
            if (licenseListBox.SelectedIndex < 0)
            {
                MessageBox.Show(
                    this,
                    "Please select a license from the list to remove.",
                    "No License Selected",
                    MessageBoxType.Information);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    this,
                    "Are you sure you want to remove the selected license?",
                    "Confirm License Removal",
                    MessageBoxType.Question);

                if (result == DialogResult.Yes)
                {
                    if (File.Exists(licenseFilePath))
                    {
                        File.Delete(licenseFilePath);
                        UpdateStatus("License removed successfully", Colors.DarkGreen);
                        LoadLicenses();
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error removing license: {ex.Message}", Colors.Red);
            }
        }

        private void OnBuyLicenseClicked(object sender, EventArgs e)
        {
            try
            {
                string licenseUrl = "https://llama-ccx.github.io/references/license/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = licenseUrl,
                    UseShellExecute = true
                });

                UpdateStatus("Opening license page...", Colors.DarkBlue);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error opening license page: {ex.Message}", Colors.Red);
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            statusLabel.Text = message;
            statusLabel.TextColor = color;
        }

        /// <summary>
        /// Static method to create and show the license management form.
        /// </summary>
        public static void ShowForm()
        {
            var form = new LicenseManagementForm();
            form.Show();
        }
    }
}
