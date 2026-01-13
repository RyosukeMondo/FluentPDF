# FluentPDF Windows Development VM

Vagrant-based Windows 11 VM for autonomous build, test, and E2E testing of FluentPDF.

## Prerequisites

### On Linux Host (Ubuntu/Debian)
```bash
# Install Vagrant
sudo apt-get install vagrant

# Install libvirt provider
sudo apt-get install libvirt-daemon-system libvirt-dev
vagrant plugin install vagrant-libvirt

# Add user to libvirt group
sudo usermod -aG libvirt $USER
newgrp libvirt
```

### On macOS/Windows Host
```bash
# Install VirtualBox instead
# Download from https://www.virtualbox.org/
vagrant plugin install vagrant-vbguest
```

## Quick Start

```bash
cd vagrant

# Start the VM (first run takes 20-30 minutes)
vagrant up

# Sync code changes from host to VM
vagrant rsync

# SSH into VM (PowerShell)
vagrant ssh

# Run build and tests
C:\fluentpdf\vagrant\scripts\build-and-test.ps1
```

## VM Details

| Setting | Value |
|---------|-------|
| OS | Windows 11 Enterprise 23H2 |
| CPUs | 4 |
| RAM | 16GB |
| Disk | 100GB |
| RDP | localhost:23389 |
| Credentials | vagrant/vagrant |

## Scripts

### build-and-test.ps1

Comprehensive build and test automation script:

```powershell
# Inside VM via SSH or RDP:

# Build only
C:\fluentpdf\vagrant\scripts\build-and-test.ps1 -Build

# Run unit tests
C:\fluentpdf\vagrant\scripts\build-and-test.ps1 -Test

# Run smoke test (launches app, verifies startup)
C:\fluentpdf\vagrant\scripts\build-and-test.ps1 -Smoke

# Run E2E tests (requires GUI session via RDP)
C:\fluentpdf\vagrant\scripts\build-and-test.ps1 -E2E

# Run everything (Build + Test + Smoke)
C:\fluentpdf\vagrant\scripts\build-and-test.ps1 -All
# Or just:
C:\fluentpdf\vagrant\scripts\build-and-test.ps1
```

## Workflow

### Development Cycle

1. **Edit code on Linux host** (use your preferred IDE)

2. **Sync to VM**:
   ```bash
   vagrant rsync
   ```

3. **Build and test**:
   ```bash
   vagrant ssh -c "C:\fluentpdf\vagrant\scripts\build-and-test.ps1"
   ```

4. **For GUI/E2E testing**, connect via RDP:
   ```bash
   # Linux
   xfreerdp /u:vagrant /p:vagrant /v:localhost:23389 /dynamic-resolution

   # macOS
   open rdp://vagrant:vagrant@localhost:23389
   ```

### Continuous Sync (Watch Mode)

```bash
# Auto-sync on file changes
vagrant rsync-auto
```

## Provisioned Components

The VM is automatically provisioned with:

- **Chocolatey** - Package manager
- **.NET 8 SDK** - Build framework
- **Visual Studio Build Tools 2022** - WinUI 3 compilation
- **Windows App SDK Runtime 1.7** - WinUI 3 runtime
- **pdfium.dll** - PDF rendering library
- **qpdf.dll** - PDF manipulation library
- **RDP** - Remote desktop access

## Troubleshooting

### "FluentPDF not synced" error
```bash
# From host
vagrant rsync
```

### Missing native DLLs
The build script auto-downloads missing DLLs. Or manually:
```powershell
C:\fluentpdf\vagrant\scripts\build-and-test.ps1 -Build
```

### App crashes on startup
Ensure Windows App SDK Runtime is installed:
```powershell
# Check if installed
Get-AppPackage *WindowsAppRuntime*

# Reinstall if needed
$installer = "$env:TEMP\windowsappruntimeinstall.exe"
Invoke-WebRequest -Uri "https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-x64.exe" -OutFile $installer
Start-Process -FilePath $installer -ArgumentList "/quiet" -Wait
```

### E2E tests fail
E2E tests require a GUI session. Connect via RDP first:
```bash
xfreerdp /u:vagrant /p:vagrant /v:localhost:23389
```

Then run E2E tests from inside the RDP session.

### VM won't start (libvirt)
```bash
sudo systemctl start libvirtd
sudo systemctl enable libvirtd
```

## Cleaning Up

```bash
# Stop VM
vagrant halt

# Destroy VM completely
vagrant destroy -f

# Remove box image
vagrant box remove gusztavvargadr/windows-11-23h2-enterprise
```
