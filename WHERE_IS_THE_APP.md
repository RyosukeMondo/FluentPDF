# 📍 Where is the Latest FluentPDF Build?

## ✅ CORRECT PATH (Use This!)

```
C:\Users\ryosu\repos\FluentPDF\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe
```

## 🚀 Quick Launch (Recommended)

### Option 1: PowerShell Script (Easiest)
```powershell
# Normal launch (with debug console)
pwsh RUN_LATEST.ps1

# With REST API server
pwsh RUN_LATEST.ps1 --api
```

### Option 2: Direct Launch
```powershell
cd src/FluentPDF.Avalonia/bin/Release/net8.0
./FluentPDF.Avalonia.exe
```

### Option 3: With API Server
```powershell
cd src/FluentPDF.Avalonia/bin/Release/net8.0
./FluentPDF.Avalonia.exe --api-server --port 5000
```

## ❌ OLD/OBSOLETE Paths (Don't Use!)

- ❌ `releases/v1.0.0/win-x64/` - Old release, missing debug console
- ❌ Any other `releases/` subdirectories

**Note**: The `releases/` directory can be safely deleted. It contains old builds without the latest fixes.

## 🔨 How to Rebuild

```bash
# Build latest version
dotnet build src/FluentPDF.Avalonia/FluentPDF.Avalonia.csproj -c Release

# Or build all projects
dotnet build -c Release
```

## 📊 What's New in Latest Build?

✅ Debug console at bottom of window
✅ Real-time log streaming
✅ Threading bugs fixed
✅ All DI services registered
✅ Color-coded logs (Error=Red, Info=Blue)
✅ Copy/Download log buttons

## 🎯 Quick Test

1. Run: `pwsh RUN_LATEST.ps1`
2. Look at **BOTTOM** of window - you should see "Debug Console" panel
3. Click "Open File" and watch logs appear in real-time!
4. If file opening fails, click "Copy Last 50" and paste logs to debug

---

**Always use `RUN_LATEST.ps1` or the path in `bin/Release/net8.0` - never use old `releases/` directory!**
