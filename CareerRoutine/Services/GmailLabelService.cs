using CareerRoutine.Models;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Label = Google.Apis.Gmail.v1.Data.Label;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace CareerRoutine.Services
{
    public class GmailLabelService
    {
        private const string LabelName = "CareerRoutine.Selected";

        public async Task<string> GetOrCreateLabelIdAsync()
        {
            var service = await GmailServiceFactory.CreateServiceAsync();

            var labelsResponse = await service.Users.Labels.List("me").ExecuteAsync();
            var existing = labelsResponse.Labels?
                .FirstOrDefault(l => l.Name == LabelName);

            if (existing != null)
                return existing.Id;

            var newLabel = new Label
            {
                Name = LabelName,
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            };

            var created = await service.Users.Labels.Create(newLabel, "me").ExecuteAsync();
            return created.Id;
        }

        public async Task ApplyLabelAsync(string messageId)
        {
            var service = await GmailServiceFactory.CreateServiceAsync();
            var labelId = await GetOrCreateLabelIdAsync();

            var request = new ModifyMessageRequest
            {
                AddLabelIds = new List<string> { labelId }
            };

            await service.Users.Messages.Modify(request, "me", messageId).ExecuteAsync();
        }

        // 🔥 ラベル付き最新メール（internalDate最大）を取得
        public async Task<Job?> GetCursorJobAsync()
        {
            var service = await GmailServiceFactory.CreateServiceAsync();
            var labelId = await GetOrCreateLabelIdAsync();

            string? pageToken = null;
            Job? newest = null;

            do
            {
                var req = service.Users.Messages.List("me");
                req.LabelIds = new Google.Apis.Util.Repeatable<string>(
                    new[] { labelId });
                req.PageToken = pageToken;
                req.MaxResults = 500;

                var resp = await req.ExecuteAsync();

                if (resp.Messages == null)
                    break;

                foreach (var msg in resp.Messages)
                {
                    var full = await service.Users.Messages
                        .Get("me", msg.Id)
                        .ExecuteAsync();

                    var internalDate = (long)full.InternalDate!;

                    if (newest == null || internalDate > newest.InternalDate)
                    {
                        newest = new Job
                        {
                            MessageId = full.Id,
                            InternalDate = internalDate
                        };
                    }
                }

                pageToken = resp.NextPageToken;

            } while (pageToken != null);

            return newest;
        }

        // 🔥 完全正確な件数取得
        public async Task<int> CountAfterCursorAsync(
            Job cursor,
            IProgress<int>? progress = null)
        {
            var service = await GmailServiceFactory.CreateServiceAsync();
            long unixSec = cursor.InternalDate / 1000;

            int count = 0;
            int processed = 0;
            string? pageToken = null;
            var allMessages = new List<string>();

            // 全メッセージIDを収集
            do
            {
                var req = service.Users.Messages.List("me");
                req.Q = $"in:inbox after:{unixSec}";
                req.PageToken = pageToken;
                req.MaxResults = 500;
                var response = await req.ExecuteAsync();

                if (response.Messages == null)
                    break;

                allMessages.AddRange(response.Messages.Select(m => m.Id));
                pageToken = response.NextPageToken;
            } while (pageToken != null);

            int total = allMessages.Count;

            // 100件ずつBatchで InternalDate を取得
            foreach (var chunk in allMessages.Chunk(100))
            {
                var batch = new BatchRequest(service);

                foreach (var id in chunk)
                {
                    var getReq = service.Users.Messages.Get("me", id);
                    getReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Minimal;
                    getReq.Fields = "internalDate";

                    batch.Queue<Message>(getReq, (result, error, index, message) =>
                    {
                        if (result?.InternalDate > cursor.InternalDate)
                            Interlocked.Increment(ref count);

                        int percent = (int)((double)Interlocked.Increment(ref processed) / total * 100);
                        progress?.Report(Math.Min(percent, 100));
                    });
                }

                await batch.ExecuteAsync();
            }

            progress?.Report(0);
            return count;
        }
    }
}