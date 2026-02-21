using CareerRoutine.Models;
using Google.Apis.Gmail.v1.Data;
using System.Net;
using System.Text;
using Message = Google.Apis.Gmail.v1.Data.Message;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace CareerRoutine.Services
{
    public class GmailFetcher
    {
        // ===== メール取得 =====
        // cursorDate が指定された場合、その日時より後のメールのみ取得する
        public async Task<List<Job>> FetchAsync(
            IProgress<int>? progress = null,
            Job? cursor = null)
        {
            var service = await GmailServiceFactory.CreateServiceAsync();

            var request = service.Users.Messages.List("me");

            // カーソルが存在する場合は after: クエリを追加
            if (cursor != null)
            {
                long unixSec = new DateTimeOffset(cursor.ReceivedAt, TimeZoneInfo.Local.GetUtcOffset(cursor.ReceivedAt))
                    .ToUnixTimeSeconds() + 1;

                request.Q = $"in:inbox after:{unixSec}";
                // 上限はデフォルト（100件）のまま
            }
            else
            {
                request.Q = "in:inbox";
            }

            var response = await request.ExecuteAsync();

            var jobs = new List<Job>();

            if (response.Messages == null)
                return jobs;

            int total = response.Messages.Count;
            int current = 0;

            foreach (var msg in response.Messages)
            {
                current++;
                progress?.Report(current * 100 / total);

                var message =
                    await service.Users.Messages
                        .Get("me", msg.Id)
                        .ExecuteAsync();

                var (text, html) = ExtractBody(message);

                jobs.Add(new Job
                {
                    MessageId = message.Id,
                    Title = GetHeader(message, "Subject"),
                    BodyText = text,
                    BodyHtml = html,
                    Sender = GetHeader(message, "From"),
                    ReceivedAt = DateTimeOffset
                        .FromUnixTimeMilliseconds((long)message.InternalDate!)
                        .ToLocalTime()  // ← 日本時間
                        .DateTime
                });
            }

            // 追加の安全策：念のためカーソル以降のメールだけに絞る（API クエリで漏れがあった場合に備える）
            if (cursor != null)
                jobs = jobs.Where(j => j.ReceivedAt > cursor.ReceivedAt).ToList();

            return jobs;
        }

        private string GetHeader(Message message, string name)
        {
            return message.Payload.Headers
                .FirstOrDefault(h => h.Name == name)
                ?.Value ?? "";
        }

        private (string text, string html) ExtractBody(Message message)
        {
            string text = "";
            string html = "";

            void Traverse(MessagePart part)
            {
                if (part == null) return;

                if (part.MimeType == "text/plain" && part.Body?.Data != null)
                    text = DecodeBase64(part.Body.Data);

                if (part.MimeType == "text/html" && part.Body?.Data != null)
                    html = DecodeBase64(part.Body.Data);

                if (part.Parts != null)
                    foreach (var p in part.Parts)
                        Traverse(p);
            }

            Traverse(message.Payload);

            if (!string.IsNullOrWhiteSpace(text))
                return (NormalizeText(text), html);
            else
                return (ConvertHtmlToText(html), html);
        }

        private string DecodeBase64(string input)
        {
            var bytes = Convert.FromBase64String(
                input.Replace('-', '+').Replace('_', '/'));
            return Encoding.UTF8.GetString(bytes);
        }

        private string ConvertHtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scripts = doc.DocumentNode.SelectNodes("//script|//style");
            if (scripts != null)
                foreach (var node in scripts)
                    node.Remove();

            var text = doc.DocumentNode.InnerText;
            text = WebUtility.HtmlDecode(text);
            text = NormalizeText(text);
            return text;
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            text = System.Text.RegularExpressions.Regex.Replace(text, "[ \t]+", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, "\n{3,}", "\n\n");
            return text.Trim();
        }
    }
}
