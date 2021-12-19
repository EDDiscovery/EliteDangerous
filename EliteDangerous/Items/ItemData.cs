﻿/*
 * Copyright © 2016-2021 EDDiscovery development team
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
 *
 * Data courtesy of Coriolis.IO https://github.com/EDCD/coriolis , data is intellectual property and copyright of Frontier Developments plc ('Frontier', 'Frontier Developments') and are subject to their terms and conditions.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace EliteDangerousCore
{
    // an Item can be a ship, a ship module, a suit module, etc

    public class ItemData
    {
        static ItemData instance = null;

        private ItemData()
        {
        }

        public static ItemData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ItemData();
                }
                return instance;
            }
        }

        public enum ShipPropID { FDID, HullMass, Name, Manu, Speed, Boost, HullCost, Class }

        public Dictionary<ShipPropID, IModuleInfo> GetShipProperties(string fdshipname)        // get properties of a ship, case insensitive, may be null
        {
            fdshipname = fdshipname.ToLowerInvariant();
            if (coriolisships.ContainsKey(fdshipname))
                return coriolisships[fdshipname];
            else if (noncoriolisships.ContainsKey(fdshipname))
                return noncoriolisships[fdshipname];
            else
                return null;
        }

        public IModuleInfo GetShipProperty(string fdshipname, ShipPropID property)        // get property of a ship, case insensitive.  property is case sensitive.  May be null
        {
            Dictionary<ShipPropID, IModuleInfo> info = GetShipProperties(fdshipname);
            return info != null ? (info.ContainsKey(property) ? info[property] : null) : null;
        }

        public ShipModule GetShipModuleProperties(string fdid)        // given an item name, return its ShipModule properties (id, mass, names). Always returns one
        {
            string lowername = fdid.ToLowerInvariant();

            ShipModule m = null;

            if (modules.ContainsKey(lowername))
            {
                //System.Diagnostics.Debug.WriteLine("Module item " + fdid + " >> " + (modules[lowername] as ShipModule).modname + " " + (modules[lowername] as ShipModule).moduleid);
                m = modules[lowername];
            }
            else if (noncorolismodules.ContainsKey(lowername))
            {
                m = noncorolismodules[lowername];
            }
            else if (synthesisedmodules.ContainsKey(lowername))
            {
                m = synthesisedmodules[lowername];
            }
            else
            {                                                           // synthesise one
                string candidatename = fdid;
                candidatename = candidatename.Replace("weaponcustomisation", "WeaponCustomisation").Replace("testbuggy", "SRV").
                                        Replace("enginecustomisation", "EngineCustomisation");

                candidatename = candidatename.SplitCapsWordFull();

                m = new ShipModule(-1, 0, candidatename, IsVanity(lowername) ? VanityType : UnknownType);

                System.Diagnostics.Debug.WriteLine("Unknown Module { \"" + lowername + "\", new ShipModule(-1,0, \"" + m.ModName + "\", " + (IsVanity(lowername) ? "VanityType" : "UnknownType") + " ) },");

                synthesisedmodules.Add(lowername, m);                   // lets cache them for completeness..

                //            string line = "{ \"" + lowername + "\", new ShipModule( -1, 0 , \"" + m.modname + "\",\"" + m.modtype + "\") },";     // DEBUG
                //            if (!synthresponses.Contains(line))  synthresponses.Add(line);
            }

            // System.Diagnostics.Debug.WriteLine("Module item " + fdid + " >> " + m.ModName + " " + m.ModType + " " + m.ModuleID);
            return m;
        }

        //List<string> synthresponses = new List<string>();               // KEEP ..  for debugging and collecting new items, comment out above
        //public void DumpItems()
        //{
        //    synthresponses.Sort();
        //    foreach (var s in synthresponses)
        //        System.Diagnostics.Debug.WriteLine(s);
        //}

        private Dictionary<string, ShipModule> synthesisedmodules = new Dictionary<string, ShipModule>();

        private const string VanityType = "Vanity Item";
        private const string UnknownType = "Unknown Module";
        private const string CockpitType = "Cockpit";
        private const string CargoBayDoorType = "Cargo Bay Door";
        private const string WearAndTearType = "Wear and Tear";
        private const string StellarScanner = "Scanners";

        public List<string> GetAllModTypes(bool removenonbuyable = true)                            // all module types..
        {
            List<ShipModule> ret = new List<ShipModule>(modules.Values);
            ret.AddRange(noncorolismodules.Values);
            var slist = (from x in ret orderby x.ModType select x.ModType).Distinct().ToList();
            slist.Add(UnknownType);                 // account for this..
            if (removenonbuyable)
            {
                slist.Remove(VanityType);               // can't buy
                slist.Remove(CockpitType);              // can't buy
                slist.Remove(CargoBayDoorType);
                slist.Remove(WearAndTearType);
            }
            return slist;
        }

        static public bool IsVanity(string ifd)
        {
            ifd = ifd.ToLowerInvariant();
            string[] vlist = new[] { "bobble", "decal", "enginecustomisation", "nameplate", "paintjob",
                                    "shipkit", "weaponcustomisation", "voicepack" , "lights" };
            return Array.Find(vlist, x => ifd.Contains(x)) != null;
        }

        static public bool IsSuit(string ifd)       // If a suit..
        {
            return ifd.Contains("suit", StringComparison.InvariantCultureIgnoreCase);
        }

        static public bool IsTaxi(string ifd)       // If a taxi
        {
            return ifd.Contains("_taxi", StringComparison.InvariantCultureIgnoreCase);
        }

        static public bool IsShip(string ifd)      // any which are not one of the others is called a ship, to allow for new unknown ships
        {
            return ifd.HasChars() && !IsSRVOrFighter(ifd) && !IsSuit(ifd) && !IsTaxi(ifd);
        }

        static public bool IsShipSRVOrFighter(string ifd)
        {
            return ifd.HasChars() && !IsSuit(ifd) && !IsTaxi(ifd);
        }

        static public bool IsSRV(string ifd)
        {
            return ifd.Equals("testbuggy", StringComparison.InvariantCultureIgnoreCase) || ifd.Contains("_SRV", StringComparison.InvariantCultureIgnoreCase);
        }


        static public bool IsFighter(string ifd)
        {
            ifd = ifd.ToLowerInvariant();
            return ifd.Equals("federation_fighter") || ifd.Equals("empire_fighter") || ifd.Equals("independent_fighter") || ifd.Contains("hybrid_fighter");
        }

        static public bool IsSRVOrFighter(string ifd)
        {
            return IsSRV(ifd) || IsFighter(ifd);
        }

        static public Actor GetActor(string fdname, string locname = null)         // actors are thinks like skimmer drones
        {
            fdname = fdname.ToLowerInvariant();
            if (actors.TryGetValue(fdname, out Actor var))
                return var;
            else
            {
                System.Diagnostics.Debug.WriteLine("Unknown Actor: {{ \"{0}\", new Weapon(\"{1}\") }},", fdname, locname ?? fdname.SplitCapsWordFull());
                return null;
            }
        }

        static public Weapon GetWeapon(string fdname, string locname = null)         // suit weapons
        {
            fdname = fdname.ToLowerInvariant();
            if (weapons.TryGetValue(fdname, out Weapon var))
                return var;
            else
            {
                System.Diagnostics.Debug.WriteLine("Unknown Weapon: {{ \"{0}\", new Weapon(\"{1}\",0.0) }},", fdname, locname ?? fdname.SplitCapsWordFull());
                return null;
            }
        }

        static public Suit GetSuit(string fdname, string locname = null)         // suit weapons
        {
            fdname = fdname.ToLowerInvariant();
            if (suit.TryGetValue(fdname, out Suit var))
                return var;
            else
            {
                System.Diagnostics.Debug.WriteLine("Unknown Suit: {{ \"{0}\", new Suit(\"{1}\") }},", fdname, locname ?? fdname.SplitCapsWordFull());
                return null;
            }
        }


        #region classes

        public interface IModuleInfo
        {
        };

        public class ShipModule : IModuleInfo
        {
            public int ModuleID;
            public double Mass;
            public string ModName;
            public string ModType;
            public double Power;
            public string Info;
            public ShipModule(int id, double m, string n, string t) { ModuleID = id; Mass = m; ModName = n; ModType = t; }
            public ShipModule(int id, double m, double p, string n, string t) { ModuleID = id; Mass = m; Power = p; ModName = n; ModType = t; }
            public ShipModule(int id, double m, double p, string i, string n, string t) { ModuleID = id; Mass = m; Power = p; Info = i; ModName = n; ModType = t; }

            public string InfoMassPower(bool mass)
            {
                string i = (Info ?? "").AppendPrePad(Power > 0 ? ("Power:" + Power.ToString("0.#MW")) : "", ", ");
                if (mass)
                    return i.AppendPrePad(Mass > 0 ? ("Mass:" + Mass.ToString("0.#t")) : "", ", ");
                else
                    return i;
            }

        };

        public class ShipInfoString : IModuleInfo
        {
            public string Value;
            public ShipInfoString(string s) { Value = s; }
        };
        public class ShipInfoInt : IModuleInfo
        {
            public int Value;
            public ShipInfoInt(int i) { Value = i; }
        };
        public class ShipInfoDouble : IModuleInfo
        {
            public double Value;
            public ShipInfoDouble(double d) { Value = d; }
        };
        public class Actor : IModuleInfo
        {
            public string Name;
            public Actor(string name) { Name = name; }
        }

        public class WeaponStats
        {
            public double DPS;
            public double RatePerSec;
            public int ClipSize;
            public int HopperSize;
            public int Range;
            public WeaponStats(double dps, double rate, int clip, int hoppersize, int range) { DPS = dps; RatePerSec = rate; ClipSize = clip; HopperSize = hoppersize; Range = range; }

        }
        public class Weapon : IModuleInfo
        {
            public string Name;
            public bool Primary;
            public enum WeaponClass { Launcher, Carbine, LongRangeRifle, Rifle, ShotGun, Pistol }
            public enum WeaponDamageType { Thermal, Plasma, Kinetic, Explosive }
            public enum WeaponFireMode { Automatic, SemiAutomatic, Burst }
            public WeaponClass Class;
            public WeaponDamageType DamageType;
            public WeaponFireMode FireMode;
            public WeaponStats[] Stats;     // 5 classes,0 to 4

            public WeaponStats GetStats(int cls) // 1 to 5
            {
                if (cls >= 1 && cls <= 5)
                    return Stats[cls - 1];
                else
                    return null;
            }


            public Weapon(string name, bool primary, WeaponDamageType ty, WeaponClass ds, WeaponFireMode fr, WeaponStats[] values)
            {
                Name = name;
                Primary = primary;
                DamageType = ty;
                Class = ds;
                FireMode = fr;
                Stats = values;
            }
        }

        public class SuitStats
        {
            public double HealthMultiplierKinetic;
            public double HealthMultiplierThermal;
            public double HealthMultiplierPlasma;
            public double HealthMultiplierExplosive;
            public double ShieldMultiplierKinetic;
            public double ShieldMultiplierThermal;
            public double ShieldMultiplierPlasma;
            public double ShieldMultiplierExplosive;
            public double ShieldRegen;     // BSH*OM = shield
            public double Shield;     // BSH*OM = shield
            public double EnergyCap;
            public int OxygenTime;
            public int ItemCap;
            public int ComponentCap;
            public int DataCap;
            public SuitStats(double hk, double ht, double hp, double he,
                             double sk, double st, double sp, double se,
                             double sregen, double stot, double ec, int o, int i, int c, int d)
            {
                HealthMultiplierKinetic = hk;
                HealthMultiplierThermal = ht;
                HealthMultiplierPlasma = hp;
                HealthMultiplierExplosive = he;
                ShieldMultiplierKinetic = hk;
                ShieldMultiplierThermal = ht;
                ShieldMultiplierPlasma = hp;
                ShieldMultiplierExplosive = he;
                ShieldRegen = sregen;
                Shield = stot;     // BSH*OM
                EnergyCap = ec;
                OxygenTime= o;
                ItemCap= i;
                ComponentCap=c;
                DataCap= d;
            }
        }

        public class Suit : IModuleInfo
        {
            public string Type;
            public int Class;
            public string Name;         // Name and class
            public int PrimaryWeapons;
            public int SecondaryWeapons;
            public string U1;
            public string U2;
            public string U3;
            public SuitStats Stats;   

            public Suit(string type, int cls, int primary, int secondary, string u1, string u2, string u3, SuitStats values)
            {
                Type = type; Class = cls; Name = type + (Class > 0 ? " Class " + Class.ToStringInvariant() : "");
                PrimaryWeapons = primary;
                SecondaryWeapons = secondary;
                U1 = u1;
                U2 = u2;
                U3 = u3;
                Stats = values;
            }
        }

        #endregion


        #region Not in Corolis Data

        static Dictionary<ShipPropID, IModuleInfo> imperial_fighter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Empire_Fighter")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Imperial Fighter")},
            { ShipPropID.Manu, new ShipInfoString("Gutamaya")},
            { ShipPropID.Speed, new ShipInfoInt(312)},
            { ShipPropID.Boost, new ShipInfoInt(540)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };

        static Dictionary<ShipPropID, IModuleInfo> federation_fighter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Federation_Fighter")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("F63 Condor")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(316)},
            { ShipPropID.Boost, new ShipInfoInt(536)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };


        static Dictionary<ShipPropID, IModuleInfo> taipan_fighter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Independent_Fighter")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Taipan")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(0)},
            { ShipPropID.Boost, new ShipInfoInt(0)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };

        static Dictionary<ShipPropID, IModuleInfo> GDN_Hybrid_v1_fighter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("GDN_Hybrid_Fighter_V1")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Guardian Hybrid Fighter V1")},
            { ShipPropID.Manu, new ShipInfoString("Unknown")},
            { ShipPropID.Speed, new ShipInfoInt(0)},
            { ShipPropID.Boost, new ShipInfoInt(0)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> GDN_Hybrid_v2_fighter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("GDN_Hybrid_Fighter_V2")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Guardian Hybrid Fighter V2")},
            { ShipPropID.Manu, new ShipInfoString("Unknown")},
            { ShipPropID.Speed, new ShipInfoInt(0)},
            { ShipPropID.Boost, new ShipInfoInt(0)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> GDN_Hybrid_v3_fighter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("GDN_Hybrid_Fighter_V3")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Guardian Hybrid Fighter V3")},
            { ShipPropID.Manu, new ShipInfoString("Unknown")},
            { ShipPropID.Speed, new ShipInfoInt(0)},
            { ShipPropID.Boost, new ShipInfoInt(0)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };

        static Dictionary<ShipPropID, IModuleInfo> srv = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("TestBuggy")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Scarab SRV")},
            { ShipPropID.Manu, new ShipInfoString("Vodel")},
            { ShipPropID.Speed, new ShipInfoInt(38)},
            { ShipPropID.Boost, new ShipInfoInt(38)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };

        static Dictionary<ShipPropID, IModuleInfo> combatsrv = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Combat_Multicrew_SRV_01")},
            { ShipPropID.HullMass, new ShipInfoDouble(0F)},
            { ShipPropID.Name, new ShipInfoString("Scorpion Combat SRV")},
            { ShipPropID.Manu, new ShipInfoString("Vodel")},
            { ShipPropID.Speed, new ShipInfoInt(32)},
            { ShipPropID.Boost, new ShipInfoInt(32)},
            { ShipPropID.HullCost, new ShipInfoInt(0)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };

        static Dictionary<string, Dictionary<ShipPropID, IModuleInfo>> noncoriolisships = new Dictionary<string, Dictionary<ShipPropID, IModuleInfo>>
        {
            { "empire_fighter",  imperial_fighter},
            { "federation_fighter",  federation_fighter},
            { "independent_fighter",  taipan_fighter},       //EDDI evidence
            { "testbuggy",  srv},
            { "combat_multicrew_srv_01",  combatsrv},
            { "gdn_hybrid_fighter_v1",  GDN_Hybrid_v1_fighter},
            { "gdn_hybrid_fighter_v2",  GDN_Hybrid_v2_fighter},
            { "gdn_hybrid_fighter_v3",  GDN_Hybrid_v3_fighter},
        };

        #endregion

        #region Other fdnames

        public static Dictionary<string, Actor> actors = new Dictionary<string, Actor>   // DO NOT USE DIRECTLY - public is for checking only
        {
             { "skimmerdrone", new Actor("Skimmer Drone") },
             { "ps_turretbasemedium02_6m", new Actor("Turret medium 2-6-M") },
        };

        public static Dictionary<string, Weapon> weapons = new Dictionary<string, Weapon>   // DO NOT USE DIRECTLY - public is for checking only
        {
             { "wpn_m_assaultrifle_kinetic_fauto", new Weapon("Karma AR-50", true, Weapon.WeaponDamageType.Kinetic, Weapon.WeaponClass.LongRangeRifle, Weapon.WeaponFireMode.Automatic,
                             new WeaponStats[] {  new WeaponStats(0.9,10,40,240,50), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0),  new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

             { "wpn_m_assaultrifle_laser_fauto", new Weapon("TK Aphelion", true, Weapon.WeaponDamageType.Thermal, Weapon.WeaponClass.Rifle, Weapon.WeaponFireMode.Automatic,
                                new WeaponStats[] { new WeaponStats(1.6,5.7,25,150,70), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

            { "wpn_m_assaultrifle_plasma_fauto", new Weapon("Manticore Oppressor", true, Weapon.WeaponDamageType.Plasma, Weapon.WeaponClass.Rifle, Weapon.WeaponFireMode.Automatic,
                                new WeaponStats[] { new WeaponStats(0.8,6.7,50,300,35), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

            { "wpn_m_launcher_rocket_sauto", new Weapon("Karma L-6", true, Weapon.WeaponDamageType.Explosive, Weapon.WeaponClass.Launcher, Weapon.WeaponFireMode.Automatic,
                                new WeaponStats[] { new WeaponStats(40,1,2,8,300), new WeaponStats(0,0,0,0,0), new WeaponStats(69.2,1,2,8,300), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

            { "wpn_m_shotgun_plasma_doublebarrel", new Weapon("Manticore Intimidator", true,  Weapon.WeaponDamageType.Plasma, Weapon.WeaponClass.ShotGun, Weapon.WeaponFireMode.SemiAutomatic,
                                new WeaponStats[] { new WeaponStats(1.8,1.25,2,24,7), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

            { "wpn_m_sniper_plasma_charged", new Weapon("Manticore Executioner", true, Weapon.WeaponDamageType.Plasma, Weapon.WeaponClass.LongRangeRifle, Weapon.WeaponFireMode.SemiAutomatic,
                                new WeaponStats[] { new WeaponStats(15,0.8,3,30,100), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

             { "wpn_m_submachinegun_kinetic_fauto", new Weapon("Karma C-44", true, Weapon.WeaponDamageType.Kinetic, Weapon.WeaponClass.Carbine, Weapon.WeaponFireMode.Automatic,
                                    new WeaponStats[] { new WeaponStats(0.65,13.3,60,360,20), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

             // TBD range
             { "wpn_m_submachinegun_laser_fauto", new Weapon("TK Eclipse", true, Weapon.WeaponDamageType.Thermal, Weapon.WeaponClass.Carbine, Weapon.WeaponFireMode.Automatic,
                    new WeaponStats[] { new WeaponStats(0.9,10,40,280,25), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

             { "wpn_s_pistol_kinetic_sauto", new Weapon("Karma P-15", false, Weapon.WeaponDamageType.Kinetic, Weapon.WeaponClass.Pistol, Weapon.WeaponFireMode.SemiAutomatic,
                                         new WeaponStats[] { new WeaponStats(1.4,10,24,240,25), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

             { "wpn_s_pistol_laser_sauto", new Weapon("TK Zenith", false, Weapon.WeaponDamageType.Thermal, Weapon.WeaponClass.Pistol, Weapon.WeaponFireMode.Burst,
                                                new WeaponStats[] { new WeaponStats(1.7,2.7,18,180,35), new WeaponStats(2.2,5.7,18,180,35), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

             // TBD range
            { "wpn_s_pistol_plasma_charged", new Weapon("Manticore Tormentor", false, Weapon.WeaponDamageType.Plasma, Weapon.WeaponClass.Pistol, Weapon.WeaponFireMode.SemiAutomatic,
                            new WeaponStats[] { new WeaponStats(7.5,1.7,6,72,15), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0), new WeaponStats(0,0,0,0,0) }) },

        };

        // rob checked 20/8/21 for all suits to class 3 in game, class 4/5 according to wiki

        public static Dictionary<string, Suit> suit = new Dictionary<string, Suit>   // DO NOT USE DIRECTLY - public is for checking only
        {
                 { "flightsuit", new Suit( "Flight Suit", 0, 0, 1, "Energylink", "Profile Analyser", "",
                new SuitStats( 1.7, 0.6, 1.2, 1, // health kinetic, thermal, plasma, explosive                  Greater is WORSE, so 1.7 is 70% worse
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                0.55, 7.5, // regen, shield health  
                7, 60, 5,10,10 )) }, // battery, oxygen, items, components, data     

                 { "tacticalsuit_class1", new Suit( "Dominator Suit", 1, 2, 1, "Energylink", "Profile Analyser", "",
                new SuitStats( 1.5, 0.4, 1, 1, // health kinetic, thermal, plasma, explosive : correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.1, 15, // regen, shield health : correct
                10, 60, 5,10,10 )) }, // battery, oxygen, items, components, data : correct

                 { "tacticalsuit_class2", new Suit( "Dominator Suit", 2, 2, 1, "Energylink", "Profile Analyser", "",
                new SuitStats( 1.26, 0.34, 0.84, 0.84, // health kinetic, thermal, plasma, explosive : correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.34, 18.3, // regen, shield health correct
                10, 60, 5,10,10 )) }, // battery, oxygen, items, components, data correct

                 { "tacticalsuit_class3", new Suit( "Dominator Suit", 3, 2, 1, "Energylink", "Profile Analyser", "",
                new SuitStats( 1.07, 0.28, 0.71, 0.71, // health kinetic, thermal, plasma, explosive : correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.65, 22.5, // regen, shield health correct
                10, 60, 5,10,10 )) }, // battery, oxygen, items, components, data correct

                 { "tacticalsuit_class4", new Suit( "Dominator Suit", 4, 2, 1, "Energylink", "Profile Analyser", "",
                new SuitStats( 0.89, 0.24, 0.59, 0.59, // health kinetic, thermal, plasma, explosive
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                2.02, 27.6, // regen, shield health matches https://elite-dangerous.fandom.com/wiki/Artemis_Suit
                10, 60, 5,10,10 )) }, // battery, oxygen, items, components, data

                 { "tacticalsuit_class5", new Suit( "Dominator Suit", 5, 2, 1, "Energylink", "Profile Analyser", "",
                new SuitStats( 0.75, 0.2, 0.5, 0.5, // health kinetic, thermal, plasma, explosive
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                2.48, 33.8, // regen, shield health matches https://elite-dangerous.fandom.com/wiki/Artemis_Suit
                10, 60, 5,10,10 )) }, // battery, oxygen, items, components, data

                 { "explorationsuit_class1", new Suit( "Artemis Suit", 1, 1, 1, "Energylink", "Profile Analyser", "Genetic Sampler",
                new SuitStats( 1.7, 0.6, 1.2, 1, // health kinetic, thermal, plasma, explosive : Correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                0.88, 12, // regen, shield health : wrong in frontier data, game says 0.88,12
                17, 60, 10,20,10 )) }, // battery, oxygen, items, components, data : correct

                 { "explorationsuit_class2", new Suit( "Artemis Suit", 2, 1, 1, "Energylink", "Profile Analyser", "Genetic Sampler",
                new SuitStats( 1.43, 0.5, 1.01, 0.84, // health kinetic, thermal, plasma, explosive : correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.07, 14.7, // regen, shield health : wrong in frontier data, game says 1.07,14.7
                17, 60, 10,20,10 )) }, // battery, oxygen, items, components, data : correct

                 { "explorationsuit_class3", new Suit( "Artemis Suit", 3, 1, 1, "Energylink", "Profile Analyser", "Genetic Sampler",
                new SuitStats( 1.21, 0.43, 0.85, 0.71, // health kinetic, thermal, plasma, explosive Correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.32, 18, // regen, shield health wrong in frontier data. fixed to game data
                17, 60, 10,20,10 )) }, // battery, oxygen, items, components, data Correct

                 { "explorationsuit_class4", new Suit( "Artemis Suit", 4, 1, 1, "Energylink", "Profile Analyser", "Genetic Sampler",
                new SuitStats( 1, 0.35, 0.71, 0.59, // health kinetic, thermal, plasma, explosive
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.62, 22.1, // regen, shield health - wrong in frontier data, corrected according to https://elite-dangerous.fandom.com/wiki/Artemis_Suit
                17, 60, 10,20,10 )) }, // battery, oxygen, items, components, data

                 { "explorationsuit_class5", new Suit( "Artemis Suit", 5, 1, 1, "Energylink", "Profile Analyser", "Genetic Sampler",
                new SuitStats( 0.85, 0.3, 0.6, 0.5, // health kinetic, thermal, plasma, explosive
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.98, 27, // regen, shield health - Artie supplied this one via discord
                17, 60, 10,20,10 )) }, // battery, oxygen, items, components, data

                 { "utilitysuit_class1", new Suit( "Maverick Suit", 1, 1, 1, "Energylink", "Profile Analyser", "Arc Cutter",
                new SuitStats( 1.6, 0.5, 1.1, 1, // health kinetic, thermal, plasma, explosive : correct
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                0.99, 13.5, // regen, shield health wrong in frontier data, game says 0.99,13.5
                13.5, 60, 15,30,10 )) }, // battery, oxygen, items, components, data correct

                 { "utilitysuit_class2", new Suit( "Maverick Suit", 2, 1, 1, "Energylink", "Profile Analyser", "Arc Cutter",
                new SuitStats( 1.34, 0.42, 0.92, 0.84, // health kinetic, thermal, plasma, explosive : Correct   
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.21, 16.5, // regen, shield health wrong in frontier data, game says 1.21,16.5 
                13.5, 60, 15,30,10 )) }, // battery, oxygen, items, components, data correct 

                 { "utilitysuit_class3", new Suit( "Maverick Suit", 3, 1, 1, "Energylink", "Profile Analyser", "Arc Cutter",
                new SuitStats( 1.14, 0.36, 0.78, 0.71, // health kinetic, thermal, plasma, explosive        // 20/8/21
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.49, 20.3, // regen, shield health     // Wrong, game says 1.49, 20.3
                13.5, 60, 15,30,10 )) }, // battery, oxygen, items, components, data    // 20/8/21

                 { "utilitysuit_class4", new Suit( "Maverick Suit", 4, 1, 1, "Energylink", "Profile Analyser", "Arc Cutter",
                new SuitStats( 0.94, 0.3, 0.65, 0.59, // health kinetic, thermal, plasma, explosive
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                1.82, 24.9, // regen, shield health - wrong in frontier data, corrected according to https://elite-dangerous.fandom.com/wiki/Artemis_Suit
                13.5, 60, 15,30,10 )) }, // battery, oxygen, items, components, data

                 { "utilitysuit_class5", new Suit( "Maverick Suit", 5, 1, 1, "Energylink", "Profile Analyser", "Arc Cutter",
                new SuitStats( 0.8, 0.25, 0.55, 0.5, // health kinetic, thermal, plasma, explosive
                0.4, 1.5, 1, 0.5, // shield kinetic, thermal, plasma, explosive
                2.23, 30.5, // regen, shield health wrong in frontier data, corrected according to https://elite-dangerous.fandom.com/wiki/Artemis_Suit
                13.5, 60, 15,30,10 )) }, // battery, oxygen, items, components, data
         };

        #endregion

        #region Modules not in corolis

        public static Dictionary<string, ShipModule> noncorolismodules = new Dictionary<string, ShipModule>   // DO NOT USE DIRECTLY - public is for checking only
        {
            // Excel frontier data for 3.3 not in corolis yet..
            // mass guessed..

            { "null", new ShipModule(-1,0, "Error in frontier journal - Null module", UnknownType ) },

             { "hpt_cannon_turret_huge", new ShipModule(-1, 1, .9, "Cannon Turret Huge", "Cannon")},
             { "hpt_plasmaburstcannon_fixed_medium", new ShipModule(-1, 1, 1.4, "Plasma Burst Cannon Fixed Medium", "Plasma Accelerator")},
             { "hpt_pulselaserstealth_fixed_small", new ShipModule(-1, 1, .2, "Pulse Laser Stealth Fixed Small", "Pulse Laser")},
             { "hpt_pulselaser_fixed_smallfree", new ShipModule(-1, 1, .4, "Pulse Laser Fixed Small Free", "Pulse Laser")},
             { "int_codexscanner", new ShipModule(-1, 0, "Codex Scanner", "Codex")},

             // 3.6 missing modules from corolis

             { "hpt_dumbfiremissilerack_fixed_medium_advanced", new ShipModule(-1, 1, 1.2, "Dumbfire Missile Rack Fixed Medium Advanced", "Weapon")},
             { "hpt_dumbfiremissilerack_fixed_small_advanced", new ShipModule(-1, 1, .4, "Dumbfire Missile Rack Fixed Small Advanced", "Weapon")},
             { "hpt_guardiangauss_fixed_gdn_fighter", new ShipModule(-1, 1, 1, "Guardian Gauss Fixed GDN Fighter", "Weapon")},
             { "hpt_guardianplasma_fixed_gdn_fighter", new ShipModule(-1, 1, 1, "Guardian Plasma Fixed GDN Fighter", "Weapon")},
             { "hpt_guardianshard_fixed_gdn_fighter", new ShipModule(-1, 1, 1, "Guardian Shard Fixed GDN Fighter", "Weapon")},
             { "hpt_multicannon_fixed_medium_advanced", new ShipModule(-1, 1, .5, "Multi Cannon Fixed Medium Advanced", "Weapon")},
             { "hpt_multicannon_fixed_small_advanced", new ShipModule(-1, 1, .3, "Multi Cannon Fixed Small Advanced", "Weapon")},
             { "int_corrosionproofcargorack_size2_class1", new ShipModule(-1, 0, "Corrosion Resistant Cargo Rack", "Internal Module")},            

            // SRV

            { "scarab_armour_grade1", new ShipModule(-1,0,"SRV Armour","Armour")},
            { "int_shieldgenerator_size0_class3" , new ShipModule(-1,0, "SRV Shields" , "Shield Generator" ) },
            { "int_sensors_surface_size1_class1" , new ShipModule(-1,0, "SRV Sensors" , "Sensors" ) },
            { "vehicle_turretgun" , new ShipModule(-1,0, "SRV Turret" , "Pulse Laser" ) },
            { "int_sinewavescanner_size1_class1" , new ShipModule(-1,0, "SRV Scanner" , "Sensors" ) },
            { "int_powerdistributor_size0_class1" , new ShipModule(-1,0, "SRV Power Distributor" , "Power Distributor" ) },
            { "hpt_datalinkscanner" , new ShipModule(-1,0, "SRV Data Link Scanner" , "Sensors" ) },
            { "int_lifesupport_size0_class1" , new ShipModule(-1,0, "SRV Life Support" , "Life Support" ) },
            { "int_powerplant_size0_class1" , new ShipModule(-1,0, "SRV Powerplant" , "Powerplant" ) },
            { "int_fueltank_size0_class3" , new ShipModule(-1,0, "SRV Fuel Tank" , "Fuel Tank" ) },
            { "testbuggy_cockpit", new ShipModule( -1, 0 , "SRV Cockpit","Module") },
            { "buggycargobaydoor", new ShipModule( -1, 0 , "SRV Cargo Bay Door","Module") },

            // Repair items
            { "wear" , new ShipModule(-1,0, "Wear" ,  WearAndTearType ) },
            { "paint" , new ShipModule(-1,0, "Paint" ,  WearAndTearType ) },
            { "all" , new ShipModule(-1,0, "Repair All" ,  WearAndTearType ) },
            { "hull" , new ShipModule(-1,0, "Repair All" ,  WearAndTearType ) },

            // Fighters

            { "hpt_beamlaser_fixed_indie_fighter", new ShipModule(-1,0,1, "Beam Laser Fixed Indie Fighter", "Beam Laser" ) },
            { "hpt_beamlaser_fixed_empire_fighter", new ShipModule(-1,0,1, "Beam Laser Fixed Empire Fighter", "Beam Laser" ) },
            { "hpt_beamlaser_fixed_fed_fighter", new ShipModule(-1,0,1, "Beam Laser Fixed Federation Fighter", "Beam Laser" ) },

            { "hpt_beamlaser_gimbal_indie_fighter", new ShipModule(-1,0,1, "Beam Laser Gimbal Indie Fighter", "Beam Laser" ) },
            { "hpt_beamlaser_gimbal_empire_fighter", new ShipModule(-1,0,1, "Beam Laser Gimbal Empire Fighter", "Beam Laser" ) },
            { "hpt_beamlaser_gimbal_fed_fighter", new ShipModule(-1,0,1, "Beam Laser Gimbal Federation Fighter", "Beam Laser" ) },

            { "empire_fighter_armour_standard", new ShipModule(-1,0, "Empire Fighter Armour Standard", "Armour" ) },

            { "hpt_pulselaser_gimbal_fed_fighter", new ShipModule(-1,0, 1,"Pulse Laser Gimbal Federation Fighter", "Pulse Laser" ) },
            { "hpt_pulselaser_gimbal_indie_fighter", new ShipModule(-1,0, 1,"Pulse Laser Gimbal Indie Fighter", "Pulse Laser" ) },
            { "hpt_pulselaser_gimbal_empire_fighter", new ShipModule(-1,0, 1, "Pulse Laser Gimbal Empire Fighter", "Pulse Laser" ) },

            { "int_powerdistributor_fighter_class1", new ShipModule(-1,0, "Int Powerdistributor Fighter Class 1", "Power Distributor" ) },
            { "int_sensors_fighter_class1", new ShipModule(-1,0, "Int Sensors Fighter Class 1", "Sensors" ) },
            { "int_powerplant_fighter_class1", new ShipModule(-1,0, "Int Powerplant Fighter Class 1", "Powerplant" ) },

            { "hpt_pulselaser_fixed_fed_fighter", new ShipModule(-1,0, 1, "Pulse Laser Fixed Federation Fighter", "Pulse Laser" ) },
            { "hpt_pulselaser_fixed_indie_fighter", new ShipModule(-1,0, 1,"Pulse Laser Fixed Indie Fighter", "Pulse Laser" ) },
            { "hpt_pulselaser_fixed_empire_fighter", new ShipModule(-1,0, 1, "Pulse Laser Fixed Empire Fighter", "Pulse Laser" ) },

            { "int_shieldgenerator_fighter_class1", new ShipModule(-1,0, "Shield Generator Fighter Class 1", "Shields" ) },
            { "independent_fighter_armour_standard", new ShipModule(-1,0, "Independent Fighter Armour Standard", "Armour" ) },
            { "ext_emitter_standard", new ShipModule(-1,0, "Ext Emitter Standard", "Fighter Module" ) },
            { "hpt_shipdatalinkscanner", new ShipModule(-1,0, "Hpt Shipdatalinkscanner", "Data Link Scanner" ) },
            { "federation_fighter_armour_standard", new ShipModule(-1,0, "Federation Fighter Armour Standard", "Armour" ) },
            { "hpt_atmulticannon_fixed_indie_fighter", new ShipModule(-1,0, 1, "AX Multicannon Fixed Indie Fighter", "Multi Cannon" ) },

            { "hpt_multicannon_fixed_fed_fighter", new ShipModule(-1,0, 1,"Multicannon Fixed Fed Fighter", "Multi Cannon" )},
            { "hpt_multicannon_fixed_empire_fighter", new ShipModule(-1,0, 1,"Multicannon Fixed Empire Fighter", "Multi Cannon" )},
            { "hpt_multicannon_fixed_indie_fighter", new ShipModule(-1,0, 1,"Multicannon Fixed Indie Fighter", "Multi Cannon" )},

            { "hpt_plasmarepeater_fixed_empire_fighter", new ShipModule(-1,0,1, "Plasma Repeater Fixed Empire Fighter", "Plasma Gun" ) },
            { "hpt_plasmarepeater_fixed_fed_fighter", new ShipModule(-1,0,1, "Plasma Repeater Fixed Fed Fighter", "Plasma Gun" ) },
            { "hpt_plasmarepeater_fixed_indie_fighter", new ShipModule(-1,0,1, "Plasma Repeater Fixed Indie Fighter", "Plasma Gun" ) },

            { "int_engine_fighter_class1", new ShipModule(-1,1,1, "Fighter Engine Class 1", "Fighter Engine" ) },

            // Prisoner cells

            { "int_passengercabin_size2_class0", new ShipModule(-1, 2.5, 0, "Prisoners:2","Prison Cell", "Prison Cells")},
            { "int_passengercabin_size3_class0", new ShipModule(-1, 5, 0, "Prisoners:4","Prison Cell", "Prison Cells")},
            { "int_passengercabin_size4_class0", new ShipModule(-1, 10, 0, "Prisoners:8","Prison Cell", "Prison Cells")},
            { "int_passengercabin_size5_class0", new ShipModule(-1, 20, 0, "Prisoners:16","Prison Cell", "Prison Cells")},
            { "int_passengercabin_size6_class0", new ShipModule(-1, 40, 0, "Prisoners:32","Prison Cell", "Prison Cells")},

            // Shield Generators

            { "int_shieldgenerator_size1_class1", new ShipModule(-1, 1.3F, 0.72F, null,"Shield Generator Class 1 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size1_class2", new ShipModule(-1, 0.5F, 0.96F, null,"Shield Generator Class 1 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size1_class3", new ShipModule(-1, 1.3F, 1.2F, null,"Shield Generator Class 1 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size1_class4", new ShipModule(-1, 2F, 1.44F, null,"Shield Generator Class 1 Rating E", "Shield Generator")},
            
            // Cockpits

            { "adder_cockpit", new ShipModule( -1, 0 , "Adder Cockpit", CockpitType) },
            { "anaconda_cockpit", new ShipModule( -1, 0 , "Anaconda Cockpit", CockpitType) },
            { "asp_cockpit", new ShipModule( -1, 0 , "Asp Cockpit", CockpitType) },
            { "asp_scout_cockpit", new ShipModule( -1, 0 , "Asp Scout Cockpit", CockpitType) },
            { "belugaliner_cockpit", new ShipModule( -1, 0 , "Beluga Cockpit", CockpitType) },
            { "cobramkiii_cockpit", new ShipModule( -1, 0 , "Cobra Mk III Cockpit", CockpitType) },
            { "cobramkiv_cockpit",new ShipModule( -1, 0 , "Cobra Mk IV Cockpit", CockpitType) },
            { "cutter_cockpit", new ShipModule( -1, 0 , "Cutter Cockpit", CockpitType) },
            { "diamondbackxl_cockpit",new ShipModule( -1, 0 , "Diamondback Explorer Cockpit", CockpitType) },
            { "diamondback_cockpit",new ShipModule( -1, 0 , "Diamondback Scout Cockpit", CockpitType) },
            { "dolphin_cockpit",new ShipModule( -1, 0 , "Dolphin Cockpit", CockpitType) },
            { "eagle_cockpit",new ShipModule( -1, 0 , "Eagle Cockpit", CockpitType) },
            { "empire_eagle_cockpit", new ShipModule( -1, 0 , "Empire Eagle Cockpit", CockpitType) },
            { "empire_trader_cockpit", new ShipModule( -1, 0 , "Empire Trader Cockpit", CockpitType) },
            { "empire_courier_cockpit",new ShipModule( -1, 0 , "Empire Courier Cockpit", CockpitType) },
            { "federation_dropship_mkii_cockpit",new ShipModule( -1, 0 , "Federal Dropship Cockpit", CockpitType) },
            { "federation_corvette_cockpit",new ShipModule( -1, 0 , "Federal Corvette Cockpit", CockpitType) },
            { "federation_dropship_cockpit",new ShipModule( -1, 0 , "Federal Gunship Cockpit", CockpitType) },
            { "federation_gunship_cockpit",new ShipModule( -1, 0 , "Federal Gunship Cockpit", CockpitType) },
            { "ferdelance_cockpit", new ShipModule( -1, 0 , "Fer De Lance Cockpit", CockpitType) },
            { "krait_mkii_cockpit",new ShipModule( -1, 0 , "Krait MkII Cockpit", CockpitType) },
            { "hauler_cockpit", new ShipModule( -1, 0 , "Hauler Cockpit", CockpitType) },
            { "independant_trader_cockpit", new ShipModule( -1, 0 , "Independant Trader Cockpit", CockpitType) },
            { "orca_cockpit",new ShipModule( -1, 0 , "Orca Cockpit", CockpitType) },
            { "python_cockpit", new ShipModule( -1, 0 , "Python Cockpit", CockpitType) },
            { "sidewinder_cockpit", new ShipModule( -1, 0 , "Sidewinder Cockpit", CockpitType) },
            { "typex_3_cockpit",new ShipModule( -1, 0 , "Alliance Challenger Cockpit", CockpitType) },
            { "type6_cockpit",new ShipModule( -1, 0 , "Type 6 Cockpit", CockpitType) },
            { "type7_cockpit", new ShipModule( -1, 0 , "Type 7 Cockpit", CockpitType) },
            { "type9_cockpit", new ShipModule( -1, 0 , "Type 9 Cockpit", CockpitType) },
            { "type9_military_cockpit", new ShipModule( -1, 0 , "Type 9 Military Cockpit", CockpitType) },
            { "typex_cockpit",new ShipModule( -1, 0 , "Alliance Chieftain Cockpit", CockpitType) },
            { "viper_cockpit", new ShipModule( -1, 0 , "Viper Cockpit", CockpitType) },
            { "viper_mkiv_cockpit", new ShipModule( -1, 0 , "Viper Mk IV Cockpit", CockpitType) },
            { "vulture_cockpit", new ShipModule( -1, 0 , "Vulture Cockpit", CockpitType) },

            { "empire_fighter_cockpit", new ShipModule(-1,0, "Empire Fighter Cockpit", CockpitType ) },
            { "independent_fighter_cockpit", new ShipModule(-1,0, "Independent Fighter Cockpit", CockpitType ) },
            { "federation_fighter_cockpit", new ShipModule(-1,0, "Federation Fighter Cockpit", CockpitType ) },

            // Bay doors

            { "modularcargobaydoor", new ShipModule( -1, 0 , "Modular Cargo Bay Door", CargoBayDoorType ) },
            { "modularcargobaydoorfdl", new ShipModule( -1, 0 , "FDL Cargo Bay Door", CargoBayDoorType ) },

            // Previous ED's had a stellar body scanner - corolis removed it in 3.3
            { "int_stellarbodydiscoveryscanner_advanced", new ShipModule(128663561, 2, 0, null,"Stellar Body Discovery Scanner Advanced", "Stellar Body Discovery Scanner")},
            { "int_stellarbodydiscoveryscanner_intermediate", new ShipModule(128663560, 2, 0, "Range:1000ls","Stellar Body Discovery Scanner Intermediate", "Stellar Body Discovery Scanner")},
            { "int_stellarbodydiscoveryscanner_standard", new ShipModule(128662535, 2, 0, "Range:500ls","Stellar Body Discovery Scanner Standard", "Stellar Body Discovery Scanner")},

            // Vanities found in logs (not exhaustive)

            { "asp_shipkit1_bumper1", new ShipModule( -1, 0 , "Asp Shipkit 1 Bumper 1", VanityType) },
            { "asp_shipkit1_bumper2", new ShipModule( -1, 0 , "Asp Shipkit 1 Bumper 2", VanityType) },
            { "asp_shipkit1_bumper3", new ShipModule( -1, 0 , "Asp Shipkit 1 Bumper 3", VanityType) },
            { "asp_shipkit1_bumper4", new ShipModule( -1, 0 , "Asp Shipkit 1 Bumper 4", VanityType) },
            { "asp_shipkit1_spoiler1", new ShipModule( -1, 0 , "Asp Shipkit 1 Spoiler 1", VanityType) },
            { "asp_shipkit1_spoiler2", new ShipModule( -1, 0 , "Asp Shipkit 1 Spoiler 2", VanityType) },
            { "asp_shipkit1_spoiler3", new ShipModule( -1, 0 , "Asp Shipkit 1 Spoiler 3", VanityType) },
            { "asp_shipkit1_spoiler4", new ShipModule( -1, 0 , "Asp Shipkit 1 Spoiler 4", VanityType) },
            { "asp_shipkit1_wings1", new ShipModule( -1, 0 , "Asp Shipkit 1 Wings 1", VanityType) },
            { "asp_shipkit1_wings2", new ShipModule( -1, 0 , "Asp Shipkit 1 Wings 2", VanityType) },
            { "asp_shipkit1_wings3", new ShipModule( -1, 0 , "Asp Shipkit 1 Wings 3", VanityType) },
            { "asp_shipkit1_wings4", new ShipModule( -1, 0 , "Asp Shipkit 1 Wings 4", VanityType) },
            { "bobble_christmastree", new ShipModule( -1, 0 , "Bobble Christmas Tree", VanityType) },
            { "bobble_davidbraben", new ShipModule( -1, 0 , "Bobble David Braben", VanityType) },
            { "decal_cannon", new ShipModule( -1, 0 , "Decal Cannon", VanityType) },
            { "decal_combat_competent", new ShipModule( -1, 0 , "Decal Combat Competent", VanityType) },
            { "decal_combat_dangerous", new ShipModule( -1, 0 , "Decal Combat Dangerous", VanityType) },
            { "decal_combat_deadly", new ShipModule( -1, 0 , "Decal Combat Deadly", VanityType) },
            { "decal_combat_expert", new ShipModule( -1, 0 , "Decal Combat Expert", VanityType) },
            { "decal_combat_master", new ShipModule( -1, 0 , "Decal Combat Master", VanityType) },
            { "decal_distantworlds", new ShipModule( -1, 0 , "Decal Distant Worlds", VanityType) },
            { "decal_explorer_elite", new ShipModule( -1, 0 , "Decal Explorer Elite", VanityType) },
            { "decal_explorer_pathfinder", new ShipModule( -1, 0 , "Decal Explorer Pathfinder", VanityType) },
            { "decal_explorer_starblazer", new ShipModule( -1, 0 , "Decal Explorer Starblazer", VanityType) },
            { "decal_fuelrats", new ShipModule( -1, 0 , "Decal Fuel Rats", VanityType) },
            { "decal_networktesters", new ShipModule( -1, 0 , "Decal Network Testers", VanityType) },
            { "decal_onionhead1", new ShipModule( -1, 0 , "Decal Onionhead 1", VanityType) },
            { "decal_onionhead2", new ShipModule( -1, 0 , "Decal Onionhead 2", VanityType) },
            { "decal_onionhead3", new ShipModule( -1, 0 , "Decal Onionhead 3", VanityType) },
            { "decal_distantworlds2", new ShipModule(-1,0, "Decal Distantworlds 2", VanityType ) },
            { "decal_paxprime", new ShipModule( -1, 0 , "Decal Pax Prime", VanityType) },
            { "decal_playergroup_wolves_of_jonai", new ShipModule( -1, 0 , "Decal Player Group Wolves Of Jonai", VanityType) },
            { "decal_powerplay_mahon", new ShipModule( -1, 0 , "Decal Power Play Mahon", VanityType) },
            { "decal_trade_broker", new ShipModule( -1, 0 , "Decal Trade Broker", VanityType) },
            { "decal_trade_elite", new ShipModule( -1, 0 , "Decal Trade Elite", VanityType) },
            { "nameplate_expedition01_white", new ShipModule( -1, 0 , "Nameplate Expedition 1 White", VanityType) },
            { "nameplate_explorer01_grey", new ShipModule( -1, 0 , "Nameplate Explorer 1 Grey", VanityType) },
            { "nameplate_explorer01_white", new ShipModule( -1, 0 , "Nameplate Explorer 1 White", VanityType) },
            { "nameplate_explorer03_white", new ShipModule( -1, 0 , "Nameplate Explorer 3 White", VanityType) },
            { "nameplate_shipid_doubleline_black", new ShipModule( -1, 0 , "Nameplate Ship ID Double Line Black", VanityType) },
            { "nameplate_shipid_doubleline_grey", new ShipModule( -1, 0 , "Nameplate Ship ID Double Line Grey", VanityType) },
            { "nameplate_shipid_doubleline_white", new ShipModule( -1, 0 , "Nameplate Ship ID Double Line White", VanityType) },
            { "nameplate_shipid_singleline_black", new ShipModule( -1, 0 , "Nameplate Ship ID Single Line Black", VanityType) },
            { "nameplate_shipid_white", new ShipModule( -1, 0 , "Nameplate Ship ID White", VanityType) },
            { "paintjob_adder_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Adder Black Friday 1", VanityType) },
            { "paintjob_anaconda_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Anaconda Blackfriday 1", VanityType) },
            { "paintjob_anaconda_luminous_stripe_03", new ShipModule( -1, 0 , "Paint Job Anaconda Luminous Stripe 3", VanityType) },
            { "paintjob_anaconda_metallic_gold", new ShipModule( -1, 0 , "Paint Job Anaconda Metallic Gold", VanityType) },
            { "paintjob_anaconda_militaire_earth_red", new ShipModule( -1, 0 , "Paint Job Anaconda Militaire Earth Red", VanityType) },
            { "paintjob_anaconda_militaire_earth_yellow", new ShipModule( -1, 0 , "Paint Job Anaconda Militaire Earth Yellow", VanityType) },
            { "paintjob_anaconda_strife_strife", new ShipModule( -1, 0 , "Paint Job Anaconda Strife Strife", VanityType) },
            { "paintjob_anaconda_vibrant_blue", new ShipModule( -1, 0 , "Paint Job Anaconda Vibrant Blue", VanityType) },
            { "paintjob_anaconda_vibrant_green", new ShipModule( -1, 0 , "Paint Job Anaconda Vibrant Green", VanityType) },
            { "paintjob_anaconda_vibrant_orange", new ShipModule( -1, 0 , "Paint Job Anaconda Vibrant Orange", VanityType) },
            { "paintjob_anaconda_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Anaconda Vibrant Purple", VanityType) },
            { "paintjob_anaconda_vibrant_red", new ShipModule( -1, 0 , "Paint Job Anaconda Vibrant Red", VanityType) },
            { "paintjob_anaconda_vibrant_yellow", new ShipModule( -1, 0 , "Paint Job Anaconda Vibrant Yellow", VanityType) },
            { "paintjob_anaconda_wireframe_01", new ShipModule( -1, 0 , "Paint Job Anaconda Wireframe 1", VanityType) },
            { "paintjob_asp_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Asp Blackfriday 1", VanityType) },
            { "paintjob_asp_gamescom_gamescom", new ShipModule( -1, 0 , "Paint Job Asp Games Com GamesCom", VanityType) },
            { "paintjob_asp_metallic_gold", new ShipModule( -1, 0 , "Paint Job Asp Metallic Gold", VanityType) },
            { "paintjob_asp_squadron_green", new ShipModule( -1, 0 , "Paint Job Asp Squadron Green", VanityType) },
            { "paintjob_asp_trespasser_01", new ShipModule( -1, 0 , "Paint Job Asp Trespasser 1", VanityType) },
            { "paintjob_asp_wireframe_01", new ShipModule( -1, 0 , "Paint Job Asp Wireframe 1", VanityType) },
            { "paintjob_cobramkiii_default_52", new ShipModule( -1, 0 , "Paint Job Cobra Mkiii Default 52", VanityType) },
            { "paintjob_cobramkiii_onionhead1_01", new ShipModule( -1, 0 , "Paint Job Cobra Mk III Onionhead 1 1", VanityType) },
            { "paintjob_cutter_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Cutter Black Friday 1", VanityType) },
            { "paintjob_cutter_metallic_chrome", new ShipModule( -1, 0 , "Paint Job Cutter Metallic Chrome", VanityType) },
            { "paintjob_empire_eagle_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Empire Eagle Black Friday 1", VanityType) },
            { "paintjob_empiretrader_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Empire Trader Black Friday 1", VanityType) },
            { "paintjob_empiretrader_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Empiretrader Vibrant Purple", VanityType) },
            { "paintjob_federation_corvette_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Federation Corvette Blackfriday 1", VanityType) },
            { "paintjob_federation_corvette_metallic_chrome", new ShipModule( -1, 0 , "Paint Job Federation Corvette Metallic Chrome", VanityType) },
            { "paintjob_ferdelance_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Fer De Lance Black Friday 1", VanityType) },
            { "paintjob_ferdelance_wireframe_01", new ShipModule( -1, 0 , "Paint Job Fer De Lance Wireframe 1", VanityType) },
            { "paintjob_hauler_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Hauler Blackfriday 1", VanityType) },
            { "paintjob_independant_trader_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Independant Trader Vibrant Purple", VanityType) },
            { "paintjob_indfighter_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Indfighter Vibrant Purple", VanityType) },
            { "paintjob_python_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Python Black Friday 1", VanityType) },
            { "paintjob_python_luminous_stripe_03", new ShipModule( -1, 0 , "Paint Job Python Luminous Stripe 3", VanityType) },
            { "paintjob_python_militaire_dark_green", new ShipModule( -1, 0 , "Paint Job Python Militaire Dark Green", VanityType) },
            { "paintjob_python_militaire_desert_sand", new ShipModule( -1, 0 , "Paint Job Python Militaire Desert Sand", VanityType) },
            { "paintjob_python_militaire_earth_red", new ShipModule( -1, 0 , "Paint Job Python Militaire Earth Red", VanityType) },
            { "paintjob_python_militaire_earth_yellow", new ShipModule( -1, 0 , "Paint Job Python Militaire Earth Yellow", VanityType) },
            { "paintjob_python_militaire_forest_green", new ShipModule( -1, 0 , "Paint Job Python Militaire Forest Green", VanityType) },
            { "paintjob_python_militaire_sand", new ShipModule( -1, 0 , "Paint Job Python Militaire Sand", VanityType) },
            { "paintjob_python_vibrant_blue", new ShipModule( -1, 0 , "Paint Job Python Vibrant Blue", VanityType) },
            { "paintjob_python_vibrant_green", new ShipModule( -1, 0 , "Paint Job Python Vibrant Green", VanityType) },
            { "paintjob_python_vibrant_orange", new ShipModule( -1, 0 , "Paint Job Python Vibrant Orange", VanityType) },
            { "paintjob_python_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Python Vibrant Purple", VanityType) },
            { "paintjob_python_vibrant_red", new ShipModule( -1, 0 , "Paint Job Python Vibrant Red", VanityType) },
            { "paintjob_python_vibrant_yellow", new ShipModule( -1, 0 , "Paint Job Python Vibrant Yellow", VanityType) },
            { "paintjob_python_wireframe_01", new ShipModule( -1, 0 , "Paint Job Python Wireframe 1", VanityType) },
            { "paintjob_sidewinder_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Sidewinder Blackfriday 1", VanityType) },
            { "paintjob_sidewinder_pax_east_pax_east", new ShipModule( -1, 0 , "Paint Job Sidewinder Pax East", VanityType) },
            { "paintjob_testbuggy_chase_04", new ShipModule( -1, 0 , "Paint Job Testbuggy Chase 4", VanityType) },
            { "paintjob_testbuggy_chase_05", new ShipModule( -1, 0 , "Paint Job Testbuggy Chase 5", VanityType) },
            { "paintjob_testbuggy_militaire_desert_sand", new ShipModule( -1, 0 , "Paint Job Testbuggy Militaire Desert Sand", VanityType) },
            { "paintjob_testbuggy_tactical_grey", new ShipModule( -1, 0 , "Paint Job Testbuggy Tactical Grey", VanityType) },
            { "paintjob_testbuggy_tactical_red", new ShipModule( -1, 0 , "Paint Job Testbuggy Tactical Red", VanityType) },
            { "paintjob_testbuggy_tactical_white", new ShipModule( -1, 0 , "Paint Job Testbuggy Tactical White", VanityType) },
            { "paintjob_testbuggy_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Testbuggy Vibrant Purple", VanityType) },
            { "paintjob_type6_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Type 6 Blackfriday 1", VanityType) },
            { "paintjob_type7_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Type 7 Black Friday 1", VanityType) },
            { "paintjob_type9_military_metallic2_chrome", new ShipModule( -1, 0 , "Paint Job Type 9 Military Metallic 2 Chrome", VanityType) },
            { "paintjob_type9_blackfriday_01", new ShipModule(-1,0, "Paintjob Type 9 Blackfriday 1", VanityType ) },
            { "paintjob_viper_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Viper Blackfriday 1", VanityType) },
            { "paintjob_viper_mkiv_blackfriday_01", new ShipModule( -1, 0 , "Paint Job Viper Mk IV Black Friday 1", VanityType) },
            { "paintjob_viper_stripe1_02", new ShipModule( -1, 0 , "Paint Job Viper Stripe 1 2", VanityType) },
            { "paintjob_viper_vibrant_blue", new ShipModule( -1, 0 , "Paint Job Viper Vibrant Blue", VanityType) },
            { "paintjob_viper_vibrant_green", new ShipModule( -1, 0 , "Paint Job Viper Vibrant Green", VanityType) },
            { "paintjob_viper_vibrant_orange", new ShipModule( -1, 0 , "Paint Job Viper Vibrant Orange", VanityType) },
            { "paintjob_viper_vibrant_purple", new ShipModule( -1, 0 , "Paint Job Viper Vibrant Purple", VanityType) },
            { "paintjob_viper_vibrant_red", new ShipModule( -1, 0 , "Paint Job Viper Vibrant Red", VanityType) },
            { "paintjob_viper_vibrant_yellow", new ShipModule( -1, 0 , "Paint Job Viper Vibrant Yellow", VanityType) },
            { "paintjob_vulture_metallic_chrome", new ShipModule( -1, 0 , "Paint Job Vulture Metallic Chrome", VanityType) },
            { "python_shipkit1_spoiler3", new ShipModule( -1, 0 , "Python Shipkit 1 Spoiler 3", VanityType) },
            { "python_shipkit1_tail1", new ShipModule( -1, 0 , "Python Shipkit 1 Tail 1", VanityType) },
            { "python_shipkit1_wings4", new ShipModule( -1, 0 , "Python Shipkit 1 Wings 4", VanityType) },
            { "string_lights_coloured", new ShipModule( -1, 0 , "String Lights Coloured", VanityType) },
            { "voicepack_verity", new ShipModule( -1, 0 , "Voice Pack Verity", VanityType) },
            { "weaponcustomisation_blue", new ShipModule( -1, 0 , "Weapon Customisation Blue", VanityType) },
            { "weaponcustomisation_green", new ShipModule( -1, 0 , "Weapon Customisation Green", VanityType) },
            { "weaponcustomisation_red", new ShipModule( -1, 0 , "Weapon Customisation Red", VanityType) },

             { "paintjob_diamondback_tactical_brown", new ShipModule(-1,0, "Paintjob Diamondback Tactical Brown", VanityType ) },
             { "paintjob_diamondback_tactical_white", new ShipModule(-1,0, "Paintjob Diamondback Tactical White", VanityType ) },
             { "paintjob_cobramkiii_horizons_lunar", new ShipModule(-1,0, "Paintjob Cobra MK III Horizons Lunar", VanityType ) },
             { "paintjob_cobramkiii_horizons_desert", new ShipModule(-1,0, "Paintjob Cobra MK III Horizons Desert", VanityType ) },
             { "decal_egx", new ShipModule(-1,0, "Decal Egx", VanityType ) },
             { "paintjob_independant_trader_blackfriday_01", new ShipModule(-1,0, "Paintjob Independant Trader Blackfriday 1", VanityType ) },
             { "paintjob_anaconda_squadron_green", new ShipModule(-1,0, "Paintjob Anaconda Squadron Green", VanityType ) },
             { "paintjob_anaconda_squadron_blue", new ShipModule(-1,0, "Paintjob Anaconda Squadron Blue", VanityType ) },
             { "paintjob_cobramkiii_yogscast_01", new ShipModule(-1,0, "Paintjob Cobra MK III Yogscast 1", VanityType ) },
             { "paintjob_cobramkiii_horizons_polar", new ShipModule(-1,0, "Paintjob Cobra MK III Horizons Polar", VanityType ) },
             { "decal_trade_entrepeneur", new ShipModule(-1,0, "Decal Trade Entrepeneur", VanityType ) },
             { "weaponcustomisation_purple", new ShipModule(-1,0, "Weapon Customisation Purple", VanityType ) },
             { "paintjob_diamondback_tactical_blue", new ShipModule(-1,0, "Paintjob Diamondback Tactical Blue", VanityType ) },
             { "decal_passenger_g", new ShipModule(-1,0, "Decal Passenger G", VanityType ) },
             { "paintjob_empire_courier_blackfriday_01", new ShipModule(-1,0, "Paintjob Empire Courier Blackfriday 1", VanityType ) },
             { "decal_passenger_e", new ShipModule(-1,0, "Decal Passenger E", VanityType ) },
             { "decal_powerplay_sirius", new ShipModule(-1,0, "Decal Powerplay Sirius", VanityType ) },
             { "paintjob_asp_scout_blackfriday_01", new ShipModule(-1,0, "Paint Job Asp Scout Black Friday 1", VanityType ) },
             { "decal_explorer_ranger", new ShipModule(-1,0, "Decal Explorer Ranger", VanityType ) },
             { "paintjob_cobramkiv_blackfriday_01", new ShipModule(-1,0, "Paint Job Cobra Mk IV Black Friday 1", VanityType ) },
             { "paintjob_vulture_blackfriday_01", new ShipModule(-1,0, "Paint Job Vulture Black Friday 1", VanityType ) },
             { "paintjob_asp_squadron_red", new ShipModule(-1,0, "Paint Job Asp Squadron Red", VanityType ) },
             { "paintjob_feddropship_mkii_blackfriday_01", new ShipModule(-1,0, "Paint Job Fed Dropship Mk II Black Friday 1", VanityType ) },
             { "paintjob_cobramkiii_blackfriday_01", new ShipModule(-1,0, "Paint Job Cobra Mk III Black Friday 1", VanityType ) },
             { "voicepack_victor", new ShipModule(-1,0, "Voicepack Victor", VanityType ) },
             { "decal_passenger_l", new ShipModule(-1,0, "Decal Passenger L", VanityType ) },
             { "decal_trade_mostly_penniless", new ShipModule(-1,0, "Decal Trade Mostly Penniless", VanityType ) },
             { "decal_espionage", new ShipModule(-1,0, "Decal Espionage", VanityType ) },
             { "anaconda_shipkit2raider_spoiler3", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Spoiler 3", VanityType ) },
             { "anaconda_shipkit2raider_wings3", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Wings 3", VanityType ) },
             { "anaconda_shipkit2raider_bumper3", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Bumper 3", VanityType ) },
             { "anaconda_shipkit2raider_tail3", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Tail 3", VanityType ) },
             { "anaconda_shipkit2raider_wings2", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Wings 2", VanityType ) },
             { "anaconda_shipkit2raider_bumper1", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Bumper 1", VanityType ) },
             { "anaconda_shipkit2raider_tail2", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Tail 2", VanityType ) },
             { "paintjob_python_squadron_black", new ShipModule(-1,0, "Paint Job Python Squadron Black", VanityType ) },
             { "bobble_pilotmale", new ShipModule(-1,0, "Bobble Pilot Male", VanityType ) },
             { "enginecustomisation_purple", new ShipModule(-1,0, "Engine Customisation Purple", VanityType ) },
             { "paintjob_anaconda_faction1_04", new ShipModule(-1,0, "Paint Job Anaconda Faction 1 4", VanityType ) },
             { "decal_bounty_hunter", new ShipModule(-1,0, "Decal Bounty Hunter", VanityType ) },
             { "anaconda_shipkit1_spoiler2", new ShipModule(-1,0, "Anaconda Shipkit 1 Spoiler 2", VanityType ) },
             { "anaconda_shipkit1_wings4", new ShipModule(-1,0, "Anaconda Shipkit 1 Wings 4", VanityType ) },
             { "anaconda_shipkit1_bumper4", new ShipModule(-1,0, "Anaconda Shipkit 1 Bumper 4", VanityType ) },
             { "anaconda_shipkit1_tail3", new ShipModule(-1,0, "Anaconda Shipkit 1 Tail 3", VanityType ) },

             { "anaconda_shipkit1_spoiler1", new ShipModule(-1,0, "Anaconda Shipkit 1 Spoiler 1", VanityType ) },
             { "anaconda_shipkit1_wings2", new ShipModule(-1,0, "Anaconda Shipkit 1 Wings 2", VanityType ) },
             { "anaconda_shipkit1_bumper3", new ShipModule(-1,0, "Anaconda Shipkit 1 Bumper 3", VanityType ) },
             { "paintjob_anaconda_pulse2_purple", new ShipModule(-1,0, "Paint Job Anaconda Pulse 2 Purple", VanityType ) },
             { "paintjob_indfighter_blackfriday_01", new ShipModule(-1,0, "Paint Job Ind Fighter Black Friday 1", VanityType ) },
             { "enginecustomisation_red", new ShipModule(-1,0, "Engine Customisation Red", VanityType ) },


             { "decal_skull3", new ShipModule(-1,0, "Decal Skull 3", VanityType ) },
             { "cobramkiii_shipkit1_spoiler2", new ShipModule(-1,0, "Cobra MK III Shipkit 1 Spoiler 2", VanityType ) },
             { "cobramkiii_shipkit1_wings3", new ShipModule(-1,0, "Co)bra MK III Shipkit 1 Wings 3", VanityType ) },
             { "cobramkiii_shipkit1_tail1", new ShipModule(-1,0, "Cobra MK III Shipkit 1 Tail 1", VanityType ) },
             { "cobramkiii_shipkit1_bumper1", new ShipModule(-1,0, "Cobra MK III Shipkit 1 Bumper 1", VanityType ) },
             { "weaponcustomisation_cyan", new ShipModule(-1,0, "Weapon Customisation Cyan", VanityType ) },
             { "nameplate_practical01_white", new ShipModule(-1,0, "Nameplate Practical 1 White", VanityType ) },
             { "paintjob_cobramkiii_stripe1_03", new ShipModule(-1,0, "Paintjob Cobra MK III Stripe 1 3", VanityType ) },
             { "bobble_ship_cobramkiii", new ShipModule(-1,0, "Bobble Ship Cobramkiii", VanityType ) },
             { "bobble_ship_viper", new ShipModule(-1,0, "Bobble Ship Viper", VanityType ) },
             { "paintjob_diamondbackxl_vibrant_blue", new ShipModule(-1,0, "Paintjob Diamondbackxl Vibrant Blue", VanityType ) },
             { "nameplate_practical03_white", new ShipModule(-1,0, "Nameplate Practical 3 White", VanityType ) },
             { "paintjob_diamondbackxl_tactical_white", new ShipModule(-1,0, "Paintjob Diamondbackxl Tactical White", VanityType ) },
             { "paintjob_diamondbackxl_tactical_blue", new ShipModule(-1,0, "Paintjob Diamondbackxl Tactical Blue", VanityType ) },
             { "paintjob_cobramkiii_tactical_white", new ShipModule(-1,0, "Paintjob Cobra MK III Tactical White", VanityType ) },
             { "nameplate_practical03_black", new ShipModule(-1,0, "Nameplate Practical 3 Black", VanityType ) },
             { "paintjob_type6_tactical_white", new ShipModule(-1,0, "Paintjob Type 6 Tactical White", VanityType ) },
             { "decal_explorer_trailblazer", new ShipModule(-1,0, "Decal Explorer Trailblazer", VanityType ) },
             { "paintjob_viper_mkiv_tactical_white", new ShipModule(-1,0, "Paintjob Viper MK IV Tactical White", VanityType ) },
             { "paintjob_viper_mkiv_tactical_blue", new ShipModule(-1,0, "Paintjob Viper MK IV Tactical Blue", VanityType ) },
             { "paintjob_testbuggy_vibrant_blue", new ShipModule(-1,0, "Paintjob SRV Vibrant Blue", VanityType ) },
             { "paintjob_vulture_tactical_blue", new ShipModule(-1,0, "Paintjob Vulture Tactical Blue", VanityType ) },
             { "vulture_shipkit1_spoiler3", new ShipModule(-1,0, "Vulture Shipkit 1 Spoiler 3", VanityType ) },
             { "vulture_shipkit1_wings2", new ShipModule(-1,0, "Vulture Shipkit 1 Wings 2", VanityType ) },
             { "vulture_shipkit1_tail1", new ShipModule(-1,0, "Vulture Shipkit 1 Tail 1", VanityType ) },
             { "vulture_shipkit1_bumper1", new ShipModule(-1,0, "Vulture Shipkit 1 Bumper 1", VanityType ) },
             { "vulture_shipkit1_spoiler4", new ShipModule(-1,0, "Vulture Shipkit 1 Spoiler 4", VanityType ) },
             { "paintjob_vulture_tactical_white", new ShipModule(-1,0, "Paintjob Vulture Tactical White", VanityType ) },
             { "paintjob_python_tactical_white", new ShipModule(-1,0, "Paintjob Python Tactical White", VanityType ) },
             { "python_shipkit1_spoiler1", new ShipModule(-1,0, "Python Shipkit 1 Spoiler 1", VanityType ) },
             { "python_shipkit1_wings3", new ShipModule(-1,0, "Python Shipkit 1 Wings 3", VanityType ) },
             { "python_shipkit1_tail3", new ShipModule(-1,0, "Python Shipkit 1 Tail 3", VanityType ) },
             { "python_shipkit1_bumper1", new ShipModule(-1,0, "Python Shipkit 1 Bumper 1", VanityType ) },
             { "python_shipkit1_bumper4", new ShipModule(-1,0, "Python Shipkit 1 Bumper 4", VanityType ) },
             { "python_shipkit1_wings1", new ShipModule(-1,0, "Python Shipkit 1 Wings 1", VanityType ) },
             { "python_shipkit1_wings2", new ShipModule(-1,0, "Python Shipkit 1 Wings 2", VanityType ) },
             { "paintjob_testbuggy_destination_blue", new ShipModule(-1,0, "Paintjob SRV Destination Blue", VanityType ) },
             { "bobble_station_coriolis_wire", new ShipModule(-1,0, "Bobble Station Coriolis Wire", VanityType ) },
             { "paintjob_anaconda_tactical_white", new ShipModule(-1,0, "Paintjob Anaconda Tactical White", VanityType ) },
             { "anaconda_shipkit1_spoiler4", new ShipModule(-1,0, "Anaconda Shipkit 1 Spoiler 4", VanityType ) },
             { "anaconda_shipkit1_tail1", new ShipModule(-1,0, "Anaconda Shipkit 1 Tail 1", VanityType ) },
             { "paintjob_sidewinder_militaire_forest_green", new ShipModule(-1,0, "Paintjob Sidewinder Militaire Forest Green", VanityType ) },
             { "paintjob_type7_vibrant_blue", new ShipModule(-1,0, "Paintjob Type 7 Vibrant Blue", VanityType ) },
             { "paintjob_sidewinder_thirds_06", new ShipModule(-1,0, "Paintjob Sidewinder Thirds 6", VanityType ) },
             { "paintjob_ferdelance_vibrant_blue", new ShipModule(-1,0, "Paintjob Ferdelance Vibrant Blue", VanityType ) },
             { "ferdelance_shipkit1_spoiler3", new ShipModule(-1,0, "Ferdelance Shipkit 1 Spoiler 3", VanityType ) },
             { "ferdelance_shipkit1_wings1", new ShipModule(-1,0, "Ferdelance Shipkit 1 Wings 1", VanityType ) },
             { "ferdelance_shipkit1_tail3", new ShipModule(-1,0, "Ferdelance Shipkit 1 Tail 3", VanityType ) },
             { "ferdelance_shipkit1_bumper1", new ShipModule(-1,0, "Ferdelance Shipkit 1 Bumper 1", VanityType ) },
             { "paintjob_ferdelance_tactical_white", new ShipModule(-1,0, "Paintjob Ferdelance Tactical White", VanityType ) },
             { "nameplate_practical01_black", new ShipModule(-1,0, "Nameplate Practical 1 Black", VanityType ) },
             { "decal_powerplay_halsey", new ShipModule(-1,0, "Decal Powerplay Halsey", VanityType ) },
             { "nameplate_explorer01_black", new ShipModule(-1,0, "Nameplate Explorer 1 Black", VanityType ) },
             { "paintjob_asp_vibrant_blue", new ShipModule(-1,0, "Paintjob Asp Vibrant Blue", VanityType ) },
             { "paintjob_asp_squadron_blue", new ShipModule(-1,0, "Paintjob Asp Squadron Blue", VanityType ) },
             { "enginecustomisation_orange", new ShipModule(-1,0, "Engine Customisation Orange", VanityType ) },
             { "nameplate_shipid_black", new ShipModule(-1,0, "Nameplate Shipid Black", VanityType ) },
             { "enginecustomisation_cyan", new ShipModule(-1,0, "Engine Customisation Cyan", VanityType ) },
             { "paintjob_testbuggy_luminous_blue", new ShipModule(-1,0, "Paintjob SRV Luminous Blue", VanityType ) },
             { "paintjob_testbuggy_luminous_red", new ShipModule(-1,0, "Paintjob SRV Luminous Red", VanityType ) },
             { "paintjob_testbuggy_chase_06", new ShipModule(-1,0, "Paintjob SRV Chase 6", VanityType ) },
             { "paintjob_eagle_tactical_blue", new ShipModule(-1,0, "Paintjob Eagle Tactical Blue", VanityType ) },
             { "nameplate_wings03_white", new ShipModule(-1,0, "Nameplate Wings 3 White", VanityType ) },
             { "eagle_shipkit1_spoiler1", new ShipModule(-1,0, "Eagle Shipkit 1 Spoiler 1", VanityType ) },
             { "eagle_shipkit1_wings1", new ShipModule(-1,0, "Eagle Shipkit 1 Wings 1", VanityType ) },
             { "eagle_shipkit1_bumper2", new ShipModule(-1,0, "Eagle Shipkit 1 Bumper 2", VanityType ) },
             { "weaponcustomisation_white", new ShipModule(-1,0, "Weapon Customisation White", VanityType ) },
             { "enginecustomisation_white", new ShipModule(-1,0, "Engine Customisation White", VanityType ) },
             { "sidewinder_shipkit1_spoiler1", new ShipModule(-1,0, "Sidewinder Shipkit 1 Spoiler 1", VanityType ) },
             { "sidewinder_shipkit1_wings4", new ShipModule(-1,0, "Sidewinder Shipkit 1 Wings 4", VanityType ) },
             { "sidewinder_shipkit1_tail3", new ShipModule(-1,0, "Sidewinder Shipkit 1 Tail 3", VanityType ) },
             { "sidewinder_shipkit1_bumper2", new ShipModule(-1,0, "Sidewinder Shipkit 1 Bumper 2", VanityType ) },
             { "bobble_textn", new ShipModule(-1,0, "Bobble Text n", VanityType ) },
             { "bobble_texte", new ShipModule(-1,0, "Bobble Text e", VanityType ) },
             { "bobble_textr", new ShipModule(-1,0, "Bobble Text r", VanityType ) },
             { "bobble_textd", new ShipModule(-1,0, "Bobble Text d", VanityType ) },
             { "bobble_textt", new ShipModule(-1,0, "Bobble Text t", VanityType ) },
             { "bobble_textf", new ShipModule(-1,0, "Bobble Text f", VanityType ) },
             { "bobble_textm", new ShipModule(-1,0, "Bobble Text m", VanityType ) },
             { "bobble_textdollar", new ShipModule(-1,0, "Bobble Text Dollar", VanityType ) },
             { "paintjob_type9_vibrant_blue", new ShipModule(-1,0, "Paintjob Type 9 Vibrant Blue", VanityType ) },
             { "paintjob_federation_fighter_vibrant_blue", new ShipModule(-1,0, "Paintjob Federation Fighter Vibrant Blue", VanityType ) },
             { "paintjob_ferdelance_vibrant_yellow", new ShipModule(-1,0, "Paintjob Ferdelance Vibrant Yellow", VanityType ) },
             { "paintjob_testbuggy_tactical_blue", new ShipModule(-1,0, "Paintjob SRV Tactical Blue", VanityType ) },
             { "bobble_texti", new ShipModule(-1,0, "Bobble Text I", VanityType ) },
             { "paintjob_hauler_doublestripe_01", new ShipModule(-1,0, "Paintjob Hauler Doublestripe 1", VanityType ) },
             { "paintjob_testbuggy_metallic2_gold", new ShipModule(-1,0, "Paintjob SRV Metallic 2 Gold", VanityType ) },
             { "paintjob_empire_courier_metallic2_gold", new ShipModule(-1,0, "Paintjob Empire Courier Metallic 2 Gold", VanityType ) },
             { "nameplate_explorer02_white", new ShipModule(-1,0, "Nameplate Explorer 2 White", VanityType ) },
             { "paintjob_empiretrader_tactical_white", new ShipModule(-1,0, "Paintjob Empiretrader Tactical White", VanityType ) },
             { "nameplate_wings02_black", new ShipModule(-1,0, "Nameplate Wings 2 Black", VanityType ) },
             { "paintjob_cutter_tactical_white", new ShipModule(-1,0, "Paintjob Cutter Tactical White", VanityType ) },
             { "python_shipkit1_spoiler4", new ShipModule(-1,0, "Python Shipkit 1 Spoiler 4", VanityType ) },
             { "paintjob_federation_corvette_vibrant_blue", new ShipModule(-1,0, "Paintjob Federation Corvette Vibrant Blue", VanityType ) },
             { "federation_corvette_shipkit1_tail3", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Tail 3", VanityType ) },
             { "federation_corvette_shipkit1_spoiler4", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Spoiler 4", VanityType ) },
             { "federation_corvette_shipkit1_wings4", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Wings 4", VanityType ) },
             { "federation_corvette_shipkit1_bumper4", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Bumper 4", VanityType ) },
             { "federation_corvette_shipkit1_tail1", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Tail 1", VanityType ) },
             { "paintjob_empire_fighter_tactical_white", new ShipModule(-1,0, "Paintjob Empire Fighter Tactical White", VanityType ) },
             { "paintjob_indfighter_tactical_white", new ShipModule(-1,0, "Paintjob Indfighter Tactical White", VanityType ) },
             { "paintjob_federation_fighter_tactical_white", new ShipModule(-1,0, "Paintjob Federation Fighter Tactical White", VanityType ) },
             { "anaconda_shipkit1_wings1", new ShipModule(-1,0, "Anaconda Shipkit 1 Wings 1", VanityType ) },
             { "nameplate_practical02_white", new ShipModule(-1,0, "Nameplate Practical 2 White", VanityType ) },
             { "paintjob_empire_fighter_vibrant_blue", new ShipModule(-1,0, "Paintjob Empire Fighter Vibrant Blue", VanityType ) },
             { "paintjob_federation_corvette_predator_blue", new ShipModule(-1,0, "Paintjob Federation Corvette Predator Blue", VanityType ) },
             { "nameplate_practical02_black", new ShipModule(-1,0, "Nameplate Practical 2 Black", VanityType ) },
             { "paintjob_belugaliner_metallic2_gold", new ShipModule(-1,0, "Paint Job Beluga Liner Metallic 2 Gold", VanityType ) },
             { "paintjob_feddropship_mkii_tactical_blue", new ShipModule(-1,0, "Paint Job Fed Dropship Mk II Tactical Blue", VanityType ) },
             { "nameplate_victory03_white", new ShipModule(-1,0, "Nameplate Victory 3 White", VanityType ) },
             { "paintjob_federation_gunship_tactical_blue", new ShipModule(-1,0, "Paint Job Federation Gunship Tactical Blue", VanityType ) },
             { "paintjob_feddropship_tactical_blue", new ShipModule(-1,0, "Paint Job Fed Dropship Tactical Blue", VanityType ) },
             { "anaconda_shipkit1_wings3", new ShipModule(-1,0, "Anaconda Shipkit 1 Wings 3", VanityType ) },
             { "paintjob_cobramkiv_gradient2_06", new ShipModule(-1,0, "Paint Job Cobra Mk IV Gradient 2 6", VanityType ) },
             { "paintjob_cutter_vibrant_blue", new ShipModule(-1,0, "Paint Job Cutter Vibrant Blue", VanityType ) },
             { "paintjob_ferdelance_vibrant_red", new ShipModule(-1,0, "Paint Job Ferdelance Vibrant Red", VanityType ) },
             { "paintjob_python_gradient2_06", new ShipModule(-1,0, "Paint Job Python Gradient 2 6", VanityType ) },
             { "nameplate_passenger01_black", new ShipModule(-1,0, "Nameplate Passenger 1 Black", VanityType ) },
             { "bobble_ship_anaconda", new ShipModule(-1,0, "Bobble Ship Anaconda", VanityType ) },
             { "paintjob_cutter_luminous_stripe_ver2_02", new ShipModule(-1,0, "Paint Job Cutter Luminous Stripe Ver 2 2", VanityType ) },
             { "cutter_shipkit1_spoiler2", new ShipModule(-1,0, "Cutter Shipkit 1 Spoiler 2", VanityType ) },
             { "cutter_shipkit1_wings2", new ShipModule(-1,0, "Cutter Shipkit 1 Wings 2", VanityType ) },
             { "cutter_shipkit1_bumper3", new ShipModule(-1,0, "Cutter Shipkit 1 Bumper 3", VanityType ) },
             { "paintjob_cutter_tactical_grey", new ShipModule(-1,0, "Paint Job Cutter Tactical Grey", VanityType ) },
             { "cutter_shipkit1_bumper4", new ShipModule(-1,0, "Cutter Shipkit 1 Bumper 4", VanityType ) },
             { "cutter_shipkit1_spoiler4", new ShipModule(-1,0, "Cutter Shipkit 1 Spoiler 4", VanityType ) },
             { "cutter_shipkit1_bumper2", new ShipModule(-1,0, "Cutter Shipkit 1 Bumper 2", VanityType ) },
             { "paintjob_empiretrader_vibrant_blue", new ShipModule(-1,0, "Paint Job Empiretrader Vibrant Blue", VanityType ) },
             { "bobble_santa", new ShipModule(-1,0, "Bobble Santa", VanityType ) },
             { "paintjob_independant_trader_tactical_white", new ShipModule(-1,0, "Paint Job Independant Trader Tactical White", VanityType ) },
             { "paintjob_anaconda_metallic2_chrome", new ShipModule(-1,0, "Paint Job Anaconda Metallic 2 Chrome", VanityType ) },
             { "paintjob_orca_corporate2_corporate2e", new ShipModule(-1,0, "Paint Job Orca Corporate 2 Corporate 2 E", VanityType ) },
             { "paintjob_empire_courier_aerial_display_blue", new ShipModule(-1,0, "Paint Job Empire Courier Aerial Display Blue", VanityType ) },
             { "nameplate_passenger01_white", new ShipModule(-1,0, "Nameplate Passenger 1 White", VanityType ) },
             { "paintjob_type9_military_vibrant_blue", new ShipModule(-1,0, "Paint Job Type 9 Military Vibrant Blue", VanityType ) },
             { "paintjob_python_metallic2_chrome", new ShipModule(-1,0, "Paint Job Python Metallic 2 Chrome", VanityType ) },
             { "paintjob_type6_tactical_blue", new ShipModule(-1,0, "Paint Job Type 6 Tactical Blue", VanityType ) },
             { "paintjob_typex_military_tactical_white", new ShipModule(-1,0, "Paintjob Typex Military Tactical White", VanityType ) },
             { "nameplate_explorer03_black", new ShipModule(-1,0, "Nameplate Explorer 3 Black", VanityType ) },
             { "voicepack_celeste", new ShipModule(-1,0, "Voicepack Celeste", VanityType ) },
             { "python_shipkit1_spoiler2", new ShipModule(-1,0, "Python Shipkit 1 Spoiler 2", VanityType ) },
             { "paintjob_federation_corvette_colourgeo_blue", new ShipModule(-1,0, "Paint Job Federation Corvette Colour Geo Blue", VanityType ) },
             { "paintjob_cutter_metallic2_chrome", new ShipModule(-1,0, "Paint Job Cutter Metallic 2 Chrome", VanityType ) },
             { "type6_shipkit1_spoiler3", new ShipModule(-1,0, "Type 6 Shipkit 1 Spoiler 3", VanityType ) },
             { "type6_shipkit1_wings1", new ShipModule(-1,0, "Type 6 Shipkit 1 Wings 1", VanityType ) },
             { "type6_shipkit1_bumper1", new ShipModule(-1,0, "Type 6 Shipkit 1 Bumper 1", VanityType ) },
             { "paintjob_type6_vibrant_blue", new ShipModule(-1,0, "Paint Job Type 6 Vibrant Blue", VanityType ) },
             { "typex_shipkit1_spoiler3", new ShipModule(-1,0, "Alliance Chieftain Shipkit 1 Spoiler 3", VanityType ) },
             { "typex_shipkit1_wings1", new ShipModule(-1,0, "Alliance Chieftain Shipkit 1 Wings 1", VanityType ) },
             { "typex_shipkit1_bumper3", new ShipModule(-1,0, "Alliance Chieftain Shipkit 1 Bumper 3", VanityType ) },
             { "cobramkiii_shipkit1_wings2", new ShipModule(-1,0, "Cobra MK III Shipkit 1 Wings 2", VanityType ) },
             { "paintjob_federation_corvette_colourgeo2_blue", new ShipModule(-1,0, "Paint Job Federation Corvette Colour Geo 2 Blue", VanityType ) },
             { "nameplate_wings03_grey", new ShipModule(-1,0, "Nameplate Wings 3 Grey", VanityType ) },
             { "nameplate_shipid_grey", new ShipModule(-1,0, "Nameplate Ship ID Grey", VanityType ) },
             { "federation_corvette_shipkit1_bumper2", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Bumper 2", VanityType ) },
             { "decal_met_espionage_gold", new ShipModule(-1,0, "Decal Met Espionage Gold", VanityType ) },
             { "nameplate_passenger03_white", new ShipModule(-1,0, "Nameplate Passenger 3 White", VanityType ) },
             { "voicepack_archer", new ShipModule(-1,0, "Voicepack Archer", VanityType ) },
             { "diamondbackxl_shipkit1_spoiler2", new ShipModule(-1,0, "Diamond Back XL Shipkit 1 Spoiler 2", VanityType ) },
             { "diamondbackxl_shipkit1_wings2", new ShipModule(-1,0, "Diamond Back XL Shipkit 1 Wings 2", VanityType ) },
             { "diamondbackxl_shipkit1_bumper1", new ShipModule(-1,0, "Diamond Back XL Shipkit 1 Bumper 1", VanityType ) },
             { "type9_military_shipkit1_spoiler3", new ShipModule(-1,0, "Type 9 Military Ship Kit 1 Spoiler 3", VanityType ) },
             { "type9_military_shipkit1_wings3", new ShipModule(-1,0, "Type 9 Military Ship Kit 1 Wings 3", VanityType ) },
             { "type9_military_shipkit1_bumper4", new ShipModule(-1,0, "Type 9 Military Ship Kit 1 Bumper 4", VanityType ) },
             { "anaconda_shipkit1_bumper1", new ShipModule(-1,0, "Anaconda Shipkit 1 Bumper 1", VanityType ) },
             { "paintjob_python_militarystripe_blue", new ShipModule(-1,0, "Paint Job Python Military Stripe Blue", VanityType ) },
             { "paintjob_ferdelance_metallic2_chrome", new ShipModule(-1,0, "Paint Job Fer De Lance Metallic 2 Chrome", VanityType ) },
             { "paintjob_belugaliner_corporatefleet_fleeta", new ShipModule(-1,0, "Paintjob Belugaliner Corporatefleet Fleeta", VanityType ) },
             { "paintjob_cutter_fullmetal_cobalt", new ShipModule(-1,0, "Paint Job Cutter Full Metal Cobalt", VanityType ) },
             { "paintjob_dolphin_metallic2_gold", new ShipModule(-1,0, "Paint Job Dolphin Metallic 2 Gold", VanityType ) },
             { "nameplate_expedition03_black", new ShipModule(-1,0, "Nameplate Expedition 3 Black", VanityType ) },
             { "paintjob_dolphin_corporatefleet_fleeta", new ShipModule(-1,0, "Paintjob Dolphin Corporatefleet Fleeta", VanityType ) },
             { "nameplate_expedition01_black", new ShipModule(-1,0, "Nameplate Expedition 1 Black", VanityType ) },
             { "nameplate_expedition02_black", new ShipModule(-1,0, "Nameplate Expedition 2 Black", VanityType ) },
             { "federation_corvette_shipkit1_tail4", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Tail 4", VanityType ) },
             { "federation_corvette_shipkit1_spoiler1", new ShipModule(-1,0, "Federation Corvette Shipkit 1 Spoiler 1", VanityType ) },
             { "nameplate_wings01_white", new ShipModule(-1,0, "Nameplate Wings 1 White", VanityType ) },


             { "decal_planet2", new ShipModule(-1,0, "Decal Planet 2", VanityType ) },
             { "bobble_pilotfemale", new ShipModule(-1,0, "Bobble Pilot Female", VanityType ) },
             { "paintjob_asp_stripe1_04", new ShipModule(-1,0, "Paintjob Asp Stripe 1 4", VanityType ) },
             { "enginecustomisation_blue", new ShipModule(-1,0, "Engine Customisation Blue", VanityType ) },
             { "bobble_texthash", new ShipModule(-1,0, "Bobble Text Hash", VanityType ) },
             { "bobble_textexclam", new ShipModule(-1,0, "Bobble Text !", VanityType ) },
             { "bobble_textquest", new ShipModule(-1,0, "Bobble Text ?", VanityType ) },
             { "bobble_textpercent", new ShipModule(-1,0, "Bobble Text %", VanityType ) },
             { "bobble_planet_jupiter", new ShipModule(-1,0, "Bobble Planet Jupiter", VanityType ) },
             { "bobble_planet_saturn", new ShipModule(-1,0, "Bobble Planet Saturn", VanityType ) },
             { "nameplate_explorer02_black", new ShipModule(-1,0, "Nameplate Explorer 2 Black", VanityType ) },
             { "bobble_pumpkin", new ShipModule(-1,0, "Bobble Pumpkin", VanityType ) },
             { "decal_trade_tycoon", new ShipModule(-1,0, "Decal Trade Tycoon", VanityType ) },
             { "bobble_station_coriolis", new ShipModule(-1,0, "Bobble Station Coriolis", VanityType ) },
             { "python_shipkit1_tail4", new ShipModule(-1,0, "Python Shipkit 1 Tail 4", VanityType ) },
             { "python_shipkit1_bumper3", new ShipModule(-1,0, "Python Shipkit 1 Bumper 3", VanityType ) },
             { "paintjob_federation_gunship_tactical_grey", new ShipModule(-1,0, "Paint Job Federation Gunship Tactical Grey", VanityType ) },
             { "paintjob_testbuggy_vibrant_orange", new ShipModule(-1,0, "Paintjob SRV Vibrant Orange", VanityType ) },
             { "nameplate_combat03_white", new ShipModule(-1,0, "Nameplate Combat 3 White", VanityType ) },
             { "ferdelance_shipkit1_wings2", new ShipModule(-1,0, "Fer De Lance Shipkit 1 Wings 2", VanityType ) },
             { "ferdelance_shipkit1_tail1", new ShipModule(-1,0, "Fer De Lance Shipkit 1 Tail 1", VanityType ) },
             { "ferdelance_shipkit1_bumper4", new ShipModule(-1,0, "Fer De Lance Shipkit 1 Bumper 4", VanityType ) },
             { "paintjob_cobramkiii_corrosive_05", new ShipModule(-1,0, "Paint Job Cobra MKIII Corrosive 5", VanityType ) },
             { "cobramkiii_shipkitraider1_spoiler3", new ShipModule(-1,0, "Cobra Mk III Shipkit Raider 1 Spoiler 3", VanityType ) },
             { "cobramkiii_shipkitraider1_wings1", new ShipModule(-1,0, "Cobra Mk III Shipkit Raider 1 Wings 1", VanityType ) },
             { "cobramkiii_shipkitraider1_tail2", new ShipModule(-1,0, "Cobra Mk III Shipkit Raider 1 Tail 2", VanityType ) },
             { "cobramkiii_shipkitraider1_bumper2", new ShipModule(-1,0, "Cobra Mk III Shipkit Raider 1 Bumper 2", VanityType ) },
             { "paintjob_python_corrosive_05", new ShipModule(-1,0, "Paint Job Python Corrosive 5", VanityType ) },
             { "decal_combat_elite", new ShipModule(-1,0, "Decal Combat Elite", VanityType ) },
             { "python_shipkit2raider_spoiler2", new ShipModule(-1,0, "Python Shipkit 2 Raider Spoiler 2", VanityType ) },
             { "python_shipkit2raider_wings2", new ShipModule(-1,0, "Python Shipkit 2 Raider Wings 2", VanityType ) },
             { "python_shipkit2raider_tail1", new ShipModule(-1,0, "Python Shipkit 2 Raider Tail 1", VanityType ) },
             { "python_shipkit2raider_bumper3", new ShipModule(-1,0, "Python Shipkit 2 Raider Bumper 3", VanityType ) },
             { "paintjob_python_metallic2_gold", new ShipModule(-1,0, "Paint Job Python Metallic 2 Gold", VanityType ) },
             { "decal_specialeffect", new ShipModule(-1,0, "Decal Special Effect", VanityType ) },
             { "nameplate_trader02_white", new ShipModule(-1,0, "Nameplate Trader 2 White", VanityType ) },
             { "bobble_pilot_dave_expo_flight_suit", new ShipModule(-1,0, "Bobble Pilot Dave Expo Flight Suit", VanityType ) },
             { "bobble_pilotmale_expo_flight_suit", new ShipModule(-1,0, "Bobble Pilot Male Expo Flight Suit", VanityType ) },
             { "enginecustomisation_green", new ShipModule(-1,0, "Engine Customisation Green", VanityType ) },
             { "nameplate_hunter01_white", new ShipModule(-1,0, "Nameplate Hunter 1 White", VanityType ) },
             { "decal_beta_tester", new ShipModule(-1,0, "Decal Beta Tester", VanityType ) },
             { "nameplate_skulls03_black", new ShipModule(-1,0, "Nameplate Skulls 3 Black", VanityType ) },
             { "paintjob_ferdelance_metallic2_gold", new ShipModule(-1,0, "Paint Job Fer De Lance Metallic 2 Gold", VanityType ) },
             { "paintjob_sidewinder_specialeffect_01", new ShipModule(-1,0, "Paintjob Sidewinder Specialeffect 1", VanityType ) },
            { "sidewinder_shipkit1_bumper1", new ShipModule(-1,0, "Sidewinder Shipkit 1 Bumper 1", VanityType ) },
            { "paintjob_federation_gunship_tactical_brown", new ShipModule(-1,0, "Paintjob Federation Gunship Tactical Brown", VanityType ) },
            { "paintjob_testbuggy_vibrant_yellow", new ShipModule(-1,0, "Paintjob SRV Vibrant Yellow", VanityType ) },
             { "paintjob_cobramkiii_tactical_grey", new ShipModule(-1,0, "Paintjob Cobramkiii Tactical Grey", VanityType ) },
             { "cobramkiii_shipkit1_tail4", new ShipModule(-1,0, "Cobramkiii Shipkit 1 Tail 4", VanityType ) },
             { "cobramkiii_shipkit1_bumper4", new ShipModule(-1,0, "Cobramkiii Shipkit 1 Bumper 4", VanityType ) },
             { "nameplate_shipid_singleline_white", new ShipModule(-1,0, "Nameplate Shipid Singleline White", VanityType ) },
             { "nameplate_combat01_white", new ShipModule(-1,0, "Nameplate Combat 1 White", VanityType ) },
             { "nameplate_combat02_white", new ShipModule(-1,0, "Nameplate Combat 2 White", VanityType ) },
             { "paintjob_empire_courier_tactical_grey", new ShipModule(-1,0, "Paint Job Empire Courier Tactical Grey", VanityType ) },
             { "paintjob_python_luminous_stripe_02", new ShipModule(-1,0, "Paintjob Python Luminous Stripe 2", VanityType ) },
             { "paintjob_eagle_tactical_white", new ShipModule(-1,0, "Paintjob Eagle Tactical White", VanityType ) },
             { "nameplate_combat03_black", new ShipModule(-1,0, "Nameplate Combat 3 Black", VanityType ) },
             { "paintjob_feddropship_tactical_grey", new ShipModule(-1,0, "Paintjob Feddropship Tactical Grey", VanityType ) },
             { "weaponcustomisation_yellow", new ShipModule(-1,0, "Weapon Customisation Yellow", VanityType ) },
             { "paintjob_python_corrosive_01", new ShipModule(-1,0, "Paintjob Python Corrosive 1", VanityType ) },
             { "paintjob_python_corrosive_06", new ShipModule(-1,0, "Paintjob Python Corrosive 6", VanityType ) },
             { "python_shipkit1_tail2", new ShipModule(-1,0, "Python Shipkit 1 Tail 2", VanityType ) },
             { "paintjob_anaconda_corrosive_05", new ShipModule(-1,0, "Paintjob Anaconda Corrosive 5", VanityType ) },
             { "anaconda_shipkit2raider_spoiler1", new ShipModule(-1,0, "Anaconda Shipkit 2 Raider Spoiler 1", VanityType ) },
             { "bobble_planet_earth", new ShipModule(-1,0, "Bobble Planet Earth", VanityType ) },
             { "anaconda_shipkit1_tail4", new ShipModule(-1,0, "Anaconda Shipkit 1 Tail 4", VanityType ) },
             { "paintjob_anaconda_tactical_grey", new ShipModule(-1,0, "Paintjob Anaconda Tactical Grey", VanityType ) },
             { "paintjob_anaconda_tactical_red", new ShipModule(-1,0, "Paintjob Anaconda Tactical Red", VanityType ) },
             { "paintjob_python_eliteexpo_eliteexpo", new ShipModule(-1,0, "Paint Job Python Elite Expo Elite Expo", VanityType ) },
             { "decal_expo", new ShipModule(-1,0, "Decal Expo", VanityType ) },
             { "paintjob_anaconda_eliteexpo_eliteexpo", new ShipModule(-1,0, "Paint Job Anaconda Elite Expo Elite Expo", VanityType ) },
             { "bobble_planet_mercury", new ShipModule(-1,0, "Bobble Planet Mercury", VanityType ) },
             { "bobble_planet_venus", new ShipModule(-1,0, "Bobble Planet Venus", VanityType ) },
             { "bobble_planet_mars", new ShipModule(-1,0, "Bobble Planet Mars", VanityType ) },
             { "bobble_planet_uranus", new ShipModule(-1,0, "Bobble Planet Uranus", VanityType ) },
             { "bobble_planet_neptune", new ShipModule(-1,0, "Bobble Planet Neptune", VanityType ) },
             { "paintjob_feddropship_vibrant_orange", new ShipModule(-1,0, "Paint Job Feddropship Vibrant Orange", VanityType ) },
             { "cobramkiii_shipkit1_spoiler4", new ShipModule(-1,0, "Cobra Mk III Shipkit 1 Spoiler 4", VanityType ) },
             { "cobramkiii_shipkit1_wings1", new ShipModule(-1,0, "Cobra Mk III Shipkit 1 Wings 1", VanityType ) },
             { "cobramkiii_shipkit1_tail3", new ShipModule(-1,0, "Cobra Mk III Shipkit 1 Tail 3", VanityType ) },
             { "decal_spider", new ShipModule(-1,0, "Decal Spider", VanityType ) },
             { "decal_skull5", new ShipModule(-1,0, "Decal Skull 5", VanityType ) },
             { "paintjob_type9_military_tactical_red", new ShipModule(-1,0, "Paint Job Type 9 Military Tactical Red", VanityType ) },
             { "paintjob_cobramkiii_flag_uk_01", new ShipModule(-1,0, "Paint Job Cobra Mk III Flag UK 1", VanityType ) },
             { "sidewinder_shipkit1_wings3", new ShipModule(-1,0, "Sidewinder Shipkit 1 Wings 3", VanityType ) },
             { "sidewinder_shipkit1_tail1", new ShipModule(-1,0, "Sidewinder Shipkit 1 Tail 1", VanityType ) },
             { "paintjob_typex_military_tactical_grey", new ShipModule(-1,0, "Paintjob Typex Military Tactical Grey", VanityType ) },
             { "nameplate_skulls01_white", new ShipModule(-1,0, "Nameplate Skulls 1 White", VanityType ) },
             { "nameplate_trader01_white", new ShipModule(-1,0, "Nameplate Trader 1 White", VanityType ) },
             { "nameplate_wings01_black", new ShipModule(-1,0, "Nameplate Wings 1 Black", VanityType ) },
             { "paintjob_eagle_tactical_grey", new ShipModule(-1,0, "Paint Job Eagle Tactical Grey", VanityType ) },
             { "paintjob_dolphin_vibrant_yellow", new ShipModule(-1,0, "Paintjob Dolphin Vibrant Yellow", VanityType ) },
             { "paintjob_orca_vibrant_yellow", new ShipModule(-1,0, "Paintjob Orca Vibrant Yellow", VanityType ) },
             { "nameplate_skulls03_white", new ShipModule(-1,0, "Nameplate Skulls 3 White", VanityType ) },

             { "paintjob_cobramkiii_vibrant_orange", new ShipModule(-1,0, "Paintjob Cobra Mk III Vibrant Orange", VanityType ) },
             { "paintjob_vulture_synth_orange", new ShipModule(-1,0, "Paint Job Vulture Synth Orange", VanityType ) },
             { "paintjob_asp_vibrant_orange", new ShipModule(-1,0, "Paintjob Asp Vibrant Orange", VanityType ) },
             { "decal_explorer_surveyor", new ShipModule(-1,0, "Decal Explorer Surveyor", VanityType ) },
             { "paintjob_anaconda_metallic2_gold", new ShipModule(-1,0, "Paintjob Anaconda Metallic 2 Gold", VanityType ) },

             { "decal_shark1", new ShipModule(-1,0, "Decal Shark 1", VanityType ) },
             { "decal_pilot_fed1", new ShipModule(-1,0, "Decal Pilot Fed 1", VanityType ) },
             { "decal_lavecon", new ShipModule(-1,0, "Decal Lave Con", VanityType ) },
             { "bobble_textg", new ShipModule(-1,0, "Bobble Text G", VanityType ) },
             { "bobble_textexclam01", new ShipModule(-1,0, "Bobble Text Exclam 1", VanityType ) },
             { "asp_shipkit2raider_wings2", new ShipModule(-1,0, "Asp Shipkit 2 Raider Wings 2", VanityType ) },
             { "asp_shipkit2raider_tail2", new ShipModule(-1,0, "Asp Shipkit 2 Raider Tail 2", VanityType ) },
             { "decal_founders_reversed", new ShipModule(-1,0, "Decal Founders Reversed", VanityType ) },
             { "asp_shipkit2raider_bumper2", new ShipModule(-1,0, "Asp Shipkit 2 Raider Bumper 2", VanityType ) },
             { "paintjob_python_gold_wireframe_01", new ShipModule(-1,0, "Paint Job Python Gold Wireframe 1", VanityType ) },
             { "python_shipkit2raider_tail3", new ShipModule(-1,0, "Python Shipkit 2 Raider Tail 3", VanityType ) },

             { "paintjob_asp_lavecon_lavecon", new ShipModule(-1,0, "Paintjob Asp Lavecon Lavecon", VanityType ) },
             { "paintjob_anaconda_lavecon_lavecon", new ShipModule(-1,0, "Paintjob Anaconda Lavecon Lavecon", VanityType ) },
             { "asp_shipkit2raider_bumper3", new ShipModule(-1,0, "Asp Shipkit 2 Raider Bumper 3", VanityType ) },
             { "weaponcustomisation_pink", new ShipModule(-1,0, "Weapon Customisation Pink", VanityType ) },
             { "paintjob_testbuggy_militaire_earth_yellow", new ShipModule(-1,0, "Paintjob SRV Militaire Earth Yellow", VanityType ) },
             { "paintjob_sidewinder_hotrod_01", new ShipModule(-1,0, "Paint Job Sidewinder Hotrod 1", VanityType ) },
             { "paintjob_testbuggy_militaire_earth_red", new ShipModule(-1,0, "Paintjob SRV Militaire Earth Red", VanityType ) },
             { "paintjob_viper_flag_norway_01", new ShipModule(-1,0, "Paintjob Viper Flag Norway 1", VanityType ) },
             { "paintjob_vulture_militaire_desert_sand", new ShipModule(-1,0, "Paint Job Vulture Militaire Desert Sand", VanityType ) },
             { "paintjob_cobramkiii_stripe2_02", new ShipModule(-1,0, "Paint Job Cobra Mk III Stripe 2 2", VanityType ) },
             { "nameplate_shipname_white", new ShipModule(-1,0, "Nameplate Ship Name White", VanityType ) },
             { "paintjob_cobramkiii_wireframe_01", new ShipModule(-1,0, "Paint Job Cobramkiii Wireframe 1", VanityType ) },
             { "nameplate_sympathiser03_white", new ShipModule(-1,0, "Nameplate Sympathiser 3 White", VanityType ) },
             { "paintjob_asp_gold_wireframe_01", new ShipModule(-1,0, "Paint Job Asp Gold Wireframe 1", VanityType ) },

             { "paintjob_anaconda_squadron_red", new ShipModule(-1,0, "Paintjob Anaconda Squadron Red", VanityType ) },
             { "paintjob_diamondbackxl_blackfriday_01", new ShipModule(-1,0, "Paintjob Diamondbackxl Blackfriday 1", VanityType ) },

             { "paintjob_viper_mkiv_tactical_brown", new ShipModule(-1,0, "Paintjob Viper MK IV Tactical Brown", VanityType ) },
             { "paintjob_viper_mkiv_militaire_sand", new ShipModule(-1,0, "Paintjob Viper MK IV Militaire Sand", VanityType ) },
             { "paintjob_viper_mkiv_squadron_orange", new ShipModule(-1,0, "Paintjob Viper MK IV Squadron Orange", VanityType ) },
             { "paintjob_viper_mkiv_tactical_grey", new ShipModule(-1,0, "Paintjob Viper MK IV Tactical Grey", VanityType ) },
             { "paintjob_viper_mkiv_tactical_green", new ShipModule(-1,0, "Paintjob Viper MK IV Tactical Green", VanityType ) },
             { "paintjob_viper_mkiv_squadron_black", new ShipModule(-1,0, "Paintjob Viper MK IV Squadron Black", VanityType ) },

             { "decal_trade_dealer", new ShipModule(-1,0, "Decal Trade Dealer", VanityType ) },
             { "decal_combat_novice", new ShipModule(-1,0, "Decal Combat Novice", VanityType ) },
             { "decal_trade_merchant", new ShipModule(-1,0, "Decal Trade Merchant", VanityType ) },
             { "nameplate_shipname_black", new ShipModule(-1,0, "Nameplate Shipname Black", VanityType ) },

             { "decal_powerplay_utopia", new ShipModule(-1,0, "Decal Power Play Utopia", VanityType ) },
             { "nameplate_wings03_black", new ShipModule(-1,0, "Nameplate Wings 3 Black", VanityType ) },
             { "nameplate_explorer02_grey", new ShipModule(-1,0, "Nameplate Explorer 2 Grey", VanityType ) },

             { "paintjob_krait_mkii_vibrant_red", new ShipModule(-1,0, "Paintjob Krait Mkii Vibrant Red", VanityType ) },

             { "nameplate_trader02_grey", new ShipModule(-1,0, "Nameplate Trader 2 Grey", VanityType ) },
             { "bobble_snowman", new ShipModule(-1,0, "Bobble Snowman", VanityType ) },
             { "bobble_snowflake", new ShipModule(-1,0, "Bobble Snowflake", VanityType ) },
             { "decal_triple_elite", new ShipModule(-1,0, "Decal Triple Elite", VanityType ) },

             { "paintjob_sidewinder_vibrant_red", new ShipModule(-1,0, "Paintjob Sidewinder Vibrant Red", VanityType ) },
             { "paintjob_asp_lrpo_azure", new ShipModule(-1,0, "Paintjob Asp Lrpo Azure", VanityType ) },
             { "paintjob_type9_lrpo_azure", new ShipModule(-1,0, "Paintjob Type 9 Lrpo Azure", VanityType ) },
             { "paintjob_python_lrpo_azure", new ShipModule(-1,0, "Paintjob Python Lrpo Azure", VanityType ) },
             { "bobble_trophy_exploration", new ShipModule(-1,0, "Bobble Trophy Exploration", VanityType ) },
             { "paintjob_asp_metallic2_gold", new ShipModule(-1,0, "Paintjob Asp Metallic 2 Gold", VanityType ) },
             { "paintjob_asp_operator_red", new ShipModule(-1,0, "Paintjob Asp Operator Red", VanityType ) },

        };

        #endregion


        #region FROM COROLIS  NEVER EVER do this manually, its done using a corolis-data scanner in testnetlogentry!

        public static Dictionary<string, ShipModule> modules = new Dictionary<string, ShipModule>       // DO NOT USE DIRECTLY - public is for checking only
        {
            ///////////////////////////////////// FROM HARDPOINT FOLDER MODULE SCAN

            { "hpt_mining_abrblstr_fixed_small", new ShipModule(128915458, 2, 0.34F, "Damage:4, Range:1000m, Speed:667m/s, Reload:2s, ThermL:1.8","Mining Abrasion Blaster Fixed Small", "Mining")},
            { "hpt_mining_abrblstr_turret_small", new ShipModule(128915459, 2, 0.47F, "Damage:4, Range:1000m, Speed:667m/s, Reload:2s, ThermL:1.8","Mining Abrasion Blaster Turret Small", "Mining")},
            { "hpt_atdumbfiremissile_fixed_medium", new ShipModule(128788699, 4, 1.2F, "Ammo:64/8, Damage:64, Speed:750m/s, Reload:5s, ThermL:2.4","AX Dumbfire Missile Fixed Medium", "Missile Rack")},
            { "hpt_atdumbfiremissile_turret_medium", new ShipModule(128788704, 4, 1.2F, "Ammo:64/8, Damage:50, Speed:750m/s, Reload:5s, ThermL:1.5","AX Dumbfire Missile Turret Medium", "Missile Rack")},
            { "hpt_atdumbfiremissile_fixed_large", new ShipModule(128788700, 8, 1.62F, "Ammo:128/12, Damage:64, Speed:750m/s, Reload:5s, ThermL:3.6","AX Dumbfire Missile Fixed Large", "Missile Rack")},
            { "hpt_atdumbfiremissile_turret_large", new ShipModule(128788705, 8, 1.75F, "Ammo:128/12, Damage:64, Speed:750m/s, Reload:5s, ThermL:1.9","AX Dumbfire Missile Turret Large", "Missile Rack")},
            { "hpt_atmulticannon_fixed_medium", new ShipModule(128788701, 4, 0.46F, "Ammo:2100/100, Damage:3.3, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2","AX Multi Cannon Fixed Medium", "Multi Cannon")},
            { "hpt_atmulticannon_turret_medium", new ShipModule(128793059, 4, 0.5F, "Ammo:2100/90, Damage:1.7, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1","AX Multi Cannon Turret Medium", "Multi Cannon")},
            { "hpt_atmulticannon_fixed_large", new ShipModule(128788702, 8, 0.64F, "Ammo:2100/100, Damage:6.1, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.3","AX Multi Cannon Fixed Large", "Multi Cannon")},
            { "hpt_atmulticannon_turret_large", new ShipModule(128793060, 8, 0.64F, "Ammo:2100/90, Damage:3.3, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1","AX Multi Cannon Turret Large", "Multi Cannon")},
            { "hpt_beamlaser_fixed_small", new ShipModule(128049428, 2, 0.62F, "Damage:9.8, Range:3000m, ThermL:3.5","Beam Laser Fixed Small", "Beam Laser")},
            { "hpt_beamlaser_gimbal_small", new ShipModule(128049432, 2, 0.6F, "Damage:7.7, Range:3000m, ThermL:3.6","Beam Laser Gimbal Small", "Beam Laser")},
            { "hpt_beamlaser_turret_small", new ShipModule(128049435, 2, 0.57F, "Damage:5.4, Range:3000m, ThermL:2.4","Beam Laser Turret Small", "Beam Laser")},
            { "hpt_beamlaser_fixed_small_heat", new ShipModule(128671346, 2, 0.62F, "Damage:4.9, Range:3000m, ThermL:2.7","Beam Laser Fixed Small Heat", "Beam Laser")},
            { "hpt_beamlaser_fixed_medium", new ShipModule(128049429, 4, 1.01F, "Damage:16, Range:3000m, ThermL:5.1","Beam Laser Fixed Medium", "Beam Laser")},
            { "hpt_beamlaser_gimbal_medium", new ShipModule(128049433, 4, 1, "Damage:12.5, Range:3000m, ThermL:5.3","Beam Laser Gimbal Medium", "Beam Laser")},
            { "hpt_beamlaser_turret_medium", new ShipModule(128049436, 4, 0.93F, "Damage:8.8, Range:3000m, ThermL:3.5","Beam Laser Turret Medium", "Beam Laser")},
            { "hpt_beamlaser_fixed_large", new ShipModule(128049430, 8, 1.62F, "Damage:25.8, Range:3000m, ThermL:7.2","Beam Laser Fixed Large", "Beam Laser")},
            { "hpt_beamlaser_gimbal_large", new ShipModule(128049434, 8, 1.6F, "Damage:20.3, Range:3000m, ThermL:7.6","Beam Laser Gimbal Large", "Beam Laser")},
            { "hpt_beamlaser_turret_large", new ShipModule(128049437, 8, 1.51F, "Damage:14.3, Range:3000m, ThermL:5.1","Beam Laser Turret Large", "Beam Laser")},
            { "hpt_beamlaser_fixed_huge", new ShipModule(128049431, 16, 2.61F, "Damage:41.4, Range:3000m, ThermL:9.9","Beam Laser Fixed Huge", "Beam Laser")},
            { "hpt_beamlaser_gimbal_huge", new ShipModule(128681994, 16, 2.57F, "Damage:32.7, Range:3000m, ThermL:10.6","Beam Laser Gimbal Huge", "Beam Laser")},
            { "hpt_pulselaserburst_fixed_small", new ShipModule(128049400, 2, 0.65F, "Damage:1.7, Range:3000m, ThermL:0.4","Burst Laser Fixed Small", "Burst Laser")},
            { "hpt_pulselaserburst_gimbal_small", new ShipModule(128049404, 2, 0.64F, "Damage:1.2, Range:3000m, ThermL:0.3","Burst Laser Gimbal Small", "Burst Laser")},
            { "hpt_pulselaserburst_turret_small", new ShipModule(128049407, 2, 0.6F, "Damage:0.9, Range:3000m, ThermL:0.2","Burst Laser Turret Small", "Burst Laser")},
            { "hpt_pulselaserburst_fixed_small_scatter", new ShipModule(128671449, 2, 0.8F, "Damage:3.6, Range:1000m, ThermL:0.3","Burst Laser Fixed Small Scatter", "Burst Laser")},
            { "hpt_pulselaserburst_fixed_medium", new ShipModule(128049401, 4, 1.05F, "Damage:3.5, Range:3000m, ThermL:0.8","Burst Laser Fixed Medium", "Burst Laser")},
            { "hpt_pulselaserburst_gimbal_medium", new ShipModule(128049405, 4, 1.04F, "Damage:2.5, Range:3000m, ThermL:0.7","Burst Laser Gimbal Medium", "Burst Laser")},
            { "hpt_pulselaserburst_turret_medium", new ShipModule(128049408, 4, 0.98F, "Damage:1.7, Range:3000m, ThermL:0.4","Burst Laser Turret Medium", "Burst Laser")},
            { "hpt_pulselaserburst_fixed_large", new ShipModule(128049402, 8, 1.66F, "Damage:7.7, Range:3000m, ThermL:1.7","Burst Laser Fixed Large", "Burst Laser")},
            { "hpt_pulselaserburst_gimbal_large", new ShipModule(128049406, 8, 1.65F, "Damage:5.2, Range:3000m, ThermL:1.4","Burst Laser Gimbal Large", "Burst Laser")},
            { "hpt_pulselaserburst_turret_large", new ShipModule(128049409, 8, 1.57F, "Damage:3.5, Range:3000m, ThermL:0.8","Burst Laser Turret Large", "Burst Laser")},
            { "hpt_pulselaserburst_fixed_huge", new ShipModule(128049403, 16, 2.58F, "Damage:20.6, Range:3000m, ThermL:4.5","Burst Laser Fixed Huge", "Burst Laser")},
            { "hpt_pulselaserburst_gimbal_huge", new ShipModule(128727920, 16, 2.59F, "Damage:12.1, Range:3000m, ThermL:3.3","Burst Laser Gimbal Huge", "Burst Laser")},
            { "hpt_cannon_fixed_small", new ShipModule(128049438, 2, 0.34F, "Ammo:120/6, Damage:22.5, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:1.4","Cannon Fixed Small", "Cannon")},
            { "hpt_cannon_gimbal_small", new ShipModule(128049442, 2, 0.38F, "Ammo:100/5, Damage:16, Range:3000m, Speed:1000m/s, Reload:4s, ThermL:1.3","Cannon Gimbal Small", "Cannon")},
            { "hpt_cannon_turret_small", new ShipModule(128049445, 2, 0.32F, "Ammo:100/5, Damage:12.8, Range:3000m, Speed:1000m/s, Reload:4s, ThermL:0.7","Cannon Turret Small", "Cannon")},
            { "hpt_cannon_fixed_medium", new ShipModule(128049439, 4, 0.49F, "Ammo:120/6, Damage:36.5, Range:3500m, Speed:1051m/s, Reload:3s, ThermL:2.1","Cannon Fixed Medium", "Cannon")},
            { "hpt_cannon_gimbal_medium", new ShipModule(128049443, 4, 0.54F, "Ammo:100/5, Damage:24.5, Range:3500m, Speed:875m/s, Reload:4s, ThermL:1.9","Cannon Gimbal Medium", "Cannon")},
            { "hpt_cannon_turret_medium", new ShipModule(128049446, 4, 0.45F, "Ammo:100/5, Damage:19.8, Range:3500m, Speed:875m/s, Reload:4s, ThermL:1","Cannon Turret Medium", "Cannon")},
            { "hpt_cannon_fixed_large", new ShipModule(128049440, 8, 0.67F, "Ammo:120/6, Damage:54.9, Range:4000m, Speed:959m/s, Reload:3s, ThermL:3.2","Cannon Fixed Large", "Cannon")},
            { "hpt_cannon_gimbal_large", new ShipModule(128671120, 8, 0.75F, "Ammo:100/5, Damage:37.4, Range:4000m, Speed:800m/s, Reload:4s, ThermL:2.9","Cannon Gimbal Large", "Cannon")},
            { "hpt_cannon_turret_large", new ShipModule(128049447, 8, 0.64F, "Ammo:100/5, Damage:30.4, Range:4000m, Speed:800m/s, Reload:4s, ThermL:1.6","Cannon Turret Large", "Cannon")},
            { "hpt_cannon_fixed_huge", new ShipModule(128049441, 16, 0.92F, "Ammo:120/6, Damage:82.1, Range:4500m, Speed:900m/s, Reload:3s, ThermL:4.8","Cannon Fixed Huge", "Cannon")},
            { "hpt_cannon_gimbal_huge", new ShipModule(128049444, 16, 1.03F, "Ammo:100/5, Damage:56.6, Range:4500m, Speed:750m/s, Reload:4s, ThermL:4.4","Cannon Gimbal Huge", "Cannon")},
            { "hpt_cargoscanner_size0_class1", new ShipModule(128662520, 1.3F, 0.2F, "Range:2000m","Cargo Scanner Rating E", "Cargo Scanner")},
            { "hpt_cargoscanner_size0_class2", new ShipModule(128662521, 1.3F, 0.4F, "Range:2500m","Cargo Scanner Rating D", "Cargo Scanner")},
            { "hpt_cargoscanner_size0_class3", new ShipModule(128662522, 1.3F, 0.8F, "Range:3000m","Cargo Scanner Rating C", "Cargo Scanner")},
            { "hpt_cargoscanner_size0_class4", new ShipModule(128662523, 1.3F, 1.6F, "Range:3500m","Cargo Scanner Rating B", "Cargo Scanner")},
            { "hpt_cargoscanner_size0_class5", new ShipModule(128662524, 1.3F, 3.2F, "Range:4000m","Cargo Scanner Rating A", "Cargo Scanner")},
            { "hpt_chafflauncher_tiny", new ShipModule(128049513, 1.3F, 0.2F, "Ammo:10/1, Reload:10s, ThermL:4","Chaff Launcher", "Chaff Launcher")},
            { "hpt_electroniccountermeasure_tiny", new ShipModule(128049516, 1.3F, 0.2F, "Range:3000m, Reload:10s, ThermL:4","Electronic Countermeasure", "Electronic Countermeasure")},
            { "hpt_causticmissile_fixed_medium", new ShipModule(128833995, 4, 1.2F, "Ammo:64/8, Damage:5, Speed:750m/s, Reload:5s, ThermL:1.5","Caustic Missile Fixed Medium", "Caustic Missile")},
            { "hpt_slugshot_fixed_small", new ShipModule(128049448, 2, 0.45F, "Ammo:180/3, Damage:1.4, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.4","Fragment Cannon Fixed Small", "Fragment Cannon")},
            { "hpt_slugshot_gimbal_small", new ShipModule(128049451, 2, 0.59F, "Ammo:180/3, Damage:1, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.4","Fragment Cannon Gimbal Small", "Fragment Cannon")},
            { "hpt_slugshot_turret_small", new ShipModule(128049453, 2, 0.42F, "Ammo:180/3, Damage:0.7, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.2","Fragment Cannon Turret Small", "Fragment Cannon")},
            { "hpt_slugshot_fixed_medium", new ShipModule(128049449, 4, 0.74F, "Ammo:180/3, Damage:3, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.7","Fragment Cannon Fixed Medium", "Fragment Cannon")},
            { "hpt_slugshot_gimbal_medium", new ShipModule(128049452, 4, 1.03F, "Ammo:180/3, Damage:2.3, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.8","Fragment Cannon Gimbal Medium", "Fragment Cannon")},
            { "hpt_slugshot_turret_medium", new ShipModule(128049454, 4, 0.79F, "Ammo:180/3, Damage:1.7, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.4","Fragment Cannon Turret Medium", "Fragment Cannon")},
            { "hpt_slugshot_fixed_large", new ShipModule(128049450, 8, 1.02F, "Ammo:180/3, Damage:4.6, Range:2000m, Speed:667m/s, Reload:5s, ThermL:1.1","Fragment Cannon Fixed Large", "Fragment Cannon")},
            { "hpt_slugshot_gimbal_large", new ShipModule(128671321, 8, 1.55F, "Ammo:180/3, Damage:3.8, Range:2000m, Speed:667m/s, Reload:5s, ThermL:1.4","Fragment Cannon Gimbal Large", "Fragment Cannon")},
            { "hpt_slugshot_turret_large", new ShipModule(128671322, 8, 1.29F, "Ammo:180/3, Damage:3, Range:2000m, Speed:667m/s, Reload:5s, ThermL:0.7","Fragment Cannon Turret Large", "Fragment Cannon")},
            { "hpt_slugshot_fixed_large_range", new ShipModule(128671343, 8, 1.02F, "Ammo:180/3, Damage:4, Speed:1000m/s, Reload:5s, ThermL:1.1","Fragment Cannon Fixed Large Range", "Fragment Cannon")},
            { "hpt_cloudscanner_size0_class1", new ShipModule(128662525, 1.3F, 0.2F, "Range:2000m","Cloud Scanner Rating E", "Hyperspace Cloud Scanner")},
            { "hpt_cloudscanner_size0_class2", new ShipModule(128662526, 1.3F, 0.4F, "Range:2500m","Cloud Scanner Rating D", "Hyperspace Cloud Scanner")},
            { "hpt_cloudscanner_size0_class3", new ShipModule(128662527, 1.3F, 0.8F, "Range:3000m","Cloud Scanner Rating C", "Hyperspace Cloud Scanner")},
            { "hpt_cloudscanner_size0_class4", new ShipModule(128662528, 1.3F, 1.6F, "Range:3500m","Cloud Scanner Rating B", "Hyperspace Cloud Scanner")},
            { "hpt_cloudscanner_size0_class5", new ShipModule(128662529, 1.3F, 3.2F, "Range:4000m","Cloud Scanner Rating A", "Hyperspace Cloud Scanner")},
            { "hpt_guardian_gausscannon_fixed_small", new ShipModule(128891610, 2, 1.91F, "Ammo:80/1, Damage:22, Range:3000m, Reload:1s, ThermL:15","Guardian Gauss Cannon Fixed Small", "Guardian")},
            { "hpt_guardian_gausscannon_fixed_medium", new ShipModule(128833687, 4, 2.61F, "Ammo:80/1, Damage:38.5, Range:3000m, Reload:1s, ThermL:25","Guardian Gauss Cannon Fixed Medium", "Guardian")},
            { "hpt_guardian_plasmalauncher_fixed_small", new ShipModule(128891607, 2, 1.4F, "Ammo:200/15, Damage:1.7, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:4.2","Guardian Plasma Launcher Fixed Small", "Guardian")},
            { "hpt_guardian_plasmalauncher_turret_small", new ShipModule(128891606, 2, 1.6F, "Ammo:200/15, Damage:1.1, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:5","Guardian Plasma Launcher Turret Small", "Guardian")},
            { "hpt_guardian_plasmalauncher_fixed_medium", new ShipModule(128833998, 4, 2.13F, "Ammo:200/15, Damage:5, Range:3500m, Speed:1200m/s, Reload:3s, ThermL:5.2","Guardian Plasma Launcher Fixed Medium", "Guardian")},
            { "hpt_guardian_plasmalauncher_turret_medium", new ShipModule(128833999, 4, 2.01F, "Ammo:200/15, Damage:4, Range:3500m, Speed:1200m/s, Reload:3s, ThermL:5.8","Guardian Plasma Launcher Turret Medium", "Guardian")},
            { "hpt_guardian_plasmalauncher_fixed_large", new ShipModule(128834783, 8, 3.1F, "Ammo:200/15, Damage:3.4, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:6.2","Guardian Plasma Launcher Fixed Large", "Guardian")},
            { "hpt_guardian_plasmalauncher_turret_large", new ShipModule(128834784, 8, 2.53F, "Ammo:200/15, Damage:3.3, Range:3000m, Speed:1200m/s, Reload:3s, ThermL:6.4","Guardian Plasma Launcher Turret Large", "Guardian")},
            { "hpt_guardian_shardcannon_fixed_small", new ShipModule(128891609, 2, 0.87F, "Ammo:180/5, Damage:2, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:0.7","Guardian Shard Cannon Fixed Small", "Guardian")},
            { "hpt_guardian_shardcannon_turret_small", new ShipModule(128891608, 2, 0.72F, "Ammo:180/5, Damage:1.1, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:0.6","Guardian Shard Cannon Turret Small", "Guardian")},
            { "hpt_guardian_shardcannon_fixed_medium", new ShipModule(128834000, 4, 1.21F, "Ammo:180/5, Damage:3.7, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:1.2","Guardian Shard Cannon Fixed Medium", "Guardian")},
            { "hpt_guardian_shardcannon_turret_medium", new ShipModule(128834001, 4, 1.16F, "Ammo:180/5, Damage:2.4, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:1.1","Guardian Shard Cannon Turret Medium", "Guardian")},
            { "hpt_guardian_shardcannon_fixed_large", new ShipModule(128834778, 8, 1.68F, "Ammo:180/5, Damage:5.2, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:2.2","Guardian Shard Cannon Fixed Large", "Guardian")},
            { "hpt_guardian_shardcannon_turret_large", new ShipModule(128834779, 8, 1.39F, "Ammo:180/5, Damage:3.4, Range:1700m, Speed:1133m/s, Reload:5s, ThermL:2","Guardian Shard Cannon Turret Large", "Guardian")},
            { "hpt_heatsinklauncher_turret_tiny", new ShipModule(128049519, 1.3F, 0.2F, "Ammo:2/1, Reload:10s","Heat Sink Launcher Turret", "Heat Sink Launcher")},
            { "hpt_crimescanner_size0_class1", new ShipModule(128662530, 1.3F, 0.2F, "Range:2000m","Crime Scanner Rating E", "Crime Scanner")},
            { "hpt_crimescanner_size0_class2", new ShipModule(128662531, 1.3F, 0.4F, "Range:2500m","Crime Scanner Rating D", "Crime Scanner")},
            { "hpt_crimescanner_size0_class3", new ShipModule(128662532, 1.3F, 0.8F, "Range:3000m","Crime Scanner Rating C", "Crime Scanner")},
            { "hpt_crimescanner_size0_class4", new ShipModule(128662533, 1.3F, 1.6F, "Range:3500m","Crime Scanner Rating B", "Crime Scanner")},
            { "hpt_crimescanner_size0_class5", new ShipModule(128662534, 1.3F, 3.2F, "Range:4000m","Crime Scanner Rating A", "Crime Scanner")},
            { "hpt_minelauncher_fixed_small", new ShipModule(128049500, 2, 0.4F, "Ammo:36/1, Damage:44, Reload:2s, ThermL:5","Mine Launcher Fixed Small", "Mine Launcher")},
            { "hpt_minelauncher_fixed_small_impulse", new ShipModule(128671448, 2, 0.4F, "Ammo:36/1, Damage:32, Reload:2s, ThermL:5","Mine Launcher Fixed Small Impulse", "Mine Launcher")},
            { "hpt_minelauncher_fixed_medium", new ShipModule(128049501, 4, 0.4F, "Ammo:72/3, Damage:44, Reload:6.6s, ThermL:7.5","Mine Launcher Fixed Medium", "Mine Launcher")},
            { "hpt_mininglaser_fixed_small", new ShipModule(128049525, 2, 0.5F, "Damage:2, Range:500m, ThermL:2","Mining Laser Fixed Small", "Mining Laser")},
            { "hpt_mininglaser_turret_small", new ShipModule(128740819, 2, 0.5F, "Damage:2, Range:500m, ThermL:2","Mining Laser Turret Small", "Mining Laser")},
            { "hpt_mininglaser_fixed_small_advanced", new ShipModule(128671340, 2, 0.7F, "Damage:8, Range:2000m, ThermL:6","Mining Laser Fixed Small Advanced", "Mining Laser")},
            { "hpt_mininglaser_fixed_medium", new ShipModule(128049526, 2, 0.75F, "Damage:4, Range:500m, ThermL:4","Mining Laser Fixed Medium", "Mining Laser")},
            { "hpt_mininglaser_turret_medium", new ShipModule(128740820, 2, 0.75F, "Damage:4, Range:500m, ThermL:4","Mining Laser Turret Medium", "Mining Laser")},
            { "hpt_dumbfiremissilerack_fixed_small", new ShipModule(128666724, 2, 0.4F, "Ammo:16/8, Damage:50, Speed:750m/s, Reload:5s, ThermL:3.6","Dumbfire Missile Rack Fixed Small", "Missile Rack")},
            { "hpt_basicmissilerack_fixed_small", new ShipModule(128049492, 2, 0.6F, "Ammo:6/6, Damage:40, Speed:625m/s, Reload:12s, ThermL:3.6","Seeker Missile Rack Fixed Small", "Missile Rack")},
            { "hpt_dumbfiremissilerack_fixed_medium", new ShipModule(128666725, 4, 1.2F, "Ammo:48/12, Damage:50, Speed:750m/s, Reload:5s, ThermL:3.6","Dumbfire Missile Rack Fixed Medium", "Missile Rack")},
            { "hpt_basicmissilerack_fixed_medium", new ShipModule(128049493, 4, 1.2F, "Ammo:18/6, Damage:40, Speed:625m/s, Reload:12s, ThermL:3.6","Seeker Missile Rack Fixed Medium", "Missile Rack")},
            { "hpt_dumbfiremissilerack_fixed_medium_lasso", new ShipModule(128732552, 4, 1.2F, "Ammo:48/12, Damage:40, Speed:750m/s, Reload:5s, ThermL:3.6","Dumbfire Missile Rack Fixed Medium Lasso", "Missile Rack")},
            { "hpt_drunkmissilerack_fixed_medium", new ShipModule(128671344, 4, 1.2F, "Ammo:120/12, Damage:7.5, Speed:600m/s, Reload:5s, ThermL:3.6","Pack Hound Missile Rack Fixed Medium", "Missile Rack")},
            { "hpt_dumbfiremissilerack_fixed_large", new ShipModule(128891602, 8, 1.62F, "Ammo:96/12, Damage:50, Speed:750m/s, Reload:5s, ThermL:3.6","Dumbfire Missile Rack Fixed Large", "Missile Rack")},
            { "hpt_basicmissilerack_fixed_large", new ShipModule(128049494, 8, 1.62F, "Ammo:36/6, Damage:40, Speed:625m/s, Reload:12s, ThermL:3.6","Seeker Missile Rack Fixed Large", "Missile Rack")},
            { "hpt_multicannon_fixed_small", new ShipModule(128049455, 2, 0.28F, "Ammo:2100/100, Damage:1.1, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1","Multi Cannon Fixed Small", "Multi Cannon")},
            { "hpt_multicannon_gimbal_small", new ShipModule(128049459, 2, 0.37F, "Ammo:2100/90, Damage:0.8, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.1","Multi Cannon Gimbal Small", "Multi Cannon")},
            { "hpt_multicannon_turret_small", new ShipModule(128049462, 2, 0.26F, "Ammo:2100/90, Damage:0.6, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0","Multi Cannon Turret Small", "Multi Cannon")},
            { "hpt_multicannon_fixed_small_strong", new ShipModule(128671345, 2, 0.28F, "Ammo:1000/60, Damage:2.9, Range:4500m, Speed:1800m/s, Reload:4s, ThermL:0.2","Multi Cannon Fixed Small Strong", "Multi Cannon")},
            { "hpt_multicannon_fixed_medium", new ShipModule(128049456, 4, 0.46F, "Ammo:2100/100, Damage:2.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2","Multi Cannon Fixed Medium", "Multi Cannon")},
            { "hpt_multicannon_gimbal_medium", new ShipModule(128049460, 4, 0.64F, "Ammo:2100/90, Damage:1.6, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.2","Multi Cannon Gimbal Medium", "Multi Cannon")},
            { "hpt_multicannon_turret_medium", new ShipModule(128049463, 4, 0.5F, "Ammo:2100/90, Damage:1.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.1","Multi Cannon Turret Medium", "Multi Cannon")},
            { "hpt_multicannon_fixed_large", new ShipModule(128049457, 8, 0.64F, "Ammo:2100/100, Damage:3.9, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.3","Multi Cannon Fixed Large", "Multi Cannon")},
            { "hpt_multicannon_gimbal_large", new ShipModule(128049461, 8, 0.97F, "Ammo:2100/90, Damage:2.8, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.3","Multi Cannon Gimbal Large", "Multi Cannon")},
            { "hpt_multicannon_turret_large", new ShipModule(128049464, 8, 0.86F, "Ammo:2100/90, Damage:2.2, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.2","Multi Cannon Turret Large", "Multi Cannon")},
            { "hpt_multicannon_fixed_huge", new ShipModule(128049458, 16, 0.73F, "Ammo:2100/100, Damage:4.6, Range:4000m, Speed:1600m/s, Reload:4s, ThermL:0.4","Multi Cannon Fixed Huge", "Multi Cannon")},
            { "hpt_multicannon_gimbal_huge", new ShipModule(128681996, 16, 1.22F, "Ammo:2100/90, Damage:3.5, Range:4000m, Speed:1600m/s, Reload:5s, ThermL:0.5","Multi Cannon Gimbal Huge", "Multi Cannon")},
            { "hpt_plasmaaccelerator_fixed_medium", new ShipModule(128049465, 4, 1.43F, "Ammo:100/5, Damage:54.3, Range:3500m, Speed:875m/s, Reload:6s, ThermL:15.6","Plasma Accelerator Fixed Medium", "Plasma Accelerator")},
            { "hpt_plasmaaccelerator_fixed_large", new ShipModule(128049466, 8, 1.97F, "Ammo:100/5, Damage:83.4, Range:3500m, Speed:875m/s, Reload:6s, ThermL:21.8","Plasma Accelerator Fixed Large", "Plasma Accelerator")},
            { "hpt_plasmaaccelerator_fixed_large_advanced", new ShipModule(128671339, 8, 1.97F, "Ammo:300/20, Damage:34.5, Range:3500m, Speed:875m/s, Reload:6s, ThermL:11","Plasma Accelerator Fixed Large Advanced", "Plasma Accelerator")},
            { "hpt_plasmaaccelerator_fixed_huge", new ShipModule(128049467, 16, 2.63F, "Ammo:100/5, Damage:125.2, Range:3500m, Speed:875m/s, Reload:6s, ThermL:29.5","Plasma Accelerator Fixed Huge", "Plasma Accelerator")},
            { "hpt_plasmapointdefence_turret_tiny", new ShipModule(128049522, 0.5F, 0.2F, "Ammo:10000/12, Damage:0.2, Range:2500m, Speed:1000m/s, Reload:0.4s, ThermL:0.1","Plasma Point Defence Turret", "Point Defence")},
            { "hpt_pulselaser_fixed_small", new ShipModule(128049381, 2, 0.39F, "Damage:2.1, Range:3000m, ThermL:0.3","Pulse Laser Fixed Small", "Pulse Laser")},
            { "hpt_pulselaser_gimbal_small", new ShipModule(128049385, 2, 0.39F, "Damage:1.6, Range:3000m, ThermL:0.3","Pulse Laser Gimbal Small", "Pulse Laser")},
            { "hpt_pulselaser_turret_small", new ShipModule(128049388, 2, 0.38F, "Damage:1.2, Range:3000m, ThermL:0.2","Pulse Laser Turret Small", "Pulse Laser")},
            { "hpt_pulselaser_fixed_medium", new ShipModule(128049382, 4, 0.6F, "Damage:3.5, Range:3000m, ThermL:0.6","Pulse Laser Fixed Medium", "Pulse Laser")},
            { "hpt_pulselaser_gimbal_medium", new ShipModule(128049386, 4, 0.6F, "Damage:2.7, Range:3000m, ThermL:0.5","Pulse Laser Gimbal Medium", "Pulse Laser")},
            { "hpt_pulselaser_turret_medium", new ShipModule(128049389, 4, 0.58F, "Damage:2.1, Range:3000m, ThermL:0.3","Pulse Laser Turret Medium", "Pulse Laser")},
            { "hpt_pulselaser_fixed_medium_disruptor", new ShipModule(128671342, 4, 0.7F, "Damage:2.8, ThermL:1","Pulse Laser Fixed Medium Disruptor", "Pulse Laser")},
            { "hpt_pulselaser_fixed_large", new ShipModule(128049383, 8, 0.9F, "Damage:6, Range:3000m, ThermL:1","Pulse Laser Fixed Large", "Pulse Laser")},
            { "hpt_pulselaser_gimbal_large", new ShipModule(128049387, 8, 0.92F, "Damage:4.6, Range:3000m, ThermL:0.9","Pulse Laser Gimbal Large", "Pulse Laser")},
            { "hpt_pulselaser_turret_large", new ShipModule(128049390, 8, 0.89F, "Damage:3.5, Range:3000m, ThermL:0.6","Pulse Laser Turret Large", "Pulse Laser")},
            { "hpt_pulselaser_fixed_huge", new ShipModule(128049384, 16, 1.33F, "Damage:10.2, Range:3000m, ThermL:1.6","Pulse Laser Fixed Huge", "Pulse Laser")},
            { "hpt_pulselaser_gimbal_huge", new ShipModule(128681995, 16, 1.37F, "Damage:7.8, Range:3000m, ThermL:1.6","Pulse Laser Gimbal Huge", "Pulse Laser")},
            { "hpt_mrascanner_size0_class5", new ShipModule(128915722, 1.3F, 3.2F, null,"MRA Scanner Rating A", "Pulse Wave Analyser")},
            { "hpt_mrascanner_size0_class4", new ShipModule(128915721, 1.3F, 1.6F, null,"Mrascanner Size 0 Rating B", "Pulse Wave Analyser")},
            { "hpt_mrascanner_size0_class3", new ShipModule(128915720, 1.3F, 0.8F, null,"Mrascanner Size 0 Rating C", "Pulse Wave Analyser")},
            { "hpt_mrascanner_size0_class2", new ShipModule(128915719, 1.3F, 0.4F, null,"Mrascanner Size 0 Rating D", "Pulse Wave Analyser")},
            { "hpt_mrascanner_size0_class1", new ShipModule(128915718, 1.3F, 0.2F, null,"Mrascanner Size 0 Rating E", "Pulse Wave Analyser")},
            { "hpt_railgun_fixed_small", new ShipModule(128049488, 2, 1.15F, "Ammo:80/1, Damage:23.3, Range:3000m, Reload:1s, ThermL:12","Railgun Fixed Small", "Railgun")},
            { "hpt_railgun_fixed_medium", new ShipModule(128049489, 4, 1.63F, "Ammo:80/1, Damage:41.5, Range:3000m, Reload:1s, ThermL:20","Railgun Fixed Medium", "Railgun")},
            { "hpt_railgun_fixed_medium_burst", new ShipModule(128671341, 4, 1.63F, "Ammo:240/3, Damage:15, Range:3000m, Reload:1s, ThermL:11","Railgun Fixed Medium Burst", "Railgun")},
            { "hpt_flakmortar_fixed_medium", new ShipModule(128785626, 4, 1.2F, "Ammo:32/1, Damage:34, Speed:550m/s, Reload:2s, ThermL:3.6","Flak Mortar Fixed Medium", "Flak Mortar")},
            { "hpt_flakmortar_turret_medium", new ShipModule(128793058, 4, 1.2F, "Ammo:32/1, Damage:34, Speed:550m/s, Reload:2s, ThermL:3.6","Flak Mortar Turret Medium", "Flak Mortar")},
            { "hpt_flechettelauncher_fixed_medium", new ShipModule(128833996, 4, 1.2F, "Ammo:72/1, Damage:13, Speed:550m/s, Reload:2s, ThermL:3.6","Flechette Launcher Fixed Medium", "Flechette Launcher")},
            { "hpt_flechettelauncher_turret_medium", new ShipModule(128833997, 4, 1.2F, "Ammo:72/1, Damage:13, Speed:550m/s, Reload:2s, ThermL:3.6","Flechette Launcher Turret Medium", "Flechette Launcher")},
            { "hpt_mining_seismchrgwarhd_turret_medium", new ShipModule(128915461, 4, 1.2F, "Ammo:72/1, Damage:15, Range:3000m, Speed:350m/s, ThermL:3.6","Mining Seismic Charge Warhead Turret Medium", "Mining")},
            { "hpt_mining_seismchrgwarhd_fixed_medium", new ShipModule(128049381, 4, 1.2F, "Ammo:72/1, Damage:15, Range:3000m, Speed:350m/s, ThermL:3.6","Mining Seismic Charge Warhead Fixed Medium", "Mining")},
            { "hpt_shieldbooster_size0_class1", new ShipModule(128668532, 0.5F, 0.2F, "Boost:4.0%, Explosive:0%, Kinetic:0%, Thermal:0%","Shield Booster Rating E", "Shield Booster")},
            { "hpt_shieldbooster_size0_class2", new ShipModule(128668533, 1, 0.5F, "Boost:8.0%, Explosive:0%, Kinetic:0%, Thermal:0%","Shield Booster Rating D", "Shield Booster")},
            { "hpt_shieldbooster_size0_class3", new ShipModule(128668534, 2, 0.7F, "Boost:12.0%, Explosive:0%, Kinetic:0%, Thermal:0%","Shield Booster Rating C", "Shield Booster")},
            { "hpt_shieldbooster_size0_class4", new ShipModule(128668535, 3, 1, "Boost:16.0%, Explosive:0%, Kinetic:0%, Thermal:0%","Shield Booster Rating B", "Shield Booster")},
            { "hpt_shieldbooster_size0_class5", new ShipModule(128668536, 3.5F, 1.2F, "Boost:20.0%, Explosive:0%, Kinetic:0%, Thermal:0%","Shield Booster Rating A", "Shield Booster")},
            { "hpt_plasmashockcannon_fixed_large", new ShipModule(128834780, 8, 0.89F, "Ammo:240/16, Damage:18.1, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:2.7","Plasma Shock Cannon Fixed Large", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_gimbal_large", new ShipModule(128834781, 8, 0.89F, "Ammo:240/16, Damage:14.9, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:3.1","Plasma Shock Cannon Gimbal Large", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_turret_large", new ShipModule(128834782, 8, 0.64F, "Ammo:240/16, Damage:12.3, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:2.2","Plasma Shock Cannon Turret Large", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_gimbal_medium", new ShipModule(128834003, 4, 0.61F, "Ammo:240/16, Damage:10.2, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:2.1","Plasma Shock Cannon Gimbal Medium", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_fixed_medium", new ShipModule(128834002, 4, 0.57F, "Ammo:240/16, Damage:13, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.8","Plasma Shock Cannon Fixed Medium", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_turret_medium", new ShipModule(128834004, 4, 0.5F, "Ammo:240/16, Damage:9, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.2","Plasma Shock Cannon Turret Medium", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_fixed_small", new ShipModule(128891605, 2, 0.41F, "Ammo:240/16, Damage:8.6, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.1","Plasma Shock Cannon Fixed Small", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_gimbal_small", new ShipModule(128891604, 2, 0.47F, "Ammo:240/16, Damage:6.9, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:1.5","Plasma Shock Cannon Gimbal Small", "Plasma Shock Cannon")},
            { "hpt_plasmashockcannon_turret_small", new ShipModule(128891603, 2, 0.54F, "Ammo:240/16, Damage:4.5, Range:3000m, Speed:1200m/s, Reload:6s, ThermL:0.7","Plasma Shock Cannon Turret Small", "Plasma Shock Cannon")},
            { "hpt_antiunknownshutdown_tiny", new ShipModule(128771884, 1.3F, 0.2F, "Range:3000m","Shutdown Field Neutraliser", "Shutdown Field Neutraliser")},
            { "hpt_mining_subsurfdispmisle_fixed_medium", new ShipModule(128915457, 4, 1.01F, "Ammo:96/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.9","Mining Sub Surface Displacement Missile Fixed Medium", "Mining")},
            { "hpt_mining_subsurfdispmisle_turret_medium", new ShipModule(128049381, 4, 0.93F, "Ammo:96/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.9","Mining Subsurface Displacement Missile Turret Medium", "Mining")},
            { "hpt_mining_subsurfdispmisle_fixed_small", new ShipModule(128915455, 2, 0.42F, "Ammo:32/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.2","Mining Sub Surface Displacement Missile Fixed Small", "Mining")},
            { "hpt_mining_subsurfdispmisle_turret_small", new ShipModule(128049381, 2, 0.53F, "Ammo:32/1, Damage:5, Range:3000m, Speed:550m/s, Reload:2s, ThermL:2.2","Mining Subsurface Displacement Missile Turret Small", "Mining")},
            { "hpt_advancedtorppylon_fixed_small", new ShipModule(128049509, 2, 0.4F, "Ammo:1/1, Damage:120, Speed:250m/s, Reload:5s, ThermL:45","Advanced Torp Pylon Fixed Small", "Missile Rack")},
            { "hpt_advancedtorppylon_fixed_medium", new ShipModule(128049510, 4, 0.4F, "Ammo:2/1, Damage:120, Speed:250m/s, Reload:5s, ThermL:50","Advanced Torp Pylon Fixed Medium", "Missile Rack")},
            { "hpt_advancedtorppylon_fixed_large", new ShipModule(128049511, 8, 0.6F, "Ammo:4/4, Damage:120, Speed:250m/s, Reload:5s, ThermL:55","Advanced Torp Pylon Fixed Large", "Missile Rack")},
            { "hpt_xenoscanner_basic_tiny", new ShipModule(128793115, 1.3F, 0.2F, "Range:500m","Xeno Scanner Seeker", "Xeno Scanner")},

            ///////////////////////////////////// FROM INTERNAL FOLDER SCAN

            { "int_repairer_size1_class1", new ShipModule(128667598, 0, 0.54F, "Ammo:1000, Repair:12","Auto Field Maintenance Class 1 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size1_class2", new ShipModule(128667606, 0, 0.72F, "Ammo:900, Repair:14.4","Auto Field Maintenance Class 1 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size1_class3", new ShipModule(128667614, 0, 0.9F, "Ammo:1000, Repair:20","Auto Field Maintenance Class 1 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size1_class4", new ShipModule(128667622, 0, 1.04F, "Ammo:1200, Repair:27.6","Auto Field Maintenance Class 1 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size1_class5", new ShipModule(128667630, 0, 1.26F, "Ammo:1100, Repair:30.8","Auto Field Maintenance Class 1 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size2_class1", new ShipModule(128667599, 0, 0.68F, "Ammo:2300, Repair:27.6","Auto Field Maintenance Class 2 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size2_class2", new ShipModule(128667607, 0, 0.9F, "Ammo:2100, Repair:33.6","Auto Field Maintenance Class 2 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size2_class3", new ShipModule(128667615, 0, 1.13F, "Ammo:2300, Repair:46","Auto Field Maintenance Class 2 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size2_class4", new ShipModule(128667623, 0, 1.29F, "Ammo:2800, Repair:64.4","Auto Field Maintenance Class 2 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size2_class5", new ShipModule(128667631, 0, 1.58F, "Ammo:2500, Repair:70","Auto Field Maintenance Class 2 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size3_class1", new ShipModule(128667600, 0, 0.81F, "Ammo:3600, Repair:43.2","Auto Field Maintenance Class 3 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size3_class2", new ShipModule(128667608, 0, 1.08F, "Ammo:3200, Repair:51.2","Auto Field Maintenance Class 3 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size3_class3", new ShipModule(128667616, 0, 1.35F, "Ammo:3600, Repair:72","Auto Field Maintenance Class 3 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size3_class4", new ShipModule(128667624, 0, 1.55F, "Ammo:4300, Repair:98.9","Auto Field Maintenance Class 3 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size3_class5", new ShipModule(128667632, 0, 1.89F, "Ammo:4000, Repair:112","Auto Field Maintenance Class 3 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size4_class1", new ShipModule(128667601, 0, 0.99F, "Ammo:4900, Repair:58.8","Auto Field Maintenance Class 4 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size4_class2", new ShipModule(128667609, 0, 1.32F, "Ammo:4400, Repair:70.4","Auto Field Maintenance Class 4 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size4_class3", new ShipModule(128667617, 0, 1.65F, "Ammo:4900, Repair:98","Auto Field Maintenance Class 4 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size4_class4", new ShipModule(128667625, 0, 1.9F, "Ammo:5900, Repair:135.7","Auto Field Maintenance Class 4 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size4_class5", new ShipModule(128667633, 0, 2.31F, "Ammo:5400, Repair:151.2","Auto Field Maintenance Class 4 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size5_class1", new ShipModule(128667602, 0, 1.17F, "Ammo:6100, Repair:73.2","Auto Field Maintenance Class 5 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size5_class2", new ShipModule(128667610, 0, 1.56F, "Ammo:5500, Repair:88","Auto Field Maintenance Class 5 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size5_class3", new ShipModule(128667618, 0, 1.95F, "Ammo:6100, Repair:122","Auto Field Maintenance Class 5 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size5_class4", new ShipModule(128667626, 0, 2.24F, "Ammo:7300, Repair:167.9","Auto Field Maintenance Class 5 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size5_class5", new ShipModule(128667634, 0, 2.73F, "Ammo:6700, Repair:187.6","Auto Field Maintenance Class 5 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size6_class1", new ShipModule(128667603, 0, 1.4F, "Ammo:7400, Repair:88.8","Auto Field Maintenance Class 6 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size6_class2", new ShipModule(128667611, 0, 1.86F, "Ammo:6700, Repair:107.2","Auto Field Maintenance Class 6 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size6_class3", new ShipModule(128667619, 0, 2.33F, "Ammo:7400, Repair:148","Auto Field Maintenance Class 6 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size6_class4", new ShipModule(128667627, 0, 2.67F, "Ammo:8900, Repair:204.7","Auto Field Maintenance Class 6 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size6_class5", new ShipModule(128667635, 0, 3.26F, "Ammo:8100, Repair:226.8","Auto Field Maintenance Class 6 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size7_class1", new ShipModule(128667604, 0, 1.58F, "Ammo:8700, Repair:104.4","Auto Field Maintenance Class 7 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size7_class2", new ShipModule(128667612, 0, 2.1F, "Ammo:7800, Repair:124.8","Auto Field Maintenance Class 7 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size7_class3", new ShipModule(128667620, 0, 2.63F, "Ammo:8700, Repair:174","Auto Field Maintenance Class 7 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size7_class4", new ShipModule(128667628, 0, 3.02F, "Ammo:10400, Repair:239.2","Auto Field Maintenance Class 7 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size7_class5", new ShipModule(128667636, 0, 3.68F, "Ammo:9600, Repair:268.8","Auto Field Maintenance Class 7 Rating A", "Auto Field Maintenance")},
            { "int_repairer_size8_class1", new ShipModule(128667605, 0, 1.8F, "Ammo:10000, Repair:120","Auto Field Maintenance Class 8 Rating E", "Auto Field Maintenance")},
            { "int_repairer_size8_class2", new ShipModule(128667613, 0, 2.4F, "Ammo:9000, Repair:144","Auto Field Maintenance Class 8 Rating D", "Auto Field Maintenance")},
            { "int_repairer_size8_class3", new ShipModule(128667621, 0, 3, "Ammo:10000, Repair:200","Auto Field Maintenance Class 8 Rating C", "Auto Field Maintenance")},
            { "int_repairer_size8_class4", new ShipModule(128667629, 0, 3.45F, "Ammo:12000, Repair:276","Auto Field Maintenance Class 8 Rating B", "Auto Field Maintenance")},
            { "int_repairer_size8_class5", new ShipModule(128667637, 0, 4.2F, "Ammo:11000, Repair:308","Auto Field Maintenance Class 8 Rating A", "Auto Field Maintenance")},
            { "int_shieldgenerator_size1_class3_fast", new ShipModule(128671331, 1.3F, 1.2F, "OptMass:25t, MaxMass:63t, MinMass:13t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 1 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size2_class3_fast", new ShipModule(128671332, 2.5F, 1.5F, "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size3_class3_fast", new ShipModule(128671333, 5, 1.8F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size4_class3_fast", new ShipModule(128671334, 10, 2.2F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size5_class3_fast", new ShipModule(128671335, 20, 2.6F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size6_class3_fast", new ShipModule(128671336, 40, 3.1F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size7_class3_fast", new ShipModule(128671337, 80, 3.5F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating C Fast", "Shield Generator")},
            { "int_shieldgenerator_size8_class3_fast", new ShipModule(128671338, 160, 4, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating C Fast", "Shield Generator")},
            { "int_passengercabin_size3_class2", new ShipModule(128734692, 5, 0, "Passengers:3","Business Class Passenger Cabin Class 3 Rating D", "Passenger Cabin")},
            { "int_passengercabin_size4_class2", new ShipModule(128727923, 10, 0, "Passengers:6","Business Class Passenger Cabin Class 4 Rating D", "Passenger Cabin")},
            { "int_passengercabin_size5_class2", new ShipModule(128734694, 20, 0, "Passengers:10","Business Class Passenger Cabin Class 5 Rating D", "Passenger Cabin")},
            { "int_passengercabin_size6_class2", new ShipModule(128727927, 40, 0, "Passengers:16","Business Class Passenger Cabin Class 6 Rating D", "Passenger Cabin")},
            { "int_cargorack_size1_class1", new ShipModule(128064338, 0, 0, "Size:2t","Cargo Rack Class 1 Rating E", "Cargo Rack")},
            { "int_cargorack_size2_class1", new ShipModule(128064339, 0, 0, "Size:4t","Cargo Rack Class 2 Rating E", "Cargo Rack")},
            { "int_cargorack_size3_class1", new ShipModule(128064340, 0, 0, "Size:8t","Cargo Rack Class 3 Rating E", "Cargo Rack")},
            { "int_cargorack_size4_class1", new ShipModule(128064341, 0, 0, "Size:16t","Cargo Rack Class 4 Rating E", "Cargo Rack")},
            { "int_cargorack_size5_class1", new ShipModule(128064342, 0, 0, "Size:32t","Cargo Rack Class 5 Rating E", "Cargo Rack")},
            { "int_cargorack_size6_class1", new ShipModule(128064343, 0, 0, "Size:64t","Cargo Rack Class 6 Rating E", "Cargo Rack")},
            { "int_cargorack_size7_class1", new ShipModule(128064344, 0, 0, "Size:128t","Cargo Rack Class 7 Rating E", "Cargo Rack")},
            { "int_cargorack_size8_class1", new ShipModule(128064345, 0, 0, "Size:256t","Cargo Rack Class 8 Rating E", "Cargo Rack")},
            { "int_corrosionproofcargorack_size1_class1", new ShipModule(128681641, 0, 0, "Size:1t","Corrosion Proof Cargo Rack Class 1 Rating E", "Cargo Rack")},
            { "int_corrosionproofcargorack_size1_class2", new ShipModule(128681992, 0, 0, "Size:2t","Corrosion Proof Cargo Rack Class 1 Rating D", "Cargo Rack")},
            { "int_corrosionproofcargorack_size4_class1", new ShipModule(128833944, 0, 0, "Size:16t","Corrosion Proof Cargo Rack Class 4 Rating E", "Cargo Rack")},
            { "int_dronecontrol_collection_size1_class1", new ShipModule(128671229, 0.5F, 0.14F, "Time:300s, Range:0.8km","Collection Drone Controller Class 1 Rating E", "Limpet Controller")},
            { "int_dronecontrol_collection_size1_class2", new ShipModule(128671230, 0.5F, 0.18F, "Time:600s, Range:0.6km","Collection Drone Controller Class 1 Rating D", "Limpet Controller")},
            { "int_dronecontrol_collection_size1_class3", new ShipModule(128671231, 1.3F, 0.23F, "Time:510s, Range:1km","Collection Drone Controller Class 1 Rating C", "Limpet Controller")},
            { "int_dronecontrol_collection_size1_class4", new ShipModule(128671232, 2, 0.28F, "Time:420s, Range:1.4km","Collection Drone Controller Class 1 Rating B", "Limpet Controller")},
            { "int_dronecontrol_collection_size1_class5", new ShipModule(128671233, 2, 0.32F, "Time:720s, Range:1.2km","Collection Drone Controller Class 1 Rating A", "Limpet Controller")},
            { "int_dronecontrol_collection_size3_class1", new ShipModule(128671234, 2, 0.2F, "Time:300s, Range:0.9km","Collection Drone Controller Class 3 Rating E", "Limpet Controller")},
            { "int_dronecontrol_collection_size3_class2", new ShipModule(128671235, 2, 0.27F, "Time:600s, Range:0.7km","Collection Drone Controller Class 3 Rating D", "Limpet Controller")},
            { "int_dronecontrol_collection_size3_class3", new ShipModule(128671236, 5, 0.34F, "Time:510s, Range:1.1km","Collection Drone Controller Class 3 Rating C", "Limpet Controller")},
            { "int_dronecontrol_collection_size3_class4", new ShipModule(128671237, 8, 0.41F, "Time:420s, Range:1.5km","Collection Drone Controller Class 3 Rating B", "Limpet Controller")},
            { "int_dronecontrol_collection_size3_class5", new ShipModule(128671238, 8, 0.48F, "Time:720s, Range:1.3km","Collection Drone Controller Class 3 Rating A", "Limpet Controller")},
            { "int_dronecontrol_collection_size5_class1", new ShipModule(128671239, 8, 0.3F, "Time:300s, Range:1km","Collection Drone Controller Class 5 Rating E", "Limpet Controller")},
            { "int_dronecontrol_collection_size5_class2", new ShipModule(128671240, 8, 0.4F, "Time:600s, Range:0.8km","Collection Drone Controller Class 5 Rating D", "Limpet Controller")},
            { "int_dronecontrol_collection_size5_class3", new ShipModule(128671241, 20, 0.5F, "Time:510s, Range:1.3km","Collection Drone Controller Class 5 Rating C", "Limpet Controller")},
            { "int_dronecontrol_collection_size5_class4", new ShipModule(128671242, 32, 0.6F, "Time:420s, Range:1.8km","Collection Drone Controller Class 5 Rating B", "Limpet Controller")},
            { "int_dronecontrol_collection_size5_class5", new ShipModule(128671243, 32, 0.7F, "Time:720s, Range:1.6km","Collection Drone Controller Class 5 Rating A", "Limpet Controller")},
            { "int_dronecontrol_collection_size7_class1", new ShipModule(128671244, 32, 0.41F, "Time:300s, Range:1.4km","Collection Drone Controller Class 7 Rating E", "Limpet Controller")},
            { "int_dronecontrol_collection_size7_class2", new ShipModule(128671245, 32, 0.55F, "Time:600s, Range:1km","Collection Drone Controller Class 7 Rating D", "Limpet Controller")},
            { "int_dronecontrol_collection_size7_class3", new ShipModule(128671246, 80, 0.69F, "Time:510s, Range:1.7km","Collection Drone Controller Class 7 Rating C", "Limpet Controller")},
            { "int_dronecontrol_collection_size7_class4", new ShipModule(128671247, 128, 0.83F, "Time:420s, Range:2.4km","Collection Drone Controller Class 7 Rating B", "Limpet Controller")},
            { "int_dronecontrol_collection_size7_class5", new ShipModule(128671248, 128, 0.97F, "Time:720s, Range:2km","Collection Drone Controller Class 7 Rating A", "Limpet Controller")},
            { "int_dronecontrol_decontamination_size1_class1", new ShipModule(128793941, 1.3F, 0.18F, "Range:0.6km","Decontamination Drone Controller Class 1 Rating E", "Drone Control Decontamination")},
            { "int_dronecontrol_decontamination_size3_class1", new ShipModule(128793942, 2, 0.2F, "Range:0.9km","Decontamination Drone Controller Class 3 Rating E", "Drone Control Decontamination")},
            { "int_dronecontrol_decontamination_size5_class1", new ShipModule(128793943, 20, 0.5F, "Range:1.3km","Decontamination Drone Controller Class 5 Rating E", "Drone Control Decontamination")},
            { "int_dronecontrol_decontamination_size7_class1", new ShipModule(128793944, 128, 0.97F, "Range:2km","Decontamination Drone Controller Class 7 Rating E", "Drone Control Decontamination")},
            { "int_dockingcomputer_standard", new ShipModule(128049549, 0, 0.39F, null,"Docking Computer Standard", "Docking Computer")},
            { "int_dockingcomputer_advanced", new ShipModule(128935155, 0, 0.45F, null,"Docking Computer Advanced", "Docking Computer")},
            { "int_passengercabin_size2_class1", new ShipModule(128734690, 2.5F, 0, "Passengers:2","Economy Passenger Cabin Class 2 Rating E", "Passenger Cabin")},
            { "int_passengercabin_size3_class1", new ShipModule(128734691, 5, 0, "Passengers:4","Economy Passenger Cabin Class 3 Rating E", "Passenger Cabin")},
            { "int_passengercabin_size4_class1", new ShipModule(128727922, 10, 0, "Passengers:8","Economy Passenger Cabin Class 4 Rating E", "Passenger Cabin")},
            { "int_passengercabin_size5_class1", new ShipModule(128734693, 20, 0, "Passengers:16","Economy Passenger Cabin Class 5 Rating E", "Passenger Cabin")},
            { "int_passengercabin_size6_class1", new ShipModule(128727926, 40, 0, "Passengers:32","Economy Passenger Cabin Class 6 Rating E", "Passenger Cabin")},
            { "int_fighterbay_size5_class1", new ShipModule(128727930, 20, 0.25F, "Rebuilds:6t","Fighter Hangar Class 5 Rating E", "Fighter Bay")},
            { "int_fighterbay_size6_class1", new ShipModule(128727931, 40, 0.35F, "Rebuilds:8t","Fighter Hangar Class 6 Rating E", "Fighter Bay")},
            { "int_fighterbay_size7_class1", new ShipModule(128727932, 60, 0.35F, "Rebuilds:15t","Fighter Hangar Class 7 Rating E", "Fighter Bay")},
            { "int_passengercabin_size4_class3", new ShipModule(128727924, 10, 0, "Passengers:3","First Class Passenger Cabin Class 4 Rating C", "Passenger Cabin")},
            { "int_passengercabin_size5_class3", new ShipModule(128734695, 20, 0, "Passengers:6","First Class Passenger Cabin Class 5 Rating C", "Passenger Cabin")},
            { "int_passengercabin_size6_class3", new ShipModule(128727928, 40, 0, "Passengers:12","First Class Passenger Cabin Class 6 Rating C", "Passenger Cabin")},
            { "int_fsdinterdictor_size1_class1", new ShipModule(128666704, 1.3F, 0.14F, null,"FSD Interdictor Class 1 Rating E", "FSD Interdictor")},
            { "int_fsdinterdictor_size1_class2", new ShipModule(128666708, 0.5F, 0.18F, null,"FSD Interdictor Class 1 Rating D", "FSD Interdictor")},
            { "int_fsdinterdictor_size1_class3", new ShipModule(128666712, 1.3F, 0.23F, null,"FSD Interdictor Class 1 Rating C", "FSD Interdictor")},
            { "int_fsdinterdictor_size1_class4", new ShipModule(128666716, 2, 0.28F, null,"FSD Interdictor Class 1 Rating B", "FSD Interdictor")},
            { "int_fsdinterdictor_size1_class5", new ShipModule(128666720, 1.3F, 0.32F, null,"FSD Interdictor Class 1 Rating A", "FSD Interdictor")},
            { "int_fsdinterdictor_size2_class1", new ShipModule(128666705, 2.5F, 0.17F, null,"FSD Interdictor Class 2 Rating E", "FSD Interdictor")},
            { "int_fsdinterdictor_size2_class2", new ShipModule(128666709, 1, 0.22F, null,"FSD Interdictor Class 2 Rating D", "FSD Interdictor")},
            { "int_fsdinterdictor_size2_class3", new ShipModule(128666713, 2.5F, 0.28F, null,"FSD Interdictor Class 2 Rating C", "FSD Interdictor")},
            { "int_fsdinterdictor_size2_class4", new ShipModule(128666717, 4, 0.34F, null,"FSD Interdictor Class 2 Rating B", "FSD Interdictor")},
            { "int_fsdinterdictor_size2_class5", new ShipModule(128666721, 2.5F, 0.39F, null,"FSD Interdictor Class 2 Rating A", "FSD Interdictor")},
            { "int_fsdinterdictor_size3_class1", new ShipModule(128666706, 5, 0.2F, null,"FSD Interdictor Class 3 Rating E", "FSD Interdictor")},
            { "int_fsdinterdictor_size3_class2", new ShipModule(128666710, 2, 0.27F, null,"FSD Interdictor Class 3 Rating D", "FSD Interdictor")},
            { "int_fsdinterdictor_size3_class3", new ShipModule(128666714, 5, 0.34F, null,"FSD Interdictor Class 3 Rating C", "FSD Interdictor")},
            { "int_fsdinterdictor_size3_class4", new ShipModule(128666718, 8, 0.41F, null,"FSD Interdictor Class 3 Rating B", "FSD Interdictor")},
            { "int_fsdinterdictor_size3_class5", new ShipModule(128666722, 5, 0.48F, null,"FSD Interdictor Class 3 Rating A", "FSD Interdictor")},
            { "int_fsdinterdictor_size4_class1", new ShipModule(128666707, 10, 0.25F, null,"FSD Interdictor Class 4 Rating E", "FSD Interdictor")},
            { "int_fsdinterdictor_size4_class2", new ShipModule(128666711, 4, 0.33F, null,"FSD Interdictor Class 4 Rating D", "FSD Interdictor")},
            { "int_fsdinterdictor_size4_class3", new ShipModule(128666715, 10, 0.41F, null,"FSD Interdictor Class 4 Rating C", "FSD Interdictor")},
            { "int_fsdinterdictor_size4_class4", new ShipModule(128666719, 16, 0.49F, null,"FSD Interdictor Class 4 Rating B", "FSD Interdictor")},
            { "int_fsdinterdictor_size4_class5", new ShipModule(128666723, 10, 0.57F, null,"FSD Interdictor Class 4 Rating A", "FSD Interdictor")},
            { "int_fuelscoop_size1_class1", new ShipModule(128666644, 0, 0.14F, "Rate:18","Fuel Scoop Class 1 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size1_class2", new ShipModule(128666652, 0, 0.18F, "Rate:24","Fuel Scoop Class 1 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size1_class3", new ShipModule(128666660, 0, 0.23F, "Rate:30","Fuel Scoop Class 1 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size1_class4", new ShipModule(128666668, 0, 0.28F, "Rate:36","Fuel Scoop Class 1 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size1_class5", new ShipModule(128666676, 0, 0.32F, "Rate:42","Fuel Scoop Class 1 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size2_class1", new ShipModule(128666645, 0, 0.17F, "Rate:32","Fuel Scoop Class 2 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size2_class2", new ShipModule(128666653, 0, 0.22F, "Rate:43","Fuel Scoop Class 2 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size2_class3", new ShipModule(128666661, 0, 0.28F, "Rate:54","Fuel Scoop Class 2 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size2_class4", new ShipModule(128666669, 0, 0.34F, "Rate:65","Fuel Scoop Class 2 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size2_class5", new ShipModule(128666677, 0, 0.39F, "Rate:75","Fuel Scoop Class 2 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size3_class1", new ShipModule(128666646, 0, 0.2F, "Rate:75","Fuel Scoop Class 3 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size3_class2", new ShipModule(128666654, 0, 0.27F, "Rate:100","Fuel Scoop Class 3 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size3_class3", new ShipModule(128666662, 0, 0.34F, "Rate:126","Fuel Scoop Class 3 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size3_class4", new ShipModule(128666670, 0, 0.41F, "Rate:151","Fuel Scoop Class 3 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size3_class5", new ShipModule(128666678, 0, 0.48F, "Rate:176","Fuel Scoop Class 3 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size4_class1", new ShipModule(128666647, 0, 0.25F, "Rate:147","Fuel Scoop Class 4 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size4_class2", new ShipModule(128666655, 0, 0.33F, "Rate:196","Fuel Scoop Class 4 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size4_class3", new ShipModule(128666663, 0, 0.41F, "Rate:245","Fuel Scoop Class 4 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size4_class4", new ShipModule(128666671, 0, 0.49F, "Rate:294","Fuel Scoop Class 4 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size4_class5", new ShipModule(128666679, 0, 0.57F, "Rate:342","Fuel Scoop Class 4 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size5_class1", new ShipModule(128666648, 0, 0.3F, "Rate:247","Fuel Scoop Class 5 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size5_class2", new ShipModule(128666656, 0, 0.4F, "Rate:330","Fuel Scoop Class 5 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size5_class3", new ShipModule(128666664, 0, 0.5F, "Rate:412","Fuel Scoop Class 5 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size5_class4", new ShipModule(128666672, 0, 0.6F, "Rate:494","Fuel Scoop Class 5 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size5_class5", new ShipModule(128666680, 0, 0.7F, "Rate:577","Fuel Scoop Class 5 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size6_class1", new ShipModule(128666649, 0, 0.35F, "Rate:376","Fuel Scoop Class 6 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size6_class2", new ShipModule(128666657, 0, 0.47F, "Rate:502","Fuel Scoop Class 6 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size6_class3", new ShipModule(128666665, 0, 0.59F, "Rate:627","Fuel Scoop Class 6 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size6_class4", new ShipModule(128666673, 0, 0.71F, "Rate:752","Fuel Scoop Class 6 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size6_class5", new ShipModule(128666681, 0, 0.83F, "Rate:878","Fuel Scoop Class 6 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size7_class1", new ShipModule(128666650, 0, 0.41F, "Rate:534","Fuel Scoop Class 7 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size7_class2", new ShipModule(128666658, 0, 0.55F, "Rate:712","Fuel Scoop Class 7 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size7_class3", new ShipModule(128666666, 0, 0.69F, "Rate:890","Fuel Scoop Class 7 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size7_class4", new ShipModule(128666674, 0, 0.83F, "Rate:1068","Fuel Scoop Class 7 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size7_class5", new ShipModule(128666682, 0, 0.97F, "Rate:1245","Fuel Scoop Class 7 Rating A", "Fuel Scoop")},
            { "int_fuelscoop_size8_class1", new ShipModule(128666651, 0, 0.48F, "Rate:720","Fuel Scoop Class 8 Rating E", "Fuel Scoop")},
            { "int_fuelscoop_size8_class2", new ShipModule(128666659, 0, 0.64F, "Rate:960","Fuel Scoop Class 8 Rating D", "Fuel Scoop")},
            { "int_fuelscoop_size8_class3", new ShipModule(128666667, 0, 0.8F, "Rate:1200","Fuel Scoop Class 8 Rating C", "Fuel Scoop")},
            { "int_fuelscoop_size8_class4", new ShipModule(128666675, 0, 0.96F, "Rate:1440","Fuel Scoop Class 8 Rating B", "Fuel Scoop")},
            { "int_fuelscoop_size8_class5", new ShipModule(128666683, 0, 1.12F, "Rate:1680","Fuel Scoop Class 8 Rating A", "Fuel Scoop")},
            { "int_dronecontrol_fueltransfer_size1_class1", new ShipModule(128671249, 1.3F, 0.18F, "Range:0.6km","Fuel Transfer Drone Controller Class 1 Rating E", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size1_class2", new ShipModule(128671250, 0.5F, 0.14F, "Range:0.8km","Fuel Transfer Drone Controller Class 1 Rating D", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size1_class3", new ShipModule(128671251, 1.3F, 0.23F, "Range:1km","Fuel Transfer Drone Controller Class 1 Rating C", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size1_class4", new ShipModule(128671252, 2, 0.32F, "Range:1.2km","Fuel Transfer Drone Controller Class 1 Rating B", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size1_class5", new ShipModule(128671253, 1.3F, 0.28F, "Range:1.4km","Fuel Transfer Drone Controller Class 1 Rating A", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size3_class1", new ShipModule(128671254, 5, 0.27F, "Range:0.7km","Fuel Transfer Drone Controller Class 3 Rating E", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size3_class2", new ShipModule(128671255, 2, 0.2F, "Range:0.9km","Fuel Transfer Drone Controller Class 3 Rating D", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size3_class3", new ShipModule(128671256, 5, 0.34F, "Range:1.1km","Fuel Transfer Drone Controller Class 3 Rating C", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size3_class4", new ShipModule(128671257, 8, 0.48F, "Range:1.3km","Fuel Transfer Drone Controller Class 3 Rating B", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size3_class5", new ShipModule(128671258, 5, 0.41F, "Range:1.5km","Fuel Transfer Drone Controller Class 3 Rating A", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size5_class1", new ShipModule(128671259, 20, 0.4F, "Range:0.8km","Fuel Transfer Drone Controller Class 5 Rating E", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size5_class2", new ShipModule(128671260, 8, 0.3F, "Range:1km","Fuel Transfer Drone Controller Class 5 Rating D", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size5_class3", new ShipModule(128671261, 20, 0.5F, "Range:1.3km","Fuel Transfer Drone Controller Class 5 Rating C", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size5_class4", new ShipModule(128671262, 32, 0.97F, "Range:1.6km","Fuel Transfer Drone Controller Class 5 Rating B", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size5_class5", new ShipModule(128671263, 20, 0.6F, "Range:1.8km","Fuel Transfer Drone Controller Class 5 Rating A", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size7_class1", new ShipModule(128671264, 80, 0.55F, "Range:1km","Fuel Transfer Drone Controller Class 7 Rating E", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size7_class2", new ShipModule(128671265, 32, 0.41F, "Range:1.4km","Fuel Transfer Drone Controller Class 7 Rating D", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size7_class3", new ShipModule(128671266, 80, 0.69F, "Range:1.7km","Fuel Transfer Drone Controller Class 7 Rating C", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size7_class4", new ShipModule(128671267, 128, 0.97F, "Range:2km","Fuel Transfer Drone Controller Class 7 Rating B", "Limpet Controller")},
            { "int_dronecontrol_fueltransfer_size7_class5", new ShipModule(128671268, 80, 0.83F, "Range:2.4km","Fuel Transfer Drone Controller Class 7 Rating A", "Limpet Controller")},
            { "int_guardianfsdbooster_size1", new ShipModule(128833975, 1.3F, 0.75F, null,"Guardian FSD Booster Class 1", "Guardian FSD Booster")},
            { "int_guardianfsdbooster_size2", new ShipModule(128833976, 1.3F, 0.98F, null,"Guardian FSD Booster Class 2", "Guardian FSD Booster")},
            { "int_guardianfsdbooster_size3", new ShipModule(128833977, 1.3F, 1.27F, null,"Guardian FSD Booster Class 3", "Guardian FSD Booster")},
            { "int_guardianfsdbooster_size4", new ShipModule(128833978, 1.3F, 1.65F, null,"Guardian FSD Booster Class 4", "Guardian FSD Booster")},
            { "int_guardianfsdbooster_size5", new ShipModule(128833979, 1.3F, 2.14F, null,"Guardian FSD Booster Class 5", "Guardian FSD Booster")},
            { "int_guardianhullreinforcement_size1_class2", new ShipModule(128833946, 1, 0.56F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 1 Rating D", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size1_class1", new ShipModule(128833945, 2, 0.45F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 1 Rating E", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size2_class2", new ShipModule(128833948, 2, 0.79F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 2 Rating D", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size2_class1", new ShipModule(128833947, 4, 0.68F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 2 Rating E", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size3_class2", new ShipModule(128833950, 4, 1.01F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 3 Rating D", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size3_class1", new ShipModule(128833949, 8, 0.9F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 3 Rating E", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size4_class2", new ShipModule(128833952, 8, 1.24F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 4 Rating D", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size4_class1", new ShipModule(128833951, 16, 1.13F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 4 Rating E", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size5_class2", new ShipModule(128833954, 16, 1.46F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 5 Rating D", "Guardian Hull Reinforcement")},
            { "int_guardianhullreinforcement_size5_class1", new ShipModule(128833953, 32, 1.35F, "Explosive:0%, Kinetic:0%, Thermal:2%","Guardian Hull Reinforcement Class 5 Rating E", "Guardian Hull Reinforcement")},
            { "int_guardianmodulereinforcement_size1_class2", new ShipModule(128833956, 1, 0.34F, "Protection:0.6","Guardian Module Reinforcement Class 1 Rating D", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size1_class1", new ShipModule(128833955, 2, 0.27F, "Protection:0.3","Guardian Module Reinforcement Class 1 Rating E", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size2_class2", new ShipModule(128833958, 2, 0.47F, "Protection:0.6","Guardian Module Reinforcement Class 2 Rating D", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size2_class1", new ShipModule(128833957, 4, 0.41F, "Protection:0.3","Guardian Module Reinforcement Class 2 Rating E", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size3_class2", new ShipModule(128833960, 4, 0.61F, "Protection:0.6","Guardian Module Reinforcement Class 3 Rating D", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size3_class1", new ShipModule(128833959, 8, 0.54F, "Protection:0.3","Guardian Module Reinforcement Class 3 Rating E", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size4_class2", new ShipModule(128833962, 8, 0.74F, "Protection:0.6","Guardian Module Reinforcement Class 4 Rating D", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size4_class1", new ShipModule(128833961, 16, 0.68F, "Protection:0.3","Guardian Module Reinforcement Class 4 Rating E", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size5_class2", new ShipModule(128833964, 16, 0.88F, "Protection:0.6","Guardian Module Reinforcement Class 5 Rating D", "Guardian Module Reinforcement")},
            { "int_guardianmodulereinforcement_size5_class1", new ShipModule(128833963, 32, 0.81F, "Protection:0.3","Guardian Module Reinforcement Class 5 Rating E", "Guardian Module Reinforcement")},
            { "int_guardianshieldreinforcement_size1_class1", new ShipModule(128833965, 2, 0.35F, null,"Guardian Shield Reinforcement Class 1 Rating E", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size1_class2", new ShipModule(128833966, 1, 0.46F, null,"Guardian Shield Reinforcement Class 1 Rating D", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size2_class1", new ShipModule(128833967, 4, 0.56F, null,"Guardian Shield Reinforcement Class 2 Rating E", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size2_class2", new ShipModule(128833968, 2, 0.67F, null,"Guardian Shield Reinforcement Class 2 Rating D", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size3_class2", new ShipModule(128833970, 4, 0.84F, null,"Guardian Shield Reinforcement Class 3 Rating D", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size3_class1", new ShipModule(128833969, 8, 0.74F, null,"Guardian Shield Reinforcement Class 3 Rating E", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size4_class1", new ShipModule(128833971, 16, 0.95F, null,"Guardian Shield Reinforcement Class 4 Rating E", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size4_class2", new ShipModule(128833972, 8, 1.05F, null,"Guardian Shield Reinforcement Class 4 Rating D", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size5_class2", new ShipModule(128833974, 16, 1.26F, null,"Guardian Shield Reinforcement Class 5 Rating D", "Guardian Shield Reinforcement")},
            { "int_guardianshieldreinforcement_size5_class1", new ShipModule(128833973, 32, 1.16F, null,"Guardian Shield Reinforcement Class 5 Rating E", "Guardian Shield Reinforcement")},
            { "int_dronecontrol_resourcesiphon_size1_class1", new ShipModule(128066532, 1.3F, 0.12F, "Time:42s, Range:1.5km","Hatch Breaker Drone Controller Class 1 Rating E", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size1_class2", new ShipModule(128066533, 0.5F, 0.16F, "Time:36s, Range:2km","Hatch Breaker Drone Controller Class 1 Rating D", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size1_class3", new ShipModule(128066534, 1.3F, 0.2F, "Time:30s, Range:2.5km","Hatch Breaker Drone Controller Class 1 Rating C", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size1_class4", new ShipModule(128066535, 2, 0.24F, "Time:24s, Range:3km","Hatch Breaker Drone Controller Class 1 Rating B", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size1_class5", new ShipModule(128066536, 1.3F, 0.28F, "Time:18s, Range:3.5km","Hatch Breaker Drone Controller Class 1 Rating A", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size3_class1", new ShipModule(128066537, 5, 0.18F, "Time:36s, Range:1.6km","Hatch Breaker Drone Controller Class 3 Rating E", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size3_class2", new ShipModule(128066538, 2, 0.24F, "Time:31s, Range:2.2km","Hatch Breaker Drone Controller Class 3 Rating D", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size3_class3", new ShipModule(128066539, 5, 0.3F, "Time:26s, Range:2.7km","Hatch Breaker Drone Controller Class 3 Rating C", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size3_class4", new ShipModule(128066540, 8, 0.36F, "Time:21s, Range:3.2km","Hatch Breaker Drone Controller Class 3 Rating B", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size3_class5", new ShipModule(128066541, 5, 0.42F, "Time:16s, Range:3.8km","Hatch Breaker Drone Controller Class 3 Rating A", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size5_class1", new ShipModule(128066542, 20, 0.3F, "Time:31s, Range:2km","Hatch Breaker Drone Controller Class 5 Rating E", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size5_class2", new ShipModule(128066543, 8, 0.4F, "Time:26s, Range:2.6km","Hatch Breaker Drone Controller Class 5 Rating D", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size5_class3", new ShipModule(128066544, 20, 0.5F, "Time:22s, Range:3.3km","Hatch Breaker Drone Controller Class 5 Rating C", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size5_class4", new ShipModule(128066545, 32, 0.6F, "Time:18s, Range:4km","Hatch Breaker Drone Controller Class 5 Rating B", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size5_class5", new ShipModule(128066546, 20, 0.7F, "Time:13s, Range:4.6km","Hatch Breaker Drone Controller Class 5 Rating A", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size7_class1", new ShipModule(128066547, 80, 0.42F, "Time:25s, Range:2.6km","Hatch Breaker Drone Controller Class 7 Rating E", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size7_class2", new ShipModule(128066548, 32, 0.56F, "Time:22s, Range:3.4km","Hatch Breaker Drone Controller Class 7 Rating D", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size7_class3", new ShipModule(128066549, 80, 0.7F, "Time:18s, Range:4.3km","Hatch Breaker Drone Controller Class 7 Rating C", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size7_class4", new ShipModule(128066550, 128, 0.84F, "Time:14s, Range:5.2km","Hatch Breaker Drone Controller Class 7 Rating B", "Limpet Controller")},
            { "int_dronecontrol_resourcesiphon_size7_class5", new ShipModule(128066551, 80, 0.98F, "Time:11s, Range:6km","Hatch Breaker Drone Controller Class 7 Rating A", "Limpet Controller")},
            { "int_hullreinforcement_size1_class1", new ShipModule(128668537, 2, 0, "Explosive:0.5%, Kinetic:0.5%, Thermal:0.5%","Hull Reinforcement Class 1 Rating E", "Hull Reinforcement")},
            { "int_hullreinforcement_size1_class2", new ShipModule(128668538, 1, 0, "Explosive:0.5%, Kinetic:0.5%, Thermal:0.5%","Hull Reinforcement Class 1 Rating D", "Hull Reinforcement")},
            { "int_hullreinforcement_size2_class1", new ShipModule(128668539, 4, 0, "Explosive:1%, Kinetic:1%, Thermal:1%","Hull Reinforcement Class 2 Rating E", "Hull Reinforcement")},
            { "int_hullreinforcement_size2_class2", new ShipModule(128668540, 2, 0, "Explosive:1%, Kinetic:1%, Thermal:1%","Hull Reinforcement Class 2 Rating D", "Hull Reinforcement")},
            { "int_hullreinforcement_size3_class1", new ShipModule(128668541, 8, 0, "Explosive:1.5%, Kinetic:1.5%, Thermal:1.5%","Hull Reinforcement Class 3 Rating E", "Hull Reinforcement")},
            { "int_hullreinforcement_size3_class2", new ShipModule(128668542, 4, 0, "Explosive:1.5%, Kinetic:1.5%, Thermal:1.5%","Hull Reinforcement Class 3 Rating D", "Hull Reinforcement")},
            { "int_hullreinforcement_size4_class1", new ShipModule(128668543, 16, 0, "Explosive:2%, Kinetic:2%, Thermal:2%","Hull Reinforcement Class 4 Rating E", "Hull Reinforcement")},
            { "int_hullreinforcement_size4_class2", new ShipModule(128668544, 8, 0, "Explosive:2%, Kinetic:2%, Thermal:2%","Hull Reinforcement Class 4 Rating D", "Hull Reinforcement")},
            { "int_hullreinforcement_size5_class1", new ShipModule(128668545, 32, 0, "Explosive:2.5%, Kinetic:2.5%, Thermal:2.5%","Hull Reinforcement Class 5 Rating E", "Hull Reinforcement")},
            { "int_hullreinforcement_size5_class2", new ShipModule(128668546, 16, 0, "Explosive:2.5%, Kinetic:2.5%, Thermal:2.5%","Hull Reinforcement Class 5 Rating D", "Hull Reinforcement")},
            { "int_fueltank_size1_class3", new ShipModule(128064346, 0, 0, "Size:2t","Fuel Tank Class 1 Rating C", "Fuel Tank")},
            { "int_fueltank_size2_class3", new ShipModule(128064347, 0, 0, "Size:4t","Fuel Tank Class 2 Rating C", "Fuel Tank")},
            { "int_fueltank_size3_class3", new ShipModule(128064348, 0, 0, "Size:8t","Fuel Tank Class 3 Rating C", "Fuel Tank")},
            { "int_fueltank_size4_class3", new ShipModule(128064349, 0, 0, "Size:16t","Fuel Tank Class 4 Rating C", "Fuel Tank")},
            { "int_fueltank_size5_class3", new ShipModule(128064350, 0, 0, "Size:32t","Fuel Tank Class 5 Rating C", "Fuel Tank")},
            { "int_fueltank_size6_class3", new ShipModule(128064351, 0, 0, "Size:64t","Fuel Tank Class 6 Rating C", "Fuel Tank")},
            { "int_fueltank_size7_class3", new ShipModule(128064352, 0, 0, "Size:128t","Fuel Tank Class 7 Rating C", "Fuel Tank")},
            { "int_fueltank_size8_class3", new ShipModule(128064353, 0, 0, "Size:256t","Fuel Tank Class 8 Rating C", "Fuel Tank")},
            { "int_passengercabin_size5_class4", new ShipModule(128727925, 20, 0, "Passengers:4","Luxury Passenger Cabin Class 5 Rating B", "Passenger Cabin")},
            { "int_passengercabin_size6_class4", new ShipModule(128727929, 40, 0, "Passengers:8","Luxury Passenger Cabin Class 6 Rating B", "Passenger Cabin")},
            { "int_metaalloyhullreinforcement_size1_class1", new ShipModule(128793117, 2, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 1 Rating E", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size1_class2", new ShipModule(128793118, 2, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 1 Rating D", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size2_class1", new ShipModule(128793119, 4, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 2 Rating E", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size2_class2", new ShipModule(128793120, 2, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 2 Rating D", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size3_class1", new ShipModule(128793121, 8, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 3 Rating E", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size3_class2", new ShipModule(128793122, 4, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 3 Rating D", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size4_class1", new ShipModule(128793123, 16, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 4 Rating E", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size4_class2", new ShipModule(128793124, 8, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 4 Rating D", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size5_class1", new ShipModule(128793125, 32, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 5 Rating E", "Meta Alloy Hull Reinforcement")},
            { "int_metaalloyhullreinforcement_size5_class2", new ShipModule(128793126, 16, 0, "Explosive:0%, Kinetic:0%, Thermal:0%","Meta Alloy Hull Reinforcement Class 5 Rating D", "Meta Alloy Hull Reinforcement")},
            { "int_modulereinforcement_size1_class1", new ShipModule(128737270, 2, 0, "Protection:0.3","Module Reinforcement Class 1 Rating E", "Module Reinforcement")},
            { "int_modulereinforcement_size1_class2", new ShipModule(128737271, 1, 0, "Protection:0.6","Module Reinforcement Class 1 Rating D", "Module Reinforcement")},
            { "int_modulereinforcement_size2_class1", new ShipModule(128737272, 4, 0, "Protection:0.3","Module Reinforcement Class 2 Rating E", "Module Reinforcement")},
            { "int_modulereinforcement_size2_class2", new ShipModule(128737273, 2, 0, "Protection:0.6","Module Reinforcement Class 2 Rating D", "Module Reinforcement")},
            { "int_modulereinforcement_size3_class1", new ShipModule(128737274, 8, 0, "Protection:0.3","Module Reinforcement Class 3 Rating E", "Module Reinforcement")},
            { "int_modulereinforcement_size3_class2", new ShipModule(128737275, 4, 0, "Protection:0.6","Module Reinforcement Class 3 Rating D", "Module Reinforcement")},
            { "int_modulereinforcement_size4_class1", new ShipModule(128737276, 16, 0, "Protection:0.3","Module Reinforcement Class 4 Rating E", "Module Reinforcement")},
            { "int_modulereinforcement_size4_class2", new ShipModule(128737277, 8, 0, "Protection:0.6","Module Reinforcement Class 4 Rating D", "Module Reinforcement")},
            { "int_modulereinforcement_size5_class1", new ShipModule(128737278, 32, 0, "Protection:0.3","Module Reinforcement Class 5 Rating E", "Module Reinforcement")},
            { "int_modulereinforcement_size5_class2", new ShipModule(128737279, 16, 0, "Protection:0.6","Module Reinforcement Class 5 Rating D", "Module Reinforcement")},
            { "int_buggybay_size2_class1", new ShipModule(128672288, 12, 0.25F, null,"Planetary Vehicle Hangar Class 2 Rating H", "Planetary Vehicle Hangar")},
            { "int_buggybay_size2_class2", new ShipModule(128672289, 6, 0.75F, null,"Planetary Vehicle Hangar Class 2 Rating G", "Planetary Vehicle Hangar")},
            { "int_buggybay_size4_class1", new ShipModule(128672290, 20, 0.4F, null,"Planetary Vehicle Hangar Class 4 Rating H", "Planetary Vehicle Hangar")},
            { "int_buggybay_size4_class2", new ShipModule(128672291, 10, 1.2F, null,"Planetary Vehicle Hangar Class 4 Rating G", "Planetary Vehicle Hangar")},
            { "int_buggybay_size6_class1", new ShipModule(128672292, 34, 0.6F, null,"Planetary Vehicle Hangar Class 6 Rating H", "Planetary Vehicle Hangar")},
            { "int_buggybay_size6_class2", new ShipModule(128672293, 17, 1.8F, null,"Planetary Vehicle Hangar Class 6 Rating G", "Planetary Vehicle Hangar")},
            { "int_shieldgenerator_size1_class5_strong", new ShipModule(128671323, 2.6F, 2.52F, "OptMass:25t, MaxMass:63t, MinMass:13t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 1 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size2_class5_strong", new ShipModule(128671324, 5, 3.15F, "OptMass:55t, MaxMass:138t, MinMass:23t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size3_class5_strong", new ShipModule(128671325, 10, 3.78F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size4_class5_strong", new ShipModule(128671326, 20, 4.62F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size5_class5_strong", new ShipModule(128671327, 40, 5.46F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size6_class5_strong", new ShipModule(128671328, 80, 6.51F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size7_class5_strong", new ShipModule(128671329, 160, 7.35F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating A Strong", "Shield Generator")},
            { "int_shieldgenerator_size8_class5_strong", new ShipModule(128671330, 320, 8.4F, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating A Strong", "Shield Generator")},
            { "int_dronecontrol_prospector_size1_class1", new ShipModule(128671269, 1.3F, 0.18F, "Range:3km","Prospector Drone Controller Class 1 Rating E", "Limpet Controller")},
            { "int_dronecontrol_prospector_size1_class2", new ShipModule(128671270, 0.5F, 0.14F, "Range:4km","Prospector Drone Controller Class 1 Rating D", "Limpet Controller")},
            { "int_dronecontrol_prospector_size1_class3", new ShipModule(128671271, 1.3F, 0.23F, "Range:5km","Prospector Drone Controller Class 1 Rating C", "Limpet Controller")},
            { "int_dronecontrol_prospector_size1_class4", new ShipModule(128671272, 2, 0.32F, "Range:6km","Prospector Drone Controller Class 1 Rating B", "Limpet Controller")},
            { "int_dronecontrol_prospector_size1_class5", new ShipModule(128671273, 1.3F, 0.28F, "Range:7km","Prospector Drone Controller Class 1 Rating A", "Limpet Controller")},
            { "int_dronecontrol_prospector_size3_class1", new ShipModule(128671274, 5, 0.27F, "Range:3.3km","Prospector Drone Controller Class 3 Rating E", "Limpet Controller")},
            { "int_dronecontrol_prospector_size3_class2", new ShipModule(128671275, 2, 0.2F, "Range:4.4km","Prospector Drone Controller Class 3 Rating D", "Limpet Controller")},
            { "int_dronecontrol_prospector_size3_class3", new ShipModule(128671276, 5, 0.34F, "Range:5.5km","Prospector Drone Controller Class 3 Rating C", "Limpet Controller")},
            { "int_dronecontrol_prospector_size3_class4", new ShipModule(128671277, 8, 0.48F, "Range:6.6km","Prospector Drone Controller Class 3 Rating B", "Limpet Controller")},
            { "int_dronecontrol_prospector_size3_class5", new ShipModule(128671278, 5, 0.41F, "Range:7.7km","Prospector Drone Controller Class 3 Rating A", "Limpet Controller")},
            { "int_dronecontrol_prospector_size5_class1", new ShipModule(128671279, 20, 0.4F, "Range:3.9km","Prospector Drone Controller Class 5 Rating E", "Limpet Controller")},
            { "int_dronecontrol_prospector_size5_class2", new ShipModule(128671280, 8, 0.3F, "Range:5.2km","Prospector Drone Controller Class 5 Rating D", "Limpet Controller")},
            { "int_dronecontrol_prospector_size5_class3", new ShipModule(128671281, 20, 0.5F, "Range:6.5km","Prospector Drone Controller Class 5 Rating C", "Limpet Controller")},
            { "int_dronecontrol_prospector_size5_class4", new ShipModule(128671282, 32, 0.97F, "Range:7.8km","Prospector Drone Controller Class 5 Rating B", "Limpet Controller")},
            { "int_dronecontrol_prospector_size5_class5", new ShipModule(128671283, 20, 0.6F, "Range:9.1km","Prospector Drone Controller Class 5 Rating A", "Limpet Controller")},
            { "int_dronecontrol_prospector_size7_class1", new ShipModule(128671284, 80, 0.55F, "Range:5.1km","Prospector Drone Controller Class 7 Rating E", "Limpet Controller")},
            { "int_dronecontrol_prospector_size7_class2", new ShipModule(128671285, 32, 0.41F, "Range:6.8km","Prospector Drone Controller Class 7 Rating D", "Limpet Controller")},
            { "int_dronecontrol_prospector_size7_class3", new ShipModule(128671286, 80, 0.69F, "Range:8.5km","Prospector Drone Controller Class 7 Rating C", "Limpet Controller")},
            { "int_dronecontrol_prospector_size7_class4", new ShipModule(128671287, 128, 0.97F, "Range:10.2km","Prospector Drone Controller Class 7 Rating B", "Limpet Controller")},
            { "int_dronecontrol_prospector_size7_class5", new ShipModule(128671288, 80, 0.83F, "Range:11.9km","Prospector Drone Controller Class 7 Rating A", "Limpet Controller")},
            { "int_dronecontrol_recon_size1_class1", new ShipModule(128837858, 1.3F, 0.18F, "Range:1.2km","Recon Drone Controller Class 1 Rating E", "Drone Control Recon")},
            { "int_dronecontrol_recon_size3_class1", new ShipModule(128841592, 2, 0.2F, "Range:1.4km","Recon Drone Controller Class 3 Rating E", "Drone Control Recon")},
            { "int_dronecontrol_recon_size5_class1", new ShipModule(128841593, 20, 0.5F, "Range:1.7km","Recon Drone Controller Class 5 Rating E", "Drone Control Recon")},
            { "int_dronecontrol_recon_size7_class1", new ShipModule(128841594, 128, 0.97F, "Range:2km","Recon Drone Controller Class 7 Rating E", "Drone Control Recon")},
            { "int_refinery_size1_class1", new ShipModule(128666684, 0, 0.14F, null,"Refinery Class 1 Rating E", "Refinery")},
            { "int_refinery_size1_class2", new ShipModule(128666688, 0, 0.18F, null,"Refinery Class 1 Rating D", "Refinery")},
            { "int_refinery_size1_class3", new ShipModule(128666692, 0, 0.23F, null,"Refinery Class 1 Rating C", "Refinery")},
            { "int_refinery_size1_class4", new ShipModule(128666696, 0, 0.28F, null,"Refinery Class 1 Rating B", "Refinery")},
            { "int_refinery_size1_class5", new ShipModule(128666700, 0, 0.32F, null,"Refinery Class 1 Rating A", "Refinery")},
            { "int_refinery_size2_class1", new ShipModule(128666685, 0, 0.17F, null,"Refinery Class 2 Rating E", "Refinery")},
            { "int_refinery_size2_class2", new ShipModule(128666689, 0, 0.22F, null,"Refinery Class 2 Rating D", "Refinery")},
            { "int_refinery_size2_class3", new ShipModule(128666693, 0, 0.28F, null,"Refinery Class 2 Rating C", "Refinery")},
            { "int_refinery_size2_class4", new ShipModule(128666697, 0, 0.34F, null,"Refinery Class 2 Rating B", "Refinery")},
            { "int_refinery_size2_class5", new ShipModule(128666701, 0, 0.39F, null,"Refinery Class 2 Rating A", "Refinery")},
            { "int_refinery_size3_class1", new ShipModule(128666686, 0, 0.2F, null,"Refinery Class 3 Rating E", "Refinery")},
            { "int_refinery_size3_class2", new ShipModule(128666690, 0, 0.27F, null,"Refinery Class 3 Rating D", "Refinery")},
            { "int_refinery_size3_class3", new ShipModule(128666694, 0, 0.34F, null,"Refinery Class 3 Rating C", "Refinery")},
            { "int_refinery_size3_class4", new ShipModule(128666698, 0, 0.41F, null,"Refinery Class 3 Rating B", "Refinery")},
            { "int_refinery_size3_class5", new ShipModule(128666702, 0, 0.48F, null,"Refinery Class 3 Rating A", "Refinery")},
            { "int_refinery_size4_class1", new ShipModule(128666687, 0, 0.25F, null,"Refinery Class 4 Rating E", "Refinery")},
            { "int_refinery_size4_class2", new ShipModule(128666691, 0, 0.33F, null,"Refinery Class 4 Rating D", "Refinery")},
            { "int_refinery_size4_class3", new ShipModule(128666695, 0, 0.41F, null,"Refinery Class 4 Rating C", "Refinery")},
            { "int_refinery_size4_class4", new ShipModule(128666699, 0, 0.49F, null,"Refinery Class 4 Rating B", "Refinery")},
            { "int_refinery_size4_class5", new ShipModule(128666703, 0, 0.57F, null,"Refinery Class 4 Rating A", "Refinery")},
            { "int_dronecontrol_repair_size1_class1", new ShipModule(128777327, 1.3F, 0.18F, "Range:0.6km","Repair Drone Controller Class 1 Rating E", "Limpet Controller")},
            { "int_dronecontrol_repair_size1_class2", new ShipModule(128777328, 0.5F, 0.14F, "Range:0.8km","Repair Drone Controller Class 1 Rating D", "Limpet Controller")},
            { "int_dronecontrol_repair_size1_class3", new ShipModule(128777329, 1.3F, 0.23F, "Range:1km","Repair Drone Controller Class 1 Rating C", "Limpet Controller")},
            { "int_dronecontrol_repair_size1_class4", new ShipModule(128777330, 2, 0.32F, "Range:1.2km","Repair Drone Controller Class 1 Rating B", "Limpet Controller")},
            { "int_dronecontrol_repair_size1_class5", new ShipModule(128777331, 1.3F, 0.28F, "Range:1.4km","Repair Drone Controller Class 1 Rating A", "Limpet Controller")},
            { "int_dronecontrol_repair_size3_class1", new ShipModule(128777332, 5, 0.27F, "Range:0.7km","Repair Drone Controller Class 3 Rating E", "Limpet Controller")},
            { "int_dronecontrol_repair_size3_class2", new ShipModule(128777333, 2, 0.2F, "Range:0.9km","Repair Drone Controller Class 3 Rating D", "Limpet Controller")},
            { "int_dronecontrol_repair_size3_class3", new ShipModule(128777334, 5, 0.34F, "Range:1.1km","Repair Drone Controller Class 3 Rating C", "Limpet Controller")},
            { "int_dronecontrol_repair_size3_class4", new ShipModule(128777335, 8, 0.48F, "Range:1.3km","Repair Drone Controller Class 3 Rating B", "Limpet Controller")},
            { "int_dronecontrol_repair_size3_class5", new ShipModule(128777336, 5, 0.41F, "Range:1.5km","Repair Drone Controller Class 3 Rating A", "Limpet Controller")},
            { "int_dronecontrol_repair_size5_class1", new ShipModule(128777337, 20, 0.4F, "Range:0.8km","Repair Drone Controller Class 5 Rating E", "Limpet Controller")},
            { "int_dronecontrol_repair_size5_class2", new ShipModule(128777338, 8, 0.3F, "Range:1km","Repair Drone Controller Class 5 Rating D", "Limpet Controller")},
            { "int_dronecontrol_repair_size5_class3", new ShipModule(128777339, 20, 0.5F, "Range:1.3km","Repair Drone Controller Class 5 Rating C", "Limpet Controller")},
            { "int_dronecontrol_repair_size5_class4", new ShipModule(128777340, 32, 0.97F, "Range:1.6km","Repair Drone Controller Class 5 Rating B", "Limpet Controller")},
            { "int_dronecontrol_repair_size5_class5", new ShipModule(128777341, 20, 0.6F, "Range:1.8km","Repair Drone Controller Class 5 Rating A", "Limpet Controller")},
            { "int_dronecontrol_repair_size7_class1", new ShipModule(128777342, 80, 0.55F, "Range:1km","Repair Drone Controller Class 7 Rating E", "Limpet Controller")},
            { "int_dronecontrol_repair_size7_class2", new ShipModule(128777343, 32, 0.41F, "Range:1.4km","Repair Drone Controller Class 7 Rating D", "Limpet Controller")},
            { "int_dronecontrol_repair_size7_class3", new ShipModule(128777344, 80, 0.69F, "Range:1.7km","Repair Drone Controller Class 7 Rating C", "Limpet Controller")},
            { "int_dronecontrol_repair_size7_class4", new ShipModule(128777345, 128, 0.97F, "Range:2km","Repair Drone Controller Class 7 Rating B", "Limpet Controller")},
            { "int_dronecontrol_repair_size7_class5", new ShipModule(128777346, 80, 0.83F, "Range:2.4km","Repair Drone Controller Class 7 Rating A", "Limpet Controller")},
            { "int_dronecontrol_unkvesselresearch", new ShipModule(128793116, 1.3F, 0.4F, "Time:300s, Range:2km","Drone Controller Vessel Research", "Research Limpet Controller")},
            { "int_shieldcellbank_size1_class1", new ShipModule(128064298, 1.3F, 0.41F, "Ammo:3/1, ThermL:170","Shield Cell Bank Class 1 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size1_class2", new ShipModule(128064299, 0.5F, 0.55F, "Ammo:0/1, ThermL:170","Shield Cell Bank Class 1 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size1_class3", new ShipModule(128064300, 1.3F, 0.69F, "Ammo:2/1, ThermL:170","Shield Cell Bank Class 1 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size1_class4", new ShipModule(128064301, 2, 0.83F, "Ammo:3/1, ThermL:170","Shield Cell Bank Class 1 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size1_class5", new ShipModule(128064302, 1.3F, 0.97F, "Ammo:2/1, ThermL:170","Shield Cell Bank Class 1 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size2_class1", new ShipModule(128064303, 2.5F, 0.5F, "Ammo:4/1, ThermL:240","Shield Cell Bank Class 2 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size2_class2", new ShipModule(128064304, 1, 0.67F, "Ammo:2/1, ThermL:240","Shield Cell Bank Class 2 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size2_class3", new ShipModule(128064305, 2.5F, 0.84F, "Ammo:3/1, ThermL:240","Shield Cell Bank Class 2 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size2_class4", new ShipModule(128064306, 4, 1.01F, "Ammo:4/1, ThermL:240","Shield Cell Bank Class 2 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size2_class5", new ShipModule(128064307, 2.5F, 1.18F, "Ammo:3/1, ThermL:240","Shield Cell Bank Class 2 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size3_class1", new ShipModule(128064308, 5, 0.61F, "Ammo:4/1, ThermL:340","Shield Cell Bank Class 3 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size3_class2", new ShipModule(128064309, 2, 0.82F, "Ammo:2/1, ThermL:340","Shield Cell Bank Class 3 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size3_class3", new ShipModule(128064310, 5, 1.02F, "Ammo:3/1, ThermL:340","Shield Cell Bank Class 3 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size3_class4", new ShipModule(128064311, 8, 1.22F, "Ammo:4/1, ThermL:340","Shield Cell Bank Class 3 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size3_class5", new ShipModule(128064312, 5, 1.43F, "Ammo:3/1, ThermL:340","Shield Cell Bank Class 3 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size4_class1", new ShipModule(128064313, 10, 0.74F, "Ammo:4/1, ThermL:410","Shield Cell Bank Class 4 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size4_class2", new ShipModule(128064314, 4, 0.98F, "Ammo:2/1, ThermL:410","Shield Cell Bank Class 4 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size4_class3", new ShipModule(128064315, 10, 1.23F, "Ammo:3/1, ThermL:410","Shield Cell Bank Class 4 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size4_class4", new ShipModule(128064316, 16, 1.48F, "Ammo:4/1, ThermL:410","Shield Cell Bank Class 4 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size4_class5", new ShipModule(128064317, 10, 1.72F, "Ammo:3/1, ThermL:410","Shield Cell Bank Class 4 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size5_class1", new ShipModule(128064318, 20, 0.9F, "Ammo:4/1, ThermL:540","Shield Cell Bank Class 5 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size5_class2", new ShipModule(128064319, 8, 1.2F, "Ammo:2/1, ThermL:540","Shield Cell Bank Class 5 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size5_class3", new ShipModule(128064320, 20, 1.5F, "Ammo:3/1, ThermL:540","Shield Cell Bank Class 5 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size5_class4", new ShipModule(128064321, 32, 1.8F, "Ammo:4/1, ThermL:540","Shield Cell Bank Class 5 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size5_class5", new ShipModule(128064322, 20, 2.1F, "Ammo:3/1, ThermL:540","Shield Cell Bank Class 5 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size6_class1", new ShipModule(128064323, 40, 1.06F, "Ammo:5/1, ThermL:640","Shield Cell Bank Class 6 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size6_class2", new ShipModule(128064324, 16, 1.42F, "Ammo:3/1, ThermL:640","Shield Cell Bank Class 6 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size6_class3", new ShipModule(128064325, 40, 1.77F, "Ammo:4/1, ThermL:640","Shield Cell Bank Class 6 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size6_class4", new ShipModule(128064326, 64, 2.12F, "Ammo:5/1, ThermL:640","Shield Cell Bank Class 6 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size6_class5", new ShipModule(128064327, 40, 2.48F, "Ammo:4/1, ThermL:640","Shield Cell Bank Class 6 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size7_class1", new ShipModule(128064328, 80, 1.24F, "Ammo:5/1, ThermL:720","Shield Cell Bank Class 7 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size7_class2", new ShipModule(128064329, 32, 1.66F, "Ammo:3/1, ThermL:720","Shield Cell Bank Class 7 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size7_class3", new ShipModule(128064330, 80, 2.07F, "Ammo:4/1, ThermL:720","Shield Cell Bank Class 7 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size7_class4", new ShipModule(128064331, 128, 2.48F, "Ammo:5/1, ThermL:720","Shield Cell Bank Class 7 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size7_class5", new ShipModule(128064332, 80, 2.9F, "Ammo:4/1, ThermL:720","Shield Cell Bank Class 7 Rating A", "Shield Cell Bank")},
            { "int_shieldcellbank_size8_class1", new ShipModule(128064333, 160, 1.44F, "Ammo:5/1, ThermL:800","Shield Cell Bank Class 8 Rating E", "Shield Cell Bank")},
            { "int_shieldcellbank_size8_class2", new ShipModule(128064334, 64, 1.92F, "Ammo:3/1, ThermL:800","Shield Cell Bank Class 8 Rating D", "Shield Cell Bank")},
            { "int_shieldcellbank_size8_class3", new ShipModule(128064335, 160, 2.4F, "Ammo:4/1, ThermL:800","Shield Cell Bank Class 8 Rating C", "Shield Cell Bank")},
            { "int_shieldcellbank_size8_class4", new ShipModule(128064336, 256, 2.88F, "Ammo:5/1, ThermL:800","Shield Cell Bank Class 8 Rating B", "Shield Cell Bank")},
            { "int_shieldcellbank_size8_class5", new ShipModule(128064337, 160, 3.36F, "Ammo:4/1, ThermL:800","Shield Cell Bank Class 8 Rating A", "Shield Cell Bank")},
            { "int_shieldgenerator_size1_class5", new ShipModule(128064262, 1.3F, 1.68F, "OptMass:25t, MaxMass:63t, MinMass:13t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 1 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size2_class1", new ShipModule(128064263, 2.5F, 0.9F, "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size2_class2", new ShipModule(128064264, 1, 1.2F, "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size2_class3", new ShipModule(128064265, 2.5F, 1.5F, "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size2_class4", new ShipModule(128064266, 4, 1.8F, "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size2_class5", new ShipModule(128064267, 2.5F, 2.1F, "OptMass:55t, MaxMass:138t, MinMass:28t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 2 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size3_class1", new ShipModule(128064268, 5, 1.08F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size3_class2", new ShipModule(128064269, 2, 1.44F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size3_class3", new ShipModule(128064270, 5, 1.8F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size3_class4", new ShipModule(128064271, 8, 2.16F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size3_class5", new ShipModule(128064272, 5, 2.52F, "OptMass:165t, MaxMass:413t, MinMass:83t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 3 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size4_class1", new ShipModule(128064273, 10, 1.32F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size4_class2", new ShipModule(128064274, 4, 1.76F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size4_class3", new ShipModule(128064275, 10, 2.2F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size4_class4", new ShipModule(128064276, 16, 2.64F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size4_class5", new ShipModule(128064277, 10, 3.08F, "OptMass:285t, MaxMass:713t, MinMass:143t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 4 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size5_class1", new ShipModule(128064278, 20, 1.56F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size5_class2", new ShipModule(128064279, 8, 2.08F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size5_class3", new ShipModule(128064280, 20, 2.6F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size5_class4", new ShipModule(128064281, 32, 3.12F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size5_class5", new ShipModule(128064282, 20, 3.64F, "OptMass:405t, MaxMass:1013t, MinMass:203t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 5 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size6_class1", new ShipModule(128064283, 40, 1.86F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size6_class2", new ShipModule(128064284, 16, 2.48F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size6_class3", new ShipModule(128064285, 40, 3.1F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size6_class4", new ShipModule(128064286, 64, 3.72F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size6_class5", new ShipModule(128064287, 40, 4.34F, "OptMass:540t, MaxMass:1350t, MinMass:270t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 6 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size7_class1", new ShipModule(128064288, 80, 2.1F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size7_class2", new ShipModule(128064289, 32, 2.8F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size7_class3", new ShipModule(128064290, 80, 3.5F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size7_class4", new ShipModule(128064291, 128, 4.2F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size7_class5", new ShipModule(128064292, 80, 4.9F, "OptMass:1060t, MaxMass:2650t, MinMass:530t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 7 Rating A", "Shield Generator")},
            { "int_shieldgenerator_size8_class1", new ShipModule(128064293, 160, 2.4F, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating E", "Shield Generator")},
            { "int_shieldgenerator_size8_class2", new ShipModule(128064294, 64, 3.2F, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating D", "Shield Generator")},
            { "int_shieldgenerator_size8_class3", new ShipModule(128064295, 160, 4, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating C", "Shield Generator")},
            { "int_shieldgenerator_size8_class4", new ShipModule(128064296, 256, 4.8F, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating B", "Shield Generator")},
            { "int_shieldgenerator_size8_class5", new ShipModule(128064297, 160, 5.6F, "OptMass:1800t, MaxMass:4500t, MinMass:900t, Explosive:50%, Kinetic:40%, Thermal:-20%","Shield Generator Class 8 Rating A", "Shield Generator")},
            { "int_supercruiseassist", new ShipModule(128932273, 0, 0.3F, null,"Supercruise Assist", "Supercruise Assist")},
            { "int_detailedsurfacescanner_tiny", new ShipModule(128666634, 1.3, 0, null,"Detailed Surface Scanner", "Detailed Surface Scanner")},

            ///////////////////////////////////// FROM STANDARD FOLDER SCAN

            { "int_hyperdrive_size8_class1", new ShipModule(128064133, 160, 0.56F, "OptMass:0t","Hyperdrive Class 8 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size8_class2", new ShipModule(128064134, 64, 0.63F, "OptMass:0t","Hyperdrive Class 8 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size8_class3", new ShipModule(128064135, 160, 0.7F, "OptMass:0t","Hyperdrive Class 8 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size8_class4", new ShipModule(128064136, 256, 0.88F, "OptMass:0t","Hyperdrive Class 8 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size8_class5", new ShipModule(128064137, 160, 1.05F, "OptMass:0t","Hyperdrive Class 8 Rating A", "Hyperdrive")},
            { "int_hyperdrive_size7_class1", new ShipModule(128064128, 80, 0.48F, "OptMass:1440t","Hyperdrive Class 7 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size7_class2", new ShipModule(128064129, 32, 0.54F, "OptMass:1620t","Hyperdrive Class 7 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size7_class3", new ShipModule(128064130, 80, 0.6F, "OptMass:1800t","Hyperdrive Class 7 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size7_class4", new ShipModule(128064131, 128, 0.75F, "OptMass:2250t","Hyperdrive Class 7 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size7_class5", new ShipModule(128064132, 80, 0.9F, "OptMass:2700t","Hyperdrive Class 7 Rating A", "Hyperdrive")},
            { "int_hyperdrive_size6_class1", new ShipModule(128064123, 40, 0.4F, "OptMass:960t","Hyperdrive Class 6 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size6_class2", new ShipModule(128064124, 16, 0.45F, "OptMass:1080t","Hyperdrive Class 6 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size6_class3", new ShipModule(128064125, 40, 0.5F, "OptMass:1200t","Hyperdrive Class 6 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size6_class4", new ShipModule(128064126, 64, 0.63F, "OptMass:1500t","Hyperdrive Class 6 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size6_class5", new ShipModule(128064127, 40, 0.75F, "OptMass:1800t","Hyperdrive Class 6 Rating A", "Hyperdrive")},
            { "int_hyperdrive_size5_class1", new ShipModule(128064118, 20, 0.32F, "OptMass:560t","Hyperdrive Class 5 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size5_class2", new ShipModule(128064119, 8, 0.36F, "OptMass:630t","Hyperdrive Class 5 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size5_class3", new ShipModule(128064120, 20, 0.4F, "OptMass:700t","Hyperdrive Class 5 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size5_class4", new ShipModule(128064121, 32, 0.5F, "OptMass:875t","Hyperdrive Class 5 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size5_class5", new ShipModule(128064122, 20, 0.6F, "OptMass:1050t","Hyperdrive Class 5 Rating A", "Hyperdrive")},
            { "int_hyperdrive_size4_class1", new ShipModule(128064113, 10, 0.24F, "OptMass:280t","Hyperdrive Class 4 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size4_class2", new ShipModule(128064114, 4, 0.27F, "OptMass:315t","Hyperdrive Class 4 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size4_class3", new ShipModule(128064115, 10, 0.3F, "OptMass:350t","Hyperdrive Class 4 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size4_class4", new ShipModule(128064116, 16, 0.38F, "OptMass:438t","Hyperdrive Class 4 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size4_class5", new ShipModule(128064117, 10, 0.45F, "OptMass:525t","Hyperdrive Class 4 Rating A", "Hyperdrive")},
            { "int_hyperdrive_size3_class1", new ShipModule(128064108, 5, 0.24F, "OptMass:80t","Hyperdrive Class 3 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size3_class2", new ShipModule(128064109, 2, 0.27F, "OptMass:90t","Hyperdrive Class 3 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size3_class3", new ShipModule(128064110, 5, 0.3F, "OptMass:100t","Hyperdrive Class 3 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size3_class4", new ShipModule(128064111, 8, 0.38F, "OptMass:125t","Hyperdrive Class 3 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size3_class5", new ShipModule(128064112, 5, 0.45F, "OptMass:150t","Hyperdrive Class 3 Rating A", "Hyperdrive")},
            { "int_hyperdrive_size2_class1", new ShipModule(128064103, 2.5F, 0.16F, "OptMass:48t","Hyperdrive Class 2 Rating E", "Hyperdrive")},
            { "int_hyperdrive_size2_class2", new ShipModule(128064104, 1, 0.18F, "OptMass:54t","Hyperdrive Class 2 Rating D", "Hyperdrive")},
            { "int_hyperdrive_size2_class3", new ShipModule(128064105, 2.5F, 0.2F, "OptMass:60t","Hyperdrive Class 2 Rating C", "Hyperdrive")},
            { "int_hyperdrive_size2_class4", new ShipModule(128064106, 4, 0.25F, "OptMass:75t","Hyperdrive Class 2 Rating B", "Hyperdrive")},
            { "int_hyperdrive_size2_class5", new ShipModule(128064107, 2.5F, 0.3F, "OptMass:90t","Hyperdrive Class 2 Rating A", "Hyperdrive")},
            { "int_lifesupport_size8_class1", new ShipModule(128064173, 160, 0.8F, "Time:300s","Life Support Class 8 Rating E", "Life Support")},
            { "int_lifesupport_size8_class2", new ShipModule(128064174, 64, 0.9F, "Time:450s","Life Support Class 8 Rating D", "Life Support")},
            { "int_lifesupport_size8_class3", new ShipModule(128064175, 160, 1, "Time:600s","Life Support Class 8 Rating C", "Life Support")},
            { "int_lifesupport_size8_class4", new ShipModule(128064176, 256, 1.1F, "Time:900s","Life Support Class 8 Rating B", "Life Support")},
            { "int_lifesupport_size8_class5", new ShipModule(128064177, 160, 1.2F, "Time:1500s","Life Support Class 8 Rating A", "Life Support")},
            { "int_lifesupport_size7_class1", new ShipModule(128064168, 80, 0.72F, "Time:300s","Life Support Class 7 Rating E", "Life Support")},
            { "int_lifesupport_size7_class2", new ShipModule(128064169, 32, 0.81F, "Time:450s","Life Support Class 7 Rating D", "Life Support")},
            { "int_lifesupport_size7_class3", new ShipModule(128064170, 80, 0.9F, "Time:600s","Life Support Class 7 Rating C", "Life Support")},
            { "int_lifesupport_size7_class4", new ShipModule(128064171, 128, 0.99F, "Time:900s","Life Support Class 7 Rating B", "Life Support")},
            { "int_lifesupport_size7_class5", new ShipModule(128064172, 80, 1.08F, "Time:1500s","Life Support Class 7 Rating A", "Life Support")},
            { "int_lifesupport_size6_class1", new ShipModule(128064163, 40, 0.64F, "Time:300s","Life Support Class 6 Rating E", "Life Support")},
            { "int_lifesupport_size6_class2", new ShipModule(128064164, 16, 0.72F, "Time:450s","Life Support Class 6 Rating D", "Life Support")},
            { "int_lifesupport_size6_class3", new ShipModule(128064165, 40, 0.8F, "Time:600s","Life Support Class 6 Rating C", "Life Support")},
            { "int_lifesupport_size6_class4", new ShipModule(128064166, 64, 0.88F, "Time:900s","Life Support Class 6 Rating B", "Life Support")},
            { "int_lifesupport_size6_class5", new ShipModule(128064167, 40, 0.96F, "Time:1500s","Life Support Class 6 Rating A", "Life Support")},
            { "int_lifesupport_size5_class1", new ShipModule(128064158, 20, 0.57F, "Time:300s","Life Support Class 5 Rating E", "Life Support")},
            { "int_lifesupport_size5_class2", new ShipModule(128064159, 8, 0.64F, "Time:450s","Life Support Class 5 Rating D", "Life Support")},
            { "int_lifesupport_size5_class3", new ShipModule(128064160, 20, 0.71F, "Time:600s","Life Support Class 5 Rating C", "Life Support")},
            { "int_lifesupport_size5_class4", new ShipModule(128064161, 32, 0.78F, "Time:900s","Life Support Class 5 Rating B", "Life Support")},
            { "int_lifesupport_size5_class5", new ShipModule(128064162, 20, 0.85F, "Time:1500s","Life Support Class 5 Rating A", "Life Support")},
            { "int_lifesupport_size4_class1", new ShipModule(128064153, 10, 0.5F, "Time:300s","Life Support Class 4 Rating E", "Life Support")},
            { "int_lifesupport_size4_class2", new ShipModule(128064154, 4, 0.56F, "Time:450s","Life Support Class 4 Rating D", "Life Support")},
            { "int_lifesupport_size4_class3", new ShipModule(128064155, 10, 0.62F, "Time:600s","Life Support Class 4 Rating C", "Life Support")},
            { "int_lifesupport_size4_class4", new ShipModule(128064156, 16, 0.68F, "Time:900s","Life Support Class 4 Rating B", "Life Support")},
            { "int_lifesupport_size4_class5", new ShipModule(128064157, 10, 0.74F, "Time:1500s","Life Support Class 4 Rating A", "Life Support")},
            { "int_lifesupport_size3_class1", new ShipModule(128064148, 5, 0.42F, "Time:300s","Life Support Class 3 Rating E", "Life Support")},
            { "int_lifesupport_size3_class2", new ShipModule(128064149, 2, 0.48F, "Time:450s","Life Support Class 3 Rating D", "Life Support")},
            { "int_lifesupport_size3_class3", new ShipModule(128064150, 5, 0.53F, "Time:600s","Life Support Class 3 Rating C", "Life Support")},
            { "int_lifesupport_size3_class4", new ShipModule(128064151, 8, 0.58F, "Time:900s","Life Support Class 3 Rating B", "Life Support")},
            { "int_lifesupport_size3_class5", new ShipModule(128064152, 5, 0.64F, "Time:1500s","Life Support Class 3 Rating A", "Life Support")},
            { "int_lifesupport_size2_class1", new ShipModule(128064143, 2.5F, 0.37F, "Time:300s","Life Support Class 2 Rating E", "Life Support")},
            { "int_lifesupport_size2_class2", new ShipModule(128064144, 1, 0.41F, "Time:450s","Life Support Class 2 Rating D", "Life Support")},
            { "int_lifesupport_size2_class3", new ShipModule(128064145, 2.5F, 0.46F, "Time:600s","Life Support Class 2 Rating C", "Life Support")},
            { "int_lifesupport_size2_class4", new ShipModule(128064146, 4, 0.51F, "Time:900s","Life Support Class 2 Rating B", "Life Support")},
            { "int_lifesupport_size2_class5", new ShipModule(128064147, 2.5F, 0.55F, "Time:1500s","Life Support Class 2 Rating A", "Life Support")},
            { "int_lifesupport_size1_class1", new ShipModule(128064138, 1.3F, 0.32F, "Time:300s","Life Support Class 1 Rating E", "Life Support")},
            { "int_lifesupport_size1_class2", new ShipModule(128064139, 0.5F, 0.36F, "Time:450s","Life Support Class 1 Rating D", "Life Support")},
            { "int_lifesupport_size1_class3", new ShipModule(128064140, 1.3F, 0.4F, "Time:600s","Life Support Class 1 Rating C", "Life Support")},
            { "int_lifesupport_size1_class4", new ShipModule(128064141, 2, 0.44F, "Time:900s","Life Support Class 1 Rating B", "Life Support")},
            { "int_lifesupport_size1_class5", new ShipModule(128064142, 1.3F, 0.48F, "Time:1500s","Life Support Class 1 Rating A", "Life Support")},
            { "int_planetapproachsuite_advanced", new ShipModule(-1,0,0,null, "Advanced Planet Approach Suite", "Planet Approach Suite" ) },
            { "int_planetapproachsuite", new ShipModule(128672317, 0, 0, null,"Planet Approach Suite", "Planet Approach Suite")},
            { "int_powerdistributor_size8_class1", new ShipModule(128064213, 160, 0.64F, "Sys:3.2MW, Eng:3.2MW, Wep:4.8MW","Power Distributor Class 8 Rating E", "Power Distributor")},
            { "int_powerdistributor_size8_class2", new ShipModule(128064214, 64, 0.72F, "Sys:3.6MW, Eng:3.6MW, Wep:5.4MW","Power Distributor Class 8 Rating D", "Power Distributor")},
            { "int_powerdistributor_size8_class3", new ShipModule(128064215, 160, 0.8F, "Sys:4MW, Eng:4MW, Wep:6MW","Power Distributor Class 8 Rating C", "Power Distributor")},
            { "int_powerdistributor_size8_class4", new ShipModule(128064216, 256, 0.88F, "Sys:4.4MW, Eng:4.4MW, Wep:6.6MW","Power Distributor Class 8 Rating B", "Power Distributor")},
            { "int_powerdistributor_size8_class5", new ShipModule(128064217, 160, 0.96F, "Sys:4.8MW, Eng:4.8MW, Wep:7.2MW","Power Distributor Class 8 Rating A", "Power Distributor")},
            { "int_powerdistributor_size7_class1", new ShipModule(128064208, 80, 0.59F, "Sys:2.6MW, Eng:2.6MW, Wep:4.1MW","Power Distributor Class 7 Rating E", "Power Distributor")},
            { "int_powerdistributor_size7_class2", new ShipModule(128064209, 32, 0.67F, "Sys:3MW, Eng:3MW, Wep:4.6MW","Power Distributor Class 7 Rating D", "Power Distributor")},
            { "int_powerdistributor_size7_class3", new ShipModule(128064210, 80, 0.74F, "Sys:3.3MW, Eng:3.3MW, Wep:5.1MW","Power Distributor Class 7 Rating C", "Power Distributor")},
            { "int_powerdistributor_size7_class4", new ShipModule(128064211, 128, 0.81F, "Sys:3.6MW, Eng:3.6MW, Wep:5.6MW","Power Distributor Class 7 Rating B", "Power Distributor")},
            { "int_powerdistributor_size7_class5", new ShipModule(128064212, 80, 0.89F, "Sys:4MW, Eng:4MW, Wep:6.1MW","Power Distributor Class 7 Rating A", "Power Distributor")},
            { "int_powerdistributor_size6_class1", new ShipModule(128064203, 40, 0.54F, "Sys:2.2MW, Eng:2.2MW, Wep:3.4MW","Power Distributor Class 6 Rating E", "Power Distributor")},
            { "int_powerdistributor_size6_class2", new ShipModule(128064204, 16, 0.61F, "Sys:2.4MW, Eng:2.4MW, Wep:3.9MW","Power Distributor Class 6 Rating D", "Power Distributor")},
            { "int_powerdistributor_size6_class3", new ShipModule(128064205, 40, 0.68F, "Sys:2.7MW, Eng:2.7MW, Wep:4.3MW","Power Distributor Class 6 Rating C", "Power Distributor")},
            { "int_powerdistributor_size6_class4", new ShipModule(128064206, 64, 0.75F, "Sys:3MW, Eng:3MW, Wep:4.7MW","Power Distributor Class 6 Rating B", "Power Distributor")},
            { "int_powerdistributor_size6_class5", new ShipModule(128064207, 40, 0.82F, "Sys:3.2MW, Eng:3.2MW, Wep:5.2MW","Power Distributor Class 6 Rating A", "Power Distributor")},
            { "int_powerdistributor_size5_class1", new ShipModule(128064198, 20, 0.5F, "Sys:1.7MW, Eng:1.7MW, Wep:2.9MW","Power Distributor Class 5 Rating E", "Power Distributor")},
            { "int_powerdistributor_size5_class2", new ShipModule(128064199, 8, 0.56F, "Sys:1.9MW, Eng:1.9MW, Wep:3.2MW","Power Distributor Class 5 Rating D", "Power Distributor")},
            { "int_powerdistributor_size5_class3", new ShipModule(128064200, 20, 0.62F, "Sys:2.1MW, Eng:2.1MW, Wep:3.6MW","Power Distributor Class 5 Rating C", "Power Distributor")},
            { "int_powerdistributor_size5_class4", new ShipModule(128064201, 32, 0.68F, "Sys:2.3MW, Eng:2.3MW, Wep:4MW","Power Distributor Class 5 Rating B", "Power Distributor")},
            { "int_powerdistributor_size5_class5", new ShipModule(128064202, 20, 0.74F, "Sys:2.5MW, Eng:2.5MW, Wep:4.3MW","Power Distributor Class 5 Rating A", "Power Distributor")},
            { "int_powerdistributor_size4_class1", new ShipModule(128064193, 10, 0.45F, "Sys:1.3MW, Eng:1.3MW, Wep:2.3MW","Power Distributor Class 4 Rating E", "Power Distributor")},
            { "int_powerdistributor_size4_class2", new ShipModule(128064194, 4, 0.5F, "Sys:1.4MW, Eng:1.4MW, Wep:2.6MW","Power Distributor Class 4 Rating D", "Power Distributor")},
            { "int_powerdistributor_size4_class3", new ShipModule(128064195, 10, 0.56F, "Sys:1.6MW, Eng:1.6MW, Wep:2.9MW","Power Distributor Class 4 Rating C", "Power Distributor")},
            { "int_powerdistributor_size4_class4", new ShipModule(128064196, 16, 0.62F, "Sys:1.8MW, Eng:1.8MW, Wep:3.2MW","Power Distributor Class 4 Rating B", "Power Distributor")},
            { "int_powerdistributor_size4_class5", new ShipModule(128064197, 10, 0.67F, "Sys:1.9MW, Eng:1.9MW, Wep:3.5MW","Power Distributor Class 4 Rating A", "Power Distributor")},
            { "int_powerdistributor_size3_class1", new ShipModule(128064188, 5, 0.4F, "Sys:0.9MW, Eng:0.9MW, Wep:1.8MW","Power Distributor Class 3 Rating E", "Power Distributor")},
            { "int_powerdistributor_size3_class2", new ShipModule(128064189, 2, 0.45F, "Sys:1MW, Eng:1MW, Wep:2.1MW","Power Distributor Class 3 Rating D", "Power Distributor")},
            { "int_powerdistributor_size3_class3", new ShipModule(128064190, 5, 0.5F, "Sys:1.1MW, Eng:1.1MW, Wep:2.3MW","Power Distributor Class 3 Rating C", "Power Distributor")},
            { "int_powerdistributor_size3_class4", new ShipModule(128064191, 8, 0.55F, "Sys:1.2MW, Eng:1.2MW, Wep:2.5MW","Power Distributor Class 3 Rating B", "Power Distributor")},
            { "int_powerdistributor_size3_class5", new ShipModule(128064192, 5, 0.6F, "Sys:1.3MW, Eng:1.3MW, Wep:2.8MW","Power Distributor Class 3 Rating A", "Power Distributor")},
            { "int_powerdistributor_size2_class1", new ShipModule(128064183, 2.5F, 0.36F, "Sys:0.6MW, Eng:0.6MW, Wep:1.4MW","Power Distributor Class 2 Rating E", "Power Distributor")},
            { "int_powerdistributor_size2_class2", new ShipModule(128064184, 1, 0.41F, "Sys:0.6MW, Eng:0.6MW, Wep:1.6MW","Power Distributor Class 2 Rating D", "Power Distributor")},
            { "int_powerdistributor_size2_class3", new ShipModule(128064185, 2.5F, 0.45F, "Sys:0.7MW, Eng:0.7MW, Wep:1.8MW","Power Distributor Class 2 Rating C", "Power Distributor")},
            { "int_powerdistributor_size2_class4", new ShipModule(128064186, 4, 0.5F, "Sys:0.8MW, Eng:0.8MW, Wep:2MW","Power Distributor Class 2 Rating B", "Power Distributor")},
            { "int_powerdistributor_size2_class5", new ShipModule(128064187, 2.5F, 0.54F, "Sys:0.8MW, Eng:0.8MW, Wep:2.2MW","Power Distributor Class 2 Rating A", "Power Distributor")},
            { "int_powerdistributor_size1_class1", new ShipModule(128064178, 1.3F, 0.32F, "Sys:0.4MW, Eng:0.4MW, Wep:1.2MW","Power Distributor Class 1 Rating E", "Power Distributor")},
            { "int_powerdistributor_size1_class2", new ShipModule(128064179, 0.5F, 0.36F, "Sys:0.5MW, Eng:0.5MW, Wep:1.4MW","Power Distributor Class 1 Rating D", "Power Distributor")},
            { "int_powerdistributor_size1_class3", new ShipModule(128064180, 1.3F, 0.4F, "Sys:0.5MW, Eng:0.5MW, Wep:1.5MW","Power Distributor Class 1 Rating C", "Power Distributor")},
            { "int_powerdistributor_size1_class4", new ShipModule(128064181, 2, 0.44F, "Sys:0.6MW, Eng:0.6MW, Wep:1.7MW","Power Distributor Class 1 Rating B", "Power Distributor")},
            { "int_powerdistributor_size1_class5", new ShipModule(128064182, 1.3F, 0.48F, "Sys:0.6MW, Eng:0.6MW, Wep:1.8MW","Power Distributor Class 1 Rating A", "Power Distributor")},
            { "int_guardianpowerdistributor_size1", new ShipModule(128833980, 1.4F, 0.62F, "Sys:0.8MW, Eng:0.8MW, Wep:2.5MW","Guardian Power Distributor Class 1", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size2", new ShipModule(128833981, 2.6F, 0.73F, "Sys:0.8MW, Eng:0.8MW, Wep:2.5MW","Guardian Power Distributor Class 2", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size3", new ShipModule(128833982, 5.25F, 0.78F, "Sys:1.7MW, Eng:1.7MW, Wep:3.1MW","Guardian Power Distributor Class 3", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size4", new ShipModule(128833983, 10.5F, 0.87F, "Sys:1.7MW, Eng:2.5MW, Wep:4.9MW","Guardian Power Distributor Class 4", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size5", new ShipModule(128833984, 21, 0.96F, "Sys:3.3MW, Eng:3.3MW, Wep:6MW","Guardian Power Distributor Class 5", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size6", new ShipModule(128833985, 42, 1.07F, "Sys:4.2MW, Eng:4.2MW, Wep:7.3MW","Guardian Power Distributor Class 6", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size7", new ShipModule(128833986, 84, 1.16F, "Sys:5.2MW, Eng:5.2MW, Wep:8.5MW","Guardian Power Distributor Class 7", "Guardian Power Distributor")},
            { "int_guardianpowerdistributor_size8", new ShipModule(128833987, 168, 1.25F, "Sys:6.2MW, Eng:6.2MW, Wep:10.1MW","Guardian Power Distributor Class 8", "Guardian Power Distributor")},
            { "int_powerplant_size8_class1", new ShipModule(128064063, 160, 0, "Power:24MW","Powerplant Class 8 Rating E", "Powerplant")},
            { "int_powerplant_size8_class2", new ShipModule(128064064, 64, 0, "Power:27MW","Powerplant Class 8 Rating D", "Powerplant")},
            { "int_powerplant_size8_class3", new ShipModule(128064065, 80, 0, "Power:30MW","Powerplant Class 8 Rating C", "Powerplant")},
            { "int_powerplant_size8_class4", new ShipModule(128064066, 128, 0, "Power:33MW","Powerplant Class 8 Rating B", "Powerplant")},
            { "int_powerplant_size8_class5", new ShipModule(128064067, 80, 0, "Power:36MW","Powerplant Class 8 Rating A", "Powerplant")},
            { "int_powerplant_size7_class1", new ShipModule(128064058, 80, 0, "Power:20MW","Powerplant Class 7 Rating E", "Powerplant")},
            { "int_powerplant_size7_class2", new ShipModule(128064059, 32, 0, "Power:22.5MW","Powerplant Class 7 Rating D", "Powerplant")},
            { "int_powerplant_size7_class3", new ShipModule(128064060, 40, 0, "Power:25MW","Powerplant Class 7 Rating C", "Powerplant")},
            { "int_powerplant_size7_class4", new ShipModule(128064061, 64, 0, "Power:27.5MW","Powerplant Class 7 Rating B", "Powerplant")},
            { "int_powerplant_size7_class5", new ShipModule(128064062, 40, 0, "Power:30MW","Powerplant Class 7 Rating A", "Powerplant")},
            { "int_powerplant_size6_class1", new ShipModule(128064053, 40, 0, "Power:16.8MW","Powerplant Class 6 Rating E", "Powerplant")},
            { "int_powerplant_size6_class2", new ShipModule(128064054, 16, 0, "Power:18.9MW","Powerplant Class 6 Rating D", "Powerplant")},
            { "int_powerplant_size6_class3", new ShipModule(128064055, 20, 0, "Power:21MW","Powerplant Class 6 Rating C", "Powerplant")},
            { "int_powerplant_size6_class4", new ShipModule(128064056, 32, 0, "Power:23.1MW","Powerplant Class 6 Rating B", "Powerplant")},
            { "int_powerplant_size6_class5", new ShipModule(128064057, 20, 0, "Power:25.2MW","Powerplant Class 6 Rating A", "Powerplant")},
            { "int_powerplant_size5_class1", new ShipModule(128064048, 20, 0, "Power:13.6MW","Powerplant Class 5 Rating E", "Powerplant")},
            { "int_powerplant_size5_class2", new ShipModule(128064049, 8, 0, "Power:15.3MW","Powerplant Class 5 Rating D", "Powerplant")},
            { "int_powerplant_size5_class3", new ShipModule(128064050, 10, 0, "Power:17MW","Powerplant Class 5 Rating C", "Powerplant")},
            { "int_powerplant_size5_class4", new ShipModule(128064051, 16, 0, "Power:18.7MW","Powerplant Class 5 Rating B", "Powerplant")},
            { "int_powerplant_size5_class5", new ShipModule(128064052, 10, 0, "Power:20.4MW","Powerplant Class 5 Rating A", "Powerplant")},
            { "int_powerplant_size4_class1", new ShipModule(128064043, 10, 0, "Power:10.4MW","Powerplant Class 4 Rating E", "Powerplant")},
            { "int_powerplant_size4_class2", new ShipModule(128064044, 4, 0, "Power:11.7MW","Powerplant Class 4 Rating D", "Powerplant")},
            { "int_powerplant_size4_class3", new ShipModule(128064045, 5, 0, "Power:13MW","Powerplant Class 4 Rating C", "Powerplant")},
            { "int_powerplant_size4_class4", new ShipModule(128064046, 8, 0, "Power:14.3MW","Powerplant Class 4 Rating B", "Powerplant")},
            { "int_powerplant_size4_class5", new ShipModule(128064047, 5, 0, "Power:15.6MW","Powerplant Class 4 Rating A", "Powerplant")},
            { "int_powerplant_size3_class1", new ShipModule(128064038, 5, 0, "Power:8MW","Powerplant Class 3 Rating E", "Powerplant")},
            { "int_powerplant_size3_class2", new ShipModule(128064039, 2, 0, "Power:9MW","Powerplant Class 3 Rating D", "Powerplant")},
            { "int_powerplant_size3_class3", new ShipModule(128064040, 2.5F, 0, "Power:10MW","Powerplant Class 3 Rating C", "Powerplant")},
            { "int_powerplant_size3_class4", new ShipModule(128064041, 4, 0, "Power:11MW","Powerplant Class 3 Rating B", "Powerplant")},
            { "int_powerplant_size3_class5", new ShipModule(128064042, 2.5F, 0, "Power:12MW","Powerplant Class 3 Rating A", "Powerplant")},
            { "int_powerplant_size2_class1", new ShipModule(128064033, 2.5F, 0, "Power:6.4MW","Powerplant Class 2 Rating E", "Powerplant")},
            { "int_powerplant_size2_class2", new ShipModule(128064034, 1, 0, "Power:7.2MW","Powerplant Class 2 Rating D", "Powerplant")},
            { "int_powerplant_size2_class3", new ShipModule(128064035, 1.3F, 0, "Power:8MW","Powerplant Class 2 Rating C", "Powerplant")},
            { "int_powerplant_size2_class4", new ShipModule(128064036, 2, 0, "Power:8.8MW","Powerplant Class 2 Rating B", "Powerplant")},
            { "int_powerplant_size2_class5", new ShipModule(128064037, 1.3F, 0, "Power:9.6MW","Powerplant Class 2 Rating A", "Powerplant")},
            { "int_guardianpowerplant_size2", new ShipModule(128833988, 1.5F, 0, "Power:12.7MW","Guardian Powerplant Class 2", "Guardian Powerplant")},
            { "int_guardianpowerplant_size3", new ShipModule(128833989, 2.9F, 0, "Power:15.8MW","Guardian Powerplant Class 3", "Guardian Powerplant")},
            { "int_guardianpowerplant_size4", new ShipModule(128833990, 5.9F, 0, "Power:20.6MW","Guardian Powerplant Class 4", "Guardian Powerplant")},
            { "int_guardianpowerplant_size5", new ShipModule(128833991, 11.7F, 0, "Power:26.9MW","Guardian Powerplant Class 5", "Guardian Powerplant")},
            { "int_guardianpowerplant_size6", new ShipModule(128833992, 23.4F, 0, "Power:33.3MW","Guardian Powerplant Class 6", "Guardian Powerplant")},
            { "int_guardianpowerplant_size7", new ShipModule(128833993, 46.8F, 0, "Power:39.6MW","Guardian Powerplant Class 7", "Guardian Powerplant")},
            { "int_guardianpowerplant_size8", new ShipModule(128833994, 93.6F, 0, "Power:47.5MW","Guardian Powerplant Class 8", "Guardian Powerplant")},
            { "int_sensors_size8_class1", new ShipModule(128064253, 160, 0.55F, "Range:5.1km","Sensors Class 8 Rating E", "Sensors")},
            { "int_sensors_size8_class2", new ShipModule(128064254, 64, 0.62F, "Range:5.8km","Sensors Class 8 Rating D", "Sensors")},
            { "int_sensors_size8_class3", new ShipModule(128064255, 160, 0.69F, "Range:6.4km","Sensors Class 8 Rating C", "Sensors")},
            { "int_sensors_size8_class4", new ShipModule(128064256, 256, 1.14F, "Range:7km","Sensors Class 8 Rating B", "Sensors")},
            { "int_sensors_size8_class5", new ShipModule(128064257, 160, 2.07F, "Range:7.7km","Sensors Class 8 Rating A", "Sensors")},
            { "int_sensors_size7_class1", new ShipModule(128064248, 80, 0.47F, "Range:5km","Sensors Class 7 Rating E", "Sensors")},
            { "int_sensors_size7_class2", new ShipModule(128064249, 32, 0.53F, "Range:5.6km","Sensors Class 7 Rating D", "Sensors")},
            { "int_sensors_size7_class3", new ShipModule(128064250, 80, 0.59F, "Range:6.2km","Sensors Class 7 Rating C", "Sensors")},
            { "int_sensors_size7_class4", new ShipModule(128064251, 128, 0.97F, "Range:6.8km","Sensors Class 7 Rating B", "Sensors")},
            { "int_sensors_size7_class5", new ShipModule(128064252, 80, 1.77F, "Range:7.4km","Sensors Class 7 Rating A", "Sensors")},
            { "int_sensors_size6_class1", new ShipModule(128064243, 40, 0.4F, "Range:4.8km","Sensors Class 6 Rating E", "Sensors")},
            { "int_sensors_size6_class2", new ShipModule(128064244, 16, 0.45F, "Range:5.4km","Sensors Class 6 Rating D", "Sensors")},
            { "int_sensors_size6_class3", new ShipModule(128064245, 40, 0.5F, "Range:6km","Sensors Class 6 Rating C", "Sensors")},
            { "int_sensors_size6_class4", new ShipModule(128064246, 64, 0.83F, "Range:6.6km","Sensors Class 6 Rating B", "Sensors")},
            { "int_sensors_size6_class5", new ShipModule(128064247, 40, 1.5F, "Range:7.2km","Sensors Class 6 Rating A", "Sensors")},
            { "int_sensors_size5_class1", new ShipModule(128064238, 20, 0.33F, "Range:4.6km","Sensors Class 5 Rating E", "Sensors")},
            { "int_sensors_size5_class2", new ShipModule(128064239, 8, 0.37F, "Range:5.2km","Sensors Class 5 Rating D", "Sensors")},
            { "int_sensors_size5_class3", new ShipModule(128064240, 20, 0.41F, "Range:5.8km","Sensors Class 5 Rating C", "Sensors")},
            { "int_sensors_size5_class4", new ShipModule(128064241, 32, 0.68F, "Range:6.4km","Sensors Class 5 Rating B", "Sensors")},
            { "int_sensors_size5_class5", new ShipModule(128064242, 20, 1.23F, "Range:7km","Sensors Class 5 Rating A", "Sensors")},
            { "int_sensors_size4_class1", new ShipModule(128064233, 10, 0.27F, "Range:4.5km","Sensors Class 4 Rating E", "Sensors")},
            { "int_sensors_size4_class2", new ShipModule(128064234, 4, 0.31F, "Range:5km","Sensors Class 4 Rating D", "Sensors")},
            { "int_sensors_size4_class3", new ShipModule(128064235, 10, 0.34F, "Range:5.6km","Sensors Class 4 Rating C", "Sensors")},
            { "int_sensors_size4_class4", new ShipModule(128064236, 16, 0.56F, "Range:6.2km","Sensors Class 4 Rating B", "Sensors")},
            { "int_sensors_size4_class5", new ShipModule(128064237, 10, 1.02F, "Range:6.7km","Sensors Class 4 Rating A", "Sensors")},
            { "int_sensors_size3_class1", new ShipModule(128064228, 5, 0.22F, "Range:4.3km","Sensors Class 3 Rating E", "Sensors")},
            { "int_sensors_size3_class2", new ShipModule(128064229, 2, 0.25F, "Range:4.9km","Sensors Class 3 Rating D", "Sensors")},
            { "int_sensors_size3_class3", new ShipModule(128064230, 5, 0.28F, "Range:5.4km","Sensors Class 3 Rating C", "Sensors")},
            { "int_sensors_size3_class4", new ShipModule(128064231, 8, 0.46F, "Range:5.9km","Sensors Class 3 Rating B", "Sensors")},
            { "int_sensors_size3_class5", new ShipModule(128064232, 5, 0.84F, "Range:6.5km","Sensors Class 3 Rating A", "Sensors")},
            { "int_sensors_size2_class1", new ShipModule(128064223, 2.5F, 0.18F, "Range:4.2km","Sensors Class 2 Rating E", "Sensors")},
            { "int_sensors_size2_class2", new ShipModule(128064224, 1, 0.21F, "Range:4.7km","Sensors Class 2 Rating D", "Sensors")},
            { "int_sensors_size2_class3", new ShipModule(128064225, 2.5F, 0.23F, "Range:5.2km","Sensors Class 2 Rating C", "Sensors")},
            { "int_sensors_size2_class4", new ShipModule(128064226, 4, 0.38F, "Range:5.7km","Sensors Class 2 Rating B", "Sensors")},
            { "int_sensors_size2_class5", new ShipModule(128064227, 2.5F, 0.69F, "Range:6.2km","Sensors Class 2 Rating A", "Sensors")},
            { "int_sensors_size1_class1", new ShipModule(128064218, 1.3F, 0.16F, "Range:4km","Sensors Class 1 Rating E", "Sensors")},
            { "int_sensors_size1_class2", new ShipModule(128064219, 0.5F, 0.18F, "Range:4.5km","Sensors Class 1 Rating D", "Sensors")},
            { "int_sensors_size1_class3", new ShipModule(128064220, 1.3F, 0.2F, "Range:5km","Sensors Class 1 Rating C", "Sensors")},
            { "int_sensors_size1_class4", new ShipModule(128064221, 2, 0.33F, "Range:5.5km","Sensors Class 1 Rating B", "Sensors")},
            { "int_sensors_size1_class5", new ShipModule(128064222, 1.3F, 0.6F, "Range:6km","Sensors Class 1 Rating A", "Sensors")},
            { "int_engine_size8_class1", new ShipModule(128064098, 160, 7.2F, "OptMass:2240t, MaxMass:3360t, MinMass:1120t","Thrusters Class 8 Rating E", "Thrusters")},
            { "int_engine_size8_class2", new ShipModule(128064099, 64, 8.1F, "OptMass:2520t, MaxMass:3780t, MinMass:1260t","Thrusters Class 8 Rating D", "Thrusters")},
            { "int_engine_size8_class3", new ShipModule(128064100, 160, 9, "OptMass:2800t, MaxMass:4200t, MinMass:1400t","Thrusters Class 8 Rating C", "Thrusters")},
            { "int_engine_size8_class4", new ShipModule(128064101, 256, 9.9F, "OptMass:3080t, MaxMass:4620t, MinMass:1540t","Thrusters Class 8 Rating B", "Thrusters")},
            { "int_engine_size8_class5", new ShipModule(128064102, 160, 10.8F, "OptMass:3360t, MaxMass:5040t, MinMass:1680t","Thrusters Class 8 Rating A", "Thrusters")},
            { "int_engine_size7_class1", new ShipModule(128064093, 80, 6.08F, "OptMass:1440t, MaxMass:2160t, MinMass:720t","Thrusters Class 7 Rating E", "Thrusters")},
            { "int_engine_size7_class2", new ShipModule(128064094, 32, 6.84F, "OptMass:1620t, MaxMass:2430t, MinMass:810t","Thrusters Class 7 Rating D", "Thrusters")},
            { "int_engine_size7_class3", new ShipModule(128064095, 80, 7.6F, "OptMass:1800t, MaxMass:2700t, MinMass:900t","Thrusters Class 7 Rating C", "Thrusters")},
            { "int_engine_size7_class4", new ShipModule(128064096, 128, 8.36F, "OptMass:1980t, MaxMass:2970t, MinMass:990t","Thrusters Class 7 Rating B", "Thrusters")},
            { "int_engine_size7_class5", new ShipModule(128064097, 80, 9.12F, "OptMass:2160t, MaxMass:3240t, MinMass:1080t","Thrusters Class 7 Rating A", "Thrusters")},
            { "int_engine_size6_class1", new ShipModule(128064088, 40, 5.04F, "OptMass:960t, MaxMass:1440t, MinMass:480t","Thrusters Class 6 Rating E", "Thrusters")},
            { "int_engine_size6_class2", new ShipModule(128064089, 16, 5.67F, "OptMass:1080t, MaxMass:1620t, MinMass:540t","Thrusters Class 6 Rating D", "Thrusters")},
            { "int_engine_size6_class3", new ShipModule(128064090, 40, 6.3F, "OptMass:1200t, MaxMass:1800t, MinMass:600t","Thrusters Class 6 Rating C", "Thrusters")},
            { "int_engine_size6_class4", new ShipModule(128064091, 64, 6.93F, "OptMass:1320t, MaxMass:1980t, MinMass:660t","Thrusters Class 6 Rating B", "Thrusters")},
            { "int_engine_size6_class5", new ShipModule(128064092, 40, 7.56F, "OptMass:1440t, MaxMass:2160t, MinMass:720t","Thrusters Class 6 Rating A", "Thrusters")},
            { "int_engine_size5_class1", new ShipModule(128064083, 20, 4.08F, "OptMass:560t, MaxMass:840t, MinMass:280t","Thrusters Class 5 Rating E", "Thrusters")},
            { "int_engine_size5_class2", new ShipModule(128064084, 8, 4.59F, "OptMass:630t, MaxMass:945t, MinMass:315t","Thrusters Class 5 Rating D", "Thrusters")},
            { "int_engine_size5_class3", new ShipModule(128064085, 20, 5.1F, "OptMass:700t, MaxMass:1050t, MinMass:350t","Thrusters Class 5 Rating C", "Thrusters")},
            { "int_engine_size5_class4", new ShipModule(128064086, 32, 5.61F, "OptMass:770t, MaxMass:1155t, MinMass:385t","Thrusters Class 5 Rating B", "Thrusters")},
            { "int_engine_size5_class5", new ShipModule(128064087, 20, 6.12F, "OptMass:840t, MaxMass:1260t, MinMass:420t","Thrusters Class 5 Rating A", "Thrusters")},
            { "int_engine_size4_class1", new ShipModule(128064078, 10, 3.28F, "OptMass:280t, MaxMass:420t, MinMass:140t","Thrusters Class 4 Rating E", "Thrusters")},
            { "int_engine_size4_class2", new ShipModule(128064079, 4, 3.69F, "OptMass:315t, MaxMass:472t, MinMass:158t","Thrusters Class 4 Rating D", "Thrusters")},
            { "int_engine_size4_class3", new ShipModule(128064080, 10, 4.1F, "OptMass:350t, MaxMass:525t, MinMass:175t","Thrusters Class 4 Rating C", "Thrusters")},
            { "int_engine_size4_class4", new ShipModule(128064081, 16, 4.51F, "OptMass:385t, MaxMass:578t, MinMass:192t","Thrusters Class 4 Rating B", "Thrusters")},
            { "int_engine_size4_class5", new ShipModule(128064082, 10, 4.92F, "OptMass:420t, MaxMass:630t, MinMass:210t","Thrusters Class 4 Rating A", "Thrusters")},
            { "int_engine_size3_class1", new ShipModule(128064073, 5, 2.48F, "OptMass:80t, MaxMass:120t, MinMass:40t","Thrusters Class 3 Rating E", "Thrusters")},
            { "int_engine_size3_class2", new ShipModule(128064074, 2, 2.79F, "OptMass:90t, MaxMass:135t, MinMass:45t","Thrusters Class 3 Rating D", "Thrusters")},
            { "int_engine_size3_class3", new ShipModule(128064075, 5, 3.1F, "OptMass:100t, MaxMass:150t, MinMass:50t","Thrusters Class 3 Rating C", "Thrusters")},
            { "int_engine_size3_class4", new ShipModule(128064076, 8, 3.41F, "OptMass:110t, MaxMass:165t, MinMass:55t","Thrusters Class 3 Rating B", "Thrusters")},
            { "int_engine_size3_class5", new ShipModule(128064077, 5, 3.72F, "OptMass:120t, MaxMass:180t, MinMass:60t","Thrusters Class 3 Rating A", "Thrusters")},
            { "int_engine_size2_class1", new ShipModule(128064068, 2.5F, 2, "OptMass:48t, MaxMass:72t, MinMass:24t","Thrusters Class 2 Rating E", "Thrusters")},
            { "int_engine_size2_class2", new ShipModule(128064069, 1, 2.25F, "OptMass:54t, MaxMass:81t, MinMass:27t","Thrusters Class 2 Rating D", "Thrusters")},
            { "int_engine_size2_class3", new ShipModule(128064070, 2.5F, 2.5F, "OptMass:60t, MaxMass:90t, MinMass:30t","Thrusters Class 2 Rating C", "Thrusters")},
            { "int_engine_size2_class4", new ShipModule(128064071, 4, 2.75F, "OptMass:66t, MaxMass:99t, MinMass:33t","Thrusters Class 2 Rating B", "Thrusters")},
            { "int_engine_size2_class5", new ShipModule(128064072, 2.5F, 3, "OptMass:72t, MaxMass:108t, MinMass:36t","Thrusters Class 2 Rating A", "Thrusters")},
            { "int_engine_size3_class5_fast", new ShipModule(128682013, 5, 5, "OptMass:90t, MaxMass:200t, MinMass:70t","Thrusters Class 3 Rating A Fast", "Thrusters")},
            { "int_engine_size2_class5_fast", new ShipModule(128682014, 2.5F, 4, "OptMass:60t, MaxMass:120t, MinMass:50t","Thrusters Class 2 Rating A Fast", "Thrusters")},

            ///////////////////////////////////// FROM SHIP DATA SCAN - Corolis has some module info in the ships folder

            { "adder_armour_grade1", new ShipModule(128049268,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Adder Lightweight Armour","Armour")},
            { "adder_armour_grade2", new ShipModule(128049269,3,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Adder Reinforced Armour","Armour")},
            { "adder_armour_grade3", new ShipModule(128049270,5,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Adder Military Armour","Armour")},
            { "adder_armour_mirrored", new ShipModule(128049271,5,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Adder Mirrored Surface Composite Armour","Armour")},
            { "adder_armour_reactive", new ShipModule(128049272,5,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Adder Reactive Surface Composite Armour","Armour")},
            { "typex_3_armour_grade1", new ShipModule(128816590,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Challenger Lightweight Armour","Armour")},
            { "typex_3_armour_grade2", new ShipModule(128816591,40,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Challenger Reinforced Armour","Armour")},
            { "typex_3_armour_grade3", new ShipModule(128816592,78,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Challenger Military Armour","Armour")},
            { "typex_3_armour_mirrored", new ShipModule(128816593,78,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Alliance Challenger Mirrored Surface Composite Armour","Armour")},
            { "typex_3_armour_reactive", new ShipModule(128816594,78,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Alliance Challenger Reactive Surface Composite Armour","Armour")},
            { "typex_armour_grade1", new ShipModule(128816576,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Chieftain Lightweight Armour","Armour")},
            { "typex_armour_grade2", new ShipModule(128816577,40,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Chieftain Reinforced Armour","Armour")},
            { "typex_armour_grade3", new ShipModule(128816578,78,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Chieftain Military Armour","Armour")},
            { "typex_armour_mirrored", new ShipModule(128816579,78,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Alliance Chieftain Mirrored Surface Composite Armour","Armour")},
            { "typex_armour_reactive", new ShipModule(128816580,78,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Alliance Chieftain Reactive Surface Composite Armour","Armour")},
            { "typex_2_armour_grade1", new ShipModule(128816583,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Crusader Lightweight Armour","Armour")},
            { "typex_2_armour_grade2", new ShipModule(128816584,40,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Crusader Reinforced Armour","Armour")},
            { "typex_2_armour_grade3", new ShipModule(128816585,78,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Alliance Crusader Military Armour","Armour")},
            { "typex_2_armour_mirrored", new ShipModule(128816586,78,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Alliance Crusader Mirrored Surface Composite Armour","Armour")},
            { "typex_2_armour_reactive", new ShipModule(128816587,78,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Alliance Crusader Reactive Surface Composite Armour","Armour")},
            { "anaconda_armour_grade1", new ShipModule(128049364,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Anaconda Lightweight Armour","Armour")},
            { "anaconda_armour_grade2", new ShipModule(128049365,30,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Anaconda Reinforced Armour","Armour")},
            { "anaconda_armour_grade3", new ShipModule(128049366,60,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Anaconda Military Armour","Armour")},
            { "anaconda_armour_mirrored", new ShipModule(128049367,60,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Anaconda Mirrored Surface Composite Armour","Armour")},
            { "anaconda_armour_reactive", new ShipModule(128049368,60,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Anaconda Reactive Surface Composite Armour","Armour")},
            { "asp_armour_grade1", new ShipModule(128049304,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Asp Explorer Lightweight Armour","Armour")},
            { "asp_armour_grade2", new ShipModule(128049305,21,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Asp Explorer Reinforced Armour","Armour")},
            { "asp_armour_grade3", new ShipModule(128049306,42,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Asp Explorer Military Armour","Armour")},
            { "asp_armour_mirrored", new ShipModule(128049307,42,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Asp Explorer Mirrored Surface Composite Armour","Armour")},
            { "asp_armour_reactive", new ShipModule(128049308,42,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Asp Explorer Reactive Surface Composite Armour","Armour")},
            { "asp_scout_armour_grade1", new ShipModule(128672278,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Asp Scout Lightweight Armour","Armour")},
            { "asp_scout_armour_grade2", new ShipModule(128672279,21,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Asp Scout Reinforced Armour","Armour")},
            { "asp_scout_armour_grade3", new ShipModule(128672280,42,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Asp Scout Military Armour","Armour")},
            { "asp_scout_armour_mirrored", new ShipModule(128672281,42,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Asp Scout Mirrored Surface Composite Armour","Armour")},
            { "asp_scout_armour_reactive", new ShipModule(128672282,42,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Asp Scout Reactive Surface Composite Armour","Armour")},
            { "belugaliner_armour_grade1", new ShipModule(128049346,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Beluga Liner Lightweight Armour","Armour")},
            { "belugaliner_armour_grade2", new ShipModule(128049347,83,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Beluga Liner Reinforced Armour","Armour")},
            { "belugaliner_armour_grade3", new ShipModule(128049348,165,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Beluga Liner Military Armour","Armour")},
            { "belugaliner_armour_mirrored", new ShipModule(128049349,165,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Beluga Liner Mirrored Surface Composite Armour","Armour")},
            { "belugaliner_armour_reactive", new ShipModule(128049350,165,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Beluga Liner Reactive Surface Composite Armour","Armour")},
            { "cobramkiii_armour_grade1", new ShipModule(128049280,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Cobra Mk III Lightweight Armour","Armour")},
            { "cobramkiii_armour_grade2", new ShipModule(128049281,14,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Cobra Mk III Reinforced Armour","Armour")},
            { "cobramkiii_armour_grade3", new ShipModule(128049282,27,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Cobra Mk III Military Armour","Armour")},
            { "cobramkiii_armour_mirrored", new ShipModule(128049283,27,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Cobra Mk III Mirrored Surface Composite Armour","Armour")},
            { "cobramkiii_armour_reactive", new ShipModule(128049284,27,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Cobra Mk III Reactive Surface Composite Armour","Armour")},
            { "cobramkiv_armour_grade1", new ShipModule(128672264,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Cobra Mk IV Lightweight Armour","Armour")},
            { "cobramkiv_armour_grade2", new ShipModule(128672265,14,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Cobra Mk IV Reinforced Armour","Armour")},
            { "cobramkiv_armour_grade3", new ShipModule(128672266,27,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Cobra Mk IV Military Armour","Armour")},
            { "cobramkiv_armour_mirrored", new ShipModule(128672267,27,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Cobra Mk IV Mirrored Surface Composite Armour","Armour")},
            { "cobramkiv_armour_reactive", new ShipModule(128672268,27,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Cobra Mk IV Reactive Surface Composite Armour","Armour")},
            { "diamondbackxl_armour_grade1", new ShipModule(128671832,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Diamondback Explorer Lightweight Armour","Armour")},
            { "diamondbackxl_armour_grade2", new ShipModule(128671833,23,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Diamondback Explorer Reinforced Armour","Armour")},
            { "diamondbackxl_armour_grade3", new ShipModule(128671834,47,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Diamondback Explorer Military Armour","Armour")},
            { "diamondbackxl_armour_mirrored", new ShipModule(128671835,26,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Diamondback Explorer Mirrored Surface Composite Armour","Armour")},
            { "diamondbackxl_armour_reactive", new ShipModule(128671836,47,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Diamondback Explorer Reactive Surface Composite Armour","Armour")},
            { "diamondback_armour_grade1", new ShipModule(128671218,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Diamondback Scout Lightweight Armour","Armour")},
            { "diamondback_armour_grade2", new ShipModule(128671219,13,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Diamondback Scout Reinforced Armour","Armour")},
            { "diamondback_armour_grade3", new ShipModule(128671220,26,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Diamondback Scout Military Armour","Armour")},
            { "diamondback_armour_mirrored", new ShipModule(128671221,26,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Diamondback Scout Mirrored Surface Composite Armour","Armour")},
            { "diamondback_armour_reactive", new ShipModule(128671222,26,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Diamondback Scout Reactive Surface Composite Armour","Armour")},
            { "dolphin_armour_grade1", new ShipModule(128049292,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Dolphin Lightweight Armour","Armour")},
            { "dolphin_armour_grade2", new ShipModule(128049293,32,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Dolphin Reinforced Armour","Armour")},
            { "dolphin_armour_grade3", new ShipModule(128049294,63,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Dolphin Military Armour","Armour")},
            { "dolphin_armour_mirrored", new ShipModule(128049295,63,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Dolphin Mirrored Surface Composite Armour","Armour")},
            { "dolphin_armour_reactive", new ShipModule(128049296,63,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Dolphin Reactive Surface Composite Armour","Armour")},
            { "eagle_armour_grade1", new ShipModule(128049256,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Eagle Lightweight Armour","Armour")},
            { "eagle_armour_grade2", new ShipModule(128049257,4,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Eagle Reinforced Armour","Armour")},
            { "eagle_armour_grade3", new ShipModule(128049258,8,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Eagle Military Armour","Armour")},
            { "eagle_armour_mirrored", new ShipModule(128049259,8,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Eagle Mirrored Surface Composite Armour","Armour")},
            { "eagle_armour_reactive", new ShipModule(128049260,8,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Eagle Reactive Surface Composite Armour","Armour")},
            { "federation_dropship_mkii_armour_grade1", new ShipModule(128672147,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Assault Ship Lightweight Armour","Armour")},
            { "federation_dropship_mkii_armour_grade2", new ShipModule(128672148,44,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Assault Ship Reinforced Armour","Armour")},
            { "federation_dropship_mkii_armour_grade3", new ShipModule(128672149,87,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Assault Ship Military Armour","Armour")},
            { "federation_dropship_mkii_armour_mirrored", new ShipModule(128672150,87,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Federal Assault Ship Mirrored Surface Composite Armour","Armour")},
            { "federation_dropship_mkii_armour_reactive", new ShipModule(128672151,87,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Federal Assault Ship Reactive Surface Composite Armour","Armour")},
            { "federation_corvette_armour_grade1", new ShipModule(128049370,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Corvette Lightweight Armour","Armour")},
            { "federation_corvette_armour_grade2", new ShipModule(128049371,30,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Corvette Reinforced Armour","Armour")},
            { "federation_corvette_armour_grade3", new ShipModule(128049372,60,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Corvette Military Armour","Armour")},
            { "federation_corvette_armour_mirrored", new ShipModule(128049373,60,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Federal Corvette Mirrored Surface Composite Armour","Armour")},
            { "federation_corvette_armour_reactive", new ShipModule(128049374,60,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Federal Corvette Reactive Surface Composite Armour","Armour")},
            { "federation_dropship_armour_grade1", new ShipModule(128049322,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Dropship Lightweight Armour","Armour")},
            { "federation_dropship_armour_grade2", new ShipModule(128049323,44,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Dropship Reinforced Armour","Armour")},
            { "federation_dropship_armour_grade3", new ShipModule(128049324,87,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Dropship Military Armour","Armour")},
            { "federation_dropship_armour_mirrored", new ShipModule(128049325,87,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Federal Dropship Mirrored Surface Composite Armour","Armour")},
            { "federation_dropship_armour_reactive", new ShipModule(128049326,87,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Federal Dropship Reactive Surface Composite Armour","Armour")},
            { "federation_gunship_armour_grade1", new ShipModule(128672154,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Gunship Lightweight Armour","Armour")},
            { "federation_gunship_armour_grade2", new ShipModule(128672155,44,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Gunship Reinforced Armour","Armour")},
            { "federation_gunship_armour_grade3", new ShipModule(128672156,87,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Federal Gunship Military Armour","Armour")},
            { "federation_gunship_armour_mirrored", new ShipModule(128672157,87,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Federal Gunship Mirrored Surface Composite Armour","Armour")},
            { "federation_gunship_armour_reactive", new ShipModule(128672158,87,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Federal Gunship Reactive Surface Composite Armour","Armour")},
            { "ferdelance_armour_grade1", new ShipModule(128049352,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Fer-de-Lance Lightweight Armour","Armour")},
            { "ferdelance_armour_grade2", new ShipModule(128049353,19,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Fer-de-Lance Reinforced Armour","Armour")},
            { "ferdelance_armour_grade3", new ShipModule(128049354,38,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Fer-de-Lance Military Armour","Armour")},
            { "ferdelance_armour_mirrored", new ShipModule(128049355,38,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Fer-de-Lance Mirrored Surface Composite Armour","Armour")},
            { "ferdelance_armour_reactive", new ShipModule(128049356,38,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Fer-de-Lance Reactive Surface Composite Armour","Armour")},
            { "hauler_armour_grade1", new ShipModule(128049262,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Hauler Lightweight Armour","Armour")},
            { "hauler_armour_grade2", new ShipModule(128049263,1,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Hauler Reinforced Armour","Armour")},
            { "hauler_armour_grade3", new ShipModule(128049264,2,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Hauler Military Armour","Armour")},
            { "hauler_armour_mirrored", new ShipModule(128049265,2,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Hauler Mirrored Surface Composite Armour","Armour")},
            { "hauler_armour_reactive", new ShipModule(128049266,2,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Hauler Reactive Surface Composite Armour","Armour")},
            { "empire_trader_armour_grade1", new ShipModule(128049316,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Clipper Lightweight Armour","Armour")},
            { "empire_trader_armour_grade2", new ShipModule(128049317,30,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Clipper Reinforced Armour","Armour")},
            { "empire_trader_armour_grade3", new ShipModule(128049318,60,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Clipper Military Armour","Armour")},
            { "empire_trader_armour_mirrored", new ShipModule(128049319,60,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Imperial Clipper Mirrored Surface Composite Armour","Armour")},
            { "empire_trader_armour_reactive", new ShipModule(128049320,60,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Imperial Clipper Reactive Surface Composite Armour","Armour")},
            { "empire_courier_armour_grade1", new ShipModule(128671224,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Courier Lightweight Armour","Armour")},
            { "empire_courier_armour_grade2", new ShipModule(128671225,4,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Courier Reinforced Armour","Armour")},
            { "empire_courier_armour_grade3", new ShipModule(128671226,8,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Courier Military Armour","Armour")},
            { "empire_courier_armour_mirrored", new ShipModule(128671227,8,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Imperial Courier Mirrored Surface Composite Armour","Armour")},
            { "empire_courier_armour_reactive", new ShipModule(128671228,8,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Imperial Courier Reactive Surface Composite Armour","Armour")},
            { "cutter_armour_grade1", new ShipModule(128049376,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Cutter Lightweight Armour","Armour")},
            { "cutter_armour_grade2", new ShipModule(128049377,30,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Cutter Reinforced Armour","Armour")},
            { "cutter_armour_grade3", new ShipModule(128049378,60,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Cutter Military Armour","Armour")},
            { "cutter_armour_mirrored", new ShipModule(128049379,60,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Imperial Cutter Mirrored Surface Composite Armour","Armour")},
            { "cutter_armour_reactive", new ShipModule(128049380,60,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Imperial Cutter Reactive Surface Composite Armour","Armour")},
            { "empire_eagle_armour_grade1", new ShipModule(128672140,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Eagle Lightweight Armour","Armour")},
            { "empire_eagle_armour_grade2", new ShipModule(128672141,4,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Eagle Reinforced Armour","Armour")},
            { "empire_eagle_armour_grade3", new ShipModule(128672142,8,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Imperial Eagle Military Armour","Armour")},
            { "empire_eagle_armour_mirrored", new ShipModule(128672143,8,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Imperial Eagle Mirrored Surface Composite Armour","Armour")},
            { "empire_eagle_armour_reactive", new ShipModule(128672144,8,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Imperial Eagle Reactive Surface Composite Armour","Armour")},
            { "independant_trader_armour_grade1", new ShipModule(128672271,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Keelback Lightweight Armour","Armour")},
            { "independant_trader_armour_grade2", new ShipModule(128672272,12,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Keelback Reinforced Armour","Armour")},
            { "independant_trader_armour_grade3", new ShipModule(128672273,23,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Keelback Military Armour","Armour")},
            { "independant_trader_armour_mirrored", new ShipModule(128672274,23,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Keelback Mirrored Surface Composite Armour","Armour")},
            { "independant_trader_armour_reactive", new ShipModule(128672275,23,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Keelback Reactive Surface Composite Armour","Armour")},
            { "krait_mkii_armour_grade1", new ShipModule(128816569,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Krait Mk II Lightweight Armour","Armour")},
            { "krait_mkii_armour_grade2", new ShipModule(128816570,36,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Krait Mk II Reinforced Armour","Armour")},
            { "krait_mkii_armour_grade3", new ShipModule(128816571,67,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Krait Mk II Military Armour","Armour")},
            { "krait_mkii_armour_mirrored", new ShipModule(128816572,67,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Krait Mk II Mirrored Surface Composite Armour","Armour")},
            { "krait_mkii_armour_reactive", new ShipModule(128816573,67,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Krait Mk II Reactive Surface Composite Armour","Armour")},
            { "krait_light_armour_grade1", new ShipModule(128839283,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Krait Phantom Lightweight Armour","Armour")},
            { "krait_light_armour_grade2", new ShipModule(128839284,26,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Krait Phantom Reinforced Armour","Armour")},
            { "krait_light_armour_grade3", new ShipModule(128839285,53,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Krait Phantom Military Armour","Armour")},
            { "krait_light_armour_mirrored", new ShipModule(128839286,53,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Krait Phantom Mirrored Surface Composite Armour","Armour")},
            { "krait_light_armour_reactive", new ShipModule(128839287,53,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Krait Phantom Reactive Surface Composite Armour","Armour")},
            { "mamba_armour_grade1", new ShipModule(128915981,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Mamba Lightweight Armour","Armour")},
            { "mamba_armour_grade2", new ShipModule(128915982,19,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Mamba Reinforced Armour","Armour")},
            { "mamba_armour_grade3", new ShipModule(128915983,38,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Mamba Military Armour","Armour")},
            { "mamba_armour_mirrored", new ShipModule(128915984,38,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Mamba Mirrored Surface Composite Armour","Armour")},
            { "mamba_armour_reactive", new ShipModule(128915985,38,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Mamba Reactive Surface Composite Armour","Armour")},
            { "orca_armour_grade1", new ShipModule(128049328,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Orca Lightweight Armour","Armour")},
            { "orca_armour_grade2", new ShipModule(128049329,21,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Orca Reinforced Armour","Armour")},
            { "orca_armour_grade3", new ShipModule(128049330,87,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Orca Military Armour","Armour")},
            { "orca_armour_mirrored", new ShipModule(128049331,87,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Orca Mirrored Surface Composite Armour","Armour")},
            { "orca_armour_reactive", new ShipModule(128049332,87,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Orca Reactive Surface Composite Armour","Armour")},
            { "python_armour_grade1", new ShipModule(128049340,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Python Lightweight Armour","Armour")},
            { "python_armour_grade2", new ShipModule(128049341,26,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Python Reinforced Armour","Armour")},
            { "python_armour_grade3", new ShipModule(128049342,53,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Python Military Armour","Armour")},
            { "python_armour_mirrored", new ShipModule(128049343,53,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Python Mirrored Surface Composite Armour","Armour")},
            { "python_armour_reactive", new ShipModule(128049344,53,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Python Reactive Surface Composite Armour","Armour")},
            { "sidewinder_armour_grade1", new ShipModule(128049250,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Sidewinder Lightweight Armour","Armour")},
            { "sidewinder_armour_grade2", new ShipModule(128049251,2,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Sidewinder Reinforced Armour","Armour")},
            { "sidewinder_armour_grade3", new ShipModule(128049252,4,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Sidewinder Military Armour","Armour")},
            { "sidewinder_armour_mirrored", new ShipModule(128049253,4,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Sidewinder Mirrored Surface Composite Armour","Armour")},
            { "sidewinder_armour_reactive", new ShipModule(128049254,4,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Sidewinder Reactive Surface Composite Armour","Armour")},
            { "type9_military_armour_grade1", new ShipModule(128785621,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-10 Defender Lightweight Armour","Armour")},
            { "type9_military_armour_grade2", new ShipModule(128785622,75,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-10 Defender Reinforced Armour","Armour")},
            { "type9_military_armour_grade3", new ShipModule(128785623,150,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-10 Defender Military Armour","Armour")},
            { "type9_military_armour_mirrored", new ShipModule(128785624,150,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Type-10 Defender Mirrored Surface Composite Armour","Armour")},
            { "type9_military_armour_reactive", new ShipModule(128785625,150,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Type-10 Defender Reactive Surface Composite Armour","Armour")},
            { "type6_armour_grade1", new ShipModule(128049286,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-6 Transporter Lightweight Armour","Armour")},
            { "type6_armour_grade2", new ShipModule(128049287,12,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-6 Transporter Reinforced Armour","Armour")},
            { "type6_armour_grade3", new ShipModule(128049288,23,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-6 Transporter Military Armour","Armour")},
            { "type6_armour_mirrored", new ShipModule(128049289,23,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Type-6 Transporter Mirrored Surface Composite Armour","Armour")},
            { "type6_armour_reactive", new ShipModule(128049290,23,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Type-6 Transporter Reactive Surface Composite Armour","Armour")},
            { "type7_armour_grade1", new ShipModule(128049298,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-7 Transporter Lightweight Armour","Armour")},
            { "type7_armour_grade2", new ShipModule(128049299,32,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-7 Transporter Reinforced Armour","Armour")},
            { "type7_armour_grade3", new ShipModule(128049300,63,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-7 Transporter Military Armour","Armour")},
            { "type7_armour_mirrored", new ShipModule(128049301,63,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Type-7 Transporter Mirrored Surface Composite Armour","Armour")},
            { "type7_armour_reactive", new ShipModule(128049302,63,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Type-7 Transporter Reactive Surface Composite Armour","Armour")},
            { "type9_armour_grade1", new ShipModule(128049334,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-9 Heavy Lightweight Armour","Armour")},
            { "type9_armour_grade2", new ShipModule(128049335,75,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-9 Heavy Reinforced Armour","Armour")},
            { "type9_armour_grade3", new ShipModule(128049336,150,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Type-9 Heavy Military Armour","Armour")},
            { "type9_armour_mirrored", new ShipModule(128049337,150,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Type-9 Heavy Mirrored Surface Composite Armour","Armour")},
            { "type9_armour_reactive", new ShipModule(128049338,150,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Type-9 Heavy Reactive Surface Composite Armour","Armour")},
            { "viper_armour_grade1", new ShipModule(128049274,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Viper Lightweight Armour","Armour")},
            { "viper_armour_grade2", new ShipModule(128049275,5,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Viper Reinforced Armour","Armour")},
            { "viper_armour_grade3", new ShipModule(128049276,9,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Viper Military Armour","Armour")},
            { "viper_armour_mirrored", new ShipModule(128049277,9,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Viper Mirrored Surface Composite Armour","Armour")},
            { "viper_armour_reactive", new ShipModule(128049278,9,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Viper Reactive Surface Composite Armour","Armour")},
            { "viper_mkiv_armour_grade1", new ShipModule(128672257,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Viper Mk IV Lightweight Armour","Armour")},
            { "viper_mkiv_armour_grade2", new ShipModule(128672258,5,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Viper Mk IV Reinforced Armour","Armour")},
            { "viper_mkiv_armour_grade3", new ShipModule(128672259,9,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Viper Mk IV Military Armour","Armour")},
            { "viper_mkiv_armour_mirrored", new ShipModule(128672260,9,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Viper Mk IV Mirrored Surface Composite Armour","Armour")},
            { "viper_mkiv_armour_reactive", new ShipModule(128672261,9,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Viper Mk IV Reactive Surface Composite Armour","Armour")},
            { "vulture_armour_grade1", new ShipModule(128049310,0,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Vulture Lightweight Armour","Armour")},
            { "vulture_armour_grade2", new ShipModule(128049311,17,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Vulture Reinforced Armour","Armour")},
            { "vulture_armour_grade3", new ShipModule(128049312,35,0,"Explosive:-40%, Kinetic:-20%, Thermal:0%","Vulture Military Armour","Armour")},
            { "vulture_armour_mirrored", new ShipModule(128049313,35,0,"Explosive:-50%, Kinetic:-75%, Thermal:50%","Vulture Mirrored Surface Composite Armour","Armour")},
            { "vulture_armour_reactive", new ShipModule(128049314,35,0,"Explosive:20%, Kinetic:25%, Thermal:-40%","Vulture Reactive Surface Composite Armour","Armour")},

        };

        #endregion

        #region ship data FROM COROLIS - use the netlogentry scanner

        static Dictionary<ShipPropID, IModuleInfo> adder = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Adder")},
            { ShipPropID.HullMass, new ShipInfoDouble(35F)},
            { ShipPropID.Name, new ShipInfoString("Adder")},
            { ShipPropID.Manu, new ShipInfoString("Zorgon Peterson")},
            { ShipPropID.Speed, new ShipInfoInt(220)},
            { ShipPropID.Boost, new ShipInfoInt(320)},
            { ShipPropID.HullCost, new ShipInfoInt(40000)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> alliance_challenger = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("TypeX_3")},
            { ShipPropID.HullMass, new ShipInfoDouble(450F)},
            { ShipPropID.Name, new ShipInfoString("Alliance Challenger")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(204)},
            { ShipPropID.Boost, new ShipInfoInt(310)},
            { ShipPropID.HullCost, new ShipInfoInt(28041035)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> alliance_chieftain = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("TypeX")},
            { ShipPropID.HullMass, new ShipInfoDouble(400F)},
            { ShipPropID.Name, new ShipInfoString("Alliance Chieftain")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(230)},
            { ShipPropID.Boost, new ShipInfoInt(330)},
            { ShipPropID.HullCost, new ShipInfoInt(18182883)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> alliance_crusader = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("TypeX_2")},
            { ShipPropID.HullMass, new ShipInfoDouble(500F)},
            { ShipPropID.Name, new ShipInfoString("Alliance Crusader")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(180)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(22866341)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> anaconda = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Anaconda")},
            { ShipPropID.HullMass, new ShipInfoDouble(400F)},
            { ShipPropID.Name, new ShipInfoString("Anaconda")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(180)},
            { ShipPropID.Boost, new ShipInfoInt(240)},
            { ShipPropID.HullCost, new ShipInfoInt(141889930)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> asp = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Asp")},
            { ShipPropID.HullMass, new ShipInfoDouble(280F)},
            { ShipPropID.Name, new ShipInfoString("Asp Explorer")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(250)},
            { ShipPropID.Boost, new ShipInfoInt(340)},
            { ShipPropID.HullCost, new ShipInfoInt(6135660)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> asp_scout = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Asp_Scout")},
            { ShipPropID.HullMass, new ShipInfoDouble(150F)},
            { ShipPropID.Name, new ShipInfoString("Asp Scout")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(220)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(3818240)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> beluga = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("BelugaLiner")},
            { ShipPropID.HullMass, new ShipInfoDouble(950F)},
            { ShipPropID.Name, new ShipInfoString("Beluga Liner")},
            { ShipPropID.Manu, new ShipInfoString("Saud Kruger")},
            { ShipPropID.Speed, new ShipInfoInt(200)},
            { ShipPropID.Boost, new ShipInfoInt(280)},
            { ShipPropID.HullCost, new ShipInfoInt(79654610)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> cobra_mk_iii = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("CobraMkIII")},
            { ShipPropID.HullMass, new ShipInfoDouble(180F)},
            { ShipPropID.Name, new ShipInfoString("Cobra Mk III")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(280)},
            { ShipPropID.Boost, new ShipInfoInt(400)},
            { ShipPropID.HullCost, new ShipInfoInt(205800)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> cobra_mk_iv = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("CobraMkIV")},
            { ShipPropID.HullMass, new ShipInfoDouble(210F)},
            { ShipPropID.Name, new ShipInfoString("Cobra Mk IV")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(200)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(603740)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> diamondback_explorer = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("DiamondBackXL")},
            { ShipPropID.HullMass, new ShipInfoDouble(260F)},
            { ShipPropID.Name, new ShipInfoString("Diamondback Explorer")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(260)},
            { ShipPropID.Boost, new ShipInfoInt(340)},
            { ShipPropID.HullCost, new ShipInfoInt(1635700)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> diamondback = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("DiamondBack")},
            { ShipPropID.HullMass, new ShipInfoDouble(170F)},
            { ShipPropID.Name, new ShipInfoString("Diamondback Scout")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(280)},
            { ShipPropID.Boost, new ShipInfoInt(380)},
            { ShipPropID.HullCost, new ShipInfoInt(461340)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> dolphin = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Dolphin")},
            { ShipPropID.HullMass, new ShipInfoDouble(140F)},
            { ShipPropID.Name, new ShipInfoString("Dolphin")},
            { ShipPropID.Manu, new ShipInfoString("Saud Kruger")},
            { ShipPropID.Speed, new ShipInfoInt(250)},
            { ShipPropID.Boost, new ShipInfoInt(350)},
            { ShipPropID.HullCost, new ShipInfoInt(1115330)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> eagle = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Eagle")},
            { ShipPropID.HullMass, new ShipInfoDouble(50F)},
            { ShipPropID.Name, new ShipInfoString("Eagle")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(240)},
            { ShipPropID.Boost, new ShipInfoInt(350)},
            { ShipPropID.HullCost, new ShipInfoInt(10440)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> federal_assault_ship = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Federation_Dropship_MkII")},
            { ShipPropID.HullMass, new ShipInfoDouble(480F)},
            { ShipPropID.Name, new ShipInfoString("Federal Assault Ship")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(210)},
            { ShipPropID.Boost, new ShipInfoInt(350)},
            { ShipPropID.HullCost, new ShipInfoInt(19072000)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> federal_corvette = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Federation_Corvette")},
            { ShipPropID.HullMass, new ShipInfoDouble(900F)},
            { ShipPropID.Name, new ShipInfoString("Federal Corvette")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(200)},
            { ShipPropID.Boost, new ShipInfoInt(260)},
            { ShipPropID.HullCost, new ShipInfoInt(182589570)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> federal_dropship = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Federation_Dropship")},
            { ShipPropID.HullMass, new ShipInfoDouble(580F)},
            { ShipPropID.Name, new ShipInfoString("Federal Dropship")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(180)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(13469990)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> federal_gunship = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Federation_Gunship")},
            { ShipPropID.HullMass, new ShipInfoDouble(580F)},
            { ShipPropID.Name, new ShipInfoString("Federal Gunship")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(170)},
            { ShipPropID.Boost, new ShipInfoInt(280)},
            { ShipPropID.HullCost, new ShipInfoInt(34774790)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> fer_de_lance = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("FerDeLance")},
            { ShipPropID.HullMass, new ShipInfoDouble(250F)},
            { ShipPropID.Name, new ShipInfoString("Fer-de-Lance")},
            { ShipPropID.Manu, new ShipInfoString("Zorgon Peterson")},
            { ShipPropID.Speed, new ShipInfoInt(260)},
            { ShipPropID.Boost, new ShipInfoInt(350)},
            { ShipPropID.HullCost, new ShipInfoInt(51232230)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> hauler = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Hauler")},
            { ShipPropID.HullMass, new ShipInfoDouble(14F)},
            { ShipPropID.Name, new ShipInfoString("Hauler")},
            { ShipPropID.Manu, new ShipInfoString("Zorgon Peterson")},
            { ShipPropID.Speed, new ShipInfoInt(200)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(29790)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> imperial_clipper = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Empire_Trader")},
            { ShipPropID.HullMass, new ShipInfoDouble(400F)},
            { ShipPropID.Name, new ShipInfoString("Imperial Clipper")},
            { ShipPropID.Manu, new ShipInfoString("Gutamaya")},
            { ShipPropID.Speed, new ShipInfoInt(300)},
            { ShipPropID.Boost, new ShipInfoInt(380)},
            { ShipPropID.HullCost, new ShipInfoInt(21077780)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> imperial_courier = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Empire_Courier")},
            { ShipPropID.HullMass, new ShipInfoDouble(35F)},
            { ShipPropID.Name, new ShipInfoString("Imperial Courier")},
            { ShipPropID.Manu, new ShipInfoString("Gutamaya")},
            { ShipPropID.Speed, new ShipInfoInt(280)},
            { ShipPropID.Boost, new ShipInfoInt(380)},
            { ShipPropID.HullCost, new ShipInfoInt(2481550)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> imperial_cutter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Cutter")},
            { ShipPropID.HullMass, new ShipInfoDouble(1100F)},
            { ShipPropID.Name, new ShipInfoString("Imperial Cutter")},
            { ShipPropID.Manu, new ShipInfoString("Gutamaya")},
            { ShipPropID.Speed, new ShipInfoInt(200)},
            { ShipPropID.Boost, new ShipInfoInt(320)},
            { ShipPropID.HullCost, new ShipInfoInt(199926890)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> imperial_eagle = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Empire_Eagle")},
            { ShipPropID.HullMass, new ShipInfoDouble(50F)},
            { ShipPropID.Name, new ShipInfoString("Imperial Eagle")},
            { ShipPropID.Manu, new ShipInfoString("Gutamaya")},
            { ShipPropID.Speed, new ShipInfoInt(300)},
            { ShipPropID.Boost, new ShipInfoInt(400)},
            { ShipPropID.HullCost, new ShipInfoInt(72180)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> keelback = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Independant_Trader")},
            { ShipPropID.HullMass, new ShipInfoDouble(180F)},
            { ShipPropID.Name, new ShipInfoString("Keelback")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(200)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(2943870)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> krait_mkii = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Krait_MkII")},
            { ShipPropID.HullMass, new ShipInfoDouble(320F)},
            { ShipPropID.Name, new ShipInfoString("Krait Mk II")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(240)},
            { ShipPropID.Boost, new ShipInfoInt(330)},
            { ShipPropID.HullCost, new ShipInfoInt(42409425)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> krait_phantom = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Krait_Light")},
            { ShipPropID.HullMass, new ShipInfoDouble(270F)},
            { ShipPropID.Name, new ShipInfoString("Krait Phantom")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(250)},
            { ShipPropID.Boost, new ShipInfoInt(350)},
            { ShipPropID.HullCost, new ShipInfoInt(42409425)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> mamba = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Mamba")},
            { ShipPropID.HullMass, new ShipInfoDouble(250F)},
            { ShipPropID.Name, new ShipInfoString("Mamba")},
            { ShipPropID.Manu, new ShipInfoString("Zorgon Peterson")},
            { ShipPropID.Speed, new ShipInfoInt(310)},
            { ShipPropID.Boost, new ShipInfoInt(380)},
            { ShipPropID.HullCost, new ShipInfoInt(55866341)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> orca = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Orca")},
            { ShipPropID.HullMass, new ShipInfoDouble(290F)},
            { ShipPropID.Name, new ShipInfoString("Orca")},
            { ShipPropID.Manu, new ShipInfoString("Saud Kruger")},
            { ShipPropID.Speed, new ShipInfoInt(300)},
            { ShipPropID.Boost, new ShipInfoInt(380)},
            { ShipPropID.HullCost, new ShipInfoInt(47790590)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> python = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Python")},
            { ShipPropID.HullMass, new ShipInfoDouble(350F)},
            { ShipPropID.Name, new ShipInfoString("Python")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(230)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(55171380)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> sidewinder = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("SideWinder")},
            { ShipPropID.HullMass, new ShipInfoDouble(25F)},
            { ShipPropID.Name, new ShipInfoString("Sidewinder")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(220)},
            { ShipPropID.Boost, new ShipInfoInt(320)},
            { ShipPropID.HullCost, new ShipInfoInt(4070)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> type_10_defender = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Type9_Military")},
            { ShipPropID.HullMass, new ShipInfoDouble(1200F)},
            { ShipPropID.Name, new ShipInfoString("Type-10 Defender")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(179)},
            { ShipPropID.Boost, new ShipInfoInt(219)},
            { ShipPropID.HullCost, new ShipInfoInt(121454173)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> type_6_transporter = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Type6")},
            { ShipPropID.HullMass, new ShipInfoDouble(155F)},
            { ShipPropID.Name, new ShipInfoString("Type-6 Transporter")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(220)},
            { ShipPropID.Boost, new ShipInfoInt(350)},
            { ShipPropID.HullCost, new ShipInfoInt(865790)},
            { ShipPropID.Class, new ShipInfoInt(2)},
        };
        static Dictionary<ShipPropID, IModuleInfo> type_7_transport = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Type7")},
            { ShipPropID.HullMass, new ShipInfoDouble(350F)},
            { ShipPropID.Name, new ShipInfoString("Type-7 Transporter")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(180)},
            { ShipPropID.Boost, new ShipInfoInt(300)},
            { ShipPropID.HullCost, new ShipInfoInt(16780510)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> type_9_heavy = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Type9")},
            { ShipPropID.HullMass, new ShipInfoDouble(850F)},
            { ShipPropID.Name, new ShipInfoString("Type-9 Heavy")},
            { ShipPropID.Manu, new ShipInfoString("Lakon")},
            { ShipPropID.Speed, new ShipInfoInt(130)},
            { ShipPropID.Boost, new ShipInfoInt(200)},
            { ShipPropID.HullCost, new ShipInfoInt(73255150)},
            { ShipPropID.Class, new ShipInfoInt(3)},
        };
        static Dictionary<ShipPropID, IModuleInfo> viper = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Viper")},
            { ShipPropID.HullMass, new ShipInfoDouble(50F)},
            { ShipPropID.Name, new ShipInfoString("Viper Mk III")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(320)},
            { ShipPropID.Boost, new ShipInfoInt(400)},
            { ShipPropID.HullCost, new ShipInfoInt(95900)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> viper_mk_iv = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Viper_MkIV")},
            { ShipPropID.HullMass, new ShipInfoDouble(190F)},
            { ShipPropID.Name, new ShipInfoString("Viper Mk IV")},
            { ShipPropID.Manu, new ShipInfoString("Faulcon DeLacy")},
            { ShipPropID.Speed, new ShipInfoInt(270)},
            { ShipPropID.Boost, new ShipInfoInt(340)},
            { ShipPropID.HullCost, new ShipInfoInt(310220)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<ShipPropID, IModuleInfo> vulture = new Dictionary<ShipPropID, IModuleInfo>
        {
            { ShipPropID.FDID, new ShipInfoString("Vulture")},
            { ShipPropID.HullMass, new ShipInfoDouble(230F)},
            { ShipPropID.Name, new ShipInfoString("Vulture")},
            { ShipPropID.Manu, new ShipInfoString("Core Dynamics")},
            { ShipPropID.Speed, new ShipInfoInt(210)},
            { ShipPropID.Boost, new ShipInfoInt(340)},
            { ShipPropID.HullCost, new ShipInfoInt(4689640)},
            { ShipPropID.Class, new ShipInfoInt(1)},
        };
        static Dictionary<string, Dictionary<ShipPropID, IModuleInfo>> coriolisships = new Dictionary<string, Dictionary<ShipPropID, IModuleInfo>>
        {
            { "adder",adder},
            { "typex_3",alliance_challenger},
            { "typex",alliance_chieftain},
            { "typex_2",alliance_crusader},
            { "anaconda",anaconda},
            { "asp",asp},
            { "asp_scout",asp_scout},
            { "belugaliner",beluga},
            { "cobramkiii",cobra_mk_iii},
            { "cobramkiv",cobra_mk_iv},
            { "diamondbackxl",diamondback_explorer},
            { "diamondback",diamondback},
            { "dolphin",dolphin},
            { "eagle",eagle},
            { "federation_dropship_mkii",federal_assault_ship},
            { "federation_corvette",federal_corvette},
            { "federation_dropship",federal_dropship},
            { "federation_gunship",federal_gunship},
            { "ferdelance",fer_de_lance},
            { "hauler",hauler},
            { "empire_trader",imperial_clipper},
            { "empire_courier",imperial_courier},
            { "cutter",imperial_cutter},
            { "empire_eagle",imperial_eagle},
            { "independant_trader",keelback},
            { "krait_mkii",krait_mkii},
            { "krait_light",krait_phantom},
            { "mamba",mamba},
            { "orca",orca},
            { "python",python},
            { "sidewinder",sidewinder},
            { "type9_military",type_10_defender},
            { "type6",type_6_transporter},
            { "type7",type_7_transport},
            { "type9",type_9_heavy},
            { "viper",viper},
            { "viper_mkiv",viper_mk_iv},
            { "vulture",vulture},
        };

        #endregion

    }
}
