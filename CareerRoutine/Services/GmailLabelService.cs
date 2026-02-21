using CareerRoutine.Models;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Label = Google.Apis.Gmail.v1.Data.Label;

namespace CareerRoutine.Services
{
    public class GmailLabelService
    {
        private const string LabelName = "CareerRoutine.Selected";

        // ===== ラベルIDを取得（なければ作成） =====
        public async Task<string> GetOrCreateLabelIdAsync()
        {
            var service = await GmailServiceFactory.CreateServiceAsync();

            var labelsResponse = await service.Users.Labels.List("me").ExecuteAsync();
            var existing = labelsResponse.Labels?
                .FirstOrDefault(l => l.Name == LabelName);

            if (existing != null)
                return existing.Id;

            // ラベルが存在しない場合は新規作成
            var newLabel = new Label
            {
                Name = LabelName,
                LabelListVisibility = "labelShow",
                MessageListVisibility = "show"
            };

            var created = await service.Users.Labels.Create(newLabel, "me").ExecuteAsync();
            return created.Id;
        }

        // ===== メールにラベルを付与 =====
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

        // ===== CareerRoutine.Selected が付いた最新メールを取得 =====
        // 戻り値: カーソルとなる Job（見つからなければ null）
        public async Task<Job?> GetCursorJobAsync()
        {
            var service = await GmailServiceFactory.CreateServiceAsync();
            var labelId = await GetOrCreateLabelIdAsync();

            var req = service.Users.Messages.List("me");
            req.LabelIds = new Google.Apis.Util.Repeatable<string>(
                new[] { labelId });
            req.MaxResults = 1;

            var resp = await req.ExecuteAsync();
            if (resp.Messages == null || !resp.Messages.Any())
                return null;

            var msg = await service.Users.Messages
                .Get("me", resp.Messages[0].Id)
                .ExecuteAsync();

            return new Job
            {
                MessageId = msg.Id,
                InternalDate = (long)msg.InternalDate!
            };
        }

        // ===== カーソルより後のメール件数を取得 =====
        public async Task<int> CountAfterCursorAsync(Job cursor)
        {
            var service = await GmailServiceFactory.CreateServiceAsync();

            // Gmail の after: は Unix 秒
            long unixSec = (cursor.InternalDate / 1000) + 1;

            var req = service.Users.Messages.List("me");
            req.Q = $"in:inbox after:{unixSec}";

            // 件数だけ知りたいので最小限取得
            req.MaxResults = 500;

            var resp = await req.ExecuteAsync();
            return resp.Messages?.Count ?? 0;
        }
    }
}
