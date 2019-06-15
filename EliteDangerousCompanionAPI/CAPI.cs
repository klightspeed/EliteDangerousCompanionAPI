using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EliteDangerousCompanionAPI
{
    public class CAPI
    {
        private const string ProfileURL = "https://companion.orerve.net/profile";
        private const string MarketURL = "https://companion.orerve.net/market";
        private const string ShipyardURL = "https://companion.orerve.net/shipyard";
        private const string JournalURL = "https://companion.orerve.net/journal";


        public OAuth2 OAuth { get; private set; }

        public CAPI(OAuth2 auth)
        {
            OAuth = auth;
        }

        private JObject Get(string url)
        {
            var req = OAuth.CreateRequest(url);
            req.Method = "GET";
            using (var response = req.GetResponse())
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
            }
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
    }
}
