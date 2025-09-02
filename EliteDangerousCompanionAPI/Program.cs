using System;
using System.IO;
using System.Threading;

namespace EliteDangerousCompanionAPI
{
    class Program
    {
        private static void Main(string[] args)
        {
            var beta = false;
            var legacy = false;
            string name = null;

            foreach (var arg in args)
            {
                if (arg == "--beta")
                {
                    beta = true;
                    legacy = false;
                }
                else if (arg == "--legacy")
                {
                    legacy = true;
                    beta = false;
                }
                else if (arg == "--live")
                {
                    legacy = false;
                    beta = false;
                }
                else
                {
                    name = arg;
                }
            }


            OAuth2 auth = OAuth2.Load(name);

            if (auth == null || !auth.Refresh())
            {
                var req = OAuth2.Authorize();
                Console.WriteLine(req.AuthURL);
                auth = req.GetAuth();
            }

            auth.Save(name);

            var capi = new CAPI(auth, beta, legacy);

            string lastprofilestr = null;
            string lastmarketstr = null;
            string lastshipyardstr = null;

            while (true)
            {
                var profile = capi.GetProfile();
                var market = capi.GetMarket();
                var shipyard = capi.GetShipyard();
                var now = DateTime.UtcNow;

                var profilestr = profile.ToString(Newtonsoft.Json.Formatting.Indented);
                var marketstr = market.ToString(Newtonsoft.Json.Formatting.Indented);
                var shipyardstr = shipyard.ToString(Newtonsoft.Json.Formatting.Indented);

                if (profilestr != lastprofilestr)
                {
                    string outname = $"profile-{now:yyyy-MM-dd'T'HH-mm-ss}[{Uri.EscapeDataString(profile["lastStarport"]?.Value<string>("name") ?? "")}].json";
                    Console.WriteLine($"Writing {outname}");
                    File.WriteAllText(outname, profilestr);
                    lastprofilestr = profilestr;
                }

                if (marketstr != lastmarketstr)
                {
                    string outname = $"market-{now:yyyy-MM-dd'T'HH-mm-ss}[{Uri.EscapeDataString(market.Value<string>("name"))}].json";
                    Console.WriteLine($"Writing {outname}");
                    File.WriteAllText(outname, market.ToString(Newtonsoft.Json.Formatting.Indented));
                    lastmarketstr = marketstr;
                }

                if (shipyardstr != lastshipyardstr)
                {
                    string outname = $"shipyard-{now:yyyy-MM-dd'T'HH-mm-ss}[{Uri.EscapeDataString(shipyard.Value<string>("name"))}].json";
                    Console.WriteLine($"Writing {outname}");
                    File.WriteAllText(outname, shipyard.ToString(Newtonsoft.Json.Formatting.Indented));
                    lastshipyardstr = shipyardstr;
                }

                Thread.Sleep(30000);
            }
        }
    }
}
