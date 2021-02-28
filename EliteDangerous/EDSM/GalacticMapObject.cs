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

using EMK.LightGeometry;
using System;
using System.Collections.Generic;
using BaseUtils.JSON;
using System.Diagnostics;

namespace EliteDangerousCore.EDSM
{
    [DebuggerDisplay("GMO {name} {type}")]
    public class GalacticMapObject
    {
        public int id;
        public string type;
        public string name;
        public string galMapSearch;
        public string galMapUrl;
        public string colour;
        public List<Vector3> points;
        public string description;
        public string descriptionhtml;

        public GalMapType galMapType;

        public GalacticMapObject()
        {
            points = new List<Vector3>();
        }

        public GalacticMapObject(JObject jo)
        {
            id = jo["id"].Int();
            type = jo["type"].Str("Not Set");
            name = jo["name"].Str("No name set");
            galMapSearch = jo["galMapSearch"].Str("");
            galMapUrl = jo["galMapUrl"].Str("");
            colour = jo["color"].Str("Orange");
            description = jo["descriptionMardown"].Str("No description");
            descriptionhtml = jo["descriptionHtml"].Str("");
            
            points = new List<Vector3>();

            try
            {
                JArray coords = (JArray)jo["coordinates"];

                if (coords.Count > 0)
                {
                    if (coords[0].IsArray)
                    {
                        foreach (JArray ja in coords)
                        {
                            float x, y, z;
                            x = ja[0].Float();
                            y = ja[1].Float();
                            z = ja[2].Float();
                            points.Add(new Vector3(x, y, z));
                        }
                    }
                    else
                    {
                        JArray plist = coords;

                        float x, y, z;
                        x = plist[0].Float();
                        y = plist[1].Float();
                        z = plist[2].Float();
                        points.Add(new Vector3(x, y, z));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GalacticMapObject parse coordinate error: type" + type + " " + ex.Message);
                points = null;
            }
        }

        public GalacticMapSystem GetSystem(ISystem sys = null)
        {
            if (sys != null)
                return new EDSM.GalacticMapSystem(sys, this);
            else
                return new EDSM.GalacticMapSystem(this);
        }
    }
}

