using Newtonsoft.Json.Linq;
using System.IO;

namespace EliteDangerousCompanionAPI
{
    public class OAuth2Provider
    {
        private readonly string ClientID;
        private readonly string AppName;

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
            return new OAuth2Request(ClientID, AppName);
        }

        public OAuth2 Load()
        {
            return Load(null);
        }

        public OAuth2 Load(string cmdr)
        {
            try
            {
                var jo = JObject.Parse(File.ReadAllText(cmdr == null ? "access-token.json" : $"access-token_{cmdr}.json"));

                return new OAuth2(ClientID, AppName, jo.Value<string>("access_token"), jo.Value<string>("refresh_token"), jo.Value<string>("token_type"));
            }
            catch
            {
                return null;
            }
        }
    }
}
