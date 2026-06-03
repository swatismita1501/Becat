using System;
using System.Drawing;
using System.Windows.Forms;
using EcatDesktop.Common;

namespace EcatDesktop.Forms
{
    public sealed class MainForm : Form
    {
        private readonly AppRuntime _runtime;

        public MainForm(AppRuntime runtime)
        {
            _runtime = runtime;
            Text = "Eagle Catalog / EFM Data Processing";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(860, 520);
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Controls.Add(BuildLayout());
        }

        private Control BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(28);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = new Label();
            header.Text = "EAGLE CATALOG / EFM DATA PROCESSING";
            header.Dock = DockStyle.Fill;
            header.AutoSize = true;
            header.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point);
            header.ForeColor = Color.FromArgb(28, 57, 91);
            header.Padding = new Padding(0, 8, 0, 24);

            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 2;
            panel.Margin = new Padding(0);
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            panel.Controls.Add(CreateMenuButton("Set Up", "Maintain the job queue records before processing.", OpenSetup), 0, 0);
            panel.Controls.Add(CreateMenuButton("Job Queue", "Run, resume, review, and clean up queued jobs.", OpenQueue), 1, 0);
            panel.Controls.Add(CreateMenuButton("Server Archive", "Archive reviewed jobs and maintain archive settings.", OpenArchive), 0, 1);
            panel.Controls.Add(CreateMenuButton("Exit", "Close the application.", CloseApplication), 1, 1);

            var footer = new Label();
            footer.Text = "CSV-backed conversion with no Paradox runtime dependency.";
            footer.Dock = DockStyle.Fill;
            footer.AutoSize = true;
            footer.ForeColor = Color.FromArgb(93, 104, 117);
            footer.Padding = new Padding(0, 18, 0, 0);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(panel, 0, 1);
            root.Controls.Add(footer, 0, 2);

            return root;
        }

        private Control CreateMenuButton(string title, string description, EventHandler onClick)
        {
            var button = new Button();
            button.Dock = DockStyle.Fill;
            button.Height = 160;
            button.BackColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.Margin = new Padding(10);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            button.Padding = new Padding(20);
            button.FlatAppearance.BorderColor = Color.FromArgb(205, 214, 223);
            button.FlatAppearance.BorderSize = 1;
            button.Text = title + Environment.NewLine + Environment.NewLine + description;
            button.Click += onClick;
            return button;
        }

        private void OpenSetup(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Opening setup form.");
                using (var form = new SetupJobForm(_runtime))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to open setup form");
            }
        }

        private void OpenQueue(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Opening job queue form.");
                using (var form = new JobQueueForm(_runtime))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to open job queue form");
            }
        }

        private void OpenArchive(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Opening archive form.");
                using (var form = new ArchiveForm(_runtime))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to open archive form");
            }
        }

        private void CloseApplication(object sender, EventArgs e)
        {
            try
            {
                Log.Information("User requested application close.");
                Close();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to close application");
            }
        }
    }
}
