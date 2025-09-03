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
        private const string LiveURLBase = "https://companion.orerve.net";
        private const string LegacyURLBase = "https://companion-legacy.orerve.net";
        private const string BetaURLBase = "https://companion-pts.orerve.net";
        private const string ProfileEndpoint = "/profile";
        private const string MarketEndpoint = "/market";
        private const string ShipyardEndpoint = "/shipyard";
        private const string CommunityGoalsEndpoint = "/communitygoals";
        private const string FleetCarrierEndpoint = "/communitygoals";
        private const string JournalEndpoint = "/journal";
        private const string VisitedStarsEndpoint = "/visitedstars";
        private const string SquadronEndpoint = "/squadron";

        public OAuth2 OAuth { get; private set; }
        public bool Beta { get; private set; }
        public bool Legacy { get; private set; }

        public CAPI(OAuth2 auth) : this(auth, false, false)
        {
        }

        public CAPI(OAuth2 auth, bool beta, bool legacy)
        {
            OAuth = auth;
            Beta = beta;
            Legacy = legacy;
        }

        private JObject Get(string endpoint)
        {
            string url = (Beta, Legacy) switch
            {
                (false, false) => LiveURLBase + endpoint,
                (false, true) => LegacyURLBase + endpoint,
                (true, _) => BetaURLBase + endpoint
            };

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

        private string GetString(string endpoint)
        {
            string url = (Beta, Legacy) switch
            {
                (false, false) => LiveURLBase + endpoint,
                (false, true) => LegacyURLBase + endpoint,
                (true, _) => BetaURLBase + endpoint
            };

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

        private byte[] GetBinary(string endpoint)
        {
            string url = (Beta, Legacy) switch
            {
                (false, false) => LiveURLBase + endpoint,
                (false, true) => LegacyURLBase + endpoint,
                (true, _) => BetaURLBase + endpoint
            };

            return OAuth.ExecuteGetRequest(url, response =>
            {
                if (response.StatusCode == (HttpStatusCode)102 /* HttpStatusCode.Processing */)
                {
                    return null;
                }

                using var memstream = new MemoryStream();
                using var stream = response.GetResponseStream();
                stream.CopyTo(memstream);
                return memstream.ToArray();
            });
        }

        public JObject GetProfile()
        {
            return Get(ProfileEndpoint);
        }

        public JObject GetMarket()
        {
            return Get(MarketEndpoint);
        }

        public JObject GetShipyard()
        {
            return Get(ShipyardEndpoint);
        }

        public JObject GetCommunityGoals()
        {
            return Get(CommunityGoalsEndpoint);
        }

        public JObject GetFleetCarrier()
        {
            return Get(FleetCarrierEndpoint);
        }

        public JObject GetSquadron()
        {
            return Get(SquadronEndpoint);
        }

        public byte[] GetVisitedStars()
        {
            return GetBinary(VisitedStarsEndpoint);
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
            return GetString(JournalEndpoint + "/" + date.ToString("yyyy/MM/dd")).Split('\n').Select(l => TryParseJson(l)).ToArray();
        }

        public string[] GetJournalRaw(DateTime date)
        {
            return GetString(JournalEndpoint + "/" + date.ToString("yyyy/MM/dd")).Split('\n');
        }
    }
}
