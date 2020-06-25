using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Net;

namespace EliteDangerousCompanionAPI
{
    public class CAPI
    {
        private const string ProfileURL = "https://companion.orerve.net/profile";
        private const string MarketURL = "https://companion.orerve.net/market";
        private const string ShipyardURL = "https://companion.orerve.net/shipyard";
        private const string CommunityGoalsURL = "https://companion.orerve.net/communitygoals";
        private const string JournalURL = "https://companion.orerve.net/journal";

        public OAuth2 OAuth { get; private set; }
        public bool Beta { get; private set; }

        public CAPI(OAuth2 auth) : this(auth, false)
        {
        }

        public CAPI(OAuth2 auth, bool beta)
        {
            OAuth = auth;
            Beta = beta;
        }

        private JObject Get(string url)
        {
            if (Beta)
            {
                url = url.Replace("https://companion", "https://pts-companion");
            }

            return OAuth.ExecuteGetRequest(url, response =>
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var textreader = new StreamReader(stream, Encoding.UTF8))
                    {
                        using (var jsonreader = new JsonTextReader(textreader))
                        {
                            return JObject.Load(jsonreader);
                        }
                    }
                }
            });
        }

        private string GetString(string url)
        {
            return OAuth.ExecuteGetRequest(url, response =>
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var textreader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return textreader.ReadToEnd();
                    }
                }
            });
        }

        public JObject GetProfile()
        {
            return Get(ProfileURL);
        }

        public JObject GetMarket()
        {
            return Get(MarketURL);
        }

        public JObject GetShipyard()
        {
            return Get(ShipyardURL);
        }

        public JObject GetCommunityGoals()
        {
            return Get(CommunityGoalsURL);
        }

        private JObject TryParseJson(string data)
        {
            try
            {
                return JObject.Parse(data);
            }
            catch
            {
                return null;
            }
        }

        public JObject[] GetJournal(DateTime date)
        {
            return GetString(JournalURL + "/" + date.ToString("yyyy/MM/dd")).Split('\n').Select(l => TryParseJson(l)).ToArray();
        }
    }
}
