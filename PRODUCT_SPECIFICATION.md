# FocusPulse: Product Specification & Experience Design

## 1. Product Vision

**FocusPulse** is designed to transform passive time tracking into an active, high-performance focus station inspired by modern developer productivity tools like **Linear**, **Raycast**, and **Apple Dynamic Island**.

Unlike conventional monitors that merely log historical timesheets, FocusPulse provides real-time situational awareness, encourages deep uninterrupted flow, prevents cognitive fatigue, and gives power users granular command over their work sessions.

---

## 2. Core Pillars & Feature Specifications

### 2.1 🏆 Real-Time Deep Flow Score
* **Concept**: Evaluates the quality of work rather than just quantity of screen time.
* **Mathematical Model**:
  $$\text{Focus Score} = \left(\frac{\text{Focus Work Seconds}}{\text{Total Active Seconds}} \times 100\right) - \text{Context Switch Penalty} - \text{Distraction Penalty}$$
* **Context-Switching Velocity**:
  - Measures application window switches per hour.
  - $<40\text{ switches/hr}$: Calm, uninterrupted deep flow state.
  - $40 - 80\text{ switches/hr}$: Moderate switching.
  - $>80\text{ switches/hr}$: Fragmented multi-tasking (score penalized).
* **Display**:
  - Radial score badge: `🏆 95% Deep Focus Flow` (80–100%), `⚡ Solid Productivity` (60–79%), or `🛑 High Fragmentation` (<40%).

---

### 2.2 🎯 Flow Sprints (Structured Focus Intervals)
* **Concept**: Built-in sprint timers to eliminate distractions during high-cognitive tasks.
* **Presets**:
  - `25m Focus Sprint` (Standard Pomodoro)
  - `50m Deep Flow` (Ultra-focus deep session)
* **Distraction Defense**:
  - If a user opens a distracting application or website (social media, entertainment, games) while a sprint is active, the sprint alerts counter increments and flashes a warning badge.
* **Audio Feedback**: Completion chime upon sprint finish.

---

### 2.3 📊 Automated Productivity Intelligence
* **Automatic Classification Engine**:
  - **Focus Work**: Google Antigravity IDE, Visual Studio Code, Visual Studio, Windows Terminal, PowerShell, Git, PyCharm, IntelliJ, Sublime Text, Notepad++, Notion, Obsidian, Figma, Excel. Active browser tabs on GitHub, Stack Overflow, Documentation, ChatGPT, Claude, Gemini, Jira, Confluence.
  - **General**: File Explorer, Windows Settings, Task Manager, Slack, Microsoft Teams, Calculator.
  - **Distraction**: Steam, Netflix, Spotify, games, and entertainment browser tabs (YouTube, Reddit, Twitter/X, TikTok, Twitch, Facebook, Instagram).
* **Daily Target Goal**:
  - Visual circular & linear progress bar tracking progress toward a customizable 4.0-hour daily focus target.

---

### 2.4 🧘 Ergonomic Screen Fatigue & Wellness Coach
* **Concept**: Combats continuous posture fatigue and computer vision syndrome (20-20-20 rule).
* **Trigger Heuristics**:
  - Measures uninterrupted screen activity.
  - Automatically resets when the user steps away from the keyboard for $\ge 2\text{ minutes}$ (real break detected).
  - Triggers an alert when continuous work reaches 50 minutes:
    *"🧘 Healthy Pacing: Continuous work 50m. Time for a quick stretch!"*
  - Includes instant `[Dismiss]` and `[Snooze 10m]` actions.

---

### 2.5 ⏱ Chronological Work Matrix (8 AM – 8 PM)
* Visual hour-by-hour timeline strip displaying active duration and focused work density for every hour of the workday.
* Allows quick retrospectives on peak performance hours.

---

### 2.6 🏷 Project Workspace Tagging
* Tag sessions to active workspaces: `[Development]`, `[Engineering]`, `[Design]`, `[Research]`, `[Personal]`.
* Aggregates daily metrics per workspace automatically.

---

### 2.7 🛡️ Privacy Shield & Pause
* **Live Pause**: Click `⏸` to pause tracking instantly.
* **Privacy Shield**: Click `🛡️` to mask window titles to `"Private Workspace"`, preventing sensitive documents, financial accounts, or internal URLs from appearing in logs.

---

### 2.8 🎨 Surface Themes & Opacity Control
* **5 Surface Themes**:
  - `🌙 Obsidian`: Linear dark palette with deep blue accents.
  - `🖤 OLED Midnight`: Pure pitch black with crisp glowing contrast.
  - `☀️ Clean Studio`: Crisp modern high-contrast light mode.
  - `🌊 Nordic Slate`: Arctic cool dark slate palette.
  - `🔮 Cyber Violet`: Modern cyberpunk purple aesthetic.
* **4 Opacity Presets**:
  - `100% Solid`, `95% Glass`, `80% Frosted`, `65% Translucent`.
