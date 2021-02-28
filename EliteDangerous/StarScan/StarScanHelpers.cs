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

using EliteDangerousCore;
using EliteDangerousCore.JournalEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteDangerousCore
{

    public partial class StarScan
    {
        // make or get a system node for a system. See starsystemnode for the rules on using these two structures for lookups

        private SystemNode GetOrCreateSystemNode(ISystem sys)
        {
            if (scanDataByName.TryGetValue(sys.Name, out SystemNode sn))            // try name, it may have been stored with an old entry without sys address 
                return sn;                                                          // so we check to see if we already have that first

            // then try sysaddr
            if (sys.SystemAddress.HasValue && ScanDataByNameSysaddr.TryGetValue(sys.NameSystemAddress, out sn))
                return sn;

            // not found, make a new node
            sn = new SystemNode(sys);

            // if it has a system address, we store it to the list that way. Else we add to name list
            if (sys.SystemAddress.HasValue)
                ScanDataByNameSysaddr[sys.NameSystemAddress] = sn;
            else
                scanDataByName[sys.Name] = sn;

            return sn;
        }

        private SystemNode FindSystemNode(ISystem sys)
        {
            if (scanDataByName.TryGetValue(sys.Name, out SystemNode sn))            // try name first, in case the entry is old enough not to have a system address
                return sn;

            if (sys.SystemAddress.HasValue)                                         // if the find has a system address, then we should now only check the system address table
            {
                if (ScanDataByNameSysaddr.TryGetValue(sys.NameSystemAddress, out sn)) // try system address
                    return sn;
            }
            else
            {                                                                       // find does not have system address, and was not found in DataByName
                                                                                    // it could be an old journal system with no sysaddr, in which case its probably has no data
                                                                                    //      as it should have been picked up by the DataByName if it did. But we can't distinguish so can't screen that out
                                                                                    // Or its synthesised with just the name available
                                                                                    // Either way, last check for sysaddr by name
                                                                                    // this is unlikely to be used now, probably just by Action system
                sn = ScanDataByNameSysaddr.Values.ToList().Find(x => x.System.Name.Equals(sys.Name));     // try and find it in the system by address by name
                if (sn != null)
                    return sn;
            }

            return null;
        }

        // bodyid can be null, bodyname must be set.
        // scan the history and try and find the best star system this bodyname is associated with

        private static Tuple<string, ISystem> FindBestSystem(int startindex, List<HistoryEntry> hl, string bodyname, int? bodyid, bool isstar )
        {
            System.Diagnostics.Debug.Assert(bodyname != null);

            for (int j = startindex; j >= 0; j--)
            {
                HistoryEntry he = hl[j];

                if (he.IsLocOrJump)
                {
                    JournalLocOrJump jl = (JournalLocOrJump)he.journalEntry;
                    string designation = BodyDesignations.GetBodyDesignation(bodyname, bodyid, isstar, he.System.Name);

                    if (IsStarNameRelated(he.System.Name, designation))       // if its part of the name, use it
                    {
                        return new Tuple<string, ISystem>(designation, he.System);
                    }
                    else if (jl != null && IsStarNameRelated(jl.StarSystem, designation))
                    {
                        // Ignore scans where the system name has changed
                        System.Diagnostics.Trace.WriteLine($"Rejecting body {designation} ({bodyname}) in system {he.System.Name} => {jl.StarSystem} due to system rename");
                        return null;
                    }
                }
            }

            return new Tuple<string, ISystem>(BodyDesignations.GetBodyDesignation(bodyname, bodyid, false, hl[startindex].System.Name), hl[startindex].System);
        }

        private static bool IsStarNameRelated(string starname, string bodyname )
        {
            if (bodyname.Length >= starname.Length)
            {
                string s = bodyname.Substring(0, starname.Length);
                return starname.Equals(s, StringComparison.InvariantCultureIgnoreCase);
            }
            else
                return false;
        }

        public static string IsStarNameRelatedReturnRest(string starname, string bodyname, string designation )          // null if not related, else rest of string
        {
            if (designation == null)
            {
                designation = bodyname;
            }

            if (designation.Length >= starname.Length)
            {
                string s = designation.Substring(0, starname.Length);
                if (starname.Equals(s, StringComparison.InvariantCultureIgnoreCase))
                    return designation.Substring(starname.Length).Trim();
            }

            return null;
        }

        private class DuplicateKeyComparer<TKey> : IComparer<string> where TKey : IComparable      // special compare for sortedlist
        {
            public int Compare(string x, string y)
            {
                if (x.Length > 0 && Char.IsDigit(x[0]))      // numbers..
                {
                    if (x.Length < y.Length)
                        return -1;
                    else if (x.Length > y.Length)
                        return 1;

                }

                return StringComparer.InvariantCultureIgnoreCase.Compare(x, y);
            }
        }

    }
}
