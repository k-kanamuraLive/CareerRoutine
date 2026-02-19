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
        public async Task<List<Job>> FetchAsync(
            IProgress<int>? progress = null)
        {
            var service = await GmailServiceFactory.CreateServiceAsync();

            var request = service.Users.Messages.List("me");

            //request.MaxResults = 50;

            request.Q = "in:inbox";

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
                        .FromUnixTimeMilliseconds((long)message.InternalDate)
                        .DateTime
                });
            }

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
                {
                    text = DecodeBase64(part.Body.Data);
                }

                if (part.MimeType == "text/html" && part.Body?.Data != null)
                {
                    html = DecodeBase64(part.Body.Data);
                }

                if (part.Parts != null)
                {
                    foreach (var p in part.Parts)
                        Traverse(p);
                }
            }

            Traverse(message.Payload);

            // ★ text優先、無ければhtmlを整形
            if (!string.IsNullOrWhiteSpace(text))
            {
                return (NormalizeText(text), html);
            }
            else
            {
                return (ConvertHtmlToText(html), html);
            }
        }

        private string DecodeBase64(string input)
        {
            var bytes = Convert.FromBase64String(
                input.Replace('-', '+').Replace('_', '/'));

            return Encoding.UTF8.GetString(bytes);
        }
        private string ConvertHtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // script / style を削除
            var scripts = doc.DocumentNode.SelectNodes("//script|//style");
            if (scripts != null)
            {
                foreach (var node in scripts)
                    node.Remove();
            }

            // テキスト抽出
            var text = doc.DocumentNode.InnerText;

            // HTMLエンティティ変換 (&nbsp; → 空白など)
            text = WebUtility.HtmlDecode(text);

            // 改行整理
            text = NormalizeText(text);

            return text;
        }
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // 連続スペース削除
            text = System.Text.RegularExpressions.Regex.Replace(text, "[ \t]+", " ");

            // 3行以上の改行を2行に圧縮
            text = System.Text.RegularExpressions.Regex.Replace(text, "\n{3,}", "\n\n");

            return text.Trim();
        }
    }
}
