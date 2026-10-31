using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace FocusPulse
{
    public class MainWindow : Window
    {
        private readonly ActivityEngine _engine;
        private readonly SyncClient _syncClient;
        private readonly DispatcherTimer _uiTimer;
        private Thread _trackingWorkerThread;
        private volatile bool _isTrackingActive = true;

        private readonly Forms.NotifyIcon _notifyIcon;
        private readonly string _configFilePath;
        private PulseConfig _config;
        private SurfaceTheme _theme;

        // UI State
        private bool _isMiniMode = false;
        private bool _isNodeConnected = false;
        private int _uiTickCounter = 0;
        private int _currentTabIndex = 0; // 0: Overview, 1: Focus, 2: Timeline, 3: Settings
        private string _activeCategoryFilter = "All";
        private string _activeSearchQuery = "";

        // Containers
        private Border _mainShellBorder;
        private Grid _fullDashboardGrid;
        private Grid _miniIslandGrid;

        // Tab Views
        private Grid _overviewTabView;
        private Grid _focusTabView;
        private Grid _timelineTabView;
        private Grid _settingsTabView;

        private Border _tabBtnOverview;
        private Border _tabBtnFocus;
        private Border _tabBtnTimeline;
        private Border _tabBtnSettings;
        private TextBlock _tabTxtOverview;
        private TextBlock _tabTxtFocus;
        private TextBlock _tabTxtTimeline;
        private TextBlock _tabTxtSettings;

        // Header controls
        private TextBlock _statusPillText;
        private Border _statusPillBorder;
        private Ellipse _headerLiveDot;
        private TextBlock _pinIconText;
        private TextBlock _pauseIconText;

        // Hero Card (Overview Tab)
        private TextBlock _heroAvatarGlyph;
        private Border _heroAvatarBorder;
        private TextBlock _heroAppName;
        private TextBlock _heroWindowTitle;
        private TextBlock _heroSessionTimer;
        private TextBlock _heroStatusBadge;
        private Border _heroStatusBadgeBorder;
        private TextBlock _heroProductivityBadge;
        private Border _heroProductivityBadgeBorder;

        // Proportional Flow Breakdown Bar
        private ColumnDefinition _flowColFocus;
        private ColumnDefinition _flowColGeneral;
        private ColumnDefinition _flowColDistract;
        private TextBlock _flowLegendFocus;
        private TextBlock _flowLegendGeneral;
        private TextBlock _flowLegendDistract;

        // Metrics Row
        private TextBlock _metricActiveText;
        private TextBlock _metricFocusText;
        private TextBlock _metricIdleText;

        // Applications Section Controls
        private TextBlock _appsCountBadgeText;
        private TextBox _appSearchBox;
        private Border _searchClearBtn;
        private StackPanel _topApplicationsStack;
        private ScrollViewer _appsScrollViewer;
        private StackPanel _filterChipsPanel;
        private Border _btnViewCards;
        private Border _btnViewCompact;
        private TextBlock _txtViewCards;
        private TextBlock _txtViewCompact;
        private Border _btnSortOrder;
        private TextBlock _txtSortOrder;

        // Focus & Pomodoro Interval Controls
        private TextBlock _focusScoreValueText;
        private TextBlock _focusScoreStatusText;
        private TextBlock _velocitySwitchText;
        private TextBlock _sprintCountdownText;
        private TextBlock _sprintSubStatusText;
        private ProgressBar _sprintProgressBar;
        private Border _coachAlertBanner;
        private TextBlock _coachAlertMessageText;
        private TextBlock _dailyTargetSummaryText;
        private ProgressBar _dailyTargetProgressBar;

        private TextBlock _pomodoroModeBadgeText;
        private Border _pomodoroModeBadgeBorder;
        private StackPanel _pomodoroCycleDotsPanel;
        private TextBlock _pomodoroCycleLabelText;
        private Border _pomodoroTargetTaskBorder;
        private TextBlock _pomodoroTargetTaskText;
        private Border _pomodoroClearTargetBtn;
        private TextBlock _pomodoroPlayPauseText;
        private Border _pomodoroPlayPauseBtn;
        private Border _pomodoroSkipBreakBtn;

        // Today's Priorities (Task Checklist) Controls
        private TextBox _taskInputBox;
        private Border _taskPriorityToggleBtn;
        private TextBlock _taskPriorityToggleText;
        private string _taskSelectedPriority = "Medium";
        private TextBlock _tasksSummaryBadgeText;
        private ProgressBar _tasksSummaryProgressBar;
        private StackPanel _tasksContainerStack;

        // Timeline Tab Controls
        private StackPanel _hourlyMatrixStack;
        private TextBlock _workspaceTagText;
        private TextBlock _weeklyStreakBadgeText;
        private Grid _weeklyBarsGrid;

        // Settings Tab Controls
        private TextBlock _settingsWindowModeText;
        private TextBlock _settingsWindowSizeText;
        private TextBlock _settingsActiveThemeText;
        private TextBlock _settingsActiveOpacityText;
        private Border _togglePinSwitch;
        private Border _togglePinKnob;
        private Border _toggleTaskbarSwitch;
        private Border _toggleTaskbarKnob;

        // Footer
        private TextBlock _footerStatusPill;
        private TextBlock _footerSyncIndicator;

        // Mini Island HUD Controls
        private TextBlock _miniAppNameText;
        private TextBlock _miniSessionTimerText;
        private TextBlock _miniDayTotalText;
        private TextBlock _miniScoreBadgeText;
        private TextBlock _miniSprintBadgeText;
        private Ellipse _miniStatusPulseDot;
        private TextBlock _miniAppGlyph;

        public MainWindow()
        {
            _configFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusPulse",
                "pulse_config.json"
            );
            LoadConfiguration();

            if (string.IsNullOrEmpty(_config.AccentPalette))
            {
                SurfaceTheme resolved = FocusPulseStyles.ResolveTheme(_config.ThemeName);
                _config.IsDarkMode = resolved.IsDark;
                _config.AccentPalette = resolved.PaletteKey;
            }
            _theme = FocusPulseStyles.ResolveTheme(_config.IsDarkMode, _config.AccentPalette);
            _config.ThemeName = _theme.Key;
            _activeCategoryFilter = !string.IsNullOrEmpty(_config.AppCategoryFilter) ? _config.AppCategoryFilter : "All";

            _engine = new ActivityEngine(_config.IdleTimeoutSeconds, _config.DailyGoalHours, _config.BreakReminderMinutes);
            _engine.IsPrivacyShield = false;
            _config.IsPrivacyShieldActive = false;
            _engine.ActiveProjectWorkspace = _config.ActiveProjectWorkspace;
            _engine.PomodoroWorkMinutes = _config.PomodoroWorkMinutes > 0 ? _config.PomodoroWorkMinutes : 25;
            _engine.PomodoroBreakMinutes = _config.PomodoroBreakMinutes > 0 ? _config.PomodoroBreakMinutes : 5;
            _engine.PomodoroLongBreakMinutes = _config.PomodoroLongBreakMinutes > 0 ? _config.PomodoroLongBreakMinutes : 15;
            _engine.SoundChimesEnabled = _config.SoundChimesEnabled;

            _syncClient = new SyncClient(5600, 2000);

            InitializeWindowChrome();
            ConstructInterface();

            // Native high-res Tray Icon
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "FocusPulse Companion";
            _notifyIcon.Icon = FocusPulseStyles.CreateFallbackIcon();
            _notifyIcon.Visible = true;
            InitializeTrayMenu();

            // 1. High-Precision Dedicated Win32 Background Worker Thread
            _isTrackingActive = true;
            _trackingWorkerThread = new Thread(new ThreadStart(BackgroundTrackingLoop));
            _trackingWorkerThread.IsBackground = true;
            _trackingWorkerThread.Priority = ThreadPriority.Normal;
            _trackingWorkerThread.Name = "FocusPulseEngineWorker";
            _trackingWorkerThread.Start();

            // 2. High-Priority Smooth UI Refresh Loop
            _uiTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _uiTimer.Interval = TimeSpan.FromSeconds(1);
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            UpdateDisplay();
            QueryExternalSyncNodeAsync();
        }

        private void InitializeWindowChrome()
        {
            this.Title = "FocusPulse";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ResizeMode = _config.IsMiniMode ? ResizeMode.NoResize : ResizeMode.CanResize;
            if (_config.IsMiniMode)
            {
                this.MinWidth = 0;
                this.MinHeight = 0;
                this.MaxWidth = 500;
                this.MaxHeight = 100;
            }
            else
            {
                this.MinWidth = 340;
                this.MinHeight = 520;
                this.MaxWidth = 1000;
                this.MaxHeight = 1400;
            }
            this.ShowInTaskbar = _config.ShowInTaskbar;
            this.FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Inter, -apple-system, sans-serif");

            // Attach multi-res native icon to WPF Window for Alt-Tab / Task Switcher
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FocusPulse.ico");
                if (!System.IO.File.Exists(iconPath))
                {
                    iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src", "FocusPulse.ico");
                }
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = BitmapFrame.Create(new Uri(System.IO.Path.GetFullPath(iconPath)), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                else
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        using (System.Drawing.Icon sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                        {
                            if (sysIcon != null)
                            {
                                using (System.Drawing.Bitmap bmp = sysIcon.ToBitmap())
                                {
                                    IntPtr hBmp = bmp.GetHbitmap();
                                    try
                                    {
                                        this.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                            hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                                    }
                                    finally
                                    {
                                        NativeMethods.DeleteObject(hBmp);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            this.Topmost = _config.AlwaysOnTop;
            this.Opacity = (_config.Opacity >= 0.4 && _config.Opacity <= 1.0) ? _config.Opacity : 0.95;

            ApplyDisplayMode(_config.IsMiniMode);

            if (_config.Left >= 0 && _config.Top >= 0)
            {
                this.Left = _config.Left;
                this.Top = _config.Top;
            }
            else
            {
                Rect workArea = SystemParameters.WorkArea;
                this.Left = workArea.Right - this.Width - 30;
                this.Top = workArea.Top + 40;
            }

            // Keyboard Shortcuts
            this.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (e.Key == Key.D1) { NavigateToTab(0); e.Handled = true; }
                    else if (e.Key == Key.D2) { NavigateToTab(1); e.Handled = true; }
                    else if (e.Key == Key.D3) { NavigateToTab(2); e.Handled = true; }
                    else if (e.Key == Key.D4) { NavigateToTab(3); e.Handled = true; }
                    else if (e.Key == Key.M) { ToggleDisplayMode(); e.Handled = true; }
                    else if (e.Key == Key.P) { ToggleAlwaysOnTop(); e.Handled = true; }
                    else if (e.Key == Key.F && _appSearchBox != null) { _appSearchBox.Focus(); e.Handled = true; }
                }
                else if (e.Key == Key.Escape)
                {
                    this.Hide();
                    e.Handled = true;
                }
            };

            // Drag Window
            this.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is DependencyObject)
                {
                    if (!_isMiniMode)
                    {
                        Point pt = e.GetPosition(this);
                        const int b = 18;
                        if (pt.X <= b || pt.X >= this.ActualWidth - b || pt.Y <= b || pt.Y >= this.ActualHeight - b)
                        {
                            return; // Allow native non-client sizing to take precedence
                        }
                    }

                    // Avoid initiating window drag when clicking on textboxes or interactive controls
                    if (!(e.OriginalSource is TextBox))
                    {
                        this.DragMove();
                    }
                }
            };

            // Native Hardware Accelerated Edge Resizing via Win32 Hook
            this.SourceInitialized += delegate
            {
                try
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;
                    HwndSource source = HwndSource.FromHwnd(hwnd);
                    if (source != null)
                    {
                        source.AddHook(WndProcHook);
                    }

                    // Send high-res Win32 icon directly to HWND for Taskbar and Alt+Tab
                    string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FocusPulse.ico");
                    if (!System.IO.File.Exists(iconPath))
                    {
                        iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src", "FocusPulse.ico");
                    }
                    if (System.IO.File.Exists(iconPath))
                    {
                        using (System.Drawing.Icon bigIcon = new System.Drawing.Icon(iconPath, 48, 48))
                        {
                            NativeMethods.SendMessage(hwnd, (uint)NativeMethods.WM_SETICON, (IntPtr)NativeMethods.ICON_BIG, bigIcon.Handle);
                        }
                        using (System.Drawing.Icon smIcon = new System.Drawing.Icon(iconPath, 16, 16))
                        {
                            NativeMethods.SendMessage(hwnd, (uint)NativeMethods.WM_SETICON, (IntPtr)NativeMethods.ICON_SMALL, smIcon.Handle);
                        }
                    }
                }
                catch { }
            };

            this.SizeChanged += delegate
            {
                if (!_isMiniMode && this.Width > 300 && this.Height > 400)
                {
                    _config.Width = this.Width;
                    _config.Height = this.Height;
                    if (_settingsWindowSizeText != null)
                    {
                        _settingsWindowSizeText.Text = string.Format("Current Dimensions: {0:F0} × {1:F0} px", this.Width, this.Height);
                    }
                }
            };

            this.Closing += delegate
            {
                _isTrackingActive = false;
                SaveConfiguration();
                _engine.SaveMetrics();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            };
        }

        private void ConstructInterface()
        {
            // Apply iOS 18 & Android 15 Slim Floating Capsule Scrollbar style globally
            Style slimScroll = FocusPulseStyles.CreateSlimScrollBarStyle(_theme);
            if (slimScroll != null)
            {
                this.Resources[typeof(ScrollBar)] = slimScroll;
            }

            Grid rootWrapper = new Grid() { Background = Brushes.Transparent };
            this.Content = rootWrapper;

            _mainShellBorder = new Border();
            _mainShellBorder.Background = _theme.CanvasBg;
            _mainShellBorder.BorderBrush = _theme.HairlineBorder;
            _mainShellBorder.BorderThickness = new Thickness(1);
            _mainShellBorder.CornerRadius = new CornerRadius(16);
            _mainShellBorder.Padding = new Thickness(14);
            _mainShellBorder.Margin = new Thickness(10); // Breathing room for soft floating drop shadow

            DropShadowEffect shadow = new DropShadowEffect();
            shadow.Color = Colors.Black;
            shadow.BlurRadius = 24;
            shadow.ShadowDepth = 3;
            shadow.Opacity = _theme.IsDark ? 0.45 : 0.15;
            _mainShellBorder.Effect = shadow;

            Grid rootContainer = new Grid();
            _mainShellBorder.Child = rootContainer;
            rootWrapper.Children.Add(_mainShellBorder);

            _fullDashboardGrid = BuildFullDashboard();
            rootContainer.Children.Add(_fullDashboardGrid);

            _miniIslandGrid = BuildMiniIslandHUD();
            rootContainer.Children.Add(_miniIslandGrid);

            if (_isMiniMode)
            {
                _fullDashboardGrid.Visibility = Visibility.Collapsed;
                _miniIslandGrid.Visibility = Visibility.Visible;
            }
            else
            {
                _fullDashboardGrid.Visibility = Visibility.Visible;
                _miniIslandGrid.Visibility = Visibility.Collapsed;
            }
        }

        public void RebuildUserInterface()
        {
            // Reapply theme-aware slim scrollbar style
            Style slimScroll = FocusPulseStyles.CreateSlimScrollBarStyle(_theme);
            if (slimScroll != null)
            {
                this.Resources[typeof(ScrollBar)] = slimScroll;
            }

            _mainShellBorder.Background = _theme.CanvasBg;
            _mainShellBorder.BorderBrush = _theme.HairlineBorder;

            DropShadowEffect shadow = (DropShadowEffect)_mainShellBorder.Effect;
            if (shadow != null)
            {
                shadow.Opacity = _theme.IsDark ? 0.45 : 0.15;
            }

            Grid rootContainer = (Grid)_mainShellBorder.Child;
            rootContainer.Children.Clear();

            _fullDashboardGrid = BuildFullDashboard();
            rootContainer.Children.Add(_fullDashboardGrid);

            _miniIslandGrid = BuildMiniIslandHUD();
            rootContainer.Children.Add(_miniIslandGrid);

            if (_isMiniMode)
            {
                _fullDashboardGrid.Visibility = Visibility.Collapsed;
                _miniIslandGrid.Visibility = Visibility.Visible;
            }
            else
            {
                _fullDashboardGrid.Visibility = Visibility.Visible;
                _miniIslandGrid.Visibility = Visibility.Collapsed;
            }

            NavigateToTab(_currentTabIndex);
            UpdateDisplay();
        }

        private Grid BuildFullDashboard()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 0: Command Bar
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 1: Segmented Nav
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // 2: Tab Content
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 3: Footer

            // --- Row 0: Command Header ---
            Grid commandBar = new Grid();
            commandBar.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            commandBar.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            commandBar.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            // Brand Mark with Glowing Live Pulse Dot & PRO Tag
            StackPanel brandStack = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            _headerLiveDot = new Ellipse()
            {
                Width = 7,
                Height = 7,
                Fill = _theme.FocusBadgeText,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            brandStack.Children.Add(_headerLiveDot);

            Border brandLogoBorder = new Border()
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(7),
                Background = _theme.BrandAccent,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock brandGlyph = new TextBlock()
            {
                Text = "⚡",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = FocusPulseStyles.Brush("#0F172A"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            brandLogoBorder.Child = brandGlyph;
            brandStack.Children.Add(brandLogoBorder);

            TextBlock brandLabel = new TextBlock()
            {
                Text = "FocusPulse",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            brandStack.Children.Add(brandLabel);

            Border proBadge = new Border()
            {
                Background = _theme.FocusBadgeBg,
                BorderBrush = _theme.FocusBadgeBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 1, 5, 1),
                VerticalAlignment = VerticalAlignment.Center
            };
            proBadge.Child = new TextBlock()
            {
                Text = "PRO",
                FontSize = 7.5,
                FontWeight = FontWeights.ExtraBold,
                Foreground = _theme.FocusBadgeText
            };
            brandStack.Children.Add(proBadge);

            Grid.SetColumn(brandStack, 0);
            commandBar.Children.Add(brandStack);

            // Dynamic Status Badge
            _statusPillBorder = new Border()
            {
                Background = _theme.FocusBadgeBg,
                BorderBrush = _theme.FocusBadgeBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusPillText = new TextBlock()
            {
                Text = "● ACTIVE FLOW",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText
            };
            _statusPillBorder.Child = _statusPillText;
            Grid.SetColumn(_statusPillBorder, 1);
            commandBar.Children.Add(_statusPillBorder);

            // Command Action Cluster
            StackPanel actions = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            Border pauseBtn = CreateMicroIconButton("❚❚", "Pause / Resume Tracking", out _pauseIconText, delegate
            {
                _engine.IsPaused = !_engine.IsPaused;
                _pauseIconText.Text = _engine.IsPaused ? "▶" : "❚❚";
                _pauseIconText.Foreground = _engine.IsPaused ? _theme.AmberBadgeText : _theme.TextSecondary;
            });

            Border pinBtn = CreateMicroIconButton(this.Topmost ? "📌" : "🪟", this.Topmost ? "Always on Top (Click for Normal Window)" : "Normal Window (Behind active apps)", out _pinIconText, delegate
            {
                ToggleAlwaysOnTop();
            });
            _pinIconText.Foreground = this.Topmost ? _theme.AmberBadgeText : _theme.TextMuted;

            TextBlock dummyMode, dummyMin, dummyClose;
            Border modeBtn = CreateMicroIconButton("◩", "Switch to Dynamic Island HUD (Ctrl+M)", out dummyMode, delegate { ToggleDisplayMode(); });
            Border minBtn = CreateMicroIconButton("—", "Hide to System Tray (Esc)", out dummyMin, delegate { this.Hide(); });
            Border closeBtn = CreateMicroIconButton("✕", "Close Application", out dummyClose, delegate { this.Close(); });

            actions.Children.Add(pauseBtn);
            actions.Children.Add(pinBtn);
            actions.Children.Add(modeBtn);
            actions.Children.Add(minBtn);
            actions.Children.Add(closeBtn);
            Grid.SetColumn(actions, 2);
            commandBar.Children.Add(actions);

            Grid.SetRow(commandBar, 0);
            grid.Children.Add(commandBar);

            // --- Row 1: Segmented Navigation Capsule ---
            Border navCapsule = new Border()
            {
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(3),
                Margin = new Thickness(0, 9, 0, 11)
            };
            Grid navGrid = new Grid();
            navGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            _tabBtnOverview = CreateSegmentTab("📊 Overview", true, out _tabTxtOverview, delegate { NavigateToTab(0); });
            _tabBtnFocus = CreateSegmentTab("⏱ Flow", false, out _tabTxtFocus, delegate { NavigateToTab(1); });
            _tabBtnTimeline = CreateSegmentTab("📈 Cadence", false, out _tabTxtTimeline, delegate { NavigateToTab(2); });
            _tabBtnSettings = CreateSegmentTab("⚙️ Settings", false, out _tabTxtSettings, delegate { NavigateToTab(3); });

            Grid.SetColumn(_tabBtnOverview, 0);
            Grid.SetColumn(_tabBtnFocus, 1);
            Grid.SetColumn(_tabBtnTimeline, 2);
            Grid.SetColumn(_tabBtnSettings, 3);

            navGrid.Children.Add(_tabBtnOverview);
            navGrid.Children.Add(_tabBtnFocus);
            navGrid.Children.Add(_tabBtnTimeline);
            navGrid.Children.Add(_tabBtnSettings);
            navCapsule.Child = navGrid;

            Grid.SetRow(navCapsule, 1);
            grid.Children.Add(navCapsule);

            // --- Row 2: Tab Viewport ---
            Grid viewport = new Grid();

            _overviewTabView = BuildOverviewView();
            _focusTabView = BuildFocusView();
            _timelineTabView = BuildTimelineView();
            _settingsTabView = BuildSettingsView();

            viewport.Children.Add(_overviewTabView);
            viewport.Children.Add(_focusTabView);
            viewport.Children.Add(_timelineTabView);
            viewport.Children.Add(_settingsTabView);

            NavigateToTab(_currentTabIndex);

            Grid.SetRow(viewport, 2);
            grid.Children.Add(viewport);

            // --- Row 3: Elegant Footer with Live Metrics & Resize Grip ---
            Grid footerGrid = new Grid() { Margin = new Thickness(0, 10, 0, 0) };
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _footerStatusPill = new TextBlock()
            {
                Text = "🔒 100% Offline & Private",
                FontSize = 9,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "All data is stored strictly on your local PC in %LOCALAPPDATA%\\FocusPulse. No telemetry, no accounts, and zero cloud tracking."
            };
            Grid.SetColumn(_footerStatusPill, 0);
            footerGrid.Children.Add(_footerStatusPill);

            _footerSyncIndicator = new TextBlock()
            {
                Text = "Never Leaves Your PC",
                FontSize = 9,
                Foreground = _theme.TextMuted,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "FocusPulse never sends or uploads logs anywhere. Your privacy is 100% guaranteed."
            };
            Grid.SetColumn(_footerSyncIndicator, 2);
            footerGrid.Children.Add(_footerSyncIndicator);

            TextBlock resizeGrip = new TextBlock()
            {
                Text = "◢",
                FontSize = 11,
                Foreground = _theme.TextMuted,
                Cursor = Cursors.SizeNWSE,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                ToolTip = "Drag to resize window"
            };
            resizeGrip.MouseEnter += delegate { resizeGrip.Foreground = _theme.TextPrimary; };
            resizeGrip.MouseLeave += delegate { resizeGrip.Foreground = _theme.TextMuted; };
            resizeGrip.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(hwnd, unchecked((uint)NativeMethods.WM_NCLBUTTONDOWN), (IntPtr)NativeMethods.HTBOTTOMRIGHT, IntPtr.Zero);
            };
            Grid.SetColumn(resizeGrip, 3);
            footerGrid.Children.Add(resizeGrip);

            Grid.SetRow(footerGrid, 3);
            grid.Children.Add(footerGrid);

            return grid;
        }

        private Grid BuildOverviewView()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 0: Hero Card
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 1: 3 Metric Cards
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // 2: Flow Breakdown Bar
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // 3: Top Apps Section (STAR = Scrollable!)

            // Global fluid mouse wheel routing: scrolling anywhere in Overview scrolls the applications list
            grid.PreviewMouseWheel += delegate(object sender, MouseWheelEventArgs e)
            {
                if (_appsScrollViewer != null)
                {
                    _appsScrollViewer.ScrollToVerticalOffset(_appsScrollViewer.VerticalOffset - (e.Delta * 0.45));
                    e.Handled = true;
                }
            };

            // 0. Hero Window Tile
            Border heroTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _heroAvatarBorder = new Border()
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.HairlineBorder,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 11, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _heroAvatarGlyph = new TextBlock()
            {
                Text = "✦",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.BrandAccent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _heroAvatarBorder.Child = _heroAvatarGlyph;
            Grid.SetColumn(_heroAvatarBorder, 0);
            heroGrid.Children.Add(_heroAvatarBorder);

            StackPanel centerMeta = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            _heroAppName = new TextBlock()
            {
                Text = "Google Antigravity IDE",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _heroWindowTitle = new TextBlock()
            {
                Text = "FocusPulse Workspace",
                FontSize = 10,
                Foreground = _theme.TextSecondary,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 3)
            };

            StackPanel timerPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock timerPrefix = new TextBlock()
            {
                Text = "CURRENT SESSION: ",
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            _heroSessionTimer = new TextBlock()
            {
                Text = "00:00:00",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.AmberBadgeText,
                VerticalAlignment = VerticalAlignment.Center
            };
            timerPanel.Children.Add(timerPrefix);
            timerPanel.Children.Add(_heroSessionTimer);

            centerMeta.Children.Add(_heroAppName);
            centerMeta.Children.Add(_heroWindowTitle);
            centerMeta.Children.Add(timerPanel);
            Grid.SetColumn(centerMeta, 1);
            heroGrid.Children.Add(centerMeta);

            StackPanel badges = new StackPanel() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            _heroProductivityBadgeBorder = new Border()
            {
                Background = _theme.FocusBadgeBg,
                BorderBrush = _theme.FocusBadgeBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 0, 0, 4)
            };
            _heroProductivityBadge = new TextBlock()
            {
                Text = "Deep Focus",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText
            };
            _heroProductivityBadgeBorder.Child = _heroProductivityBadge;
            badges.Children.Add(_heroProductivityBadgeBorder);

            _heroStatusBadgeBorder = new Border()
            {
                Background = _theme.FocusBadgeBg,
                BorderBrush = _theme.FocusBadgeBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 2, 7, 2)
            };
            _heroStatusBadge = new TextBlock()
            {
                Text = "🟢 Active",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.FocusBadgeText
            };
            _heroStatusBadgeBorder.Child = _heroStatusBadge;
            badges.Children.Add(_heroStatusBadgeBorder);

            Grid.SetColumn(badges, 2);
            heroGrid.Children.Add(badges);

            heroTile.Child = heroGrid;
            Grid.SetRow(heroTile, 0);
            grid.Children.Add(heroTile);

            // 1. 3-Column Metric Surface Grid
            Grid metricsRow = new Grid() { Margin = new Thickness(0, 0, 0, 10) };
            metricsRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            metricsRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(6) });
            metricsRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            metricsRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(6) });
            metricsRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Border cardActive = CreateMetricTile("⏱ ACTIVE", "0m", _theme.FocusBadgeText, out _metricActiveText);
            Grid.SetColumn(cardActive, 0);
            metricsRow.Children.Add(cardActive);

            Border cardFocus = CreateMetricTile("🎯 FOCUS", "0m", _theme.BrandAccent, out _metricFocusText);
            Grid.SetColumn(cardFocus, 2);
            metricsRow.Children.Add(cardFocus);

            Border cardIdle = CreateMetricTile("☕ REST / AWAY", "0m", _theme.TextSecondary, out _metricIdleText);
            Grid.SetColumn(cardIdle, 4);
            metricsRow.Children.Add(cardIdle);

            Grid.SetRow(metricsRow, 1);
            grid.Children.Add(metricsRow);

            // 2. Flow Profile Proportional Breakdown
            Border flowBox = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            StackPanel flowStack = new StackPanel();

            Grid flowHeader = new Grid();
            flowHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            flowHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock flowLabel = new TextBlock()
            {
                Text = "FLOW VELOCITY BREAKDOWN",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted
            };
            Grid.SetColumn(flowLabel, 0);
            flowHeader.Children.Add(flowLabel);

            TextBlock flowTag = new TextBlock()
            {
                Text = "Live Proportions",
                FontSize = 8.5,
                Foreground = _theme.BrandAccent
            };
            Grid.SetColumn(flowTag, 1);
            flowHeader.Children.Add(flowTag);
            flowStack.Children.Add(flowHeader);

            Border flowBarContainer = new Border()
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                ClipToBounds = true,
                Background = _theme.SurfaceElevated,
                Margin = new Thickness(0, 6, 0, 6)
            };
            Grid flowSegmentsGrid = new Grid();
            _flowColFocus = new ColumnDefinition() { Width = new GridLength(100, GridUnitType.Star) };
            _flowColGeneral = new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) };
            _flowColDistract = new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) };
            flowSegmentsGrid.ColumnDefinitions.Add(_flowColFocus);
            flowSegmentsGrid.ColumnDefinitions.Add(_flowColGeneral);
            flowSegmentsGrid.ColumnDefinitions.Add(_flowColDistract);

            Rectangle rectFocus = new Rectangle() { Fill = _theme.FocusBadgeText };
            Rectangle rectGeneral = new Rectangle() { Fill = _theme.BrandAccent };
            Rectangle rectDistract = new Rectangle() { Fill = _theme.DistractBadgeText };
            Grid.SetColumn(rectFocus, 0);
            Grid.SetColumn(rectGeneral, 1);
            Grid.SetColumn(rectDistract, 2);

            flowSegmentsGrid.Children.Add(rectFocus);
            flowSegmentsGrid.Children.Add(rectGeneral);
            flowSegmentsGrid.Children.Add(rectDistract);
            flowBarContainer.Child = flowSegmentsGrid;
            flowStack.Children.Add(flowBarContainer);

            StackPanel legendStack = new StackPanel() { Orientation = Orientation.Horizontal };
            _flowLegendFocus = new TextBlock()
            {
                Text = "● 100% Focus",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.FocusBadgeText,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _flowLegendGeneral = new TextBlock()
            {
                Text = "● 0% General",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.BrandAccent,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _flowLegendDistract = new TextBlock()
            {
                Text = "● 0% Distract",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.DistractBadgeText
            };
            legendStack.Children.Add(_flowLegendFocus);
            legendStack.Children.Add(_flowLegendGeneral);
            legendStack.Children.Add(_flowLegendDistract);
            flowStack.Children.Add(legendStack);

            flowBox.Child = flowStack;
            Grid.SetRow(flowBox, 2);
            grid.Children.Add(flowBox);

            // 3. Applications Master Section (Scrollable & Filterable)
            Grid appsMasterGrid = new Grid();
            appsMasterGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Header + Count
            appsMasterGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Search + Category Filter chips
            appsMasterGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable list!

            // Header Row with Title, Live Count, Sort Pill, and Density Capsule
            Grid appsHeaderGrid = new Grid() { Margin = new Thickness(0, 0, 0, 6) };
            appsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            appsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            appsHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            StackPanel titleGroup = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock appsTitle = new TextBlock()
            {
                Text = "APPLICATIONS TODAY",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleGroup.Children.Add(appsTitle);

            // Live App Count Badge
            Border countPill = new Border()
            {
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(7, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _appsCountBadgeText = new TextBlock()
            {
                Text = "0 apps",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.BrandAccent
            };
            countPill.Child = _appsCountBadgeText;
            titleGroup.Children.Add(countPill);

            Grid.SetColumn(titleGroup, 0);
            appsHeaderGrid.Children.Add(titleGroup);

            // Controls Cluster: Sort Pill + Density Capsule
            StackPanel controlsCluster = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Sort Pill Toggle
            _btnSortOrder = new Border()
            {
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            _txtSortOrder = new TextBlock()
            {
                Text = (_config.AppSortOrder == "Name") ? "🔤 Name" : "⚡ Duration",
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextSecondary
            };
            _btnSortOrder.Child = _txtSortOrder;
            _btnSortOrder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _config.AppSortOrder = (_config.AppSortOrder == "Name") ? "Duration" : "Name";
                _txtSortOrder.Text = (_config.AppSortOrder == "Name") ? "🔤 Name" : "⚡ Duration";
                UpdateDisplay();
                SaveConfiguration();
            };
            controlsCluster.Children.Add(_btnSortOrder);

            // Density Capsule: Cards vs Compact
            Border densityCapsule = new Border()
            {
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(2)
            };
            StackPanel capsuleStack = new StackPanel() { Orientation = Orientation.Horizontal };

            _btnViewCards = new Border()
            {
                Background = !_config.IsCompactAppsView ? _theme.SurfaceActiveTab : Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Cursor = Cursors.Hand
            };
            _txtViewCards = new TextBlock()
            {
                Text = "⊞ Cards",
                FontSize = 8,
                FontWeight = !_config.IsCompactAppsView ? FontWeights.Bold : FontWeights.Normal,
                Foreground = !_config.IsCompactAppsView ? _theme.TextPrimary : _theme.TextMuted
            };
            _btnViewCards.Child = _txtViewCards;
            _btnViewCards.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                SetAppsViewDensity(false);
            };

            _btnViewCompact = new Border()
            {
                Background = _config.IsCompactAppsView ? _theme.SurfaceActiveTab : Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Cursor = Cursors.Hand
            };
            _txtViewCompact = new TextBlock()
            {
                Text = "☰ Compact",
                FontSize = 8,
                FontWeight = _config.IsCompactAppsView ? FontWeights.Bold : FontWeights.Normal,
                Foreground = _config.IsCompactAppsView ? _theme.TextPrimary : _theme.TextMuted
            };
            _btnViewCompact.Child = _txtViewCompact;
            _btnViewCompact.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                SetAppsViewDensity(true);
            };

            capsuleStack.Children.Add(_btnViewCards);
            capsuleStack.Children.Add(_btnViewCompact);
            densityCapsule.Child = capsuleStack;
            controlsCluster.Children.Add(densityCapsule);

            Grid.SetColumn(controlsCluster, 2);
            appsHeaderGrid.Children.Add(controlsCluster);

            Grid.SetRow(appsHeaderGrid, 0);
            appsMasterGrid.Children.Add(appsHeaderGrid);

            // Filter & Search Controls Row
            StackPanel filterStack = new StackPanel() { Margin = new Thickness(0, 0, 0, 6) };

            // Raycast / Spotlight Style Instant Search Bar
            Border searchBarBorder = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            System.Windows.Shapes.Path searchIcon = new System.Windows.Shapes.Path()
            {
                Data = Geometry.Parse("M6.5,1 A5.5,5.5 0 1,0 10.3,10.3 L13.5,13.5 A0.8,0.8 0 0,0 14.6,12.4 L11.4,9.2 A5.5,5.5 0 0,0 6.5,1 Z M6.5,2.5 A4,4 0 1,1 2.5,6.5 A4,4 0 0,1 6.5,2.5 Z"),
                Fill = _theme.TextMuted,
                Width = 11,
                Height = 11,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            _appSearchBox = new TextBox()
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = _theme.TextPrimary,
                FontSize = 9.5,
                CaretBrush = _theme.BrandAccent,
                VerticalAlignment = VerticalAlignment.Center
            };
            _appSearchBox.TextChanged += delegate
            {
                _activeSearchQuery = _appSearchBox.Text.Trim();
                if (_searchClearBtn != null)
                {
                    _searchClearBtn.Visibility = string.IsNullOrEmpty(_activeSearchQuery) ? Visibility.Collapsed : Visibility.Visible;
                }
                UpdateDisplay();
            };
            Grid.SetColumn(_appSearchBox, 1);
            searchGrid.Children.Add(_appSearchBox);

            _searchClearBtn = new Border()
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = _theme.SurfaceElevated,
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            TextBlock clearGlyph = new TextBlock()
            {
                Text = "✕",
                FontSize = 7.5,
                Foreground = _theme.TextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _searchClearBtn.Child = clearGlyph;
            _searchClearBtn.MouseLeftButtonDown += delegate
            {
                _appSearchBox.Text = "";
                _appSearchBox.Focus();
            };
            Grid.SetColumn(_searchClearBtn, 2);
            searchGrid.Children.Add(_searchClearBtn);

            searchBarBorder.Child = searchGrid;
            filterStack.Children.Add(searchBarBorder);

            // Quick Category Filter Chips - Single-Row Horizontally Scrollable Capsule Bar
            ScrollViewer chipScrollViewer = new ScrollViewer()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Focusable = false,
                Margin = new Thickness(0, 0, 0, 4)
            };
            chipScrollViewer.PreviewMouseWheel += delegate(object sender, MouseWheelEventArgs e)
            {
                chipScrollViewer.ScrollToHorizontalOffset(chipScrollViewer.HorizontalOffset - (e.Delta * 0.45));
                e.Handled = true;
            };
            _filterChipsPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            chipScrollViewer.Content = _filterChipsPanel;
            RebuildFilterChips();
            filterStack.Children.Add(chipScrollViewer);

            Grid.SetRow(filterStack, 1);
            appsMasterGrid.Children.Add(filterStack);

            // Dedicated Smooth ScrollViewer for Applications List with Custom Slim ScrollBar
            _appsScrollViewer = new ScrollViewer()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 3, 0),
                Focusable = false,
                CanContentScroll = false,
                PanningMode = PanningMode.VerticalOnly
            };
            Style slimScroll = FocusPulseStyles.CreateSlimScrollBarStyle(_theme);
            if (slimScroll != null)
            {
                _appsScrollViewer.Resources[typeof(ScrollBar)] = slimScroll;
            }
            _topApplicationsStack = new StackPanel();
            _appsScrollViewer.Content = _topApplicationsStack;

            Grid.SetRow(_appsScrollViewer, 2);
            appsMasterGrid.Children.Add(_appsScrollViewer);

            Grid.SetRow(appsMasterGrid, 3);
            grid.Children.Add(appsMasterGrid);

            return grid;
        }

        private void RebuildFilterChips()
        {
            if (_filterChipsPanel == null) return;
            _filterChipsPanel.Children.Clear();

            string[] chips = new string[] { "All", "Focus", "Browsing", "Engineering", "Office", "System", "Distraction" };
            foreach (string cat in chips)
            {
                bool isSelected = cat.Equals(_activeCategoryFilter, StringComparison.OrdinalIgnoreCase);
                Border chip = new Border()
                {
                    Background = isSelected ? _theme.BrandAccent : _theme.SurfaceElevated,
                    BorderBrush = isSelected ? _theme.BrandAccent : _theme.SubtleBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 3, 10, 3),
                    Margin = new Thickness(0, 0, 5, 0),
                    Cursor = Cursors.Hand
                };
                TextBlock txt = new TextBlock()
                {
                    Text = cat,
                    FontSize = 8.5,
                    FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = isSelected ? (_theme.IsDark ? FocusPulseStyles.Brush("#000000") : FocusPulseStyles.Brush("#FFFFFF")) : _theme.TextSecondary
                };
                chip.Child = txt;

                string capture = cat;
                chip.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _activeCategoryFilter = capture;
                    _config.AppCategoryFilter = capture;
                    RebuildFilterChips();
                    UpdateDisplay();
                    SaveConfiguration();
                };

                _filterChipsPanel.Children.Add(chip);
            }
        }

        private Grid BuildFocusView()
        {
            Grid root = new Grid();
            ScrollViewer scroll = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Style slimScroll = FocusPulseStyles.CreateSlimScrollBarStyle(_theme);
            if (slimScroll != null)
            {
                scroll.Resources[typeof(ScrollBar)] = slimScroll;
            }

            StackPanel panel = new StackPanel();

            // ==========================================
            // 1. Pomodoro Work / Break Interval Hero Card
            // ==========================================
            Border pomoHeroCard = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel pomoStack = new StackPanel();

            // Mode & Cycle Header
            Grid pomoHeader = new Grid();
            pomoHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            pomoHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            pomoHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _pomodoroModeBadgeBorder = new Border()
            {
                CornerRadius = new CornerRadius(5),
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            _pomodoroModeBadgeText = new TextBlock()
            {
                Text = "🎯 READY TO FOCUS",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.BrandAccent
            };
            _pomodoroModeBadgeBorder.Child = _pomodoroModeBadgeText;
            Grid.SetColumn(_pomodoroModeBadgeBorder, 0);
            pomoHeader.Children.Add(_pomodoroModeBadgeBorder);

            StackPanel cycleCluster = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _pomodoroCycleDotsPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            cycleCluster.Children.Add(_pomodoroCycleDotsPanel);

            _pomodoroCycleLabelText = new TextBlock()
            {
                Text = "Cycle 1 of 4",
                FontSize = 8.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            cycleCluster.Children.Add(_pomodoroCycleLabelText);
            Grid.SetColumn(cycleCluster, 2);
            pomoHeader.Children.Add(cycleCluster);

            pomoStack.Children.Add(pomoHeader);

            // Big Digital Countdown
            _sprintCountdownText = new TextBlock()
            {
                Text = "25:00",
                FontSize = 44,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Consolas, monospace"),
                Foreground = _theme.TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 4)
            };
            pomoStack.Children.Add(_sprintCountdownText);

            // Active Target Task Banner
            _pomodoroTargetTaskBorder = new Border()
            {
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid targetGrid = new Grid();
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            targetGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            _pomodoroTargetTaskText = new TextBlock()
            {
                Text = "🎯 No task target linked • Click 🎯 on any task below",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_pomodoroTargetTaskText, 0);
            targetGrid.Children.Add(_pomodoroTargetTaskText);

            _pomodoroClearTargetBtn = new Border()
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(3),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock clearTb = new TextBlock()
            {
                Text = "✕",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _pomodoroClearTargetBtn.Child = clearTb;
            _pomodoroClearTargetBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _engine.ClearPomodoroTargetTask();
                RebuildTasksList();
                UpdateDisplay();
            };
            Grid.SetColumn(_pomodoroClearTargetBtn, 1);
            targetGrid.Children.Add(_pomodoroClearTargetBtn);

            _pomodoroTargetTaskBorder.Child = targetGrid;
            pomoStack.Children.Add(_pomodoroTargetTaskBorder);

            // Progress bar
            _sprintProgressBar = new ProgressBar()
            {
                Height = 5,
                Value = 0,
                Foreground = _theme.BrandAccent,
                Background = _theme.SurfaceElevated,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 6)
            };
            pomoStack.Children.Add(_sprintProgressBar);

            _sprintSubStatusText = new TextBlock()
            {
                Text = "Ready for an uninterrupted sprint",
                FontSize = 9.5,
                Foreground = _theme.TextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            pomoStack.Children.Add(_sprintSubStatusText);

            // Primary Control Buttons Row
            StackPanel pomoControls = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };

            // Play / Pause Button
            _pomodoroPlayPauseBtn = new Border()
            {
                CornerRadius = new CornerRadius(10),
                Background = _theme.BrandAccent,
                Padding = new Thickness(18, 7, 18, 7),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            _pomodoroPlayPauseText = new TextBlock()
            {
                Text = "▶ Start",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = FocusPulseStyles.Brush("#FFFFFF"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _pomodoroPlayPauseBtn.Child = _pomodoroPlayPauseText;
            _pomodoroPlayPauseBtn.MouseEnter += delegate { _pomodoroPlayPauseBtn.Background = _theme.BrandAccentHover; };
            _pomodoroPlayPauseBtn.MouseLeave += delegate { _pomodoroPlayPauseBtn.Background = _theme.BrandAccent; };
            _pomodoroPlayPauseBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _engine.TogglePomodoroPause();
                UpdateDisplay();
            };
            pomoControls.Children.Add(_pomodoroPlayPauseBtn);

            // Skip Break Button
            _pomodoroSkipBreakBtn = new Border()
            {
                CornerRadius = new CornerRadius(10),
                Background = _theme.AmberBadgeBg,
                BorderBrush = _theme.AmberBadgeText,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 7, 14, 7),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed
            };
            TextBlock skipTb = new TextBlock()
            {
                Text = "⏭ Skip Break",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.AmberBadgeText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _pomodoroSkipBreakBtn.Child = skipTb;
            _pomodoroSkipBreakBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                _engine.SkipPomodoroBreak();
                UpdateDisplay();
            };
            pomoControls.Children.Add(_pomodoroSkipBreakBtn);

            // Reset Button
            Border resetBtn = CreateInteractivePill("↺ Reset", delegate
            {
                _engine.ResetPomodoro();
                UpdateDisplay();
            });
            pomoControls.Children.Add(resetBtn);

            pomoStack.Children.Add(pomoControls);

            // Presets row
            StackPanel presetsRow = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            presetsRow.Children.Add(CreateInteractivePill("25m Focus", delegate { _engine.StartPomodoro(25, 5, 15); UpdateDisplay(); }));
            presetsRow.Children.Add(CreateInteractivePill("50m Deep", delegate { _engine.StartPomodoro(50, 10, 20); UpdateDisplay(); }));
            presetsRow.Children.Add(CreateInteractivePill("5m Break", delegate { _engine.StartPomodoro(5, 5, 15); UpdateDisplay(); }));
            presetsRow.Children.Add(CreateInteractivePill("15m Long", delegate { _engine.StartPomodoro(15, 5, 15); UpdateDisplay(); }));
            pomoStack.Children.Add(presetsRow);

            pomoHeroCard.Child = pomoStack;
            panel.Children.Add(pomoHeroCard);

            // ==========================================
            // 2. Today's Priorities & Task Checklist
            // ==========================================
            Border tasksCard = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel tasksStack = new StackPanel();

            // Header
            Grid tasksHeader = new Grid();
            tasksHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            tasksHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock tasksTitle = new TextBlock()
            {
                Text = "TODAY'S PRIORITIES & FOCUS QUEUE",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tasksTitle, 0);
            tasksHeader.Children.Add(tasksTitle);

            _tasksSummaryBadgeText = new TextBlock()
            {
                Text = "0 Tasks",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.BrandAccent,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_tasksSummaryBadgeText, 1);
            tasksHeader.Children.Add(_tasksSummaryBadgeText);
            tasksStack.Children.Add(tasksHeader);

            _tasksSummaryProgressBar = new ProgressBar()
            {
                Height = 3,
                Value = 0,
                Foreground = _theme.BrandAccent,
                Background = _theme.SurfaceElevated,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 4, 0, 10)
            };
            tasksStack.Children.Add(_tasksSummaryProgressBar);

            // Quick Add Task Row
            Grid quickAddGrid = new Grid() { Margin = new Thickness(0, 0, 0, 8) };
            quickAddGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            quickAddGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            quickAddGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            Border inputBorder = new Border()
            {
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 6, 0)
            };
            _taskInputBox = new TextBox()
            {
                Text = "Add a focus task...",
                Foreground = _theme.TextMuted,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            _taskInputBox.GotFocus += delegate
            {
                if (_taskInputBox.Text == "Add a focus task...")
                {
                    _taskInputBox.Text = "";
                    _taskInputBox.Foreground = _theme.TextPrimary;
                }
            };
            _taskInputBox.LostFocus += delegate
            {
                if (string.IsNullOrWhiteSpace(_taskInputBox.Text))
                {
                    _taskInputBox.Text = "Add a focus task...";
                    _taskInputBox.Foreground = _theme.TextMuted;
                }
            };
            _taskInputBox.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    SubmitNewTask();
                }
            };
            inputBorder.Child = _taskInputBox;
            Grid.SetColumn(inputBorder, 0);
            quickAddGrid.Children.Add(inputBorder);

            // Priority toggle button
            _taskPriorityToggleBtn = new Border()
            {
                CornerRadius = new CornerRadius(6),
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            _taskPriorityToggleText = new TextBlock()
            {
                Text = "🟡 Med",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.AmberBadgeText
            };
            _taskPriorityToggleBtn.Child = _taskPriorityToggleText;
            _taskPriorityToggleBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                CycleTaskPriority();
            };
            Grid.SetColumn(_taskPriorityToggleBtn, 1);
            quickAddGrid.Children.Add(_taskPriorityToggleBtn);

            // Add Button
            Border addBtn = new Border()
            {
                CornerRadius = new CornerRadius(6),
                Background = _theme.BrandAccent,
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock addTb = new TextBlock()
            {
                Text = "+ Add",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = FocusPulseStyles.Brush("#FFFFFF"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            addBtn.Child = addTb;
            addBtn.MouseEnter += delegate { addBtn.Background = _theme.BrandAccentHover; };
            addBtn.MouseLeave += delegate { addBtn.Background = _theme.BrandAccent; };
            addBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                SubmitNewTask();
            };
            Grid.SetColumn(addBtn, 2);
            quickAddGrid.Children.Add(addBtn);

            tasksStack.Children.Add(quickAddGrid);

            // Tasks List
            _tasksContainerStack = new StackPanel();
            tasksStack.Children.Add(_tasksContainerStack);

            tasksCard.Child = tasksStack;
            panel.Children.Add(tasksCard);

            // ==========================================
            // 3. Focus Score & Velocity Card
            // ==========================================
            Border scoreTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid scoreGrid = new Grid();
            scoreGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            scoreGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Border scoreBadge = new Border()
            {
                Width = 52,
                Height = 52,
                CornerRadius = new CornerRadius(26),
                Background = _theme.FocusBadgeBg,
                BorderBrush = _theme.FocusBadgeBorder,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 12, 0)
            };
            _focusScoreValueText = new TextBlock()
            {
                Text = "95%",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            scoreBadge.Child = _focusScoreValueText;
            Grid.SetColumn(scoreBadge, 0);
            scoreGrid.Children.Add(scoreBadge);

            StackPanel scoreInfo = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            TextBlock scoreHeader = new TextBlock()
            {
                Text = "DAILY FOCUS FLOW SCORE",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted
            };
            _focusScoreStatusText = new TextBlock()
            {
                Text = "🏆 Deep Focus Flow",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 2, 0, 2)
            };
            _velocitySwitchText = new TextBlock()
            {
                Text = "Switching Rate: Calm & Focused",
                FontSize = 9.5,
                Foreground = _theme.TextSecondary
            };
            scoreInfo.Children.Add(scoreHeader);
            scoreInfo.Children.Add(_focusScoreStatusText);
            scoreInfo.Children.Add(_velocitySwitchText);
            Grid.SetColumn(scoreInfo, 1);
            scoreGrid.Children.Add(scoreInfo);

            scoreTile.Child = scoreGrid;
            panel.Children.Add(scoreTile);

            // ==========================================
            // 4. Daily Target Card
            // ==========================================
            Border targetCard = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel targetStack = new StackPanel();

            Grid targetHeader = new Grid();
            targetHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            targetHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock targetTitle = new TextBlock()
            {
                Text = "DAILY FOCUS TARGET",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted
            };
            Grid.SetColumn(targetTitle, 0);
            targetHeader.Children.Add(targetTitle);

            _dailyTargetSummaryText = new TextBlock()
            {
                Text = "0% (0h / 4h)",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText
            };
            Grid.SetColumn(_dailyTargetSummaryText, 1);
            targetHeader.Children.Add(_dailyTargetSummaryText);
            targetStack.Children.Add(targetHeader);

            _dailyTargetProgressBar = new ProgressBar()
            {
                Height = 5,
                Value = 0,
                Foreground = _theme.FocusBadgeText,
                Background = _theme.SurfaceElevated,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 4, 0, 0)
            };
            targetStack.Children.Add(_dailyTargetProgressBar);
            targetCard.Child = targetStack;
            panel.Children.Add(targetCard);

            // ==========================================
            // 5. Ergonomic Wellness Banner
            // ==========================================
            _coachAlertBanner = new Border()
            {
                Background = _theme.VioletBadgeBg,
                BorderBrush = _theme.VioletBadgeText,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };
            Grid coachGrid = new Grid();
            coachGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            coachGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            _coachAlertMessageText = new TextBlock()
            {
                Text = "🧘 Healthy Pacing: Continuous work 50 mins. Time for a quick stretch!",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.VioletBadgeText,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_coachAlertMessageText, 0);
            coachGrid.Children.Add(_coachAlertMessageText);

            StackPanel coachActions = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            coachActions.Children.Add(CreateInteractivePill("Dismiss", delegate { _engine.DismissBreakPrompt(); }));
            coachActions.Children.Add(CreateInteractivePill("Snooze 10m", delegate { _engine.SnoozeBreakPrompt(10); }));
            Grid.SetRow(coachActions, 1);
            coachGrid.Children.Add(coachActions);

            _coachAlertBanner.Child = coachGrid;
            panel.Children.Add(_coachAlertBanner);

            scroll.Content = panel;
            root.Children.Add(scroll);

            RebuildTasksList();

            return root;
        }

        private void SubmitNewTask()
        {
            if (_taskInputBox == null) return;
            string txt = _taskInputBox.Text != null ? _taskInputBox.Text.Trim() : "";
            if (string.IsNullOrEmpty(txt) || txt == "Add a focus task...") return;

            _engine.AddTask(txt, _taskSelectedPriority);
            _taskInputBox.Text = "";
            RebuildTasksList();
        }

        private void CycleTaskPriority()
        {
            if (_taskSelectedPriority == "High")
            {
                _taskSelectedPriority = "Low";
                if (_taskPriorityToggleText != null)
                {
                    _taskPriorityToggleText.Text = "🟢 Low";
                    _taskPriorityToggleText.Foreground = _theme.FocusBadgeText;
                }
            }
            else if (_taskSelectedPriority == "Low")
            {
                _taskSelectedPriority = "Medium";
                if (_taskPriorityToggleText != null)
                {
                    _taskPriorityToggleText.Text = "🟡 Med";
                    _taskPriorityToggleText.Foreground = _theme.AmberBadgeText;
                }
            }
            else
            {
                _taskSelectedPriority = "High";
                if (_taskPriorityToggleText != null)
                {
                    _taskPriorityToggleText.Text = "🔴 High";
                    _taskPriorityToggleText.Foreground = _theme.DistractBadgeText;
                }
            }
        }

        private void RebuildTasksList()
        {
            if (_tasksContainerStack == null) return;
            _tasksContainerStack.Children.Clear();

            List<FocusTaskItem> tasks = _engine.Tasks;
            int total = tasks.Count;
            int completed = 0;
            for (int i = 0; i < total; i++)
            {
                if (tasks[i].IsCompleted) completed++;
            }

            if (_tasksSummaryBadgeText != null)
            {
                if (total == 0)
                {
                    _tasksSummaryBadgeText.Text = "0 Tasks";
                }
                else
                {
                    double pct = (completed / (double)total) * 100.0;
                    _tasksSummaryBadgeText.Text = string.Format("{0}/{1} Done ({2:F0}%)", completed, total, pct);
                }
            }

            if (_tasksSummaryProgressBar != null)
            {
                _tasksSummaryProgressBar.Value = (total > 0) ? ((completed / (double)total) * 100.0) : 0;
            }

            if (total == 0)
            {
                Border emptyCard = new Border()
                {
                    Background = _theme.SurfaceElevated,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14),
                    Margin = new Thickness(0, 4, 0, 4)
                };
                TextBlock emptyMsg = new TextBlock()
                {
                    Text = "✨ All caught up! Add your next focus priority above.",
                    FontSize = 9.5,
                    Foreground = _theme.TextMuted,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyCard.Child = emptyMsg;
                _tasksContainerStack.Children.Add(emptyCard);
                return;
            }

            string currentLinkedId = _engine.ActiveSprint.LinkedTaskId;

            for (int i = 0; i < total; i++)
            {
                FocusTaskItem item = tasks[i];
                string taskId = item.Id;
                string taskTitle = item.Title;
                bool isLinked = !string.IsNullOrEmpty(currentLinkedId) && currentLinkedId == taskId;

                Border row = new Border()
                {
                    Background = isLinked ? _theme.SurfaceActiveTab : _theme.SurfaceElevated,
                    BorderBrush = isLinked ? _theme.BrandAccent : _theme.SubtleBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 5),
                    Opacity = item.IsCompleted ? 0.6 : 1.0
                };

                Grid rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Checkbox
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // Title & Priority
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Pomo badge
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Focus target button
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Delete

                // 1. Checkbox
                Border chk = new Border()
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(4),
                    Background = item.IsCompleted ? _theme.BrandAccent : _theme.CanvasBg,
                    BorderBrush = item.IsCompleted ? _theme.BrandAccent : _theme.TextMuted,
                    BorderThickness = new Thickness(1.5),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock chkGlyph = new TextBlock()
                {
                    Text = item.IsCompleted ? "✓" : "",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = FocusPulseStyles.Brush("#FFFFFF"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                chk.Child = chkGlyph;
                chk.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _engine.ToggleTask(taskId);
                    RebuildTasksList();
                };
                Grid.SetColumn(chk, 0);
                rowGrid.Children.Add(chk);

                // 2. Title & Priority Pill
                StackPanel titleStack = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                TextBlock titleTb = new TextBlock()
                {
                    Text = item.Title,
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = item.IsCompleted ? _theme.TextMuted : _theme.TextPrimary,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 135,
                    VerticalAlignment = VerticalAlignment.Center
                };
                if (item.IsCompleted)
                {
                    titleTb.TextDecorations = TextDecorations.Strikethrough;
                }
                titleStack.Children.Add(titleTb);

                // Priority Badge
                string prio = item.Priority ?? "Medium";
                Border prioPill = new Border()
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock prioTb = new TextBlock()
                {
                    FontSize = 7.5,
                    FontWeight = FontWeights.Bold
                };
                if (prio.Equals("High", StringComparison.OrdinalIgnoreCase))
                {
                    prioPill.Background = _theme.DistractBadgeBg;
                    prioTb.Text = "HIGH";
                    prioTb.Foreground = _theme.DistractBadgeText;
                }
                else if (prio.Equals("Low", StringComparison.OrdinalIgnoreCase))
                {
                    prioPill.Background = _theme.FocusBadgeBg;
                    prioTb.Text = "LOW";
                    prioTb.Foreground = _theme.FocusBadgeText;
                }
                else
                {
                    prioPill.Background = _theme.AmberBadgeBg;
                    prioTb.Text = "MED";
                    prioTb.Foreground = _theme.AmberBadgeText;
                }
                prioPill.Child = prioTb;
                titleStack.Children.Add(prioPill);
                Grid.SetColumn(titleStack, 1);
                rowGrid.Children.Add(titleStack);

                // 3. Pomodoro count badge
                Border pomoBadge = new Border()
                {
                    Background = _theme.SurfaceCard,
                    BorderBrush = _theme.SubtleBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock pomoTb = new TextBlock()
                {
                    Text = string.Format("🍅 {0}", item.PomodorosCompleted),
                    FontSize = 8.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = _theme.TextPrimary
                };
                pomoBadge.Child = pomoTb;
                Grid.SetColumn(pomoBadge, 2);
                rowGrid.Children.Add(pomoBadge);

                // 4. Focus target action button
                Border targetBtn = new Border()
                {
                    CornerRadius = new CornerRadius(4),
                    Background = isLinked ? _theme.BrandAccent : _theme.SurfaceCard,
                    BorderBrush = isLinked ? _theme.BrandAccent : _theme.SubtleBorder,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(0, 0, 5, 0),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock targetTb = new TextBlock()
                {
                    Text = isLinked ? "🎯 Active" : "🎯 Focus",
                    FontSize = 8.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = isLinked ? FocusPulseStyles.Brush("#FFFFFF") : _theme.TextSecondary
                };
                targetBtn.Child = targetTb;
                targetBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _engine.SetPomodoroTargetTask(taskId, taskTitle);
                    if (!_engine.ActiveSprint.IsActive)
                    {
                        _engine.StartPomodoro(_config.PomodoroWorkMinutes, _config.PomodoroBreakMinutes, _config.PomodoroLongBreakMinutes, taskId, taskTitle);
                    }
                    RebuildTasksList();
                    UpdateDisplay();
                };
                Grid.SetColumn(targetBtn, 3);
                rowGrid.Children.Add(targetBtn);

                // 5. Delete action button
                Border delBtn = new Border()
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(4),
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock delTb = new TextBlock()
                {
                    Text = "✕",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = _theme.TextMuted,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                delBtn.Child = delTb;
                delBtn.MouseEnter += delegate { delTb.Foreground = _theme.DistractBadgeText; };
                delBtn.MouseLeave += delegate { delTb.Foreground = _theme.TextMuted; };
                delBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    _engine.DeleteTask(taskId);
                    RebuildTasksList();
                    UpdateDisplay();
                };
                Grid.SetColumn(delBtn, 4);
                rowGrid.Children.Add(delBtn);

                row.Child = rowGrid;
                _tasksContainerStack.Children.Add(row);
            }
        }

        private Grid BuildTimelineView()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Workspace tag selector
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto }); // Weekly consistency & streak card
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Hourly Matrix

            Border workspaceTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid wsGrid = new Grid();
            wsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            wsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            wsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock wsLabel = new TextBlock()
            {
                Text = "🏷 ACTIVE WORKSPACE: ",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(wsLabel, 0);
            wsGrid.Children.Add(wsLabel);

            _workspaceTagText = new TextBlock()
            {
                Text = _engine.ActiveProjectWorkspace,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.AmberBadgeText,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_workspaceTagText, 1);
            wsGrid.Children.Add(_workspaceTagText);

            Border cycleBtn = CreateInteractivePill("Switch", delegate { CycleWorkspace(); });
            Grid.SetColumn(cycleBtn, 2);
            wsGrid.Children.Add(cycleBtn);

            workspaceTile.Child = wsGrid;
            Grid.SetRow(workspaceTile, 0);
            grid.Children.Add(workspaceTile);

            // Weekly Consistency & Streak Card
            Border streakCard = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel streakStack = new StackPanel();

            Grid streakHeader = new Grid();
            streakHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            streakHeader.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock streakTitle = new TextBlock()
            {
                Text = "WEEKLY CONSISTENCY & STREAK",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(streakTitle, 0);
            streakHeader.Children.Add(streakTitle);

            Border streakBadge = new Border()
            {
                Background = _theme.AmberBadgeBg,
                BorderBrush = _theme.AmberBadgeText,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 2, 6, 2)
            };
            _weeklyStreakBadgeText = new TextBlock()
            {
                Text = "🔥 1 DAY STREAK",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.AmberBadgeText
            };
            streakBadge.Child = _weeklyStreakBadgeText;
            Grid.SetColumn(streakBadge, 1);
            streakHeader.Children.Add(streakBadge);
            streakStack.Children.Add(streakHeader);

            _weeklyBarsGrid = new Grid()
            {
                Margin = new Thickness(0, 10, 0, 2)
            };
            streakStack.Children.Add(_weeklyBarsGrid);

            streakCard.Child = streakStack;
            Grid.SetRow(streakCard, 1);
            grid.Children.Add(streakCard);

            // Chronological Work Matrix
            Border timelineCard = new Border()
            {
                Background = _theme.SurfaceCard,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1)
            };
            StackPanel matrixStack = new StackPanel();

            TextBlock matrixTitle = new TextBlock()
            {
                Text = "CHRONOLOGICAL WORK MATRIX (8 AM - 8 PM)",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 8)
            };
            matrixStack.Children.Add(matrixTitle);

            _hourlyMatrixStack = new StackPanel();
            matrixStack.Children.Add(_hourlyMatrixStack);

            timelineCard.Child = matrixStack;
            Grid.SetRow(timelineCard, 2);
            grid.Children.Add(timelineCard);

            return grid;
        }

        private Grid BuildSettingsView()
        {
            Grid grid = new Grid();
            ScrollViewer scroll = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Style slimScroll = FocusPulseStyles.CreateSlimScrollBarStyle(_theme);
            if (slimScroll != null)
            {
                scroll.Resources[typeof(ScrollBar)] = slimScroll;
            }
            StackPanel settingsPanel = new StackPanel();

            // 1. Theme & Appearance Card
            Border themeTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel themeStack = new StackPanel();
            themeStack.Children.Add(new TextBlock()
            {
                Text = "THEME & COLOR PALETTES",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 8)
            });

            _settingsActiveThemeText = new TextBlock()
            {
                Text = "Active: " + (_config.IsDarkMode ? "Dark Mode" : "Light Mode") + " • " + _theme.PaletteName,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 10)
            };
            themeStack.Children.Add(_settingsActiveThemeText);

            // A. Appearance Mode Segmented Picker: [ ☀️ Light Mode ] [ 🌙 Dark Mode ]
            Grid modePicker = new Grid() { Margin = new Thickness(0, 0, 0, 12) };
            modePicker.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            modePicker.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(6) });
            modePicker.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            // Light Button
            Border btnLight = new Border()
            {
                Background = !_config.IsDarkMode ? _theme.BrandAccent : _theme.SurfaceElevated,
                BorderBrush = !_config.IsDarkMode ? _theme.BrandAccent : _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            TextBlock txtLight = new TextBlock()
            {
                Text = "☀️ Light Mode",
                FontSize = 9.5,
                FontWeight = !_config.IsDarkMode ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = !_config.IsDarkMode ? (_theme.IsDark ? FocusPulseStyles.Brush("#000000") : FocusPulseStyles.Brush("#FFFFFF")) : _theme.TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnLight.Child = txtLight;
            btnLight.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                SetAppearanceMode(false);
            };
            Grid.SetColumn(btnLight, 0);
            modePicker.Children.Add(btnLight);

            // Dark Button
            Border btnDark = new Border()
            {
                Background = _config.IsDarkMode ? _theme.BrandAccent : _theme.SurfaceElevated,
                BorderBrush = _config.IsDarkMode ? _theme.BrandAccent : _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand
            };
            TextBlock txtDark = new TextBlock()
            {
                Text = "🌙 Dark Mode",
                FontSize = 9.5,
                FontWeight = _config.IsDarkMode ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = _config.IsDarkMode ? (_theme.IsDark ? FocusPulseStyles.Brush("#000000") : FocusPulseStyles.Brush("#FFFFFF")) : _theme.TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnDark.Child = txtDark;
            btnDark.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                SetAppearanceMode(true);
            };
            Grid.SetColumn(btnDark, 2);
            modePicker.Children.Add(btnDark);

            themeStack.Children.Add(modePicker);

            // Subheader: Color Palettes
            themeStack.Children.Add(new TextBlock()
            {
                Text = "SELECT ACCENT COLOR PALETTE",
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // B. Color Palettes List
            foreach (PaletteOption pal in FocusPulseStyles.AvailablePalettes)
            {
                bool isSelected = pal.Key.Equals(_config.AccentPalette, StringComparison.OrdinalIgnoreCase);

                Border palCard = new Border()
                {
                    Background = isSelected ? _theme.SurfaceElevated : Brushes.Transparent,
                    BorderBrush = isSelected ? _theme.BrandAccent : _theme.SubtleBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };

                Grid cardGrid = new Grid();
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                // Jewel dot with ring
                Border jewelContainer = new Border()
                {
                    Width = 14,
                    Height = 14,
                    CornerRadius = new CornerRadius(7),
                    Background = FocusPulseStyles.Brush(pal.AccentColor),
                    BorderBrush = isSelected ? _theme.TextPrimary : Brushes.Transparent,
                    BorderThickness = new Thickness(isSelected ? 1.5 : 0),
                    Margin = new Thickness(0, 0, 9, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(jewelContainer, 0);
                cardGrid.Children.Add(jewelContainer);

                StackPanel titleStack = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
                TextBlock palTitle = new TextBlock()
                {
                    Text = pal.Name,
                    FontSize = 10,
                    FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = _theme.TextPrimary
                };
                titleStack.Children.Add(palTitle);
                Grid.SetColumn(titleStack, 1);
                cardGrid.Children.Add(titleStack);

                if (isSelected)
                {
                    Border activePill = new Border()
                    {
                        Background = _theme.FocusBadgeBg,
                        BorderBrush = _theme.FocusBadgeBorder,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 1, 6, 1),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    TextBlock activeTxt = new TextBlock()
                    {
                        Text = "✓ ACTIVE",
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        Foreground = _theme.FocusBadgeText
                    };
                    activePill.Child = activeTxt;
                    Grid.SetColumn(activePill, 2);
                    cardGrid.Children.Add(activePill);
                }
                else
                {
                    TextBlock hexTxt = new TextBlock()
                    {
                        Text = pal.AccentColor,
                        FontSize = 8,
                        FontFamily = new FontFamily("Consolas, Courier New"),
                        Foreground = _theme.TextMuted,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(hexTxt, 2);
                    cardGrid.Children.Add(hexTxt);
                }

                palCard.Child = cardGrid;

                string captureKey = pal.Key;
                palCard.MouseEnter += delegate { if (!captureKey.Equals(_config.AccentPalette, StringComparison.OrdinalIgnoreCase)) palCard.Background = _theme.SurfaceElevatedHover; };
                palCard.MouseLeave += delegate { if (!captureKey.Equals(_config.AccentPalette, StringComparison.OrdinalIgnoreCase)) palCard.Background = Brushes.Transparent; };
                palCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    SetColorPalette(captureKey);
                };

                themeStack.Children.Add(palCard);
            }

            themeTile.Child = themeStack;
            settingsPanel.Children.Add(themeTile);

            // 2. Window Mode & Pinning Card
            Border placementTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel placeStack = new StackPanel();
            placeStack.Children.Add(new TextBlock()
            {
                Text = "WINDOW DISPLAY & PINNING",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 6)
            });

            _settingsWindowModeText = new TextBlock()
            {
                Text = this.Topmost ? "Current: Always on Top" : "Current: Normal Desktop Window",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Topmost ? _theme.AmberBadgeText : _theme.BrandAccent,
                Margin = new Thickness(0, 0, 0, 8)
            };
            placeStack.Children.Add(_settingsWindowModeText);

            // Always on Top switch row
            Grid pinSwitchRow = new Grid() { Margin = new Thickness(0, 0, 0, 8) };
            pinSwitchRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            pinSwitchRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock pinLabel = new TextBlock()
            {
                Text = "Float above all active windows",
                FontSize = 10,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pinLabel, 0);
            pinSwitchRow.Children.Add(pinLabel);

            _togglePinSwitch = CreateSwitchToggle(this.Topmost, out _togglePinKnob, delegate(bool state)
            {
                SetAlwaysOnTopState(state);
            });
            Grid.SetColumn(_togglePinSwitch, 1);
            pinSwitchRow.Children.Add(_togglePinSwitch);
            placeStack.Children.Add(pinSwitchRow);

            // Show in Taskbar switch row
            Grid taskbarSwitchRow = new Grid() { Margin = new Thickness(0, 0, 0, 8) };
            taskbarSwitchRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            taskbarSwitchRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock taskbarLabel = new TextBlock()
            {
                Text = "Display icon on Windows Taskbar",
                FontSize = 10,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(taskbarLabel, 0);
            taskbarSwitchRow.Children.Add(taskbarLabel);

            _toggleTaskbarSwitch = CreateSwitchToggle(this.ShowInTaskbar, out _toggleTaskbarKnob, delegate(bool state)
            {
                this.ShowInTaskbar = state;
                _config.ShowInTaskbar = state;
                SaveConfiguration();
            });
            Grid.SetColumn(_toggleTaskbarSwitch, 1);
            taskbarSwitchRow.Children.Add(_toggleTaskbarSwitch);
            placeStack.Children.Add(taskbarSwitchRow);

            // Launch on Windows Startup switch row
            Grid startupSwitchRow = new Grid() { Margin = new Thickness(0, 0, 0, 8) };
            startupSwitchRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            startupSwitchRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock startupLabel = new TextBlock()
            {
                Text = "Launch automatically on Windows startup",
                FontSize = 10,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(startupLabel, 0);
            startupSwitchRow.Children.Add(startupLabel);

            Border toggleStartupKnob;
            Border toggleStartupSwitch = CreateSwitchToggle(IsWindowsStartupEnabled(), out toggleStartupKnob, delegate(bool state)
            {
                SetWindowsStartup(state);
            });
            Grid.SetColumn(toggleStartupSwitch, 1);
            startupSwitchRow.Children.Add(toggleStartupSwitch);
            placeStack.Children.Add(startupSwitchRow);

            // Mode action pills
            StackPanel modeButtons = new StackPanel() { Orientation = Orientation.Horizontal };
            modeButtons.Children.Add(CreateInteractivePill("📌 Pin on Top", delegate { SetAlwaysOnTopState(true); }));
            modeButtons.Children.Add(CreateInteractivePill("🪟 Normal Window", delegate { SetAlwaysOnTopState(false); }));
            modeButtons.Children.Add(CreateInteractivePill("🔕 Hide to Tray", delegate { this.Hide(); }));
            placeStack.Children.Add(modeButtons);

            placementTile.Child = placeStack;
            settingsPanel.Children.Add(placementTile);

            // 3. Window Geometry & View Presets Card
            Border sizeTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel sizeStack = new StackPanel();
            sizeStack.Children.Add(new TextBlock()
            {
                Text = "WINDOW GEOMETRY & VIEW PRESETS",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 6)
            });

            _settingsWindowSizeText = new TextBlock()
            {
                Text = string.Format("Current Dimensions: {0:F0} × {1:F0} px", this.Width, this.Height),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 8)
            };
            sizeStack.Children.Add(_settingsWindowSizeText);

            StackPanel sizeButtons = new StackPanel() { Orientation = Orientation.Horizontal };
            sizeButtons.Children.Add(CreateInteractivePill("Compact (380×600)", delegate { SetWindowDimensions(380, 600); }));
            sizeButtons.Children.Add(CreateInteractivePill("Standard (400×740)", delegate { SetWindowDimensions(400, 740); }));
            sizeButtons.Children.Add(CreateInteractivePill("Pro (480×880)", delegate { SetWindowDimensions(480, 880); }));
            sizeStack.Children.Add(sizeButtons);

            TextBlock sizeTip = new TextBlock()
            {
                Text = "💡 Tip: Drag any window edge or corner to freely resize to any size.",
                FontSize = 8.5,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 7, 0, 0)
            };
            sizeStack.Children.Add(sizeTip);

            sizeTile.Child = sizeStack;
            settingsPanel.Children.Add(sizeTile);

            // 4. Privacy Shield Card
            Border privacyTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel privStack = new StackPanel();
            privStack.Children.Add(new TextBlock()
            {
                Text = "🔒 100% OFFLINE & PRIVATE ARCHITECTURE",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText,
                Margin = new Thickness(0, 0, 0, 4)
            });

            privStack.Children.Add(new TextBlock()
            {
                Text = "FocusPulse is built on a zero-cloud, 100% local architecture. All activity metrics, window titles, and productivity logs stay strictly on your local PC in %LOCALAPPDATA%\\FocusPulse. No telemetry, no remote servers, and zero data leakage.",
                FontSize = 9.5,
                Foreground = _theme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 14
            });

            privacyTile.Child = privStack;
            settingsPanel.Children.Add(privacyTile);

            // 4. Opacity Presets
            Border opacityTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel opacityStack = new StackPanel();
            opacityStack.Children.Add(new TextBlock()
            {
                Text = "GLASS OPACITY PRESETS",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 6)
            });

            _settingsActiveOpacityText = new TextBlock()
            {
                Text = string.Format("Current Opacity: {0:F0}%", this.Opacity * 100.0),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 8)
            };
            opacityStack.Children.Add(_settingsActiveOpacityText);

            StackPanel opacityButtons = new StackPanel() { Orientation = Orientation.Horizontal };
            opacityButtons.Children.Add(CreateInteractivePill("100% Solid", delegate { SetWindowOpacityLevel(1.0); }));
            opacityButtons.Children.Add(CreateInteractivePill("95% Glass", delegate { SetWindowOpacityLevel(0.95); }));
            opacityButtons.Children.Add(CreateInteractivePill("80% Frosted", delegate { SetWindowOpacityLevel(0.80); }));
            opacityButtons.Children.Add(CreateInteractivePill("65% Ghost", delegate { SetWindowOpacityLevel(0.65); }));
            opacityStack.Children.Add(opacityButtons);

            opacityTile.Child = opacityStack;
            settingsPanel.Children.Add(opacityTile);

            // 5. Pomodoro Interval & Acoustic Audio Chimes Card
            Border pomoSettingsTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel pomoSetStack = new StackPanel();
            pomoSetStack.Children.Add(new TextBlock()
            {
                Text = "POMODORO INTERVALS & ACOUSTIC CHIMES",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Focus duration
            pomoSetStack.Children.Add(new TextBlock()
            {
                Text = string.Format("Default Work Interval: {0} minutes", _config.PomodoroWorkMinutes),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 4)
            });
            StackPanel workPills = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            workPills.Children.Add(CreateInteractivePill("20m", delegate { _config.PomodoroWorkMinutes = 20; _engine.PomodoroWorkMinutes = 20; SaveConfiguration(); UpdateDisplay(); }));
            workPills.Children.Add(CreateInteractivePill("25m (Classic)", delegate { _config.PomodoroWorkMinutes = 25; _engine.PomodoroWorkMinutes = 25; SaveConfiguration(); UpdateDisplay(); }));
            workPills.Children.Add(CreateInteractivePill("45m", delegate { _config.PomodoroWorkMinutes = 45; _engine.PomodoroWorkMinutes = 45; SaveConfiguration(); UpdateDisplay(); }));
            workPills.Children.Add(CreateInteractivePill("50m (Deep)", delegate { _config.PomodoroWorkMinutes = 50; _engine.PomodoroWorkMinutes = 50; SaveConfiguration(); UpdateDisplay(); }));
            pomoSetStack.Children.Add(workPills);

            // Audio Chimes toggle row
            Grid chimeRow = new Grid() { Margin = new Thickness(0, 2, 0, 8) };
            chimeRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            chimeRow.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            TextBlock chimeLabel = new TextBlock()
            {
                Text = "Play acoustic chime when work/break completes",
                FontSize = 10,
                Foreground = _theme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(chimeLabel, 0);
            chimeRow.Children.Add(chimeLabel);

            Border chimeKnob;
            Border chimeToggle = CreateSwitchToggle(_config.SoundChimesEnabled, out chimeKnob, delegate(bool state)
            {
                _config.SoundChimesEnabled = state;
                _engine.SoundChimesEnabled = state;
                SaveConfiguration();
            });
            Grid.SetColumn(chimeToggle, 1);
            chimeRow.Children.Add(chimeToggle);
            pomoSetStack.Children.Add(chimeRow);

            // Test chime button
            pomoSetStack.Children.Add(CreateInteractivePill("🔔 Preview Focus Chime", delegate
            {
                _engine.PlayChime(true);
            }));

            pomoSettingsTile.Child = pomoSetStack;
            settingsPanel.Children.Add(pomoSettingsTile);

            // 6. Data Management & Reset
            Border dataTile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel dataStack = new StackPanel();
            dataStack.Children.Add(new TextBlock()
            {
                Text = "DATA & TELEMETRY",
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 6)
            });
            dataStack.Children.Add(CreateInteractivePill("🔄 Reset Today's Metrics", delegate
            {
                _engine.ResetMetrics();
                UpdateDisplay();
            }));
            dataTile.Child = dataStack;
            settingsPanel.Children.Add(dataTile);

            scroll.Content = settingsPanel;
            grid.Children.Add(scroll);
            return grid;
        }

        private Ellipse CreateMiniSwatchDot(string hexColor)
        {
            return new Ellipse()
            {
                Width = 9,
                Height = 9,
                Fill = FocusPulseStyles.Brush(hexColor),
                Margin = new Thickness(0, 0, 3, 0)
            };
        }

        private Grid BuildMiniIslandHUD()
        {
            Grid grid = new Grid()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            Border miniAvatar = new Border()
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(7),
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.HairlineBorder,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _miniAppGlyph = new TextBlock()
            {
                Text = "✦",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.BrandAccent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            miniAvatar.Child = _miniAppGlyph;
            Grid.SetColumn(miniAvatar, 0);
            grid.Children.Add(miniAvatar);

            StackPanel center = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            _miniAppNameText = new TextBlock()
            {
                Text = "Google Antigravity IDE",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            StackPanel metadataLine = new StackPanel() { Orientation = Orientation.Horizontal };
            _miniStatusPulseDot = new Ellipse()
            {
                Width = 6,
                Height = 6,
                Fill = _theme.FocusBadgeText,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _miniSessionTimerText = new TextBlock()
            {
                Text = "00:00:00",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.AmberBadgeText,
                Margin = new Thickness(0, 0, 6, 0)
            };
            _miniScoreBadgeText = new TextBlock()
            {
                Text = "🏆 95%",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText,
                Margin = new Thickness(0, 0, 6, 0)
            };
            _miniSprintBadgeText = new TextBlock()
            {
                Text = "",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.DistractBadgeText
            };
            metadataLine.Children.Add(_miniStatusPulseDot);
            metadataLine.Children.Add(_miniSessionTimerText);
            metadataLine.Children.Add(_miniScoreBadgeText);
            metadataLine.Children.Add(_miniSprintBadgeText);

            center.Children.Add(_miniAppNameText);
            center.Children.Add(metadataLine);
            Grid.SetColumn(center, 1);
            grid.Children.Add(center);

            StackPanel rightSummary = new StackPanel()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            rightSummary.Children.Add(new TextBlock()
            {
                Text = "TODAY",
                FontSize = 7,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted
            });
            _miniDayTotalText = new TextBlock()
            {
                Text = "0m",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.FocusBadgeText
            };
            rightSummary.Children.Add(_miniDayTotalText);
            Grid.SetColumn(rightSummary, 2);
            grid.Children.Add(rightSummary);

            TextBlock dummyExpand;
            Border expandButton = CreateMicroIconButton("◳", "Expand to Full Dashboard (Ctrl+M)", out dummyExpand, delegate { ToggleDisplayMode(); });
            Grid.SetColumn(expandButton, 3);
            grid.Children.Add(expandButton);

            return grid;
        }

        private Border CreateMetricTile(string label, string initialValue, SolidColorBrush valueBrush, out TextBlock valueRef)
        {
            Border tile = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 8, 10, 8)
            };
            StackPanel stack = new StackPanel();
            stack.Children.Add(new TextBlock()
            {
                Text = label,
                FontSize = 8.5,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 0, 2)
            });
            valueRef = new TextBlock()
            {
                Text = initialValue,
                FontSize = 14.5,
                FontWeight = FontWeights.Bold,
                Foreground = valueBrush,
                FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI, Consolas, monospace"),
                Margin = new Thickness(0, 1, 0, 0)
            };
            stack.Children.Add(valueRef);
            tile.Child = stack;

            tile.MouseEnter += delegate { tile.Background = _theme.SurfaceCardHover; };
            tile.MouseLeave += delegate { tile.Background = _theme.SurfaceCard; };

            return tile;
        }

        private Border CreateSegmentTab(string label, bool isActive, out TextBlock labelRef, Action onClick)
        {
            Border tab = new Border()
            {
                CornerRadius = new CornerRadius(8),
                Background = isActive ? _theme.SurfaceActiveTab : Brushes.Transparent,
                BorderBrush = isActive ? _theme.HairlineBorder : Brushes.Transparent,
                BorderThickness = isActive ? new Thickness(1) : new Thickness(0),
                Height = 27,
                Cursor = Cursors.Hand,
                Margin = new Thickness(1)
            };
            TextBlock tb = new TextBlock()
            {
                Text = label,
                FontSize = 10,
                FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = isActive ? _theme.BrandAccent : _theme.TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            tab.Child = tb;
            labelRef = tb;

            tab.MouseEnter += delegate
            {
                if (tab.Background != _theme.SurfaceActiveTab)
                {
                    tab.Background = _theme.SurfaceElevatedHover;
                }
            };
            tab.MouseLeave += delegate
            {
                if (tab.Background != _theme.SurfaceActiveTab)
                {
                    tab.Background = Brushes.Transparent;
                }
            };
            tab.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                e.Handled = true;
                onClick();
            };

            return tab;
        }

        private Border CreateInteractivePill(string title, Action onClick)
        {
            Border btn = new Border()
            {
                CornerRadius = new CornerRadius(8),
                Background = _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 4),
                Cursor = Cursors.Hand
            };
            TextBlock tb = new TextBlock()
            {
                Text = title,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Child = tb;

            btn.MouseEnter += delegate { btn.Background = _theme.SurfaceElevatedHover; };
            btn.MouseLeave += delegate { btn.Background = _theme.SurfaceElevated; };
            btn.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                e.Handled = true;
                onClick();
            };

            return btn;
        }

        private Border CreateMicroIconButton(string iconGlyph, string tip, out TextBlock iconRef, Action onClick)
        {
            Border btn = new Border()
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = tip
            };
            TextBlock tb = new TextBlock()
            {
                Text = iconGlyph,
                FontSize = 11,
                Foreground = _theme.TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI Symbol, Segoe UI Emoji, Segoe UI, Arial")
            };
            btn.Child = tb;
            iconRef = tb;

            btn.MouseEnter += delegate { btn.Background = _theme.SurfaceElevated; };
            btn.MouseLeave += delegate { btn.Background = Brushes.Transparent; };
            btn.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                e.Handled = true;
                onClick();
            };

            return btn;
        }

        private Border CreateSwitchToggle(bool initialValue, out Border knobRef, Action<bool> onToggled)
        {
            bool isChecked = initialValue;
            Border track = new Border()
            {
                Width = 34,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = isChecked ? _theme.FocusBadgeText : _theme.SurfaceElevated,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Padding = new Thickness(2)
            };

            Border knob = new Border()
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = Brushes.White,
                HorizontalAlignment = isChecked ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            track.Child = knob;
            knobRef = knob;

            track.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                e.Handled = true;
                isChecked = !isChecked;
                track.Background = isChecked ? _theme.FocusBadgeText : _theme.SurfaceElevated;
                knob.HorizontalAlignment = isChecked ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                onToggled(isChecked);
            };

            return track;
        }

        private void UpdatePrivacyToggleUI()
        {
        }

        private void SetAlwaysOnTopState(bool topMost)
        {
            this.Topmost = topMost;
            _config.AlwaysOnTop = topMost;
            if (_pinIconText != null)
            {
                _pinIconText.Text = topMost ? "📌" : "🪟";
                _pinIconText.Foreground = topMost ? _theme.AmberBadgeText : _theme.TextMuted;
            }
            if (_settingsWindowModeText != null)
            {
                _settingsWindowModeText.Text = topMost ? "Current: Always on Top" : "Current: Normal Desktop Window";
                _settingsWindowModeText.Foreground = topMost ? _theme.AmberBadgeText : _theme.BrandAccent;
            }
            if (_togglePinSwitch != null && _togglePinKnob != null)
            {
                _togglePinSwitch.Background = topMost ? _theme.FocusBadgeText : _theme.SurfaceElevated;
                _togglePinKnob.HorizontalAlignment = topMost ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            SaveConfiguration();
        }

        private void ToggleAlwaysOnTop()
        {
            SetAlwaysOnTopState(!this.Topmost);
        }

        private void SetWindowOpacityLevel(double opac)
        {
            this.Opacity = opac;
            _config.Opacity = opac;
            if (_settingsActiveOpacityText != null)
            {
                _settingsActiveOpacityText.Text = string.Format("Current Opacity: {0:F0}%", opac * 100.0);
            }
            SaveConfiguration();
        }

        private void SetAppearanceMode(bool isDark)
        {
            _config.IsDarkMode = isDark;
            _theme = FocusPulseStyles.ResolveTheme(_config.IsDarkMode, _config.AccentPalette);
            _config.ThemeName = _theme.Key;
            RebuildUserInterface();
            SaveConfiguration();
        }

        private void SetColorPalette(string paletteKey)
        {
            _config.AccentPalette = paletteKey;
            _theme = FocusPulseStyles.ResolveTheme(_config.IsDarkMode, _config.AccentPalette);
            _config.ThemeName = _theme.Key;
            RebuildUserInterface();
            SaveConfiguration();
        }

        private void SwitchSurfaceTheme(string themeKey)
        {
            _theme = FocusPulseStyles.ResolveTheme(themeKey);
            _config.ThemeName = _theme.Key;
            _config.IsDarkMode = _theme.IsDark;
            _config.AccentPalette = _theme.PaletteKey;
            RebuildUserInterface();
            SaveConfiguration();
        }

        private bool IsWindowsStartupEnabled()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("FocusPulse") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SetWindowsStartup(bool enable)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            key.SetValue("FocusPulse", "\"" + exePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("FocusPulse", false);
                        }
                    }
                }
            }
            catch { }
        }

        private void CycleWorkspace()
        {
            string[] tags = new string[] { "Development", "Engineering", "Design", "Research", "Personal" };
            int idx = Array.IndexOf(tags, _engine.ActiveProjectWorkspace);
            idx = (idx + 1) % tags.Length;
            _engine.ActiveProjectWorkspace = tags[idx];
            _config.ActiveProjectWorkspace = tags[idx];
            if (_workspaceTagText != null) _workspaceTagText.Text = tags[idx];
            SaveConfiguration();
        }

        private void NavigateToTab(int tabIndex)
        {
            _currentTabIndex = tabIndex;
            _overviewTabView.Visibility = (tabIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
            _focusTabView.Visibility = (tabIndex == 1) ? Visibility.Visible : Visibility.Collapsed;
            _timelineTabView.Visibility = (tabIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
            _settingsTabView.Visibility = (tabIndex == 3) ? Visibility.Visible : Visibility.Collapsed;

            _tabBtnOverview.Background = (tabIndex == 0) ? _theme.SurfaceActiveTab : Brushes.Transparent;
            _tabBtnFocus.Background = (tabIndex == 1) ? _theme.SurfaceActiveTab : Brushes.Transparent;
            _tabBtnTimeline.Background = (tabIndex == 2) ? _theme.SurfaceActiveTab : Brushes.Transparent;
            _tabBtnSettings.Background = (tabIndex == 3) ? _theme.SurfaceActiveTab : Brushes.Transparent;

            _tabBtnOverview.BorderBrush = (tabIndex == 0) ? _theme.HairlineBorder : Brushes.Transparent;
            _tabBtnFocus.BorderBrush = (tabIndex == 1) ? _theme.HairlineBorder : Brushes.Transparent;
            _tabBtnTimeline.BorderBrush = (tabIndex == 2) ? _theme.HairlineBorder : Brushes.Transparent;
            _tabBtnSettings.BorderBrush = (tabIndex == 3) ? _theme.HairlineBorder : Brushes.Transparent;

            _tabTxtOverview.Foreground = (tabIndex == 0) ? _theme.BrandAccent : _theme.TextSecondary;
            _tabTxtFocus.Foreground = (tabIndex == 1) ? _theme.BrandAccent : _theme.TextSecondary;
            _tabTxtTimeline.Foreground = (tabIndex == 2) ? _theme.BrandAccent : _theme.TextSecondary;
            _tabTxtSettings.Foreground = (tabIndex == 3) ? _theme.BrandAccent : _theme.TextSecondary;

            _tabTxtOverview.FontWeight = (tabIndex == 0) ? FontWeights.Bold : FontWeights.SemiBold;
            _tabTxtFocus.FontWeight = (tabIndex == 1) ? FontWeights.Bold : FontWeights.SemiBold;
            _tabTxtTimeline.FontWeight = (tabIndex == 2) ? FontWeights.Bold : FontWeights.SemiBold;
            _tabTxtSettings.FontWeight = (tabIndex == 3) ? FontWeights.Bold : FontWeights.SemiBold;
        }

        private void ToggleDisplayMode()
        {
            ApplyDisplayMode(!_isMiniMode);
        }

        private void ApplyDisplayMode(bool isMini)
        {
            _isMiniMode = isMini;
            _config.IsMiniMode = isMini;

            if (_isMiniMode)
            {
                this.ResizeMode = ResizeMode.NoResize;
                this.MinWidth = 0;
                this.MinHeight = 0;
                this.MaxWidth = 500;
                this.MaxHeight = 100;
                this.Width = 330;
                this.Height = 68;
                if (_mainShellBorder != null)
                {
                    _mainShellBorder.CornerRadius = new CornerRadius(24);
                    _mainShellBorder.Padding = new Thickness(12, 8, 12, 8);
                }
                if (_fullDashboardGrid != null) _fullDashboardGrid.Visibility = Visibility.Collapsed;
                if (_miniIslandGrid != null) _miniIslandGrid.Visibility = Visibility.Visible;
            }
            else
            {
                this.ResizeMode = ResizeMode.CanResize;
                this.MinWidth = 340;
                this.MinHeight = 520;
                this.MaxWidth = 1000;
                this.MaxHeight = 1400;
                this.Width = (_config.Width >= 340 && _config.Width <= 1000) ? _config.Width : 400;
                this.Height = (_config.Height >= 520 && _config.Height <= 1400) ? _config.Height : 740;
                if (_mainShellBorder != null)
                {
                    _mainShellBorder.CornerRadius = new CornerRadius(16);
                    _mainShellBorder.Padding = new Thickness(14);
                }
                if (_fullDashboardGrid != null) _fullDashboardGrid.Visibility = Visibility.Visible;
                if (_miniIslandGrid != null) _miniIslandGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void SetWindowDimensions(double w, double h)
        {
            this.Width = w;
            this.Height = h;
            _config.Width = w;
            _config.Height = h;
            if (_settingsWindowSizeText != null)
            {
                _settingsWindowSizeText.Text = string.Format("Current Dimensions: {0:F0} × {1:F0} px", w, h);
            }
            SaveConfiguration();
        }

        private void SetAppsViewDensity(bool isCompact)
        {
            _config.IsCompactAppsView = isCompact;
            if (_btnViewCards != null && _btnViewCompact != null)
            {
                _btnViewCards.Background = !isCompact ? _theme.SurfaceActiveTab : Brushes.Transparent;
                _txtViewCards.FontWeight = !isCompact ? FontWeights.Bold : FontWeights.Normal;
                _txtViewCards.Foreground = !isCompact ? _theme.TextPrimary : _theme.TextMuted;

                _btnViewCompact.Background = isCompact ? _theme.SurfaceActiveTab : Brushes.Transparent;
                _txtViewCompact.FontWeight = isCompact ? FontWeights.Bold : FontWeights.Normal;
                _txtViewCompact.Foreground = isCompact ? _theme.TextPrimary : _theme.TextMuted;
            }
            UpdateDisplay();
            SaveConfiguration();
        }

        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST && !_isMiniMode)
            {
                int x = unchecked((short)(long)lParam);
                int y = unchecked((short)((long)lParam >> 16));

                try
                {
                    Point pt = this.PointFromScreen(new Point(x, y));
                    double w = this.ActualWidth;
                    double h = this.ActualHeight;
                    const int b = 18;
                    const int corner = 28;

                    bool isLeft = pt.X <= b;
                    bool isRight = pt.X >= w - b;
                    bool isTop = pt.Y <= b;
                    bool isBottom = pt.Y >= h - b;

                    bool isCornerLeft = pt.X <= corner;
                    bool isCornerRight = pt.X >= w - corner;
                    bool isCornerTop = pt.Y <= corner;
                    bool isCornerBottom = pt.Y >= h - corner;

                    if (isCornerLeft && isCornerTop) { handled = true; return new IntPtr(HTTOPLEFT); }
                    if (isCornerRight && isCornerTop) { handled = true; return new IntPtr(HTTOPRIGHT); }
                    if (isCornerLeft && isCornerBottom) { handled = true; return new IntPtr(HTBOTTOMLEFT); }
                    if (isCornerRight && isCornerBottom) { handled = true; return new IntPtr(HTBOTTOMRIGHT); }

                    if (isTop) { handled = true; return new IntPtr(HTTOP); }
                    if (isBottom) { handled = true; return new IntPtr(HTBOTTOM); }
                    if (isLeft) { handled = true; return new IntPtr(HTLEFT); }
                    if (isRight) { handled = true; return new IntPtr(HTRIGHT); }
                }
                catch { }
            }
            return IntPtr.Zero;
        }

        private void BackgroundTrackingLoop()
        {
            while (_isTrackingActive)
            {
                try
                {
                    _engine.Tick();
                }
                catch { }
                Thread.Sleep(1000);
            }
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            _uiTickCounter++;

            if (_uiTickCounter % 10 == 0)
            {
                QueryExternalSyncNodeAsync();
            }

            if (_uiTickCounter % 30 == 0)
            {
                _engine.SaveMetrics();
                SaveConfiguration();
            }

            UpdateDisplay();
        }

        private void QueryExternalSyncNodeAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                bool connected = _syncClient.ProbeLocalNode();
                DaySummary syncSummary = null;
                if (connected)
                {
                    syncSummary = _syncClient.QuerySynchronizedMetrics();
                }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _isNodeConnected = connected;
                    if (connected)
                    {
                        _statusPillText.Text = "● SYNCED";
                        _statusPillText.Foreground = _theme.FocusBadgeText;
                        _statusPillBorder.Background = _theme.FocusBadgeBg;
                        _statusPillBorder.BorderBrush = _theme.FocusBadgeBorder;

                        if (syncSummary != null && syncSummary.TotalActiveSeconds > 0)
                        {
                            _metricActiveText.Text = syncSummary.FormattedActiveTime;
                            _metricIdleText.Text = syncSummary.FormattedIdleTime;
                            _miniDayTotalText.Text = syncSummary.FormattedActiveTime;
                        }
                    }
                    else
                    {
                        _statusPillText.Text = "● ACTIVE";
                        _statusPillText.Foreground = _theme.FocusBadgeText;
                        _statusPillBorder.Background = _theme.FocusBadgeBg;
                        _statusPillBorder.BorderBrush = _theme.FocusBadgeBorder;
                    }
                }));
            });
        }

        private void UpdateDisplay()
        {
            ActiveWindowSnapshot win = _engine.CurrentWindow;
            DaySummary summary = _engine.Summary;
            FlowSprintSession sprint = _engine.ActiveSprint;
            WellnessCoach coach = _engine.Coach;

            // 1. Hero Card Updates
            _heroAppName.Text = win.FriendlyName;
            _heroWindowTitle.Text = string.IsNullOrEmpty(win.WindowTitle) ? win.FriendlyName : win.WindowTitle;
            _heroWindowTitle.ToolTip = win.WindowTitle;
            _heroSessionTimer.Text = win.FormattedElapsed;
            string appGlyph = FocusPulseStyles.GetAppGlyph(win.FriendlyName);
            _heroAvatarGlyph.Text = appGlyph;
            _miniAppGlyph.Text = appGlyph;

            if (win.Productivity == ProductivityClass.FocusWork)
            {
                _heroProductivityBadge.Text = "Deep Focus";
                _heroProductivityBadge.Foreground = _theme.FocusBadgeText;
                _heroProductivityBadgeBorder.Background = _theme.FocusBadgeBg;
                _heroProductivityBadgeBorder.BorderBrush = _theme.FocusBadgeBorder;
            }
            else if (win.Productivity == ProductivityClass.Distraction)
            {
                _heroProductivityBadge.Text = "Distraction";
                _heroProductivityBadge.Foreground = _theme.DistractBadgeText;
                _heroProductivityBadgeBorder.Background = _theme.DistractBadgeBg;
                _heroProductivityBadgeBorder.BorderBrush = _theme.DistractBadgeBorder;
            }
            else
            {
                _heroProductivityBadge.Text = "General";
                _heroProductivityBadge.Foreground = _theme.GeneralBadgeText;
                _heroProductivityBadgeBorder.Background = _theme.GeneralBadgeBg;
                _heroProductivityBadgeBorder.BorderBrush = _theme.GeneralBadgeBorder;
            }

            if (win.IsIdle)
            {
                _heroStatusBadge.Text = string.Format("💤 Away ({0:F0}s)", win.IdleSeconds);
                _heroStatusBadge.Foreground = _theme.AmberBadgeText;
                _heroStatusBadgeBorder.Background = _theme.AmberBadgeBg;
                _heroStatusBadgeBorder.BorderBrush = _theme.AmberBadgeText;
                _miniStatusPulseDot.Fill = _theme.AmberBadgeText;
                _headerLiveDot.Fill = _theme.AmberBadgeText;
            }
            else
            {
                _heroStatusBadge.Text = "🟢 Active";
                _heroStatusBadge.Foreground = _theme.FocusBadgeText;
                _heroStatusBadgeBorder.Background = _theme.FocusBadgeBg;
                _heroStatusBadgeBorder.BorderBrush = _theme.FocusBadgeBorder;
                _miniStatusPulseDot.Fill = _theme.FocusBadgeText;
                _headerLiveDot.Fill = _theme.FocusBadgeText;
            }

            // 2. Metrics & Proportional Segmented Flow Bar
            if (!_isNodeConnected)
            {
                _metricActiveText.Text = summary.FormattedActiveTime;
                _metricIdleText.Text = summary.FormattedIdleTime;
                _miniDayTotalText.Text = summary.FormattedActiveTime;
            }
            _metricFocusText.Text = summary.FormattedFocusTime;

            double totalAct = Math.Max(1.0, summary.TotalActiveSeconds);
            double pctFocus = (summary.FocusWorkSeconds / totalAct) * 100.0;
            double pctGeneral = (summary.GeneralSeconds / totalAct) * 100.0;
            double pctDistract = (summary.DistractionSeconds / totalAct) * 100.0;

            _flowColFocus.Width = new GridLength(Math.Max(0.5, pctFocus), GridUnitType.Star);
            _flowColGeneral.Width = new GridLength(Math.Max(0.5, pctGeneral), GridUnitType.Star);
            _flowColDistract.Width = new GridLength(Math.Max(0.5, pctDistract), GridUnitType.Star);

            _flowLegendFocus.Text = string.Format("● {0:F0}% Focus", pctFocus);
            _flowLegendGeneral.Text = string.Format("● {0:F0}% General", pctGeneral);
            _flowLegendDistract.Text = string.Format("● {0:F0}% Distract", pctDistract);

            // 3. Focus Tab Updates
            int focusScore = summary.CalculateFocusScore();
            _focusScoreValueText.Text = focusScore + "%";
            _focusScoreValueText.Foreground = FocusPulseStyles.ScoreToBrush(focusScore, _theme);
            if (focusScore >= 80) _focusScoreStatusText.Text = "🏆 Deep Focus Flow";
            else if (focusScore >= 60) _focusScoreStatusText.Text = "⚡ Solid Productivity";
            else if (focusScore >= 40) _focusScoreStatusText.Text = "⚠️ Frequent Distraction";
            else _focusScoreStatusText.Text = "🛑 High Fragmentation";

            double activeHours = Math.Max(0.1, summary.TotalActiveSeconds / 3600.0);
            double switchesPerHour = summary.WindowSwitches / activeHours;
            _velocitySwitchText.Text = string.Format("Switching Rate: {0:F0}/hr ({1})", switchesPerHour,
                switchesPerHour < 40 ? "Calm & Focused" : (switchesPerHour < 80 ? "Moderate" : "Fragmented"));

            if (sprint.IsActive)
            {
                _sprintCountdownText.Text = sprint.FormattedRemaining;
                _sprintProgressBar.Value = sprint.ProgressPercent;
                _sprintSubStatusText.Text = string.Format("{0} • Distraction Alerts: {1}", sprint.SprintLabel, sprint.DistractionWarnings);
                _miniSprintBadgeText.Text = string.Format("🍅 {0}", sprint.FormattedRemaining);

                if (_pomodoroPlayPauseText != null)
                {
                    _pomodoroPlayPauseText.Text = sprint.IsPaused ? "▶ Resume" : "❚❚ Pause";
                }

                if (_pomodoroModeBadgeText != null && _pomodoroModeBadgeBorder != null)
                {
                    if (sprint.Mode == PomodoroMode.ShortBreak)
                    {
                        _pomodoroModeBadgeText.Text = "☕ SHORT BREAK";
                        _pomodoroModeBadgeText.Foreground = _theme.AmberBadgeText;
                        _pomodoroModeBadgeBorder.Background = _theme.AmberBadgeBg;
                        _pomodoroModeBadgeBorder.BorderBrush = _theme.AmberBadgeText;
                        if (_pomodoroSkipBreakBtn != null) _pomodoroSkipBreakBtn.Visibility = Visibility.Visible;
                    }
                    else if (sprint.Mode == PomodoroMode.LongBreak)
                    {
                        _pomodoroModeBadgeText.Text = "🌿 LONG BREAK";
                        _pomodoroModeBadgeText.Foreground = _theme.FocusBadgeText;
                        _pomodoroModeBadgeBorder.Background = _theme.FocusBadgeBg;
                        _pomodoroModeBadgeBorder.BorderBrush = _theme.FocusBadgeBorder;
                        if (_pomodoroSkipBreakBtn != null) _pomodoroSkipBreakBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        _pomodoroModeBadgeText.Text = "🎯 DEEP FOCUS";
                        _pomodoroModeBadgeText.Foreground = _theme.BrandAccent;
                        _pomodoroModeBadgeBorder.Background = _theme.SurfaceElevated;
                        _pomodoroModeBadgeBorder.BorderBrush = _theme.BrandAccent;
                        if (_pomodoroSkipBreakBtn != null) _pomodoroSkipBreakBtn.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                _sprintCountdownText.Text = string.Format("{0:D2}:00", _config.PomodoroWorkMinutes > 0 ? _config.PomodoroWorkMinutes : 25);
                _sprintProgressBar.Value = 0;
                _sprintSubStatusText.Text = "Ready for an uninterrupted sprint";
                _miniSprintBadgeText.Text = "";

                if (_pomodoroPlayPauseText != null) _pomodoroPlayPauseText.Text = "▶ Start";
                if (_pomodoroSkipBreakBtn != null) _pomodoroSkipBreakBtn.Visibility = Visibility.Collapsed;
                if (_pomodoroModeBadgeText != null && _pomodoroModeBadgeBorder != null)
                {
                    _pomodoroModeBadgeText.Text = "🎯 READY TO FOCUS";
                    _pomodoroModeBadgeText.Foreground = _theme.TextMuted;
                    _pomodoroModeBadgeBorder.Background = _theme.SurfaceElevated;
                    _pomodoroModeBadgeBorder.BorderBrush = _theme.SubtleBorder;
                }
            }

            // Update Target Task Banner
            if (_pomodoroTargetTaskText != null)
            {
                if (!string.IsNullOrEmpty(sprint.LinkedTaskTitle))
                {
                    _pomodoroTargetTaskText.Text = string.Format("🎯 Target: {0}", sprint.LinkedTaskTitle);
                    _pomodoroTargetTaskText.Foreground = _theme.BrandAccent;
                    if (_pomodoroClearTargetBtn != null) _pomodoroClearTargetBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    _pomodoroTargetTaskText.Text = "🎯 No task target linked • Click 🎯 on any task below";
                    _pomodoroTargetTaskText.Foreground = _theme.TextMuted;
                    if (_pomodoroClearTargetBtn != null) _pomodoroClearTargetBtn.Visibility = Visibility.Collapsed;
                }
            }

            // Update 4-Cycle Dots
            if (_pomodoroCycleDotsPanel != null)
            {
                _pomodoroCycleDotsPanel.Children.Clear();
                int currentCycle = Math.Max(1, Math.Min(4, sprint.CurrentCycle));
                for (int c = 1; c <= 4; c++)
                {
                    Border dot = new Border()
                    {
                        Width = 14,
                        Height = 6,
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(0, 0, 4, 0),
                        Background = (c <= currentCycle && sprint.IsActive) ? _theme.BrandAccent : _theme.SurfaceElevated,
                        BorderBrush = (c == currentCycle) ? _theme.BrandAccent : _theme.SubtleBorder,
                        BorderThickness = new Thickness(1)
                    };
                    _pomodoroCycleDotsPanel.Children.Add(dot);
                }
            }
            if (_pomodoroCycleLabelText != null)
            {
                _pomodoroCycleLabelText.Text = string.Format("Cycle {0} of 4", sprint.CurrentCycle);
            }

            double targetSeconds = _engine.DailyGoalHours * 3600.0;
            double goalPercent = Math.Min(100.0, (summary.FocusWorkSeconds / targetSeconds) * 100.0);
            _dailyTargetProgressBar.Value = goalPercent;
            _dailyTargetSummaryText.Text = string.Format("{0:F0}% ({1} / {2}h)", goalPercent, summary.FormattedFocusTime, (int)_engine.DailyGoalHours);

            if (coach.IsBreakRecommended && !coach.IsDismissed)
            {
                _coachAlertBanner.Visibility = Visibility.Visible;
                _coachAlertMessageText.Text = string.Format("🧘 Healthy Pacing: Continuous work {0}. Time for a quick stretch!", coach.FormattedContinuousDuration);
            }
            else
            {
                _coachAlertBanner.Visibility = Visibility.Collapsed;
            }

            // 4. Mini Island Labels
            _miniAppNameText.Text = win.FriendlyName;
            _miniSessionTimerText.Text = win.FormattedElapsed;
            _miniScoreBadgeText.Text = string.Format("🏆 {0}%", focusScore);

            // 5. Scrollable & Filterable Applications List (fetches ALL active apps)
            string sortMode = !string.IsNullOrEmpty(_config.AppSortOrder) ? _config.AppSortOrder : "Duration";
            List<AppSessionRecord> allRecords = _engine.GetTopApplications(200, _activeCategoryFilter, _activeSearchQuery, sortMode);
            if (_appsCountBadgeText != null)
            {
                _appsCountBadgeText.Text = allRecords.Count + " apps";
            }

            if (_topApplicationsStack != null)
            {
                _topApplicationsStack.Children.Clear();

                if (allRecords.Count == 0)
                {
                    Border emptyCard = new Border()
                    {
                        Background = _theme.SurfaceCard,
                        BorderBrush = _theme.SubtleBorder,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(16),
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    TextBlock emptyMsg = new TextBlock()
                    {
                        Text = string.IsNullOrEmpty(_activeSearchQuery) 
                            ? "No applications recorded in this category yet."
                            : string.Format("No applications matching \"{0}\"", _activeSearchQuery),
                        FontSize = 10,
                        Foreground = _theme.TextMuted,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    emptyCard.Child = emptyMsg;
                    _topApplicationsStack.Children.Add(emptyCard);
                }
                else
                {
                    foreach (AppSessionRecord rec in allRecords)
                    {
                        Border row = _config.IsCompactAppsView ? CreateCompactApplicationRow(rec) : CreateApplicationRow(rec);
                        _topApplicationsStack.Children.Add(row);
                    }
                }
            }

            // 6. Chronological Matrix & Weekly Streak
            if (_currentTabIndex == 2)
            {
                RenderChronologicalMatrix();
                UpdateWeeklyMetricsDisplay();
            }

            // 7. System Tray Tooltip Synchronization
            if (_notifyIcon != null)
            {
                try
                {
                    string trayTip = string.Format("FocusPulse: {0} Active ({1}% Focus)", 
                        summary.FormattedActiveTime, focusScore);
                    if (trayTip.Length > 63) trayTip = trayTip.Substring(0, 63);
                    _notifyIcon.Text = trayTip;
                }
                catch { }
            }
        }

        private void UpdateWeeklyMetricsDisplay()
        {
            if (_weeklyBarsGrid == null) return;
            _weeklyBarsGrid.Children.Clear();
            _weeklyBarsGrid.ColumnDefinitions.Clear();

            List<WeeklyDayMetric> weekly = _engine.GetWeeklyMetrics(_engine.DailyGoalHours);
            int streak = _engine.GetActiveStreakDays();

            if (_weeklyStreakBadgeText != null)
            {
                _weeklyStreakBadgeText.Text = string.Format("🔥 {0} DAY STREAK", streak);
            }

            for (int i = 0; i < weekly.Count; i++)
            {
                _weeklyBarsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                WeeklyDayMetric m = weekly[i];

                StackPanel colStack = new StackPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // Vertical Bar Container
                Border barContainer = new Border()
                {
                    Width = 16,
                    Height = 44,
                    Background = _theme.SurfaceElevated,
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 0, 4),
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // Fill indicator
                double fillHeight = Math.Max(2, Math.Min(44, m.Ratio * 44.0));
                Border barFill = new Border()
                {
                    Width = 16,
                    Height = fillHeight,
                    CornerRadius = new CornerRadius(3),
                    Background = m.IsToday ? _theme.BrandAccent : (m.Ratio >= 1.0 ? _theme.FocusBadgeText : _theme.FocusBadgeBorder),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                barContainer.Child = barFill;
                colStack.Children.Add(barContainer);

                // Day Label (e.g. Mon)
                TextBlock dayTb = new TextBlock()
                {
                    Text = m.DayLabel,
                    FontSize = 8,
                    FontWeight = m.IsToday ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = m.IsToday ? _theme.BrandAccent : _theme.TextMuted,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                colStack.Children.Add(dayTb);

                // Hours Label (e.g. 3.5h)
                TextBlock hrsTb = new TextBlock()
                {
                    Text = string.Format("{0:F1}h", m.FocusHours),
                    FontSize = 7.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = m.IsToday ? _theme.TextPrimary : _theme.TextSecondary,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                colStack.Children.Add(hrsTb);

                Grid.SetColumn(colStack, i);
                _weeklyBarsGrid.Children.Add(colStack);
            }
        }

        private void RenderChronologicalMatrix()
        {
            _hourlyMatrixStack.Children.Clear();
            DaySummary summary = _engine.Summary;

            for (int h = 8; h <= 20; h++)
            {
                double actSec = (summary.HourlyActiveSeconds != null && h < summary.HourlyActiveSeconds.Length) ? summary.HourlyActiveSeconds[h] : 0;
                double focusSec = (summary.HourlyFocusSeconds != null && h < summary.HourlyFocusSeconds.Length) ? summary.HourlyFocusSeconds[h] : 0;

                double actPct = Math.Min(100.0, (actSec / 3600.0) * 100.0);
                double focusPct = Math.Min(100.0, (focusSec / 3600.0) * 100.0);

                Grid row = new Grid() { Margin = new Thickness(0, 0, 0, 3) };
                row.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(45) });
                row.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(35) });

                TextBlock hourText = new TextBlock()
                {
                    Text = string.Format("{0:D2}:00", h),
                    FontSize = 9,
                    Foreground = _theme.TextMuted,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(hourText, 0);
                row.Children.Add(hourText);

                ProgressBar bar = new ProgressBar()
                {
                    Height = 6,
                    Value = actPct,
                    Foreground = focusPct > 0 ? _theme.FocusBadgeText : _theme.BrandAccent,
                    Background = _theme.SurfaceElevated,
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(bar, 1);
                row.Children.Add(bar);

                TextBlock durText = new TextBlock()
                {
                    Text = string.Format("{0}m", (int)(actSec / 60.0)),
                    FontSize = 9,
                    Foreground = _theme.TextSecondary,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(durText, 2);
                row.Children.Add(durText);

                _hourlyMatrixStack.Children.Add(row);
            }
        }

        private Border CreateCompactApplicationRow(AppSessionRecord rec)
        {
            Border row = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 3),
                Cursor = Cursors.Hand
            };

            row.MouseEnter += delegate { row.Background = _theme.SurfaceCardHover; };
            row.MouseLeave += delegate { row.Background = _theme.SurfaceCard; };

            Grid mainGrid = new Grid();

            // Background meter
            Grid bgGrid = new Grid();
            double pct = Math.Max(0.5, Math.Min(100.0, rec.PercentOfTotal));
            bgGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(pct, GridUnitType.Star) });
            bgGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(Math.Max(0.1, 100.0 - pct), GridUnitType.Star) });
            Border meterFill = new Border()
            {
                Background = FocusPulseStyles.Brush(rec.ColorHex),
                Opacity = _theme.IsDark ? 0.10 : 0.06,
                CornerRadius = new CornerRadius(6, 0, 0, 6)
            };
            Grid.SetColumn(meterFill, 0);
            bgGrid.Children.Add(meterFill);
            mainGrid.Children.Add(bgGrid);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) }); // Rank
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(22) }); // Avatar
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // Name + Cat
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Micro Bar + Duration + Pct

            // Rank
            TextBlock rankTxt = new TextBlock()
            {
                Text = "#" + rec.Rank,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(rankTxt, 0);
            grid.Children.Add(rankTxt);

            // Glyph
            TextBlock glyphTxt = new TextBlock()
            {
                Text = rec.IconGlyph,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = FocusPulseStyles.Brush(rec.ColorHex),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(glyphTxt, 1);
            grid.Children.Add(glyphTxt);

            // Name + Category pill inline
            StackPanel namePanel = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock nameTxt = new TextBlock()
            {
                Text = rec.FriendlyName,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 135
            };
            namePanel.Children.Add(nameTxt);

            Border catPill = new Border()
            {
                Background = _theme.SurfaceElevated,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock catTxt = new TextBlock()
            {
                Text = rec.Category,
                FontSize = 7.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted
            };
            catPill.Child = catTxt;
            namePanel.Children.Add(catPill);

            Grid.SetColumn(namePanel, 2);
            grid.Children.Add(namePanel);

            // Stats cluster
            StackPanel statsPanel = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            ProgressBar miniBar = new ProgressBar()
            {
                Width = 36,
                Height = 3,
                Value = rec.PercentOfTotal,
                Foreground = FocusPulseStyles.Brush(rec.ColorHex),
                Background = _theme.SurfaceElevated,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            statsPanel.Children.Add(miniBar);

            TextBlock timeTxt = new TextBlock()
            {
                Text = rec.FormattedDuration,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = FocusPulseStyles.Brush(rec.ColorHex),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            statsPanel.Children.Add(timeTxt);

            TextBlock pctTxt = new TextBlock()
            {
                Text = rec.FormattedPercentage,
                FontSize = 8,
                Foreground = _theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center
            };
            statsPanel.Children.Add(pctTxt);

            Grid.SetColumn(statsPanel, 3);
            grid.Children.Add(statsPanel);

            mainGrid.Children.Add(grid);
            row.Child = mainGrid;
            return row;
        }

        private Border CreateApplicationRow(AppSessionRecord rec)
        {
            Border card = new Border()
            {
                Background = _theme.SurfaceCard,
                BorderBrush = _theme.SubtleBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(9, 7, 9, 7),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = Cursors.Hand
            };

            card.MouseEnter += delegate { card.Background = _theme.SurfaceCardHover; };
            card.MouseLeave += delegate { card.Background = _theme.SurfaceCard; };

            Grid rowGrid = new Grid();

            // Integrated background progress fill meter
            Grid bgGrid = new Grid();
            double pct = Math.Max(0.5, Math.Min(100.0, rec.PercentOfTotal));
            bgGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(pct, GridUnitType.Star) });
            bgGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(Math.Max(0.1, 100.0 - pct), GridUnitType.Star) });
            Border meterFill = new Border()
            {
                Background = FocusPulseStyles.Brush(rec.ColorHex),
                Opacity = _theme.IsDark ? 0.12 : 0.08,
                CornerRadius = new CornerRadius(8, 0, 0, 8)
            };
            Grid.SetColumn(meterFill, 0);
            bgGrid.Children.Add(meterFill);
            rowGrid.Children.Add(bgGrid);

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Rank
            header.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Avatar
            header.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // Name & Cat
            header.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // Duration & Pct

            // Rank Badge
            Border rankBadge = new Border()
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(5),
                Background = _theme.SurfaceElevated,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock rankText = new TextBlock()
            {
                Text = "#" + rec.Rank,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = _theme.TextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            rankBadge.Child = rankText;
            Grid.SetColumn(rankBadge, 0);
            header.Children.Add(rankBadge);

            // Avatar Tile
            Border avatar = new Border()
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(8),
                Background = _theme.SurfaceElevated,
                BorderBrush = FocusPulseStyles.Brush(rec.ColorHex),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock glyph = new TextBlock()
            {
                Text = rec.IconGlyph,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = FocusPulseStyles.Brush(rec.ColorHex),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            avatar.Child = glyph;
            Grid.SetColumn(avatar, 1);
            header.Children.Add(avatar);

            StackPanel titleStack = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            TextBlock nameBlock = new TextBlock()
            {
                Text = rec.FriendlyName,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            
            StackPanel metaSub = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock catBlock = new TextBlock()
            {
                Text = rec.Category,
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                Foreground = _theme.TextMuted,
                Margin = new Thickness(0, 0, 6, 0)
            };
            TextBlock prodTag = new TextBlock()
            {
                Text = rec.Productivity == ProductivityClass.FocusWork ? "● Deep Focus" : (rec.Productivity == ProductivityClass.Distraction ? "● Distraction" : "● General"),
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                Foreground = rec.Productivity == ProductivityClass.FocusWork ? _theme.FocusBadgeText : (rec.Productivity == ProductivityClass.Distraction ? _theme.DistractBadgeText : _theme.BrandAccent)
            };
            metaSub.Children.Add(catBlock);
            metaSub.Children.Add(prodTag);

            titleStack.Children.Add(nameBlock);
            titleStack.Children.Add(metaSub);
            Grid.SetColumn(titleStack, 2);
            header.Children.Add(titleStack);

            StackPanel statsStack = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            TextBlock timeBlock = new TextBlock()
            {
                Text = rec.FormattedDuration,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI Variable Display, Consolas, monospace"),
                Foreground = FocusPulseStyles.Brush(rec.ColorHex),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            TextBlock pctBlock = new TextBlock()
            {
                Text = rec.FormattedPercentage,
                FontSize = 8.5,
                Foreground = _theme.TextMuted,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            statsStack.Children.Add(timeBlock);
            statsStack.Children.Add(pctBlock);
            Grid.SetColumn(statsStack, 3);
            header.Children.Add(statsStack);

            rowGrid.Children.Add(header);

            card.Child = rowGrid;
            return card;
        }

        private void InitializeTrayMenu()
        {
            Forms.ContextMenu menu = new Forms.ContextMenu();

            menu.MenuItems.Add("Show FocusPulse Dashboard", delegate
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            });

            menu.MenuItems.Add("Toggle Dynamic Island HUD (Ctrl+M)", delegate
            {
                ToggleDisplayMode();
                this.Show();
                this.Activate();
            });

            Forms.MenuItem pinItem = new Forms.MenuItem("Always on Top");
            pinItem.Checked = this.Topmost;
            pinItem.Click += delegate
            {
                ToggleAlwaysOnTop();
                pinItem.Checked = this.Topmost;
            };
            menu.MenuItems.Add(pinItem);

            menu.MenuItems.Add("-");

            menu.MenuItems.Add("Start 25m Focus Sprint", delegate
            {
                _engine.StartFlowSprint(25, "25m Focus Sprint");
                this.Show();
                NavigateToTab(1);
            });

            menu.MenuItems.Add("Pause / Resume Tracking", delegate
            {
                _engine.IsPaused = !_engine.IsPaused;
                if (_pauseIconText != null)
                {
                    _pauseIconText.Text = _engine.IsPaused ? "▶" : "❚❚";
                }
            });

            menu.MenuItems.Add("-");

            menu.MenuItems.Add("Exit FocusPulse", delegate
            {
                this.Close();
            });

            _notifyIcon.ContextMenu = menu;
            _notifyIcon.DoubleClick += delegate
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    _config = ser.Deserialize<PulseConfig>(json);
                    if (_config != null)
                    {
                        if (_config.Width < 340 || _config.Height < 520)
                        {
                            _config.Width = 400;
                            _config.Height = 740;
                        }
                        return;
                    }
                }
            }
            catch { }

            _config = new PulseConfig();
        }

        private void SaveConfiguration()
        {
            try
            {
                _config.Left = this.Left;
                _config.Top = this.Top;
                if (!_isMiniMode && this.Width > 320 && this.Height > 450)
                {
                    _config.Width = this.Width;
                    _config.Height = this.Height;
                }
                _config.AlwaysOnTop = this.Topmost;
                _config.ShowInTaskbar = this.ShowInTaskbar;
                _config.Opacity = this.Opacity;
                _config.ThemeName = _theme.Key;
                _config.IsMiniMode = _isMiniMode;
                _config.IsPrivacyShieldActive = _engine.IsPrivacyShield;
                _config.ActiveProjectWorkspace = _engine.ActiveProjectWorkspace;
                _config.AppCategoryFilter = _activeCategoryFilter;
                _config.IsCompactAppsView = _config.IsCompactAppsView;
                _config.AppSortOrder = _config.AppSortOrder;
                _config.PomodoroWorkMinutes = _engine.PomodoroWorkMinutes;
                _config.PomodoroBreakMinutes = _engine.PomodoroBreakMinutes;
                _config.PomodoroLongBreakMinutes = _engine.PomodoroLongBreakMinutes;
                _config.SoundChimesEnabled = _engine.SoundChimesEnabled;

                string dir = System.IO.Path.GetDirectoryName(_configFilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JavaScriptSerializer ser = new JavaScriptSerializer();
                string json = ser.Serialize(_config);
                File.WriteAllText(_configFilePath, json);
            }
            catch { }
        }
    }
}
