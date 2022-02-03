﻿/*
 * Copyright © 2019 EDDiscovery development team
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

using BaseUtils;
using EliteDangerousCore.JournalEvents;
using ExtendedControls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;

namespace EliteDangerousCore
{
    public partial class SystemDisplay
    {
        enum DrawLevel { TopLevelStar, PlanetLevel, MoonLevel };

        Dictionary<Bitmap, float> imageintensities = new Dictionary<Bitmap, float>();       // cached

        // return right bottom of area used from curpos

        Point DrawNode(List<ExtPictureBox.ImageElement> pc,
                            StarScan.ScanNode sn,
                            List<MaterialCommodityMicroResource> historicmats,    // curmats may be null
                            List<MaterialCommodityMicroResource> curmats,    // curmats may be null
                            Image notscanned,               // image if sn is not known
                            Point position,                 // position is normally left/middle, unless xiscentre is set.
                            bool xiscentre,
                            out Rectangle imagepos, 
                            Size size,                      // nominal size
                            DrawLevel drawtype,          // drawing..
                            Color? backwash = null,         // optional back wash on image 
                            string appendlabeltext = ""     // any label text to append
                )
        {
            string tip;
            Point endpoint = position;
            imagepos = Rectangle.Empty;

            JournalScan sc = sn.ScanData;

            if (sc != null && (!sc.IsEDSMBody || ShowEDSMBodies))     // has a scan and its our scan, or we are showing EDSM
            {
                if (sn.NodeType != StarScan.ScanNodeType.ring)       // not rings
                {
                    tip = sc.DisplayString(0, historicmats, curmats);
                    if (sn.Signals != null)
                        tip += "\n" + "Signals".T(EDTx.ScanDisplayUserControl_Signals) + ":\n" + JournalSAASignalsFound.SignalList(sn.Signals, 4, "\n");
                    if ( sn.Organics != null )
                        tip += "\n" + "Organics".T(EDTx.ScanDisplayUserControl_Organics) + ":\n" + JournalScanOrganic.OrganicList(sn.Organics,4);

                    Bitmap nodeimage = (Bitmap)BaseUtils.Icons.IconSet.GetIcon(sc.GetStarPlanetTypeImageName());

                    string overlaytext = "";
                    var nodelabels = new string[2] { "", "" };

                    nodelabels[0] = sn.CustomNameOrOwnname;
                    if (sc.IsEDSMBody)
                        nodelabels[0] = "_" + nodelabels[0];

                    if (sc.IsStar)
                    {
                        if (ShowStarClasses)
                            overlaytext = sc.StarClassificationAbv;

                        if (sc.nStellarMass.HasValue)
                            nodelabels[1] = nodelabels[1].AppendPrePad($"{sc.nStellarMass.Value:N2} SM", Environment.NewLine);

                        if (drawtype == DrawLevel.TopLevelStar)
                        {
                            if (sc.nAge.HasValue)
                                nodelabels[1] = nodelabels[1].AppendPrePad($"{sc.nAge.Value:N0} MY", Environment.NewLine);

                            if (ShowHabZone)
                            {
                                var habZone = sc.GetHabZoneStringLs();
                                if (habZone.HasChars())
                                    nodelabels[1] = nodelabels[1].AppendPrePad($"{habZone}", Environment.NewLine);
                            }
                        }
                    }
                    else
                    {
                        if (ShowPlanetClasses)
                            overlaytext = Bodies.PlanetAbv(sc.PlanetTypeID);

                        if ((sn.ScanData.IsLandable || ShowAllG) && sn.ScanData.nSurfaceGravity != null)
                        {
                            nodelabels[1] = nodelabels[1].AppendPrePad($"{(sn.ScanData.nSurfaceGravity / JournalScan.oneGee_m_s2):N2}g", Environment.NewLine);
                        }
                    }

                    if (ShowDist)
                    {
                        if (drawtype != DrawLevel.MoonLevel)       // other than moons
                        {
                            if (sn.ScanData.IsOrbitingBaryCentre)          // if in orbit of barycentre
                            {
                                string s = $"{(sn.ScanData.DistanceFromArrivalLS):N1}ls";
                                if (sn.ScanData.nSemiMajorAxis.HasValue)
                                    s += "/" + sn.ScanData.SemiMajorAxisLSKM;
                                nodelabels[1] = nodelabels[1].AppendPrePad(s, Environment.NewLine);
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine(sn.ScanData.BodyName + " SMA " + sn.ScanData.nSemiMajorAxis + " " + sn.ScanData.DistanceFromArrivalm);
                                string s = sn.ScanData.nSemiMajorAxis.HasValue && Math.Abs(sn.ScanData.nSemiMajorAxis.Value- sn.ScanData.DistanceFromArrivalm) > JournalScan.oneAU_m ? (" / " + sn.ScanData.SemiMajorAxisLSKM) : "";
                                nodelabels[1] = nodelabels[1].AppendPrePad($"{sn.ScanData.DistanceFromArrivalLS:N1}ls" + s, Environment.NewLine);
                            }
                        }
                        else
                        {
                            if (!sn.ScanData.IsOrbitingBaryCentre && sn.ScanData.nSemiMajorAxis.HasValue)          // if not in orbit of barycentre
                            {
                                nodelabels[1] = nodelabels[1].AppendPrePad($"{(sn.ScanData.nSemiMajorAxis / JournalScan.oneLS_m):N1}ls", Environment.NewLine);
                            }
                        }
                    }

                    nodelabels[1] = nodelabels[1].AppendPrePad(appendlabeltext, Environment.NewLine);

//  nodelabels[1] = nodelabels[1].AppendPrePad("" + sn.ScanData?.BodyID, Environment.NewLine);
                                                        
                    bool valuable = sc.GetEstimatedValues().EstimatedValue(sc.WasDiscovered, sc.WasMapped, true, true,false) >= ValueLimit;
                    bool isdiscovered = sc.IsPreviouslyDiscovered && sc.IsPlanet;
                    int iconoverlays = ShowOverlays ? ((sc.Terraformable ? 1 : 0) + (sc.HasMeaningfulVolcanism ? 1 : 0) + 
                                        (valuable ? 1 : 0) + (sc.Mapped ? 1 : 0) + (isdiscovered ? 1 : 0) + (sc.IsPreviouslyMapped ? 1 : 0) +
                                        (sn.Signals != null ? 1 : 0) + (sn.Organics != null ? 1 : 0)
                                        ) : 0;

                    //   if (sc.BodyName.Contains("4 b"))  iconoverlays = 0;

                    bool materialsicon = sc.HasMaterials && !ShowMaterials;
                    bool imageoverlays = sc.IsLandable || (sc.HasRings && drawtype != DrawLevel.TopLevelStar) || materialsicon;

                    int bitmapheight = size.Height * nodeheightratio / noderatiodivider;
                    int overlaywidth = bitmapheight / 6;
                    int imagewidtharea = (imageoverlays ? 2 : 1) * size.Width;            // area used by image+overlay if any
                    int iconwidtharea = (iconoverlays > 0 ? overlaywidth : 0);          // area used by icon width area on left

                    int bitmapwidth = iconwidtharea + imagewidtharea;                   // total width
                    int imageleft = iconwidtharea + imagewidtharea / 2 - size.Width / 2;  // calculate where the left of the image is 
                    int imagetop = bitmapheight / 2 - size.Height / 2;                  // and the top

                    Bitmap bmp = new Bitmap(bitmapwidth, bitmapheight);

                    using (Graphics g = Graphics.FromImage(bmp))
                    {
        //  backwash = Color.FromArgb(128, 40, 40, 40); // debug

                        if (backwash.HasValue)
                        {
                            using (Brush b = new SolidBrush(backwash.Value))
                            {
                                g.FillRectangle(b, new Rectangle(iconwidtharea, 0, imagewidtharea, bitmapheight));
                            }
                        }

                        g.DrawImage(nodeimage, imageleft, imagetop, size.Width, size.Height);

                        if (sc.IsLandable)
                        {
                            int offset = size.Height * 4 / 16;
                            int scale = 5;
                            g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Landable"), new Rectangle(imageleft + size.Width / 2 - offset * scale / 2,
                                                                                           imagetop + size.Height / 2 - offset * scale / 2, offset * scale, offset * scale));
                        }

                        if (sc.HasRings && drawtype != DrawLevel.TopLevelStar)
                        {
                            g.DrawImage(sc.Rings.Count() > 1 ? BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.RingGap") : BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.RingOnly"),
                                            new Rectangle(imageleft - size.Width / 2, imagetop, size.Width * 2, size.Height));
                        }

                        if (iconoverlays > 0)
                        {
                            int ovsize = bmp.Height / 6;
                            int pos = 4;

                            if (sc.Terraformable)
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Terraformable"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if (sc.HasMeaningfulVolcanism) //this renders below the terraformable icon if present
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Volcanism"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if (valuable)
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.HighValue"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if (sc.Mapped)
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Mapped"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if (sc.IsPreviouslyMapped)
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.MappedByOthers"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if (isdiscovered)
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.DiscoveredByOthers"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if (sn.Signals != null)
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Signals"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }

                            if ( sn.Organics != null )
                            {
                                g.DrawImage(BaseUtils.Icons.IconSet.GetIcon("Journal.ScanOrganic"), new Rectangle(0, pos, ovsize, ovsize));
                                pos += ovsize + 1;
                            }
                        }

                        if (materialsicon)
                        {
                            Image mm = BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.MaterialMore");
                            g.DrawImage(mm, new Rectangle(bmp.Width - mm.Width, bmp.Height - mm.Height, mm.Width, mm.Height));
                        }

                        if (overlaytext.HasChars())
                        {
                            float ii;
                            if (imageintensities.ContainsKey(nodeimage))        // find cache
                            {
                                ii = imageintensities[nodeimage];
                                //System.Diagnostics.Debug.WriteLine("Cached Image intensity of " + sn.fullname + " " + ii);
                            }
                            else
                            {
                                var imageintensity = nodeimage.Function(BitMapHelpers.BitmapFunction.Brightness, nodeimage.Width * 3 / 8, nodeimage.Height * 3 / 8, nodeimage.Width * 2 / 8, nodeimage.Height * 2 / 8);
                                ii = imageintensity.Item2;
                                imageintensities[nodeimage] = ii;
                                //System.Diagnostics.Debug.WriteLine("Calculated Image intensity of " + sn.fullname + " " + ii);
                            }

                            Color text = ii> 0.3f ? Color.Black : Color.FromArgb(255, 200, 200, 200);

                            using (Font f = new Font(Font.Name, size.Width / 5.0f))
                            {
                                using (Brush b = new SolidBrush(text))
                                {
                                    g.DrawString(overlaytext, f, b, new Rectangle(iconwidtharea, 0, bitmapwidth - iconwidtharea, bitmapheight), new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                                }
                            }
                        }
                    }

                    // need left middle, if xiscentre, translate to it
                    Point postoplot = xiscentre ? new Point(position.X - bmp.Width/2, position.Y) : position; 

                    //System.Diagnostics.Debug.WriteLine("Body " + sc.BodyName + " plot at "  + postoplot + " " + bmp.Size + " " + (postoplot.X+imageleft) + "," + (postoplot.Y-bmp.Height/2+imagetop));
                    endpoint = CreateImageAndLabel(pc, bmp, postoplot, bmp.Size, out imagepos, nodelabels, tip);
                    //System.Diagnostics.Debug.WriteLine("Draw {0} at {1} {2} out {3}", nodelabels[0], postoplot, bmp.Size, imagepos);

                    if (sc.HasMaterials && ShowMaterials)
                    {
                        Point matpos = new Point(endpoint.X + 4, position.Y);
                        Point endmat = CreateMaterialNodes(pc, sc, historicmats, curmats, matpos, materialsize);
                        endpoint = new Point(Math.Max(endpoint.X, endmat.X), Math.Max(endpoint.Y, endmat.Y)); // record new right point..
                    }
                }
            }
            else if (sn.NodeType == StarScan.ScanNodeType.belt)
            {
                if (sn.BeltData != null)
                    tip = sn.BeltData.RingInformationMoons(true,"");
                else
                    tip = sn.OwnName + Environment.NewLine + Environment.NewLine + "No scan data available".T(EDTx.ScanDisplayUserControl_NSD);

                if (sn.Children != null && sn.Children.Count != 0)
                {
                    foreach (StarScan.ScanNode snc in sn.Children.Values)
                    {
                        if (snc.ScanData != null)
                        {
                            string sd = snc.ScanData.DisplayString() + "\n";
                            tip += "\n" + sd;
                        }
                    }
                }

                Size bmpsize = new Size(size.Width, planetsize.Height * nodeheightratio / noderatiodivider);

                endpoint = CreateImageAndLabel(pc, BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Belt"), position, bmpsize, out imagepos, new string[] { sn.OwnName.AppendPrePad(appendlabeltext, Environment.NewLine) }, tip, false);
            }
            else
            {
                if (sn.NodeType == StarScan.ScanNodeType.barycentre)
                    tip = string.Format("Barycentre of {0}".T(EDTx.ScanDisplayUserControl_BC), sn.OwnName);
                else
                    tip = sn.OwnName + Environment.NewLine + Environment.NewLine + "No scan data available".T(EDTx.ScanDisplayUserControl_NSD);

                string nodelabel = sn.CustomName ?? sn.OwnName;
                nodelabel = nodelabel.AppendPrePad(appendlabeltext,Environment.NewLine);

                endpoint = CreateImageAndLabel(pc, notscanned, position, size, out imagepos, new string[] { nodelabel }, tip, false);
            }

            //    System.Diagnostics.Debug.WriteLine("Node " + sn.ownname + " " + position + " " + size + " -> "+ endpoint);
            return endpoint;
        }

        // curmats may be null
        Point CreateMaterialNodes(List<ExtPictureBox.ImageElement> pc, JournalScan sn, List<MaterialCommodityMicroResource> historicmats, List<MaterialCommodityMicroResource> curmats, 
                                Point matpos, Size matsize)
        {
            Point startpos = matpos;
            Point maximum = matpos;
            int noperline = 0;

            string matclicktext = sn.DisplayMaterials(2, historicmats, curmats );

            foreach (KeyValuePair<string, double> sd in sn.Materials)
            {
                string tooltip = sn.DisplayMaterial(sd.Key, sd.Value, historicmats, curmats);

                Color fillc = Color.Yellow;
                string abv = sd.Key.Substring(0, 1);

                MaterialCommodityMicroResourceType mc = MaterialCommodityMicroResourceType.GetByFDName(sd.Key);

                if (mc != null)
                {
                    abv = mc.Shortname;
                    fillc = mc.Colour;
                    //System.Diagnostics.Debug.WriteLine("Colour {0} {1}", fillc.ToString(), fillc.GetBrightness());

                    if (HideFullMaterials)                 // check full
                    {
                        int? limit = mc.MaterialLimit();
                        MaterialCommodityMicroResource matnow = curmats?.Find(x=>x.Details == mc);  // allow curmats = null

                        // debug if (matnow != null && mc.shortname == "Fe")  matnow.count = 10000;

                        if (matnow != null && limit != null && matnow.Count >= limit)        // and limit
                            continue;
                    }

                    if (ShowOnlyMaterialsRare && mc.IsCommonMaterial)
                        continue;
                }

                System.Drawing.Imaging.ColorMap colormap = new System.Drawing.Imaging.ColorMap();
                colormap.OldColor = Color.White;    // this is the marker colour to replace
                colormap.NewColor = fillc;

                Bitmap mat = BaseUtils.BitMapHelpers.ReplaceColourInBitmap((Bitmap)BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Material"), new System.Drawing.Imaging.ColorMap[] { colormap });

                BaseUtils.BitMapHelpers.DrawTextCentreIntoBitmap(ref mat, abv, Font, fillc.GetBrightness() > 0.4f ?  Color.Black : Color.White);

                ExtPictureBox.ImageElement ie = new ExtPictureBox.ImageElement(
                                new Rectangle(matpos.X, matpos.Y, matsize.Width, matsize.Height), mat, tooltip + "\n\n" + "All " + matclicktext, tooltip);

                pc.Add(ie);

                maximum = new Point(Math.Max(maximum.X, matpos.X + matsize.Width), Math.Max(maximum.Y, matpos.Y + matsize.Height));

                if (++noperline == 4)
                {
                    matpos = new Point(startpos.X, matpos.Y + matsize.Height + materiallinespacerxy);
                    noperline = 0;
                }
                else
                    matpos.X += matsize.Width + materiallinespacerxy;
            }

            return maximum;
        }

        // Create a signals list
        Point DrawSignals(List<ExtPictureBox.ImageElement> pc, Point leftmiddle , 
                                List<JournalFSSSignalDiscovered.FSSSignal> signals, List<JournalCodexEntry> codex,
                                int height, int shiftrightifreq)
        {
            const int maxicons = 5;
            int iconsize = height / maxicons;
            Bitmap bmp = new Bitmap(iconsize, height);

            int[] count = new int[]     // in priority order
            {
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.Station).Count(),
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.Carrier).Count(),
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.Installation).Count(),
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.NotableStellarPhenomena).Count(),
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.ResourceExtraction).Count(),
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.ConflictZone).Count(),
                signals.Where(x => x.ClassOfSignal == JournalFSSSignalDiscovered.FSSSignal.Classification.USS).Count(),
                0, // 7, slot for others
                0, // 8, slot for codex
            };

            count[7] = signals.Count - (from x in count select x).Sum();        // set seven to signals left

            int icons;
            int knockout = 6;       // starting from this signal, work backwards knocking out if required
            while(true)
            {
                icons = (from x in count where x > 0 select 1).Sum();           // how many are set?
                if (icons > maxicons)        // too many
                {
                    count[7] = 1;               // okay set the generic signal one
                    count[knockout--] = 0;      // and knock this out
                }
                else
                    break;
            }

            if ( icons < maxicons && codex.Count>0 )        // if we have space for codex, and we have codex, add in
            {
                icons++;
                count[8] = 1;
            }

            Image[] images = new Image[]
            {
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Stations"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Carriers"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Installations"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.NSP"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.RES"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.CZ"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.USS"),
                BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Signals"),
                BaseUtils.Icons.IconSet.GetIcon("Journal.CodexEntry"),
            };

            int vpos = height / 2 - iconsize * icons / 2;

            using (Graphics g = Graphics.FromImage(bmp))
            {
            //    g.Clear(Color.FromArgb(20, 64, 64)); // debug
                for (int i = 0; i < count.Length; i++)
                {
                    if (count[i] > 0)
                    {
                        g.DrawImage(images[i], new Rectangle(0, vpos, iconsize, iconsize));
                        vpos += iconsize;
                    }
                }
            }

            string tip = "";

            var notexpired = signals.Where(x => !x.TimeRemaining.HasValue || x.ExpiryUTC >= DateTime.UtcNow).ToList();
            notexpired.Sort(delegate (JournalFSSSignalDiscovered.FSSSignal l, JournalFSSSignalDiscovered.FSSSignal r) { return l.ClassOfSignal.CompareTo(r.ClassOfSignal); });
            foreach (var sig in notexpired )
                tip = tip.AppendPrePad(sig.ToString(true), Environment.NewLine);

            var expired = signals.Where(x => x.TimeRemaining.HasValue && x.ExpiryUTC < DateTime.UtcNow).ToList();

            if (expired.Count > 0)
            {
                expired.Sort(delegate (JournalFSSSignalDiscovered.FSSSignal l, JournalFSSSignalDiscovered.FSSSignal r) { return r.ExpiryUTC.CompareTo(l.ExpiryUTC); });
                tip = tip.AppendPrePad("Expired:".T(EDTx.UserControlScan_Expired), Environment.NewLine + Environment.NewLine);
                foreach (var sig in expired)
                    tip = tip.AppendPrePad(sig.ToString(true), Environment.NewLine);
            }

            if ( codex.Count>0)
            {
                tip = tip.AppendPrePad("Codex".T(EDTx.ScanDisplayUserControl_Codex) + ":", Environment.NewLine + Environment.NewLine);
                foreach ( var c in codex)
                {
                    tip = tip.AppendPrePad(c.Info(), Environment.NewLine);
                }
            }

            if (icons > 4)
                leftmiddle.X += shiftrightifreq;

            return CreateImageAndLabel(pc, bmp, leftmiddle, bmp.Size, out Rectangle xic, new string[] { "" }, tip, false);
        }


        // plot at leftmiddle the image of size, return bot left accounting for label 
        // label can be null. returns ximagecentre of image

        Point CreateImageAndLabel(List<ExtPictureBox.ImageElement> c, Image i, Point leftmiddle, Size size, out Rectangle imageloc , 
                                    string[] labels, string ttext, bool imgowned = true)
        {
            //System.Diagnostics.Debug.WriteLine("    " + label + " " + postopright + " size " + size + " hoff " + labelhoff + " laby " + (postopright.Y + size.Height + labelhoff));

            ExtPictureBox.ImageElement ie = new ExtPictureBox.ImageElement(new Rectangle(leftmiddle.X, leftmiddle.Y - size.Height / 2, size.Width, size.Height), i, ttext, ttext, imgowned);

            Point max = new Point(leftmiddle.X + size.Width, leftmiddle.Y + size.Height / 2);

            var labelie = new List<ExtPictureBox.ImageElement>();
            int laboff = 0;
            int vpos = leftmiddle.Y + size.Height / 2;

            foreach (string label in labels)
            {
                if (label.HasChars())
                {
                    Font f = Font;
                    int labcut = 0;
                    if (label[0] == '_')
                    {
                        f = FontUnderlined;
                        labcut = 1;
                    }

                    Point labposcenthorz = new Point(leftmiddle.X + size.Width / 2, vpos);

                    ExtPictureBox.ImageElement labie = new ExtPictureBox.ImageElement();

                    using (var frmt = new StringFormat() { Alignment = StringAlignment.Center })
                    {
                        labie.TextCentreAutoSize(labposcenthorz, new Size(0, 1000), label.Substring(labcut), f, LabelColor, BackColor, frmt: frmt);
                    }

                    labelie.Add(labie);

                    if (labie.Location.X < leftmiddle.X)
                        laboff = Math.Max(laboff, leftmiddle.X - labie.Location.X);
                    vpos += labie.Location.Height;
                }
            }


            foreach (var l in labelie)
            {
                l.Translate(laboff, 0);
                c.Add(l);
                max = new Point(Math.Max(max.X, l.Location.Right), Math.Max(max.Y, l.Location.Bottom));
                //System.Diagnostics.Debug.WriteLine("Label " + l.Location);
            }

            ie.Translate(laboff, 0);
            max = new Point(Math.Max(max.X, ie.Location.Right), Math.Max(max.Y, ie.Location.Bottom));
            c.Add(ie);

            imageloc = ie.Location;     // used to be ximagecentre = ie.Location.X+ie.Location.Width/2

            //System.Diagnostics.Debug.WriteLine(".. Max " + max);

            return max;
        }


    }
}

