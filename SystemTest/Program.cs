using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace EliteDangerousCompanionAPI.SystemTest
{
    class Program
    {
        private static readonly Guid Win32FolderId_SavedGames = new("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");

        [DllImport("Shell32.dll")]
        private static extern uint SHGetKnownFolderPathW(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
            uint dwFlags,
            nint hToken,
            out nint pszPath  // API uses CoTaskMemAlloc
        );

        private static string GetJournalDirectory()
        {
            if (SHGetKnownFolderPathW(Win32FolderId_SavedGames, 0, nint.Zero, out nint pszPath) == 0)
            {
                string path = Marshal.PtrToStringUni(pszPath);
                Marshal.FreeCoTaskMem(pszPath);
                return Path.Combine(path, "Frontier Developments", "Elite Dangerous");
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        static void Main(string[] args)
        {
            var beta = false;
            var legacy = false;
            var journaldir = GetJournalDirectory();
            var journaldirs = new List<string> { journaldir };

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
                    journaldirs.Add(arg);
                }
            }

            var config =
                new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
                    .Build();

            var provider = new OAuth2Provider(config.GetSection("OAuth2").Get<OAuth2Settings>());

            var id2addr = new Dictionary<long, long>();

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

            var commanders = new Dictionary<string, Commander>();
            Commander cmdrdata = null;

            var logname = $"Profile_{DateTime.UtcNow:yyyyMMddHHmmss}.log";
            int journalpos = 0;
            byte[] data = new byte[65536];
            string lastjournalfile = null;
            var journalpat = beta ? "JournalBeta.*.log" : "Journal.*.log";
            var journalfilequeue = new Queue<string>();
            string journalfilename = null;
            string journalline = null;

            foreach (var dir in journaldirs)
            {
                foreach (var jfile in Directory.EnumerateFiles(dir, journalpat).OrderBy(e => e))
                {
                    journalfilequeue.Enqueue(jfile);
                }
            }

            using var outfile = File.OpenWrite(logname);
            using var writer = new StreamWriter(outfile);

            while (true)
            {
                cmdrdata?.RetryFetch();

                if (journalline == null || journalfilename == null)
                {
                    if (!journalfilequeue.TryDequeue(out journalfilename))
                    {
                        journalfilename = Directory.EnumerateFiles(journaldir, journalpat).OrderByDescending(e => e).FirstOrDefault();
                    }
                }

                if (journalfilename != lastjournalfile)
                {
                    journalpos = 0;
                }

                lastjournalfile = journalfilename;

                journalline = null;

                using (var jfile = File.Open(journalfilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    jfile.Seek(journalpos, SeekOrigin.Begin);
                    int len = jfile.Read(data, 0, data.Length);

                    if (len != 0)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            if (data[i] == '\n')
                            {
                                journalpos += i + 1;
                                journalline = Encoding.UTF8.GetString(data, 0, i);
                                break;
                            }
                        }
                    }
                }

                JObject jo = null;

                if (journalline != null)
                {
                    try
                    {
                        jo = JObject.Parse(journalline);
                    }
                    catch
                    {
                        Trace.WriteLine($"Unable to parse line {journalline}");
                    }
                }

                if (jo != null && jo["event"] != null)
                {
                    var jevent = jo.Value<string>("event");
                    switch (jevent)
                    {
                        case "Fileheader":
                            var gameversion = jo.Value<string>("gameversion");
                            if (gameversion?.StartsWith("3.8") == true)
                            {
                                legacy = true;
                            }
                            else
                            {
                                legacy = false;
                            }
                            break;
                        case "Commander":
                            var fid = jo.Value<string>("FID");

                            if (fid != null)
                            {
                                var commander = jo.Value<string>("Name");
                                commanders.TryGetValue(fid, out cmdrdata);

                                if (cmdrdata == null)
                                {
                                    var auth = provider.Load(fid);

                                    if (auth == null)
                                    {
                                        Console.WriteLine($"Please log in as {commander}");
                                        var req = provider.Authorize();
                                        Console.WriteLine(req.AuthURL);
                                        auth = req.GetAuth();
                                    }

                                    while (true)
                                    {
                                        try
                                        {
                                            var authdec = auth.Decode();
                                            var token = JObject.Parse(authdec);
                                            var tokenusr = token["usr"];
                                            var cid = tokenusr.Value<string>("customer_id");
                                            File.WriteAllText($"access-token-contents_F{cid}.json", authdec);

                                            if (fid != $"F{cid}")
                                            {
                                                Console.WriteLine("Wrong Account");
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        catch
                                        {
                                        }

                                        Console.WriteLine($"Please log in as {commander}");
                                        var req = provider.Authorize();
                                        Console.WriteLine(req.AuthURL);
                                        auth = req.GetAuth();
                                    }

                                    auth.Save(fid);

                                    cmdrdata = new Commander(fid, commander, new CAPI(auth, beta, legacy));
                                    cmdrdata.Init();
                                    commanders[fid] = cmdrdata;
                                }

                                writer.WriteLine(journalline.Trim('\r', '\n'));
                                Trace.WriteLine(journalline.Trim('\r', '\n'));
                            }
                            else
                            {
                                cmdrdata = null;
                            }
                            break;
                        case "FSDJump":
                        case "Location":
                        case "Docked":
                        case "CarrierJump":
                            if (cmdrdata != null)
                            {
                                writer.WriteLine(journalline.Trim('\r', '\n'));
                                Trace.WriteLine(journalline.Trim('\r', '\n'));
                                var starsystem = jo.Value<string>("StarSystem");
                                var systemaddress = jo.Value<long?>("SystemAddress");
                                var marketid = jo.Value<long?>("MarketID");
                                var stationname = jo.Value<string>("StationName");
                                var stationtype = jo.Value<string>("StationType");
                                var timestamp = jo.Value<DateTime>("timestamp");
                                var docked = cmdrdata.Docked;

                                if (jevent == "Docked" || (jevent == "Location" || jevent == "CarrierJump") && jo.Value<bool?>("Docked") == true)
                                {
                                    docked = true;
                                }
                                else if (jevent == "FSDJump" || jevent == "Location" && jo.Value<bool?>("Docked") != true)
                                {
                                    docked = false;
                                }

                                cmdrdata.Fetch(timestamp, docked, starsystem, systemaddress, stationname, marketid, stationtype);
                            }

                            break;
                    }

                    writer.Flush();
                }
                else if (journalfilequeue.Count == 0)
                {
                    foreach (var cmdr in commanders.Values)
                    {
                        foreach (var ejo in cmdr.CheckProfile())
                        {
                            writer.WriteLine(ejo.ToString(Newtonsoft.Json.Formatting.None));
                            Trace.WriteLine(ejo.ToString(Newtonsoft.Json.Formatting.None));
                        }
                    }

                    Thread.Sleep(2000);
                }
            }
        }
    }
}
