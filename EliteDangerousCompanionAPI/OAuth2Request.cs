using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Http;

namespace EliteDangerousCompanionAPI
{
    public class OAuth2Request : IOAuth2Request, IDisposable
    {
        private string ClientID { get; }
        private string AppName { get; }
        private string CodeVerifier { get; } = GetBase64Random(32);
        private string State { get; } = GetBase64Random(8);
        public string AuthURL { get; private set; }

        private HttpClient HttpClient { get; }
        private HttpListener Listener { get; }
        private ManualResetEventSlim Waithandle { get; } = new ManualResetEventSlim(false);
        private string RedirectURI { get; }
        private OAuth2 OAuth { get; set; }

        public OAuth2Request(string clientid, string appname, HttpClient httpClient)
        {
            AppName = appname;
            ClientID = clientid;
            HttpClient = httpClient;

            try
            {
                Listener = CreateListener(out int port);
                CodeVerifier = GetBase64Random(32);
                State = GetBase64Random(8);
                string challenge = Base64URLEncode(SHA256(Encoding.ASCII.GetBytes(CodeVerifier)));

                var query = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["scope"] = OAuth2.Scope,
                    ["response_type"] = "code",
                    ["client_id"] = ClientID,
                    ["code_challenge"] = challenge,
                    ["code_challenge_method"] = "S256",
                    ["state"] = State,
                    ["redirect_uri"] = RedirectURI
                });

                var uribuilder = new UriBuilder(OAuth2.AuthServerAuthURL)
                {
                    Query = query.ReadAsStringAsync().Result
                };

                AuthURL = uribuilder.Uri.ToString();

                Listener.BeginGetContext(EndGetContext, null);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = AuthURL,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch
            {
                Listener.Stop();
            }
        }

        private void EndGetContext(IAsyncResult target)
        {
            var ctx = Listener.EndGetContext(target);
            var req = ctx.Request;
            var code = req.QueryString["code"];

            string tokenurl = OAuth2.AuthServerTokenURL;

            using var reqmsg = new HttpRequestMessage(HttpMethod.Post, tokenurl);
            reqmsg.Headers.UserAgent.ParseAdd(AppName);
            reqmsg.Headers.Accept.ParseAdd("application/json");
            reqmsg.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = ClientID,
                ["code_verifier"] = CodeVerifier,
                ["code"] = code,
                ["redirect_uri"] = RedirectURI
            });

            using var respmsg = HttpClient.SendAsync(reqmsg).Result;
            respmsg.EnsureSuccessStatusCode();
            var resptext = respmsg.Content.ReadAsStringAsync().Result;
            
            var tokenresp = JsonConvert.DeserializeAnonymousType(
                resptext,
                new { access_token = "", refresh_token = "", token_type = "" }
            );

            var oauth = new OAuth2(ClientID, AppName, HttpClient, tokenresp.access_token, tokenresp.refresh_token, tokenresp.token_type);
            this.OAuth = oauth;

            var resp = ctx.Response;
            resp.StatusCode = 200;
            resp.StatusDescription = "OK";
            resp.ContentType = "text/plain";
            resp.OutputStream.Write(Encoding.ASCII.GetBytes("OK"), 0, 2);
            resp.Close();
            Waithandle.Set();
        }

        public OAuth2 GetAuth()
        {
            Waithandle.Wait();
            return OAuth;
        }

        public void Dispose()
        {
            Listener.Stop();
            GC.SuppressFinalize(this);
        }

        private static HttpListener CreateListener(out int port)
        {
            HttpListener listener;
            var usedports = new HashSet<int>();
            var rnd = new Random();

            while (true)
            {
                port = rnd.Next(49152, 65534);

                if (usedports.Contains(port))
                {
                    continue;
                }

                listener = new HttpListener();

                try
                {
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Start();
                    return listener;
                }
                catch
                {
                    listener.Stop();
                    ((IDisposable)listener).Dispose();
                    usedports.Add(port);
                }
            }
        }

        private static string Base64URLEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string GetBase64Random(int len)
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[len];
            rng.GetBytes(bytes);
            return Base64URLEncode(bytes);
        }

        private static byte[] SHA256(byte[] data)
        {
#if NETCOREAPP2_2_OR_GREATER
            return System.Security.Cryptography.SHA256.HashData(data);
#else
            var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(data);
#endif
        }
    }
}
