// ============================================================================
//  ConsoleSplash Setup —— 界面 / installer window
// ============================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ConsoleSplash.Setup
{
    internal sealed class SetupForm : Form
    {
        private Texts _texts;
        private string _language;

        private Label _title;
        private Label _subtitle;
        private Label _languageLabel;
        private ComboBox _languageBox;

        private readonly List<Label> _checkNames = new List<Label>();
        private readonly List<Label> _checkStates = new List<Label>();
        private readonly List<PictureBox> _checkIcons = new List<PictureBox>();

        private TextBox _log;
        private Button _install;
        private Button _close;
        private Label _status;

        private EnvironmentInfo _info;

        private static readonly Color Bg = Color.FromArgb(6, 9, 12);
        private static readonly Color Bg2 = Color.FromArgb(11, 18, 24);
        private static readonly Color Ink = Color.FromArgb(226, 238, 246);
        private static readonly Color Muted = Color.FromArgb(129, 149, 161);
        private static readonly Color Accent = Color.FromArgb(127, 219, 255);
        private static readonly Color Good = Color.FromArgb(63, 185, 80);
        private static readonly Color Warn = Color.FromArgb(210, 153, 34);

        internal SetupForm(Texts texts, string language)
        {
            _texts = texts;
            _language = texts.Code;

            Build();
            ApplyLanguage(texts);
            RunChecks();
        }

        private void Build()
        {
            Text = "ConsoleSplash Setup";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(660, 566);
            BackColor = Bg;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);

            _title = new Label();
            _title.SetBounds(24, 20, 470, 34);
            _title.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            _title.ForeColor = Accent;
            _title.AutoSize = false;
            Controls.Add(_title);

            _subtitle = new Label();
            _subtitle.SetBounds(24, 56, 610, 22);
            _subtitle.ForeColor = Muted;
            Controls.Add(_subtitle);

            _languageLabel = new Label();
            _languageLabel.SetBounds(470, 24, 90, 18);
            _languageLabel.ForeColor = Muted;
            _languageLabel.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(_languageLabel);

            _languageBox = new ComboBox();
            _languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageBox.SetBounds(566, 21, 78, 24);
            _languageBox.BackColor = Bg2;
            _languageBox.ForeColor = Ink;
            _languageBox.FlatStyle = FlatStyle.Flat;
            string[] codes = new string[] {
                "en", "zh-CN", "zh-TW", "ru", "uk", "ko", "de", "fr", "es", "it",
                "pt-BR", "pl", "tr", "vi"
            };
            _languageBox.Items.AddRange(codes);
            _languageBox.SelectedItem = _language;
            _languageBox.SelectedIndexChanged += delegate
            {
                string code = _languageBox.SelectedItem as string;
                if (code == null) return;
                _language = code;
                ApplyLanguage(Texts.For(code));
            };
            Controls.Add(_languageBox);

            int top = 96;
            for (int i = 0; i < 5; i++)
            {
                PictureBox icon = new PictureBox();
                icon.SetBounds(24, top + 5, 12, 12);
                icon.BackColor = Bg2;
                Controls.Add(icon);
                _checkIcons.Add(icon);

                Label name = new Label();
                name.SetBounds(46, top, 380, 22);
                name.ForeColor = Ink;
                Controls.Add(name);
                _checkNames.Add(name);

                Label state = new Label();
                state.SetBounds(430, top, 200, 22);
                state.TextAlign = ContentAlignment.MiddleRight;
                Controls.Add(state);
                _checkStates.Add(state);

                top += 30;
            }

            _status = new Label();
            _status.SetBounds(24, top + 6, 610, 22);
            _status.ForeColor = Muted;
            Controls.Add(_status);

            _log = new TextBox();
            _log.SetBounds(24, top + 34, 612, 172);
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Bg2;
            _log.ForeColor = Color.FromArgb(185, 212, 226);
            _log.BorderStyle = BorderStyle.FixedSingle;
            _log.Font = new Font("Consolas", 8.5f);
            Controls.Add(_log);

            _install = new Button();
            _install.SetBounds(428, 512, 100, 32);
            _install.FlatStyle = FlatStyle.Flat;
            _install.FlatAppearance.BorderColor = Accent;
            _install.BackColor = Color.FromArgb(18, 34, 42);
            _install.ForeColor = Accent;
            _install.Click += delegate { Install(); };
            Controls.Add(_install);

            _close = new Button();
            _close.SetBounds(536, 512, 100, 32);
            _close.FlatStyle = FlatStyle.Flat;
            _close.FlatAppearance.BorderColor = Color.FromArgb(52, 68, 78);
            _close.BackColor = Bg2;
            _close.ForeColor = Muted;
            _close.Click += delegate { Close(); };
            Controls.Add(_close);
        }

        private void ApplyLanguage(Texts texts)
        {
            _texts = texts;
            Text = texts.Title;
            _title.Text = texts.Title;
            _subtitle.Text = texts.Subtitle;
            _languageLabel.Text = texts.Language;
            _install.Text = texts.Install;
            _close.Text = texts.Close;

            string[] names = new string[]
            {
                texts.CheckOs, texts.CheckDotNet, texts.CheckWebView,
                texts.CheckFramework, texts.CheckTar
            };

            for (int i = 0; i < names.Length && i < _checkNames.Count; i++)
                _checkNames[i].Text = names[i];

            if (_info != null) RenderStates();
        }

        private void RunChecks()
        {
            _info = EnvironmentInfo.Detect();
            RenderStates();

            _status.ForeColor = _info.Supported ? Good : Warn;
            _status.Text = _info.Supported ? _texts.Ready : _texts.Unsupported;
            _install.Enabled = _info.Supported;

            if (_info.Blockers.Contains("framework"))
                Log("LabAPI / EXILED folder was not found under %APPDATA%\\SCP Secret Laboratory.");
            if (_info.Blockers.Contains("dotnet"))
                Log(".NET Framework 4.8 is required: https://dotnet.microsoft.com/download/dotnet-framework");
        }

        private void RenderStates()
        {
            bool[] states = new bool[]
            {
                _info.OsOk, _info.DotNetOk, _info.WebView2, _info.HasFramework, _info.TarAvailable
            };

            for (int i = 0; i < states.Length; i++)
            {
                bool value = states[i];
                bool optional = i == 2 || i == 4;

                _checkStates[i].Text = value ? _texts.Yes : (optional ? _texts.No + " ·" : _texts.No);
                _checkStates[i].ForeColor = value ? Good : (optional ? Muted : Warn);
                _checkIcons[i].BackColor = value ? Good : (optional ? Color.FromArgb(52, 68, 78) : Warn);
            }
        }

        private void Install()
        {
            _install.Enabled = false;
            _log.Clear();

            if (!_info.WebView2) Log(_texts.WarningWebView);
            Log(_texts.Extracting + " ...");

            bool ok = Installer.Run(_info, _texts, Log);

            _status.ForeColor = ok ? Good : Warn;
            _status.Text = ok ? _texts.Done : _texts.Unsupported;
            _install.Enabled = true;
        }

        private void Log(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _log.AppendText(line + Environment.NewLine);
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
    }
}
