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
TASKS_FILE = os.path.join(CONFIG_DIR, "tasks.json")
HISTORY_FILE = os.path.join(CONFIG_DIR, "history.json")

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

    study_keywords = ["tutorial", "course", "lecture", "study", "learn", "how to", "guide", "explained", "walkthrough", "documentation", "docs", "coding", "programming", "developer", "conference", "paper", "research", "academic", "deep learning", "machine learning", "ai", "algorithm", "math", "science", "physics", "history", "university", "mit", "stanford", "harvard", "khan academy", "crash course", "analysis", "benchmark", "leetcode", "interview"]
    if any(k in title_lower for k in study_keywords):
        return "Focus"
    if any(k in title_lower for k in ["github", "stackoverflow", "gitlab", "pull request", "terminal", "debugger", "chatgpt", "claude", "gemini"]):
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

        # Sprint & Pomodoro state
        self.sprint_active = False
        self.sprint_total_seconds = 25 * 60
        self.sprint_remaining_seconds = 25 * 60
        self.sprint_type = "25m Deep Work"
        self.pomodoro_mode = "Idle"
        self.pomodoro_current_cycle = 1
        self.pomodoro_target_cycles = 4
        self.pomodoro_is_paused = False
        self.linked_task_id = ""
        self.linked_task_title = ""
        self.pomodoro_work_mins = 25
        self.pomodoro_break_mins = 5
        self.pomodoro_long_break_mins = 15

        self.tasks = []
        self.daily_history = {}

        self.load_metrics()
        self.load_tasks()
        self.load_history()

    def load_tasks(self):
        if os.path.exists(TASKS_FILE):
            try:
                with open(TASKS_FILE, "r", encoding="utf-8") as f:
                    self.tasks = json.load(f)
                    return
            except Exception:
                pass
        self.tasks = [
            {"Id": "daf01621", "Title": "Ship FocusPulse v1.1", "IsCompleted": False, "Priority": "High", "PomodorosCompleted": 2},
            {"Id": "c1a6baf7", "Title": "Review weekly focus analytics", "IsCompleted": False, "Priority": "Medium", "PomodorosCompleted": 1},
            {"Id": "ce16f093", "Title": "5-minute hydration and posture stretch", "IsCompleted": True, "Priority": "Low", "PomodorosCompleted": 1}
        ]
        self.save_tasks()

    def save_tasks(self):
        try:
            with open(TASKS_FILE, "w", encoding="utf-8") as f:
                json.dump(self.tasks, f, indent=2)
        except Exception:
            pass

    def add_task(self, title, priority="Medium"):
        if not title.strip(): return
        import uuid
        task = {
            "Id": uuid.uuid4().hex[:8],
            "Title": title.strip(),
            "IsCompleted": False,
            "Priority": priority or "Medium",
            "PomodorosCompleted": 0,
            "CreatedDate": datetime.now().strftime("%Y-%m-%d")
        }
        with self.lock:
            self.tasks.insert(0, task)
            self.save_tasks()

    def toggle_task(self, task_id):
        with self.lock:
            for t in self.tasks:
                if t.get("Id") == task_id:
                    t["IsCompleted"] = not t.get("IsCompleted", False)
                    break
            self.save_tasks()

    def delete_task(self, task_id):
        with self.lock:
            self.tasks = [t for t in self.tasks if t.get("Id") != task_id]
            if self.linked_task_id == task_id:
                self.linked_task_id = ""
                self.linked_task_title = ""
            self.save_tasks()

    def increment_task_pomo(self, task_id):
        with self.lock:
            for t in self.tasks:
                if t.get("Id") == task_id:
                    t["PomodorosCompleted"] = t.get("PomodorosCompleted", 0) + 1
                    break
            self.save_tasks()

    def load_history(self):
        if os.path.exists(HISTORY_FILE):
            try:
                with open(HISTORY_FILE, "r", encoding="utf-8") as f:
                    self.daily_history = json.load(f)
            except Exception:
                pass

    def get_weekly_metrics(self):
        from datetime import timedelta
        today = datetime.now()
        metrics = []
        for i in range(6, -1, -1):
            d = today - timedelta(days=i)
            key = d.strftime("%Y-%m-%d")
            foc = self.focus_work_seconds if key == today.strftime("%Y-%m-%d") else self.daily_history.get(key, 0)
            metrics.append({
                "day": d.strftime("%a"),
                "date": key,
                "hours": round(foc / 3600, 1),
                "is_today": (i == 0),
                "ratio": min(1.0, (foc / 3600) / 4.0)
            })
        return metrics

    def get_streak_days(self):
        from datetime import timedelta
        streak = 0
        today = datetime.now()
        if (self.focus_work_seconds / 3600) >= 0.5:
            streak += 1
        d = today - timedelta(days=1)
        while True:
            key = d.strftime("%Y-%m-%d")
            if (self.daily_history.get(key, 0) / 3600) >= 0.5:
                streak += 1
                d -= timedelta(days=1)
            else:
                break
        return max(1, streak) if (streak > 0 or self.focus_work_seconds > 300) else 0

    def start_pomodoro(self, work_mins=25, break_mins=5, long_break_mins=15, task_id="", task_title=""):
        with self.lock:
            self.pomodoro_work_mins = work_mins
            self.pomodoro_break_mins = break_mins
            self.pomodoro_long_break_mins = long_break_mins
            self.sprint_active = True
            self.pomodoro_is_paused = False
            self.pomodoro_mode = "Focus"
            self.sprint_total_seconds = work_mins * 60
            self.sprint_remaining_seconds = self.sprint_total_seconds
            self.sprint_type = f"{work_mins}m Deep Work"
            if task_id:
                self.linked_task_id = task_id
                self.linked_task_title = task_title
        send_notification("🎯 Focus Session Started", f"{work_mins}-minute focus sprint initiated.")

    def toggle_pomodoro_pause(self):
        with self.lock:
            if not self.sprint_active:
                self.start_pomodoro(self.pomodoro_work_mins, self.pomodoro_break_mins, self.pomodoro_long_break_mins, self.linked_task_id, self.linked_task_title)
            else:
                self.pomodoro_is_paused = not self.pomodoro_is_paused

    def skip_pomodoro_break(self):
        with self.lock:
            if self.pomodoro_mode == "ShortBreak":
                self.pomodoro_current_cycle += 1
                if self.pomodoro_current_cycle > self.pomodoro_target_cycles:
                    self.pomodoro_current_cycle = 1
            elif self.pomodoro_mode == "LongBreak":
                self.pomodoro_current_cycle = 1
            self.pomodoro_mode = "Focus"
            self.sprint_total_seconds = self.pomodoro_work_mins * 60
            self.sprint_remaining_seconds = self.sprint_total_seconds
            self.sprint_type = f"{self.pomodoro_work_mins}m Deep Work"
            self.pomodoro_is_paused = False
            self.sprint_active = True

    def reset_pomodoro(self):
        with self.lock:
            self.sprint_active = False
            self.pomodoro_is_paused = False
            self.pomodoro_mode = "Idle"
            self.sprint_total_seconds = self.pomodoro_work_mins * 60
            self.sprint_remaining_seconds = self.sprint_total_seconds

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
            today_str = datetime.now().strftime("%Y-%m-%d")
            data = {
                "Date": today_str,
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
                self.daily_history[today_str] = self.focus_work_seconds
                with open(HISTORY_FILE, "w", encoding="utf-8") as f:
                    json.dump(self.daily_history, f, indent=2)
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

            # Handle Pomodoro interval transitions & countdown
            if self.sprint_active and not self.pomodoro_is_paused:
                self.sprint_remaining_seconds -= 1
                if self.sprint_remaining_seconds <= 0:
                    if self.pomodoro_mode == "Focus":
                        if self.linked_task_id:
                            self.increment_task_pomo(self.linked_task_id)
                        send_notification("🏆 Focus Session Complete!", "Awesome! Take a well-deserved break.")
                        if self.pomodoro_current_cycle >= self.pomodoro_target_cycles:
                            self.pomodoro_mode = "LongBreak"
                            self.sprint_total_seconds = self.pomodoro_long_break_mins * 60
                            self.sprint_remaining_seconds = self.sprint_total_seconds
                            self.sprint_type = f"🌿 Long Break ({self.pomodoro_long_break_mins}m)"
                        else:
                            self.pomodoro_mode = "ShortBreak"
                            self.sprint_total_seconds = self.pomodoro_break_mins * 60
                            self.sprint_remaining_seconds = self.sprint_total_seconds
                            self.sprint_type = f"☕ Short Break ({self.pomodoro_break_mins}m)"
                    elif self.pomodoro_mode == "ShortBreak":
                        send_notification("☕ Break Finished", "Ready for your next deep focus cycle!")
                        self.pomodoro_current_cycle += 1
                        self.pomodoro_mode = "Focus"
                        self.sprint_total_seconds = self.pomodoro_work_mins * 60
                        self.sprint_remaining_seconds = self.sprint_total_seconds
                        self.sprint_type = f"{self.pomodoro_work_mins}m Deep Work"
                    elif self.pomodoro_mode == "LongBreak":
                        send_notification("🌿 Long Break Finished", "Cycle completed! Starting fresh cycle.")
                        self.pomodoro_current_cycle = 1
                        self.pomodoro_mode = "Focus"
                        self.sprint_total_seconds = self.pomodoro_work_mins * 60
                        self.sprint_remaining_seconds = self.sprint_total_seconds
                        self.sprint_type = f"{self.pomodoro_work_mins}m Deep Work"
                    else:
                        self.sprint_active = False
                elif cat == "Distraction" and self.sprint_remaining_seconds % 30 == 0:
                    send_notification("🛡️ FocusPulse Shield Alert", f"Distraction detected ({app}) during active focus session!")

    def start_sprint(self, minutes):
        self.start_pomodoro(work_mins=minutes)

    def cancel_sprint(self):
        self.reset_pomodoro()

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

            mins = self.sprint_remaining_seconds // 60
            secs = self.sprint_remaining_seconds % 60

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
                    "paused": self.pomodoro_is_paused,
                    "mode": self.pomodoro_mode,
                    "cycle": self.pomodoro_current_cycle,
                    "target_cycles": self.pomodoro_target_cycles,
                    "type": self.sprint_type,
                    "remaining": self.sprint_remaining_seconds,
                    "formatted_remaining": f"{mins:02d}:{secs:02d}",
                    "total": self.sprint_total_seconds,
                    "progress": int(((self.sprint_total_seconds - self.sprint_remaining_seconds) / max(1, self.sprint_total_seconds)) * 100) if self.sprint_active else 0,
                    "linked_task_id": self.linked_task_id,
                    "linked_task_title": self.linked_task_title
                },
                "tasks": list(self.tasks),
                "weekly_metrics": self.get_weekly_metrics(),
                "streak_days": self.get_streak_days(),
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
      <!-- Hero Pomodoro Work / Break Interval Engine -->
      <div class="card" style="text-align: center; padding: 18px;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
          <div class="status-badge" id="pomoModeBadge" style="font-size: 10px; font-weight: 800;">🎯 READY TO FOCUS</div>
          <div style="display: flex; align-items: center; gap: 6px;">
            <div id="pomoCycleDots" style="display: flex; gap: 4px;"></div>
            <span id="pomoCycleLabel" style="font-size: 10px; color: var(--text-mut); font-weight: 600;">Cycle 1 of 4</span>
          </div>
        </div>

        <div style="font-size: 44px; font-weight: 900; letter-spacing: -1px; margin: 4px 0;" id="pomoCountdown">25:00</div>

        <!-- Linked Focus Target Banner -->
        <div id="pomoTargetBanner" style="font-size: 11px; font-weight: 600; color: var(--text-mut); background: var(--elevated); padding: 5px 10px; border-radius: 6px; margin: 4px 0 8px 0; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;">
          🎯 No task target linked • Click 🎯 on any priority below
        </div>

        <div style="height: 5px; background: var(--elevated); border-radius: 3px; overflow: hidden; margin-bottom: 12px;">
          <div id="pomoProgressBar" style="height: 100%; width: 0%; background: var(--accent); transition: width 0.3s;"></div>
        </div>

        <div style="display: flex; justify-content: center; gap: 8px; margin-bottom: 8px;">
          <button id="pomoPlayPauseBtn" style="background: var(--accent); border: none; color: #fff; padding: 8px 18px; border-radius: 8px; font-size: 12px; font-weight: 700; cursor: pointer;" onclick="togglePomodoroPause()">▶ Start</button>
          <button id="pomoSkipBtn" style="display: none; background: var(--amber); border: none; color: #000; padding: 8px 14px; border-radius: 8px; font-size: 12px; font-weight: 700; cursor: pointer;" onclick="skipPomodoroBreak()">⏭ Skip Break</button>
          <button style="background: var(--elevated); border: 1px solid var(--border); color: var(--text-pri); padding: 8px 14px; border-radius: 8px; font-size: 12px; font-weight: 600; cursor: pointer;" onclick="resetPomodoro()">↺ Reset</button>
        </div>

        <div style="display: flex; justify-content: center; gap: 6px; flex-wrap: wrap;">
          <button class="chip" onclick="startPomodoro(25, 5, 15)">25m Focus</button>
          <button class="chip" onclick="startPomodoro(50, 10, 20)">50m Deep</button>
          <button class="chip" onclick="startPomodoro(5, 5, 15)">5m Break</button>
          <button class="chip" onclick="startPomodoro(15, 5, 15)">15m Long</button>
        </div>
      </div>

      <!-- Today's Priorities & Task Checklist -->
      <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span style="font-size: 11px; font-weight: 800; color: var(--text-mut);">TODAY'S PRIORITIES & FOCUS QUEUE</span>
          <span id="tasksSummaryBadge" style="font-size: 11px; font-weight: 700; color: var(--accent);">0 Tasks</span>
        </div>
        <div style="height: 3px; background: var(--elevated); border-radius: 2px; overflow: hidden; margin: 4px 0 8px 0;">
          <div id="tasksSummaryProgressBar" style="height: 100%; width: 0%; background: var(--accent); transition: width 0.3s;"></div>
        </div>

        <!-- Quick Add Task Bar -->
        <div style="display: flex; gap: 6px; margin-bottom: 8px;">
          <input type="text" id="newTaskInput" placeholder="Add a focus task..." style="flex: 1; background: var(--elevated); border: 1px solid var(--border); border-radius: 6px; padding: 6px 10px; color: var(--text-pri); font-size: 11px; outline: none;" onkeydown="if(event.key==='Enter') submitTask()">
          <button id="taskPriorityToggle" style="background: var(--elevated); border: 1px solid var(--border); color: var(--amber); border-radius: 6px; padding: 6px 10px; font-size: 10px; font-weight: 700; cursor: pointer;" onclick="cyclePriority()">🟡 Med</button>
          <button style="background: var(--accent); border: none; color: #fff; border-radius: 6px; padding: 6px 12px; font-size: 11px; font-weight: 700; cursor: pointer;" onclick="submitTask()">+ Add</button>
        </div>

        <!-- Tasks Container -->
        <div id="tasksContainer" style="display: flex; flex-direction: column; gap: 6px;"></div>
      </div>

      <!-- Flow Score & Pacing Summary -->
      <div class="card" style="display: flex; flex-direction: row; align-items: center; gap: 14px; padding: 14px;">
        <div style="width: 50px; height: 50px; border-radius: 50%; border: 2px solid var(--focus); display: flex; align-items: center; justify-content: center; font-size: 16px; font-weight: 900; color: var(--focus);" id="bigScore">94</div>
        <div style="flex: 1;">
          <div style="font-size: 9px; font-weight: 800; color: var(--text-mut);">DAILY FLOW SCORE</div>
          <div style="font-size: 13px; font-weight: 700; color: var(--text-pri);" id="scoreDesc">Deep Focus Flow</div>
          <div style="font-size: 10px; color: var(--text-sec);">Automated distraction defense & ergonomic pacing</div>
        </div>
      </div>
    </div>

    <!-- TIMELINE TAB -->
    <div id="tabTimeline" style="display: none; flex-direction: column; gap: 10px;">
      <!-- Weekly Consistency & Streak Card -->
      <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
          <span style="font-size: 11px; font-weight: 800; color: var(--text-mut);">WEEKLY CONSISTENCY & STREAK</span>
          <div class="status-badge" id="streakBadge" style="background: rgba(245,158,11,0.15); color: var(--amber); border-color: rgba(245,158,11,0.3); font-size: 10px;">🔥 1 DAY STREAK</div>
        </div>
        <div id="weeklyBarsGrid" style="display: grid; grid-template-columns: repeat(7, 1fr); gap: 6px; align-items: end; height: 75px; padding-top: 6px;"></div>
      </div>

      <!-- Hourly Chronological Matrix -->
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
      <span>🔒 100% Offline & Private</span>
      <span id="osBadge">Never Leaves Your PC</span>
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

      // Pomodoro HUD
      if (appData.sprint) {
        const sp = appData.sprint;
        document.getElementById('pomoCountdown').innerText = sp.formatted_remaining || '25:00';
        document.getElementById('pomoProgressBar').style.width = (sp.progress || 0) + '%';
        
        const badge = document.getElementById('pomoModeBadge');
        const playBtn = document.getElementById('pomoPlayPauseBtn');
        const skipBtn = document.getElementById('pomoSkipBtn');

        if (sp.active) {
          playBtn.innerText = sp.paused ? '▶ Resume' : '❚❚ Pause';
          if (sp.mode === 'ShortBreak') {
            badge.innerText = '☕ SHORT BREAK';
            badge.style.color = 'var(--amber)';
            skipBtn.style.display = 'inline-block';
          } else if (sp.mode === 'LongBreak') {
            badge.innerText = '🌿 LONG BREAK';
            badge.style.color = 'var(--focus)';
            skipBtn.style.display = 'inline-block';
          } else {
            badge.innerText = '🎯 DEEP FOCUS';
            badge.style.color = 'var(--accent)';
            skipBtn.style.display = 'none';
          }
        } else {
          badge.innerText = '🎯 READY TO FOCUS';
          badge.style.color = 'var(--text-mut)';
          playBtn.innerText = '▶ Start';
          skipBtn.style.display = 'none';
        }

        // Target task banner
        const targetBanner = document.getElementById('pomoTargetBanner');
        if (sp.linked_task_title) {
          targetBanner.innerText = '🎯 Target: ' + sp.linked_task_title;
          targetBanner.style.color = 'var(--accent)';
        } else {
          targetBanner.innerText = '🎯 No task target linked • Click 🎯 on any priority below';
          targetBanner.style.color = 'var(--text-mut)';
        }

        // 4 cycle dots
        const dotsBox = document.getElementById('pomoCycleDots');
        dotsBox.innerHTML = '';
        const curCycle = sp.cycle || 1;
        for (let c = 1; c <= 4; c++) {
          const d = document.createElement('div');
          d.style.cssText = `width: 14px; height: 6px; border-radius: 3px; background: ${(c <= curCycle && sp.active) ? 'var(--accent)' : 'var(--elevated)'}; border: 1px solid ${(c === curCycle) ? 'var(--accent)' : 'var(--border)'};`;
          dotsBox.appendChild(d);
        }
        document.getElementById('pomoCycleLabel').innerText = `Cycle ${curCycle} of 4`;
      }

      renderTasks();
      renderWeeklyMetrics();
      renderApps();
      renderTimeline();
    }

    let selectedPriority = 'Medium';
    function cyclePriority() {
      const btn = document.getElementById('taskPriorityToggle');
      if (selectedPriority === 'High') {
        selectedPriority = 'Low';
        btn.innerText = '🟢 Low';
        btn.style.color = 'var(--focus)';
      } else if (selectedPriority === 'Low') {
        selectedPriority = 'Medium';
        btn.innerText = '🟡 Med';
        btn.style.color = 'var(--amber)';
      } else {
        selectedPriority = 'High';
        btn.innerText = '🔴 High';
        btn.style.color = 'var(--distract)';
      }
    }

    async function submitTask() {
      const input = document.getElementById('newTaskInput');
      const val = (input.value || '').trim();
      if (!val) return;
      await fetch(`/api/tasks?action=add&title=${encodeURIComponent(val)}&priority=${encodeURIComponent(selectedPriority)}`);
      input.value = '';
      pollMetrics();
    }

    async function toggleTask(id) {
      await fetch(`/api/tasks?action=toggle&id=${id}`);
      pollMetrics();
    }

    async function deleteTask(id) {
      await fetch(`/api/tasks?action=delete&id=${id}`);
      pollMetrics();
    }

    async function focusTask(id, title) {
      await fetch(`/api/pomodoro?action=start&work=25&break=5&long=15&task_id=${id}&task_title=${encodeURIComponent(title)}`);
      pollMetrics();
    }

    function renderTasks() {
      if (!appData) return;
      const tasks = appData.tasks || [];
      const container = document.getElementById('tasksContainer');
      container.innerHTML = '';

      const total = tasks.length;
      let completed = 0;
      tasks.forEach(t => { if (t.IsCompleted) completed++; });

      const pct = total > 0 ? Math.round((completed / total) * 100) : 0;
      document.getElementById('tasksSummaryBadge').innerText = `${completed}/${total} Done (${pct}%)`;
      document.getElementById('tasksSummaryProgressBar').style.width = pct + '%';

      if (total === 0) {
        container.innerHTML = '<div style="padding: 12px; text-align: center; font-size: 11px; color: var(--text-mut); background: var(--elevated); border-radius: 8px;">✨ All caught up! Add your next priority above.</div>';
        return;
      }

      const curLinkedId = appData.sprint ? appData.sprint.linked_task_id : '';

      tasks.forEach(t => {
        const isLinked = curLinkedId && curLinkedId === t.Id;
        const row = document.createElement('div');
        row.style.cssText = `display: flex; align-items: center; justify-content: space-between; padding: 7px 9px; border-radius: 8px; background: ${isLinked ? 'var(--card-hover)' : 'var(--elevated)'}; border: 1px solid ${isLinked ? 'var(--accent)' : 'var(--border)'}; opacity: ${t.IsCompleted ? 0.6 : 1};`;

        const prioColor = t.Priority === 'High' ? 'var(--distract)' : (t.Priority === 'Low' ? 'var(--focus)' : 'var(--amber)');

        row.innerHTML = `
          <div style="display: flex; align-items: center; gap: 8px; flex: 1; overflow: hidden;">
            <div onclick="toggleTask('${t.Id}')" style="width: 16px; height: 16px; border-radius: 4px; border: 1.5px solid ${t.IsCompleted ? 'var(--accent)' : 'var(--text-mut)'}; background: ${t.IsCompleted ? 'var(--accent)' : 'transparent'}; display: flex; align-items: center; justify-content: center; cursor: pointer; color: #fff; font-size: 10px; font-weight: 800;">
              ${t.IsCompleted ? '✓' : ''}
            </div>
            <span style="font-size: 11.5px; font-weight: 600; text-decoration: ${t.IsCompleted ? 'line-through' : 'none'}; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
              ${t.Title}
            </span>
            <span style="font-size: 8px; font-weight: 800; padding: 1px 4px; border-radius: 3px; background: rgba(128,128,128,0.15); color: ${prioColor};">
              ${(t.Priority || 'MED').toUpperCase()}
            </span>
          </div>
          <div style="display: flex; align-items: center; gap: 6px;">
            <span style="font-size: 9px; font-weight: 700; background: var(--card); border: 1px solid var(--border); padding: 2px 5px; border-radius: 4px;">🍅 ${t.PomodorosCompleted || 0}</span>
            <button onclick="focusTask('${t.Id}', '${t.Title.replace(/'/g, "\\'")}')" style="background: ${isLinked ? 'var(--accent)' : 'var(--card)'}; border: 1px solid ${isLinked ? 'var(--accent)' : 'var(--border)'}; color: ${isLinked ? '#fff' : 'var(--text-sec)'}; padding: 3px 7px; border-radius: 4px; font-size: 9px; font-weight: 700; cursor: pointer;">
              ${isLinked ? '🎯 Active' : '🎯 Focus'}
            </button>
            <button onclick="deleteTask('${t.Id}')" style="background: none; border: none; color: var(--text-mut); font-size: 11px; cursor: pointer; padding: 0 4px;">✕</button>
          </div>
        `;
        container.appendChild(row);
      });
    }

    function renderWeeklyMetrics() {
      if (!appData) return;
      const weekly = appData.weekly_metrics || [];
      const streak = appData.streak_days || 1;
      document.getElementById('streakBadge').innerText = `🔥 ${streak} DAY STREAK`;

      const grid = document.getElementById('weeklyBarsGrid');
      grid.innerHTML = '';
      weekly.forEach(m => {
        const col = document.createElement('div');
        col.style.cssText = 'display: flex; flex-direction: column; align-items: center; justify-content: flex-end; height: 100%;';
        const fillHeight = Math.max(3, Math.min(42, Math.round(m.ratio * 42)));
        col.innerHTML = `
          <div style="width: 14px; height: 42px; background: var(--elevated); border-radius: 3px; display: flex; align-items: flex-end; overflow: hidden; margin-bottom: 4px;">
            <div style="width: 100%; height: ${fillHeight}px; background: ${m.is_today ? 'var(--accent)' : 'var(--focus)'}; border-radius: 3px;"></div>
          </div>
          <span style="font-size: 8.5px; font-weight: ${m.is_today ? '800' : '500'}; color: ${m.is_today ? 'var(--accent)' : 'var(--text-mut)'};">${m.day}</span>
          <span style="font-size: 7.5px; font-weight: 700;">${m.hours}h</span>
        `;
        grid.appendChild(col);
      });
    }

    async function togglePomodoroPause() {
      await fetch('/api/pomodoro?action=toggle_pause');
      pollMetrics();
    }

    async function skipPomodoroBreak() {
      await fetch('/api/pomodoro?action=skip');
      pollMetrics();
    }

    async function resetPomodoro() {
      await fetch('/api/pomodoro?action=reset');
      pollMetrics();
    }

    async function startPomodoro(work, brk, lng) {
      await fetch(`/api/pomodoro?action=start&work=${work}&break=${brk}&long=${lng}`);
      pollMetrics();
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
        elif parsed.path == "/api/tasks":
            query = urllib.parse.parse_qs(parsed.query)
            action = query.get("action", [""])[0]
            if action == "add":
                title = query.get("title", [""])[0]
                priority = query.get("priority", ["Medium"])[0]
                engine.add_task(title, priority)
            elif action == "toggle":
                t_id = query.get("id", [""])[0]
                engine.toggle_task(t_id)
            elif action == "delete":
                t_id = query.get("id", [""])[0]
                engine.delete_task(t_id)
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            self.wfile.write(b'{"status":"ok"}')
        elif parsed.path == "/api/pomodoro":
            query = urllib.parse.parse_qs(parsed.query)
            action = query.get("action", [""])[0]
            if action == "start":
                work = int(query.get("work", [25])[0])
                brk = int(query.get("break", [5])[0])
                lng = int(query.get("long", [15])[0])
                task_id = query.get("task_id", [""])[0]
                task_title = query.get("task_title", [""])[0]
                engine.start_pomodoro(work, brk, lng, task_id, task_title)
            elif action == "toggle_pause":
                engine.toggle_pomodoro_pause()
            elif action == "skip":
                engine.skip_pomodoro_break()
            elif action == "reset":
                engine.reset_pomodoro()
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            self.wfile.write(b'{"status":"ok"}')
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
