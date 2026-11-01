using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using IFCAlignmentManager.Core;
using IFCAlignmentManager.Services;

namespace IFCAlignmentManager.UI
{
    public class DiagnosticsViewForm : System.Windows.Forms.Form
    {
        private readonly CoordinateSnapshot _snapshot;

        public DiagnosticsViewForm(CoordinateSnapshot snapshot = null)
        {
            _snapshot = snapshot;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"IFC Alignment — Diagnostics  [v{AppVersion.Version}]";
            this.Size = new Size(720, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Icon = AppIcons.ToIcon(AppIcons.CreateDiagnosticsIcon(32));

            var picIcon = new PictureBox
            {
                Image = AppIcons.CreateDiagnosticsIcon(24),
                Location = new System.Drawing.Point(20, 18),
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            var lblHeader = new System.Windows.Forms.Label
            {
                Text = $"Model: {_snapshot?.DocumentTitle ?? "Active Model"}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new System.Drawing.Point(52, 18),
                AutoSize = true
            };

            var lblPath = new System.Windows.Forms.Label
            {
                Text = $"Path: {_snapshot?.DocumentPath ?? "—"}",
                Location = new System.Drawing.Point(20, 50),
                Size = new Size(660, 20),
                ForeColor = System.Drawing.Color.Gray
            };

            var grid = new DataGridView
            {
                Location = new System.Drawing.Point(20, 80),
                Size = new Size(660, 330),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = System.Drawing.Color.White
            };

            grid.Columns.Add("Property", "Coordinate Property");
            grid.Columns.Add("Value", "Internal Feet / Radians");
            grid.Columns.Add("Notes", "Description");

            if (_snapshot != null)
            {
                grid.Rows.Add("Project Base Point X", _snapshot.PBP_X.ToString("F4"), "Internal East/West");
                grid.Rows.Add("Project Base Point Y", _snapshot.PBP_Y.ToString("F4"), "Internal North/South");
                grid.Rows.Add("Project Base Point Z", _snapshot.PBP_Z.ToString("F4"), "Internal Elevation");
                grid.Rows.Add("Survey Point X", _snapshot.SP_X.ToString("F4"), "Shared East/West");
                grid.Rows.Add("Survey Point Y", _snapshot.SP_Y.ToString("F4"), "Shared North/South");
                grid.Rows.Add("Survey Point Z", _snapshot.SP_Z.ToString("F4"), "Shared Elevation");
                grid.Rows.Add("Shared Origin EW", _snapshot.SharedOrigin_EastWest.ToString("F4"), "ProjectPosition.EastWest");
                grid.Rows.Add("Shared Origin NS", _snapshot.SharedOrigin_NorthSouth.ToString("F4"), "ProjectPosition.NorthSouth");
                grid.Rows.Add("Shared Origin Elev", _snapshot.SharedOrigin_Elevation.ToString("F4"), "ProjectPosition.Elevation");
                double deg = _snapshot.TrueNorthAngle_Radians * (180.0 / Math.PI);
                grid.Rows.Add("True North Angle", $"{_snapshot.TrueNorthAngle_Radians:F5} rad ({deg:F2}°)", "ProjectPosition.Angle");
                grid.Rows.Add("Project Length Unit", _snapshot.ProjectLengthUnit, "Document Length Spec");
                grid.Rows.Add("Bounding Box Min", $"({_snapshot.BBox_MinX:F2}, {_snapshot.BBox_MinY:F2}, {_snapshot.BBox_MinZ:F2})", "Min extent");
                grid.Rows.Add("Bounding Box Max", $"({_snapshot.BBox_MaxX:F2}, {_snapshot.BBox_MaxY:F2}, {_snapshot.BBox_MaxZ:F2})", "Max extent");
            }

            var btnClose = new System.Windows.Forms.Button
            {
                Text = " Close",
                Image = AppIcons.CreateCheckIcon(16, System.Drawing.Color.FromArgb(70, 70, 80)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(570, 425),
                Size = new Size(110, 32)
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new System.Windows.Forms.Control[] { picIcon, lblHeader, lblPath, grid, btnClose });
        }
    }
}
