using System;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using NLog;

namespace TVRename.TheTVDB
{
    internal class TokenProvider
    {
        [NotNull]
        // ReSharper disable once InconsistentNaming
        public static string TVDB_API_URL
        {
            get
            {
                return LocalCache.VERS switch
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    ApiVersion.v2 => "https://api.thetvdb.com",
                    // ReSharper disable once HeuristicUnreachableCode
                    // ReSharper disable once HeuristicUnreachableCode
                    ApiVersion.v3 => "https://api-dev.thetvdb.com",
                    // ReSharper disable once HeuristicUnreachableCode
                    _ => throw new NotSupportedException()
                };
            }
        }

        public static readonly string TVDB_API_KEY = "5FEC454623154441";

        private string lastKnownToken = string.Empty;
        private DateTime lastRefreshTime = DateTime.MinValue;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string GetToken()
        {
            //If we have not logged on at all then logon
            if (!IsTokenAcquired())
            {
                AcquireToken();
            }
            //If we have logged in but the token has expired so logon again
            if (!TokenIsValid())
            {
                AcquireToken();
            }
            //If we have logged on and have a valid token that is nearing its use-by date then refresh
            if (ShouldRefreshToken())
            {
                RefreshToken();
            }

            return lastKnownToken;
        }

        public void EnsureValid()
        {
            GetToken();
        }

        private void AcquireToken()
        {
            Logger.Info("Acquire a TheTVDB token... ");
            JObject request = new JObject(new JProperty("apikey", TVDB_API_KEY));
            JObject jsonResponse = HttpHelper.JsonHttpPostRequest($"{TVDB_API_URL}/login", request, true);

            string newToken = (string)jsonResponse["token"];
            if (newToken == null)
            {
                Logger.Error("Could not refresh Token");
                return;
            }
            UpdateToken(newToken);

            Logger.Info("Performed login at " + DateTime.UtcNow);
            Logger.Info("New Token " + lastKnownToken);
        }

        private void RefreshToken()
        {
            Logger.Info("Refreshing TheTVDB token... ");
            JObject jsonResponse = HttpHelper.JsonHttpGetRequest($"{TVDB_API_URL}/refresh_token", lastKnownToken);

            string newToken = (string)jsonResponse["token"];
            if (newToken == null)
            {
                Logger.Error("Could not refresh Token");
                return;
            }
            UpdateToken(newToken);

            Logger.Info("Refreshed token at " + DateTime.UtcNow);
            Logger.Info("New Token " + lastKnownToken);
        }

        private void UpdateToken(string token)
        {
            lastKnownToken = token;
            lastRefreshTime = DateTime.Now;
        }

        private bool ShouldRefreshToken() => DateTime.Now - lastRefreshTime >= TimeSpan.FromHours(23);

        private bool TokenIsValid() => DateTime.Now - lastRefreshTime < TimeSpan.FromDays(1) - TimeSpan.FromMinutes(1);

        private bool IsTokenAcquired() => lastKnownToken != string.Empty;
    }
}
