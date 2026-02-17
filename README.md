<p align="center">
  <img src="assets/logo.png" alt="TrayStats" width="128" height="128">
</p>

<h1 align="center">TrayStats</h1>

<p align="center">
  <b>iStat Menus for Windows. System monitoring in your tray.</b>
</p>

If you've ever used iStat Menus on macOS, you know the joy of glancing at your menu bar to see CPU, GPU, RAM, disk, and network stats at a glance. TrayStats brings that same experience to Windows. It lives in your system tray, shows a live-updating icon, and pops up a rich dashboard when clicked.

This project was vibecoded by a developer who missed having quick system stats without opening Task Manager every five seconds.

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Dashboard](#dashboard)
  - [CPU](#cpu)
  - [GPU](#gpu)
  - [RAM](#ram)
  - [Disk](#disk)
  - [Network](#network)
- [Tray Icon Options](#tray-icon-options)
  - [Tray Metric](#tray-metric)
  - [Icon Style](#icon-style)
- [Running as Admin](#running-as-admin)
- [Architecture](#architecture)
- [Known Limitations](#known-limitations)
- [Building from Source](#building-from-source)
- [License](#license)

---

## Features

| Feature | Detail |
|---|---|
| **Dynamic tray icon** | Live-updating icon showing CPU, GPU, or RAM usage in your system tray |
| **Multiple icon styles** | Bar, percentage text, or mini chart -- pick your preference |
| **Pop-up dashboard** | Click the tray icon to see a compact dashboard with sparkline charts and detailed stats |
| **CPU monitoring** | Total load, per-core usage bars, temperature, max clock, package power |
| **GPU monitoring** | Core load, temperature, core/memory clocks, VRAM usage, fan speed, power draw |
| **RAM monitoring** | Used / available / total memory, load percentage |
| **Disk monitoring** | Per-drive space usage, temperature, read/write speeds |
| **Network monitoring** | Real-time download/upload speeds with sparkline history |
| **Expandable detail panels** | Click any section header to expand/collapse detailed stats |
| **Start with Windows** | Optional auto-start via the context menu |
| **Restart as Admin** | Elevate to get full sensor access without relaunching manually |
| **Single instance** | Mutex-based enforcement prevents duplicate instances |
| **Lightweight** | Pure WPF, no Electron, no web views. ~50MB RAM footprint |

---

## Installation

### Option 1: Download Release

Download the latest release from the [Releases](../../releases) page. Extract the zip and run `TrayStats.exe`.

> **Note:** You need the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed. If you don't have it, Windows will prompt you to install it on first launch.

### Option 2: Build from Source

```powershell
git clone https://github.com/andruu/TrayStats.git
cd TrayStats
dotnet build
dotnet run
```

---

## Usage

1. Run `TrayStats.exe`
2. A dynamic icon appears in the system tray (bottom-right near the clock)
3. **Left-click** the tray icon to toggle the dashboard popup
4. **Right-click** the tray icon for options:
   - **Show Dashboard** -- open the popup
   - **Tray Metric** -- choose what the icon displays (CPU, GPU, or RAM)
   - **Icon Style** -- choose how the icon looks (Bar, Percentage, or Mini Chart)
   - **Restart as Admin** -- relaunch with elevated privileges for full sensor data
   - **Start with Windows** -- toggle auto-start at login
   - **Exit** -- quit the app

> **Tip:** If the tray icon is hidden, click the `^` overflow arrow in the taskbar to find it, then drag it out to pin it permanently.

---

## Dashboard

The dashboard is a dark-themed popup window that anchors near your system tray. Each section shows a sparkline chart with 60 data points of history and a summary. Click the arrow on any section to expand its detail panel.

### CPU

| Stat | Description |
|---|---|
| **Total Load** | Overall CPU utilization percentage |
| **Per-Core Bars** | Individual usage bars for each core (P-cores and E-cores) |
| **Temperature** | CPU package or average temperature |
| **Max Clock** | Highest current clock speed across all cores |
| **Package Power** | Total CPU package power draw |

### GPU

| Stat | Description |
|---|---|
| **Core Load** | GPU core utilization percentage |
| **Temperature** | GPU core temperature |
| **Core Clock** | Current GPU core clock speed |
| **Memory Clock** | Current GPU memory clock speed |
| **VRAM Used** | GPU video memory in use |
| **Fan Speed** | Fan RPM (may show N/A if fans are stopped at idle) |
| **Power** | GPU power draw |

### RAM

| Stat | Description |
|---|---|
| **Used / Total** | Physical memory usage in GB |
| **Available** | Free physical memory |
| **Load** | Memory utilization percentage |

### Disk

| Stat | Description |
|---|---|
| **Space bar** | Visual bar showing used vs total space per drive |
| **Total / Used / Free** | Drive capacity breakdown |
| **Temperature** | Drive temperature (requires admin on some systems) |

### Network

| Stat | Description |
|---|---|
| **Download speed** | Current download rate |
| **Upload speed** | Current upload rate |
| **Sparkline charts** | Separate download/upload history graphs |

---

## Tray Icon Options

### Tray Metric

Choose which metric the tray icon displays:

| Option | Icon Shows |
|---|---|
| **CPU** | CPU total load percentage |
| **GPU** | GPU core load percentage |
| **RAM** | Memory load percentage |

### Icon Style

Choose the visual style of the tray icon:

| Style | Description |
|---|---|
| **Bar** | Vertical fill bar -- higher usage = taller bar. Green/yellow/red color coding |
| **Percentage** | Numeric percentage text rendered directly on the icon |
| **Mini Chart** | Rolling 16-sample bar chart showing recent history |

All styles use color coding: **green** (< 60%), **yellow** (60-85%), **red** (> 85%).

---

## Running as Admin

Some hardware sensors (CPU temperature, disk temperature, GPU fan speed) require administrator privileges to read. Without admin:

- These values will show **N/A** in the dashboard
- Everything else (load percentages, memory, disk space, network) works normally

To get full sensor data:

1. Right-click the tray icon
2. Click **Restart as Admin**
3. Accept the UAC prompt

The app seamlessly restarts itself with elevated privileges. Your dashboard state and settings are preserved.

> **Note:** Some very new CPUs (e.g., Intel Arrow Lake / Core Ultra 200 series) may show N/A for temperature, clock, and power even with admin access. This is a known limitation of the underlying hardware monitoring library ([LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)) which hasn't added full support for these processors yet. WMI fallbacks are used where available.

---

## Architecture

```
TrayStats/
  App.xaml / App.xaml.cs         Entry point, tray icon, context menu, single-instance mutex
  app.manifest                   UAC execution level configuration

  Models/
    CpuData.cs                   CPU model (total load, per-core, temp, clock, power)
    GpuData.cs                   GPU model (load, temp, clocks, VRAM, fan, power)
    RamData.cs                   RAM model (used, available, total, load)
    DiskData.cs                  Disk model (drive info, temp, read/write rates)
    NetData.cs                   Network model (download/upload speeds, formatters)

  Services/
    HardwareMonitorService.cs    LibreHardwareMonitor wrapper + WMI fallbacks for CPU/GPU/RAM
    NetworkMonitorService.cs     Network bandwidth via System.Net.NetworkInformation
    DiskMonitorService.cs        Disk space via System.IO.DriveInfo + LHM for temps

  ViewModels/
    DashboardViewModel.cs        MVVM ViewModel, sparkline data, relay commands

  Views/
    DashboardPopup.xaml(.cs)     WPF popup window with dark theme, positioned near tray
    Components/
      SparklineChart.cs          Custom WPF Canvas control for sparkline rendering

  Helpers/
    IconGenerator.cs             Dynamic 16x16 tray icon bitmap generation
    Converters.cs                WPF value converters (percent-to-width, sensor formatting)
    StartupHelper.cs             Windows startup registry management
```

### Key Libraries

| Library | Purpose |
|---|---|
| [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) | CPU/GPU temps, clocks, fan speeds, power, disk health |
| [H.NotifyIcon.Wpf](https://github.com/HavenDV/H.NotifyIcon) | Modern system tray icon for WPF |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM base classes, ObservableObject, RelayCommand |
| System.Management | WMI queries as fallback for unsupported hardware |

### Data Flow

```
Hardware Sensors (1s polling interval)
    |
    v
HardwareMonitorService / NetworkMonitorService / DiskMonitorService
    |
    v
DashboardViewModel (ObservableCollections, data binding)
    |
    +---> DashboardPopup (WPF UI, sparklines, detail panels)
    +---> App.xaml.cs (tray icon update every 2s)
```

---

## Known Limitations

| Limitation | Detail |
|---|---|
| **Intel Arrow Lake** | Core Ultra 200 series CPUs have incomplete sensor support in LibreHardwareMonitor. Temperature, clock, and power may show N/A even with admin. WMI fallbacks provide partial data |
| **Laptop GPU fans** | Modern NVIDIA laptop GPUs stop fans at idle. Fan speed showing 0 RPM / N/A at low temps is expected behavior |
| **Admin required for full data** | Some sensors need elevated privileges. Use "Restart as Admin" from the tray menu |
| **Single monitor positioning** | The dashboard popup positions relative to the primary taskbar. Multi-monitor setups with taskbars on secondary displays may need manual adjustment |
| **No GPU selection** | On systems with multiple GPUs (e.g., integrated + discrete), the first detected GPU is shown |

---

## Building from Source

### Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build

```powershell
cd TrayStats
dotnet build
```

### Run

```powershell
dotnet run
```

### Publish Self-Contained

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o dist
```

This produces a single-file executable in `./dist/` that includes the .NET runtime. No .NET installation required on the target machine.

### Project Structure

```
TrayStats/
  TrayStats.csproj       Project file (.NET 8, WPF)
  App.xaml(.cs)           Application entry point
  app.manifest            UAC manifest
  Models/                 Data models (MVVM ObservableObjects)
  Services/               Hardware monitoring services
  ViewModels/             Dashboard ViewModel
  Views/                  WPF views and custom controls
  Helpers/                Icon generation, converters, startup
  README.md
```

---

## License

MIT
