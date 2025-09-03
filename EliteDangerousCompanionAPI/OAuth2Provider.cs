using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace EliteDangerousCompanionAPI
{
    public class OAuth2Provider
    {
        private readonly string ClientID;
        private readonly string AppName;
        private readonly HttpClient HttpClient = new();

        public OAuth2Provider(string clientid, string appname)
        {
            ClientID = clientid;
            AppName = appname;
        }

        public OAuth2Provider(OAuth2Settings oauth2Settings)
        {
            ClientID = oauth2Settings.ClientID;
            AppName = oauth2Settings.AppName;
        }

        public IOAuth2Request Authorize()
        {
            return new OAuth2Request(ClientID, AppName, HttpClient);
        }

        public OAuth2 Load()
        {
            return Load(null);
        }

        public OAuth2 Load(string cmdr)
        {
            try
            {
                var tokendata = JsonConvert.DeserializeAnonymousType(
                    File.ReadAllText(cmdr == null ? "access-token.json" : $"access-token_{cmdr}.json"),
                    new { access_token = "", refresh_token = "", token_type = "" }
                );

                return new OAuth2(ClientID, AppName, HttpClient, tokendata.access_token, tokendata.refresh_token, tokendata.token_type);
            }
            catch
            {
                return null;
            }
        }
    }
}
