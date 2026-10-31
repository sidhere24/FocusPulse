using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

namespace FocusPulse
{
    public class ActivityEngine
    {
        private static readonly Dictionary<string, string> KnownApplications = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Development & Creative Suites
            { "antigravity.exe", "Google Antigravity IDE" },
            { "Antigravity", "Google Antigravity IDE" },
            { "Code.exe", "Visual Studio Code" },
            { "cursor.exe", "Cursor AI Editor" },
            { "windsurf.exe", "Windsurf IDE" },
            { "devenv.exe", "Visual Studio" },
            { "WindowsTerminal.exe", "Windows Terminal" },
            { "powershell.exe", "PowerShell" },
            { "cmd.exe", "Command Prompt" },
            { "git-bash.exe", "Git Bash" },
            { "pycharm64.exe", "PyCharm" },
            { "idea64.exe", "IntelliJ IDEA" },
            { "sublime_text.exe", "Sublime Text" },
            { "notepad++.exe", "Notepad++" },
            { "notepad.exe", "Notepad" },
            { "figma.exe", "Figma" },
            { "blender.exe", "Blender" },

            // Web Browsers
            { "chrome.exe", "Google Chrome" },
            { "msedge.exe", "Microsoft Edge" },
            { "firefox.exe", "Mozilla Firefox" },
            { "brave.exe", "Brave Browser" },

            // Collaboration & Knowledge
            { "discord.exe", "Discord" },
            { "slack.exe", "Slack" },
            { "teams.exe", "Microsoft Teams" },
            { "notion.exe", "Notion" },
            { "obsidian.exe", "Obsidian" },
            { "excel.exe", "Microsoft Excel" },
            { "winword.exe", "Microsoft Word" },

            // Media & Games
            { "spotify.exe", "Spotify" },
            { "steam.exe", "Steam" },
            { "vlc.exe", "VLC Media Player" },

            // Windows System Shell & Productivity
            { "explorer.exe", "File Explorer" },
            { "taskmgr.exe", "Task Manager" },
            { "SystemSettings.exe", "Windows Settings" },
            { "CalculatorApp.exe", "Calculator" },
            { "SearchHost.exe", "Windows Search" },
            { "ShellExperienceHost.exe", "Windows Shell" },
            { "mstsc.exe", "Remote Desktop" },
            { "SnippingTool.exe", "Snipping Tool" },
            { "ScreenClippingHost.exe", "Snipping Tool" },
            { "DesktopEditors.exe", "ONLYOFFICE Docs" },
            { "LinkedIn.exe", "LinkedIn" },
            { "WinStore.App.exe", "Microsoft Store" },
            { "wt.exe", "Windows Terminal" },
            { "WhatsApp.exe", "WhatsApp" },
            { "zoom.exe", "Zoom Meetings" },
            { "postman.exe", "Postman API" },
            { "githubdesktop.exe", "GitHub Desktop" },
            { "opencode.exe", "OpenCode Studio" },
            { "Twitter.exe", "X / Twitter" },
            { "X.exe", "X / Twitter" }
        };

        private static readonly Dictionary<string, string> CategoryCatalog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "antigravity.exe", "Engineering" },
            { "Antigravity", "Engineering" },
            { "Code.exe", "Engineering" },
            { "cursor.exe", "Engineering" },
            { "windsurf.exe", "Engineering" },
            { "devenv.exe", "Engineering" },
            { "WindowsTerminal.exe", "Engineering" },
            { "powershell.exe", "Engineering" },
            { "cmd.exe", "Engineering" },
            { "git-bash.exe", "Engineering" },
            { "pycharm64.exe", "Engineering" },
            { "idea64.exe", "Engineering" },
            { "sublime_text.exe", "Engineering" },
            { "notepad++.exe", "Engineering" },

            { "chrome.exe", "Browsing" },
            { "msedge.exe", "Browsing" },
            { "firefox.exe", "Browsing" },
            { "brave.exe", "Browsing" },

            { "discord.exe", "Communication" },
            { "slack.exe", "Communication" },
            { "teams.exe", "Communication" },

            { "spotify.exe", "Media" },
            { "vlc.exe", "Media" },

            { "notion.exe", "Productivity" },
            { "obsidian.exe", "Productivity" },
            { "notepad.exe", "Productivity" },
            { "figma.exe", "Design" },
            { "excel.exe", "Productivity" },
            { "winword.exe", "Productivity" },

            { "explorer.exe", "System" },
            { "taskmgr.exe", "System" },
            { "SystemSettings.exe", "System" },
            { "SnippingTool.exe", "System" },
            { "ScreenClippingHost.exe", "System" },
            { "WinStore.App.exe", "System" },
            { "DesktopEditors.exe", "Productivity" },
            { "LinkedIn.exe", "Networking" },
            { "wt.exe", "Engineering" },
            { "opencode.exe", "Engineering" },
            { "WhatsApp.exe", "Communication" },
            { "zoom.exe", "Communication" },
            { "postman.exe", "Engineering" },
            { "githubdesktop.exe", "Engineering" },
            { "Twitter.exe", "Distraction" },
            { "X.exe", "Distraction" },
            { "steam.exe", "Gaming" }
        };

        private readonly object _lock = new object();
        private readonly uint _myPid;
        private DaySummary _summary;
        private ActiveWindowSnapshot _currentWindow;
        private FlowSprintSession _flowSprint;
        private WellnessCoach _wellnessCoach;
        private int _idleTimeoutSeconds = 180;
        private readonly string _storageFilePath;
        private readonly string _tasksFilePath;
        private readonly string _historyFilePath;
        private readonly List<FocusTaskItem> _tasks = new List<FocusTaskItem>();
        private readonly Dictionary<string, double> _dailyHistory = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public bool IsPaused { get; set; }
        public bool IsPrivacyShield { get; set; }
        public string ActiveProjectWorkspace { get; set; }
        public double DailyGoalHours { get; set; }
        public bool LastTickDistractionWarning { get; private set; }
        public bool SoundChimesEnabled { get; set; }
        public int PomodoroWorkMinutes { get; set; }
        public int PomodoroBreakMinutes { get; set; }
        public int PomodoroLongBreakMinutes { get; set; }

        public ActivityEngine(int idleTimeoutSeconds, double dailyGoalHours = 4.0, int breakMinutes = 50)
        {
            try
            {
                _myPid = (uint)Process.GetCurrentProcess().Id;
            }
            catch
            {
                _myPid = 0;
            }

            _idleTimeoutSeconds = idleTimeoutSeconds > 0 ? idleTimeoutSeconds : 180;
            DailyGoalHours = dailyGoalHours > 0 ? dailyGoalHours : 4.0;
            ActiveProjectWorkspace = "Development";
            IsPaused = false;
            IsPrivacyShield = false;
            SoundChimesEnabled = true;
            PomodoroWorkMinutes = 25;
            PomodoroBreakMinutes = 5;
            PomodoroLongBreakMinutes = 15;

            _currentWindow = new ActiveWindowSnapshot();
            _flowSprint = new FlowSprintSession();
            _wellnessCoach = new WellnessCoach();
            _wellnessCoach.BreakAlertThresholdSeconds = (breakMinutes > 0 ? breakMinutes : 50) * 60;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "FocusPulse");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _storageFilePath = Path.Combine(dir, "day_metrics.json");
            _tasksFilePath = Path.Combine(dir, "tasks.json");
            _historyFilePath = Path.Combine(dir, "history.json");

            LoadMetrics();
            LoadTasks();
        }

        public DaySummary Summary
        {
            get
            {
                lock (_lock)
                {
                    return _summary;
                }
            }
        }

        public ActiveWindowSnapshot CurrentWindow
        {
            get
            {
                lock (_lock)
                {
                    return _currentWindow;
                }
            }
        }

        public FlowSprintSession ActiveSprint
        {
            get
            {
                lock (_lock)
                {
                    return _flowSprint;
                }
            }
        }

        public WellnessCoach Coach
        {
            get
            {
                lock (_lock)
                {
                    return _wellnessCoach;
                }
            }
        }

        // Task Management
        public List<FocusTaskItem> Tasks
        {
            get
            {
                lock (_lock)
                {
                    return new List<FocusTaskItem>(_tasks);
                }
            }
        }

        public void AddTask(string title, string priority = "Medium")
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            lock (_lock)
            {
                FocusTaskItem item = new FocusTaskItem
                {
                    Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                    Title = title.Trim(),
                    Priority = string.IsNullOrEmpty(priority) ? "Medium" : priority,
                    IsCompleted = false,
                    PomodorosCompleted = 0,
                    CreatedDate = DateTime.Today.ToString("yyyy-MM-dd")
                };
                _tasks.Insert(0, item);
                SaveTasks();
            }
        }

        public void ToggleTask(string id)
        {
            lock (_lock)
            {
                for (int i = 0; i < _tasks.Count; i++)
                {
                    if (_tasks[i].Id == id)
                    {
                        _tasks[i].IsCompleted = !_tasks[i].IsCompleted;
                        SaveTasks();
                        break;
                    }
                }
            }
        }

        public void DeleteTask(string id)
        {
            lock (_lock)
            {
                _tasks.RemoveAll(t => t.Id == id);
                if (_flowSprint.LinkedTaskId == id)
                {
                    _flowSprint.LinkedTaskId = "";
                    _flowSprint.LinkedTaskTitle = "";
                }
                SaveTasks();
            }
        }

        public void IncrementTaskPomodoro(string id)
        {
            lock (_lock)
            {
                for (int i = 0; i < _tasks.Count; i++)
                {
                    if (_tasks[i].Id == id)
                    {
                        _tasks[i].PomodorosCompleted++;
                        SaveTasks();
                        break;
                    }
                }
            }
        }

        public void SetPomodoroTargetTask(string taskId, string taskTitle)
        {
            lock (_lock)
            {
                _flowSprint.LinkedTaskId = taskId ?? "";
                _flowSprint.LinkedTaskTitle = taskTitle ?? "";
            }
        }

        public void ClearPomodoroTargetTask()
        {
            lock (_lock)
            {
                _flowSprint.LinkedTaskId = "";
                _flowSprint.LinkedTaskTitle = "";
            }
        }

        // Pomodoro Work / Break Interval Engine
        public void StartPomodoro(int workMins, int breakMins, int longBreakMins, string taskId = "", string taskTitle = "")
        {
            lock (_lock)
            {
                PomodoroWorkMinutes = workMins > 0 ? workMins : 25;
                PomodoroBreakMinutes = breakMins > 0 ? breakMins : 5;
                PomodoroLongBreakMinutes = longBreakMins > 0 ? longBreakMins : 15;

                _flowSprint.IsActive = true;
                _flowSprint.IsPaused = false;
                _flowSprint.Mode = PomodoroMode.Focus;
                _flowSprint.TotalDurationSeconds = PomodoroWorkMinutes * 60;
                _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                _flowSprint.SprintLabel = string.Format("{0}m Deep Work", PomodoroWorkMinutes);
                _flowSprint.DistractionWarnings = 0;
                if (!string.IsNullOrEmpty(taskId))
                {
                    _flowSprint.LinkedTaskId = taskId;
                    _flowSprint.LinkedTaskTitle = taskTitle;
                }
            }
        }

        public void TogglePomodoroPause()
        {
            lock (_lock)
            {
                if (!_flowSprint.IsActive)
                {
                    StartPomodoro(PomodoroWorkMinutes, PomodoroBreakMinutes, PomodoroLongBreakMinutes, _flowSprint.LinkedTaskId, _flowSprint.LinkedTaskTitle);
                }
                else
                {
                    _flowSprint.IsPaused = !_flowSprint.IsPaused;
                }
            }
        }

        public void SkipPomodoroBreak()
        {
            lock (_lock)
            {
                if (_flowSprint.Mode == PomodoroMode.ShortBreak)
                {
                    _flowSprint.CurrentCycle++;
                    if (_flowSprint.CurrentCycle > _flowSprint.TargetCycles) _flowSprint.CurrentCycle = 1;
                }
                else if (_flowSprint.Mode == PomodoroMode.LongBreak)
                {
                    _flowSprint.CurrentCycle = 1;
                }

                _flowSprint.Mode = PomodoroMode.Focus;
                _flowSprint.TotalDurationSeconds = PomodoroWorkMinutes * 60;
                _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                _flowSprint.SprintLabel = string.Format("{0}m Deep Work", PomodoroWorkMinutes);
                _flowSprint.IsPaused = false;
                _flowSprint.IsActive = true;
            }
        }

        public void ResetPomodoro()
        {
            lock (_lock)
            {
                _flowSprint.IsActive = false;
                _flowSprint.IsPaused = false;
                _flowSprint.Mode = PomodoroMode.Idle;
                _flowSprint.TotalDurationSeconds = PomodoroWorkMinutes * 60;
                _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                _flowSprint.SprintLabel = string.Format("{0}m Deep Work", PomodoroWorkMinutes);
                _flowSprint.DistractionWarnings = 0;
            }
        }

        public void PlayChime(bool isFocusCompleted)
        {
            if (!SoundChimesEnabled) return;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (isFocusCompleted)
                    {
                        // Ascending celebratory major chord: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
                        Console.Beep(523, 100);
                        Console.Beep(659, 100);
                        Console.Beep(784, 120);
                        Console.Beep(1046, 250);
                    }
                    else
                    {
                        // Gentle dual-tone return to work chime: G5 (784Hz), E5 (659Hz)
                        Console.Beep(784, 140);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(659, 220);
                    }
                }
                catch
                {
                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                }
            });
        }

        // 7-Day Consistency & Streak Engine
        public List<WeeklyDayMetric> GetWeeklyMetrics(double targetDailyHours)
        {
            lock (_lock)
            {
                List<WeeklyDayMetric> list = new List<WeeklyDayMetric>();
                DateTime today = DateTime.Today;
                double target = targetDailyHours > 0 ? targetDailyHours : 4.0;

                for (int i = 6; i >= 0; i--)
                {
                    DateTime day = today.AddDays(-i);
                    string dateKey = day.ToString("yyyy-MM-dd");
                    double focusSec = 0;

                    if (dateKey == _summary.Date)
                    {
                        focusSec = _summary.FocusWorkSeconds;
                    }
                    else if (!_dailyHistory.TryGetValue(dateKey, out focusSec))
                    {
                        focusSec = 0;
                    }

                    WeeklyDayMetric m = new WeeklyDayMetric
                    {
                        DayLabel = day.ToString("ddd"),
                        DateString = dateKey,
                        FocusHours = focusSec / 3600.0,
                        IsToday = (i == 0),
                        TargetHours = target
                    };
                    list.Add(m);
                }
                return list;
            }
        }

        public int GetActiveStreakDays(double minHoursForStreak = 0.5)
        {
            lock (_lock)
            {
                int streak = 0;
                DateTime check = DateTime.Today;

                double todaySec = _summary.FocusWorkSeconds;
                bool todayActive = (todaySec / 3600.0) >= minHoursForStreak;
                if (todayActive)
                {
                    streak++;
                    check = check.AddDays(-1);
                }
                else
                {
                    check = check.AddDays(-1);
                }

                while (true)
                {
                    string dateKey = check.ToString("yyyy-MM-dd");
                    double sec = 0;
                    if (_dailyHistory.TryGetValue(dateKey, out sec) && (sec / 3600.0) >= minHoursForStreak)
                    {
                        streak++;
                        check = check.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }

                return (streak > 0) ? streak : (todaySec > 300 ? 1 : 0);
            }
        }

        public void StartFlowSprint(int minutes, string label)
        {
            StartPomodoro(minutes, PomodoroBreakMinutes, PomodoroLongBreakMinutes, _flowSprint.LinkedTaskId, _flowSprint.LinkedTaskTitle);
            lock (_lock)
            {
                _flowSprint.SprintLabel = label;
            }
        }

        public void StopFlowSprint()
        {
            ResetPomodoro();
        }

        public void DismissBreakPrompt()
        {
            lock (_lock)
            {
                _wellnessCoach.IsDismissed = true;
                _wellnessCoach.IsBreakRecommended = false;
            }
        }

        public void SnoozeBreakPrompt(int minutes)
        {
            lock (_lock)
            {
                _wellnessCoach.ContinuousScreenSeconds = Math.Max(0, _wellnessCoach.BreakAlertThresholdSeconds - (minutes * 60));
                _wellnessCoach.IsBreakRecommended = false;
                _wellnessCoach.IsDismissed = false;
            }
        }

        public void Tick()
        {
            if (IsPaused) return;

            // 1. Inactivity tracking via Win32 hardware input timer
            NativeMethods.LASTINPUTINFO lii = new NativeMethods.LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);
            uint idleMs = 0;
            if (NativeMethods.GetLastInputInfo(ref lii))
            {
                idleMs = (uint)Environment.TickCount - lii.dwTime;
            }
            double idleSec = idleMs / 1000.0;
            bool isIdle = idleSec >= _idleTimeoutSeconds;

            // 2. Foreground application acquisition
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            string appExe = null;
            string title = null;

            if (hwnd != IntPtr.Zero)
            {
                uint pid = 0;
                NativeMethods.GetWindowThreadProcessId(hwnd, out pid);

                // Ignore FocusPulse self-window so inspecting stats never hijacks the session!
                if (pid != 0 && pid == _myPid)
                {
                    lock (_lock)
                    {
                        appExe = _currentWindow.AppName;
                        title = _currentWindow.WindowTitle;
                    }
                }
                else if (pid != 0)
                {
                    string resolved = QueryProcessBinary(pid);
                    if (!string.IsNullOrEmpty(resolved) && 
                        (resolved.IndexOf("FocusPulse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         resolved.IndexOf("ActivityWatch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         resolved.IndexOf("Widget", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        lock (_lock)
                        {
                            appExe = _currentWindow.AppName;
                            title = _currentWindow.WindowTitle;
                        }
                    }
                    else if (!string.IsNullOrEmpty(resolved))
                    {
                        if (string.Equals(resolved, "ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            uint uwpPid = FindUwpChildPid(hwnd, pid);
                            if (uwpPid != 0 && uwpPid != pid && uwpPid != _myPid)
                            {
                                string uwpName = QueryProcessBinary(uwpPid);
                                if (!string.IsNullOrEmpty(uwpName))
                                {
                                    appExe = uwpName;
                                }
                            }
                            if (string.IsNullOrEmpty(appExe))
                            {
                                appExe = resolved;
                            }
                        }
                        else
                        {
                            appExe = resolved;
                        }

                        StringBuilder sbTitle = new StringBuilder(512);
                        NativeMethods.GetWindowText(hwnd, sbTitle, 512);
                        title = sbTitle.ToString().Trim();
                    }
                }
            }

            // Fallback for transient window switches
            lock (_lock)
            {
                if (string.IsNullOrEmpty(appExe))
                {
                    if (!string.IsNullOrEmpty(_currentWindow.AppName) && !_currentWindow.AppName.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                    {
                        appExe = _currentWindow.AppName;
                        title = _currentWindow.WindowTitle;
                    }
                    else
                    {
                        appExe = "Desktop";
                    }
                }
            }

            if (string.IsNullOrEmpty(title))
            {
                title = ResolveFriendlyTitle(appExe);
            }

            ProductivityClass prod = DetermineProductivity(appExe, title);

            lock (_lock)
            {
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                if (_summary.Date != today)
                {
                    _summary = new DaySummary();
                }

                _currentWindow.IsIdle = isIdle;
                _currentWindow.IdleSeconds = idleSec;
                _currentWindow.Productivity = prod;

                int hour = DateTime.Now.Hour;

                if (isIdle)
                {
                    _summary.TotalIdleSeconds += 1.0;

                    if (idleSec >= 120)
                    {
                        _wellnessCoach.ContinuousScreenSeconds = 0;
                        _wellnessCoach.IsBreakRecommended = false;
                        _wellnessCoach.IsDismissed = false;
                    }
                }
                else
                {
                    _summary.TotalActiveSeconds += 1.0;
                    _summary.HourlyActiveSeconds[hour] += 1.0;

                    if (prod == ProductivityClass.FocusWork)
                    {
                        _summary.FocusWorkSeconds += 1.0;
                        _summary.HourlyFocusSeconds[hour] += 1.0;
                    }
                    else if (prod == ProductivityClass.Distraction)
                    {
                        _summary.DistractionSeconds += 1.0;
                    }
                    else
                    {
                        _summary.GeneralSeconds += 1.0;
                    }

                    // Workspace Project Accumulation
                    string proj = string.IsNullOrEmpty(ActiveProjectWorkspace) ? "Development" : ActiveProjectWorkspace;
                    double curProjSec = 0;
                    if (_summary.ProjectDurations.TryGetValue(proj, out curProjSec))
                    {
                        _summary.ProjectDurations[proj] = curProjSec + 1.0;
                    }
                    else
                    {
                        _summary.ProjectDurations[proj] = 1.0;
                    }

                    // Ergonomic Continuous Screen Coach
                    _wellnessCoach.ContinuousScreenSeconds += 1.0;
                    if (_wellnessCoach.ContinuousScreenSeconds >= _wellnessCoach.BreakAlertThresholdSeconds && !_wellnessCoach.IsDismissed)
                    {
                        _wellnessCoach.IsBreakRecommended = true;
                    }

                    // Application Metrics (Ignore FocusPulse and widget itself)
                    if (appExe.IndexOf("FocusPulse", StringComparison.OrdinalIgnoreCase) < 0 &&
                        appExe.IndexOf("ActivityWatch", StringComparison.OrdinalIgnoreCase) < 0 &&
                        appExe.IndexOf("Widget", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        string trackKey = appExe;
                        if (IsBrowserProcess(appExe))
                        {
                            bool isStudy;
                            string domain = ExtractBrowserDomainOrPlatform(title, out isStudy);
                            if (!string.IsNullOrEmpty(domain))
                            {
                                trackKey = appExe + ":" + domain;
                            }
                        }

                        double curSec = 0;
                        if (_summary.AppDurations.TryGetValue(trackKey, out curSec))
                        {
                            _summary.AppDurations[trackKey] = curSec + 1.0;
                        }
                        else
                        {
                            _summary.AppDurations[trackKey] = 1.0;
                        }

                        // Session Switch Logic
                        if (!string.Equals(_currentWindow.AppName, trackKey, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(_currentWindow.WindowTitle, title, StringComparison.OrdinalIgnoreCase))
                        {
                            _summary.WindowSwitches++;
                            _currentWindow.AppName = trackKey;
                            _currentWindow.FriendlyName = ResolveFriendlyTitle(trackKey);
                            _currentWindow.WindowTitle = title;
                            _currentWindow.StartTime = DateTime.Now;
                            _currentWindow.ElapsedSeconds = 1;
                        }
                        else
                        {
                            _currentWindow.ElapsedSeconds += 1.0;
                        }
                    }
                    else
                    {
                        if (!string.Equals(_currentWindow.AppName, appExe, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(_currentWindow.WindowTitle, title, StringComparison.OrdinalIgnoreCase))
                        {
                            _currentWindow.AppName = appExe;
                            _currentWindow.FriendlyName = ResolveFriendlyTitle(appExe);
                            _currentWindow.WindowTitle = title;
                            _currentWindow.StartTime = DateTime.Now;
                            _currentWindow.ElapsedSeconds = 1;
                        }
                        else
                        {
                            _currentWindow.ElapsedSeconds += 1.0;
                        }
                    }

                    // Flow Sprint & Pomodoro Work / Break Interval Transition
                    LastTickDistractionWarning = false;
                    if (_flowSprint.IsActive && !_flowSprint.IsPaused)
                    {
                        _flowSprint.RemainingSeconds--;

                        if (prod == ProductivityClass.Distraction)
                        {
                            _flowSprint.DistractionWarnings++;
                            LastTickDistractionWarning = true;
                        }

                        if (_flowSprint.RemainingSeconds <= 0)
                        {
                            if (_flowSprint.Mode == PomodoroMode.Focus)
                            {
                                // Focus interval complete: increment task pomodoro counter
                                if (!string.IsNullOrEmpty(_flowSprint.LinkedTaskId))
                                {
                                    IncrementTaskPomodoro(_flowSprint.LinkedTaskId);
                                }
                                PlayChime(true);

                                if (_flowSprint.CurrentCycle >= _flowSprint.TargetCycles)
                                {
                                    // 4 cycles complete -> Long Break
                                    _flowSprint.Mode = PomodoroMode.LongBreak;
                                    _flowSprint.TotalDurationSeconds = PomodoroLongBreakMinutes * 60;
                                    _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                                    _flowSprint.SprintLabel = string.Format("🌿 Long Break ({0}m)", PomodoroLongBreakMinutes);
                                }
                                else
                                {
                                    // Short Break
                                    _flowSprint.Mode = PomodoroMode.ShortBreak;
                                    _flowSprint.TotalDurationSeconds = PomodoroBreakMinutes * 60;
                                    _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                                    _flowSprint.SprintLabel = string.Format("☕ Short Break ({0}m)", PomodoroBreakMinutes);
                                }
                            }
                            else if (_flowSprint.Mode == PomodoroMode.ShortBreak)
                            {
                                // Short Break complete -> Advance to next Focus cycle
                                PlayChime(false);
                                _flowSprint.CurrentCycle++;
                                _flowSprint.Mode = PomodoroMode.Focus;
                                _flowSprint.TotalDurationSeconds = PomodoroWorkMinutes * 60;
                                _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                                _flowSprint.SprintLabel = string.Format("{0}m Deep Work", PomodoroWorkMinutes);
                            }
                            else if (_flowSprint.Mode == PomodoroMode.LongBreak)
                            {
                                // Long Break complete -> Reset cycles to 1 and start fresh
                                PlayChime(false);
                                _flowSprint.CurrentCycle = 1;
                                _flowSprint.Mode = PomodoroMode.Focus;
                                _flowSprint.TotalDurationSeconds = PomodoroWorkMinutes * 60;
                                _flowSprint.RemainingSeconds = _flowSprint.TotalDurationSeconds;
                                _flowSprint.SprintLabel = string.Format("{0}m Deep Work", PomodoroWorkMinutes);
                            }
                            else
                            {
                                _flowSprint.IsActive = false;
                                PlayChime(true);
                            }
                        }
                    }
                }
            }
        }

        public static bool IsBrowserProcess(string exeName)
        {
            if (string.IsNullOrEmpty(exeName)) return false;
            string lower = exeName.ToLower();
            if (lower.Contains(":")) lower = lower.Split(':')[0];
            return lower == "chrome.exe" || lower == "msedge.exe" || lower == "firefox.exe" || 
                   lower == "brave.exe" || lower == "opera.exe" || lower == "vivaldi.exe" || 
                   lower == "arc.exe" || lower == "waterfox.exe" || lower == "chromium.exe";
        }

        public static string GetBrowserShortName(string exeName)
        {
            if (string.IsNullOrEmpty(exeName)) return "Browser";
            string lower = exeName.ToLower();
            if (lower.Contains("chrome")) return "Chrome";
            if (lower.Contains("edge")) return "Edge";
            if (lower.Contains("firefox")) return "Firefox";
            if (lower.Contains("brave")) return "Brave";
            if (lower.Contains("opera")) return "Opera";
            if (lower.Contains("vivaldi")) return "Vivaldi";
            if (lower.Contains("arc")) return "Arc";
            return "Web";
        }

        public static string ExtractBrowserDomainOrPlatform(string title, out bool isStudyOrResearch)
        {
            isStudyOrResearch = false;
            if (string.IsNullOrEmpty(title)) return null;
            string lower = title.ToLower();

            bool hasStudyKeywords = 
                lower.Contains("tutorial") || lower.Contains("course") || lower.Contains("lecture") ||
                lower.Contains("study") || lower.Contains("learn") || lower.Contains("how to") ||
                lower.Contains("guide") || lower.Contains("explained") || lower.Contains("walkthrough") ||
                lower.Contains("documentation") || lower.Contains("docs") || lower.Contains("coding") ||
                lower.Contains("programming") || lower.Contains("developer") || lower.Contains("conference") ||
                lower.Contains("paper") || lower.Contains("research") || lower.Contains("academic") ||
                lower.Contains("deep learning") || lower.Contains("machine learning") || lower.Contains("ai") ||
                lower.Contains("algorithm") || lower.Contains("math") || lower.Contains("science") ||
                lower.Contains("physics") || lower.Contains("history") || lower.Contains("university") ||
                lower.Contains("mit") || lower.Contains("stanford") || lower.Contains("harvard") ||
                lower.Contains("khan academy") || lower.Contains("crash course") || lower.Contains("analysis") ||
                lower.Contains("benchmark") || lower.Contains("leetcode") || lower.Contains("interview") ||
                lower.Contains("exam") || lower.Contains("prep") || lower.Contains("class") || lower.Contains("syllabus") ||
                lower.Contains("education") || lower.Contains("curriculum") || lower.Contains("thesis") || lower.Contains("journal");

            if (lower.Contains("youtube"))
            {
                isStudyOrResearch = hasStudyKeywords;
                return hasStudyKeywords ? "YouTube (Study & Research)" : "YouTube";
            }
            if (lower.Contains("reddit") || lower.Contains("r/"))
            {
                isStudyOrResearch = hasStudyKeywords || lower.Contains("r/programming") || lower.Contains("r/learnprogramming") ||
                                    lower.Contains("r/csharp") || lower.Contains("r/dotnet") || lower.Contains("r/datascience") ||
                                    lower.Contains("r/machinelearning") || lower.Contains("r/compsci") || lower.Contains("r/askscience");
                return isStudyOrResearch ? "Reddit (Research & Tech)" : "Reddit";
            }
            if (lower.Contains("twitter") || lower.Contains("x.com") || lower.Contains(" / x") || lower.EndsWith(" / x"))
            {
                isStudyOrResearch = hasStudyKeywords;
                return isStudyOrResearch ? "X / Twitter (Tech & Research)" : "X / Twitter";
            }
            if (lower.Contains("linkedin"))
            {
                isStudyOrResearch = true;
                return "LinkedIn";
            }
            if (lower.Contains("github"))
            {
                isStudyOrResearch = true;
                return "GitHub";
            }
            if (lower.Contains("gitlab"))
            {
                isStudyOrResearch = true;
                return "GitLab";
            }
            if (lower.Contains("stackoverflow") || lower.Contains("stack overflow") || lower.Contains("stackexchange") || lower.Contains("superuser"))
            {
                isStudyOrResearch = true;
                return "Stack Overflow";
            }
            if (lower.Contains("chatgpt") || lower.Contains("openai"))
            {
                isStudyOrResearch = true;
                return "ChatGPT";
            }
            if (lower.Contains("claude"))
            {
                isStudyOrResearch = true;
                return "Claude AI";
            }
            if (lower.Contains("gemini") || lower.Contains("aistudio") || lower.Contains("google ai"))
            {
                isStudyOrResearch = true;
                return "Gemini AI";
            }
            if (lower.Contains("google docs") || lower.Contains("google sheets") || lower.Contains("google slides") || lower.Contains("google drive"))
            {
                isStudyOrResearch = true;
                return "Google Workspace";
            }
            if (lower.Contains("notion"))
            {
                isStudyOrResearch = true;
                return "Notion";
            }
            if (lower.Contains("coursera") || lower.Contains("udemy") || lower.Contains("edx") || lower.Contains("khan academy"))
            {
                isStudyOrResearch = true;
                return "Online Courses";
            }
            if (lower.Contains("wikipedia"))
            {
                isStudyOrResearch = true;
                return "Wikipedia (Research)";
            }
            if (lower.Contains("medium") || lower.Contains("dev.to") || lower.Contains("substack"))
            {
                isStudyOrResearch = true;
                return "Tech Articles";
            }
            if (lower.Contains("google search") || lower.Contains("bing") || lower.Contains("duckduckgo"))
            {
                isStudyOrResearch = hasStudyKeywords;
                return "Search Engine";
            }
            if (lower.Contains("twitch") || lower.Contains("netflix") || lower.Contains("prime video") || lower.Contains("disney+") || lower.Contains("hulu"))
            {
                isStudyOrResearch = false;
                return "Streaming & Entertainment";
            }

            if (hasStudyKeywords)
            {
                isStudyOrResearch = true;
                return "Research & Study";
            }

            return null;
        }

        public static ProductivityClass DetermineProductivity(string appExe, string windowTitle)
        {
            string lowerExe = (appExe ?? "").ToLower();
            string lowerTitle = (windowTitle ?? "").ToLower();

            // Sub-tracked browser site key
            if (lowerExe.Contains(":"))
            {
                string sitePart = lowerExe.Split(':')[1];
                if (sitePart.Contains("study") || sitePart.Contains("research") || sitePart.Contains("github") ||
                    sitePart.Contains("stackoverflow") || sitePart.Contains("chatgpt") || sitePart.Contains("claude") ||
                    sitePart.Contains("gemini") || sitePart.Contains("workspace") || sitePart.Contains("notion") ||
                    sitePart.Contains("course") || sitePart.Contains("wikipedia") || sitePart.Contains("tech") ||
                    sitePart.Contains("linkedin"))
                {
                    return ProductivityClass.FocusWork;
                }
                if (sitePart.Contains("entertainment") || sitePart == "youtube" || sitePart == "reddit" || sitePart.Contains("twitter") || sitePart.Contains("x / twitter"))
                {
                    return ProductivityClass.Distraction;
                }
            }

            // Antigravity & AI Developer Tools
            if (lowerExe.Contains("antigravity") || lowerTitle.Contains("antigravity"))
            {
                return ProductivityClass.FocusWork;
            }

            // Distracting Applications & Games
            if (lowerExe == "steam.exe" || lowerExe == "netflix.exe" || lowerExe == "spotify.exe" ||
                lowerExe == "vlc.exe" || lowerExe.Contains("game") || lowerExe == "epicgameslauncher.exe")
            {
                return ProductivityClass.Distraction;
            }

            // Browsers (Context-aware based on active tab title and site)
            if (IsBrowserProcess(lowerExe))
            {
                bool isStudyOrResearch;
                string domain = ExtractBrowserDomainOrPlatform(windowTitle, out isStudyOrResearch);

                if (isStudyOrResearch)
                {
                    return ProductivityClass.FocusWork;
                }

                if (domain == "Streaming & Entertainment" || domain == "YouTube" || domain == "Reddit" || domain == "X / Twitter")
                {
                    return ProductivityClass.Distraction;
                }

                return ProductivityClass.General;
            }

            // Core Engineering & Deep Focus Tools
            if (lowerExe == "code.exe" || lowerExe == "devenv.exe" || lowerExe == "windowsterminal.exe" ||
                lowerExe == "wt.exe" || lowerExe == "powershell.exe" || lowerExe == "cmd.exe" || lowerExe == "git-bash.exe" ||
                lowerExe == "opencode.exe" || lowerExe == "pycharm64.exe" || lowerExe == "idea64.exe" || lowerExe == "sublime_text.exe" ||
                lowerExe == "notepad++.exe" || lowerExe == "notepad.exe" || lowerExe == "notion.exe" ||
                lowerExe == "obsidian.exe" || lowerExe == "figma.exe" || lowerExe == "excel.exe" ||
                lowerExe == "winword.exe" || lowerExe == "blender.exe" || lowerExe == "cursor.exe" ||
                lowerExe == "windsurf.exe" || lowerExe == "zed.exe" || lowerExe == "desktopeditors.exe" ||
                lowerExe == "postman.exe" || lowerExe == "githubdesktop.exe")
            {
                return ProductivityClass.FocusWork;
            }

            return ProductivityClass.General;
        }

        private static string QueryProcessBinary(uint pid)
        {
            IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProc == IntPtr.Zero) return null;

            try
            {
                StringBuilder sb = new StringBuilder(1024);
                int size = sb.Capacity;
                if (NativeMethods.QueryFullProcessImageName(hProc, 0, sb, ref size))
                {
                    return Path.GetFileName(sb.ToString());
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }
            return null;
        }

        private static uint FindUwpChildPid(IntPtr parentHwnd, uint parentPid)
        {
            uint childPidFound = 0;
            NativeMethods.EnumChildWindows(parentHwnd, delegate(IntPtr childHwnd, IntPtr lParam)
            {
                uint cPid = 0;
                NativeMethods.GetWindowThreadProcessId(childHwnd, out cPid);
                if (cPid != 0 && cPid != parentPid)
                {
                    childPidFound = cPid;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return childPidFound;
        }

        public static string ResolveFriendlyTitle(string appExe)
        {
            if (string.IsNullOrEmpty(appExe)) return "Unknown";

            // If it's a sub-tracked browser site e.g. "chrome.exe:YouTube (Study & Research)"
            if (appExe.Contains(":"))
            {
                string[] parts = appExe.Split(new char[] { ':' }, 2);
                string bName = GetBrowserShortName(parts[0]);
                string site = parts[1];
                return site + " (" + bName + ")";
            }

            string friendly;
            if (KnownApplications.TryGetValue(appExe, out friendly))
            {
                return friendly;
            }

            if (appExe.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Google Antigravity IDE";
            }
            if (appExe.IndexOf("FocusPulse", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "FocusPulse HUD";
            }

            string clean = appExe;
            if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 4);
            }
            if (clean.Length > 0)
            {
                return char.ToUpper(clean[0]) + clean.Substring(1);
            }
            return clean;
        }

        public static string ResolveCategory(string appExe)
        {
            if (string.IsNullOrEmpty(appExe)) return "General";

            if (appExe.Contains(":"))
            {
                string site = appExe.Split(new char[] { ':' }, 2)[1];
                if (site.IndexOf("Study", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Research", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Course", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Wikipedia", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Research & Study";
                }
                if (site.IndexOf("GitHub", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("GitLab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Stack Overflow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("ChatGPT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Claude", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Gemini", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Engineering";
                }
                if (site.IndexOf("Google Workspace", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("Notion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.IndexOf("LinkedIn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Productivity";
                }
                if (site.IndexOf("Entertainment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    site.Equals("YouTube", StringComparison.OrdinalIgnoreCase) ||
                    site.Equals("Reddit", StringComparison.OrdinalIgnoreCase) ||
                    site.IndexOf("Twitter", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Entertainment";
                }
                return "Browsing";
            }

            string cat;
            if (CategoryCatalog.TryGetValue(appExe, out cat))
            {
                return cat;
            }
            if (appExe.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Engineering";
            }
            return "General";
        }

        public List<AppSessionRecord> GetTopApplications(int count = 100, string categoryFilter = "All", string searchFilter = "", string sortOrder = "Duration")
        {
            lock (_lock)
            {
                List<AppSessionRecord> list = new List<AppSessionRecord>();
                double total = _summary.TotalActiveSeconds;
                if (total <= 0) total = 1.0;

                int colorIdx = 0;
                foreach (KeyValuePair<string, double> kvp in _summary.AppDurations)
                {
                    if (kvp.Key.IndexOf("FocusPulse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Key.IndexOf("ActivityWatch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kvp.Key.IndexOf("Widget", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    string friendly = ResolveFriendlyTitle(kvp.Key);
                    string cat = ResolveCategory(kvp.Key);
                    ProductivityClass prod = DetermineProductivity(kvp.Key, "");

                    if (!string.IsNullOrEmpty(categoryFilter) && !categoryFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        if (categoryFilter.Equals("Focus", StringComparison.OrdinalIgnoreCase) && prod != ProductivityClass.FocusWork)
                            continue;
                        if (categoryFilter.Equals("General", StringComparison.OrdinalIgnoreCase) && prod != ProductivityClass.General)
                            continue;
                        if (categoryFilter.Equals("Distraction", StringComparison.OrdinalIgnoreCase) && prod != ProductivityClass.Distraction)
                            continue;
                        if (categoryFilter.Equals("Browsing", StringComparison.OrdinalIgnoreCase) && 
                            !cat.Equals("Browsing", StringComparison.OrdinalIgnoreCase) && 
                            !cat.Equals("Research & Study", StringComparison.OrdinalIgnoreCase) && 
                            !cat.Equals("Entertainment", StringComparison.OrdinalIgnoreCase) && 
                            !kvp.Key.Contains(":"))
                            continue;
                        if (categoryFilter.Equals("Engineering", StringComparison.OrdinalIgnoreCase) && !cat.Equals("Engineering", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (categoryFilter.Equals("Office", StringComparison.OrdinalIgnoreCase) && !cat.Equals("Productivity", StringComparison.OrdinalIgnoreCase) && !cat.Equals("Office", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (categoryFilter.Equals("System", StringComparison.OrdinalIgnoreCase) && !cat.Equals("System", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        if (friendly.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                            kvp.Key.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                            cat.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }
                    }

                    AppSessionRecord rec = new AppSessionRecord();
                    rec.AppName = kvp.Key;
                    rec.FriendlyName = friendly;
                    rec.Category = cat;
                    rec.Productivity = prod;
                    rec.DurationSeconds = kvp.Value;
                    rec.PercentOfTotal = Math.Min(100.0, (kvp.Value / total) * 100.0);
                    rec.ColorHex = FocusPulseStyles.GetSwatch(colorIdx++);
                    rec.IconGlyph = FocusPulseStyles.GetAppGlyph(friendly);
                    list.Add(rec);
                }

                if (string.Equals(sortOrder, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    list.Sort(delegate(AppSessionRecord a, AppSessionRecord b)
                    {
                        return string.Compare(a.FriendlyName, b.FriendlyName, StringComparison.OrdinalIgnoreCase);
                    });
                }
                else
                {
                    list.Sort(delegate(AppSessionRecord a, AppSessionRecord b)
                    {
                        return b.DurationSeconds.CompareTo(a.DurationSeconds);
                    });
                }

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Rank = i + 1;
                }

                if (count > 0 && list.Count > count)
                {
                    return list.GetRange(0, count);
                }
                return list;
            }
        }

        public void SaveMetrics()
        {
            try
            {
                lock (_lock)
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    string json = ser.Serialize(_summary);
                    File.WriteAllText(_storageFilePath, json);

                    _dailyHistory[_summary.Date] = _summary.FocusWorkSeconds;
                    string histJson = ser.Serialize(_dailyHistory);
                    File.WriteAllText(_historyFilePath, histJson);
                }
            }
            catch { }
        }

        public void ResetMetrics()
        {
            lock (_lock)
            {
                _summary = new DaySummary();
                _currentWindow = new ActiveWindowSnapshot();
                _wellnessCoach = new WellnessCoach();
                _flowSprint = new FlowSprintSession();
                SaveMetrics();
            }
        }

        private void LoadMetrics()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    string histJson = File.ReadAllText(_historyFilePath);
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> raw = ser.Deserialize<Dictionary<string, object>>(histJson);
                    if (raw != null)
                    {
                        foreach (KeyValuePair<string, object> kvp in raw)
                        {
                            _dailyHistory[kvp.Key] = Convert.ToDouble(kvp.Value);
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (File.Exists(_storageFilePath))
                {
                    string json = File.ReadAllText(_storageFilePath);
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    Dictionary<string, object> dict = ser.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        string savedDate = dict.ContainsKey("Date") ? Convert.ToString(dict["Date"]) : "";
                        if (savedDate == DateTime.Today.ToString("yyyy-MM-dd"))
                        {
                            _summary = new DaySummary();
                            _summary.Date = savedDate;
                            if (dict.ContainsKey("TotalActiveSeconds"))
                                _summary.TotalActiveSeconds = Convert.ToDouble(dict["TotalActiveSeconds"]);
                            if (dict.ContainsKey("TotalIdleSeconds"))
                                _summary.TotalIdleSeconds = Convert.ToDouble(dict["TotalIdleSeconds"]);
                            if (dict.ContainsKey("FocusWorkSeconds"))
                                _summary.FocusWorkSeconds = Convert.ToDouble(dict["FocusWorkSeconds"]);
                            if (dict.ContainsKey("GeneralSeconds"))
                                _summary.GeneralSeconds = Convert.ToDouble(dict["GeneralSeconds"]);
                            if (dict.ContainsKey("DistractionSeconds"))
                                _summary.DistractionSeconds = Convert.ToDouble(dict["DistractionSeconds"]);
                            if (dict.ContainsKey("WindowSwitches"))
                                _summary.WindowSwitches = Convert.ToInt32(dict["WindowSwitches"]);
                            if (dict.ContainsKey("AppDurations") && dict["AppDurations"] is Dictionary<string, object>)
                            {
                                Dictionary<string, object> appDict = (Dictionary<string, object>)dict["AppDurations"];
                                foreach (KeyValuePair<string, object> kvp in appDict)
                                {
                                    if (kvp.Key.IndexOf("FocusPulse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        kvp.Key.IndexOf("ActivityWatch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        kvp.Key.IndexOf("Widget", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        continue;
                                    }
                                    _summary.AppDurations[kvp.Key] = Convert.ToDouble(kvp.Value);
                                }
                            }
                            return;
                        }
                    }
                }
            }
            catch { }

            _summary = new DaySummary();
        }

        public void LoadTasks()
        {
            try
            {
                if (File.Exists(_tasksFilePath))
                {
                    string json = File.ReadAllText(_tasksFilePath);
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    object[] list = ser.Deserialize<object[]>(json);
                    if (list != null && list.Length > 0)
                    {
                        lock (_lock)
                        {
                            _tasks.Clear();
                            foreach (object obj in list)
                            {
                                if (obj is Dictionary<string, object>)
                                {
                                    Dictionary<string, object> dict = (Dictionary<string, object>)obj;
                                    FocusTaskItem t = new FocusTaskItem();
                                    if (dict.ContainsKey("Id")) t.Id = Convert.ToString(dict["Id"]);
                                    if (dict.ContainsKey("Title")) t.Title = Convert.ToString(dict["Title"]);
                                    if (dict.ContainsKey("IsCompleted")) t.IsCompleted = Convert.ToBoolean(dict["IsCompleted"]);
                                    if (dict.ContainsKey("Priority")) t.Priority = Convert.ToString(dict["Priority"]);
                                    if (dict.ContainsKey("PomodorosCompleted")) t.PomodorosCompleted = Convert.ToInt32(dict["PomodorosCompleted"]);
                                    if (dict.ContainsKey("CreatedDate")) t.CreatedDate = Convert.ToString(dict["CreatedDate"]);
                                    _tasks.Add(t);
                                }
                            }
                        }
                        return;
                    }
                }
            }
            catch { }

            lock (_lock)
            {
                _tasks.Clear();
                _tasks.Add(new FocusTaskItem { Title = "Ship FocusPulse v1.1", Priority = "High", PomodorosCompleted = 2 });
                _tasks.Add(new FocusTaskItem { Title = "Review weekly focus analytics", Priority = "Medium", PomodorosCompleted = 1 });
                _tasks.Add(new FocusTaskItem { Title = "5-minute hydration and posture stretch", Priority = "Low", IsCompleted = true, PomodorosCompleted = 1 });
                SaveTasks();
            }
        }

        public void SaveTasks()
        {
            try
            {
                lock (_lock)
                {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    string json = ser.Serialize(_tasks);
                    File.WriteAllText(_tasksFilePath, json);
                }
            }
            catch { }
        }
    }
}
