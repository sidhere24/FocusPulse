#!/usr/bin/env python3
"""
FocusPulse - Cross-Platform Activity & Deep Work Companion
Supports: Linux (X11 & Wayland), macOS (Apple Silicon & Intel), Windows
Zero external dependencies: Pure Python 3 standard library.
"""

import os
import sys
import time
import json
import socket
import platform
import subprocess
import threading
from datetime import datetime
from http.server import HTTPServer, BaseHTTPRequestHandler
import urllib.parse

OS_TYPE = platform.system()  # 'Linux', 'Darwin', 'Windows'

def get_config_dir():
    if OS_TYPE == "Darwin":
        p = os.path.expanduser("~/Library/Application Support/FocusPulse")
    elif OS_TYPE == "Windows":
        p = os.path.join(os.environ.get("LOCALAPPDATA", os.path.expanduser("~")), "FocusPulse")
    else:
        p = os.path.expanduser("~/.config/focuspulse")
    os.makedirs(p, exist_ok=True)
    return p

CONFIG_DIR = get_config_dir()
CONFIG_FILE = os.path.join(CONFIG_DIR, "pulse_config.json")
METRICS_FILE = os.path.join(CONFIG_DIR, "day_metrics.json")

# Default Categories
CATEGORIES = {
    # Engineering & Coding
    "code": "Engineering", "vscode": "Engineering", "antigravity": "Engineering",
    "cursor": "Engineering", "windsurf": "Engineering", "pycharm": "Engineering",
    "terminal": "Engineering", "iterm2": "Engineering", "bash": "Engineering",
    "zsh": "Engineering", "gnome-terminal": "Engineering", "alacritty": "Engineering",
    "kitty": "Engineering", "neovim": "Engineering", "vim": "Engineering",
    "postman": "Engineering", "docker": "Engineering", "sublime_text": "Engineering",
    # Browsing
    "google-chrome": "Browsing", "chrome": "Browsing", "firefox": "Browsing",
    "brave": "Browsing", "brave-browser": "Browsing", "safari": "Browsing",
    "microsoft-edge": "Browsing", "msedge": "Browsing", "arc": "Browsing", "opera": "Browsing",
    # Office & Productivity
    "notion": "Office", "obsidian": "Office", "slack": "Office", "teams": "Office",
    "zoom": "Office", "discord": "Office", "figma": "Office", "libreoffice": "Office",
    "pages": "Office", "numbers": "Office", "keynote": "Office", "mail": "Office",
    # System
    "finder": "System", "nautilus": "System", "dolphin": "System", "thunar": "System",
    "systemsettings": "System", "activity monitor": "System", "htop": "System",
    # Distraction
    "steam": "Distraction", "spotify": "Distraction", "netflix": "Distraction",
    "vlc": "Distraction", "mpv": "Distraction"
}

def classify_app(app_name, title=""):
    app_lower = app_name.lower().replace(".exe", "").strip()
    title_lower = title.lower().strip()

    if app_lower in CATEGORIES:
        return CATEGORIES[app_lower]

    for key, cat in CATEGORIES.items():
        if key in app_lower:
            return cat

    if any(k in title_lower for k in ["github", "stackoverflow", "gitlab", "pull request", "terminal", "debugger"]):
        return "Engineering"
    if any(k in title_lower for k in ["youtube", "twitch", "reddit", "twitter", "tiktok", "instagram"]):
        return "Distraction"
    if any(k in title_lower for k in ["docs.google.com", "spreadsheet", "jira", "confluence"]):
        return "Office"

    return "General"

def send_notification(title, message):
    try:
        if OS_TYPE == "Darwin":
            cmd = f'display notification "{message}" with title "{title}"'
            subprocess.run(["osascript", "-e", cmd], capture_output=True, check=False)
        elif OS_TYPE == "Linux":
            subprocess.run(["notify-send", title, message], capture_output=True, check=False)
    except Exception:
        pass

class ActivityEngine:
    def __init__(self):
        self.lock = threading.Lock()
        self.active_app = "Desktop"
        self.active_title = "FocusPulse Companion"
        self.active_category = "General"
        self.session_seconds = 0
        self.total_active_seconds = 0
        self.total_idle_seconds = 0
        self.focus_work_seconds = 0
        self.general_seconds = 0
        self.distraction_seconds = 0
        self.window_switches = 0
        self.app_durations = {}
        self.hourly_active = [0] * 24
        self.hourly_focus = [0] * 24
        self.last_switch_time = time.time()
        self.is_running = True

        # Sprint state
        self.sprint_active = False
        self.sprint_total_seconds = 0
        self.sprint_remaining_seconds = 0
        self.sprint_type = ""

        self.load_metrics()

    def load_metrics(self):
        if os.path.exists(METRICS_FILE):
            try:
                with open(METRICS_FILE, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    today = datetime.now().strftime("%Y-%m-%d")
                    if data.get("Date") == today:
                        self.total_active_seconds = data.get("TotalActiveSeconds", 0)
                        self.total_idle_seconds = data.get("TotalIdleSeconds", 0)
                        self.focus_work_seconds = data.get("FocusWorkSeconds", 0)
                        self.general_seconds = data.get("GeneralSeconds", 0)
                        self.distraction_seconds = data.get("DistractionSeconds", 0)
                        self.window_switches = data.get("WindowSwitches", 0)
                        self.app_durations = data.get("AppDurations", {})
                        self.hourly_active = data.get("HourlyActiveSeconds", [0]*24)
                        self.hourly_focus = data.get("HourlyFocusSeconds", [0]*24)
            except Exception:
                pass

    def save_metrics(self):
        with self.lock:
            data = {
                "Date": datetime.now().strftime("%Y-%m-%d"),
                "TotalActiveSeconds": self.total_active_seconds,
                "TotalIdleSeconds": self.total_idle_seconds,
                "FocusWorkSeconds": self.focus_work_seconds,
                "GeneralSeconds": self.general_seconds,
                "DistractionSeconds": self.distraction_seconds,
                "WindowSwitches": self.window_switches,
                "AppDurations": self.app_durations,
                "HourlyActiveSeconds": self.hourly_active,
                "HourlyFocusSeconds": self.hourly_focus
            }
            try:
                with open(METRICS_FILE, "w", encoding="utf-8") as f:
                    json.dump(data, f, indent=2)
            except Exception:
                pass

    def get_frontmost_window(self):
        if OS_TYPE == "Darwin":
            try:
                script = """
                tell application "System Events"
                    set frontApp to first application process whose frontmost is true
                    set appName to name of frontApp
                    set winTitle to ""
                    try
                        tell frontApp to set winTitle to name of front window
                    end try
                    return appName & "|||" & winTitle
                end tell
                """
                res = subprocess.check_output(["osascript", "-e", script], text=True, timeout=1.5).strip()
                if "|||" in res:
                    parts = res.split("|||", 1)
                    return parts[0].strip(), parts[1].strip()
            except Exception:
                return "Finder", "Desktop"

        elif OS_TYPE == "Linux":
            # 1. Wayland Check (Sway / Hyprland)
            if os.environ.get("SWAYSOCK"):
                try:
                    tree = json.loads(subprocess.check_output(["swaymsg", "-t", "get_tree"], text=True, timeout=1))
                    def find_focused(node):
                        if node.get("focused"): return node
                        for c in node.get("nodes", []) + node.get("floating_nodes", []):
                            f = find_focused(c)
                            if f: return f
                        return None
                    fn = find_focused(tree)
                    if fn:
                        return fn.get("app_id") or fn.get("name") or "Unknown", fn.get("name") or ""
                except Exception:
                    pass

            if os.environ.get("HYPRLAND_INSTANCE_SIGNATURE"):
                try:
                    active = json.loads(subprocess.check_output(["hyprctl", "activewindow", "-j"], text=True, timeout=1))
                    return active.get("class", "Unknown"), active.get("title", "")
                except Exception:
                    pass

            # 2. X11 Standard Query via xprop
            try:
                root_out = subprocess.check_output(["xprop", "-root", "_NET_ACTIVE_WINDOW"], text=True, timeout=1)
                win_id = root_out.split()[-1]
                if win_id and win_id != "0x0":
                    prop_out = subprocess.check_output(["xprop", "-id", win_id, "WM_CLASS", "WM_NAME", "_NET_WM_NAME"], text=True, timeout=1)
                    app_name = "Desktop"
                    title = ""
                    for line in prop_out.splitlines():
                        if "WM_CLASS" in line:
                            parts = line.split("=")
                            if len(parts) > 1:
                                app_name = parts[1].replace('"', '').split(",")[-1].strip()
                        elif "WM_NAME" in line and not title:
                            parts = line.split("=")
                            if len(parts) > 1:
                                title = parts[1].replace('"', '').strip()
                    return app_name, title
            except Exception:
                pass

            # Fallback
            return "Linux Desktop", "Active Session"

        return "FocusPulse", "Active"

    def tick(self):
        app, title = self.get_frontmost_window()
        cat = classify_app(app, title)

        with self.lock:
            if app != self.active_app or title != self.active_title:
                self.active_app = app
                self.active_title = title
                self.active_category = cat
                self.session_seconds = 0
                self.window_switches += 1
            else:
                self.session_seconds += 1

            self.total_active_seconds += 1
            self.app_durations[app] = self.app_durations.get(app, 0) + 1

            hr = datetime.now().hour
            self.hourly_active[hr] += 1

            if cat in ("Engineering", "Office"):
                self.focus_work_seconds += 1
                self.hourly_focus[hr] += 1
            elif cat == "Distraction":
                self.distraction_seconds += 1
            else:
                self.general_seconds += 1

            # Handle sprint countdown
            if self.sprint_active:
                self.sprint_remaining_seconds -= 1
                if self.sprint_remaining_seconds <= 0:
                    self.sprint_active = False
                    send_notification("🏆 Flow Sprint Complete!", f"Awesome work! You completed your {self.sprint_type} sprint.")
                elif cat == "Distraction" and self.sprint_remaining_seconds % 30 == 0:
                    send_notification("🛡️ FocusPulse Shield Alert", f"Distraction detected ({app}) during active sprint!")

    def start_sprint(self, minutes):
        with self.lock:
            self.sprint_active = True
            self.sprint_total_seconds = minutes * 60
            self.sprint_remaining_seconds = minutes * 60
            self.sprint_type = f"{minutes}m Deep Flow"
        send_notification("⚡ Sprint Started", f"{minutes}-minute deep focus sprint activated.")

    def cancel_sprint(self):
        with self.lock:
            self.sprint_active = False
            self.sprint_remaining_seconds = 0

    def get_snapshot(self):
        with self.lock:
            tot = max(1, self.total_active_seconds)
            focus_pct = int((self.focus_work_seconds / tot) * 100)
            score = min(100, max(0, int(focus_pct - (self.window_switches / max(1, tot / 3600)) * 2)))

            sorted_apps = sorted(
                [{"name": k, "duration": v, "category": classify_app(k), "percent": round((v / tot) * 100, 1)}
                 for k, v in self.app_durations.items()],
                key=lambda x: x["duration"],
                reverse=True
            )

            return {
                "active_app": self.active_app,
                "active_title": self.active_title,
                "active_category": self.active_category,
                "session_seconds": self.session_seconds,
                "total_active_seconds": self.total_active_seconds,
                "total_idle_seconds": self.total_idle_seconds,
                "focus_work_seconds": self.focus_work_seconds,
                "general_seconds": self.general_seconds,
                "distraction_seconds": self.distraction_seconds,
                "window_switches": self.window_switches,
                "focus_score": score,
                "sprint": {
                    "active": self.sprint_active,
                    "type": self.sprint_type,
                    "remaining": self.sprint_remaining_seconds,
                    "total": self.sprint_total_seconds,
                    "progress": int(((self.sprint_total_seconds - self.sprint_remaining_seconds) / max(1, self.sprint_total_seconds)) * 100) if self.sprint_active else 0
                },
                "apps": sorted_apps,
                "hourly_active": self.hourly_active,
                "hourly_focus": self.hourly_focus,
                "os": OS_TYPE
            }

engine = ActivityEngine()

def tracking_loop():
    while engine.is_running:
        engine.tick()
        if engine.total_active_seconds % 10 == 0:
            engine.save_metrics()
        time.sleep(1)

# HTML5 Web HUD with Decoupled Light/Dark Appearance & 6 Accent Palettes
HTML_DASHBOARD = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>FocusPulse Companion</title>
  <style>
    :root {
      --canvas: #0A0E17;
      --card: #121927;
      --card-hover: #1A2336;
      --elevated: #222E45;
      --border: #25334C;
      --text-pri: #F8FAFC;
      --text-sec: #94A3B8;
      --text-mut: #64748B;
      --accent: #0A84FF;
      --focus: #30D158;
      --distract: #FF453A;
      --amber: #FBBF24;
    }
    body.light {
      --canvas: #F8FAFC;
      --card: #FFFFFF;
      --card-hover: #F1F5F9;
      --elevated: #E2E8F0;
      --border: #E2E8F0;
      --text-pri: #0F172A;
      --text-sec: #475569;
      --text-mut: #94A3B8;
      --focus: #047857;
      --distract: #BE123C;
      --amber: #B45309;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI Variable Display", "Segoe UI", sans-serif; }
    body { background: var(--canvas); color: var(--text-pri); min-height: 100vh; display: flex; justify-content: center; padding: 24px 16px; transition: all 0.2s ease; }
    .widget-shell { width: 100%; max-width: 440px; background: var(--canvas); border: 1px solid var(--border); border-radius: 20px; box-shadow: 0 16px 40px rgba(0,0,0,0.3); padding: 18px; display: flex; flex-direction: column; gap: 14px; }
    .header { display: flex; justify-content: space-between; align-items: center; }
    .brand { display: flex; align-items: center; gap: 8px; font-weight: 700; font-size: 15px; }
    .pulse-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--focus); box-shadow: 0 0 10px var(--focus); }
    .status-badge { font-size: 10px; font-weight: 700; text-transform: uppercase; background: rgba(16,185,129,0.15); color: var(--focus); padding: 3px 8px; border-radius: 12px; border: 1px solid rgba(16,185,129,0.3); }
    
    .nav { display: grid; grid-template-columns: repeat(4, 1fr); background: var(--card); padding: 3px; border-radius: 10px; border: 1px solid var(--border); }
    .nav button { background: none; border: none; color: var(--text-sec); font-size: 11px; font-weight: 600; padding: 7px 0; border-radius: 7px; cursor: pointer; transition: 0.15s; }
    .nav button.active { background: var(--elevated); color: var(--text-pri); font-weight: 700; border: 1px solid var(--border); }
    
    .card { background: var(--card); border: 1px solid var(--border); border-radius: 14px; padding: 14px; display: flex; flex-direction: column; gap: 8px; transition: 0.15s; }
    .card:hover { border-color: var(--accent); }
    .hero-title { font-size: 16px; font-weight: 700; }
    .hero-sub { font-size: 11px; color: var(--text-sec); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .session-timer { font-size: 13px; font-weight: 700; color: var(--accent); }
    
    .metrics-row { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 8px; }
    .metric-box { background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 10px; text-align: center; }
    .metric-val { font-size: 16px; font-weight: 800; }
    .metric-lbl { font-size: 9px; font-weight: 700; text-transform: uppercase; color: var(--text-mut); margin-top: 2px; }

    .ratio-bar { height: 6px; background: var(--elevated); border-radius: 3px; overflow: hidden; display: flex; }
    .ratio-focus { background: var(--focus); height: 100%; transition: width 0.3s; }
    .ratio-gen { background: var(--accent); height: 100%; transition: width 0.3s; }
    .ratio-dist { background: var(--distract); height: 100%; transition: width 0.3s; }

    .chip-strip { display: flex; gap: 6px; overflow-x: auto; padding-bottom: 2px; scrollbar-width: none; }
    .chip { font-size: 10px; font-weight: 600; padding: 4px 10px; border-radius: 12px; background: var(--elevated); border: 1px solid var(--border); color: var(--text-sec); cursor: pointer; white-space: nowrap; }
    .chip.active { background: var(--accent); color: #fff; font-weight: 700; border-color: var(--accent); }

    .search-bar { background: var(--elevated); border: 1px solid var(--border); border-radius: 8px; padding: 6px 10px; display: flex; align-items: center; gap: 6px; }
    .search-bar input { background: none; border: none; color: var(--text-pri); font-size: 11px; width: 100%; outline: none; }

    .apps-list { display: flex; flex-direction: column; gap: 6px; max-height: 280px; overflow-y: auto; padding-right: 4px; }
    .apps-list::-webkit-scrollbar { width: 4px; }
    .apps-list::-webkit-scrollbar-thumb { background: rgba(128,128,128,0.3); border-radius: 2px; }
    .app-item { background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 8px 10px; display: flex; justify-content: space-between; align-items: center; }
    .app-item:hover { background: var(--card-hover); }
    .app-name { font-size: 12px; font-weight: 600; }
    .app-meta { font-size: 10px; color: var(--text-mut); }
    .app-dur { font-size: 12px; font-weight: 700; text-align: right; }

    .palettes-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
    .palette-btn { background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 10px; display: flex; align-items: center; gap: 8px; cursor: pointer; }
    .palette-btn.active { border-color: var(--accent); background: var(--elevated); }
    .swatch { width: 14px; height: 14px; border-radius: 50%; }

    .mode-toggle { display: grid; grid-template-columns: 1fr 1fr; gap: 6px; }
    .mode-btn { background: var(--elevated); border: 1px solid var(--border); border-radius: 8px; padding: 8px; text-align: center; font-size: 11px; font-weight: 700; cursor: pointer; color: var(--text-sec); }
    .mode-btn.active { background: var(--accent); color: #fff; border-color: var(--accent); }

    .footer { display: flex; justify-content: space-between; font-size: 10px; color: var(--text-mut); padding-top: 4px; }
  </style>
</head>
<body>
  <div class="widget-shell">
    <div class="header">
      <div class="brand">
        <div class="pulse-dot"></div>
        <span>FocusPulse</span>
      </div>
      <div class="status-badge" id="hudStatus">Active Tracking</div>
    </div>

    <div class="nav">
      <button class="active" onclick="setTab('overview')">Overview</button>
      <button onclick="setTab('focus')">Focus</button>
      <button onclick="setTab('timeline')">Timeline</button>
      <button onclick="setTab('settings')">Settings</button>
    </div>

    <!-- OVERVIEW TAB -->
    <div id="tabOverview" style="display: flex; flex-direction: column; gap: 10px;">
      <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <div class="hero-title" id="activeAppName">Loading...</div>
          <div class="status-badge" id="activeCategoryBadge">General</div>
        </div>
        <div class="hero-sub" id="activeWinTitle">Detecting frontmost window...</div>
        <div class="session-timer" id="sessionTimer">00:00:00</div>
      </div>

      <div class="metrics-row">
        <div class="metric-box">
          <div class="metric-val" id="metricActive" style="color: var(--focus);">0m</div>
          <div class="metric-lbl">Active</div>
        </div>
        <div class="metric-box">
          <div class="metric-val" id="metricFocus" style="color: var(--accent);">0m</div>
          <div class="metric-lbl">Deep Focus</div>
        </div>
        <div class="metric-box">
          <div class="metric-val" id="metricScore" style="color: var(--amber);">0%</div>
          <div class="metric-lbl">Flow Score</div>
        </div>
      </div>

      <div class="ratio-bar">
        <div class="ratio-focus" id="barFocus" style="width: 50%;"></div>
        <div class="ratio-gen" id="barGen" style="width: 30%;"></div>
        <div class="ratio-dist" id="barDist" style="width: 20%;"></div>
      </div>

      <div class="search-bar">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
        <input type="text" id="appSearch" placeholder="Filter applications..." oninput="renderApps()">
      </div>

      <div class="chip-strip" id="chipStrip">
        <div class="chip active" onclick="setFilter('All')">All</div>
        <div class="chip" onclick="setFilter('Engineering')">Engineering</div>
        <div class="chip" onclick="setFilter('Browsing')">Browsing</div>
        <div class="chip" onclick="setFilter('Office')">Office</div>
        <div class="chip" onclick="setFilter('System')">System</div>
        <div class="chip" onclick="setFilter('Distraction')">Distraction</div>
      </div>

      <div class="apps-list" id="appsContainer"></div>
    </div>

    <!-- FOCUS TAB -->
    <div id="tabFocus" style="display: none; flex-direction: column; gap: 10px;">
      <div class="card" style="text-align: center; padding: 20px;">
        <div style="font-size: 11px; font-weight: 700; color: var(--text-mut);">DAILY FLOW SCORE</div>
        <div style="font-size: 40px; font-weight: 900; color: var(--focus);" id="bigScore">94</div>
        <div style="font-size: 12px; color: var(--text-sec);" id="scoreDesc">Optimal Deep Work Velocity</div>
      </div>
      <div class="card">
        <div style="font-weight: 700; font-size: 13px;">⚡ POMODORO FLOW SPRINTS</div>
        <div style="font-size: 11px; color: var(--text-sec);">Structured intervals with automated distraction defense.</div>
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px; margin-top: 6px;">
          <button style="background: var(--elevated); border: 1px solid var(--border); color: var(--text-pri); padding: 10px; border-radius: 8px; font-weight: 700; cursor: pointer;" onclick="startSprint(25)">25m Sprint</button>
          <button style="background: var(--accent); border: none; color: #fff; padding: 10px; border-radius: 8px; font-weight: 700; cursor: pointer;" onclick="startSprint(50)">50m Deep Flow</button>
        </div>
        <div id="sprintActiveBox" style="display: none; margin-top: 10px; padding: 10px; background: var(--elevated); border-radius: 8px;">
          <div style="display: flex; justify-content: space-between; font-size: 12px; font-weight: 700;">
            <span id="sprintTitle">Active Sprint</span>
            <span id="sprintRemaining" style="color: var(--accent);">00:00</span>
          </div>
          <button style="margin-top: 8px; width: 100%; background: #be123c; border: none; color: #fff; padding: 6px; border-radius: 6px; font-size: 11px; font-weight: 700; cursor: pointer;" onclick="cancelSprint()">Cancel Sprint</button>
        </div>
      </div>
    </div>

    <!-- TIMELINE TAB -->
    <div id="tabTimeline" style="display: none; flex-direction: column; gap: 10px;">
      <div class="card">
        <div style="font-size: 11px; font-weight: 700; color: var(--text-mut);">HOURLY ACTIVITY MATRIX (8 AM - 8 PM)</div>
        <div id="timelineBars" style="display: flex; flex-direction: column; gap: 6px; margin-top: 8px;"></div>
      </div>
    </div>

    <!-- SETTINGS TAB -->
    <div id="tabSettings" style="display: none; flex-direction: column; gap: 12px;">
      <div class="card">
        <div style="font-size: 11px; font-weight: 700; color: var(--text-mut);">APPEARANCE MODE</div>
        <div class="mode-toggle">
          <div class="mode-btn" id="btnLight" onclick="setMode(false)">☀️ Light Mode</div>
          <div class="mode-btn active" id="btnDark" onclick="setMode(true)">🌙 Dark Mode</div>
        </div>
      </div>

      <div class="card">
        <div style="font-size: 11px; font-weight: 700; color: var(--text-mut); margin-bottom: 4px;">ACCENT COLOR PALETTES</div>
        <div class="palettes-grid">
          <div class="palette-btn active" id="pal-Sapphire" onclick="setPalette('Sapphire', '#0A84FF')">
            <div class="swatch" style="background: #0A84FF;"></div>
            <div style="font-size: 11px; font-weight: 600;">Sapphire</div>
          </div>
          <div class="palette-btn" id="pal-Violet" onclick="setPalette('Violet', '#A855F7')">
            <div class="swatch" style="background: #A855F7;"></div>
            <div style="font-size: 11px; font-weight: 600;">Tokyo Violet</div>
          </div>
          <div class="palette-btn" id="pal-Emerald" onclick="setPalette('Emerald', '#10B981')">
            <div class="swatch" style="background: #10B981;"></div>
            <div style="font-size: 11px; font-weight: 600;">Pixel Emerald</div>
          </div>
          <div class="palette-btn" id="pal-Tangerine" onclick="setPalette('Tangerine', '#F97316')">
            <div class="swatch" style="background: #F97316;"></div>
            <div style="font-size: 11px; font-weight: 600;">Tangerine</div>
          </div>
          <div class="palette-btn" id="pal-Rose" onclick="setPalette('Rose', '#F43F5E')">
            <div class="swatch" style="background: #F43F5E;"></div>
            <div style="font-size: 11px; font-weight: 600;">Rose Quartz</div>
          </div>
          <div class="palette-btn" id="pal-Minimal" onclick="setPalette('Minimal', '#64748B')">
            <div class="swatch" style="background: #64748B;"></div>
            <div style="font-size: 11px; font-weight: 600;">Studio Slate</div>
          </div>
        </div>
      </div>
    </div>

    <div class="footer">
      <span>✦ Native Win32 / macOS / Linux</span>
      <span id="osBadge">Live 1s Engine</span>
    </div>
  </div>

  <script>
    let appData = null;
    let activeFilter = 'All';
    let isDarkMode = true;
    let activePalette = 'Sapphire';

    function setTab(name) {
      ['Overview', 'Focus', 'Timeline', 'Settings'].forEach(t => {
        document.getElementById('tab' + t).style.display = (t.toLowerCase() === name) ? 'flex' : 'none';
      });
      document.querySelectorAll('.nav button').forEach(b => {
        b.classList.toggle('active', b.innerText.toLowerCase() === name);
      });
    }

    function setFilter(cat) {
      activeFilter = cat;
      document.querySelectorAll('.chip').forEach(c => c.classList.toggle('active', c.innerText === cat));
      renderApps();
    }

    function setMode(dark) {
      isDarkMode = dark;
      document.body.classList.toggle('light', !dark);
      document.getElementById('btnLight').classList.toggle('active', !dark);
      document.getElementById('btnDark').classList.toggle('active', dark);
    }

    function setPalette(name, hex) {
      activePalette = name;
      document.documentElement.style.setProperty('--accent', hex);
      document.querySelectorAll('.palette-btn').forEach(b => b.classList.toggle('active', b.id === 'pal-' + name));
    }

    function formatDuration(sec) {
      if (sec < 60) return sec + 's';
      let m = Math.floor(sec / 60);
      let s = sec % 60;
      if (m < 60) return m + 'm ' + s + 's';
      let h = Math.floor(m / 60);
      return h + 'h ' + (m % 60) + 'm';
    }

    function formatTimer(sec) {
      let h = String(Math.floor(sec / 3600)).padStart(2, '0');
      let m = String(Math.floor((sec % 3600) / 60)).padStart(2, '0');
      let s = String(sec % 60).padStart(2, '0');
      return `${h}:${m}:${s}`;
    }

    async function pollMetrics() {
      try {
        const res = await fetch('/api/metrics');
        appData = await res.json();
        updateUI();
      } catch(e) {}
    }

    function updateUI() {
      if (!appData) return;
      document.getElementById('activeAppName').innerText = appData.active_app;
      document.getElementById('activeWinTitle').innerText = appData.active_title || 'Active Window';
      document.getElementById('activeCategoryBadge').innerText = appData.active_category;
      document.getElementById('sessionTimer').innerText = formatTimer(appData.session_seconds);
      document.getElementById('metricActive').innerText = formatDuration(appData.total_active_seconds);
      document.getElementById('metricFocus').innerText = formatDuration(appData.focus_work_seconds);
      document.getElementById('metricScore').innerText = appData.focus_score + '%';
      document.getElementById('bigScore').innerText = appData.focus_score;
      document.getElementById('osBadge').innerText = appData.os + ' • 1s Engine';

      const tot = Math.max(1, appData.total_active_seconds);
      document.getElementById('barFocus').style.width = ((appData.focus_work_seconds / tot) * 100) + '%';
      document.getElementById('barGen').style.width = ((appData.general_seconds / tot) * 100) + '%';
      document.getElementById('barDist').style.width = ((appData.distraction_seconds / tot) * 100) + '%';

      // Sprint HUD
      if (appData.sprint && appData.sprint.active) {
        document.getElementById('sprintActiveBox').style.display = 'block';
        document.getElementById('sprintTitle').innerText = appData.sprint.type;
        document.getElementById('sprintRemaining').innerText = formatTimer(appData.sprint.remaining);
      } else {
        document.getElementById('sprintActiveBox').style.display = 'none';
      }

      renderApps();
      renderTimeline();
    }

    function renderApps() {
      if (!appData) return;
      const search = (document.getElementById('appSearch').value || '').toLowerCase();
      const container = document.getElementById('appsContainer');
      container.innerHTML = '';

      const filtered = (appData.apps || []).filter(a => {
        const matchesCat = (activeFilter === 'All' || a.category.toLowerCase() === activeFilter.toLowerCase());
        const matchesSearch = !search || a.name.toLowerCase().includes(search);
        return matchesCat && matchesSearch;
      });

      filtered.forEach((app, idx) => {
        const item = document.createElement('div');
        item.className = 'app-item';
        item.innerHTML = `
          <div>
            <div class="app-name">#${idx+1} ${app.name}</div>
            <div class="app-meta">${app.category} • ${app.percent}%</div>
          </div>
          <div class="app-dur">${formatDuration(app.duration)}</div>
        `;
        container.appendChild(item);
      });
    }

    function renderTimeline() {
      if (!appData) return;
      const container = document.getElementById('timelineBars');
      container.innerHTML = '';
      for (let h = 8; h <= 20; h++) {
        const act = appData.hourly_active[h] || 0;
        const foc = appData.hourly_focus[h] || 0;
        const pct = Math.min(100, Math.round((act / 3600) * 100));
        const row = document.createElement('div');
        row.style.cssText = 'display: flex; align-items: center; gap: 8px; font-size: 10px;';
        row.innerHTML = `
          <span style="width: 36px; color: var(--text-mut);">${h}:00</span>
          <div style="flex: 1; height: 6px; background: var(--elevated); border-radius: 3px; overflow: hidden;">
            <div style="width: ${pct}%; height: 100%; background: var(--accent);"></div>
          </div>
          <span style="width: 40px; text-align: right; font-weight: 600;">${Math.round(act/60)}m</span>
        `;
        container.appendChild(row);
      }
    }

    async function startSprint(mins) {
      await fetch('/api/sprint?action=start&minutes=' + mins);
      pollMetrics();
    }

    async function cancelSprint() {
      await fetch('/api/sprint?action=cancel');
      pollMetrics();
    }

    setInterval(pollMetrics, 1000);
    pollMetrics();
  </script>
</body>
</html>
"""

class FocusPulseHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        if parsed.path == "/" or parsed.path == "/index.html":
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(HTML_DASHBOARD.encode("utf-8"))
        elif parsed.path == "/api/metrics":
            snapshot = engine.get_snapshot()
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            self.wfile.write(json.dumps(snapshot).encode("utf-8"))
        elif parsed.path == "/api/sprint":
            query = urllib.parse.parse_qs(parsed.query)
            action = query.get("action", [""])[0]
            if action == "start":
                mins = int(query.get("minutes", [25])[0])
                engine.start_sprint(mins)
            elif action == "cancel":
                engine.cancel_sprint()
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(b'{"status":"ok"}')
        else:
            self.send_response(404)
            self.end_headers()

    def log_message(self, format, *args):
        pass  # Suppress console log spam

def main():
    port = 5600
    # Find open port if 5600 is taken
    for p in range(5600, 5620):
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                s.bind(('127.0.0.1', p))
                port = p
                break
        except OSError:
            continue

    # Start tracking background thread
    t = threading.Thread(target=tracking_loop, daemon=True)
    t.start()

    server = HTTPServer(('127.0.0.1', port), FocusPulseHandler)
    url = f"http://127.0.0.1:{port}"
    print(f"✦ FocusPulse Engine Active on {OS_TYPE}")
    print(f"✦ Dashboard URL: {url}")
    print("Press Ctrl+C to terminate.")

    # Try opening browser if not in headless mode
    if "--headless" not in sys.argv:
        try:
            import webbrowser
            threading.Timer(0.8, lambda: webbrowser.open(url)).start()
        except Exception:
            pass

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down FocusPulse...")
        engine.is_running = False
        engine.save_metrics()
        server.server_close()

if __name__ == "__main__":
    main()
