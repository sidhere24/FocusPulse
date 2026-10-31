# ✦ Publishing FocusPulse to GitHub (Public Open Source)

Your repository is now completely initialized with clean commits, `.gitignore`, MIT `LICENSE`, cross-platform support, and a complete GitHub Actions CI/CD release workflow.

---

## 1. Quick Steps to Publish to GitHub

### Step 1: Create a New GitHub Repository
1. Open your browser and navigate to **[github.com/new](https://github.com/new)**.
2. Enter repository name: **`FocusPulse`** (or your preferred name).
3. Set visibility to **Public**.
4. **Important**: Leave *"Add a README file"*, *"Add .gitignore"*, and *"Choose a license"* **UNCHECKED** (we have already created all of them).
5. Click **Create repository**.

### Step 2: Push Your Local Repository
In your PowerShell or terminal in this folder, run:

```powershell
# Set your personal name and email if desired
git config user.name "Your Name"
git config user.email "your.email@example.com"

# Link your local repo to GitHub (replace <YOUR_USERNAME> with your GitHub handle)
git remote add origin https://github.com/<YOUR_USERNAME>/FocusPulse.git

# Push the main branch to GitHub
git push -u origin main
```

---

## 2. Automated Multi-Platform Release (GitHub Actions)

We have included a GitHub Actions workflow in [`.github/workflows/build-and-release.yml`](.github/workflows/build-and-release.yml).

Whenever you want to release a new version with automated downloads for Windows, Linux, and macOS:

```powershell
# Create a release version tag
git tag v1.0.0

# Push the tag to GitHub
git push origin v1.0.0
```

GitHub Actions will automatically:
1. Spin up a Windows runner and compile native **x64**, **x86**, and **ARM64** executables.
2. Spin up an Ubuntu runner and package the universal **Linux & macOS** distribution (`FocusPulse-Unix.tar.gz`).
3. Create a public **GitHub Release (v1.0.0)** with all binary assets attached for download!

---

## 3. Platform Distribution Summary

| Platform | Execution Method | Dependencies |
| :--- | :--- | :--- |
| **🪟 Windows (x64 / x86 / ARM64)** | Double-click `run.bat` or direct `.exe` | Zero dependencies (native C# / Win32) |
| **🍎 macOS (Apple Silicon & Intel)** | Run `./run.sh` in Terminal | Zero dependencies (uses built-in Python 3 & Apple Events) |
| **🐧 Linux (X11 & Wayland)** | Run `./run.sh` in Terminal | Zero dependencies (uses built-in Python 3 & xprop/Wayland) |
