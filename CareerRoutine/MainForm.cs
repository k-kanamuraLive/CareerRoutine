using CareerRoutine.Services;
using CareerRoutine.Models;
using System.Diagnostics;
using CareerRoutine.Analizers;

namespace CareerRoutine
{
    public partial class MainForm : Form
    {
        private readonly GmailFetcher _fetcher = new GmailFetcher();
        private readonly SkillMatcher _matcher = new SkillMatcher();
        private readonly GmailLabelService _labelService = new GmailLabelService();

        private Job? _today;
        private Job? _cursor; // CareerRoutine.Selected が付いた最新メール

        public MainForm()
        {
            InitializeComponent();

            // フォーム表示後に非同期でカーソル情報を取得
            this.Shown += MainForm_Shown;
        }

        // ===== 起動時: カーソル後の件数をラベルに表示 =====
        private async void MainForm_Shown(object? sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel.Text = "取得中...";

                _cursor = await _labelService.GetCursorJobAsync();

                if (_cursor == null)
                {
                    toolStripStatusLabel.Text = "カーソルなし（全件対象）";
                }
                else
                {
                    int count = await _labelService.CountAfterCursorAsync(_cursor);
                    toolStripStatusLabel.Text =
                        $"カーソル以降の新着: {count} 件  " +
                        $"（最終確認: {_cursor.ReceivedAt:yyyy/MM/dd HH:mm}）";
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = $"件数取得エラー: {ex.Message}";
            }
        }

        // ===== 今日の1件ボタン =====
        private async void btnFetch_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            var progress = new Progress<int>(v => progressBar1.Value = v);

            var jobs = await _fetcher.FetchAsync(progress, _cursor);
            _today = _matcher.PickOne(jobs);

#if DEBUG
            Analizer.Output(jobs);
#endif

            lnkOpen.Text = "";

            if (_today == null)
            {
                ContentaTextBox.Text = "今日は候補なし。市場観察5分。";
                return; // 早期リターン（元コードのバグも修正）
            }

            ContentaTextBox.Text = _today.GetFullText();
            lnkOpen.Text = _today.Title;
        }

        // ===== リンククリック: Gmailを開く ＋ ラベル付与 =====
        private async void lnkOpen_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            if (_today == null) return;

            // ===== ① Gmailブラウザで開く =====
            var url = $"https://mail.google.com/mail/u/0/#inbox/{_today.MessageId}";
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

            // ===== ② Selectedラベル付与 =====
            try
            {
                await _labelService.ApplyLabelAsync(_today.MessageId);

                // カーソルを最新化して件数表示も更新
                _cursor = _today;
                int count = await _labelService.CountAfterCursorAsync(_cursor);
                toolStripStatusLabel.Text =
                    $"カーソル以降の新着: {count} 件  " +
                    $"（最終確認: {_cursor.ReceivedAt:yyyy/MM/dd HH:mm}）";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ラベル付与に失敗しました: {ex.Message}",
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
