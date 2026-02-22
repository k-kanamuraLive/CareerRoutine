using CareerRoutine.Models;
using Google.Apis.Gmail.v1.Data;
using System.Net;
using System.Text;
using Message = Google.Apis.Gmail.v1.Data.Message;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using Google.Apis.Gmail.v1;
using Google.Apis.Requests;
using System.Collections.Concurrent;

namespace CareerRoutine.Services
{
    public class GmailFetcher
    {
        public async Task<List<Job>> FetchAsync(
            IProgress<int>? progress = null,
            Job? cursor = null)
        {
            var service = await GmailServiceFactory.CreateServiceAsync();
            long unixSec = cursor != null ? cursor.InternalDate / 1000 : 0;

            // 全メッセージIDを収集
            var allMessageIds = new List<string>();
            string? pageToken = null;

            do
            {
                var req = service.Users.Messages.List("me");
                req.Q = cursor != null ? $"in:inbox after:{unixSec}" : "in:inbox";
                req.PageToken = pageToken;
                req.MaxResults = 500;
                var response = await req.ExecuteAsync();

                if (response.Messages == null)
                    break;

                allMessageIds.AddRange(response.Messages.Select(m => m.Id));
                pageToken = response.NextPageToken;
            } while (pageToken != null);

            int total = allMessageIds.Count;
            int processed = 0;
            var jobs = new ConcurrentBag<Job>();

            // 100件ずつBatch
            foreach (var chunk in allMessageIds.Chunk(100))
            {
                var batch = new BatchRequest(service);

                foreach (var id in chunk)
                {
                    var getReq = service.Users.Messages.Get("me", id);
                    getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

                    batch.Queue<Message>(getReq, (message, error, index, httpResponse) =>
                    {
                        if (error != null || message == null) return;

                        long internalDate = (long)message.InternalDate!;

                        // ミリ秒精度フィルタ
                        if (cursor != null && internalDate <= cursor.InternalDate)
                            return;

                        var (text, html) = ExtractBody(message);
                        jobs.Add(new Job
                        {
                            MessageId = message.Id,
                            Title = GetHeader(message, "Subject"),
                            BodyText = text,
                            BodyHtml = html,
                            Sender = GetHeader(message, "From"),
                            InternalDate = internalDate
                        });

                        int percent = (int)((double)Interlocked.Increment(ref processed) / total * 100);
                        progress?.Report(Math.Min(percent, 100));
                    });
                }

                await batch.ExecuteAsync();
            }

            progress?.Report(100);

            return jobs
                .OrderBy(j => j.InternalDate)
                .ToList();
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