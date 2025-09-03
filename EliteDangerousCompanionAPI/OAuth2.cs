using System;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace EliteDangerousCompanionAPI
{
    public class OAuth2
    {
        public const string Scope = "capi auth";
        public const string AuthServerAuthURL = "https://auth.frontierstore.net/auth";
        public const string AuthServerTokenURL = "https://auth.frontierstore.net/token";
        public const string AuthServerDecodeURL = "https://auth.frontierstore.net/decode";

        private readonly string ClientID;
        private readonly string AppName;
        private readonly HttpClient HttpClient;
        private string AccessToken;
        private string RefreshToken;
        private string TokenType;

        public OAuth2(string clientid, string appname, HttpClient httpClient, string accessToken, string refreshToken, string tokenType)
        {
            ClientID = clientid;
            AppName = appname;
            HttpClient = httpClient;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            TokenType = tokenType;
        }

        public void Save()
        {
            Save(null);
        }

        public void Save(string cmdr)
        {
            File.WriteAllText(
                cmdr == null ? "access-token.json" : $"access-token_{cmdr}.json",
                JsonConvert.SerializeObject(new
                {
                    access_token = AccessToken,
                    refresh_token = RefreshToken,
                    token_type = TokenType
                })
            );
        }

        public bool Refresh()
        {
            try
            {
                string tokenurl = AuthServerTokenURL;

                using var reqmsg = new HttpRequestMessage(HttpMethod.Post, tokenurl);
                reqmsg.Headers.UserAgent.ParseAdd(AppName);
                reqmsg.Headers.Accept.ParseAdd("application/json");
                reqmsg.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = ClientID,
                    ["refresh_token"] = RefreshToken
                });

                using var respmsg = HttpClient.SendAsync(reqmsg).Result;
                respmsg.EnsureSuccessStatusCode();
                var resptext = respmsg.Content.ReadAsStringAsync().Result;

                var tokenresp = JsonConvert.DeserializeAnonymousType(
                    resptext,
                    new { access_token = "", refresh_token = "", token_type = "" }
                );

                AccessToken = tokenresp.access_token;
                RefreshToken = tokenresp.refresh_token;
                TokenType = tokenresp.token_type;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error refreshing access token: {ex}");
                return false;
            }
        }

        public void Authorize(HttpRequestMessage msg)
        {
            msg.Headers.Authorization = new AuthenticationHeaderValue(TokenType, AccessToken);
            msg.Headers.UserAgent.Clear();
            msg.Headers.UserAgent.ParseAdd(AppName);
        }

        public T ExecuteGetRequest<T>(string url, Func<HttpResponseMessage, T> respact, Action<HttpRequestMessage> reqact = null)
        {
            for (int retries = 1; ; retries--)
            {
                using var reqmsg = new HttpRequestMessage(HttpMethod.Get, url);
                reqact?.Invoke(reqmsg);
                Authorize(reqmsg);
                using var respmsg = HttpClient.SendAsync(reqmsg).Result;

                if (respmsg.StatusCode == HttpStatusCode.Unauthorized && retries > 0)
                {
                    Refresh();
                    continue;
                }

                respmsg.EnsureSuccessStatusCode();

                return respact(respmsg);
            }
        }

        public string Decode()
        {
            return ExecuteGetRequest(AuthServerDecodeURL, response =>
            {
                return response.Content.ReadAsStringAsync().Result;
            });
        }
    }
}
