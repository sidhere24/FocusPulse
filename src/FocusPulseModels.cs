using System;
using System.Collections.Generic;

namespace FocusPulse
{
    public enum ProductivityClass
    {
        FocusWork,
        General,
        Distraction
    }

    public class AppSessionRecord
    {
        public int Rank { get; set; }
        public string AppName { get; set; }
        public string FriendlyName { get; set; }
        public string Category { get; set; }
        public ProductivityClass Productivity { get; set; }
        public double DurationSeconds { get; set; }
        public double PercentOfTotal { get; set; }
        public string ColorHex { get; set; }
        public string IconGlyph { get; set; }

        public string FormattedDuration
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(DurationSeconds);
                if (t.TotalHours >= 1)
                {
                    return string.Format("{0}h {1}m", (int)t.TotalHours, t.Minutes);
                }
                if (t.TotalMinutes >= 1)
                {
                    return string.Format("{0}m {1}s", (int)t.TotalMinutes, t.Seconds);
                }
                return string.Format("{0}s", (int)t.TotalSeconds);
            }
        }

        public string FormattedPercentage
        {
            get
            {
                return string.Format("{0:F0}%", PercentOfTotal);
            }
        }
    }

    public class DaySummary
    {
        public string Date { get; set; }
        public double TotalActiveSeconds { get; set; }
        public double TotalIdleSeconds { get; set; }
        public double FocusWorkSeconds { get; set; }
        public double GeneralSeconds { get; set; }
        public double DistractionSeconds { get; set; }
        public int WindowSwitches { get; set; }
        public Dictionary<string, double> AppDurations { get; set; }
        public Dictionary<string, double> ProjectDurations { get; set; }
        public double[] HourlyActiveSeconds { get; set; }
        public double[] HourlyFocusSeconds { get; set; }

        public DaySummary()
        {
            Date = DateTime.Today.ToString("yyyy-MM-dd");
            AppDurations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            ProjectDurations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            HourlyActiveSeconds = new double[24];
            HourlyFocusSeconds = new double[24];
        }

        public string FormattedActiveTime
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(TotalActiveSeconds);
                if (t.TotalHours >= 1)
                {
                    return string.Format("{0}h {1}m", (int)t.TotalHours, t.Minutes);
                }
                return string.Format("{0}m", (int)t.TotalMinutes);
            }
        }

        public string FormattedIdleTime
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(TotalIdleSeconds);
                if (t.TotalHours >= 1)
                {
                    return string.Format("{0}h {1}m", (int)t.TotalHours, t.Minutes);
                }
                return string.Format("{0}m", (int)t.TotalMinutes);
            }
        }

        public string FormattedFocusTime
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(FocusWorkSeconds);
                if (t.TotalHours >= 1)
                {
                    return string.Format("{0}h {1}m", (int)t.TotalHours, t.Minutes);
                }
                return string.Format("{0}m", (int)t.TotalMinutes);
            }
        }

        public double FocusRatio
        {
            get
            {
                double total = TotalActiveSeconds + TotalIdleSeconds;
                if (total <= 0) return 100.0;
                return (TotalActiveSeconds / total) * 100.0;
            }
        }

        public int CalculateFocusScore()
        {
            if (TotalActiveSeconds < 45) return 100;

            double ratio = FocusWorkSeconds / Math.Max(1.0, TotalActiveSeconds);
            double score = ratio * 100.0;

            double activeHours = Math.Max(0.2, TotalActiveSeconds / 3600.0);
            double switchesPerHour = WindowSwitches / activeHours;

            if (switchesPerHour > 60)
            {
                score -= Math.Min(25, (switchesPerHour - 60) * 0.4);
            }

            if (DistractionSeconds > 0)
            {
                double distractRatio = DistractionSeconds / TotalActiveSeconds;
                score -= (distractRatio * 30.0);
            }

            int finalScore = (int)Math.Round(score);
            if (finalScore > 100) finalScore = 100;
            if (finalScore < 0) finalScore = 0;
            return finalScore;
        }
    }

    public class FlowSprintSession
    {
        public bool IsActive { get; set; }
        public int TotalDurationSeconds { get; set; }
        public int RemainingSeconds { get; set; }
        public int DistractionWarnings { get; set; }
        public string SprintLabel { get; set; }

        public FlowSprintSession()
        {
            IsActive = false;
            TotalDurationSeconds = 25 * 60;
            RemainingSeconds = TotalDurationSeconds;
            DistractionWarnings = 0;
            SprintLabel = "25m Focus Sprint";
        }

        public string FormattedRemaining
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(RemainingSeconds);
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
        }

        public double ProgressPercent
        {
            get
            {
                if (TotalDurationSeconds <= 0) return 0;
                return Math.Min(100.0, ((TotalDurationSeconds - RemainingSeconds) / (double)TotalDurationSeconds) * 100.0);
            }
        }
    }

    public class WellnessCoach
    {
        public double ContinuousScreenSeconds { get; set; }
        public double BreakAlertThresholdSeconds { get; set; }
        public bool IsBreakRecommended { get; set; }
        public bool IsDismissed { get; set; }

        public WellnessCoach()
        {
            ContinuousScreenSeconds = 0;
            BreakAlertThresholdSeconds = 50 * 60; // 50 minutes ergonomic standard
            IsBreakRecommended = false;
            IsDismissed = false;
        }

        public string FormattedContinuousDuration
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(ContinuousScreenSeconds);
                return string.Format("{0}m", (int)t.TotalMinutes);
            }
        }
    }

    public class ActiveWindowSnapshot
    {
        public string AppName { get; set; }
        public string FriendlyName { get; set; }
        public string WindowTitle { get; set; }
        public ProductivityClass Productivity { get; set; }
        public DateTime StartTime { get; set; }
        public double ElapsedSeconds { get; set; }
        public bool IsIdle { get; set; }
        public double IdleSeconds { get; set; }

        public ActiveWindowSnapshot()
        {
            AppName = "Desktop";
            FriendlyName = "Desktop";
            WindowTitle = "Ready";
            Productivity = ProductivityClass.General;
            StartTime = DateTime.Now;
            ElapsedSeconds = 0;
            IsIdle = false;
            IdleSeconds = 0;
        }

        public string FormattedElapsed
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(ElapsedSeconds);
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)t.TotalHours, t.Minutes, t.Seconds);
            }
        }
    }

    public class PulseConfig
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMiniMode { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool ShowInTaskbar { get; set; }
        public string ThemeName { get; set; }
        public bool IsDarkMode { get; set; }
        public string AccentPalette { get; set; }
        public double Opacity { get; set; }
        public int IdleTimeoutSeconds { get; set; }
        public double DailyGoalHours { get; set; }
        public int BreakReminderMinutes { get; set; }
        public bool IsPrivacyShieldActive { get; set; }
        public string ActiveProjectWorkspace { get; set; }
        public string AppCategoryFilter { get; set; }
        public bool IsCompactAppsView { get; set; }
        public string AppSortOrder { get; set; }

        public PulseConfig()
        {
            Left = -1;
            Top = -1;
            Width = 400;
            Height = 740;
            IsMiniMode = false;
            AlwaysOnTop = false; // Normal desktop window by default
            ShowInTaskbar = true;
            ThemeName = "CleanStudio";
            IsDarkMode = false;
            AccentPalette = "Sapphire";
            Opacity = 0.95;
            IdleTimeoutSeconds = 180;
            DailyGoalHours = 4.0;
            BreakReminderMinutes = 50;
            IsPrivacyShieldActive = false;
            ActiveProjectWorkspace = "Development";
            AppCategoryFilter = "All";
            IsCompactAppsView = false;
            AppSortOrder = "Duration";
        }
    }

    public class ExternalSyncNode
    {
        public string Hostname { get; set; }
        public string ServerVersion { get; set; }
        public bool IsConnected { get; set; }
    }
}
