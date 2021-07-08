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
using System.Linq;

namespace EliteDangerousCore
{
    public partial class StarScan
    {
        public Dictionary<long, SystemNode> ScanDataBySysaddr { get; private set; } = new Dictionary<long, SystemNode>();       // primary store - may not be there
        public Dictionary<string, SystemNode> ScanDataByName { get; private set; } = new Dictionary<string, SystemNode>();      // by name, always there

        private const string MainStar = "Main Star";

        // this tries to reprocess any JEs associated with a system node which did not have scan data at the time.
        // Seen to work with log from Issue #2983

        public List<Tuple<JournalEntry, ISystem>> ToProcess = new List<Tuple<JournalEntry, ISystem>>();     // entries seen but yet to be processed due to no scan node (used by reports which do not create scan nodes)

        public void SaveForProcessing(JournalEntry je, ISystem sys)
        {
            ToProcess.Add(new Tuple<JournalEntry, ISystem>(je, sys));
        }

        private void ProcessedSaved()
        {
            List<Tuple<JournalEntry, ISystem>> todelete = new List<Tuple<JournalEntry, ISystem>>();
            foreach (var e in ToProcess)
            {
                if (e.Item1.EventTypeID == JournalTypeEnum.SAAScanComplete)
                { 
                    if (ProcessSAAScan((JournalSAAScanComplete)e.Item1, e.Item2, false))
                        todelete.Add(e);
                }
                else if (e.Item1.EventTypeID == JournalTypeEnum.SAASignalsFound)
                {
                    if (ProcessSAASignalsFound((JournalSAASignalsFound)e.Item1, e.Item2, false))
                        todelete.Add(e);
                }
                else if (e.Item1.EventTypeID == JournalTypeEnum.FSSSignalDiscovered)
                {
                    if (AddFSSSignalsDiscoveredToSystem((JournalFSSSignalDiscovered)e.Item1, false))
                        todelete.Add(e);
                }
            }

            foreach (var je in todelete)
                ToProcess.Remove(je);
        }

        public bool HasWebLookupOccurred(ISystem sys)       // have we had a web checkup on this system?  false if sys does not exist
        {
            SystemNode sn = FindSystemNode(sys);
            return (sn != null && sn.EDSMWebChecked);
        }

        // ONLY use this if you must because the async await won't work in the call stack.  edsmweblookup here with true is strongly discouraged

        public SystemNode FindSystemSynchronous(ISystem sys, bool edsmweblookup)    // Find the system. Optionally do a EDSM web lookup
        {
            System.Diagnostics.Debug.Assert(System.Windows.Forms.Application.MessageLoop);  // foreground only
            System.Diagnostics.Debug.Assert(sys != null);

            SystemNode sn = FindSystemNode(sys);

            // System.Diagnostics.Debug.WriteLine("Scan Lookup " + sys.Name + " found " + (sn != null) + " web? " + edsmweblookup + " edsm lookup " + (sn?.EDSMAdded ?? false));

            if ((sys.EDSMID > 0 || (sys.SystemAddress != null && sys.SystemAddress > 0) || (sys.Name.HasChars())) && (sn == null || sn.EDSMCacheCheck == false || (edsmweblookup && !sn.EDSMWebChecked)))
            {
                var jl = EliteDangerousCore.EDSM.EDSMClass.GetBodiesList(sys, edsmweblookup); // lookup, with optional web

                //if (edsmweblookup) System.Diagnostics.Debug.WriteLine("EDSM WEB Lookup bodies " + sys.Name + " " + sys.EDSMID + " result " + (jl?.Count ?? -1));

                if (jl != null && jl.Item2 == false) // found some bodies, not from the cache
                {
                    foreach (JournalScan js in jl.Item1)
                    {
                        js.BodyDesignation = BodyDesignations.GetBodyDesignation(js, sys.Name);
                        ProcessJournalScan(js, sys, true);
                    }
                }

                if (sn == null) // refind to make sure SN is set
                    sn = FindSystemNode(sys);

                if (sn != null) // if we found it, set to indicate we did a cache check
                {
                    sn.EDSMCacheCheck = true;

                    if (edsmweblookup)      // and if we did a web check, set it too..
                        sn.EDSMWebChecked = true;
                }
            }

            return sn;

        }

        // you must be returning void to use this..

        public async System.Threading.Tasks.Task<SystemNode> FindSystemAsync(ISystem sys, bool edsmweblookup )    // Find the system. Optionally do a EDSM web lookup
        {
            System.Diagnostics.Debug.Assert(System.Windows.Forms.Application.MessageLoop);  // foreground only
            System.Diagnostics.Debug.Assert(sys != null);

            SystemNode sn = FindSystemNode(sys);

            //string trace = Environment.StackTrace.StackTrace("FindSystemAsync", 4);

            //System.Diagnostics.Debug.WriteLine("Scan Lookup " + trace + " " + sys.Name + " found " + (sn != null) + " web? " + edsmweblookup + " edsm lookup " + (sn?.EDSMWebChecked ?? false));

            if ((sys.EDSMID > 0 || (sys.SystemAddress != null && sys.SystemAddress > 0) || (sys.Name.HasChars())) && (sn == null || sn.EDSMCacheCheck == false || (edsmweblookup && !sn.EDSMWebChecked)))
            {
                var jl = await EliteDangerousCore.EDSM.EDSMClass.GetBodiesListAsync(sys, edsmweblookup); // lookup, with optional web

                // return bodies and a flag indicating if from cache.
                // Scenario: Three panels are asking for data, one at a time, since its the foreground thread
                // each one awaits, sets and runs a task, blocks until tasks completes, foreground continues to next panel where it does the same
                // we have three tasks, any which could run in any order. 
                // The tasks all go thru GetBodiesListAsync, which locks.  Only 1 task gets to do the lookup, the one which got there first, because it did not see
                // a cached version
                // once that task completes the lookups, and it unlocks, the other tasks can run, and they will see the cache setup.  They won't do an EDSM web access
                // since the body is in the cache.  
                // for now, i can't guarantee that the task which gives back the bodies first runs on the foreground task.  It may be task2 gets the bodies.
                // so we will just add them in again

                if (jl != null && jl.Item1 != null)
                {
                    // removed - can't guarantee if (jl.Item2 == false)      // only want them if not previously cached
                    {
                        //System.Diagnostics.Debug.WriteLine("Process bodies from EDSM " + trace + " " + sys.Name + " " + sys.EDSMID + " result " + (jl.Item1?.Count ?? -1));
                        foreach (JournalScan js in jl.Item1)
                        {
                            js.BodyDesignation = BodyDesignations.GetBodyDesignation(js, sys.Name);
                            ProcessJournalScan(js, sys, true);
                        }
                    }
                }

                //System.Diagnostics.Debug.WriteLine("Lookup System node again");
                if (sn == null) // refind to make sure SN is set
                    sn = FindSystemNode(sys);

                if (sn != null) // if we found it, set to indicate we did a cache check
                {
                    sn.EDSMCacheCheck = true;

                    if (edsmweblookup)      // and if we did a web check, set it too..
                        sn.EDSMWebChecked = true;
                }
            }

            return sn;
        }
    }
}
