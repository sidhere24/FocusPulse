# FocusPulse

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com)
[![Architecture](https://img.shields.io/badge/Architecture-x64%20%7C%20x86%20%7C%20ARM64-green.svg)](https://github.com)
[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-Ready-blue.svg)](store/STORE_PUBLISHING_GUIDE.md)

> A modern, glassmorphic desktop and taskbar focus companion. Featuring an ultra-fast, zero-overhead native Win32 tracking engine for Windows (`x64`, `x86`, `ARM64`), plus seamless zero-dependency native support for **macOS** and **Linux**.

---

## ✦ Core Features & Capabilities

### 1. High-Precision Native Engine (24/7 Continuous Tracking)
- **Zero Overhead**: Direct native OS interop without heavy web runtimes. Runs in < 18 MB RAM with near 0% CPU consumption.
- **Dedicated Background Worker**: Tracking runs on an independent worker thread completely decoupled from the UI thread, ensuring continuous 24/7 accuracy even when the window is in the background or hidden in the system tray.
- **Smart Focus Isolation**: The engine ignores its own process ID, so inspecting your dashboard, launching sprints, or tweaking settings never interrupts your real workflow.

### 2. Deep Flow, Pomodoro Engine & Task Intelligence
- **🍅 Complete Pomodoro Work / Break Interval Engine**:
  - Full Pomodoro state machine: **Focus (25m / custom)** ➔ **Short Break (5m)** ➔ **Long Break (15m after 4 cycles)**.
  - Visual 4-cycle micro-indicators (`[ ● ] [ ○ ] [ ○ ] [ ○ ]`) tracking consecutive deep work intervals.
  - High-visibility digital countdown timer (`24:59`) with Play, Pause, Skip Break, and Reset controls.
  - Linked Focus Target banner (`🎯 Target: <Task Name>`) connecting Pomodoro sprints directly to your actionable tasks.
- **📝 Today's Priorities (Integrated Actionable Task Checklist)**:
  - Quick-add focus tasks with priority tags: `🔴 High`, `🟡 Med`, and `🟢 Low`.
  - Interactive checkboxes with smooth text strike-through animation and dimming.
  - Pomodoro counter badge (`🍅 2`) incremented automatically upon sprint completion.
  - 1-click **🎯 Focus** button linking any task to the active Pomodoro interval.
  - Task progress bar and ratio summary (`2/3 Done (67%)`).
- **🔔 Pleasant Acoustic Audio Chimes**:
  - Ascending celebratory musical triad chime on focus completion (`C5-E5-G5-C6`).
  - Gentle dual-prompt return-to-work tone on break completion (`G5-E5`).
  - Non-blocking synthesized audio thread with zero latency and fallback support.
- **🔥 7-Day Consistency & Active Streak Engine (Timeline Tab)**:
  - Active Streak Flame badge (`🔥 4 DAY STREAK`) tracking daily focus discipline.
  - 7 vertical day bars (Mon–Sun) with hours worked, today dynamically highlighted in your active `BrandAccent`.
- **🏆 Daily Focus Flow Score (0–100%)**: Evaluates focus quality in real time based on deep work ratio and context-switching velocity (application switches per hour).
- **📊 Automated Classification**: Automatically groups active applications and browser tabs into **Focus Work** (IDEs, VS Code, Terminal, GitHub, ChatGPT), **General**, and **Distraction** (Steam, Netflix, YouTube).
- **🧘 Ergonomic Wellness Coach**: Tracks continuous screen time and prompts for a healthy 5-minute stretch when reaching 50 minutes of uninterrupted work.
- **⏱ Chronological Work Matrix (8 AM – 8 PM)**: Visual hour-by-hour timeline displaying active duration and focused work density.
- **🏷 Workspace Tagging**: Categorize sessions into `[Development]`, `[Engineering]`, `[Design]`, `[Research]`, or `[Personal]`.
- **🛡️ Privacy Shield & Pause**: Mask confidential window titles to `"Private Workspace"` or pause tracking with one click.

### 3. Apple & Google Staff-Tier Craft & Ergonomics
- **Dual View Modes (Density Toggle)**:
  - **`☰ Compact Mode`**: Apple Activity Monitor / Spotlight style 34px rows displaying **10 to 20 apps simultaneously** on screen with zero scrolling required.
  - **`⊞ Cards Mode`**: Spacious 48px cards with deep focus badges, live session percentages, and micro progress bars.
- **Global Fluid Mouse Wheel Navigation**: Turning the mouse wheel anywhere over the Overview tab effortlessly scrolls the application list with sub-pixel inertia (`CanContentScroll = false`, `PanningMode = VerticalOnly`).
- **Instant Spotlight Search & Category Filtering**:
  - Real-time search omnibar (`Ctrl+F`) with clear `✕` button.
  - Category filter chips: `All`, `Focus`, `Browsing`, `Engineering`, `Office`, `System`, `Distraction`.
  - Sort order switcher: Toggle between `⚡ Duration` and `🔤 Name`.
- **Decoupled Appearance Modes & Dynamic Color Palettes**:
  - **Appearance Mode Switcher**: Instant 1-click toggle between **`☀️ Light Mode`** (Cupertino glass platinum `#F8FAFC`, pure white cards `#FFFFFF`) and **`🌙 Dark Mode`** (Deep obsidian night `#0A0E17`, dark glass surfaces `#121927`).
  - **6 User-Selectable Accent Color Palettes**:
    - 🔵 **Electric Sapphire** (`#0A84FF`): Apple flagship electric ocean blue & azure.
    - 🟣 **Tokyo Violet** (`#A855F7`): Cyberpunk neon purple & glowing lilac.
    - 🟢 **Pixel Emerald** (`#10B981`): Android Material You vibrant emerald & spring mint.
    - 🟠 **Sunset Tangerine** (`#F97316`): Warm solar amber & citrus orange.
    - 🔴 **Rose Quartz** (`#F43F5E`): Vivid ruby crimson & energetic coral.
    - ⚪ **Studio Slate** (`#64748B`): Modern monochrome neutral slate.
  - Mix and match any mode with any palette (12 total permutations) with instant 60 FPS live re-rendering.
- **Ultra-Thin Floating Pill Scrollbar**:
  - 5px iOS 18 / Material You floating capsule thumb (`CornerRadius="2.5"`). Zero legacy Windows 95 arrow boxes, zero track background.
- **Zero Corner Artifacts**:
  - Anti-aliased 16px rounded borders floating cleanly over the desktop wallpaper with 100% transparent corners and soft drop shadows. No black triangles or checkered resize grips.
- **Single-Row Horizontal Category Filter Strip**:
  - Horizontally scrollable single-row capsule chips with mouse-wheel support (`All`, `Focus`, `Browsing`, `Engineering`, `Office`, `System`, `Distraction`). No multiline wrapping.

---

## 🚀 Quick Start by Platform

### 🪟 Windows (Native WPF & Win32)
Double-click `run.bat` (automatically detects CPU architecture) or run directly:
- **x64 (Modern 64-bit Intel/AMD)**: `bin\FocusPulse-x64.exe`
- **x86 (Legacy 32-bit Windows)**: `bin\FocusPulse-x86.exe`
- **ARM64 (Qualcomm Snapdragon X / Surface ARM)**: `bin\FocusPulse-ARM64.exe`

### 🍎 macOS (Apple Silicon M1/M2/M3/M4 & Intel)
Run the native companion launcher in Terminal:
```bash
chmod +x run.sh
./run.sh
```
*Zero pip installs required — uses pure Python 3 and native `osascript` Apple Events.*

### 🐧 Linux (Ubuntu, Debian, Fedora, Arch, X11 & Wayland)
Run the native companion launcher:
```bash
chmod +x run.sh
./run.sh
```
*Supports X11 via `xprop` and Wayland via Hyprland/Sway/GNOME Shell.*

---

## 🛒 Publishing to Microsoft Store
Everything is pre-configured in the [`store/`](store/) folder:
- Follow the step-by-step instructions in [`store/STORE_PUBLISHING_GUIDE.md`](store/STORE_PUBLISHING_GUIDE.md).
- Visual assets pre-rendered in [`store/assets/`](store/assets/).
- Inno Setup script [`store/FocusPulseInstaller.iss`](store/FocusPulseInstaller.iss) for standard Win32 submission.
- MSIX manifest [`store/AppxManifest.xml`](store/AppxManifest.xml) for Store-native packaging.

---

## 🛠 Rebuilding from Source

### Windows:
The project includes an automated PowerShell build script that invokes the native Windows C# compiler (`csc.exe`) without requiring Visual Studio:
```powershell
powershell.exe -ExecutionPolicy Bypass -File .\build.ps1
```

All three architecture targets compile in under 3 seconds:
```text
[1/3] Building target: x64 (bin\FocusPulse-x64.exe)... -> Success (155 KB)
[2/3] Building target: x86 (bin\FocusPulse-x86.exe)... -> Success (155.5 KB)
[3/3] Building target: ARM64 / Universal (bin\FocusPulse-ARM64.exe)... -> Success (155.5 KB)
```

---

## 📂 Project Architecture

```
Activitywatch/
│
├── README.md                           # Documentation & usage guide
├── PRODUCT_SPECIFICATION.md           # Product specification & feature architecture
├── SYSTEM_ARCHITECTURE.md             # Native Win32 Subsystem & engine specifications
├── build.ps1                          # Cross-compilation script (x86, x64, ARM64)
├── run.bat                            # One-click architecture-detecting launcher
│
├── bin/                               # Standalone native executables
│   ├── FocusPulse-x64.exe             # 64-bit AMD64 binary
│   ├── FocusPulse-x86.exe             # 32-bit Intel binary
│   └── FocusPulse-ARM64.exe           # Native ARM64 / AnyCPU binary
│
├── src/                               # C# / WPF source files
│   ├── NativeMethods.cs               # Win32 P/Invoke signatures (user32 / kernel32)
│   ├── ActivityEngine.cs              # Native activity tracking & focus algorithm
│   ├── SyncClient.cs                  # Local node synchronization client
│   ├── FocusPulseModels.cs            # Data models, DaySummary, FlowSprint
│   ├── FocusPulseStyles.cs            # Linear/Raycast design system & themes
│   ├── MainWindow.cs                  # Segmented dashboard, Mini Island HUD, system tray
│   ├── App.cs                         # Application entry point with single-instance mutex
│   └── app.manifest                   # PerMonitorV2 high-DPI awareness & Windows 10/11 manifest
│
└── widgets/                           # Windows 11 Native Widget Board (Win + W)
    ├── adaptive-card-widget.json      # Adaptive Cards 1.5 layout template
    ├── widget-sample-data.json        # Live data binding schema
    └── WIDGET_BOARD_INTEGRATION.md    # Windows App SDK provider integration guide
```

---

## 🎛 Keyboard & Mouse Controls

- **Click & Drag**: Move the widget to any position on your screen.
- **`Overview` Tab**: Active app hero tile, metrics (Active, Focus, Breaks), flow profile gauge, and top applications.
- **`Focus` Tab**: Focus Score badge, sprint timers (`25m Sprint`, `50m Deep Flow`), daily 4h target, and wellness coach banner.
- **`Timeline` Tab**: 12-hour work matrix (8 AM – 8 PM) and workspace tag selector.
- **`Settings` Tab**: Window display modes, surface themes, opacity levels, and data management.
- **`⏸` / `▶`**: Pause or resume tracking instantly.
- **`🛡️`**: Toggle Privacy Shield to mask window titles.
- **`📌` / `🪟`**: Toggle Always-on-Top versus Normal Desktop Window.
- **`◩` / `◳`**: Switch between Full Dashboard and Mini Island HUD.
- **Tray Right-Click**: Quick actions for focus sprints, privacy mode, and window visibility.
