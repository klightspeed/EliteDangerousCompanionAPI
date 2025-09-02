using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EliteDangerousCompanionAPI
{
    public class OAuth2Request : IOAuth2Request, IDisposable
    {
        private string ClientID { get; }
        private string AppName { get; }
        private string CodeVerifier { get; } = GetBase64Random(32);
        private string State { get; } = GetBase64Random(8);
        public string AuthURL { get; private set; }

        private HttpListener Listener { get; }
        private ManualResetEventSlim Waithandle { get; } = new ManualResetEventSlim(false);
        private string RedirectURI { get; }
        private OAuth2 OAuth { get; set; }

        public OAuth2Request(string clientid, string appname)
        {
            AppName = appname;
            ClientID = clientid;

            try
            {
                Listener = CreateListener(out int port);
                CodeVerifier = GetBase64Random(32);
                State = GetBase64Random(8);
                string challenge = Base64URLEncode(SHA256(Encoding.ASCII.GetBytes(CodeVerifier)));
                RedirectURI = $"http://localhost:{port}/";
                AuthURL = OAuth2.AuthServerAuthURL +
                    "?scope=" + Uri.EscapeDataString(OAuth2.Scope) +
                    "&response_type=code" +
                    "&client_id=" + Uri.EscapeDataString(ClientID) +
                    "&code_challenge=" + Uri.EscapeDataString(challenge) +
                    "&code_challenge_method=S256" +
                    "&state=" + Uri.EscapeDataString(State) +
                    "&redirect_uri=" + Uri.EscapeDataString(RedirectURI);
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
            string postdata =
                "grant_type=authorization_code" +
                "&client_id=" + Uri.EscapeDataString(ClientID) +
                "&code_verifier=" + Uri.EscapeDataString(CodeVerifier) +
                "&code=" + Uri.EscapeDataString(code) +
                "&redirect_uri=" + RedirectURI;
            var httpreq = HttpWebRequest.Create(tokenurl);
            httpreq.Headers[HttpRequestHeader.UserAgent] = AppName;
            httpreq.Headers[HttpRequestHeader.Accept] = "application/json";
            httpreq.ContentType = "application/x-www-form-urlencoded";
            httpreq.Method = "POST";

            using (var stream = httpreq.GetRequestStream())
            {
                using (var textwriter = new StreamWriter(stream))
                {
                    textwriter.Write(postdata);
                }
            }

            JObject jo;

            using (var httpresp = httpreq.GetResponse())
            {
                using (var respstream = httpresp.GetResponseStream())
                {
                    using (var textreader = new StreamReader(respstream))
                    {
                        using (var jsonreader = new JsonTextReader(textreader))
                        {
                            jo = JObject.Load(jsonreader);
                        }
                    }
                }
            }

            var oauth = new OAuth2(ClientID, AppName, jo.Value<string>("access_token"), jo.Value<string>("refresh_token"), jo.Value<string>("token_type"));
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
        }

        private static HttpListener CreateListener(out int port)
        {
            HttpListener listener;
            HashSet<int> usedports = new HashSet<int>();
            Random rnd = new Random();

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
            var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(data);
        }
    }
}
