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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace EliteDangerousCore
{
    public class EDJournalUIScanner
    {
        public Action<JournalEntry> OnNewJournalEntry; 
        public Action<UIEvent> OnNewUIEvent;

        private Thread ScanThread;
        private ManualResetEvent StopRequested;
        private Action<Action> InvokeAsyncOnUiThread;
        private List<JournalMonitorWatcher> watchers = new List<JournalMonitorWatcher>();
        private List<StatusMonitorWatcher> statuswatchers = new List<StatusMonitorWatcher>();
        private static Guid Win32FolderId_SavedGames = new Guid("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");

        const int ScanTick = 100;       // tick time to check journals and status

        public EDJournalUIScanner(Action<Action> invokeAsyncOnUiThread)
        {
            InvokeAsyncOnUiThread = invokeAsyncOnUiThread;
        }

        public static string GetDefaultJournalDir() // may return null if not known on system
        {
            string path;

            if (EliteConfigInstance.InstanceOptions.DefaultJournalFolder.HasChars())       // journal override
                return EliteConfigInstance.InstanceOptions.DefaultJournalFolder;

            // Windows Saved Games path (Vista and above)
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6)
            {
                IntPtr pszPath;
                if (BaseUtils.Win32.UnsafeNativeMethods.SHGetKnownFolderPath(Win32FolderId_SavedGames, 0, IntPtr.Zero, out pszPath) == 0)
                {
                    path = Marshal.PtrToStringUni(pszPath);
                    Marshal.FreeCoTaskMem(pszPath);
                    return Path.Combine(path, "Frontier Developments", "Elite Dangerous");
                }
            }

            // OS X ApplicationSupportDirectory path (Darwin 12.0 == OS X 10.8)
            if (Environment.OSVersion.Platform == PlatformID.Unix && Environment.OSVersion.Version.Major >= 12)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Frontier Developments", "Elite Dangerous");

                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        #region Start stop and scan

        public void StartMonitor(bool storetodb)
        {
            StopRequested = new ManualResetEvent(false);
            ScanThread = new Thread(ScanThreadProc) { Name = "Journal Monitor Thread", IsBackground = true };
            ScanThread.Start();

            foreach (JournalMonitorWatcher mw in watchers)
            {
                mw.StartMonitor(storetodb);
            }

            foreach (StatusMonitorWatcher mw in statuswatchers)
            {
                mw.StartMonitor();
            }
        }

        public void StopMonitor()
        {
            foreach (JournalMonitorWatcher mw in watchers)
            {
                mw.StopMonitor();
            }

            foreach (StatusMonitorWatcher mw in statuswatchers)
            {
                mw.StopMonitor();
            }

            if (StopRequested != null)
            {
                lock (StopRequested) // Wait for ScanTickDone
                {
                    StopRequested.Set();
                }
            }

            if (ScanThread != null)
            {
                ScanThread.Join();
                ScanThread = null;
            }
        }

        // Journal scanner main tick - every tick, do scan tick worker, pass anything found to foreground for dispatch

        private void ScanThreadProc()
        {
            ManualResetEvent stopRequested = StopRequested;

            while (!stopRequested.WaitOne(ScanTick))
            {
                var jlu = ScanTickWorker(() => stopRequested.WaitOne(0));

                if (jlu != null && ( jlu.Item1.Count != 0 || jlu.Item2.Count != 0) && !stopRequested.WaitOne(0))
                {
                    InvokeAsyncOnUiThread(() => ScanTickDone(jlu));
                }
            }
        }

        private Tuple<List<JournalEntry>, List<UIEvent>> ScanTickWorker(Func<bool> stopRequested)     // read the entries from all watcher..
        {
            var entries = new List<JournalEntry>();
            var uientries = new List<UIEvent>();

            foreach (JournalMonitorWatcher mw in watchers)
            {
                var evret = mw.ScanForNewEntries();
                entries.AddRange(evret.Item1);
                uientries.AddRange(evret.Item2);

                if (stopRequested())
                {
                    return null;
                }
            }

            return new Tuple<List<JournalEntry>, List<UIEvent>>(entries, uientries);
        }

        private void ScanTickDone(Tuple<List<JournalEntry>, List<UIEvent>> entries)       // in UI thread..
        {
            ManualResetEvent stopRequested = StopRequested;

            if (entries != null && stopRequested != null)
            {
                foreach (var ent in entries.Item1)                    // pass them to the handler
                {
                    lock (stopRequested) // Make sure StopMonitor returns after this method returns
                    {
                        if (stopRequested.WaitOne(0))
                            return;

                        OnNewJournalEntry?.Invoke(ent);
                    }
                }

                foreach (var uient in entries.Item2)                    // pass them to the handler
                {
                    lock (stopRequested) // Make sure StopMonitor returns after this method returns
                    {
                        if (stopRequested.WaitOne(0))
                            return;

                        //System.Diagnostics.Trace.WriteLine(string.Format("New UI entry from journal {0} {1}", uient.EventTimeUTC, uient.EventTypeStr));

                        OnNewUIEvent?.Invoke(uient);
                    }
                }
            }
        }

        #endregion

        #region History refresh calls this for a set up of watchers.. then a global reparse of all journal event folders during load history

        // call to update/create watchers on joutnal and UI.  Do it with system stopped

        public void SetupWatchers()
        {
            string frontierfolder = GetDefaultJournalDir();

            System.Diagnostics.Debug.Assert(ScanThread == null);        // double check we are not scanning.

            List<EDCommander> listCommanders = EDCommander.GetListCommanders();

            // add the default frontier folder in

            if ( frontierfolder.HasChars() && Directory.Exists(frontierfolder))  // if it exists..
            {
                if (watchers.FindIndex(x => x.WatcherFolder.Equals(frontierfolder, StringComparison.InvariantCultureIgnoreCase)) < 0)  // and we are not watching it..
                {
                    System.Diagnostics.Trace.WriteLine(string.Format("New frontier watch on {0}", frontierfolder));
                    JournalMonitorWatcher mw = new JournalMonitorWatcher(frontierfolder);
                    watchers.Add(mw);

                    StatusMonitorWatcher sw = new StatusMonitorWatcher(frontierfolder, ScanTick);
                    sw.UIEventCallBack += UIEvent;
                    statuswatchers.Add(sw);
                }
            }

            // compute datapaths for each commander, making sure its set and exists. If not, use frontier folder

            List<string> datapaths = listCommanders.Select(x => (x.JournalDir.HasChars() && Directory.Exists(x.JournalDir)) ? x.JournalDir : frontierfolder).ToList();

            // see if new watchers are needed

            for (int i = 0; i < listCommanders.Count; i++)             
            {
                // if we already have a watch on this folder..
                if (watchers.FindIndex(x => x.WatcherFolder.Equals(datapaths[i], StringComparison.InvariantCultureIgnoreCase)) >= 0)       
                {
                    System.Diagnostics.Trace.WriteLine(string.Format("Duplicate watch on {0}", datapaths[i]));
                    continue;       // already done
                }

                System.Diagnostics.Trace.WriteLine(string.Format("New watch on {0}", datapaths[i]));
                JournalMonitorWatcher mw = new JournalMonitorWatcher(datapaths[i]);
                watchers.Add(mw);

                StatusMonitorWatcher sw = new StatusMonitorWatcher(datapaths[i], ScanTick);
                sw.UIEventCallBack += UIEvent;
                statuswatchers.Add(sw);

            }

            // clean up monitors on journals
            {
                List<int> tobedeleted = new List<int>();
                for (int i = 0; i < watchers.Count; i++)
                {
                    bool found = false;

                    if (frontierfolder != null && watchers[i].WatcherFolder.Equals(frontierfolder, StringComparison.InvariantCultureIgnoreCase))
                        found = true;

                    for (int j = 0; j < listCommanders.Count; j++)          // all commanders, see if this watch folder is present
                        found |= watchers[i].WatcherFolder.Equals(datapaths[j], StringComparison.InvariantCultureIgnoreCase);

                    if (!found)
                        tobedeleted.Add(i);
                }

                foreach (int i in tobedeleted)
                {
                    System.Diagnostics.Trace.WriteLine(string.Format("Delete watch on {0}", watchers[i].WatcherFolder));
                    JournalMonitorWatcher mw = watchers[i];
                    mw.StopMonitor();          // just in case
                    watchers.Remove(mw);
                }
            }

            // and on status files
            {
                List<int> statustobedeleted = new List<int>();
                for (int i = 0; i < statuswatchers.Count; i++)
                {
                    bool found = false;

                    if (frontierfolder != null && statuswatchers[i].WatcherFolder.Equals(frontierfolder, StringComparison.InvariantCultureIgnoreCase))
                        found = true;

                    for (int j = 0; j < listCommanders.Count; j++)          // all commanders, see if this watch folder is present
                        found |= statuswatchers[i].WatcherFolder.Equals(datapaths[j],StringComparison.InvariantCultureIgnoreCase);

                    if (!found)
                        statustobedeleted.Add(i);
                }

                foreach (int i in statustobedeleted)
                {
                    System.Diagnostics.Trace.WriteLine(string.Format("Delete status watch on {0}", statuswatchers[i].WatcherFolder));
                    StatusMonitorWatcher mw = statuswatchers[i];
                    mw.StopMonitor();          // just in case
                    statuswatchers.Remove(mw);
                }
            }
        }

        // Go thru all watchers and check to see if any new files have been found, if so, process them and either store to DB or fireback
        // options to force reload of last N files, to fireback instead of storing the last n

        public void ParseJournalFilesOnWatchers(Action<int, string> updateProgress, 
                                                int reloadlastn,
                                                Action<JournalEntry, int, int, int, int> firebacknostore = null, int firebacklastn = 0)
        {
            System.Diagnostics.Debug.Assert(ScanThread == null);        // double check we are not scanning.

            for (int i = 0; i < watchers.Count; i++)             // parse files of all folders being watched
            {
                // may create new commanders at the end, but won't need any new watchers, because they will obv be in the same folder
                var list = watchers[i].ScanJournalFiles(reloadlastn);    
                watchers[i].ProcessDetectedNewFiles(list, updateProgress, firebacknostore, firebacklastn );
            }
        }

        #endregion

        #region UI processing

        public void UIEvent(ConcurrentQueue<UIEvent> events, string folder)     // callback, in Thread.. from monitor
        {
            InvokeAsyncOnUiThread(() => UIEventPost(events));
        }

        public void UIEventPost(ConcurrentQueue<UIEvent> events)       // UI thread
        {
            ManualResetEvent stopRequested = StopRequested;

            Debug.Assert(System.Windows.Forms.Application.MessageLoop);

            if (stopRequested != null)
            {
                while (!events.IsEmpty)
                {
                    lock (stopRequested) // Prevent StopMonitor from returning until this method has returned
                    {
                        if (stopRequested.WaitOne(0))
                            return;

                        UIEvent e;

                        if (events.TryDequeue(out e))
                        {
                            //System.Diagnostics.Trace.WriteLine(string.Format("New UI entry from status {0} {1}", e.EventTimeUTC, e.EventTypeStr));
                            OnNewUIEvent?.Invoke(e);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
