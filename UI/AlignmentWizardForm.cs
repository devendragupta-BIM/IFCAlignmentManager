using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IFCAlignmentManager.Core;
using IFCAlignmentManager.Services;

namespace IFCAlignmentManager.UI
{
    public class AlignmentWizardForm : System.Windows.Forms.Form
    {
        private readonly UIApplication _uiApp;
        private readonly CoordinateDiagnosticsService _diagService = new CoordinateDiagnosticsService();
        private readonly TransformCalculatorService _calcService = new TransformCalculatorService();
        private readonly AlignmentExecutionService _execService = new AlignmentExecutionService();
        private readonly ProfileStorageService _profileStorage = new ProfileStorageService();
        private readonly HtmlReportService _reportService = new HtmlReportService();
        private readonly ValidationService _valService = new ValidationService();

        // State
        private Document _hostDoc;
        private Document _consultantDoc;
        private CoordinateSnapshot _hostSnapshot;
        private CoordinateSnapshot _consultantSnapshot;
        private DiagnosticsResult _diagnosticsResult;
        private AlignmentTransformResult _proposedTransform;
        private TransformResult _appliedResult;
        private AlignmentRestorePoint _restorePoint;

        private XYZ[] _manualHostPoints = new XYZ[2];
        private XYZ[] _manualConsultantPoints = new XYZ[2];

        // UI Controls
        private TabControl tabControl;
        private TabPage tabStep1;
        private TabPage tabStep2;
        private TabPage tabStep3;
        private TabPage tabStep4;
        private TabPage tabStep5;

        private System.Windows.Forms.ComboBox cmbHost;
        private System.Windows.Forms.ComboBox cmbConsultant;

        private DataGridView gridDiagnostics;
        private System.Windows.Forms.Label lblSeverity;
        private ListBox listWarnings;

        private RadioButton rbPBP;
        private RadioButton rbSP;
        private RadioButton rbOrigin;
        private RadioButton rbManual;
        private System.Windows.Forms.Panel panelManualPoints;
        private System.Windows.Forms.Button btnPickH1, btnPickH2, btnPickC1, btnPickC2;
        private System.Windows.Forms.Label lblManualStatus;
        private System.Windows.Forms.Label lblTransformPreview;

        private System.Windows.Forms.Label lblConfirmSummary;
        private CheckBox chkSaveProfile;
        private System.Windows.Forms.TextBox txtProfileName;

        private System.Windows.Forms.Label lblResultStatus;
        private System.Windows.Forms.Label lblResidualError;
        private System.Windows.Forms.Button btnOpenReport;

        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnCancel;

        public AlignmentWizardForm(UIApplication uiApp = null)
        {
            _uiApp = uiApp;
            InitializeComponent();
            PopulateDocuments();
            UpdateNavigation();
        }

        private void InitializeComponent()
        {
            this.Text = "IFC Alignment Manager — Revit 2026";
            this.Size = new Size(880, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = AppIcons.ToIcon(AppIcons.CreateWizardIcon(32));

            // Bottom Navigation Panel
            var panelBottom = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
            };

            btnCancel = new System.Windows.Forms.Button
            {
                Text = " Cancel",
                Image = AppIcons.CreateDeleteIcon(14),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Size = new Size(95, 34),
                Location = new System.Drawing.Point(20, 13)
            };
            btnCancel.Click += (s, e) => this.Close();

            btnBack = new System.Windows.Forms.Button
            {
                Text = " Back",
                Image = AppIcons.CreateBackIcon(14, System.Drawing.Color.FromArgb(40, 40, 50)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Size = new Size(95, 34),
                Location = new System.Drawing.Point(645, 13)
            };
            btnBack.Click += BtnBack_Click;

            btnNext = new System.Windows.Forms.Button
            {
                Text = "Next ",
                Image = AppIcons.CreateNextIcon(14, System.Drawing.Color.White),
                TextImageRelation = TextImageRelation.TextBeforeImage,
                Size = new Size(105, 34),
                Location = new System.Drawing.Point(750, 13),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White
            };
            btnNext.Click += BtnNext_Click;

            panelBottom.Controls.Add(btnCancel);
            panelBottom.Controls.Add(btnBack);
            panelBottom.Controls.Add(btnNext);
            this.Controls.Add(panelBottom);

            // Wizard Tabs
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Buttons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };

            BuildStep1();
            BuildStep2();
            BuildStep3();
            BuildStep4();
            BuildStep5();

            this.Controls.Add(tabControl);
        }

        private void BuildStep1()
        {
            tabStep1 = new TabPage("Step 1: Select Models");
            var lblTitle = new System.Windows.Forms.Label { Text = "Step 1: Select Host and Consultant Models", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new System.Drawing.Point(30, 25), AutoSize = true };

            var lblDesc = new System.Windows.Forms.Label
            {
                Text = "Choose the Host model (the master reference coordinate system) and the Consultant IFC model to align.",
                Location = new System.Drawing.Point(30, 60),
                Size = new Size(800, 30)
            };

            var lblHost = new System.Windows.Forms.Label { Text = "Host Document (Reference Coordinates):", Location = new System.Drawing.Point(30, 110), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            cmbHost = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(30, 135), Size = new Size(600, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblConsultant = new System.Windows.Forms.Label { Text = "Consultant Document (To Be Aligned):", Location = new System.Drawing.Point(30, 185), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            cmbConsultant = new System.Windows.Forms.ComboBox { Location = new System.Drawing.Point(30, 210), Size = new Size(600, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var btnRefresh = new System.Windows.Forms.Button
            {
                Text = " Refresh Open Documents",
                Image = AppIcons.CreateDiagnosticsIcon(16),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(640, 134),
                Size = new Size(200, 32)
            };
            btnRefresh.Click += (s, e) => PopulateDocuments();

            tabStep1.Controls.AddRange(new System.Windows.Forms.Control[] { lblTitle, lblDesc, lblHost, cmbHost, lblConsultant, cmbConsultant, btnRefresh });
            tabControl.TabPages.Add(tabStep1);
        }

        private void BuildStep2()
        {
            tabStep2 = new TabPage("Step 2: Diagnostics View");
            var lblTitle = new System.Windows.Forms.Label { Text = "Step 2: Coordinate Diagnostics & Differences", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new System.Drawing.Point(30, 20), AutoSize = true };

            lblSeverity = new System.Windows.Forms.Label { Text = "Severity: Evaluating...", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new System.Drawing.Point(30, 50), AutoSize = true };

            gridDiagnostics = new DataGridView
            {
                Location = new System.Drawing.Point(30, 80),
                Size = new Size(800, 220),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = System.Drawing.Color.White
            };
            gridDiagnostics.Columns.Add("Metric", "Coordinate Metric (ft)");
            gridDiagnostics.Columns.Add("Host", "Host");
            gridDiagnostics.Columns.Add("Consultant", "Consultant");
            gridDiagnostics.Columns.Add("Delta", "Delta (Consultant - Host)");

            var lblWarnHeader = new System.Windows.Forms.Label { Text = "Warnings and Findings:", Location = new System.Drawing.Point(30, 310), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            listWarnings = new ListBox { Location = new System.Drawing.Point(30, 335), Size = new Size(800, 110) };

            var btnLoadProfile = new System.Windows.Forms.Button
            {
                Text = " Apply Saved Profile Instead...",
                Image = AppIcons.CreateProfileIcon(16),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(30, 458),
                Size = new Size(230, 34)
            };
            btnLoadProfile.Click += BtnLoadProfile_Click;

            tabStep2.Controls.AddRange(new System.Windows.Forms.Control[] { lblTitle, lblSeverity, gridDiagnostics, lblWarnHeader, listWarnings, btnLoadProfile });
            tabControl.TabPages.Add(tabStep2);
        }

        private void BuildStep3()
        {
            tabStep3 = new TabPage("Step 3: Alignment Method");
            var lblTitle = new System.Windows.Forms.Label { Text = "Step 3: Select Alignment Strategy", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new System.Drawing.Point(30, 20), AutoSize = true };

            rbPBP = new RadioButton { Text = "Align by Project Base Point (Matches PBP translation and True North)", Location = new System.Drawing.Point(40, 60), Size = new Size(700, 26), Checked = true };
            rbSP = new RadioButton { Text = "Align by Survey Point (Matches Survey Point coordinates and orientation)", Location = new System.Drawing.Point(40, 95), Size = new Size(700, 26) };
            rbOrigin = new RadioButton { Text = "Align by Internal Origin / Shared Origin", Location = new System.Drawing.Point(40, 130), Size = new Size(700, 26) };
            rbManual = new RadioButton { Text = "Align by 2 Corresponding Point Pairs (Manual pick from model)", Location = new System.Drawing.Point(40, 165), Size = new Size(700, 26) };

            rbPBP.CheckedChanged += AlignmentMethod_Changed;
            rbSP.CheckedChanged += AlignmentMethod_Changed;
            rbOrigin.CheckedChanged += AlignmentMethod_Changed;
            rbManual.CheckedChanged += AlignmentMethod_Changed;

            panelManualPoints = new System.Windows.Forms.Panel { Location = new System.Drawing.Point(40, 200), Size = new Size(800, 100), Visible = false };
            btnPickH1 = new System.Windows.Forms.Button
            {
                Text = " Pick Host Pt 1",
                Image = AppIcons.CreateTargetIcon(14, System.Drawing.Color.FromArgb(0, 120, 215)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(10, 10),
                Size = new Size(140, 30)
            };
            btnPickH2 = new System.Windows.Forms.Button
            {
                Text = " Pick Host Pt 2",
                Image = AppIcons.CreateTargetIcon(14, System.Drawing.Color.FromArgb(0, 120, 215)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(160, 10),
                Size = new Size(140, 30)
            };
            btnPickC1 = new System.Windows.Forms.Button
            {
                Text = " Pick Consultant Pt 1",
                Image = AppIcons.CreateTargetIcon(14, System.Drawing.Color.FromArgb(16, 185, 129)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(310, 10),
                Size = new Size(165, 30)
            };
            btnPickC2 = new System.Windows.Forms.Button
            {
                Text = " Pick Consultant Pt 2",
                Image = AppIcons.CreateTargetIcon(14, System.Drawing.Color.FromArgb(16, 185, 129)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(485, 10),
                Size = new Size(165, 30)
            };

            btnPickH1.Click += (s, e) => PickModelPoint(ref _manualHostPoints[0], btnPickH1, "Host Pt 1");
            btnPickH2.Click += (s, e) => PickModelPoint(ref _manualHostPoints[1], btnPickH2, "Host Pt 2");
            btnPickC1.Click += (s, e) => PickModelPoint(ref _manualConsultantPoints[0], btnPickC1, "Consultant Pt 1");
            btnPickC2.Click += (s, e) => PickModelPoint(ref _manualConsultantPoints[1], btnPickC2, "Consultant Pt 2");

            lblManualStatus = new System.Windows.Forms.Label { Text = "Points: Please pick two pairs of corresponding points.", Location = new System.Drawing.Point(10, 50), Size = new Size(700, 25) };
            panelManualPoints.Controls.AddRange(new System.Windows.Forms.Control[] { btnPickH1, btnPickH2, btnPickC1, btnPickC2, lblManualStatus });

            var lblPreviewHeader = new System.Windows.Forms.Label { Text = "Calculated Transformation Preview:", Location = new System.Drawing.Point(40, 310), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            lblTransformPreview = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(40, 335),
                Size = new Size(780, 130),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                Font = new Font("Consolas", 10)
            };

            tabStep3.Controls.AddRange(new System.Windows.Forms.Control[] { lblTitle, rbPBP, rbSP, rbOrigin, rbManual, panelManualPoints, lblPreviewHeader, lblTransformPreview });
            tabControl.TabPages.Add(tabStep3);
        }

        private void BuildStep4()
        {
            tabStep4 = new TabPage("Step 4: Confirm & Apply");
            var lblTitle = new System.Windows.Forms.Label { Text = "Step 4: Review and Apply Alignment", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new System.Drawing.Point(30, 20), AutoSize = true };

            lblConfirmSummary = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(30, 60),
                Size = new Size(800, 200),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                Font = new Font("Segoe UI", 9)
            };

            chkSaveProfile = new CheckBox
            {
                Text = "Save configuration as reusable Consultant Alignment Profile",
                Location = new System.Drawing.Point(30, 280),
                Size = new Size(500, 25),
                Checked = true
            };

            var lblProf = new System.Windows.Forms.Label { Text = "Consultant / Project Name:", Location = new System.Drawing.Point(50, 310), AutoSize = true };
            txtProfileName = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(220, 307), Size = new Size(350, 25), Text = "Consultant_Alignment" };

            var btnApply = new System.Windows.Forms.Button
            {
                Text = "  Apply Shared Coordinate Alignment Now",
                Image = AppIcons.CreateApplyIcon(20),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(30, 365),
                Size = new Size(340, 44),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnApply.Click += BtnApply_Click;

            tabStep4.Controls.AddRange(new System.Windows.Forms.Control[] { lblTitle, lblConfirmSummary, chkSaveProfile, lblProf, txtProfileName, btnApply });
            tabControl.TabPages.Add(tabStep4);
        }

        private void BuildStep5()
        {
            tabStep5 = new TabPage("Step 5: Result & Reports");
            var lblTitle = new System.Windows.Forms.Label { Text = "Step 5: Execution Result", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new System.Drawing.Point(30, 25), AutoSize = true };

            lblResultStatus = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(30, 70),
                Size = new Size(800, 60),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            lblResidualError = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(30, 140),
                Size = new Size(800, 40),
                Font = new Font("Segoe UI", 10)
            };

            btnOpenReport = new System.Windows.Forms.Button
            {
                Text = "  Open Interactive HTML Report",
                Image = AppIcons.CreateReportIcon(18),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(30, 210),
                Size = new Size(260, 38),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnOpenReport.Click += BtnOpenReport_Click;

            var btnUndo = new System.Windows.Forms.Button
            {
                Text = "  Undo Alignment",
                Image = AppIcons.CreateUndoIcon(18),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Size = new Size(180, 38),
                Location = new System.Drawing.Point(30, 260),
                BackColor = System.Drawing.Color.FromArgb(200, 60, 60),
                ForeColor = System.Drawing.Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Enabled = false   // enabled dynamically after alignment
            };
            btnUndo.Name = "btnUndo";
            btnUndo.Click += (s, e) =>
            {
                if (_restorePoint == null || !_restorePoint.IsValid)
                {
                    MessageBox.Show(
                        "No restore point available. The alignment cannot be undone automatically.",
                        "Undo Unavailable",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"This will restore the consultant model's coordinate position to the state captured at:\n\n" +
                    $"  {_restorePoint.CapturedAt}\n\n" +
                    $"This will undo the alignment that was just applied. Continue?",
                    "Confirm Undo Alignment",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    bool restored = _execService.RestoreFromRestorePoint(
                        _consultantDoc, _restorePoint.FilePath, out string undoErr);

                    if (restored)
                    {
                        lblResultStatus.Text = "↩ Alignment undone. Coordinates restored to pre-alignment state.";
                        lblResultStatus.ForeColor = System.Drawing.Color.DarkOrange;
                        btnUndo.Enabled = false;   // prevent double-undo
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Undo failed: {undoErr}",
                            "Undo Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var btnDone = new System.Windows.Forms.Button
            {
                Text = "  Close Wizard",
                Image = AppIcons.CreateCheckIcon(16, System.Drawing.Color.FromArgb(70, 70, 80)),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Location = new System.Drawing.Point(310, 210),
                Size = new Size(140, 38),
                Font = new Font("Segoe UI", 9.5f)
            };
            btnDone.Click += (s, e) => this.Close();

            tabStep5.Controls.AddRange(new System.Windows.Forms.Control[] { lblTitle, lblResultStatus, lblResidualError, btnOpenReport, btnUndo, btnDone });
            tabControl.TabPages.Add(tabStep5);
        }

        private void PopulateDocuments()
        {
            cmbHost.Items.Clear();
            cmbConsultant.Items.Clear();

            if (_uiApp?.Application?.Documents != null)
            {
                foreach (Document d in _uiApp.Application.Documents)
                {
                    if (d.IsFamilyDocument) continue;
                    var item = new DocItem(d);
                    cmbHost.Items.Add(item);
                    cmbConsultant.Items.Add(item);
                }
            }

            if (cmbHost.Items.Count > 0)
            {
                cmbHost.SelectedIndex = 0;
                if (cmbConsultant.Items.Count > 1)
                    cmbConsultant.SelectedIndex = 1;
                else
                    cmbConsultant.SelectedIndex = 0;
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            int nextIndex = tabControl.SelectedIndex + 1;
            if (tabControl.SelectedIndex == 0)
            {
                // Validate selection
                var hostItem = cmbHost.SelectedItem as DocItem;
                var consultantItem = cmbConsultant.SelectedItem as DocItem;
                if (hostItem == null || consultantItem == null)
                {
                    MessageBox.Show("Please select both a Host and a Consultant document.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _hostDoc = hostItem.Doc;
                _consultantDoc = consultantItem.Doc;

                var preErrors = _valService.ValidatePreAlignment(_hostDoc, _consultantDoc);
                if (preErrors.Count > 0)
                {
                    MessageBox.Show(string.Join("\n• ", preErrors), "Validation Pre-check Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Run diagnostics
                RunDiagnostics();
            }
            else if (tabControl.SelectedIndex == 1)
            {
                CalculateCurrentTransform();
            }
            else if (tabControl.SelectedIndex == 2)
            {
                PrepareConfirmation();
            }

            if (nextIndex < tabControl.TabCount)
            {
                tabControl.SelectedIndex = nextIndex;
                UpdateNavigation();
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex > 0)
            {
                tabControl.SelectedIndex--;
                UpdateNavigation();
            }
        }

        private void UpdateNavigation()
        {
            btnBack.Enabled = (tabControl.SelectedIndex > 0 && tabControl.SelectedIndex < 4);
            btnNext.Visible = (tabControl.SelectedIndex < 3);
            btnNext.Enabled = true;
        }

        private void RunDiagnostics()
        {
            _hostSnapshot = _diagService.ReadSnapshot(_hostDoc);
            _consultantSnapshot = _diagService.ReadSnapshot(_consultantDoc);
            _diagnosticsResult = _diagService.Compare(_hostSnapshot, _consultantSnapshot);

            gridDiagnostics.Rows.Clear();
            listWarnings.Items.Clear();

            void AddRow(string name, double h, double c, double delta)
            {
                gridDiagnostics.Rows.Add(name, h.ToString("F4"), c.ToString("F4"), delta.ToString("F4"));
            }

            AddRow("Project Base Point X", _hostSnapshot.PBP_X, _consultantSnapshot.PBP_X, _diagnosticsResult.Delta_PBP_X);
            AddRow("Project Base Point Y", _hostSnapshot.PBP_Y, _consultantSnapshot.PBP_Y, _diagnosticsResult.Delta_PBP_Y);
            AddRow("Project Base Point Z", _hostSnapshot.PBP_Z, _consultantSnapshot.PBP_Z, _diagnosticsResult.Delta_PBP_Z);
            AddRow("Shared Origin East-West", _hostSnapshot.SharedOrigin_EastWest, _consultantSnapshot.SharedOrigin_EastWest, _diagnosticsResult.Delta_SharedOrigin_EW);
            AddRow("Shared Origin North-South", _hostSnapshot.SharedOrigin_NorthSouth, _consultantSnapshot.SharedOrigin_NorthSouth, _diagnosticsResult.Delta_SharedOrigin_NS);
            AddRow("True North (radians)", _hostSnapshot.TrueNorthAngle_Radians, _consultantSnapshot.TrueNorthAngle_Radians, _diagnosticsResult.Delta_TrueNorth_Radians);

            lblSeverity.Text = $"Overall Severity Status: {_diagnosticsResult.Severity.ToUpperInvariant()}";
            lblSeverity.ForeColor = _diagnosticsResult.Severity == "Critical" ? System.Drawing.Color.Red :
                                   (_diagnosticsResult.Severity == "Major" ? System.Drawing.Color.DarkOrange :
                                   (_diagnosticsResult.Severity == "Minor" ? System.Drawing.Color.Goldenrod : System.Drawing.Color.Green));

            foreach (var w in _diagnosticsResult.Warnings)
            {
                listWarnings.Items.Add(w);
            }

            txtProfileName.Text = $"{_consultantDoc.Title}_Alignment";
        }

        private void AlignmentMethod_Changed(object sender, EventArgs e)
        {
            panelManualPoints.Visible = rbManual.Checked;
            CalculateCurrentTransform();
        }

        private void CalculateCurrentTransform()
        {
            if (_hostSnapshot == null || _consultantSnapshot == null) return;

            try
            {
                if (rbPBP.Checked)
                {
                    _proposedTransform = _calcService.CalculateFromProjectBasePoints(_hostSnapshot, _consultantSnapshot);
                }
                else if (rbSP.Checked)
                {
                    _proposedTransform = _calcService.CalculateFromSurveyPoints(_hostSnapshot, _consultantSnapshot);
                }
                else if (rbOrigin.Checked)
                {
                    _proposedTransform = _calcService.CalculateFromInternalOrigin(_hostSnapshot, _consultantSnapshot);
                }
                else if (rbManual.Checked)
                {
                    if (_manualHostPoints[0] != null && _manualHostPoints[1] != null &&
                        _manualConsultantPoints[0] != null && _manualConsultantPoints[1] != null)
                    {
                        _proposedTransform = _calcService.CalculateFromCorrespondingPoints(_manualHostPoints, _manualConsultantPoints);
                    }
                    else
                    {
                        lblTransformPreview.Text = "Please pick all 4 points to compute the transformation.";
                        return;
                    }
                }

                if (_proposedTransform != null)
                {
                    double tx_m = _proposedTransform.Translation_X * 0.3048;
                    double ty_m = _proposedTransform.Translation_Y * 0.3048;
                    double tz_m = _proposedTransform.Translation_Z * 0.3048;
                    double rot_deg = _proposedTransform.Rotation_Radians * (180.0 / Math.PI);

                    lblTransformPreview.Text =
                        $"Method: {_proposedTransform.Method}\r\n\r\n" +
                        $"Translation X:  {_proposedTransform.Translation_X:F4} ft  ({tx_m:F4} m)\r\n" +
                        $"Translation Y:  {_proposedTransform.Translation_Y:F4} ft  ({ty_m:F4} m)\r\n" +
                        $"Translation Z:  {_proposedTransform.Translation_Z:F4} ft  ({tz_m:F4} m)\r\n" +
                        $"Rotation:       {_proposedTransform.Rotation_Radians:F5} rad  ({rot_deg:F3}°)\r\n\r\n" +
                        $"Est. Residual:  {_proposedTransform.EstimatedResidualError_Feet:F5} ft" +
                        ((_proposedTransform.EstimatedResidualError_Feet > 0.01)
                            ? "  ⚠ Exceeds 0.01 ft tolerance" : "  ✔ Within tolerance");
                }
            }
            catch (Exception ex)
            {
                lblTransformPreview.Text = $"Error computing transform: {ex.Message}";
            }
        }

        private void PickModelPoint(ref XYZ pointVar, System.Windows.Forms.Button btn, string label)
        {
            if (_uiApp?.ActiveUIDocument == null)
            {
                MessageBox.Show("Active Revit UI Document is not available to pick points.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.Hide();
                var pt = _uiApp.ActiveUIDocument.Selection.PickPoint($"Select {label} in Revit View");
                pointVar = pt;
                btn.Text = $"{label} (Picked)";
                btn.BackColor = System.Drawing.Color.LightGreen;
            }
            catch
            {
                // User cancelled pick
            }
            finally
            {
                this.Show();
                CalculateCurrentTransform();
            }
        }

        private void PrepareConfirmation()
        {
            if (_proposedTransform == null) CalculateCurrentTransform();

            double deg = _proposedTransform != null ? _proposedTransform.Rotation_Radians * (180.0 / Math.PI) : 0;
            lblConfirmSummary.Text =
                $"Host Document: {_hostDoc?.Title}\n" +
                $"Consultant Document: {_consultantDoc?.Title}\n\n" +
                $"Alignment Method: {_proposedTransform?.Method}\n" +
                $"Translation Vector (feet): X={_proposedTransform?.Translation_X:F4}, Y={_proposedTransform?.Translation_Y:F4}, Z={_proposedTransform?.Translation_Z:F4}\n" +
                $"Rotation Angle: {_proposedTransform?.Rotation_Radians:F5} rad ({deg:F2}°)\n\n" +
                $"Action: Adjusts Consultant's ProjectPosition (Shared Coordinates) to align models seamlessly.";
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Guard: verify consultant document is still open and valid
            if (_consultantDoc == null)
            {
                MessageBox.Show("Consultant document is no longer available. Please restart the wizard.",
                    "Document Closed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                // Accessing Title throws if the document has been closed
                var _ = _consultantDoc.Title;
            }
            catch
            {
                MessageBox.Show("The consultant document was closed after the wizard was opened. " +
                    "Please reopen the document and start the wizard again.",
                    "Document Closed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_proposedTransform == null) return;

            // Capture restore point BEFORE applying so the user can undo.
            string restoreErr;
            string restoreFilePath = _execService.SaveRestorePoint(_consultantDoc, out restoreErr);
            _restorePoint = restoreFilePath != null
                ? new AlignmentRestorePoint
                  {
                      FilePath      = restoreFilePath,
                      CapturedAt    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                      DocumentTitle = _consultantDoc.Title
                  }
                : null;

            string error;
            bool success = _execService.ApplySharedCoordinateAlignment(_consultantDoc, _proposedTransform, out error);

            _appliedResult = new TransformResult
            {
                DocumentTitle = _consultantDoc.Title,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Success = success,
                ErrorMessage = error,
                Method = _proposedTransform.Method,
                Translation_X = _proposedTransform.Translation_X,
                Translation_Y = _proposedTransform.Translation_Y,
                Translation_Z = _proposedTransform.Translation_Z,
                Rotation_Radians = _proposedTransform.Rotation_Radians
            };

            // Post-alignment verification
            var postReport = _valService.ValidatePostAlignment(_consultantDoc, _consultantSnapshot, _proposedTransform, 0.05);
            _appliedResult.ResidualError_Feet = postReport.ResidualError_Feet;

            // Optionally save profile
            if (chkSaveProfile.Checked && success)
            {
                var profile = new ConsultantAlignmentProfile
                {
                    ConsultantName = txtProfileName.Text.Trim(),
                    OriginalModelFileName = _consultantDoc.Title,
                    RevitVersion = "2026",
                    AlignmentMethod = _proposedTransform.Method,
                    Translation_X = _proposedTransform.Translation_X,
                    Translation_Y = _proposedTransform.Translation_Y,
                    Translation_Z = _proposedTransform.Translation_Z,
                    Rotation_Radians = _proposedTransform.Rotation_Radians,
                    RotationPivot_X = _proposedTransform.RotationPivot_X,
                    RotationPivot_Y = _proposedTransform.RotationPivot_Y,
                    RotationPivot_Z = _proposedTransform.RotationPivot_Z,
                    BaselineSnapshot = _consultantSnapshot,
                    ValidationTolerance_Feet = 0.05,
                    Notes = $"Profile created automatically on {DateTime.Now:g}"
                };

                _profileStorage.SaveProfile(profile, out string profileError);
            }

            // Move to Step 5
            lblResultStatus.Text = success ? "✔ Alignment Successfully Applied to Consultant Model!" : $"✖ Alignment Failed: {error}";
            lblResultStatus.ForeColor = success ? System.Drawing.Color.Green : System.Drawing.Color.Red;
            lblResidualError.Text = $"Post-Alignment Residual Coordinate Deviation: {postReport.ResidualError_Feet:F5} ft";

            // Enable Undo button only if restore point was captured AND alignment succeeded.
            var undoBtn = tabStep5.Controls["btnUndo"] as System.Windows.Forms.Button;
            if (undoBtn != null)
                undoBtn.Enabled = success && _restorePoint != null && _restorePoint.IsValid;

            tabControl.SelectedIndex = 4;
            UpdateNavigation();
        }

        private void BtnOpenReport_Click(object sender, EventArgs e)
        {
            string html = _reportService.GenerateReport(_diagnosticsResult, _proposedTransform, _appliedResult, _proposedTransform?.Warnings);
            _reportService.WriteAndOpen(html, null, out _);
        }

        private void BtnLoadProfile_Click(object sender, EventArgs e)
        {
            var mgr = new ProfileManagerForm();
            if (mgr.ShowDialog() == DialogResult.OK && mgr.SelectedProfile != null)
            {
                var p = mgr.SelectedProfile;
                _proposedTransform = new AlignmentTransformResult
                {
                    Method = p.AlignmentMethod,
                    Translation_X = p.Translation_X,
                    Translation_Y = p.Translation_Y,
                    Translation_Z = p.Translation_Z,
                    Rotation_Radians = p.Rotation_Radians,
                    RotationPivot_X = p.RotationPivot_X,
                    RotationPivot_Y = p.RotationPivot_Y,
                    RotationPivot_Z = p.RotationPivot_Z,
                    EstimatedResidualError_Feet = 0
                };

                tabControl.SelectedIndex = 2; // Jump to Method / Preview
                CalculateCurrentTransform();
                UpdateNavigation();
            }
        }

        private class DocItem
        {
            public Document Doc { get; }
            public DocItem(Document doc) { Doc = doc; }
            public override string ToString() => Doc.Title ?? "Untitled";
        }
    }
}
