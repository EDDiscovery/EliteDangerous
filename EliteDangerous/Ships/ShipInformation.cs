﻿/*
 * Copyright © 2016-2023 EDDiscovery development team
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
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EliteDangerousCore.JournalEvents;
using QuickJSON;

namespace EliteDangerousCore
{
    [System.Diagnostics.DebuggerDisplay("{ID}:{ShipType}:{ShipFD}:{Modules.Count}")]
    public class ShipInformation
    {
        #region Information interface

        public ulong ID { get; private set; }                 // its Frontier ID.     ID's are moved to high range when sold
        public enum ShipState { Owned, Sold, Destroyed};
        public ShipState State { get; set; } = ShipState.Owned; // if owned, sold, destroyed. Default owned
        public string ShipType { get; private set; }        // ship type name, nice, fer-de-lance, etc. can be null
        public string ShipFD { get; private set; }          // ship type name, fdname
        public string ShipUserName { get; private set; }    // ship name, may be empty or null
        public string ShipUserIdent { get; private set; }   // ship ident, may be empty or null
        public long HullValue { get; private set; }         // may be 0, not known
        public long ModulesValue { get; private set; }      // may be 0, not known
        public double HullHealthAtLoadout { get; private set; } // may be 0, in range 0-100.
        public double UnladenMass { get; private set; }     // may be 0, not known, from loadout
        public double FuelLevel { get; private set; }       // fuel level may be 0 not known
        public double FuelCapacity { get; private set; }    // fuel capacity may be 0 not known. Calculated as previous loadouts did not include this. 3.4 does
        public double ReserveFuelCapacity { get; private set; }  // 3.4 from loadout..
        public long Rebuy { get; private set; }             // may be 0, not known

        public string StoredAtSystem { get; private set; }  // null if not stored, else where stored
        public string StoredAtStation { get; private set; } // null if not stored or unknown
        public DateTime TransferArrivalTimeUTC { get; private set; }     // if current UTC < this, its in transit
        public bool Hot { get; private set; }               // if known to be hot.

        public enum SubVehicleType
        {
            None, SRV, Fighter
        }

        public SubVehicleType SubVehicle { get; private set; } = SubVehicleType.None;    // if in a sub vehicle or mothership

        public Dictionary<ShipSlots.Slot, ShipModule> Modules { get; private set; }     // slot to ship module installed

        public bool InTransit { get { return TransferArrivalTimeUTC.CompareTo(DateTime.UtcNow)>0; } }

        public ShipModule GetModuleInSlot(ShipSlots.Slot slot) { return Modules.ContainsKey(slot) ? Modules[slot] : null; }      // Name is the nice Slot name.
        public ShipModule.EngineeringData GetEngineering(ShipSlots.Slot slot) { return Modules.ContainsKey(slot) ? Modules[slot].Engineering : null; }

        public string ShipFullInfo(bool cargo = true, bool fuel = true)
        {
            StringBuilder sb = new StringBuilder(64);
            if (ShipUserIdent != null)
                sb.Append(ShipUserIdent);
            sb.AppendPrePad(ShipUserName);
            sb.AppendPrePad(ShipType);
            sb.AppendPrePad("(" + ID.ToString() + ")");

            if (SubVehicle == SubVehicleType.SRV)
                sb.AppendPrePad(" in SRV");
            else if (SubVehicle == SubVehicleType.Fighter)
                sb.AppendPrePad(" in Fighter");
            else
            {
                if (State != ShipState.Owned)
                    sb.Append(" (" + State.ToString() + ")");

                if (InTransit)
                    sb.Append(" (Tx to " + StoredAtSystem + ")");
                else if (StoredAtSystem != null)
                    sb.Append(" (@" + StoredAtSystem + ")");

                if (fuel)
                {
                    double cap = FuelCapacity;
                    if (cap > 0)
                        sb.Append(" Fuel Cap " + cap.ToString("0.#"));
                }

                if (cargo)
                {
                    double cap = CargoCapacity();
                    if (cap > 0)
                        sb.Append(" Cargo Cap " + cap);
                }
            }

            return sb.ToString();
        }

        public string Name          // Name of ship, either user named or ship type
        {
            get                  // unique ID
            {
                if (ShipUserName != null && ShipUserName.Length > 0)
                    return ShipUserName;
                else
                    return ShipType;
            }
        }

        public string ShipShortName
        {
            get                  // unique ID
            {
                StringBuilder sb = new StringBuilder(64);
                if (ShipUserName != null && ShipUserName.Length > 0)
                {
                    sb.AppendPrePad(ShipUserName);
                }
                else
                {
                    sb.AppendPrePad(ShipType);
                    sb.AppendPrePad("(" + ID.ToString() + ")");
                }
                return sb.ToString();
            }
        }

        public string ShipNameIdentType
        {
            get                  // unique ID
            {
                string res = string.IsNullOrEmpty(ShipUserName) ? "" : ShipUserName;
                res = res.AppendPrePad(string.IsNullOrEmpty(ShipUserIdent) ? "" : ShipUserIdent, ",");
                bool empty = string.IsNullOrEmpty(res);
                res = res.AppendPrePad(ShipType, ",");
                if (empty)
                    res += " (" + ID.ToString() + ")";

                if (State != ShipState.Owned)
                    res += " (" + State.ToString() + ")";

                if (InTransit)
                    res += " (Tx to " + StoredAtSystem + ")";
                else if (StoredAtSystem != null)
                    res += " (@" + StoredAtSystem + ")";

                return res;
            }
        }

        public int GetFuelCapacity()
        {
            int cap = 0;
            foreach (ShipModule sm in Modules.Values)
            {
                int classpos;
                if (sm.Item.Contains("Fuel Tank") && (classpos = sm.Item.IndexOf("Class ")) != -1)
                {
                    char digit = sm.Item[classpos + 6];
                    cap += (1 << (digit - '0'));        // 1<<1 = 2.. 1<<2 = 4, etc.
                }
            }

            return cap;
        }

        public int CargoCapacity()
        {
            int cap = 0;
            foreach (ShipModule sm in Modules.Values)
            {
                int classpos;
                if (sm.Item.Contains("Cargo Rack") && (classpos = sm.Item.IndexOf("Class ")) != -1)
                {
                    char digit = sm.Item[classpos + 6];
                    cap += (1 << (digit - '0'));        // 1<<1 = 2.. 1<<2 = 4, etc.
                }
            }

            return cap;
        }

        public EliteDangerousCalculations.FSDSpec GetFSDSpec()          // may be null due to not having the info
        {
            ShipModule fsd = GetModuleInSlot(ShipSlots.Slot.FrameShiftDrive);
            EliteDangerousCalculations.FSDSpec spec = fsd?.GetFSDSpec();

            if (spec != null)
            {
                foreach (ShipModule sm in Modules.Values)
                {
                    int classpos;
                    if (sm.Item.Contains("Guardian FSD Booster") && (classpos = sm.Item.IndexOf("Class ")) != -1)
                    {
                        spec.SetGuardianFSDBooster(sm.Item[classpos + 6] - '0');
                        break;
                    }
                }
                return spec;
            }

            return null;
        }

        // current jump range or null if can't calc
        // if no parameters, uses maximum cargo and maximum fuel
        public double? GetJumpRange(int? cargo =null, double? fuel=null)
        {
            var fsd = GetFSDSpec();
            if (fsd != null)
            {
                if (cargo == null)
                    cargo = CargoCapacity();
                if (fuel == null)
                    fuel = FuelCapacity;

                var ji = fsd.GetJumpInfo(cargo.Value, ModuleMass() + HullMass(), fuel.Value, fuel.Value, 1.0);

                return ji.cursinglejump;
            }
            else
                return null;
        }

        public double ModuleMass()
        {
            //foreach( var x in Modules)  System.Diagnostics.Debug.WriteLine($"Module {x.Value.Item} mass {x.Value.Mass}");
            return (from var in Modules select var.Value.Mass).Sum();
        }

        public double HullMass()
        {
            ItemData.IModuleInfo md = ItemData.GetShipProperty(ShipFD, ItemData.ShipPropID.HullMass);
            return md != null ? (md as ItemData.ShipInfoDouble).Value : 0;
        }

        public double HullModuleMass()      // based on modules and hull, not on FDev unladen mass in loadout
        {
            return ModuleMass() + HullMass();
        }

        public double FuelWarningPercent
        {
            get { return EliteDangerousCore.DB.UserDatabase.Instance.GetSettingDouble("ShipInformation:" + ShipFD + ID.ToStringInvariant() + "Warninglevel", 0); }
            set { EliteDangerousCore.DB.UserDatabase.Instance.PutSettingDouble("ShipInformation:" + ShipFD + ID.ToStringInvariant() + "Warninglevel", value); }
        }

        public string Manufacturer
        {
            get
            {
                ItemData.IModuleInfo md = ItemData.GetShipProperty(ShipFD, ItemData.ShipPropID.Manu);
                return md != null ? (md as ItemData.ShipInfoString).Value : "Unknown";
            }
        }

        public double Boost
        {
            get
            {
                ItemData.IModuleInfo md = ItemData.GetShipProperty(ShipFD, ItemData.ShipPropID.Boost);
                double v = md != null ? (md as ItemData.ShipInfoInt).Value : 0;
                ShipModule.EngineeringData ed = GetEngineering(ShipSlots.Slot.MainEngines); // aka "MainEngines" in fd speak, but we use a slot naming conversion
                ed?.EngineerThrusters(ref v);
                return v;
            }
        }

        public double Speed
        {
            get
            {
                ItemData.IModuleInfo md = ItemData.GetShipProperty(ShipFD, ItemData.ShipPropID.Speed);
                double v = md != null ? (md as ItemData.ShipInfoInt).Value : 0;
                ShipModule.EngineeringData ed = GetEngineering(ShipSlots.Slot.MainEngines);
                ed?.EngineerThrusters(ref v);
                return v;
            }
        }

        public string PadSize
        {
            get
            {
                ItemData.IModuleInfo md = ItemData.GetShipProperty(ShipFD, ItemData.ShipPropID.Class);
                if (md == null)
                    return "Unknown";
                else
                {
                    int i = (md as ItemData.ShipInfoInt).Value;
                    if (i == 1)
                        return "Small";
                    else if (i == 2)
                        return "Medium";
                    else
                        return "Large";
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.AppendFormat("Ship {0}", ShipFullInfo());
            sb.Append(Environment.NewLine);
            foreach (ShipModule sm in Modules.Values)
            {
                sb.AppendFormat(sm.ToString());
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        #endregion

        #region Creating and changing

        public ShipInformation(ulong id)
        {
            ID = id;
            Modules = new Dictionary<ShipSlots.Slot, ShipModule>();
        }

        public ShipInformation ShallowClone()          // shallow clone.. does not clone the ship modules, just the dictionary
        {
            ShipInformation sm = new ShipInformation(this.ID);
            sm.State = this.State;
            sm.ShipType = this.ShipType;
            sm.ShipFD = this.ShipFD;
            sm.ShipUserName = this.ShipUserName;
            sm.ShipUserIdent = this.ShipUserIdent;
            sm.FuelLevel = this.FuelLevel;
            sm.FuelCapacity = this.FuelCapacity;
            sm.SubVehicle = this.SubVehicle;
            sm.HullValue = this.HullValue;
            sm.HullHealthAtLoadout = this.HullHealthAtLoadout;
            sm.ModulesValue = this.ModulesValue;
            sm.UnladenMass = this.UnladenMass;
            sm.Rebuy = this.Rebuy;
            sm.ReserveFuelCapacity = this.ReserveFuelCapacity;
            sm.StoredAtStation = this.StoredAtStation;
            sm.StoredAtSystem = this.StoredAtSystem;
            sm.TransferArrivalTimeUTC = this.TransferArrivalTimeUTC;
            sm.Hot = this.Hot;
            sm.Modules = new Dictionary<ShipSlots.Slot, ShipModule>(this.Modules);
            return sm;
        }

        public bool Contains(ShipSlots.Slot slot)
        {
            return Modules.ContainsKey(slot);
        }

        public bool Same(ShipModule sm)
        {
            if (Modules.ContainsKey(sm.SlotFD))
            {
                return Modules[sm.SlotFD].Same(sm);
            }
            else
                return false;
        }

        public void SetModule(ShipModule sm)                // changed the module array, so you should have cloned that first..
        {
            if (Modules.ContainsKey(sm.SlotFD))
            {
                ShipModule oldsm = Modules[sm.SlotFD];

                if (sm.Item.Equals(oldsm.Item) && sm.LocalisedItem == null && oldsm.LocalisedItem != null)  // if item the same, old one has a localised name..
                    sm.LocalisedItem = oldsm.LocalisedItem; // keep it

            }

            Modules[sm.SlotFD] = sm;

            if (sm.Item.Contains("Fuel Tank") && sm.Item.IndexOf("Class ") != -1)
            {
                FuelCapacity = GetFuelCapacity();
                if (FuelLevel > FuelCapacity)
                    FuelLevel = FuelCapacity;
            }
        }

        public ShipInformation SetShipDetails(string ship, string shipfd, string name = null, string ident = null, 
                                    double fuellevel = 0, double fueltotal = 0,
                                    long hullvalue = 0, long modulesvalue = 0, long rebuy = 0,
                                    double unladenmass = 0, double reservefuelcap = 0 , double hullhealth = 0, bool? hot = null)
        {
            bool s1 = ShipFD != shipfd;
            bool s2 = ship != ShipType;
            bool s3 = name != null && name != ShipUserName;
            bool s4 = ident != null && ident != ShipUserIdent;
            bool s5 = fuellevel != 0 && fuellevel != FuelLevel;
            bool s6 = fueltotal != 0 && fueltotal != FuelCapacity;
            bool s7 = hullvalue != 0 && hullvalue != HullValue;
            bool s8 = modulesvalue != 0 && modulesvalue != ModulesValue;
            bool s9 = rebuy != 0 && rebuy != Rebuy;
            bool s10 = unladenmass != 0 && unladenmass != UnladenMass;
            bool s11 = reservefuelcap != 0 && reservefuelcap != ReserveFuelCapacity;
            bool s12 = hullhealth != 0 && HullHealthAtLoadout != hullhealth;
            bool s13 = hot != null && hot.Value != Hot;

            if (s1 || s2 || s3 || s4 || s5 || s6 || s7 || s8 || s9 || s10 || s11 || s12 || s13 )
            {
                //System.Diagnostics.Debug.WriteLine($".. update SetShipDetails");

                ShipInformation sm = this.ShallowClone();

                sm.ShipType = ship;
                sm.ShipFD = shipfd;
                if (name.HasNonSpaceChars())        // seen " " as a name!
                    sm.ShipUserName = name;
                if (ident.HasNonSpaceChars())
                    sm.ShipUserIdent = ident;
                if (fuellevel != 0)
                    sm.FuelLevel = fuellevel;
                if (fueltotal == 0 && fuellevel > sm.FuelCapacity)
                    sm.FuelCapacity = fuellevel;
                if (fueltotal != 0)
                    sm.FuelCapacity = fueltotal;
                if (hullvalue != 0)
                    sm.HullValue = hullvalue;
                if (modulesvalue != 0)
                    sm.ModulesValue = modulesvalue;
                if (rebuy != 0)
                    sm.Rebuy = rebuy;
                if (unladenmass != 0)
                    sm.UnladenMass = unladenmass;
                if (reservefuelcap != 0)
                    sm.ReserveFuelCapacity = reservefuelcap;
                if (hullhealth != 0)
                    sm.HullHealthAtLoadout = hullhealth;

                if (hot != null)
                    sm.Hot = hot.Value;

                //System.Diagnostics.Debug.WriteLine(ship + " " + sm.FuelCapacity + " " + sm.FuelLevel + " " + sm.ReserveFuelCapacity);

                return sm;
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine($".. don't update SetShipDetails");
                return this;
            }
        }

        public ShipInformation SetSubVehicle(SubVehicleType vh)
        {
            if (vh != this.SubVehicle)
            {
                ShipInformation sm = this.ShallowClone();
                sm.SubVehicle = vh;
                return sm;
            }
            else
                return this;
        }

        public ShipInformation SetFuelLevel(double fuellevel)
        {
            if (fuellevel != 0 && fuellevel != FuelLevel)
            {
                ShipInformation sm = this.ShallowClone();

                if (fuellevel != 0)
                    sm.FuelLevel = fuellevel;
                if (fuellevel > sm.FuelCapacity)
                    sm.FuelCapacity = fuellevel;

                return sm;
            }

            return this;
        }

        public ShipInformation SetFuelLevel(double fuellevel, double reserve)       // fuellevel >=0 to set
        {
            if (fuellevel >= 0 && ( Math.Abs(FuelLevel - fuellevel) > 0.01 || Math.Abs(ReserveFuelCapacity - reserve) > 0.01))
            {
                //System.Diagnostics.Debug.WriteLine("Update ship fuel to " + fuellevel + " " + reserve);

                ShipInformation sm = this.ShallowClone();

                if (fuellevel != 0)
                    sm.FuelLevel = fuellevel;
                if (fuellevel > sm.FuelCapacity)
                    sm.FuelCapacity = fuellevel;
                sm.ReserveFuelCapacity = reserve;

                return sm;
            }

            return this;
        }

        public ShipInformation AddModule(string slot, ShipSlots.Slot slotfd, string item, string itemfd, string itemlocalised)
        {
            if (!Modules.ContainsKey(slotfd) || Modules[slotfd].Item.Equals(item) == false)       // if does not have it, or item is not the same..
            {
                ShipInformation sm = this.ShallowClone();
                sm.Modules[slotfd] = new ShipModule(slot, slotfd, item, itemfd, itemlocalised);
                //System.Diagnostics.Debug.WriteLine("Slot add " + slot);

                if (item.Contains("Fuel Tank") && item.IndexOf("Class ") != -1)
                {
                    sm.FuelCapacity = sm.GetFuelCapacity();
                    if (sm.FuelLevel > sm.FuelCapacity)
                        sm.FuelLevel = sm.FuelCapacity;
                }

                return sm;
            }
            return this;
        }

        public ShipInformation RemoveModule(ShipSlots.Slot slot, string item)
        {
            if (Modules.ContainsKey(slot))       // if has it..
            {
                ShipInformation sm = this.ShallowClone();
                sm.Modules.Remove(slot);
                //System.Diagnostics.Debug.WriteLine("Slot remove " + slot);

                if (item.Contains("Fuel Tank") && item.IndexOf("Class ") != -1)
                {
                    sm.FuelCapacity = sm.GetFuelCapacity();
                    if (sm.FuelLevel > sm.FuelCapacity)
                        sm.FuelLevel = sm.FuelCapacity;
                }

                return sm;
            }
            return this;
        }

        public ShipInformation RemoveModules(JournalMassModuleStore.ModuleItem[] items)
        {
            ShipInformation sm = null;
            foreach (var it in items)
            {
                if (Modules.ContainsKey(it.SlotFD))       // if has it..
                {
                    if (sm == null)
                        sm = this.ShallowClone();

                    //System.Diagnostics.Debug.WriteLine("Slot mass remove " + it.Slot + " Exists " + sm.Modules.ContainsKey(it.Slot));
                    sm.Modules.Remove(it.SlotFD);

                    if (it.Name.Contains("Fuel Tank") && it.Name.IndexOf("Class ") != -1)
                    {
                        sm.FuelCapacity = sm.GetFuelCapacity();
                        if (sm.FuelLevel > sm.FuelCapacity)
                            sm.FuelLevel = sm.FuelCapacity;
                    }
                }
            }

            return sm ?? this;
        }

        public ShipInformation SwapModule(string fromslot, ShipSlots.Slot fromslotfd, string fromitem, string fromitemfd, string fromiteml,
                                          string toslot, ShipSlots.Slot toslotfd, string toitem, string toitemfd, string toiteml)
        {
            ShipInformation sm = this.ShallowClone();
            if (Modules.ContainsKey(fromslotfd))
            {
                if (Modules.ContainsKey(toslotfd))
                {
                    sm.Modules[fromslotfd] = new ShipModule(fromslot, fromslotfd, toitem, toitemfd, toiteml);
                }
                else
                    sm.Modules.Remove(fromslotfd);

                sm.Modules[toslotfd] = new ShipModule(toslot, toslotfd, fromitem, fromitemfd, fromiteml);

                if (fromitem != toitem && ((fromitem.Contains("Fuel Tank") && fromitem.IndexOf("Class ") != -1) ||
                                           (fromitem.Contains("Fuel Tank") && fromitem.IndexOf("Class ") != -1)))
                {
                    sm.FuelCapacity = sm.GetFuelCapacity();
                    if (sm.FuelLevel > sm.FuelCapacity)
                        sm.FuelLevel = sm.FuelCapacity;
                }
            }
            return sm;
        }

        public ShipInformation Craft(ShipSlots.Slot slotfd, string item, ShipModule.EngineeringData eng)
        {
            if (Modules.ContainsKey(slotfd) && Modules[slotfd].Item.Equals(item))       // craft, module must be there, otherwise just ignore
            {
                ShipInformation sm = this.ShallowClone();
                sm.Modules[slotfd] = new ShipModule(sm.Modules[slotfd]);        // clone
                sm.Modules[slotfd].SetEngineering(eng);                       // and update engineering
                return sm;
            }

            return this;
        }

        public ShipInformation SellShip()
        {
            ShipInformation sm = this.ShallowClone();
            sm.State = ShipState.Sold;
            sm.SubVehicle = SubVehicleType.None;
            sm.ClearStorage();
            return sm;
        }

        public ShipInformation Destroyed()
        {
            ShipInformation sm = this.ShallowClone();
            sm.State = ShipState.Destroyed;
            sm.SubVehicle = SubVehicleType.None;
            sm.ClearStorage();
            return sm;
        }

        public ShipInformation Store(string station, string system)
        {
            ShipInformation sm = this.ShallowClone();
            //if (sm.StoredAtSystem != null) { if (sm.StoredAtSystem.Equals(system)) System.Diagnostics.Debug.WriteLine("..Previous known stored at" + sm.StoredAtSystem + ":" + sm.StoredAtStation); else System.Diagnostics.Debug.WriteLine("************************ DISGREEE..Previous known stored at" + sm.StoredAtSystem + ":" + sm.StoredAtStation); }
            sm.SubVehicle = SubVehicleType.None;
            sm.StoredAtSystem = system;
            sm.StoredAtStation = station ?? sm.StoredAtStation;     // we may get one with just the system, so use the previous station if we have one
            //System.Diagnostics.Debug.WriteLine(".." + ShipFD + " Stored at " + sm.StoredAtSystem + ":" + sm.StoredAtStation);
            return sm;                                              // don't change transfer time as it may be in progress..
        }

        public ShipInformation SwapTo()
        {
            ShipInformation sm = this.ShallowClone();
            sm.ClearStorage();    // just in case
            return sm;
        }

        public ShipInformation Transfer(string tosystem , string tostation, DateTime arrivaltimeutc)
        {
            ShipInformation sm = this.ShallowClone();
            sm.StoredAtStation = tostation;
            sm.StoredAtSystem = tosystem;
            sm.TransferArrivalTimeUTC = arrivaltimeutc;
            return sm;
        }

        private void ClearStorage()
        {
            StoredAtStation = StoredAtSystem = null;
            TransferArrivalTimeUTC = DateTime.MinValue;
        }

        #endregion

        #region Export

        public bool CheckMinimumJSONModules()
        {
            // these are required slots..
            string[] requiredmodules = { "PowerPlant", "MainEngines", "FrameShiftDrive", "LifeSupport", "PowerDistributor", "Radar", "FuelTank", "Armour" };
            int reqmodules = 0;

            foreach (ShipModule sm in Modules.Values)
            {
                int index = Array.FindIndex(requiredmodules, x => x.Equals(sm.SlotFD));
                if (index >= 0)
                    reqmodules |= (1 << index);     // bit map them in, the old fashioned way
            }

            return (reqmodules == (1 << requiredmodules.Length) - 1);
        }

        public string ToJSONCoriolis(out string errstring)
        {
            return JSONCoriolis(out errstring).ToString();
        }
        
        public JObject JSONCoriolis(out string errstring)
        {
            errstring = "";

            JObject jo = new JObject();

            jo["event"] = "Loadout";
            jo["Ship"] = ShipFD;

            JArray mlist = new JArray();
            foreach (ShipModule sm in Modules.Values)
            {
                JObject module = new JObject();

                if (ItemData.TryGetShipModule(sm.ItemFD, out ItemData.ShipModule si, false) && si.ModuleID != 0)   // don't synth it
                {
                    module["Item"] = sm.ItemFD;
                    module["Slot"] = sm.SlotFD.ToString();
                    module["On"] = sm.Enabled.HasValue ? sm.Enabled : true;
                    module["Priority"] = sm.Priority.HasValue ? sm.Priority : 0;

                    if (sm.Engineering != null)
                        module["Engineering"] = ToJsonCoriolisEngineering(sm);

                    mlist.Add(module);
                }
                else
                {
                    errstring += sm.Item + ":" + sm.ItemFD + Environment.NewLine;
                }
            }

            jo["Modules"] = mlist;

            return jo;
        }

        private JObject ToJsonCoriolisEngineering(ShipModule module)
        {
            JObject engineering = new JObject();

            engineering["BlueprintID"] = module.Engineering.BlueprintID;
            engineering["BlueprintName"] = module.Engineering.BlueprintName;
            engineering["Level"] = module.Engineering.Level;
            engineering["Quality"] = module.Engineering.Quality;

            if (module.Engineering.Modifiers != null) // may not have any
            {
                JArray modifiers = new JArray();
                foreach (ShipModule.EngineeringModifiers modifier in module.Engineering.Modifiers)
                {
                    JObject jmodifier = new JObject();
                    jmodifier["Label"] = modifier.Label;
                    jmodifier["Value"] = modifier.Value;
                    jmodifier["OriginalValue"] = modifier.OriginalValue;
                    jmodifier["LessIsGood"] = modifier.LessIsGood;
                    modifiers.Add(jmodifier);
                }

                engineering["Modifiers"] = modifiers;
            }

            if (module.Engineering.ExperimentalEffect.HasChars() )
                engineering["ExperimentalEffect"] = module.Engineering.ExperimentalEffect;

            return engineering;
        }

        public string ToJSONLoadout()
        {
            return JSONLoadout().ToString();
        }

        public JObject JSONLoadout()
        {
            JObject jo = new JObject();

            jo["timestamp"] = DateTime.UtcNow.ToStringZuluInvariant();
            jo["event"] = "Loadout";
            jo["Ship"] = ShipFD;
            jo["ShipID"] = ID;
            if (!string.IsNullOrEmpty(ShipUserName))
                jo["ShipName"] = ShipUserName;
            if (!string.IsNullOrEmpty(ShipUserIdent))
                jo["ShipIdent"] = ShipUserIdent;
            if (HullValue > 0)
                jo["HullValue"] = HullValue;
            if (ModulesValue > 0)
                jo["ModulesValue"] = ModulesValue;
            if (HullHealthAtLoadout > 0)
                jo["HullHealth"] = HullHealthAtLoadout / 100.0;
            if (UnladenMass > 0)
                jo["UnladenMass"] = UnladenMass;
            jo["CargoCapacity"] = CargoCapacity();
            if (FuelCapacity > 0 && ReserveFuelCapacity > 0)
            {
                JObject fc = new JObject();
                fc["Main"] = FuelCapacity;
                fc["Reserve"] = ReserveFuelCapacity;
                jo["FuelCapacity"] = fc;
            }
            if (Rebuy > 0)
                jo["Rebuy"] = Rebuy;

            JArray mlist = new JArray();

            foreach (ShipModule sm in Modules.Values)
            {
                JObject module = new JObject();

                module["Slot"] = sm.SlotFD.ToString();
                module["Item"] = sm.ItemFD;
                module["On"] = sm.Enabled.HasValue ? sm.Enabled : true;
                module["Priority"] = sm.Priority.HasValue ? sm.Priority : 0;

                if (sm.Value.HasValue)
                    module["Value"] = sm.Value;

                if ( sm.Engineering != null )
                    module["Engineering"] = sm.Engineering.ToJSONLoadout();

                mlist.Add(module);
            }

            jo["Modules"] = mlist;

            return jo;
        }

        #endregion
    }
}

