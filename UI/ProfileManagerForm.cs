using System;
using System.Drawing;
using System.Windows.Forms;
using IFCAlignmentManager.Core;
using IFCAlignmentManager.Services;

namespace IFCAlignmentManager.UI
{
    public class ProfileManagerForm : System.Windows.Forms.Form
    {
        private readonly ProfileStorageService _storage = new ProfileStorageService();
        private System.Windows.Forms.ListView listProfiles;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Label lblDetails;

        public ConsultantAlignmentProfile SelectedProfile { get; private set; }

        public ProfileManagerForm()
        {
            InitializeComponent();
            LoadProfiles();
        }

        private void InitializeComponent()
        {
            this.Text = "IFC Alignment — Consultant Profiles Manager";
            this.Size = new Size(800, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Icon = AppIcons.ToIcon(AppIcons.CreateProfileIcon(32));

            var picIcon = new PictureBox
            {
                Image = AppIcons.CreateProfileIcon(24),
                Location = new System.Drawing.Point(20, 15),
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            var lblTitle = new System.Windows.Forms.Label
            {
                Text = "Saved Consultant Alignment Profiles",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new System.Drawing.Point(52, 15),
                AutoSize = true
            };

            var imgList = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
            imgList.Images.Add("profile", AppIcons.CreateProfileIcon(16));

            listProfiles = new System.Windows.Forms.ListView
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new Size(745, 230),
                View = System.Windows.Forms.View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                SmallImageList = imgList
            };
            listProfiles.Columns.Add("Consultant / Name", 220);
            listProfiles.Columns.Add("Alignment Method", 140);
            listProfiles.Columns.Add("Created Date", 160);
            listProfiles.Columns.Add("Last Used", 160);

            listProfiles.SelectedIndexChanged += ListProfiles_SelectedIndexChanged;

            lblDetails = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(20, 295),
                Size = new Size(745, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                Font = new Font("Segoe UI", 9)
            };

            btnSelect = new System.Windows.Forms.Button
            {
                Text = " Use Selected Profile",
                Image = AppIcons.CreateCheckIcon(16, Color.White),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(20, 430),
                Size = new Size(160, 34),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                Enabled = false
            };
            btnSelect.Click += (s, e) =>
            {
                if (listProfiles.SelectedItems.Count > 0)
                {
                    SelectedProfile = listProfiles.SelectedItems[0].Tag as ConsultantAlignmentProfile;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            btnDelete = new System.Windows.Forms.Button
            {
                Text = " Delete Profile",
                Image = AppIcons.CreateDeleteIcon(16),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(190, 430),
                Size = new Size(120, 34),
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnExport = new System.Windows.Forms.Button
            {
                Text = " Export All...",
                Location = new System.Drawing.Point(320, 430),
                Size = new Size(110, 34)
            };
            btnExport.Click += BtnExport_Click;

            btnImport = new System.Windows.Forms.Button
            {
                Text = " Import...",
                Location = new System.Drawing.Point(440, 430),
                Size = new Size(100, 34)
            };
            btnImport.Click += BtnImport_Click;

            var btnClose = new System.Windows.Forms.Button
            {
                Text = " Close",
                Image = AppIcons.CreateCheckIcon(16, Color.FromArgb(70, 70, 80)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(655, 430),
                Size = new Size(110, 34)
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new System.Windows.Forms.Control[] { picIcon, lblTitle, listProfiles, lblDetails, btnSelect, btnDelete, btnExport, btnImport, btnClose });
        }

        private void LoadProfiles()
        {
            listProfiles.Items.Clear();
            var profiles = _storage.LoadAllProfiles();

            foreach (var p in profiles)
            {
                var item = new ListViewItem(p.ConsultantName ?? "Unnamed", "profile");
                item.SubItems.Add(p.AlignmentMethod ?? "Unknown");
                item.SubItems.Add(p.CreatedDate ?? "—");
                item.SubItems.Add(p.LastUsedDate ?? "—");
                item.Tag = p;
                listProfiles.Items.Add(item);
            }

            lblDetails.Text = profiles.Count > 0 ? "Select a profile to view details." : "No saved profiles found.";
            btnSelect.Enabled = false;
            btnDelete.Enabled = false;
        }

        private void ListProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listProfiles.SelectedItems.Count > 0)
            {
                var p = listProfiles.SelectedItems[0].Tag as ConsultantAlignmentProfile;
                if (p != null)
                {
                    double deg = p.Rotation_Radians * (180.0 / Math.PI);
                    lblDetails.Text =
                        $"Profile ID: {p.ProfileId}\n" +
                        $"Consultant / Name: {p.ConsultantName}\n" +
                        $"Target Model File: {p.OriginalModelFileName}\n" +
                        $"Alignment Method: {p.AlignmentMethod}\n" +
                        $"Translation: X={p.Translation_X:F4} ft, Y={p.Translation_Y:F4} ft, Z={p.Translation_Z:F4} ft\n" +
                        $"Rotation: {p.Rotation_Radians:F5} rad ({deg:F2}°)\n" +
                        $"Notes: {p.Notes ?? "—"}";

                    btnSelect.Enabled = true;
                    btnDelete.Enabled = true;
                    return;
                }
            }

            lblDetails.Text = string.Empty;
            btnSelect.Enabled = false;
            btnDelete.Enabled = false;
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listProfiles.SelectedItems.Count > 0)
            {
                var p = listProfiles.SelectedItems[0].Tag as ConsultantAlignmentProfile;
                if (p != null)
                {
                    var confirm = MessageBox.Show($"Are you sure you want to delete profile '{p.ConsultantName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirm == DialogResult.Yes)
                    {
                        _storage.DeleteProfile(p.ProfileId, out _);
                        LoadProfiles();
                    }
                }
            }
        }
        private void RefreshProfileList()
        {
            LoadProfiles();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
            {
                Title = "Export Alignment Profiles",
                Filter = "ZIP Archive|*.zip",
                FileName = $"IFCAlignmentProfiles_{DateTime.Now:yyyyMMdd}.zip"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                int count = _storage.ExportProfilesAsZip(dlg.FileName, out string err);
                if (count < 0)
                    MessageBox.Show($"Export failed: {err}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show($"Exported {count} profile(s) to:\n{dlg.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Import Alignment Profiles",
                Filter = "ZIP Archive|*.zip",
                Multiselect = false
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var confirm = MessageBox.Show(
                    "Overwrite existing profiles with the same ID?",
                    "Import Options", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (confirm == DialogResult.Cancel) return;

                bool overwrite = (confirm == DialogResult.Yes);
                var (imported, skipped) = _storage.ImportProfilesFromZip(dlg.FileName, overwrite, out string err);

                if (!string.IsNullOrEmpty(err))
                    MessageBox.Show($"Import error: {err}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show($"Imported: {imported}  |  Skipped: {skipped}", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                RefreshProfileList(); // re-read and repopulate listProfiles
            }
        }
    }
}
