﻿/*
 * Copyright © 2015 - 2021 EDDiscovery development team
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

namespace EliteDangerousCore
{
    public partial class StarScan
    {
        // used by historylist directly for a single update during play, in foreground..  Also used by above.. so can be either in fore/back
        public bool AddSAAScanToBestSystem(JournalSAAScanComplete jsaa, int startindex, List<HistoryEntry> hl)
        {
            if (jsaa.BodyName == null)
                return false;

            var best = FindBestSystem(startindex, hl, jsaa.BodyName, jsaa.BodyID, false);

            if (best == null)
                return false;

            jsaa.BodyDesignation = best.Item1;

            return ProcessSAAScan(jsaa, best.Item2);         
        }

        private bool ProcessSAAScan(JournalSAAScanComplete jsaa, ISystem sys, bool saveprocessinglater = true)  // background or foreground.. FALSE if you can't process it
        {
            SystemNode sn = GetOrCreateSystemNode(sys);
            ScanNode relatednode = null;

            if (sn.NodesByID.ContainsKey((int)jsaa.BodyID))
            {
                relatednode = sn.NodesByID[(int)jsaa.BodyID];
                if (relatednode.ScanData != null && relatednode.ScanData.BodyDesignation != null)
                {
                    jsaa.BodyDesignation = relatednode.ScanData.BodyDesignation;
                }
            }
            else if (jsaa.BodyDesignation != null && jsaa.BodyDesignation != jsaa.BodyName)
            {
                foreach (var body in sn.Bodies)
                {
                    if (body.FullName == jsaa.BodyDesignation)
                    {
                        relatednode = body;
                        break;
                    }
                }
            }

            if (relatednode == null)
            {
                foreach (var body in sn.Bodies)
                {
                    if ((body.FullName == jsaa.BodyName || body.CustomName == jsaa.BodyName) &&
                        (body.FullName != sys.Name || body.Level != 0))
                    {
                        relatednode = body;
                        break;
                    }
                }
            }

            if (relatednode != null)
            {
                relatednode.IsMapped = true;        // keep data here since we can get scans replaced later..
                relatednode.WasMappedEfficiently = jsaa.ProbesUsed <= jsaa.EfficiencyTarget;
                //System.Diagnostics.Debug.WriteLine("Setting SAA Scan for " + jsaa.BodyName + " " + sys.Name + " to Mapped: " + relatednode.WasMappedEfficiently);

                if (relatednode.ScanData != null)       // if we have a scan, set its values - this keeps the calculation self contained in the class.
                {
                    relatednode.ScanData.SetMapped(relatednode.IsMapped, relatednode.WasMappedEfficiently);
                    //System.Diagnostics.Debug.WriteLine(".. passing down to scan " + relatedScan.ScanData.ScanType);
                }

                return true; // We already have the scan
            }
            else
            {
                if (saveprocessinglater)
                    sn.SaveForProcessing(jsaa,sys);
                //System.Diagnostics.Debug.WriteLine("No body to attach data found for " + jsaa.BodyName + " @ " + sys.Name + " body " + jsaa.BodyDesignation);
            }

            return false;
        }


        #region FSS DISCOVERY *************************************************************

        public void SetFSSDiscoveryScan(JournalFSSDiscoveryScan je, ISystem sys)
        {
            SystemNode sn = GetOrCreateSystemNode(sys);
            sn.FSSTotalBodies = je.BodyCount;
        }

        #endregion

    }
}
