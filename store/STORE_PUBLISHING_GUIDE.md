# ✦ Microsoft Store Publishing Guide for FocusPulse

This comprehensive guide details the step-by-step process for publishing **FocusPulse** to the Microsoft Store.

---

## 1. Prerequisites (Microsoft Partner Center Account)

1. Navigate to the **[Microsoft Partner Center](https://partner.microsoft.com/dashboard)**.
2. Sign in with your Microsoft Account and register as a Windows App Developer:
   - **Individual Account**: ~$19 USD (one-time fee).
   - **Company Account**: ~$99 USD (requires business verification).
3. Once registered, go to **Apps and games** -> **Overview**.

---

## 2. Choosing Your Submission Method

Microsoft Store in Windows 10/11 supports two submission paths for desktop applications:

### Path A: Modern Win32 App Submission (Fastest & Simplest)
Microsoft Store allows you to submit standard desktop Win32 applications directly.
- **Package Type**: `.exe` or `.msi` (or direct download URL).
- **Silent Install Parameters**: `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART` (built into the provided `store/FocusPulseInstaller.iss`).
- **Store Handles**: The Store creates your official listing, verifies safety, and coordinates the install.

### Path B: MSIX Native Package (Store-Managed Auto-Updates)
Store-native package that installs cleanly with zero registry bloat and automatic background updates.
- Use the free **MSIX Packaging Tool** (available directly in the Microsoft Store):
  1. Open MSIX Packaging Tool -> Select **Application package**.
  2. Point to `store/` directory with `store/AppxManifest.xml` and `store/assets/`.
  3. Include `bin/FocusPulse-x64.exe` (renamed as `FocusPulse.exe`).
  4. Generate `FocusPulse.msix`.

---

## 3. Pre-Generated Store Visual Assets

All required Microsoft Store visual assets have been pre-rendered and placed in `store/assets/`:
- `Square44x44Logo.png` (Taskbar & app list icon)
- `Square150x150Logo.png` (Medium Start menu tile)
- `StoreLogo.png` (50x50 Microsoft Store listing logo)
- `Wide310x150Logo.png` (Wide Start menu tile)
- `SplashScreen.png` (620x300 Launch splash screen)

---

## 4. Step-by-Step Submission in Partner Center

### Step 1: Reserve Product Name
1. Click **New product** -> **MSIX or PWA app** (or **Win32 app**).
2. Enter `FocusPulse` (or `FocusPulse - Focus & Activity Companion`).
3. Click **Reserve product name**.

### Step 2: Pricing and Availability
- **Pricing**: Free (or set your chosen price tier).
- **Markets**: All markets (or select specific countries).
- **Visibility**: Public in Microsoft Store.

### Step 3: Properties & Category
- **Category**: `Productivity` -> `Time Management & Productivity`.
- **System Requirements**: 
  - OS: Windows 10 version 17763.0 or higher / Windows 11.
  - Architecture: x64, x86, ARM64.

### Step 4: Age Rating (IARC)
1. Complete the brief IARC questionnaire:
   - Violence? **No**
   - Gambling? **No**
   - User content sharing? **No**
   - Location sharing? **No**
2. Result: **Rated for All / Everyone (3+)**.

### Step 5: Store Listing & Metadata
- **Product Title**: `FocusPulse: Activity & Deep Work Companion`
- **Short Description**:
  > A modern, zero-overhead desktop companion featuring live window tracking, Pomodoro focus sprints, dynamic color palettes, and privacy shields.
- **Full Description**:
  > FocusPulse is an ultra-lightweight, native Windows focus and productivity companion designed for creators, engineers, and professionals.
  > 
  > ✦ FEATURES:
  > • 24/7 Zero-Overhead Tracking: Direct Win32 monitoring in under 18 MB RAM.
  > • Deep Work Coaching: Live 25m & 50m Pomodoro flow sprints with distraction defense.
  > • Dynamic Island HUD: Compact ambient capsule widget (Ctrl+M) that stays out of your way.
  > • Decoupled Theming: Light & Dark modes paired with 6 vibrant color palettes (Electric Sapphire, Tokyo Violet, Pixel Emerald, Sunset Tangerine, Rose Quartz, Studio Slate).
  > • 100% Privacy-First: All metrics remain exclusively on your local PC. No cloud tracking, no telemetries, no accounts required.
- **Search Keywords**: `focus`, `activity monitor`, `time tracking`, `pomodoro`, `productivity`, `deep work`, `screen time`.
- **Privacy Policy URL**: Link to your GitHub repository or project privacy statement.

### Step 6: Packages / Installer
- Upload `FocusPulse.msix` (or enter your installer URL).
- The Store will validate package architecture (`x64`, `x86`, `ARM64`) and manifest capabilities.

### Step 7: Submit for Certification
- Click **Submit to the Store**.
- Automated validation takes between **2 to 24 hours**.
- Once certified, FocusPulse will be live on the Microsoft Store worldwide!
