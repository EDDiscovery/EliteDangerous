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
        public bool AddScanToBestSystem(JournalScan je, int startindex, List<HistoryEntry> hl, out HistoryEntry he, out JournalLocOrJump jl)
        {
            he = null;
            jl = null;

            if (je?.BodyName == null)
                return false;

            // go thru the list of history entries looking for a Loc 

            for (int j = startindex; j >= 0; j--)   // same as FindBestSystem
            {
                he = hl[j];

                if (he.IsLocOrJump)
                {
                    jl = (JournalLocOrJump)he.journalEntry;

                    // get the body designation, given the je/system name

                    string designation = BodyDesignations.GetBodyDesignation(je, he.System.Name);
                    System.Diagnostics.Debug.Assert(designation != null);

                    // either the name/sys address matches, or the designation matches the star of the system name
                    if (je.IsStarNameRelated(he.System.Name,he.System.SystemAddress, designation))       
                    {
                        je.BodyDesignation = designation;
                        return ProcessJournalScan(je, he.System, true);
                    }
                    else if (jl.StarSystem != null && je.IsStarNameRelated(jl.StarSystem, jl.SystemAddress, designation)) // if we have a starsystem name, and its related, its a rename, ignore it
                    {
                        System.Diagnostics.Trace.WriteLine($"Rejecting body {designation} ({je.BodyName}) in system {he.System.Name} => {jl.StarSystem} due to system rename");
                        return false;
                    }
                }
            }

            je.BodyDesignation = BodyDesignations.GetBodyDesignation(je, hl[startindex].System.Name);
            return ProcessJournalScan(je, hl[startindex].System, true);         // no relationship, add..
        }

        // take the journal scan and add it to the node tree

        private bool ProcessJournalScan(JournalScan sc, ISystem sys, bool reprocessPrimary = false)  // background or foreground.. FALSE if you can't process it
        {
            SystemNode sn = GetOrCreateSystemNode(sys);

            // handle Earth, starname = Sol
            // handle Eol Prou LW-L c8-306 A 4 a and Eol Prou LW-L c8-306
            // handle Colonia 4 , starname = Colonia, planet 4
            // handle Aurioum B A BELT
            // Kyloasly OY-Q d5-906 13 1

            // Extract elements from name, and extract if belt, top node type, if ring 
            List<string> elements = ExtractElementsJournalScan(sc, sys, out ScanNodeType starscannodetype, out bool isbeltcluster, out bool isring);

            // Bail out if no elements extracted
            if (elements.Count == 0)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to add body {sc.BodyName} to system {sys.Name} - not enough elements");
                return false;
            }
            // Bail out if more than 5 elements extracted
            else if (elements.Count > 5)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to add body {sc.BodyName} to system {sys.Name} - too deep");
                return false;
            }

            //System.Diagnostics.Debug.WriteLine("Made body JS " + sc.BodyName);

            // Get custom name if different to designation
            string customname = GetCustomNameJournalScan(sc, sys);

            // Process elements, 
            ScanNode node = ProcessElementsJournalScan(sc, sys, sn, customname, elements, starscannodetype, isbeltcluster, isring);

            if (node.BodyID != null)
            {
                sn.NodesByID[(int)node.BodyID] = node;
            }

            // Process top-level star
            if (elements.Count == 1)
            {
                // Process any belts if present
                ProcessBelts(sc, node);

                // Process primary star in multi-star system
                if (elements[0].Equals("A", StringComparison.InvariantCultureIgnoreCase))
                {
                    BodyDesignations.CachePrimaryStar(sc, sys);

                    // Reprocess if we've encountered the primary (A) star and we already have a "Main Star", we reprocess to 
                    // allow any updates to PrimaryCache to make a difference

                    if (reprocessPrimary && sn.StarNodes.Any(n => n.Key.Length > 1 && n.Value.NodeType == ScanNodeType.star))       
                    {
                        // get bodies with scans
                        List<JournalScan> bodies = sn.Bodies.Where(b => b.ScanData != null).Select(b => b.ScanData).ToList();

                        // reset the nodes to zero
                        sn.StarNodes = new SortedList<string, ScanNode>(new DuplicateKeyComparer<string>());
                        sn.NodesByID = new SortedList<int, ScanNode>();

                        foreach (JournalScan js in bodies)              // replay into process the body scans.. using the newly updated body designation (primary star cache) to correct any errors
                        {
                            js.BodyDesignation = BodyDesignations.GetBodyDesignation(js, sn.System.Name);
                            ProcessJournalScan(js, sn.System);
                        }
                    }
                }
            }

            ProcessedSaved(sn);  // any saved JEs due to no scan, add

            return true;
        }

        // extract elements of the name and form into an element array
        // element[0] is the star or barycentre
        // element[0] star/barycentre [ element[1] planet [ element[2] submoon [ element[3] subsubmoon ]]]
        // if belt cluster, we get [0] = star, [1] = belt, [2] = cluster N
        // if ring, we get [0] = star, [2] = body, ... [last-1] = ring name

        private List<string> ExtractElementsJournalScan(JournalScan sc, ISystem sys, out ScanNodeType starscannodetype, out bool isbeltcluster, out bool isring)
        {
            starscannodetype = ScanNodeType.star;
            isbeltcluster = false;
            isring = false;
            List<string> elements;
                
            string rest = sc.IsStarNameRelatedReturnRest(sys.Name, sys.SystemAddress);      // extract any relationship between the system we are in and the name, and return it if so

            if (rest != null)                                   // if we have a relationship between the system name and the body name
            {
                if (sc.IsStar && !sc.IsEDSMBody && sc.DistanceFromArrivalLS == 0 && rest.Length >= 2)       // star, primary, with name >= 2 (AB)
                {
                    elements = new List<string> { rest };       // its a star, default
                }
                else if (rest.Length > 0)                       // we have characters in rest, and its related to the system name
                {
                    elements = rest.Split(' ').ToList();        // split into spaced parts

                    if (elements.Count == 4 && elements[0].Length == 1 && char.IsLetter(elements[0][0]) &&          // A belt cluster N
                            elements[1].Equals("belt", StringComparison.InvariantCultureIgnoreCase) &&
                            elements[2].Equals("cluster", StringComparison.InvariantCultureIgnoreCase))
                    {
                        elements = new List<string> { MainStar, elements[0] + " " + elements[1], elements[2] + " " + elements[3] };     // reform into Main Star | A belt | Cluster N
                        isbeltcluster = true;
                    }
                    else if (elements.Count == 5 && elements[0].Length >= 1 &&                                      // AA A belt cluster N
                            elements[1].Length == 1 && char.IsLetter(elements[1][0]) &&
                            elements[2].Equals("belt", StringComparison.InvariantCultureIgnoreCase) &&
                            elements[3].Equals("cluster", StringComparison.InvariantCultureIgnoreCase))
                    {
                        elements = new List<string> { elements[0], elements[1] + " " + elements[2], elements[3] + " " + elements[4] };      // reform into <star> | A belt | Cluster N
                        isbeltcluster = true;
                    }
                    else if (elements.Count >= 3 &&
                             elements[elements.Count - 1].Equals("ring", StringComparison.InvariantCultureIgnoreCase) &&        // A 2 A ring
                             elements[elements.Count - 2].Length == 1 &&
                             char.IsLetter(elements[elements.Count - 2][0]))
                    {                                                                                               // reform into A | 2 | A ring three level or A A ring
                        elements = elements.Take(elements.Count - 2).Concat(new string[] { elements[elements.Count - 2] + " " + elements[elements.Count - 1] }).ToList();
                        isring = true;
                    }

                    if (char.IsDigit(elements[0][0]))                                   // if digits, planet number, no star designator
                        elements.Insert(0, MainStar);                                   // no star designator, main star, add MAIN
                    else if (elements[0].Length > 1 && elements[0] != MainStar)         // designator, is it multiple chars.. its a barycentre ABC
                        starscannodetype = ScanNodeType.barycentre;                     // override node type to barycentre
                }
                else
                {
                    elements = new List<string>();                                      // only 1 item, the star, which is the same as the system name..
                    elements.Add(MainStar);                                             // Sol / SN:Sol should come thru here
                }
            }
            else if (sc.IsStar && !sc.IsEDSMBody && sc.DistanceFromArrivalLS == 0)      // name has no relationship to system (Gliese..) and its a star at LS=0
            {
                elements = new List<string> { sc.BodyName };                            // its a star
            }
            else
            {                                                                           // name has no relationship to system (Earth) but its not at LS=0
                elements = sc.BodyName.Split(' ').ToList();                             // use all bodyparts, and 
                elements.Insert(0, MainStar);                                           // insert the MAIN designator as the star designator
            }

            return elements;
        }

        // see above for elements

        private ScanNode ProcessElementsJournalScan(JournalScan sc, ISystem sys, SystemNode systemnode, string customname, List<string> elements, 
                                                    ScanNodeType starscannodetype, bool isbeltcluster, bool isring)
        {

            List<JournalScan.BodyParent> ancestors = sc.Parents?.AsEnumerable()?.ToList();      // this includes Rings, Barycentres(Nulls) that frontier put into the list..

            // remove all rings and barycenters first, since thats not in our element list. We just want the bodies and belts
            List<JournalScan.BodyParent> ancestorbodies = ancestors?.Where(a => a.Type == "Star" || a.Type == "Planet" || a.Type == "Belt")?.Reverse()?.ToList();

            // but we need to add back the barycenter at the top, since we do add that that in the element list
            if (ancestorbodies != null && ancestorbodies.Count>0 && starscannodetype == ScanNodeType.barycentre)      
            {                                                                               
               // this checks out, but disable for safety.  System.Diagnostics.Debug.Assert(ancestors[ancestors.Count - 1].Type == "Null");     // double check its a barycentre, it should be
                ancestorbodies.Insert(0, ancestors[ancestors.Count - 1]);
            }

            // for each element we process into the tree

            SortedList<string, ScanNode> currentnodelist = systemnode.StarNodes;            // current operating node list, always made
            ScanNode previousnode = null;                                                   // trails subnode by 1 to point to previous node

            for (int lvl = 0; lvl < elements.Count; lvl++)
            {
                ScanNodeType sublvtype = starscannodetype;                                  // top level, element[0] type is starscannode (star/barycentre)    

                if (lvl > 0)                                                                // levels pass 0, we need to determine what it is    
                {
                    if (isbeltcluster)                                                      // a belt cluster is in three levels (star, belt, cluster number)
                    {
                        if (lvl == 1)                                                       // next level, its a belt
                            sublvtype = ScanNodeType.belt;              
                        else
                            sublvtype = ScanNodeType.beltcluster;                           // third level, cluster
                    }
                    else if (isring && lvl == elements.Count - 1)                           // and level, and a ring, mark as a ring
                    {
                        sublvtype = ScanNodeType.ring;
                    }
                    else
                        sublvtype = ScanNodeType.body;                                      // default is body for levels 1 on
                }

                // if not got a node list (only happens when we have a scannode from another scannode), or we are not in the node list

                if (currentnodelist == null || !currentnodelist.TryGetValue(elements[lvl], out ScanNode subnode)) // either no nodes, or not found the element name in the node list.
                {
                    if ( currentnodelist == null)    // no node list, happens when we are at least 1 level down as systemnode always has a node list, make one 
                        currentnodelist = previousnode.Children = new SortedList<string, ScanNode>(new DuplicateKeyComparer<string>()); 

                    string ownname = elements[lvl];

                    subnode = new ScanNode            
                    {
                        OwnName = ownname,
                        FullName = previousnode == null ? (sys.Name + (ownname.Contains("Main") ? "" : (" " + ownname))) : previousnode.FullName + " " + ownname,
                        ScanData = null,
                        Children = null,
                        NodeType = sublvtype,
                        Level = lvl,
                    };

                    currentnodelist.Add(ownname, subnode);
                }

                if (ancestorbodies != null && lvl < ancestorbodies.Count)       // if we have the ancestor list, we can fill in the bodyid for each part.
                {
                    subnode.BodyID = ancestorbodies[lvl].BodyID;
                    systemnode.NodesByID[(int)subnode.BodyID] = subnode;
                }
                    
                if (lvl == elements.Count - 1)                                  // if we are at the end node..
                {
                    subnode.ScanData = sc;                                      // only overwrites if scan is better
                    subnode.ScanData.SetMapped(subnode.IsMapped, subnode.WasMappedEfficiently);      // pass this data to node, as we may have previously had a SAA Scan
                    subnode.CustomName = customname;                            // and its custom name

                    if (sc.BodyID != null)                                      // if scan has a body ID, pass it to the node
                    {
                        subnode.BodyID = sc.BodyID;
                    }
                }

                previousnode = subnode;                                         // move forward 1 step
                currentnodelist = previousnode.Children;
            }

            return previousnode;
        }

        // asteroid belts, not rings, are assigned to sub nodes of the star in the node heirarchy as type==belt.

        private void ProcessBelts(JournalScan sc, ScanNode node)
        {
            if (sc.HasRings)
            {
                foreach (JournalScan.StarPlanetRing ring in sc.Rings)
                {
                    string beltname = ring.Name;
                    string stardesig = sc.BodyDesignation ?? sc.BodyName;

                    if (beltname.StartsWith(stardesig, StringComparison.InvariantCultureIgnoreCase))
                    {
                        beltname = beltname.Substring(stardesig.Length).Trim();
                    }
                    else if (stardesig.ToLowerInvariant() == "lave" && beltname.ToLowerInvariant() == "castellan belt")
                    {
                        beltname = "A Belt";
                    }

                    if (node.Children == null || !node.Children.TryGetValue(beltname, out ScanNode belt))
                    {
                        if (node.Children == null)
                            node.Children = new SortedList<string, ScanNode>(new DuplicateKeyComparer<string>());

                        belt = new ScanNode
                        {
                            OwnName = beltname,
                            FullName = node.FullName + " " + beltname,
                            CustomName = ring.Name,
                            ScanData = null,
                            BeltData = ring,
                            Children = null,
                            NodeType = ScanNodeType.belt,
                            Level = 1
                        };

                        node.Children.Add(beltname, belt);
                    }

                    belt.BeltData = ring;
                }
            }
        }

        // find a better name for the body

        private string GetCustomNameJournalScan(JournalScan sc, ISystem sys)
        {
            string rest = sc.IsStarNameRelatedReturnRest(sys.Name, sys.SystemAddress);      // this can be null
            string customname = null;

            if (sc.BodyName.StartsWith(sys.Name, StringComparison.InvariantCultureIgnoreCase))  // if body starts with system name
            {
                customname = sc.BodyName.Substring(sys.Name.Length).TrimStart(' ', '-');    // cut out system name

                if (customname == "" && !sc.IsStar)                                         // if empty, and not star, customname is just the body name
                {
                    customname = sc.BodyName;
                }
                else if (customname == "" || customname == rest)                            // if empty, or its the same as the star name related, its not got a customname
                {
                    customname = null;
                }
            }
            else if (rest == null || !sc.BodyName.EndsWith(rest))                           // not related to star, or not related to bodyname, set back to body name
            {
                customname = sc.BodyName;
            }

            return customname;
        }
    }
}
