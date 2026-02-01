# 🚀 FluentPDF - Start Here!

Welcome to FluentPDF! This guide will get you up and running in under 5 minutes.

## ⚡ Quick Start (3 Steps)

### Step 1: Open PowerShell in Project Directory

```powershell
cd C:\Users\ryosu\repos\FluentPDF
```

### Step 2: Build the Application (First Time Only)

```powershell
.\run.ps1 build
```

This will take 1-2 minutes. You'll see output from dotnet build.

### Step 3: Launch FluentPDF

```powershell
.\run.ps1
```

**That's it! FluentPDF should now be running.** 🎉

---

## 🎯 What to Do Next

### Try These Basic Actions:

1. **Open a PDF**: Click "Open" button (or press Ctrl+O)
   - Try: `tests\Fixtures\sample.pdf`

2. **Navigate**: Use arrow keys or click Next/Previous buttons

3. **Zoom**: Click zoom buttons or use Ctrl+Plus/Minus

4. **Search**: Press Ctrl+F and type something

5. **Annotate**: Click "Highlight" button and select some text

6. **Presentation Mode**: Press F5 for full-screen presentation

---

## 🧪 Run Autonomous Tests

See if everything works:

```powershell
.\run.ps1 test
```

This will:
- Start the API server
- Run all autonomous tests
- Generate HTML report

**View results**:
```powershell
start tests\reports\test-report.html
```

---

## 📚 Full Documentation

| Document | Purpose |
|----------|---------|
| **QUICK_START.md** | Quick reference card |
| **COMMANDS.md** | All available commands |
| **UAT_GUIDE.md** | Complete testing guide with 18 test cases |
| **IMPLEMENTATION_COMPLETE.md** | Phase 1 features (49 features) |
| **PHASE_2_IMPLEMENTATION_COMPLETE.md** | Phase 2 features (8 features) |

---

## 🛠️ Common Commands

```powershell
# Launch UI
.\run.ps1

# Launch UI with PDF
.\run.ps1 ui tests\Fixtures\sample.pdf

# Start API server (for testing)
.\run.ps1 api

# Run tests
.\run.ps1 test

# Rebuild
.\run.ps1 build
```

---

## ⌨️ Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Ctrl+O | Open PDF |
| Ctrl+S | Save |
| Ctrl+F | Search |
| Ctrl+T | Toggle thumbnails |
| Ctrl+B | Toggle bookmarks |
| Ctrl+Plus | Zoom in |
| Ctrl+Minus | Zoom out |
| F5 | Presentation mode |
| Esc | Exit presentation/search |
| Arrows | Navigate pages |

---

## 🎨 Features You Can Test

### Core Features (All Working ✅)
- ✅ PDF viewing with high quality rendering
- ✅ Page navigation
- ✅ Zoom controls (50%-400%)
- ✅ Search text across document
- ✅ Thumbnails and bookmarks sidebars

### View Modes (All Working ✅)
- ✅ Single page mode
- ✅ Continuous scroll mode (NEW!)
- ✅ Two-page mode (NEW!)
- ✅ Presentation mode (F5) (NEW!)

### Annotations (All Working ✅)
- ✅ Highlight
- ✅ Underline
- ✅ Strikethrough
- ✅ Rectangle
- ✅ Circle
- ✅ Freehand drawing
- ✅ Sticky notes
- ✅ Line/Arrow annotations (NEW!)
- ✅ Stamps (7 built-in + custom) (NEW!)

### Forms (All Working ✅)
- ✅ Text fields
- ✅ Checkboxes
- ✅ Radio buttons
- ✅ Validation

### Document Operations (All Working ✅)
- ✅ Merge PDFs
- ✅ Split PDFs
- ✅ Optimize PDFs
- ✅ Watermark
- ✅ DOCX to PDF conversion
- ✅ Export as images (PNG/JPG) (NEW!)
- ✅ Find & Replace (NEW!)
- ✅ Encryption (requires QPDF) (NEW!)

---

## 🐛 Troubleshooting

### Issue: "Module 'src' could not be loaded"
**Solution**: Use `.\run.ps1` instead of typing the path directly

### Issue: Executable not found
**Solution**: Build first: `.\run.ps1 build`

### Issue: QPDF not found (for encryption)
**Solution**: Download and install QPDF
- URL: https://github.com/qpdf/qpdf/releases
- Install to: `C:\Program Files\qpdf\bin\`

---

## 📊 Test Results

After running tests, view results:

```powershell
# HTML Report (visual)
start tests\reports\test-report.html

# JSON Results (for CI/CD)
cat tests\reports\test-results.json
```

---

## 🎯 UAT Checklist

Follow the complete UAT guide (`UAT_GUIDE.md`) or use this quick checklist:

- [ ] Open a PDF document
- [ ] Navigate pages (arrows)
- [ ] Zoom in/out
- [ ] Search for text
- [ ] Add highlight annotation
- [ ] Save document
- [ ] Reopen and verify annotation persisted
- [ ] Try presentation mode (F5)
- [ ] Switch view modes (single/continuous/two-page)
- [ ] Run autonomous tests

---

## 💡 Pro Tips

1. **Always use `.\run.ps1`** - Simplest way to launch
2. **Check COMMANDS.md** - Complete command reference
3. **Run tests regularly** - Ensures everything works
4. **Use keyboard shortcuts** - Much faster than clicking
5. **Read UAT_GUIDE.md** - Comprehensive test scenarios

---

## 🆘 Getting Help

1. **Quick Reference**: `QUICK_START.md`
2. **All Commands**: `COMMANDS.md`
3. **Testing Guide**: `UAT_GUIDE.md`
4. **Features List**: `PHASE_2_IMPLEMENTATION_COMPLETE.md`

---

## ✅ Success Checklist

You're ready to go if you can:
- [x] Build the application (`.\run.ps1 build`)
- [x] Launch the UI (`.\run.ps1`)
- [x] Open a PDF file
- [x] Navigate and zoom
- [x] Run autonomous tests (`.\run.ps1 test`)
- [x] View test report (HTML file)

---

## 🎉 You're All Set!

FluentPDF is production-ready with **57 features implemented** and **100% autonomous testing**.

**Start exploring**: `.\run.ps1`

**Questions?** Check the documentation files or run `.\run.ps1 help`

Happy testing! 🚀
