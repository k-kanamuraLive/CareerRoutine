using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace CareerRoutine.Services
{
    public static class GmailServiceFactory
    {
        public static async Task<GmailService> CreateServiceAsync()
        {
            using var stream =
                new FileStream("credentials.json",
                    FileMode.Open, FileAccess.Read);

            var credPath = "token.json";

            var credential =
                await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { GmailService.Scope.GmailModify },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));

            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "CareerRoutine"
            });
        }
    }
}
