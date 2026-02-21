using System.Text.RegularExpressions;

namespace CareerRoutine.Models
{
    public class Job
    {
        public string MessageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BodyText { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        // Gmail内部時間（Unixミリ秒）
        public long InternalDate { get; set; }
        // 人間表示用（UTC）
        public DateTimeOffset ReceivedAtUtc =>
            DateTimeOffset.FromUnixTimeMilliseconds(InternalDate);
        // 人間表示用（ローカル）
        public DateTimeOffset ReceivedAtLocal =>
            ReceivedAtUtc.ToLocalTime();

        public string GetFullText()
        {
            string htmlText = StripHtml(BodyHtml);
            return $"{Title} {BodyText} {htmlText}";
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            return Regex.Replace(html, "<.*?>", " ");
        }
    }
}
