namespace CareerRoutine
{
    using CareerRoutine.Services;
    using Google.Apis.Gmail.v1;
    using CareerRoutine.Models;
    using System.Xml;

    public partial class MainForm : Form
    {
        private readonly GmailFetcher _fetcher =
            new GmailFetcher();

        private readonly SkillMatcher _matcher =
            new SkillMatcher();

        private Job? _today;

        public MainForm()
        {
            InitializeComponent();
        }

        private async void btnFetch_Click(
            object sender, EventArgs e)
        {
            progressBar1.Value = 0;

            var progress =
            new Progress<int>(v =>
            {
                progressBar1.Value = v;
            });

            var jobs = await _fetcher.FetchAsync(progress);

            _today = _matcher.PickOne(jobs);

            lnkOpen.Text = "";
            if (_today == null)
            {
                ContentaTextBox.Text = "今日は候補なし。市場観察5分。";
                lnkOpen.Text = "";
            }
            ContentaTextBox.Text = _today.GetFullText();
            lnkOpen.Text = _today.Title;
        }

        private async void lnkOpen_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            if (_today == null)
                return;

            // ===== ① Gmailブラウザで開く =====
            var url =
                $"https://mail.google.com/mail/u/0/#inbox/{_today.MessageId}";

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

            // ===== ② Selectedラベル付与 =====
        }
    }
}
