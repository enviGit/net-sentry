# NetSentry WPF Dashboard

<img width="1094" height="649" alt="netSentry" src="https://github.com/user-attachments/assets/609d3446-a5d3-4ad8-bd37-7f1fcd56aa10" />

**NetSentry** is a high-performance system monitoring dashboard and threat hunting tool designed with a Cyberpunk aesthetic. Built with **.NET 10**, **WPF**, and **MVVM** architecture, it demonstrates advanced UI styling, real-time data visualization, and system diagnostics integration.

## 📥 Download & Demo

Want to see the dashboard in action without compiling?
You can download the latest stable executable from the releases page.

[**👉 Download Latest Release (v1.0.0)**](https://github.com/enviGit/net-sentry/releases/latest)

---

## 🚀 Key Features

### 1. Dual-Layer Real-Time Monitoring
* **CPU & RAM Visualization:** Monitor system load with fluid, real-time charts powered by *LiveCharts*.
* **Dual Mode:** Compare CPU and RAM usage simultaneously on a single graph to identify performance correlations.
* **Dynamic Theme Engine:** The entire UI (borders, charts, buttons) changes color dynamically based on system load (Cyan → Yellow → Red).

### 2. Threat Hunter Module
* **Process Audit:** Scans running processes to identify "Ghost Processes" (high memory usage with no visible window), a common characteristic of background malware.
* **Smart Whitelisting:** Intelligently ignores known Windows system processes and developer tools (Visual Studio, Roslyn) to reduce false positives.

### 3. Network Diagnostics
* **Port Scanner:** Asynchronous TCP port scanning to identify open ports on the local machine.
* **Non-Blocking UI:** Uses `async/await` patterns to ensure the dashboard remains responsive during intensive scans.

### 4. System Integrity
* **Auto-Repair:** Detecting corrupted Performance Counters in the Windows Registry and offering a one-click fix (requires Admin privileges/UAC).

---

## 🛠 Technical Stack

* **Framework:** .NET 10 (Desktop)
* **UI Framework:** WPF (Windows Presentation Foundation)
* **Architecture:** MVVM (Model-View-ViewModel) using `CommunityToolkit.Mvvm`
* **Visualization:** LiveCharts
* **Styling:** Custom ResourceDictionaries, LinearGradients, DropShadows (No 3rd party UI libraries used for styling).

### Architecture Overview

The project follows strict **Separation of Concerns**:

* **`ViewModels/MainViewModel.cs`**: Handles UI logic, state management, and data binding.
* **`Services/`**:
    * `SystemMonitorService.cs`: Wraps `PerformanceCounter` and low-level system APIs.
    * `ProcessService.cs`: Handles process filtration and heuristic analysis.
    * `NetworkService.cs`: Manages asynchronous socket connections.
* **`Resources/Styles.xaml`**: Centralized styling for a consistent "Glassmorphism" & Neon look.

---

## 💻 Getting Started

### Prerequisites
* Visual Studio 2026
* .NET 10 SDK

### Installation
1.  Clone the repository:
    ```bash
    git clone [https://github.com/yourusername/NetSentry-WPF-Dashboard.git](https://github.com/yourusername/NetSentry-WPF-Dashboard.git)
    ```
2.  Open `NetSentry_Dashboard.sln` in Visual Studio.
3.  Restore NuGet packages.
4.  Build and Run (F5).

*Note: For the "System Repair" feature to work, the application must be able to trigger a UAC prompt.*

---

## 🎨 Visual Design

The interface is designed to mimic a futuristic HUD:
* **Stealth Scrollbars:** Custom styling for TextBoxes to match the dark theme.
* **Glow Effects:** Text and borders emit a neon glow corresponding to the current system status.
* **Holographic Borders:** Panels use gradient borders to simulate depth and lighting.

---

## 📄 License
This project is open-source and available under the [MIT License](LICENSE).
