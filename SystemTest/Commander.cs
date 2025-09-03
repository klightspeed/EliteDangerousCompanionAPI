using EliteDangerousCompanionAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace EliteDangerousCompanionAPI.SystemTest
{
    public class CAPIData
    {
        private readonly Func<CAPI, JObject> Getter;
        private readonly string Name;

        public CAPIData(Func<CAPI, JObject> getter, string name)
        {
            Getter = getter;
            Name = name;
        }

        public JObject Data { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool FetchRetry { get; set; }
        public double FetchDelay { get; set; }
        public bool Process { get; set; }

        public void Init(CAPI capi)
        {
            if (capi != null && Data == null)
            {
                Trace.WriteLine($"Fetching {Name}");
                Data = Getter(capi);

                LastUpdate = DateTime.UtcNow;
            }
        }

        public void Fetch(CAPI capi, DateTime timestamp)
        {
            if (capi != null && timestamp > LastUpdate.AddSeconds(FetchDelay))
            {
                Trace.WriteLine($"Fetching {Name}");
                Data = Getter(capi);

                LastUpdate = timestamp;
            }

            Process = true;
        }

        public void RetryFetch(CAPI capi)
        {
            if (FetchRetry == true && capi != null && DateTime.UtcNow > LastUpdate.AddSeconds(FetchDelay))
            {
                Trace.WriteLine($"Retrying {Name} fetch");
                Data = Getter(capi);

                LastUpdate = DateTime.UtcNow;
                Process = true;
            }
        }
    }

    public class Commander
    {
        private static readonly Dictionary<long, long> id2addr = new Dictionary<long, long>();

        static Commander()
        {
            if (File.Exists("edsystems-id2addr.jsonl"))
            {
                foreach (var line in File.ReadAllLines("edsystems-id2addr.jsonl"))
                {
                    var jo = JObject.Parse(line);
                    var id = jo.Value<long>("id");
                    var sysaddr = jo.Value<long>("systemAddress");
                    id2addr[id] = sysaddr;
                }
            }
        }

        public Commander(string fid, string cmdrname, CAPI capi)
        {
            FrontierID = fid;
            CommanderName = cmdrname;
            CAPI = capi;
        }

        public string FrontierID { get; }
        public string CommanderName { get; }
        public CAPI CAPI { get; }
        public CAPIData Profile { get; } = new CAPIData(c => c.GetProfile(), "profile");
        public CAPIData Market { get; } = new CAPIData(c => c.GetMarket(), "market");
        public CAPIData Shipyard { get; } = new CAPIData(c => c.GetShipyard(), "shipyard");
        public CAPIData FleetCarrier { get; } = new CAPIData(c => c.GetFleetCarrier(), "fleetcarrier") { FetchDelay = 60 };
        public CAPIData CommunityGoals { get; } = new CAPIData(c => c.GetCommunityGoals(), "communitygoals") { FetchDelay = 60 };
        public DateTime LastUpdate { get; private set; }
        public bool Docked { get; private set; }
        public long? SystemAddress { get; private set; }
        public string StarSystem { get; private set; }
        public long? MarketId { get; private set; }
        public string StationName { get; private set; }
        public string StationType { get; private set; }


        public void Init()
        {
            Profile.Init(CAPI);
            Market.Init(CAPI);
            Shipyard.Init(CAPI);
            FleetCarrier.Init(CAPI);
            CommunityGoals.Init(CAPI);
        }

        public void Fetch(DateTime timestamp, bool docked, string starsystem, long? systemaddress, string stationname, long? marketid, string stationtype)
        {
            Profile.Fetch(CAPI, timestamp);
            File.WriteAllText($"profile_{FrontierID}.json", Profile.Data.ToString());

            if (docked)
            {
                Market.Fetch(CAPI, timestamp);
                File.WriteAllText($"market_{FrontierID}.json", Market.Data.ToString());
                Shipyard.Fetch(CAPI, timestamp);
                File.WriteAllText($"shipyard_{FrontierID}.json", Shipyard.Data.ToString());
            }

            if (timestamp > LastUpdate)
            {
                Docked = docked;
                StarSystem = starsystem;
                SystemAddress = systemaddress;
                StationName = stationname;
                MarketId = marketid;
                StationType = stationtype;
                LastUpdate = timestamp;
            }
        }

        public void RetryFetch()
        {
            Profile.RetryFetch(CAPI);
            Market.RetryFetch(CAPI);
            Shipyard.RetryFetch(CAPI);
        }

        public IEnumerable<JToken> CheckProfile()
        {
            if (Profile.Process && Profile.Data != null && SystemAddress != null)
            {
                Profile.Process = false;

                var pcommander = Profile.Data["commander"];
                var lastSystem = Profile.Data["lastSystem"];
                var sysid = lastSystem.Value<long>("id");
                var lastsysaddr = lastSystem.Value<long?>("systemaddress");
                var ship = Profile.Data["ship"];
                var ships = Profile.Data["ships"] as JObject;
                var station = ship["station"];
                var mktid = station.Value<long>("id");

                if (ship != null)
                {
                    var shipSystem = ship["system"];

                    if (shipSystem?.Value<long?>("id") is long shipSysId
                        && shipSystem?.Value<long?>("systemaddress") is long shipSysaddr
                        && id2addr.ContainsKey(shipSysId))
                    {
                        id2addr[shipSysId] = shipSysaddr;
                        File.AppendAllLines("edsystems-id2addr.jsonl", new[] { JsonConvert.SerializeObject(new { id = shipSysId, systemAddress = shipSysaddr }, Formatting.None) });
                    }
                }

                if (ships != null)
                {
                    foreach (var ent in ships.Properties())
                    {
                        var shipSystem = ent["system"];

                        if (shipSystem?.Value<long?>("id") is long shipSysId
                            && shipSystem?.Value<long?>("systemaddress") is long shipSysaddr
                            && !id2addr.ContainsKey(shipSysId))
                        {
                            id2addr[shipSysId] = shipSysaddr;
                            File.AppendAllLines("edsystems-id2addr.jsonl", new[] { JsonConvert.SerializeObject(new { id = shipSysId, systemAddress = shipSysaddr }, Formatting.None) });
                        }
                    }
                }

                if (lastsysaddr is long sysaddr)
                {
                    sysid = sysaddr;
                }
                else if (id2addr.TryGetValue(sysid, out sysaddr))
                {
                    sysid = sysaddr;
                }
                else if (sysid < 155000 && SystemAddress != null && lastSystem.Value<string>("name") == StarSystem)
                {
                    sysid = (long)SystemAddress;
                }

                var ejo = Profile.Data.DeepClone();
                ejo["event"] = "CAPIProfile";
                ejo["timestamp"] = Profile.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                yield return ejo;

                if (sysid != SystemAddress || MarketId != null && mktid != MarketId || Docked != pcommander.Value<bool>("docked"))
                {
                    Profile.FetchRetry = true;
                    Profile.FetchDelay *= 2;

                    ejo = JObject.FromObject(new
                    {
                        @event = "ProfileMismatch",
                        timestamp = Profile.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                        SystemAddress,
                        StarSystem,
                        MarketId,
                        StationName,
                        Docked,
                        ProfileSystemId = lastSystem.Value<long>("id"),
                        ProfileStarSystem = lastSystem.Value<string>("name"),
                        ProfileMarketId = station.Value<long>("id"),
                        ProfileStationName = station.Value<string>("name")
                    });
                    yield return ejo;
                }
                else
                {
                    Profile.FetchDelay = 5;
                    var psystemaddress = sysid;
                    var pstarsystem = lastSystem.Value<string>("name");
                    var pmarketid = mktid;
                    var pstationname = station.Value<string>("name");

                    if (pstarsystem != StarSystem)
                    {
                        ejo = JObject.FromObject(new
                        {
                            @event = "SystemNameMismatch",
                            SystemId = lastSystem.Value<long>("id"),
                            SystemAddress,
                            StarSystem,
                            CAPISystemAddress = sysid,
                            CAPIStarSystem = pstarsystem
                        });
                        yield return ejo;
                    }

                    if (StationName != null && pstationname != StationName)
                    {
                        ejo = JObject.FromObject(new
                        {
                            @event = "StationNameMismatch",
                            SystemAddress,
                            StarSystem,
                            MarketId,
                            StationName,
                            CAPIMarketId = mktid,
                            CAPIStationName = pstationname
                        });
                        yield return ejo;
                    }

                    Profile.FetchRetry = false;
                }
            }

            if (Market.Process && Market.Data != null && Docked == true)
            {
                Market.Process = false;
                var mktid = Market.Data.Value<long>("id");
                var stationname = Market.Data.Value<string>("name");

                var ejo = Market.Data.DeepClone();
                ejo["event"] = "CAPIMarket";
                ejo["timestamp"] = Market.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                yield return ejo;

                if (MarketId != null && mktid != MarketId)
                {
                    Market.FetchRetry = true;
                    Market.FetchDelay *= 2;

                    ejo = JObject.FromObject(new
                    {
                        @event = "MarketMismatch",
                        timestamp = Market.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                        SystemAddress,
                        StarSystem,
                        MarketId,
                        StationName,
                        Docked,
                        CAPIMarketId = mktid,
                        CAPIStationName = stationname
                    });
                    yield return ejo;
                }
                else
                {
                    if (StationName != null && stationname != StationName)
                    {
                        ejo = JObject.FromObject(new
                        {
                            @event = "StationNameMismatch",
                            timestamp = Market.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                            SystemAddress,
                            StarSystem,
                            MarketId,
                            StationName,
                            CAPIMarketId = mktid,
                            CAPIStationName = stationname
                        });
                        yield return ejo;
                    }

                    Market.FetchRetry = false;
                }
            }

            if (Shipyard.Process && Shipyard.Data != null && Docked == true)
            {
                Shipyard.Process = false;
                var mktid = Shipyard.Data.Value<long>("id");
                var stationname = Shipyard.Data.Value<string>("name");

                var ejo = Shipyard.Data.DeepClone();
                ejo["event"] = "CAPIShipyard";
                ejo["timestamp"] = Shipyard.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                yield return ejo;

                if (MarketId != null && mktid != MarketId)
                {
                    Shipyard.FetchRetry = true;
                    Shipyard.FetchDelay *= 2;

                    ejo = JObject.FromObject(new
                    {
                        @event = "ShipyardMismatch",
                        timestamp = Shipyard.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                        SystemAddress,
                        StarSystem,
                        MarketId,
                        StationName,
                        Docked,
                        CAPIMarketId = mktid,
                        CAPIStationName = stationname
                    });
                    yield return ejo;
                }
                else
                {
                    if (StationName != null && stationname != StationName)
                    {
                        ejo = JObject.FromObject(new
                        {
                            @event = "StationNameMismatch",
                            timestamp = Shipyard.LastUpdate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
                            SystemAddress,
                            StarSystem,
                            MarketId,
                            StationName,
                            CAPIMarketId = mktid,
                            CAPIStationName = stationname
                        });
                        yield return ejo;
                    }

                    Shipyard.FetchRetry = false;
                }
            }
        }
    }
}
