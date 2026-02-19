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
        public DateTime ReceivedAt { get; set; }

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
