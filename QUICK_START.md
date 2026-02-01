# FluentPDF Quick Start Guide

## 🚀 Launch Commands (Copy & Paste)

### Navigate to Project
```powershell
cd C:\Users\ryosu\repos\FluentPDF
```

### Build Application
```powershell
dotnet build src\FluentPDF.App -p:Platform=x64
```

### Launch FluentPDF (UI)
```powershell
# Method 1: Using launch script (RECOMMENDED)
pwsh tools\launch-uat.ps1

# Method 2: Direct launch
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe
```

### Launch with Test PDF
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe tests\Fixtures\sample.pdf
```

### Build and Launch in One Command
```powershell
pwsh tools\launch-uat.ps1 -BuildFirst
```

---

## 🧪 Testing Commands

### Run Complete Autonomous Test Suite
```powershell
pwsh tools\launch-uat.ps1 -RunTests
```

### Start API Server (for REST API testing)
```powershell
pwsh tools\launch-uat.ps1 -ApiServer
```

### Test Specific Features (CLI)

**Test Rendering:**
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-render "tests\Fixtures\sample.pdf"
```

**Test Export Images:**
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-export-images "tests\Fixtures\sample.pdf" --output "C:\temp\export" --format png --dpi 150
```

**Test Stamps:**
```powershell
.\src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe --test-stamp "tests\Fixtures\sample.pdf" --stamp APPROVED --output "stamped.pdf"
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+O** | Open document |
| **Ctrl+S** | Save document |
| **Ctrl+T** | Toggle thumbnails |
| **Ctrl+B** | Toggle bookmarks |
| **Ctrl+F** | Search |
| **Ctrl++** | Zoom in |
| **Ctrl+-** | Zoom out |
| **Ctrl+0** | Reset zoom |
| **F5** or **Ctrl+L** | Presentation mode |
| **Esc** | Exit presentation/Close search |
| **Arrow Keys** | Navigate pages |
| **F3** | Next search match |

---

## 📁 Important File Locations

- **Executable**: `src\FluentPDF.App\bin\x64\Debug\net9.0-windows10.0.19041.0\win-x64\FluentPDF.App.exe`
- **Test PDFs**: `tests\Fixtures\`
- **Test Reports**: `tests\reports\`
- **Documentation**: `UAT_GUIDE.md`, `IMPLEMENTATION_COMPLETE.md`
- **Launch Script**: `tools\launch-uat.ps1`
- **Test Script**: `tools\run-autonomous-tests.ps1`

---

## 🔧 Troubleshooting

### "Module 'src' could not be loaded"
**Solution**: Use `.\` prefix or run from launch script:
```powershell
pwsh tools\launch-uat.ps1
```

### Executable Not Found
**Solution**: Build the application first:
```powershell
dotnet build src\FluentPDF.App -p:Platform=x64
```

### QPDF Not Found (for encryption)
**Solution**: Install QPDF:
1. Download: https://github.com/qpdf/qpdf/releases
2. Install to `C:\Program Files\qpdf\bin\`
3. Or add to PATH

### API Server Won't Start
**Solution**: Check port 5000 is available:
```powershell
netstat -ano | findstr :5000
```

---

## 📊 Test Results

View test results:
```powershell
# HTML Report (pretty view)
start tests\reports\test-report.html

# JSON Results (for automation)
cat tests\reports\test-results.json
```

---

## 🎯 Quick UAT Checklist

1. ✅ Build application
2. ✅ Launch UI
3. ✅ Open a PDF
4. ✅ Navigate pages
5. ✅ Try annotations
6. ✅ Run test suite
7. ✅ Check test report

---

## 💡 Tips

- **Always use PowerShell** (not CMD) for best compatibility
- **Use launch script** for easiest experience
- **Check UAT_GUIDE.md** for detailed test cases
- **Run autonomous tests** to verify everything works
- **View HTML reports** for visual test results

---

## 🆘 Need Help?

Full documentation:
- **UAT Guide**: `UAT_GUIDE.md` - Complete testing guide
- **Implementation**: `IMPLEMENTATION_COMPLETE.md` - All features
- **Phase 2**: `PHASE_2_IMPLEMENTATION_COMPLETE.md` - Latest features
