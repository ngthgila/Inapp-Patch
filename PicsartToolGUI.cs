using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PicsartResetTool
{
    public class DeviceInfo
    {
        public string Serial { get; set; }
        public string GoogleAccount { get; set; }
        public string PicsartAccount { get; set; }
        public string SubStatus { get; set; }
        public string PlanType { get; set; }
        public string Credits { get; set; }
        public string LastResult { get; set; }
    }

    public class MainForm : Form
    {
        private DataGridView grid;
        private RichTextBox logBox;
        private Button btnRefresh;
        private Button btnSelectAll;
        private Button btnKeepFix;
        private Button btnOpenPaywall;
        private Button btnForcePlus;
        private Button btnForceMonthly;
        private Button btnForceUltra;
        private Button btnGrammarlyNoTrial;
        private Button btnChessNoTrial;
        private Button btnFullReset;
        private Label lblStatus;
        private bool allSelected = true;
        private static readonly Random rng = new Random();
        private static readonly object rngLock = new object();

        public MainForm()
        {
            Text = "Picsart, Grammarly & Chess Multi-Device Tool v2.5";
            Size = new Size(1380, 720);
            MinimumSize = new Size(1180, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.FromArgb(245, 247, 250);

            InitializeComponents();
            Shown += async (s, e) => await RefreshDevicesAsync();
        }

        private void InitializeComponents()
        {
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 112,
                Padding = new Padding(10, 8, 10, 8),
                BackColor = Color.White
            };

            // Hàng 1: Các chức năng chính cho Picsart & Quét máy
            btnRefresh = CreateButton("🔄 Quét máy", Color.FromArgb(59, 130, 246), Color.White, 125);
            btnRefresh.Location = new Point(12, 10);
            btnRefresh.Click += async (s, e) => await RefreshDevicesAsync();

            btnSelectAll = CreateButton("☑️ Chọn tất cả", Color.FromArgb(100, 116, 139), Color.White, 130);
            btnSelectAll.Location = new Point(145, 10);
            btnSelectAll.Click += (s, e) => ToggleSelectAll();

            btnKeepFix = CreateButton("⚡ Picsart: Fix Dính Pro + Ép 1 Tháng", Color.FromArgb(16, 185, 129), Color.White, 265);
            btnKeepFix.Location = new Point(283, 10);
            btnKeepFix.Click += async (s, e) => await RunActionOnSelectedAsync("keep");

            btnOpenPaywall = CreateButton("💎 Picsart: Mở thẳng Bảng giá", Color.FromArgb(245, 158, 11), Color.White, 235);
            btnOpenPaywall.Location = new Point(556, 10);
            btnOpenPaywall.Click += async (s, e) => await RunActionOnSelectedAsync("deeplink");

            btnForcePlus = CreateButton("⭐ Picsart: Ép Gói Plus (1 Tháng)", Color.FromArgb(236, 72, 153), Color.White, 245);
            btnForcePlus.Location = new Point(799, 10);
            btnForcePlus.Click += async (s, e) => await RunActionOnSelectedAsync("plus");

            btnForceMonthly = CreateButton("📅 Picsart: Ép Gói Pro (1 Tháng)", Color.FromArgb(139, 92, 246), Color.White, 240);
            btnForceMonthly.Location = new Point(1052, 10);
            btnForceMonthly.Click += async (s, e) => await RunActionOnSelectedAsync("monthly");

            // Hàng 2: Ép Gói Ultra, Grammarly/Chess Tắt Trial & Reset Picsart
            btnForceUltra = CreateButton("🔥 Picsart: Ép Gói Ultra (1 Tháng - 1500 Credit)", Color.FromArgb(234, 88, 12), Color.White, 325);
            btnForceUltra.Location = new Point(12, 58);
            btnForceUltra.Click += async (s, e) => await RunActionOnSelectedAsync("ultra");

            btnGrammarlyNoTrial = CreateButton("🟢 Grammarly: Tắt Trial 7 Ngày (Ép Mua Thẳng Không Lỗi Item)", Color.FromArgb(5, 150, 105), Color.White, 440);
            btnGrammarlyNoTrial.Location = new Point(345, 58);
            btnGrammarlyNoTrial.Click += async (s, e) => await RunActionOnSelectedAsync("grammarly_notrial");

            btnChessNoTrial = CreateButton("♟ Chess: Bỏ Trial, Mua Thẳng", Color.FromArgb(37, 99, 235), Color.White, 245);
            btnChessNoTrial.Location = new Point(1111, 58);
            btnChessNoTrial.Click += async (s, e) => await RunActionOnSelectedAsync("chess_notrial");

            btnFullReset = CreateButton("🧹 Reset Dữ Liệu Picsart (Giữ Nguyên Gmail)", Color.FromArgb(239, 68, 68), Color.White, 310);
            btnFullReset.Location = new Point(793, 58);
            btnFullReset.Click += async (s, e) =>
            {
                var confirm = MessageBox.Show(
                    "Chế độ này sẽ xóa dữ liệu tài khoản Picsart cũ, xóa token thanh toán cũ và cấp sẵn device_id mới (KHÔNG đụng tới android_id hệ thống, giữ nguyên Gmail và giữ nguyên cấu hình Bảng giá).\n\nBạn có muốn tiếp tục?",
                    "Xác nhận Reset Picsart",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (confirm == DialogResult.Yes)
                {
                    await RunActionOnSelectedAsync("full");
                }
            };

            topPanel.Controls.AddRange(new Control[]
            {
                btnRefresh, btnSelectAll, btnKeepFix, btnOpenPaywall, btnForcePlus, btnForceMonthly,
                btnForceUltra, btnGrammarlyNoTrial, btnFullReset, btnChessNoTrial
            });

            Panel bottomStatusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10, 4, 10, 4)
            };
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Text = "Sẵn sàng (v2.5: Picsart + Grammarly + Chess.com mua thẳng không qua trial).",
                TextAlign = ContentAlignment.MiddleLeft
            };
            bottomStatusPanel.Controls.Add(lblStatus);

            Panel logPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 185,
                Padding = new Padding(12, 6, 12, 10),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            Label lblLog = new Label
            {
                Text = "Nhật ký xử lý (Real-time Log):",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };

            logPanel.Controls.Add(logBox);
            logPanel.Controls.Add(lblLog);

            Panel gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 4)
            };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 36 },
                ColumnHeadersHeight = 38,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);

            var colCheck = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "Chọn",
                FillWeight = 22,
                MinimumWidth = 50
            };
            var colSerial = new DataGridViewTextBoxColumn
            {
                Name = "Serial",
                HeaderText = "Serial Thiết Bị",
                ReadOnly = true,
                FillWeight = 55
            };
            var colGoogle = new DataGridViewTextBoxColumn
            {
                Name = "GoogleAccount",
                HeaderText = "Tài khoản Google Play",
                ReadOnly = true,
                FillWeight = 85
            };
            var colPicsart = new DataGridViewTextBoxColumn
            {
                Name = "PicsartAccount",
                HeaderText = "Tài khoản Picsart",
                ReadOnly = true,
                FillWeight = 75
            };
            var colSub = new DataGridViewTextBoxColumn
            {
                Name = "SubStatus",
                HeaderText = "Trạng thái Gói",
                ReadOnly = true,
                FillWeight = 55
            };
            var colPlan = new DataGridViewTextBoxColumn
            {
                Name = "PlanType",
                HeaderText = "Gói hiển thị (Paywall)",
                ReadOnly = true,
                FillWeight = 60
            };
            var colCredits = new DataGridViewTextBoxColumn
            {
                Name = "Credits",
                HeaderText = "Credit | SKU đã mua",
                ReadOnly = true,
                FillWeight = 85
            };
            var colResult = new DataGridViewTextBoxColumn
            {
                Name = "Result",
                HeaderText = "Kết quả xử lý",
                ReadOnly = true,
                FillWeight = 85
            };

            grid.Columns.AddRange(new DataGridViewColumn[] { colCheck, colSerial, colGoogle, colPicsart, colSub, colPlan, colCredits, colResult });
            grid.CellFormatting += Grid_CellFormatting;
            grid.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0)
                {
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            gridPanel.Controls.Add(grid);

            Controls.Add(gridPanel);
            Controls.Add(logPanel);
            Controls.Add(topPanel);
            Controls.Add(bottomStatusPanel);
        }

        private Button CreateButton(string text, Color bg, Color fg, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            string colName = grid.Columns[e.ColumnIndex].Name;
            string val = e.Value.ToString();

            if (colName == "SubStatus")
            {
                if (val.Contains("PRO"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
            }
            else if (colName == "Credits")
            {
                if (val.StartsWith("500") || val.StartsWith("1500"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (val != "-")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
            }
            else if (colName == "PlanType")
            {
                if (val.Contains("Ultra"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(234, 88, 12);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (val.Contains("1 Tuần"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(217, 119, 6);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (val.Contains("Plus"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(219, 39, 119);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
                else if (val.Contains("1 Tháng"))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(37, 99, 235);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                }
            }
        }

        private void ToggleSelectAll()
        {
            allSelected = !allSelected;
            foreach (DataGridViewRow row in grid.Rows)
            {
                row.Cells["Selected"].Value = allSelected;
            }
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), msg);
                return;
            }
            string line = string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, msg, Environment.NewLine);
            logBox.AppendText(line);
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnRefresh.Enabled = enabled;
            btnSelectAll.Enabled = enabled;
            btnKeepFix.Enabled = enabled;
            btnOpenPaywall.Enabled = enabled;
            btnForcePlus.Enabled = enabled;
            btnForceMonthly.Enabled = enabled;
            btnForceUltra.Enabled = enabled;
            btnGrammarlyNoTrial.Enabled = enabled;
            btnChessNoTrial.Enabled = enabled;
            btnFullReset.Enabled = enabled;
        }

        private static string GetRandomHex(int length)
        {
            const string chars = "0123456789abcdef";
            StringBuilder sb = new StringBuilder(length);
            lock (rngLock)
            {
                for (int i = 0; i < length; i++)
                {
                    sb.Append(chars[rng.Next(chars.Length)]);
                }
            }
            return sb.ToString();
        }

        private static string RunAdb(string args, int timeoutMs = 15000)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                using (Process p = Process.Start(psi))
                {
                    // Đọc bất đồng bộ cả stdout và stderr để timeout thực sự có tác dụng
                    // và tránh deadlock khi buffer stderr đầy.
                    Task<string> outTask = p.StandardOutput.ReadToEndAsync();
                    Task<string> errTask = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(timeoutMs))
                    {
                        try { p.Kill(); } catch { }
                        return outTask.Wait(1000) ? (outTask.Result ?? "") : "";
                    }
                    p.WaitForExit();
                    return outTask.Result ?? "";
                }
            }
            catch (Exception ex)
            {
                return "ERR: " + ex.Message;
            }
        }

        private List<string> GetConnectedSerials()
        {
            List<string> list = new List<string>();
            string output = RunAdb("devices");
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                Match m = Regex.Match(line.Trim(), @"^(\S+)\s+device$");
                if (m.Success)
                {
                    list.Add(m.Groups[1].Value);
                }
            }
            return list;
        }

        private DeviceInfo QueryDeviceInfo(string serial)
        {
            RunAdb(string.Format("-s {0} root", serial), 5000);

            string cmd = "shell \"dumpsys account | grep 'type=com.google' | head -n 1; " +
                         "grep -o '\\\"email\\\":\\\"[^\\\"]*\\\"' /data/data/com.picsart.studio/files/user_data 2>/dev/null; " +
                         "grep -o '\\\"accessScopes\\\":\\[\\\"premium\\\"\\]' /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml 2>/dev/null; " +
                         "grep -o 'plan_meta&quot;:{&quot;id&quot;:&quot;[a-z_0-9]*' /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml 2>/dev/null | head -n 1; " +
                         "grep -o 'tierCredits&quot;:[0-9]*' /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml 2>/dev/null | head -n 1; " +
                         "grep -o 'pro_weekly[^}]*is_selected' /data/data/com.picsart.studio/files/settings.json 2>/dev/null | wc -l; " +
                         "grep -o 'pro_monthly[^}]*is_selected' /data/data/com.picsart.studio/files/settings.json 2>/dev/null | wc -l; " +
                         "grep -o 'content_id...plus_tab_default.}' /data/data/com.picsart.studio/files/settings.json 2>/dev/null | wc -l; " +
                         "grep -o 'package_id...subscription_ultra_monthly' /data/data/com.picsart.studio/files/settings.json 2>/dev/null | wc -l\"";

            string outText = RunAdb(string.Format("-s {0} {1}", serial, cmd), 8000);
            string google = "Chưa đăng nhập";
            string picsart = "Chưa đăng nhập";
            string sub = "FREE (Có nút mua)";
            int weeklyCount = 0;
            int monthlyCount = 0;
            int plusTabCount = 0;
            int ultraCount = 0;
            string boughtSku = "";
            string tierCredits = "";
            List<int> counts = new List<int>();

            string[] lines = outText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string l in lines)
            {
                string line = l.Trim();
                Match mg = Regex.Match(line, @"name=([^,]+),\s*type=com\.google");
                if (mg.Success)
                {
                    google = mg.Groups[1].Value;
                    continue;
                }
                Match mp = Regex.Match(line, "\"email\":\"([^\"]+)\"");
                if (mp.Success)
                {
                    picsart = mp.Groups[1].Value;
                    continue;
                }
                if (line.StartsWith("accessScopes&quot;") || line.StartsWith("\"accessScopes\":[\"premium\"]"))
                {
                    sub = "PRO (Đang có gói)";
                    continue;
                }
                Match msku = Regex.Match(line, @"plan_meta&quot;:\{&quot;id&quot;:&quot;([a-z_0-9]+)");
                if (msku.Success)
                {
                    boughtSku = msku.Groups[1].Value;
                    continue;
                }
                Match mcr = Regex.Match(line, @"tierCredits&quot;:(\d+)");
                if (mcr.Success)
                {
                    tierCredits = mcr.Groups[1].Value;
                    continue;
                }
                int num;
                if (int.TryParse(line, out num))
                {
                    counts.Add(num);
                }
            }

            if (counts.Count >= 4)
            {
                weeklyCount = counts[counts.Count - 4];
                monthlyCount = counts[counts.Count - 3];
                plusTabCount = counts[counts.Count - 2];
                ultraCount = counts[counts.Count - 1];
            }

            string planType = "Chưa tải cấu hình";
            if (ultraCount > 0)
            {
                planType = "🔥 Gói Ultra (1 Tháng - 1500Cr)";
            }
            else if (weeklyCount > 0)
            {
                planType = "1 Tuần (Dính A/B Test)";
            }
            else if (monthlyCount > 0)
            {
                planType = plusTabCount > 0 ? "⭐ Gói Plus (1 Tháng)" : "💎 Gói Pro (1 Tháng)";
            }

            return new DeviceInfo
            {
                Serial = serial,
                GoogleAccount = google,
                PicsartAccount = picsart,
                SubStatus = sub,
                PlanType = planType,
                Credits = string.IsNullOrEmpty(boughtSku) && string.IsNullOrEmpty(tierCredits)
                    ? "-"
                    : string.Format("{0} credit | {1}",
                        string.IsNullOrEmpty(tierCredits) ? "?" : tierCredits,
                        string.IsNullOrEmpty(boughtSku) ? "?" : boughtSku),
                LastResult = "Sẵn sàng"
            };
        }

        private async Task RefreshDevicesAsync()
        {
            SetButtonsEnabled(false);
            lblStatus.Text = "Đang quét danh sách thiết bị ADB...";
            AppendLog("Đang quét các thiết bị kết nối ADB...");

            List<string> serials = await Task.Run(() => GetConnectedSerials());
            grid.Rows.Clear();

            if (serials.Count == 0)
            {
                AppendLog("Không tìm thấy thiết bị ADB nào!");
                lblStatus.Text = "Không tìm thấy thiết bị ADB.";
                SetButtonsEnabled(true);
                return;
            }

            var tasks = new List<Task<DeviceInfo>>();
            foreach (string s in serials)
            {
                string ser = s;
                tasks.Add(Task.Run(() => QueryDeviceInfo(ser)));
            }

            DeviceInfo[] infos = await Task.WhenAll(tasks);
            foreach (var info in infos)
            {
                grid.Rows.Add(true, info.Serial, info.GoogleAccount, info.PicsartAccount, info.SubStatus, info.PlanType, info.Credits, info.LastResult);
            }

            allSelected = true;
            lblStatus.Text = string.Format("Đã tìm thấy {0} thiết bị.", infos.Length);
            AppendLog(string.Format("Hoàn tất quét {0} thiết bị.", infos.Length));
            SetButtonsEnabled(true);
        }

        private void UpdateRowStatus(string serial, string resultText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(UpdateRowStatus), serial, resultText);
                return;
            }
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Cells["Serial"].Value != null && row.Cells["Serial"].Value.ToString() == serial)
                {
                    row.Cells["Result"].Value = resultText;
                    break;
                }
            }
        }

        private const string FixPermissionsSnippet =
            "PUID=$(stat -c '%u' /data/data/com.picsart.studio); " +
            "chown -R $PUID:$PUID /data/data/com.picsart.studio/shared_prefs /data/data/com.picsart.studio/files /data/data/com.picsart.studio/cache 2>/dev/null; " +
            "restorecon -R /data/data/com.picsart.studio 2>/dev/null; ";

        private static string GetCleanSubscriptionCacheSnippet(string newDeviceId)
        {
            // Xóa triệt để token thuê bao cũ (store_subscription_validation_key), guard_subscription_key
            // và device-id / internal-device-id để Google Play luôn mở hộp thoại MUA MỚI thay vì báo lỗi
            // "We are unable to change your subscription plan. Manage subscriptions".
            return string.Format(
                "sed -i 's/d_[0-9a-f]\\{{32\\}}/d_{0}/g' /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.xml /data/data/com.picsart.studio/shared_prefs/participated_experiments_prefs_key.xml /data/data/com.picsart.studio/cache/bugsnag/user-info 2>/dev/null; " +
                "sed -i 's/\\\"subscription_status\\\":\\\"subscribed\\\"/\\\"subscription_status\\\":\\\"not_subscribed\\\"/g' /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.tag_settings_cache.xml 2>/dev/null; " +
                "sed -i 's/app_start_subscription_status\\\" value=\\\"true/app_start_subscription_status\\\" value=\\\"false/g; s/SUBSCRIPTION\\\" value=\\\"true/SUBSCRIPTION\\\" value=\\\"false/g' /data/data/com.picsart.studio/shared_prefs/com.picsart.studio_preferences.xml 2>/dev/null; " +
                "rm -f /data/data/com.picsart.studio/files/device-id /data/data/com.picsart.studio/files/internal-device-id /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml /data/data/com.picsart.studio/shared_prefs/com.facebook.internal.iap.IAP_CACHE_GPBLV2V7.xml; ",
                newDeviceId);
        }

        private const string PatchMonthlySnippet =
            "sed -i 's/\\\"pro_weekly_100\\\",\\\"is_selected\\\"/\\\"pro_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"pro_weekly\\\",\\\"is_selected\\\"/\\\"pro_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"ultra_monthly\\\",\\\"is_selected\\\"/\\\"pro_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"ultra_yearly\\\",\\\"is_selected\\\"/\\\"pro_yearly_trial\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"plus_weekly\\\",\\\"is_selected\\\"/\\\"plus_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"package_id\\\":\\\"subscription_pro_weekly\\\"/\\\"package_id\\\":\\\"subscription_pro_monthly\\\"/g; " +
            "s/\\\"package_id\\\":\\\"subscription_ultra_monthly\\\"/\\\"package_id\\\":\\\"subscription_pro_monthly\\\"/g; " +
            "s/\\\"package_id\\\":\\\"subscription_plus_weekly\\\"/\\\"package_id\\\":\\\"subscription_plus_tier_monthly\\\"/g; " +
            "s/title_12/title_3/g; s/subtitle_8/subtitle_2/g; " +
            // Nút pro_monthly mặc định trỏ tới subscription_pro_monthly_100 (chỉ 100 credit/tháng).
            // Ép về SKU chuẩn subscription_pro_monthly (500 credit/tháng - đã xác nhận GPA.3340-5436-4024-22484).
            "s/\\\"package_sku\\\":\\\"subscription_pro_monthly_100\\\"/\\\"package_sku\\\":\\\"subscription_pro_monthly\\\"/g; " +
            "s/\\\"package_sku\\\":\\\"subscription_pro_1_monthly\\\"/\\\"package_sku\\\":\\\"subscription_pro_monthly\\\"/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; ";

        private const string PatchUltraSnippet =
            "sed -i 's/\\\"package_sku\\\":\\\"subscription_ultra_monthly_750_credits\\\"/\\\"package_sku\\\":\\\"subscription_ultra_monthly\\\"/g; " +
            "s/\\\"package_sku\\\":\\\"subscription_ultra_yearly_750_credits\\\"/\\\"package_sku\\\":\\\"subscription_ultra_yearly\\\"/g; " +
            "s/\\\"pro_weekly_100\\\",\\\"is_selected\\\":false/\\\"ultra_monthly\\\",\\\"is_selected\\\":true/g; " +
            "s/\\\"pro_weekly_100\\\",\\\"is_selected\\\"/\\\"ultra_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"pro_weekly\\\",\\\"is_selected\\\":false/\\\"ultra_monthly\\\",\\\"is_selected\\\":true/g; " +
            "s/\\\"pro_weekly\\\",\\\"is_selected\\\"/\\\"ultra_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"pro_monthly\\\",\\\"is_selected\\\":false/\\\"ultra_monthly\\\",\\\"is_selected\\\":true/g; " +
            "s/\\\"pro_monthly\\\",\\\"is_selected\\\"/\\\"ultra_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"plus_weekly\\\",\\\"is_selected\\\":false/\\\"ultra_monthly\\\",\\\"is_selected\\\":true/g; " +
            "s/\\\"plus_weekly\\\",\\\"is_selected\\\"/\\\"ultra_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"plus_monthly\\\",\\\"is_selected\\\":false/\\\"ultra_monthly\\\",\\\"is_selected\\\":true/g; " +
            "s/\\\"plus_monthly\\\",\\\"is_selected\\\"/\\\"ultra_monthly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"ultra_monthly\\\",\\\"is_selected\\\":false/\\\"ultra_monthly\\\",\\\"is_selected\\\":true/g; " +
            "s/\\\"pro_yearly_trial\\\",\\\"is_selected\\\":true/\\\"ultra_yearly\\\",\\\"is_selected\\\":false/g; " +
            "s/\\\"pro_yearly_trial\\\",\\\"is_selected\\\"/\\\"ultra_yearly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"pro_yearly_wo_trial\\\",\\\"is_selected\\\":true/\\\"ultra_yearly\\\",\\\"is_selected\\\":false/g; " +
            "s/\\\"pro_yearly_wo_trial\\\",\\\"is_selected\\\"/\\\"ultra_yearly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"plus_yearly_trial\\\",\\\"is_selected\\\":true/\\\"ultra_yearly\\\",\\\"is_selected\\\":false/g; " +
            "s/\\\"plus_yearly_trial\\\",\\\"is_selected\\\"/\\\"ultra_yearly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"plus_yearly_wo_trial\\\",\\\"is_selected\\\":true/\\\"ultra_yearly\\\",\\\"is_selected\\\":false/g; " +
            "s/\\\"plus_yearly_wo_trial\\\",\\\"is_selected\\\"/\\\"ultra_yearly\\\",\\\"is_selected\\\"/g; " +
            "s/\\\"ultra_yearly\\\",\\\"is_selected\\\":true/\\\"ultra_yearly\\\",\\\"is_selected\\\":false/g; " +
            "s/\\\"package_id\\\":\\\"subscription_pro_weekly\\\"/\\\"package_id\\\":\\\"subscription_ultra_monthly\\\"/g; " +
            "s/\\\"package_id\\\":\\\"subscription_pro_monthly\\\"/\\\"package_id\\\":\\\"subscription_ultra_monthly\\\"/g; " +
            "s/\\\"package_id\\\":\\\"subscription_plus_weekly\\\"/\\\"package_id\\\":\\\"subscription_ultra_monthly\\\"/g; " +
            "s/\\\"package_id\\\":\\\"subscription_plus_tier_monthly\\\"/\\\"package_id\\\":\\\"subscription_ultra_monthly\\\"/g; " +
            "s/\\\"content_id\\\":\\\"plus_tab_default\\\"}/\\\"content_id\\\":\\\"pro_tab_default\\\"}/g; " +
            "s/\\\"content_id\\\":\\\"pro_tab_ultra\\\",\\\"selected\\\"/\\\"content_id\\\":\\\"pro_tab_default\\\",\\\"selected\\\"/g; " +
            "s/\\\"content_id\\\":\\\"ultra_yearly\\\",\\\"badge\\\"/\\\"content_id\\\":\\\"pro_tab_default\\\",\\\"badge\\\"/g; " +
            "s/\\\"content_id\\\":\\\"ultra_yearly_upgrade\\\",\\\"selected\\\"/\\\"content_id\\\":\\\"pro_tab_default\\\",\\\"selected\\\"/g; " +
            "s/title_3/title_12/g; s/subtitle_2/subtitle_8/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; ";

        private const string OpenPaywallDeepLinkCmd =
            "am start -a android.intent.action.VIEW -d 'picsart://subscription?type=full-screen-promotion&subscription_force_enabled=true&source=profile' com.picsart.studio";

        private void PatchGrammarlyNoTrial(string serial)
        {
            UpdateRowStatus(serial, "Đang tắt Trial Grammarly...");
            AppendLog(string.Format("[{0}] Đang trích xuất file cấu hình A/B (manakin-treatments) của Grammarly...", serial));

            string tempFile = Path.Combine(Path.GetTempPath(), "manakin_" + serial + ".pb");
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                RunAdb(string.Format("-s {0} pull /data/data/com.grammarly.android.keyboard/files/datastore/manakin-treatments \"{1}\"", serial, tempFile), 8000);

                if (!File.Exists(tempFile))
                {
                    AppendLog(string.Format("[{0}] ⚠️ Không tìm thấy file manakin-treatments (Hãy mở Grammarly và đăng nhập trước)!", serial));
                    UpdateRowStatus(serial, "⚠️ Chưa mở Grammarly");
                    return;
                }

                byte[] bytes = File.ReadAllBytes(tempFile);
                string hex = BitConverter.ToString(bytes).Replace("-", "");

                // Tìm protobuf field "android_free_trial" (0A 12 616E64726F69645F667265655F747269616C)
                const string targetHeader = "1A660A12616E64726F69645F667265655F747269616C";
                const string replacementHeader = "1A690A12616E64726F69645F667265655F747269616C";
                const string testGroupHex = "2A06746573745F31";       // * \x06 test_1
                const string controlGroupHex = "2A09636F6E74726F6C5F31"; // * \x09 control_1

                int headerIdx = hex.IndexOf(targetHeader);
                if (headerIdx >= 0)
                {
                    int testIdx = hex.IndexOf(testGroupHex, headerIdx);
                    if (testIdx >= 0 && testIdx - headerIdx < 240)
                    {
                        hex = hex.Substring(0, headerIdx) + replacementHeader + hex.Substring(headerIdx + targetHeader.Length);
                        int adjustedTestIdx = hex.IndexOf(testGroupHex, headerIdx);
                        hex = hex.Substring(0, adjustedTestIdx) + controlGroupHex + hex.Substring(adjustedTestIdx + testGroupHex.Length);

                        byte[] newBytes = new byte[hex.Length / 2];
                        for (int i = 0; i < newBytes.Length; i++)
                        {
                            newBytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                        }
                        File.WriteAllBytes(tempFile, newBytes);

                        RunAdb(string.Format("-s {0} shell \"am force-stop com.grammarly.android.keyboard; chmod 660 /data/data/com.grammarly.android.keyboard/files/datastore/manakin-treatments 2>/dev/null\"", serial), 5000);
                        RunAdb(string.Format("-s {0} push \"{1}\" /data/data/com.grammarly.android.keyboard/files/datastore/manakin-treatments", serial, tempFile), 8000);
                        RunAdb(string.Format("-s {0} shell \"PUID=$(stat -c '%u' /data/data/com.grammarly.android.keyboard); chown -R $PUID:$PUID /data/data/com.grammarly.android.keyboard/files/datastore; chmod 440 /data/data/com.grammarly.android.keyboard/files/datastore/manakin-treatments; restorecon -R /data/data/com.grammarly.android.keyboard/files/datastore; am start -n com.grammarly.android.keyboard/com.grammarly.widget.ui.settings.MainCompanionActivity\"", serial), 8000);

                        AppendLog(string.Format("[{0}] 🟢 Đã chuyển android_free_trial -> control_1 & khóa file thành công! Nút mua đã chuyển sang Mua Thẳng (Continue with Plan)!", serial));
                        UpdateRowStatus(serial, "🟢 Đã tắt Trial Grammarly");
                        return;
                    }
                }

                if (hex.Contains(replacementHeader))
                {
                    RunAdb(string.Format("-s {0} shell \"am start -n com.grammarly.android.keyboard/com.grammarly.widget.ui.settings.MainCompanionActivity\"", serial), 5000);
                    AppendLog(string.Format("[{0}] 🟢 Máy này đã ở chế độ Mua Thẳng (control_1) sẵn rồi!", serial));
                    UpdateRowStatus(serial, "🟢 Đã tắt Trial sẵn");
                }
                else
                {
                    AppendLog(string.Format("[{0}] ⚠️ Không tìm thấy cờ android_free_trial trong manakin-treatments!", serial));
                    UpdateRowStatus(serial, "⚠️ Chưa có cờ Trial");
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        private void PatchChessNoTrial(string serial)
        {
            const string playDb = "/data/data/com.android.vending/databases/sku_details.db";
            const string farFutureExpiration = "4102444800000"; // 2100-01-01 UTC

            UpdateRowStatus(serial, "Đang vá offer Billing Chess...");
            AppendLog(string.Format("[{0}] Kiểm tra cache Google Play Billing của Chess.com...", serial));

            string safeSerial = Regex.Replace(serial, @"[^A-Za-z0-9._-]", "_");
            string backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PicsartResetTool", "Backups", "Chess", safeSerial,
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string dbBefore = Path.Combine(backupDir, "sku_details.before.db");
            string dbAfter = Path.Combine(backupDir, "sku_details.after.db");
            string sqlFile = Path.Combine(backupDir, "patch_chess_no_trial.sql");
            string remoteSql = "/data/local/tmp/chess_no_trial_" + safeSerial + ".sql";

            try
            {
                string preflight = RunAdb(string.Format(
                    "-s {0} shell \"test -f {1} && echo PLAY_DB_OK; command -v sqlite3; pm path com.chess\"",
                    serial, playDb), 8000);
                if (!preflight.Contains("PLAY_DB_OK"))
                {
                    throw new FileNotFoundException("Chưa có cache Google Play Billing. Hãy mở Chess.com và trang Premium một lần rồi chạy lại.");
                }
                if (!preflight.Contains("sqlite3"))
                {
                    throw new FileNotFoundException("Thiết bị không có sqlite3 để sửa cache Billing an toàn.");
                }
                if (!preflight.Contains("package:"))
                {
                    throw new FileNotFoundException("Không tìm thấy ứng dụng com.chess trên thiết bị.");
                }

                Directory.CreateDirectory(backupDir);
                RunAdb(string.Format("-s {0} shell \"am force-stop com.chess; am force-stop com.android.vending\"", serial), 8000);
                RunAdb(string.Format("-s {0} pull {1} \"{2}\"", serial, playDb, dbBefore), 10000);
                RunAdb(string.Format("-s {0} pull {1}-wal \"{2}\"", serial, playDb, Path.Combine(backupDir, "sku_details.before.db-wal")), 10000);
                RunAdb(string.Format("-s {0} pull {1}-shm \"{2}\"", serial, playDb, Path.Combine(backupDir, "sku_details.before.db-shm")), 10000);
                if (!File.Exists(dbBefore) || new FileInfo(dbBefore).Length == 0)
                {
                    throw new IOException("Không thể sao lưu sku_details.db trước khi sửa.");
                }

                // Chess ưu tiên offerId từ incentive, sau đó offerId "freetrial", rồi mới tới base plan.
                // Đổi nhãn trial bằng chuỗi cùng độ dài giữ nguyên framing protobuf trong BLOB cache.
                string sql =
                    ".bail on\n" +
                    ".timeout 10000\n" +
                    "SELECT 'CHESS_CACHE_ROWS=' || count(*) FROM cache_table " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' AND cache_key LIKE '%skuType=subs%';\n" +
                    "SELECT 'BYTES_BEFORE=' || ifnull(sum(length(cache_data)),0) FROM cache_table " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' AND cache_key LIKE '%skuType=subs%';\n" +
                    "BEGIN IMMEDIATE;\n" +
                    "UPDATE cache_table SET " +
                    "cache_data=CAST(replace(replace(CAST(cache_data AS TEXT)," +
                    "'\"offerId\":\"freetrial\"','\"offerId\":\"notrialxx\"')," +
                    "'free7days','no7trials') AS BLOB), " +
                    "expiration_timestamp=" + farFutureExpiration + " " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' " +
                    "AND cache_key LIKE '%skuType=subs%' " +
                    "AND (instr(cache_data,'\"offerId\":\"freetrial\"')>0 " +
                    "OR instr(cache_data,'free7days')>0 " +
                    "OR instr(cache_data,'\"offerId\":\"notrialxx\"')>0 " +
                    "OR instr(cache_data,'no7trials')>0);\n" +
                    "SELECT 'TOUCHED_ROWS=' || changes();\n" +
                    "COMMIT;\n" +
                    "PRAGMA wal_checkpoint(FULL);\n" +
                    "SELECT 'TRIAL_REMAINING=' || count(*) FROM cache_table " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' " +
                    "AND (instr(cache_data,'\"offerId\":\"freetrial\"')>0 OR instr(cache_data,'free7days')>0);\n" +
                    "SELECT 'PATCHED_ROWS_PRESENT=' || count(*) FROM cache_table " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' " +
                    "AND (instr(cache_data,'\"offerId\":\"notrialxx\"')>0 OR instr(cache_data,'no7trials')>0);\n" +
                    "SELECT 'BASE_PLAN_ROWS=' || count(*) FROM cache_table " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' " +
                    "AND instr(cache_data,'\"basePlanId\"')>0;\n" +
                    "SELECT 'BYTES_AFTER=' || ifnull(sum(length(cache_data)),0) FROM cache_table " +
                    "WHERE cache_key LIKE '%callingPackageName=com.chess%' AND cache_key LIKE '%skuType=subs%';\n";
                File.WriteAllText(sqlFile, sql, new UTF8Encoding(false));
                RunAdb(string.Format("-s {0} push \"{1}\" {2}", serial, sqlFile, remoteSql), 10000);

                string patchOutput = RunAdb(string.Format(
                    "-s {0} shell \"sqlite3 {1} < {2}; rm -f {2}\"",
                    serial, playDb, remoteSql), 20000);
                Match cacheRows = Regex.Match(patchOutput, @"CHESS_CACHE_ROWS=(\d+)");
                Match patchedRows = Regex.Match(patchOutput, @"PATCHED_ROWS_PRESENT=(\d+)");
                Match baseRows = Regex.Match(patchOutput, @"BASE_PLAN_ROWS=(\d+)");
                Match bytesBefore = Regex.Match(patchOutput, @"BYTES_BEFORE=(\d+)");
                Match bytesAfter = Regex.Match(patchOutput, @"BYTES_AFTER=(\d+)");
                if (!patchOutput.Contains("TRIAL_REMAINING=0"))
                {
                    throw new InvalidDataException("Cache Billing vẫn còn offer trial sau khi vá.");
                }
                if (!cacheRows.Success || Convert.ToInt32(cacheRows.Groups[1].Value) == 0)
                {
                    throw new InvalidDataException("Không tìm thấy dòng subscription của Chess trong cache Billing.");
                }
                if (!patchedRows.Success || Convert.ToInt32(patchedRows.Groups[1].Value) == 0)
                {
                    throw new InvalidDataException("Không tìm thấy offer trial để vô hiệu hóa.");
                }
                if (!baseRows.Success || Convert.ToInt32(baseRows.Groups[1].Value) == 0)
                {
                    throw new InvalidDataException("Không còn base plan để thanh toán trực tiếp; đã dừng để tránh lỗi mua hàng.");
                }
                if (!bytesBefore.Success || !bytesAfter.Success || bytesBefore.Groups[1].Value != bytesAfter.Groups[1].Value)
                {
                    throw new InvalidDataException("Kích thước cache Billing thay đổi ngoài dự kiến.");
                }

                RunAdb(string.Format("-s {0} pull {1} \"{2}\"", serial, playDb, dbAfter), 10000);
                RunAdb(string.Format("-s {0} pull {1}-wal \"{2}\"", serial, playDb, Path.Combine(backupDir, "sku_details.after.db-wal")), 10000);
                RunAdb(string.Format("-s {0} pull {1}-shm \"{2}\"", serial, playDb, Path.Combine(backupDir, "sku_details.after.db-shm")), 10000);
                RunAdb(string.Format("-s {0} shell \"monkey -p com.chess -c android.intent.category.LAUNCHER 1\"", serial), 8000);
                AppendLog(string.Format(
                    "[{0}] ♟ Thành công: đã bỏ offer trial của Chess.com, giữ base plan để mua trực tiếp. Backup: {1}",
                    serial, backupDir));
                UpdateRowStatus(serial, "♟ Chess: Go Premium (mua thẳng)");
            }
            catch (Exception ex)
            {
                AppendLog(string.Format("[{0}] ❌ Lỗi tắt Trial Chess.com: {1}", serial, ex.Message));
                UpdateRowStatus(serial, "❌ Chess: " + ex.Message);
            }
        }

        private void CheckAndWarnIfAccountTainted(string serial)
        {
            string check = RunAdb(string.Format("-s {0} shell \"grep -o 'accessScopes.*premium' /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml 2>/dev/null\"", serial), 5000);
            if (!string.IsNullOrEmpty(check) && check.Contains("premium"))
            {
                AppendLog(string.Format("[{0}] ⚠️ LƯU Ý: Tài khoản Picsart đang đăng nhập đã bị dính gói Pro cũ trên server của Picsart! Để mua gói mới không bị lỗi 'Unable to change your subscription plan', hãy bấm nút '🧹 Reset Dữ Liệu Picsart' (hoặc đăng xuất nick Picsart này) rồi đăng nhập nick Picsart mới!", serial));
            }
        }

        private void ProcessSingleDevice(string serial, string mode)
        {
            UpdateRowStatus(serial, "Đang xử lý...");
            AppendLog(string.Format("[{0}] Bắt đầu xử lý (Chế độ: {1})...", serial, mode));

            RunAdb(string.Format("-s {0} root", serial), 5000);
            string newDeviceId = GetRandomHex(32);

            if (mode == "grammarly_notrial")
            {
                PatchGrammarlyNoTrial(serial);
                return;
            }

            if (mode == "chess_notrial")
            {
                PatchChessNoTrial(serial);
                return;
            }

            if (mode == "deeplink")
            {
                UpdateRowStatus(serial, "Đang mở Bảng giá...");
                string dlCmd = string.Format("shell \"{0}\"", OpenPaywallDeepLinkCmd);
                RunAdb(string.Format("-s {0} {1}", serial, dlCmd), 8000);
                AppendLog(string.Format("[{0}] Đã bắn DeepLink mở thẳng màn hình Bảng giá!", serial));
                UpdateRowStatus(serial, "💎 Đã mở Bảng giá");
                return;
            }

            if (mode == "ultra")
            {
                UpdateRowStatus(serial, "Đang ép Gói Ultra (1500Cr)...");
                string ultraCmd = "shell \"am force-stop com.picsart.studio; " +
                                  GetCleanSubscriptionCacheSnippet(newDeviceId) +
                                  PatchUltraSnippet +
                                  FixPermissionsSnippet +
                                  "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1\"";
                RunAdb(string.Format("-s {0} {1}", serial, ultraCmd), 25000);
                UpdateRowStatus(serial, "Đang kiểm tra gói Ultra...");
                System.Threading.Thread.Sleep(4500);
                CheckAndWarnIfAccountTainted(serial);
                AppendLog(string.Format("[{0}] 🔥 Đã ép Gói Ultra 1 Tháng (subscription_ultra_monthly - 1500 Credit/tháng) & xóa token thuê bao cũ thành công!", serial));
                UpdateRowStatus(serial, "🔥 Đã bật Ultra (1 Tháng - 1500Cr)");
                return;
            }

            if (mode == "plus")
            {
                UpdateRowStatus(serial, "Đang ép Gói Plus (1 Tháng)...");
                string plusCmd = "shell \"am force-stop com.picsart.studio; " +
                                 GetCleanSubscriptionCacheSnippet(newDeviceId) +
                                 PatchMonthlySnippet +
                                 "sed -i 's/\\\"content_id\\\":\\\"pro_tab_default\\\"}/\\\"content_id\\\":\\\"plus_tab_default\\\"}/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; " +
                                 FixPermissionsSnippet +
                                 "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1\"";
                RunAdb(string.Format("-s {0} {1}", serial, plusCmd), 25000);
                UpdateRowStatus(serial, "Đang kiểm tra gói Plus...");
                System.Threading.Thread.Sleep(4500);
                CheckAndWarnIfAccountTainted(serial);
                AppendLog(string.Format("[{0}] ⭐ Đã chuyển Bảng giá sang Gói Plus (1 Tháng) & xóa token thuê bao cũ thành công!", serial));
                UpdateRowStatus(serial, "⭐ Đã bật Gói Plus (1 Tháng)");
                return;
            }

            if (mode == "monthly")
            {
                UpdateRowStatus(serial, "Đang ép Gói Pro 1 Tháng...");
                string patchCmd = "shell \"am force-stop com.picsart.studio; " +
                                  GetCleanSubscriptionCacheSnippet(newDeviceId) +
                                  PatchMonthlySnippet +
                                  "sed -i 's/\\\"content_id\\\":\\\"plus_tab_default\\\"}/\\\"content_id\\\":\\\"pro_tab_default\\\"}/g' /data/data/com.picsart.studio/files/settings.json 2>/dev/null; " +
                                  FixPermissionsSnippet +
                                  "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1\"";
                RunAdb(string.Format("-s {0} {1}", serial, patchCmd), 25000);
                UpdateRowStatus(serial, "Đang kiểm tra gói Pro...");
                System.Threading.Thread.Sleep(4500);
                CheckAndWarnIfAccountTainted(serial);
                AppendLog(string.Format("[{0}] 📅 Đã ép cấu hình Gói Pro 1 Tháng (500 Credit) & xóa token thuê bao cũ thành công!", serial));
                UpdateRowStatus(serial, "✅ Đã ép Gói Pro 1 Tháng");
                return;
            }

            if (mode == "full")
            {
                UpdateRowStatus(serial, "Đang xóa dữ liệu Picsart...");
                AppendLog(string.Format("[{0}] Xóa dữ liệu Picsart, giữ cấu hình Bảng giá & tạo sẵn device_id mới d_{1} (Giữ nguyên Gmail)...", serial, newDeviceId));

                string fullCmd = string.Format(
                    "shell \"cp /data/data/com.picsart.studio/files/settings.json /sdcard/.picsart_settings_bak.json 2>/dev/null; " +
                    "pm clear com.picsart.studio; " +
                    "mkdir -p /data/data/com.picsart.studio/shared_prefs /data/data/com.picsart.studio/files; " +
                    "if [ -f /sdcard/.picsart_settings_bak.json ]; then cp /sdcard/.picsart_settings_bak.json /data/data/com.picsart.studio/files/settings.json; rm -f /sdcard/.picsart_settings_bak.json; fi; " +
                    "printf '<?xml version=\\\"1.0\\\" encoding=\\\"utf-8\\\" standalone=\\\"yes\\\" ?>\\n<map>\\n    <string name=\\\"device_id\\\">d_{0}</string>\\n</map>\\n' > /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.xml; " +
                    "chmod 660 /data/data/com.picsart.studio/shared_prefs/com.picsart.analytics.xml; " +
                    FixPermissionsSnippet +
                    "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1\"",
                    newDeviceId);

                RunAdb(string.Format("-s {0} {1}", serial, fullCmd), 25000);
                System.Threading.Thread.Sleep(4500);
                AppendLog(string.Format("[{0}] Hoàn tất Reset Picsart + xóa sạch liên kết gói cũ + cấp device_id mới (Gmail an toàn 100%)!", serial));
                UpdateRowStatus(serial, "✅ Đã Reset Picsart (Giữ Gmail)");
            }
            else
            {
                UpdateRowStatus(serial, "Đang đổi Picsart ID & ép 1 Tháng...");
                AppendLog(string.Format("[{0}] Đổi riêng Picsart device_id -> d_{1}, xóa token gói cũ & ép gói 1 Tháng (Không đụng tới Gmail)...", serial, newDeviceId));

                string keepCmd =
                    "shell \"am force-stop com.picsart.studio; " +
                    GetCleanSubscriptionCacheSnippet(newDeviceId) +
                    PatchMonthlySnippet +
                    FixPermissionsSnippet +
                    "monkey -p com.picsart.studio -c android.intent.category.LAUNCHER 1\"";

                RunAdb(string.Format("-s {0} {1}", serial, keepCmd), 25000);
                UpdateRowStatus(serial, "Đang kiểm tra lại gói...");
                System.Threading.Thread.Sleep(4500);
                CheckAndWarnIfAccountTainted(serial);

                AppendLog(string.Format("[{0}] Hoàn tất Fix Dính Pro + Ép Gói 1 Tháng (Giữ nguyên Gmail & Nick Picsart)!", serial));
                UpdateRowStatus(serial, "✅ Đã Fix Pro + Ép 1 Tháng");
            }
        }

        private async Task RunActionOnSelectedAsync(string mode)
        {
            List<string> selected = new List<string>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                bool isChecked = row.Cells["Selected"].Value != null && (bool)row.Cells["Selected"].Value;
                if (isChecked && row.Cells["Serial"].Value != null)
                {
                    selected.Add(row.Cells["Serial"].Value.ToString());
                }
            }

            if (selected.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất 1 thiết bị trong bảng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetButtonsEnabled(false);
            lblStatus.Text = string.Format("Đang xử lý đồng loạt trên {0} thiết bị...", selected.Count);

            var tasks = new List<Task>();
            foreach (string s in selected)
            {
                string ser = s;
                tasks.Add(Task.Run(() => ProcessSingleDevice(ser, mode)));
            }

            await Task.WhenAll(tasks);

            if (mode != "deeplink" && mode != "grammarly_notrial" && mode != "chess_notrial")
            {
                await RefreshDevicesAsync();
            }
            else
            {
                SetButtonsEnabled(true);
            }
            lblStatus.Text = string.Format("Hoàn tất xử lý {0} thiết bị!", selected.Count);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
