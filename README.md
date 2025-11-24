# **Angry Miao Infinity Mouse Battery Tray**  
_A lightweight Windows system tray utility for monitoring the Angry Miao Infinity Mouse and dongle battery levels._

<p align="left">
  <img src="https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows" />
  <img src="https://img.shields.io/badge/Build-MSBuild-success?logo=visualstudio" />
  <img src="https://img.shields.io/badge/Type-System%20Tray%20App-lightgrey" />
</p>

---

## 📌 **Overview**

The **Angry Miao Infinity Mouse Battery Tray** is a small background application that runs in the Windows system tray and displays **battery levels for both the mouse and its dongle**. It is designed to be minimal, silent, and reliable — perfect for users who want quick access to battery information without launching a full-sized application.

<img width="405" height="166" alt="image" src="https://github.com/user-attachments/assets/108edb7c-1e5e-4839-b76f-0935b5997e39" />

The main deliverable in this repository is:

👉 **`AMInfinityBatterySysTray`** — a Windows tray executable built on .NET.

---

# 🧑‍💻 Features (For End Users)

### 🎛️ **System Tray Application**
- Runs quietly in the Windows notification area  
- No window opens on startup  
- No admin privileges required
- Right-click tray icon to access features  

### 🔋 **Mouse + Dongle Battery Levels**
- Displays **mouse battery percentage**  
- Displays **dongle battery percentage**
- Icon changes to red when battery is critically low (<=5%)

### ⚙️ **Auto-Start Toggle**
- Enable “start with Windows”  
- Disable auto-start anytime  

### 🚀 **Lightweight**
- Tiny footprint  
- No intrusive background activity
- Minimal popup at system tray on low battery  
- Safe to leave running at all times  

---

# 🧩 Developer Overview

The `AMInfinityBatterySysTray` project is a WinForms-based, windowless tray app.

### Key Components

| File | Purpose |
|------|---------|
| **Program.cs** | Entry point, sets up tray message loop |
| **TrayContext.cs** | Manages tray icon + menu |
| **StartupManager.cs** | Handles registry auto-start |
| **AMInfinityBattery** | Core logic for reading mouse & dongle battery |

---

## 📁 Project Structure

```
AMInfinityBattery/
│
├── AMInfinityBattery/               # Core shared battery logic
│
├── AMInfinityBatterySysTray/        # ★ Main system tray executable
│   ├── Program.cs
│   ├── TrayContext.cs
│   ├── StartupManager.cs
│   └── AMInfinityBatterySysTray.csproj
│
├── TestConsole/                     # Console-based testing utility
│
└── AMInfinityBattery.slnx           # Main solution file
```

---

# 🛠️ Build Instructions

## 📦 Prerequisites

Install **Visual Studio 2026** with:

- **.NET Desktop Development**  
- **Desktop Development with C++**

---

## 1️⃣ Restore NuGet Packages

```powershell
msbuild AMInfinityBattery.slnx /t:Restore
```

---

## 2️⃣ Build the SysTray Executable (Release)

The project includes a **post-build publish script**, so **no manual publish step is required**.

```powershell
msbuild AMInfinityBattery.slnx /t:AMInfinityBatterySysTray /p:Configuration=Release
```

### 📂 Published Output Folder

Your final publish-ready build will appear in:

```
AMInfinityBatterySysTray\bin\Release\net10.0-windows\publish
```

Contains:
- ✔ Final executable  

---

# ▶️ Running the Application

Run:

```
AMInfinityBatterySysTray.exe
```

You will see an icon appear in the Windows system tray.  
Mouse hover it to view battery levels or right-click to toggle auto-start or close.

---

# 📜 License
[MIT License](https://github.com/lwYeo/Angry-Miao-Infinity-Mouse-Battery-Tray/blob/master/LICENSE)

