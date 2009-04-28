﻿/*
 * Process Hacker - 
 *   Node implementation for the process tree
 * 
 * Copyright (C) 2008-2009 wj32
 * 
 * This file is part of Process Hacker.
 * 
 * Process Hacker is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Process Hacker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Process Hacker.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using Aga.Controls.Tree;
using ProcessHacker.Native.Objects;
using ProcessHacker.Native.Security;

namespace ProcessHacker
{
    public class ProcessNode : Node, IDisposable
    {
        private List<ProcessNode> _children = new List<ProcessNode>();
        private ProcessItem _pitem;
        private bool _wasNoIcon = false;
        private Bitmap _icon;

        public ProcessNode(ProcessItem pitem)
        {
            _pitem = pitem;
            this.Tag = pitem.Pid;

            if (_pitem.Icon == null)
            {
                _wasNoIcon = true;
                _icon = global::ProcessHacker.Properties.Resources.Process_small.ToBitmap();
            }
            else
            {
                try
                {
                    _icon = _pitem.Icon.ToBitmap();
                }
                catch
                {
                    _wasNoIcon = true;
                    _icon = global::ProcessHacker.Properties.Resources.Process_small.ToBitmap();
                }
            }
        }

        ~ProcessNode()
        {
            if (_icon != null)
                this.Dispose();
        }

        public void Dispose()
        {
            if (_icon != null)
            {
                _icon.Dispose();
                _icon = null;
            }
        }

        public ProcessItem ProcessItem
        {
            get { return _pitem; }
            set
            {
                _pitem = value;

                if (_wasNoIcon && _pitem.Icon != null)
                {
                    if (_icon != null)
                        _icon.Dispose();

                    _icon = new Bitmap(16, 16);

                    try
                    {
                        using (Graphics g = Graphics.FromImage(_icon))
                            g.DrawIcon(_pitem.Icon, new Rectangle(0, 0, 16, 16));

                        _wasNoIcon = false;
                    }
                    catch
                    {
                        _icon.Dispose();
                        _icon = null;
                    }
                }
            }
        }

        public List<ProcessNode> Children
        {
            get { return _children; }
        }

        public ProcessHacker.Components.NodePlotter.PlotterInfo CpuHistory
        {
            get
            {
                return new ProcessHacker.Components.NodePlotter.PlotterInfo()
                {
                    UseSecondLine = true,
                    OverlaySecondLine = false,
                    UseLongData = false,
                    Data1 = _pitem.FloatHistoryManager[ProcessStats.CpuKernel],
                    Data2 = _pitem.FloatHistoryManager[ProcessStats.CpuUser],
                    LineColor1 = Properties.Settings.Default.PlotterCPUKernelColor,
                    LineColor2 = Properties.Settings.Default.PlotterCPUUserColor
                };
            }
        }

        public ProcessHacker.Components.NodePlotter.PlotterInfo IoHistory
        {
            get
            {
                return new ProcessHacker.Components.NodePlotter.PlotterInfo()
                {
                    UseSecondLine = true,
                    OverlaySecondLine = true,
                    UseLongData = true,
                    LongData1 = _pitem.LongHistoryManager[ProcessStats.IoReadOther],
                    LongData2 = _pitem.LongHistoryManager[ProcessStats.IoWrite],
                    LineColor1 = Properties.Settings.Default.PlotterIOROColor,
                    LineColor2 = Properties.Settings.Default.PlotterIOWColor
                };
            }
        }

        public string Name
        {
            get { return _pitem.Name != null ? _pitem.Name : ""; }
        }

        public string DisplayPID
        {
            get
            {
                if (_pitem.Pid >= 0)
                    return _pitem.Pid.ToString();
                else
                    return "";
            }
        }

        public int PID
        {
            get { return _pitem.Pid; }
        }

        public int PPID
        {
            get { if (_pitem.Pid == _pitem.ParentPid) return -1; else return _pitem.ParentPid; }
        }

        public string PvtMemory
        {
            get { return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.PrivateBytes); }
        }

        public string WorkingSet
        {
            get
            {
                return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.WorkingSetSize);
            }
        }

        public string PeakWorkingSet
        {
            get { return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.PeakWorkingSetSize); }
        }

        private int GetWorkingSetNumber(NProcessHacker.WS_INFORMATION_CLASS WsInformationClass)
        {
            int wsInfo;
            int retLen;

            try
            {
                using (var phandle = new ProcessHandle(_pitem.Pid, 
                    ProcessAccess.QueryInformation | ProcessAccess.VmRead))
                {
                    if ((retLen = NProcessHacker.PhpQueryProcessWs(phandle, WsInformationClass, out wsInfo,
                        4, out retLen)) == 0)
                        return wsInfo * Program.ProcessProvider.System.PageSize;
                }
            }
            catch
            { }

            return 0;
        }

        public int WorkingSetNumber
        {
            get { return this.GetWorkingSetNumber(NProcessHacker.WS_INFORMATION_CLASS.WsCount); }
        }

        public int PrivateWorkingSetNumber
        {
            get { return this.GetWorkingSetNumber(NProcessHacker.WS_INFORMATION_CLASS.WsPrivateCount); }
        }

        public string PrivateWorkingSet
        {
            get { return Misc.GetNiceSizeName(this.PrivateWorkingSetNumber); }
        }

        public int SharedWorkingSetNumber
        {
            get { return this.GetWorkingSetNumber(NProcessHacker.WS_INFORMATION_CLASS.WsSharedCount); }
        }

        public string SharedWorkingSet
        {
            get { return Misc.GetNiceSizeName(this.SharedWorkingSetNumber); }
        }

        public int ShareableWorkingSetNumber
        {
            get { return this.GetWorkingSetNumber(NProcessHacker.WS_INFORMATION_CLASS.WsShareableCount); }
        }

        public string ShareableWorkingSet
        {
            get { return Misc.GetNiceSizeName(this.ShareableWorkingSetNumber); }
        }

        public string VirtualSize
        {
            get { return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.VirtualSize); }
        }

        public string PeakVirtualSize
        {
            get { return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.PeakVirtualSize); }
        }

        public string PagefileUsage
        {
            get { return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.PagefileUsage); }
        }

        public string PeakPagefileUsage
        {
            get { return Misc.GetNiceSizeName(_pitem.Process.VirtualMemoryCounters.PeakPagefileUsage); }
        }

        public string PageFaults
        {
            get { return _pitem.Process.VirtualMemoryCounters.PageFaultCount.ToString("N0"); }
        }

        public string CPU
        {
            get
            {
                if (_pitem.CpuUsage == 0)
                    return "";
                else
                    return _pitem.CpuUsage.ToString("F2");
            }
        }

        private string GetBestUsername(string username, bool includeDomain)
        {
            if (username == null)
                return "";

            if (!username.Contains("\\"))
                return username;

            string[] split = username.Split(new char[] { '\\' }, 2);
            string domain = split[0];
            string user = split[1];

            if (includeDomain)
                return domain + "\\" + user;
            else
                return user;
        }

        public string Username
        {
            get { return this.GetBestUsername(_pitem.Username, Properties.Settings.Default.ShowAccountDomains); }
        }

        public string SessionId
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return _pitem.SessionId.ToString();
            }
        }

        public string PriorityClass
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return Misc.GetStringPriority(_pitem.Process.BasePriority);
            }
        }

        public string BasePriority
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return _pitem.Process.BasePriority.ToString();
            }
        }

        public string Description
        {
            get
            {
                if (PID == 0)
                    return "System Idle Process";
                else if (PID == -2)
                    return "Deferred Procedure Calls";
                else if (PID == -3)
                    return "Interrupts";
                else if (_pitem.VersionInfo != null && _pitem.VersionInfo.FileDescription != null)
                    return _pitem.VersionInfo.FileDescription;
                else
                    return "";
            }
        }

        public string Company
        {
            get
            {
                if (_pitem.VersionInfo != null && _pitem.VersionInfo.CompanyName != null)
                    return _pitem.VersionInfo.CompanyName;
                else
                    return "";
            }
        }

        public string FileName
        {
            get
            {
                if (_pitem.FileName == null)
                    return "";
                else
                    return _pitem.FileName;
            }
        }

        public string CommandLine
        {
            get
            {
                if (_pitem.CmdLine == null)
                    return "";
                else
                    return _pitem.CmdLine.Replace("\0", "");
            }
        }

        public string Threads
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return _pitem.Process.NumberOfThreads.ToString();
            }
        }

        public string Handles
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return _pitem.Process.HandleCount.ToString();
            }
        }

        public int GdiHandlesNumber
        {
            get
            {
                try
                {
                    using (var phandle = new ProcessHandle(PID, ProcessAccess.QueryInformation))
                        return phandle.GetGuiResources(false);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public string GdiHandles
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                {
                    int number = this.GdiHandlesNumber;

                    if (number == 0)
                        return "";
                    else
                        return number.ToString();
                }
            }
        }

        public int UserHandlesNumber
        {
            get
            {
                try
                {
                    using (var phandle = new ProcessHandle(PID, ProcessAccess.QueryInformation))
                        return phandle.GetGuiResources(true);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public string UserHandles
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                {
                    int number = this.UserHandlesNumber;

                    if (number == 0)
                        return "";
                    else
                        return number.ToString();
                }
            }
        }

        public long IoTotalNumber
        {
            get
            {
                if (_pitem.LongHistoryManager[ProcessStats.IoReadOther].Count == 0)
                    return 0;
                else
                    return (_pitem.LongHistoryManager[ProcessStats.IoReadOther][0] +
                        _pitem.LongHistoryManager[ProcessStats.IoWrite][0]) * 1000 /
                        Properties.Settings.Default.RefreshInterval;
            }
        }

        public string IoTotal
        {
            get
            {
                if (this.IoTotalNumber == 0)
                    return "";
                else
                    return Misc.GetNiceSizeName(this.IoTotalNumber) + "/s";
            }
        }

        public long IoReadOtherNumber
        {
            get
            {
                if (_pitem.LongHistoryManager[ProcessStats.IoReadOther].Count == 0)
                    return 0;
                else
                    return _pitem.LongHistoryManager[ProcessStats.IoReadOther][0] * 1000 /
                        Properties.Settings.Default.RefreshInterval;
            }
        }

        public string IoReadOther
        {
            get
            {
                if (this.IoReadOtherNumber == 0)
                    return "";
                else
                    return Misc.GetNiceSizeName(this.IoReadOtherNumber) + "/s";
            }
        }

        public long IoWriteNumber
        {
            get
            {
                if (_pitem.LongHistoryManager[ProcessStats.IoReadOther].Count == 0)
                    return 0;
                else
                    return _pitem.LongHistoryManager[ProcessStats.IoWrite][0] * 1000 /
                        Properties.Settings.Default.RefreshInterval;
            }
        }

        public string IoWrite
        {
            get
            {
                if (this.IoWriteNumber == 0)
                    return "";
                else
                    return Misc.GetNiceSizeName(this.IoWriteNumber) + "/s";
            }
        }

        public string Integrity
        {
            get { return _pitem.Integrity; }
        }

        public int IntegrityLevel
        {
            get { return _pitem.IntegrityLevel; }
        }

        public int IoPriority
        {
            get
            {
                try
                {
                    return _pitem.ProcessQueryHandle.GetIoPriority();
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int PagePriority
        {
            get
            {
                try
                {
                    return _pitem.ProcessQueryHandle.GetPagePriority();
                }
                catch
                {
                    return 0;
                }
            }
        }

        public Bitmap Icon
        {
            get { return _icon; }
        }

        public string StartTime
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return _pitem.CreateTime.ToString();
            }
        }

        public string RelativeStartTime
        {
            get
            {
                if (PID < 4)
                    return "";
                else
                    return Misc.GetNiceRelativeDateTime(_pitem.CreateTime);
            }
        }

        public string TotalCpuTime
        {
            get { return Misc.GetNiceTimeSpan(new TimeSpan(_pitem.Process.KernelTime + _pitem.Process.UserTime)); }
        }

        public string KernelCpuTime
        {
            get { return Misc.GetNiceTimeSpan(new TimeSpan(_pitem.Process.KernelTime)); }
        }

        public string UserCpuTime
        {
            get { return Misc.GetNiceTimeSpan(new TimeSpan(_pitem.Process.UserTime)); }
        }
    }
}
