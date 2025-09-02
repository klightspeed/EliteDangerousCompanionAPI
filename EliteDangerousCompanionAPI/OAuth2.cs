using System;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private string AccessToken;
        private string RefreshToken;
        private string TokenType;

        public OAuth2(string clientid, string appname, string accessToken, string refreshToken, string tokenType)
        {
            ClientID = clientid;
            AppName = appname;
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
            var jo = new JObject
            {
                ["access_token"] = AccessToken,
                ["refresh_token"] = RefreshToken,
                ["token_type"] = TokenType
            };

            File.WriteAllText(cmdr == null ? "access-token.json" : $"access-token_{cmdr}.json", jo.ToString());
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
            HttpWebRequest req;

            try
            {
                req = CreateRequest(url);
                req.Method = "GET";
                reqact?.Invoke(req);
                resp = (HttpWebResponse)req.GetResponse();
                return respact(resp);
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse exresp && exresp.StatusCode == HttpStatusCode.Unauthorized)
            {
                using var memstream = new MemoryStream();

                using (var exstream = exresp.GetResponseStream())
                {
                    exstream.CopyTo(memstream);
                }

                var exdata = memstream.ToArray();
                var exstring = Encoding.UTF8.GetString(exdata);

                Refresh();

                req = CreateRequest(url);
                req.Method = "GET";
                reqact?.Invoke(req);
                resp = (HttpWebResponse)req.GetResponse();
                return respact(resp);
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
