﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace EliteDangerousCompanionAPI
{
    public class OAuth2
    {
        private static readonly string ClientID = ConfigurationManager.AppSettings["ClientID"];
        private static readonly string AppName = ConfigurationManager.AppSettings["AppName"];
        private const string Scope = "capi";
        private const string AuthServerAuthURL = "https://auth.frontierstore.net/auth";
        private const string AuthServerTokenURL = "https://auth.frontierstore.net/token";
        private const string AuthServerDecodeURL = "https://auth.frontierstore.net/decode";

        private string AccessToken;
        private string RefreshToken;
        private string TokenType;

        public interface IOAuth2Request : IDisposable
        {
            string AuthURL { get; }
            OAuth2 GetAuth();
        }

        public class OAuth2Request : IOAuth2Request, IDisposable
        {
            private string CodeVerifier { get; } = GetBase64Random(32);
            private string State { get; } = GetBase64Random(8);
            public string AuthURL { get; private set; }

            private HttpListener Listener { get; }
            private ManualResetEventSlim Waithandle { get; } = new ManualResetEventSlim(false);
            private string RedirectURI { get; }
            private OAuth2 OAuth { get; set; }

            public OAuth2Request()
            {
                try
                {
                    Listener = CreateListener(out int port);
                    CodeVerifier = GetBase64Random(32);
                    State = GetBase64Random(8);
                    string challenge = Base64URLEncode(SHA256(Encoding.ASCII.GetBytes(CodeVerifier)));
                    RedirectURI = $"http://localhost:{port}/";
                    AuthURL = AuthServerAuthURL +
                        "?scope=" + Uri.EscapeDataString(Scope) +
                        "&audience=frontier" +
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

                string tokenurl = AuthServerTokenURL;
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

                var oauth = new OAuth2();
                oauth.AccessToken = jo.Value<string>("access_token");
                oauth.RefreshToken = jo.Value<string>("refresh_token");
                oauth.TokenType = jo.Value<string>("token_type");
                this.OAuth = oauth;

                var resp = ctx.Response;
                resp.StatusCode = 200;
                resp.StatusDescription = "OK";
                resp.ContentType = "text/plain";
                resp.OutputStream.Write(Encoding.ASCII.GetBytes("OK"));
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
        }

        private OAuth2()
        {
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

        public static IOAuth2Request Authorize()
        {
            return new OAuth2Request();
        }

        public static OAuth2 Load()
        {
            try
            {
                var jo = JObject.Parse(File.ReadAllText("access-token.json"));

                return new OAuth2
                {
                    AccessToken = jo.Value<string>("access_token"),
                    RefreshToken = jo.Value<string>("refresh_token"),
                    TokenType = jo.Value<string>("token_type")
                };
            }
            catch
            {
                return null;
            }
        }

        public void Save()
        {
            var jo = new JObject
            {
                ["access_token"] = AccessToken,
                ["refresh_token"] = RefreshToken,
                ["token_type"] = TokenType
            };

            File.WriteAllText("access-token.json", jo.ToString());
        }

        public bool Refresh()
        {
            try
            {
                string tokenurl = AuthServerTokenURL;
                string postdata =
                    "grant_type=refresh_token" +
                    "&client_id=" + Uri.EscapeDataString(ClientID) +
                    "&refresh_token=" + Uri.EscapeDataString(RefreshToken);

                var httpreq = WebRequest.CreateHttp(tokenurl);
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

                AccessToken = jo.Value<string>("access_token");
                RefreshToken = jo.Value<string>("refresh_token");
                TokenType = jo.Value<string>("token_type");

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public HttpWebRequest CreateRequest(string url)
        {
            var request = WebRequest.CreateHttp(url);
            request.Headers[HttpRequestHeader.Authorization] = TokenType + " " + AccessToken;
            request.Headers[HttpRequestHeader.UserAgent] = AppName;
            return request;
        }

        public T ExecuteGetRequest<T>(string url, Func<HttpWebResponse, T> respact, Action<HttpWebRequest> reqact = null)
        {
            HttpWebResponse resp = null;

            try
            {
                var req = CreateRequest(url);
                req.Method = "GET";
                reqact?.Invoke(req);
                resp = (HttpWebResponse)req.GetResponse();
                return respact(resp);
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse exresp)
                {
                    if (exresp.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Refresh();

                        var req = CreateRequest(url);
                        req.Method = "GET";
                        reqact?.Invoke(req);
                        resp = (HttpWebResponse)req.GetResponse();
                        return respact(resp);
                    }
                }

                throw;
            }
            finally
            {
                resp?.Dispose();
            }
        }

        public string Decode()
        {
            return ExecuteGetRequest(AuthServerDecodeURL, response =>
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            });
        }
    }
}
