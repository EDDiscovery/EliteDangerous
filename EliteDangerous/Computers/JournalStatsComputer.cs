﻿/*
 * Copyright © 2022-2022 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using EliteDangerousCore.JournalEvents;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EliteDangerousCore
{
    public class JournalStats
    {
        public struct JumpInfo
        {
            public string bodyname;
            public DateTime utc;
            public int boostvalue;
            public double jumpdist;
            public string shipid;
        };

        public class JetConeBoostInfo
        {
            public DateTime utc;
        }
        public class ShipInfo
        {
            public string name;
            public string ident;
            public int died;
        }

        public JournalLocOrJump MostNorth;
        public JournalLocOrJump MostSouth;
        public JournalLocOrJump MostEast;
        public JournalLocOrJump MostWest;
        public JournalLocOrJump MostUp;
        public JournalLocOrJump MostDown;

        public DateTime lastdockedutc;

        public string currentshipid;

        public JournalEvents.JournalStatistics laststats;

        public int fsdcarrierjumps;

        public List<JumpInfo> FSDJumps = new List<JumpInfo>();
        public Dictionary<string, JournalScan> Scans = new Dictionary<string, JournalScan>();
        public List<JournalScanOrganic> OrganicScans = new List<JournalScanOrganic>();
        public Dictionary<string, JournalSAAScanComplete> SAAScanComplete = new Dictionary<string, JournalSAAScanComplete>();
        public List<JetConeBoostInfo> JetConeBoost = new List<JetConeBoostInfo>();
        public Dictionary<string, ShipInfo> Ships = new Dictionary<string, ShipInfo>();
        public List<JournalPVPKill> PVPKills = new List<JournalPVPKill>();
        public List<JournalBounty> Bounties = new List<JournalBounty>();
        public List<JournalCommitCrime> Crimes = new List<JournalCommitCrime>();
        public List<JournalFactionKillBond> FactionKillBonds = new List<JournalFactionKillBond>();
        public List<JournalInterdiction> Interdiction = new List<JournalInterdiction>();
        public List<JournalInterdicted> Interdicted = new List<JournalInterdicted>();

        public JournalStats()
        {
        }

        public void Process(JournalEntry ev)
        {
            //   System.Diagnostics.Debug.WriteLine($"Stats JE {ev.EventTimeUTC} {ev.EventTypeStr}");

            switch (ev.EventTypeID)
            {
                case JournalTypeEnum.FSDJump:
                case JournalTypeEnum.Location:
                case JournalTypeEnum.CarrierJump:
                    {
                        JournalLocOrJump jl = ev as JournalLocOrJump;

                        //System.Diagnostics.Debug.WriteLine("NS System {0} {1}", jl.EventTimeUTC, jl.StarSystem);

                        if (jl.HasCoordinate)
                        {
                            if (this.MostNorth == null || this.MostNorth.StarPos.Z < jl.StarPos.Z)
                                this.MostNorth = jl;
                            if (this.MostSouth == null || this.MostSouth.StarPos.Z > jl.StarPos.Z)
                                this.MostSouth = jl;
                            if (this.MostEast == null || this.MostEast.StarPos.X < jl.StarPos.X)
                                this.MostEast = jl;
                            if (this.MostWest == null || this.MostWest.StarPos.X > jl.StarPos.X)
                                this.MostWest = jl;
                            if (this.MostUp == null || this.MostUp.StarPos.Y < jl.StarPos.Y)
                                this.MostUp = jl;
                            if (this.MostDown == null || this.MostDown.StarPos.Y > jl.StarPos.Y)
                                this.MostDown = jl;
                        }

                        JournalFSDJump fsd = ev as JournalFSDJump;
                        if (fsd != null)
                        {
                            this.fsdcarrierjumps++;
                            this.FSDJumps.Add(new JournalStats.JumpInfo() { utc = fsd.EventTimeUTC, jumpdist = fsd.JumpDist, boostvalue = fsd.BoostUsed, shipid = this.currentshipid, bodyname = fsd.StarSystem });
                        }
                        else if (ev.EventTypeID == JournalTypeEnum.CarrierJump)
                        {
                            this.fsdcarrierjumps++;
                        }

                        break;
                    }

                case JournalTypeEnum.Scan:
                    {
                        JournalScan sc = ev as JournalScan;
                        sc.ShipIDForStatsOnly = currentshipid;
                        this.Scans[sc.BodyName.ToLowerInvariant()] = sc;
                        break;
                    }

                case JournalTypeEnum.ScanOrganic:
                    {
                        JournalScanOrganic sc = ev as JournalScanOrganic;
                        if (sc.ScanType == JournalScanOrganic.ScanTypeEnum.Analyse)
                            this.OrganicScans.Add(sc);
                        break;
                    }

                case JournalTypeEnum.SAAScanComplete:
                    {
                        JournalSAAScanComplete sc = ev as JournalSAAScanComplete;
                        this.SAAScanComplete[sc.BodyName.ToLowerInvariant()] = sc;
                        break;
                    }

                case JournalTypeEnum.JetConeBoost:
                    {
                        this.JetConeBoost.Add(new JournalStats.JetConeBoostInfo() { utc = ev.EventTimeUTC });
                        break;
                    }

                case JournalTypeEnum.Docked:
                    {
                        this.lastdockedutc = ev.EventTimeUTC;
                        break;
                    }

                case JournalTypeEnum.ShipyardSwap:
                    {
                        var j = ev as JournalShipyardSwap;
                        this.currentshipid = j.ShipType + ":" + j.ShipId.ToStringInvariant();
                        break;
                    }
                case JournalTypeEnum.ShipyardNew:
                    {
                        var j = ev as JournalShipyardNew;
                        this.currentshipid = j.ShipType + ":" + j.ShipId.ToStringInvariant();
                        break;
                    }
                case JournalTypeEnum.LoadGame:
                    {
                        var j = ev as JournalLoadGame;
                        this.currentshipid = j.Ship + ":" + j.ShipId.ToStringInvariant();
                        // System.Diagnostics.Debug.WriteLine("Stats Loadgame ship details {0} {1} {2} {3}", j.EventTimeUTC, j.ShipFD, j.ShipName, j.ShipIdent);

                        if (!this.Ships.TryGetValue(this.currentshipid, out var cls))
                            cls = new JournalStats.ShipInfo();
                        cls.ident = j.ShipIdent;
                        cls.name = j.ShipName;
                        System.Diagnostics.Debug.Assert(this.currentshipid != null);
                        this.Ships[this.currentshipid] = cls;
                        break;
                    }
                case JournalTypeEnum.Loadout:
                    {
                        var j = ev as JournalLoadout;
                        this.currentshipid = j.Ship + ":" + j.ShipId.ToStringInvariant();
                        //System.Diagnostics.Debug.WriteLine("Stats loadout ship details {0} {1} {2} {3} now {4}", j.EventTimeUTC, j.ShipFD, j.ShipName, j.ShipIdent, this.currentshipid);
                        if (!this.Ships.TryGetValue(this.currentshipid, out var cls))
                            cls = new JournalStats.ShipInfo();
                        cls.ident = j.ShipIdent;
                        cls.name = j.ShipName;
                        System.Diagnostics.Debug.Assert(this.currentshipid != null);
                        this.Ships[this.currentshipid] = cls;

                        break;
                    }
                case JournalTypeEnum.SetUserShipName:
                    {
                        var j = ev as JournalSetUserShipName;
                        this.currentshipid = j.Ship + ":" + j.ShipID.ToStringInvariant();
                        if (!this.Ships.TryGetValue(this.currentshipid, out var cls))
                            cls = new JournalStats.ShipInfo();
                        cls.ident = j.ShipIdent;
                        cls.name = j.ShipName;
                        System.Diagnostics.Debug.Assert(this.currentshipid != null);
                        this.Ships[this.currentshipid] = cls;
                        break;
                    }
                case JournalTypeEnum.Died:
                    {
                        if (this.currentshipid.HasChars())
                        {
                            var j = ev as JournalDied;
                            if (!this.Ships.TryGetValue(this.currentshipid, out var cls))
                                cls = new JournalStats.ShipInfo();
                            cls.died++;
                            //System.Diagnostics.Debug.WriteLine("Died {0} {1}", this.currentshipid, cls.died);
                            System.Diagnostics.Debug.Assert(this.currentshipid != null);
                            this.Ships[this.currentshipid] = cls;
                        }
                        break;
                    }
                case JournalTypeEnum.Statistics:
                    {
                        this.laststats = ev as JournalEvents.JournalStatistics;
                        break;
                    }

                case JournalTypeEnum.PVPKill:
                    {
                        var j = ev as JournalEvents.JournalPVPKill;
                        this.PVPKills.Add(j);
                        break;
                    }
                case JournalTypeEnum.Bounty:
                    {
                        var j = ev as JournalEvents.JournalBounty;
                        this.Bounties.Add(j);
                        break;
                    }

                case JournalTypeEnum.CommitCrime:
                    {
                        var j = ev as JournalEvents.JournalCommitCrime;
                        this.Crimes.Add(j);
                        break;
                    }
                case JournalTypeEnum.FactionKillBond:
                    {
                        var j = ev as JournalEvents.JournalFactionKillBond;
                        this.FactionKillBonds.Add(j);
                        break;
                    }
                case JournalTypeEnum.Interdicted:
                    {
                        var j = ev as JournalEvents.JournalInterdicted;
                        this.Interdicted.Add(j);
                        break;
                    }
                case JournalTypeEnum.Interdiction:
                    {
                        var j = ev as JournalEvents.JournalInterdiction;
                        this.Interdiction.Add(j);
                        break;
                    }
            }

        }
    }

    public class JournalStatisticsComputer
    {
        public bool Running { get; set; }
        public void Start(int pcmdrid, DateTime? pstartutc, DateTime? pendutc, Action<JournalStats> pcallback)
        {
            cmdrid = pcmdrid;
            start = pstartutc;
            end = pendutc;
            callback = pcallback;

            Running = true;

            StatsThread = new System.Threading.Thread(new System.Threading.ThreadStart(StatisticsThread));
            StatsThread.Name = "Stats";
            StatsThread.IsBackground = true;
            StatsThread.Start();
        }

        public void Stop()
        {
            if ( StatsThread != null && StatsThread.IsAlive)
            {
                Exit = true;
                StatsThread.Join();
            }

            Exit = Running = false;
            StatsThread = null;
        }

        private static JournalTypeEnum[] events = new JournalTypeEnum[]     // 
        {
                JournalTypeEnum.FSDJump, JournalTypeEnum.CarrierJump, JournalTypeEnum.Location, JournalTypeEnum.Docked, JournalTypeEnum.JetConeBoost,
                JournalTypeEnum.Scan, JournalTypeEnum.SAAScanComplete, JournalTypeEnum.Docked,
                JournalTypeEnum.ShipyardNew, JournalTypeEnum.ShipyardSwap, JournalTypeEnum.LoadGame,
                JournalTypeEnum.Statistics, JournalTypeEnum.SetUserShipName, JournalTypeEnum.Loadout, JournalTypeEnum.Died, JournalTypeEnum.ScanOrganic,
                JournalTypeEnum.PVPKill, JournalTypeEnum.Bounty, JournalTypeEnum.CommitCrime, JournalTypeEnum.FactionKillBond,
                JournalTypeEnum.Interdiction, JournalTypeEnum.Interdicted,
        };

        static public bool IsJournalEntryForStats(JournalEntry e)
        {
            return Array.FindIndex(events, x => e.EventTypeID == x) >= 0;
        }

        private void StatisticsThread()
        {
            System.Diagnostics.Debug.WriteLine($"{BaseUtils.AppTicks.TickCountLap("JSC", true)} Stats table read start");

            var jlist = JournalEntry.GetAll(cmdrid, ids: events, startdateutc: start, enddateutc: end, cancelRequested:()=> { return Exit; });

            System.Diagnostics.Debug.WriteLine($"{BaseUtils.AppTicks.TickCountLap("JSC")} Stats table read end");

            JournalStats stats = new JournalStats();

            foreach (var e in jlist)        // fire through stats
            {
                stats.Process(e);
                if (Exit)
                {
                    Running = false;
                    return;
                }
            }

            foreach ( var saa in stats.SAAScanComplete )     // go thru all SAA scan completes, and make sure the scans have the flags set probably
            {
                if ( stats.Scans.TryGetValue(saa.Value.BodyName.ToLowerInvariant(), out JournalScan scan))
                {
                    scan.SetMapped(true, saa.Value.ProbesUsed <= saa.Value.EfficiencyTarget);
                }
            }

            System.Diagnostics.Debug.WriteLine($"{BaseUtils.AppTicks.TickCountLap("JSC")} Stats analysis finished");

            callback?.Invoke(stats);
            Running = false;
        }


        private Thread StatsThread;
        private DateTime? start;
        private DateTime? end;
        private int cmdrid;
        private Action<JournalStats> callback;
        private bool Exit;
    };
}
