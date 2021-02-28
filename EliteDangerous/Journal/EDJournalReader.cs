﻿/*
 * Copyright © 2016-2020 EDDiscovery development team
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

using EliteDangerousCore.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using BaseUtils.JSON;
using System.IO;

namespace EliteDangerousCore
{
    public class EDJournalReader : TravelLogUnitLogReader
    {
        JournalEvents.JournalShipyard lastshipyard = null;
        JournalEvents.JournalStoredShips laststoredships = null;
        JournalEvents.JournalStoredModules laststoredmodules = null;
        JournalEvents.JournalOutfitting lastoutfitting = null;
        JournalEvents.JournalMarket lastmarket = null;
        JournalEvents.JournalNavRoute lastnavroute = null;
        JournalEvents.JournalCargo lastcargo = null;

        bool cqc = false;
        const int timelimit = 5 * 60;   //seconds.. 5 mins between logs. Note if we undock, we reset the counters.

        static JournalEvents.JournalContinued lastcontinued = null;

        private Queue<JournalEntry> StartEntries = new Queue<JournalEntry>();

        public EDJournalReader(string filename) : base(filename)
        {
        }

        public EDJournalReader(TravelLogUnit tlu) : base(tlu)
        {
        }

        // inhistoryrefreshparse = means reading history in batch mode
        // returns null if journal line is bad or its a repeat.. It does not throw
        private JournalEntry ProcessLine(string line, bool inhistoryrefreshparse)
        {
         //   System.Diagnostics.Debug.WriteLine("Line in '" + line + "'");
            int cmdrid = TravelLogUnit.CommanderId.HasValue  ? TravelLogUnit.CommanderId.Value  : -2; //-1 is hidden, -2 is never shown

            if (line.Length == 0)
                return null;

            JournalEntry je = null;

            try
            {           // use a try block in case anything in the creation goes tits up
                je = JournalEntry.CreateJournalEntry(line, true, true);       // save JSON, save json, don't return if bad
            }
            catch ( Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"{TravelLogUnit.FullName} Exception Bad journal line: {line} {ex.Message} {ex.StackTrace}");
                return null;
            }

            if ( je == null )
            {
                System.Diagnostics.Trace.WriteLine($"{TravelLogUnit.FullName} Bad journal line: {line}");
                return null;
            }

            bool toosoon = false;

            if (je.EventTypeID == JournalTypeEnum.Fileheader)
            {
                JournalEvents.JournalFileheader header = (JournalEvents.JournalFileheader)je;

                if ((header.Beta && !EliteConfigInstance.InstanceOptions.DisableBetaCommanderCheck) || EliteConfigInstance.InstanceOptions.ForceBetaOnCommander) // if beta, and not disabled, or force beta
                {
                    TravelLogUnit.Type |= TravelLogUnit.BetaMarker;
                }

                if (header.Part > 1)
                {
                    // if we have a last continued, and its header parts match, and it has a commander, and its not too different in time..
                    if (lastcontinued != null && lastcontinued.Part == header.Part && lastcontinued.CommanderId >= 0 &&
                            Math.Abs(header.EventTimeUTC.Subtract(lastcontinued.EventTimeUTC).TotalSeconds) < 5)
                    {
                        cmdrid = lastcontinued.CommanderId;
                        TravelLogUnit.CommanderId = lastcontinued.CommanderId;      // copy commander across.
                    }
                    else
                    {           // this only works if you have a history... EDD does.
                        JournalEvents.JournalContinued contd = JournalEntry.GetLast<JournalEvents.JournalContinued>(je.EventTimeUTC.AddSeconds(1), e => e.Part == header.Part);

                        // Carry commander over from previous log if it ends with a Continued event.
                        if (contd != null && Math.Abs(header.EventTimeUTC.Subtract(contd.EventTimeUTC).TotalSeconds) < 5 && contd.CommanderId >= 0)
                        {
                            cmdrid = lastcontinued.CommanderId;
                            TravelLogUnit.CommanderId = contd.CommanderId;
                        }
                    }
                }
            }
            else if ( je.EventTypeID == JournalTypeEnum.Continued )
            {
                lastcontinued = je as JournalEvents.JournalContinued;       // save.. we are getting a new file soon
            }
            else if (je.EventTypeID == JournalTypeEnum.LoadGame)
            {
                var jlg = je as JournalEvents.JournalLoadGame;
                string newname = jlg.LoadGameCommander;

                if ((TravelLogUnit.Type & TravelLogUnit.BetaMarker) == TravelLogUnit.BetaMarker)
                {
                    newname = "[BETA] " + newname;
                }

                EDCommander commander = EDCommander.GetCommander(newname);

                if (commander == null )
                {
                    // in the default condition, we have a hidden commander, and first Cmdr. Jameson.
                    commander = EDCommander.GetListCommanders().FirstOrDefault();
                    if (EDCommander.NumberOfCommanders == 2 && commander != null && commander.Name == "Jameson (Default)")
                    {
                        commander.Name = newname;
                        commander.EdsmName = newname;
                        EDCommander.Update(new List<EDCommander> { commander }, false);
                    }
                    else
                    {
                        string defpath = EDJournalUIScanner.GetDefaultJournalDir();     // may be null if the system is not known
                        string jp = defpath != null && defpath.Equals(TravelLogUnit.Path) ? "" : TravelLogUnit.Path;
                        commander = EDCommander.Create(name: newname, journalpath: jp);

                        if (EDCommander.Current.Name.Contains("[BETA]") && !newname.Contains("[BETA]"))        // if current commander is beta, and we dont, swap to it
                            EDCommander.CurrentCmdrID = commander.Id;
                    }

                }

                commander.FID = jlg.FID;

                cmdrid = commander.Id;

                if (!TravelLogUnit.CommanderId.HasValue)        // we do not need to write to DB the TLU at this point, since we read something the upper layers will do that
                {
                    TravelLogUnit.CommanderId = cmdrid;
                    //System.Diagnostics.Trace.WriteLine(string.Format("TLU {0} updated with commander {1} at {2}", TravelLogUnit.Path, cmdrid, TravelLogUnit.Size));
                }
            }
            else if (je is ISystemStationEntry && ((ISystemStationEntry)je).IsTrainingEvent)
            {
                //System.Diagnostics.Trace.WriteLine($"{filename} Training detected:\n{line}");
                return null;
            }

            if (je is IAdditionalFiles)
            {
                if ((je as IAdditionalFiles).ReadAdditionalFiles(TravelLogUnit.Path, inhistoryrefreshparse) == false)     // if failed
                    return null;
            }

            if (je is JournalEvents.JournalShipyard)                // when going into shipyard
            {
                toosoon = lastshipyard != null && lastshipyard.Yard.Equals((je as JournalEvents.JournalShipyard).Yard);
                lastshipyard = je as JournalEvents.JournalShipyard;
            }
            else if (je is JournalEvents.JournalStoredShips)        // when going into shipyard
            {
                toosoon = laststoredships != null && CollectionStaticHelpers.Equals(laststoredships.ShipsHere, (je as JournalEvents.JournalStoredShips).ShipsHere) &&
                    CollectionStaticHelpers.Equals(laststoredships.ShipsRemote, (je as JournalEvents.JournalStoredShips).ShipsRemote);
                laststoredships = je as JournalEvents.JournalStoredShips;
            }
            else if (je is JournalEvents.JournalStoredModules)      // when going into outfitting
            {
                toosoon = laststoredmodules != null && CollectionStaticHelpers.Equals(laststoredmodules.ModuleItems, (je as JournalEvents.JournalStoredModules).ModuleItems);
                laststoredmodules = je as JournalEvents.JournalStoredModules;
            }
            else if (je is JournalEvents.JournalOutfitting)         // when doing into outfitting
            {
                toosoon = lastoutfitting != null && lastoutfitting.ItemList.Equals((je as JournalEvents.JournalOutfitting).ItemList);
                lastoutfitting = je as JournalEvents.JournalOutfitting;
            }
            else if (je is JournalEvents.JournalMarket)
            {
                toosoon = lastmarket != null && lastmarket.Equals(je as JournalEvents.JournalMarket);
                lastmarket = je as JournalEvents.JournalMarket;
            }
            else if ( je is JournalEvents.JournalCargo )
            {
                var cargo = je as JournalEvents.JournalCargo;
                if ( lastcargo != null )
                {
                    toosoon = lastcargo.SameAs(cargo);     // if exactly the same, swallow.
                    //System.Diagnostics.Debug.WriteLine("Cargo vs last " + toosoon);
                }
                lastcargo = cargo;
            }
            else if (je is JournalEvents.JournalUndocked || je is JournalEvents.JournalLoadGame)             // undocked, Load Game, repeats are cleared
            {
                lastshipyard = null;
                laststoredmodules = null;
                lastoutfitting = null;
                laststoredmodules = null;
                laststoredships = null;
                lastcargo = null;
                cqc = (je is JournalEvents.JournalLoadGame) && ((JournalEvents.JournalLoadGame)je).GameMode == null;
            }
            else if (je is JournalEvents.JournalMusic)
            {
                var music = je as JournalEvents.JournalMusic;
                
                if (music.MusicTrackID == JournalEvents.EDMusicTrackEnum.CQC || music.MusicTrackID == JournalEvents.EDMusicTrackEnum.CQCMenu)
                {
                    cqc = true;
                }
            }
            else if (je is JournalEvents.JournalNavRoute)
            {
                var route = je as JournalEvents.JournalNavRoute;

                if (lastnavroute != null && (route.EventTimeUTC == lastnavroute.EventTimeUTC || route.EventTimeUTC == lastnavroute.EventTimeUTC.AddSeconds(1)))
                {
                    toosoon = true;
                }

                lastnavroute = route;
            }

            if (toosoon)                                                // if seeing repeats, remove
            {
               // System.Diagnostics.Debug.WriteLine("**** Remove as dup " + je.EventTypeStr);
                return null;
            }

            if (cqc)  // Ignore events if in CQC
            {
                return null;
            }

            je.SetTLUCommander(TravelLogUnit.ID, cmdrid);

            return je;
        }

        // function needs to report two things, list of JREs (may be empty) and UIs, and if it read something, bool.. hence form changed
        // bool reporting we have performed any sort of action is important.. it causes the TLU pos to be updated above even if we have junked all the events or delayed them
        // function does not throw.

        public bool ReadJournal(List<JournalEntry> jent, List<UIEvent> uievents, bool historyrefreshparsing )      // True if anything was processed, even if we rejected it
        {
            bool readanything = false;

            while (true)
            {
                string line = ReadLine();           // read line from TLU.

                if (line == null)                   // null means finished, no more data
                    return readanything;

                //System.Diagnostics.Debug.WriteLine("Line read '" + line + "'");
                readanything = true;

                JournalEntry newentry = ProcessLine(line, historyrefreshparsing);

                if (newentry != null)                           // if we got a record back, we may not because it may not be valid or be rejected..
                {
                    // if we don't have a commander yet, we need to queue it until we have one, since every entry needs a commander

                    if ((this.TravelLogUnit.CommanderId == null || this.TravelLogUnit.CommanderId < 0) && newentry.EventTypeID != JournalTypeEnum.LoadGame)
                    {
                        //System.Diagnostics.Debug.WriteLine("*** Delay " + newentry.JournalEntry.EventTypeStr);
                        StartEntries.Enqueue(newentry);         // queue..
                    }
                    else
                    {
                        while (StartEntries.Count != 0)     // we have a commander, anything queued up, play that in first.
                        {
                            var dentry = StartEntries.Dequeue();
                            dentry.SetCommander(TravelLogUnit.CommanderId.Value);
                            //System.Diagnostics.Debug.WriteLine("*** UnDelay " + dentry.JournalEntry.EventTypeStr);
                            AddEntry(dentry, jent, uievents);
                        }

                        //System.Diagnostics.Debug.WriteLine("*** Send  " + newentry.JournalEntry.EventTypeStr);
                        AddEntry(newentry, jent, uievents);
                    }
                }
            }
        }

        // this class looks at the JE and decides if its really a UI not a journal entry

        private void AddEntry( JournalEntry newentry, List<JournalEntry> jent, List<UIEvent> uievents )
        {
            if (newentry.EventTypeID == JournalTypeEnum.Music)     // MANUALLY sync this list with ActionEventList.cs::EventList function
            {
                var jm = newentry as JournalEvents.JournalMusic;
                uievents.Add(new UIEvents.UIMusic(jm.MusicTrack, jm.MusicTrackID, jm.EventTimeUTC, false));
                return;
            }
            else if (newentry.EventTypeID == JournalTypeEnum.UnderAttack)
            {
                var ja = newentry as JournalEvents.JournalUnderAttack;
                uievents.Add(new UIEvents.UIUnderAttack(ja.Target, ja.EventTimeUTC, false));
                return;
            }
            else if (newentry.EventTypeID == JournalTypeEnum.SendText)
            {
                var jt = newentry as JournalEvents.JournalSendText;
                if (jt.Command)
                {
                    uievents.Add(new UIEvents.UICommand(jt.Message, jt.To, jt.EventTimeUTC, false));
                    return;
                }
            }
            else if (newentry.EventTypeID == JournalTypeEnum.ShipTargeted)
            {
                var jst = newentry as JournalEvents.JournalShipTargeted;
                if (jst.TargetLocked == false)
                {
                    uievents.Add(new UIEvents.UIShipTargeted(jst, jst.EventTimeUTC, false));
                    return;
                }

            }
            else if (newentry.EventTypeID == JournalTypeEnum.ReceiveText)
            {
                var jt = newentry as JournalEvents.JournalReceiveText;
                if (jt.Channel == "Info")
                {
                    uievents.Add(new UIEvents.UIReceiveText(jt, jt.EventTimeUTC, false));
                    return;
                }
            }
            else if (newentry.EventTypeID == JournalTypeEnum.FSDTarget)
            {
                var jt = newentry as JournalEvents.JournalFSDTarget;
                uievents.Add(new UIEvents.UIFSDTarget(jt, jt.EventTimeUTC, false));
                return;
            }

            jent.Add(newentry);
        }
    }
}


