﻿/*
 * Copyright © 2017-2019 EDDiscovery development team
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

namespace EliteDangerousCore
{
    public interface IMaterialJournalEntry
    {
        void UpdateMaterials(MaterialCommoditiesList mc);
    }

    public interface ICommodityJournalEntry
    {
        void UpdateCommodities(MaterialCommoditiesList mc);
    }

    public interface ILedgerJournalEntry
    {
        void Ledger(Ledger mcl);
    }

    public interface ILedgerNoCashJournalEntry
    {
        void LedgerNC(Ledger mcl);
    }

    public interface IShipInformation
    {
        void ShipInformation(ShipInformationList shp, string whereami, ISystem system);
    }

    public interface IBodyNameAndID
    {
        string Body { get; }
        string BodyType { get; }
        int? BodyID { get; }
        string BodyDesignation { get; set; }
        string StarSystem { get; }
        long? SystemAddress { get; }
    }

    public interface IMissions
    {
        void UpdateMissions(MissionListAccumulator mlist, ISystem sys, string body);
    }

    public interface ISystemStationEntry
    {
        bool IsTrainingEvent { get; }
    }

    public interface IAdditionalFiles
    {
        bool ReadAdditionalFiles(string directory, bool inhistoryparse);     // true if your happy
    }

    public interface IScanDataChanges       // no functions, just marks entries which change scan data. IBodyNameAndID also changes starscan
    {

    }

    public interface IJournalJumpColor
    {
        int MapColor { get; set; }
    }

    public interface IStatsJournalEntry
    {
        void UpdateStats(Stats stats, string stationfaction);
    }

    public interface IStatsJournalEntryMatCommod : IStatsJournalEntry
    {
        List<Tuple<string, int>> ItemsList { get; }     // negative means sold
    }

    public interface IStatsJournalEntryBountyOrBond : IStatsJournalEntry
    {
        string Type { get; }
        string Target { get; }
        string TargetFaction { get; }
        bool HasFaction(string faction);
        long FactionReward(string faction);
    }
}
