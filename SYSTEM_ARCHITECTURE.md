# FocusPulse: System Architecture & Native Windows Engine Specification

## 1. Overview & Core Engineering Principles

**FocusPulse** is an autonomous, privacy-first desktop focus and productivity HUD for Windows. It operates with zero telemetry and stores all metrics locally in `%LOCALAPPDATA%\FocusPulse\`.

FocusPulse is engineered around three foundational principles:
1. **Zero-Overhead Native Execution**: Written in clean native C# utilizing direct Win32 API interop (`user32.dll`, `kernel32.dll`) without heavy web wrappers (Electron/Chromium). Memory footprint is under 18 MB RAM and CPU usage is near 0%.
2. **24/7 Independent Tracking Thread**: Tracking is completely decoupled from the UI thread, ensuring continuous tracking even when occluded, minimized to the tray, or running behind full-screen applications.
3. **Multi-Architecture Native Support**: Cross-compiled natively for **x64** (AMD64 / Intel 64-bit), **x86** (32-bit legacy), and **ARM64** (Snapdragon X Elite / Surface Pro ARM) Windows.

---

## 2. High-Level System Architecture

```
┌────────────────────────────────────────────────────────┐
│                   Desktop Applications                 │
│      (Antigravity, VS Code, Browser Tabs, Slack, etc.) │
└───────────────────────────┬────────────────────────────┘
                            │ Foreground changes / Hardware inputs
                            ▼
┌────────────────────────────────────────────────────────┐
│               Windows Subsystem Layer                  │
│       user32.dll / kernel32.dll Win32 Interop          │
└─────────────┬──────────────────────────┬───────────────┘
              │                          │
              │ GetForegroundWindow()    │ GetLastInputInfo()
              ▼                          ▼
   ┌─────────────────────────────────────────────────┐
   │            FocusPulse Activity Engine           │
   │      Dedicated 24/7 Background Worker Loop      │
   │      - Win32 Process Image Resolution           │
   │      - UWP Child Window Extraction              │
   │      - Hardware Inactivity Detection            │
   │      - Deep Work & Flow Score Algorithm         │
   │      - Local Persistence (%LOCALAPPDATA%)       │
   └──────────────────────┬──────────────────────────┘
                          │ High-performance local state
                          ▼
   ┌─────────────────────────────────────────────────┐
   │         FocusPulse Fluent Glassmorphic HUD      │
   │      Linear / Raycast Style Desktop Widget      │
   │  (Full Dashboard • Mini Island HUD • System Tray)│
   └─────────────────────────────────────────────────┘
```

---

## 3. Low-Level Win32 Subsystem Mechanics

### 3.1 Foreground Window Identification
```c
HWND hwnd = GetForegroundWindow();
```
* Queries the top-level focused window handle from the Windows shell.

### 3.2 Title & Process Image Resolution
```c
DWORD pid;
GetWindowThreadProcessId(hwnd, &pid);

// Use PROCESS_QUERY_LIMITED_INFORMATION (0x1000) for unprivileged, safe access
HANDLE hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid);

wchar_t pathBuffer[1024];
DWORD size = 1024;
QueryFullProcessImageNameW(hProc, 0, pathBuffer, &size);
CloseHandle(hProc);
```
* **Security Resilience**: `PROCESS_QUERY_LIMITED_INFORMATION` (`0x1000`) allows non-elevated user processes to query the binary path of elevated administrative processes (e.g., Administrator Terminal) without throwing `ERROR_ACCESS_DENIED`.

### 3.3 Modern UWP Child Window Resolution (`ApplicationFrameHost.exe`)
Modern Universal Windows Platform (UWP) apps—such as Windows Settings, Calculator, and Photos—are encapsulated inside `ApplicationFrameHost.exe`.
FocusPulse enumerates child windows using `EnumChildWindows`:
```csharp
uint realChildPid = 0;
NativeMethods.EnumChildWindows(parentHwnd, (childHwnd, lParam) => {
    uint cPid = 0;
    NativeMethods.GetWindowThreadProcessId(childHwnd, out cPid);
    if (cPid != 0 && cPid != hostPid) {
        realChildPid = cPid;
        return false; // Stop enumeration
    }
    return true;
}, IntPtr.Zero);
```
Resolving `realChildPid` retrieves the actual application binary (e.g. `CalculatorApp.exe` or `SystemSettings.exe`).

### 3.4 Hardware Inactivity & Away Detection
FocusPulse measures user activity without recording keystrokes:
```c
LASTINPUTINFO lii;
lii.cbSize = sizeof(LASTINPUTINFO);
GetLastInputInfo(&lii);

DWORD idleMilliseconds = GetTickCount() - lii.dwTime;
```
* If `idleMilliseconds >= 180,000` (3 minutes): User state transitions to `Away`.
* If `idleMilliseconds < 180,000`: User state transitions to `Active`.

### 3.5 Self-Focus Filtering
To ensure that interacting with the HUD (e.g. changing settings, checking stats, or launching a sprint) never hijacks your workflow:
* The engine checks `pid == _myPid`.
* When focused on the HUD, FocusPulse maintains the active session on your actual work application, preserving your active session duration.

---

## 4. Multi-Architecture Native Compilation

FocusPulse targets all Windows architectures using the native Microsoft C# Compiler (`csc.exe`):

| Architecture Target | Flag | Execution Characteristic |
|---|---|---|
| **x64** | `/platform:x64` | Native 64-bit AMD64 binary for modern Intel/AMD PCs. |
| **x86** | `/platform:x86` | 32-bit PE binary for legacy Windows installations. |
| **ARM64** | `/platform:anycpu` | **Executes natively as 64-bit ARM64** on Windows on ARM (Qualcomm Snapdragon X Elite, Surface Pro) with zero emulation overhead. |
