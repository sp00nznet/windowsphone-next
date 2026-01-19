# Windows Phone Next Build Tool

A modern, Windows Phone Next-themed Win32 application for building and deploying the Windows Phone Next operating system.

## Features

- **Intuitive GUI**: Windows Phone Next-themed interface with real-time progress updates
- **ISO Management**: Browse for Windows 11 ISO or automatically download Windows 11 IoT Enterprise LTSC
- **Complete Build Pipeline**:
  - Build all 19 Windows Phone applications
  - Download and integrate LattePanda 3 Delta drivers
  - Create bootable Windows Phone Next ISO image
  - Generate USB creation scripts
- **Real-time Logging**: Live build output with timestamps
- **Progress Tracking**: Visual progress indicators for each build step
- **Flexible Options**: Enable/disable individual build steps as needed

## Requirements

- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** (included with SDK if building)
- **Administrator Privileges** (required for ISO/image manipulation)
- **Windows ADK** (optional, for ISO creation)
  - Download: https://go.microsoft.com/fwlink/?linkid=2196127
- **DISM** (included with Windows)

## Installation

### Option 1: Build from Source

```powershell
# Navigate to BuildTool directory
cd BuildTool

# Build the application
dotnet build -c Release

# Run the application
.\bin\Release\net8.0-windows\WindowsPhoneNextBuildTool.exe
```

### Option 2: Use Pre-built Binary

1. Download the latest release
2. Right-click `WindowsPhoneNextBuildTool.exe`
3. Select "Run as administrator"

## Usage

### Quick Start

1. **Launch** the application as Administrator
2. **Select ISO** (optional):
   - Click "Browse..." to select a Windows 11 ISO
   - Or leave unchecked to auto-download Windows 11 IoT Enterprise LTSC
3. **Choose Build Options**:
   - ✓ Clean build (removes previous artifacts)
   - ✓ Build all applications (19 WPF apps)
   - ✓ Download drivers (LattePanda 3 Delta)
   - ✓ Create bootable ISO image
   - ✓ Generate USB creation script
4. **Click "Start Build"**
5. **Monitor Progress** in the log window

### Build Options Explained

| Option | Description | Required |
|--------|-------------|----------|
| **Clean Build** | Removes previous build artifacts (bin/obj folders) | No |
| **Build Applications** | Compiles all 19 Windows Phone apps with .NET 8.0 | Yes |
| **Download Drivers** | Downloads 8 LattePanda 3 Delta driver packages | Recommended |
| **Create ISO Image** | Creates bootable WindowsPhoneNext.iso with DISM | Yes (for deployment) |
| **Generate USB Script** | Creates create-usb.ps1 for bootable USB creation | Recommended |

### Output Locations

After a successful build:

```
WindowsPhone-Next/
├── Output/                     # Built applications
│   ├── WindowsPhoneLauncher/
│   ├── WindowsPhoneDialer/
│   └── ... (17 more apps)
├── ImageWork/                  # Image creation workspace
│   ├── ISO/                    # Extracted Windows ISO
│   ├── Mount/                  # Mounted Windows image
│   ├── create-usb.ps1         # USB creation script
│   └── WindowsPhoneNext.iso   # Bootable ISO (if created)
└── Drivers/                    # Downloaded drivers
```

## Build Process Steps

The build tool orchestrates the following process:

1. **Prerequisites Check**
   - Verifies administrator privileges
   - Checks for .NET 8.0 SDK
   - Validates DISM and oscdimg availability

2. **Clean (Optional)**
   - Removes bin/obj folders
   - Clears Output directory

3. **Build Applications**
   - Restores NuGet packages
   - Builds shared libraries (ModemLib, BlockingService, SharedServices)
   - Compiles 19 WPF applications
   - Generates apps-manifest.json

4. **Download Drivers (Optional)**
   - Downloads 8 LattePanda 3 Delta drivers
   - Extracts to Drivers/ directory

5. **Download ISO (If needed)**
   - Downloads Windows 11 IoT Enterprise LTSC
   - Saves to project root

6. **Create Image**
   - Extracts Windows ISO
   - Mounts install.wim with DISM
   - Injects drivers into Windows image
   - Copies applications to C:\WindowsPhoneNext\Apps\
   - Configures first-boot setup
   - Creates bootable ISO

7. **Generate USB Script**
   - Creates create-usb.ps1 for USB deployment

## Troubleshooting

### "Administrator Privileges Required"

**Solution**: Right-click the application and select "Run as administrator"

### "DISM not found"

**Solution**: DISM is included with Windows. Ensure you're running on Windows 10/11.

### "oscdimg.exe not found"

**Solution**: Install Windows ADK:
```
https://go.microsoft.com/fwlink/?linkid=2196127
```

Select "Deployment Tools" during installation.

### "Build failed"

**Solutions**:
1. Ensure .NET 8.0 SDK is installed
2. Check build log for specific errors
3. Try enabling "Clean build" option
4. Verify all project files are present

### "ISO creation failed"

**Solutions**:
1. Verify ISO path is correct
2. Ensure ISO is Windows 11 (not Windows 10)
3. Check available disk space (requires ~15GB)
4. Run as Administrator

## Advanced Usage

### Manual Script Execution

The build tool uses PowerShell scripts from the `Build/` directory:

```powershell
# Build applications only
.\Build\build-all.ps1 -Configuration Release

# Create image with custom ISO
.\Build\create-image.ps1 -IsoPath "C:\custom.iso"

# Full deployment with auto-download
.\Build\deploy.ps1 -DownloadIso
```

### Custom Output Paths

Modify project output paths in the build tool source:

```csharp
// MainWindow.xaml.cs
string outputPath = Path.Combine(projectRoot, "CustomOutput");
```

## Architecture

### Technology Stack

- **Framework**: .NET 8.0 Windows
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Scripting**: PowerShell 5.1+ (System.Management.Automation)
- **Image Tools**: DISM, oscdimg (Windows ADK)

### Project Structure

```
BuildTool/
├── WindowsPhoneNextBuildTool.csproj
├── App.xaml                    # Application entry
├── App.xaml.cs                 # Admin privilege check
├── MainWindow.xaml             # Main UI
├── MainWindow.xaml.cs          # Build orchestration logic
├── app.manifest                # UAC elevation manifest
├── Themes/
│   └── BuildToolTheme.xaml    # Windows Phone Next theme
└── README.md
```

### Key Components

1. **PowerShell Integration**: Uses `System.Management.Automation` to execute build scripts
2. **Real-time Output**: Captures PowerShell streams for live logging
3. **Progress Tracking**: Task-based progress calculation
4. **Cancellation Support**: Async cancellation tokens for stopping builds
5. **Theme System**: Windows Phone Next dark theme with accent colors

## Theme Customization

The build tool uses the Windows Phone Next color palette:

```xaml
<!-- Primary Colors -->
PrimaryBackground: #1F1F1F (Dark Gray)
SecondaryBackground: #2D2D2D (Medium Gray)
Accent: #0078D7 (Windows Blue)
Text: #FFFFFF (White)

<!-- Status Colors -->
Success: #2ECC71 (Green)
Error: #E74C3C (Red)
Warning: #F39C12 (Orange)
```

Modify `Themes/BuildToolTheme.xaml` to customize colors.

## Contributing

To contribute to the Windows Phone Next Build Tool:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with full build process
5. Submit a pull request

## License

This project is part of the Windows Phone Next ecosystem.

## Support

For issues or questions:
- Open an issue on GitHub
- Check existing documentation in `/docs`
- Review PowerShell scripts in `/Build`

---

**Windows Phone Next Build Tool v1.0**
*Making Windows Phone Next builds accessible to everyone*
