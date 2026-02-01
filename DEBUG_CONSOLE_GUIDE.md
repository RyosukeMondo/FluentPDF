# 🐛 Debug Console - Visual Real-Time Logging

## What's New

✅ **Real-time debug console at the bottom of the main window**
✅ **Visual feedback showing every operation as it happens**
✅ **Copy last 50 logs to clipboard**
✅ **Download full log file**
✅ **Color-coded log levels (Error=Red, Warning=Orange, Info=Blue)**
✅ **Auto-scroll to latest logs**
✅ **500ms refresh rate - see logs instantly**

---

## How to Use

### 1. Launch the App

```bash
cd src/FluentPDF.Avalonia/bin/Release/net8.0
./FluentPDF.Avalonia.exe
```

### 2. Look at the Bottom of the Window

You'll see a **Debug Console** panel with:
- **Real-time log stream** showing every operation
- **Timestamp** (HH:mm:ss.fff) - millisecond precision
- **Level** (ERROR, WARN, INFO, DEBUG) - color-coded
- **Source** (which component logged it)
- **Message** (what happened)

### 3. Try to Open a File

1. Click "Open File" or press Ctrl+O
2. Select a PDF file
3. **WATCH THE DEBUG CONSOLE** at the bottom

You should see logs like:
```
[14:23:45.123] [INFO] [MainWindow] >>> USER CLICKED: Open file: C:/test.pdf
[14:23:45.125] [INFO] [MainWindow] >>> Calling ViewModel.OpenRecentFileCommand...
[14:23:45.127] [INFO] [MainViewModel] [1/8] Starting OpenFileInTabAsync for: C:/test.pdf
[14:23:45.130] [INFO] [MainViewModel] [2/8] File not already open. Current tab count: 0
[14:23:45.132] [INFO] [MainViewModel] [3/8] Requesting PdfViewerViewModel from DI container...
```

**If something fails, you'll see RED ERROR logs with full details!**

### 4. Buttons

- **Copy Last 50** - Copies last 50 log entries to clipboard (paste anywhere)
- **Download Log** - Saves full log file (1000 entries) to disk
- **Clear** - Clears the console (starts fresh)
- **▼ Toggle** - Show/hide the console panel

---

## What to Look For

### ✅ Success Pattern

If file opening works, you'll see:
```
[INFO] [MainViewModel] [1/8] Starting OpenFileInTabAsync...
[INFO] [MainViewModel] [2/8] File not already open...
[INFO] [MainViewModel] [3/8] Requesting PdfViewerViewModel...
[INFO] [MainViewModel] [4/8] TabLogger created...
[INFO] [MainViewModel] [5/8] TabViewModel created successfully
[INFO] [MainViewModel] [6/8] Tab added (new count: 1)
[INFO] [MainViewModel] [7/8] LoadDocumentFromPathAsync completed!
[INFO] [MainViewModel] [8/8] SUCCESS! File opened in new tab
```

### ❌ Failure Pattern

If file opening fails, you'll see:
```
[INFO] [MainViewModel] [1/8] Starting OpenFileInTabAsync...
[INFO] [MainViewModel] [2/8] File not already open...
[INFO] [MainViewModel] [3/8] Requesting PdfViewerViewModel...
[ERROR] [MainViewModel] [ERROR] CRITICAL: Failed to open file in tab
[ERROR] [MainViewModel] [ERROR] Exception Type: InvalidOperationException
[ERROR] [MainViewModel] [ERROR] Exception Message: Unable to resolve service...
[ERROR] [MainViewModel] [ERROR] Stack Trace: ...
```

**The ERROR logs will tell us EXACTLY what failed!**

---

## Common Error Scenarios

### Error: "Unable to resolve service"
**What it means**: A required service is missing from DI
**What to do**: Copy logs and share - shows which service is missing

### Error: "Call from invalid thread"
**What it means**: UI object created on background thread
**What to do**: Copy logs - shows which ViewModel has threading bug

### Error: "File not found"
**What it means**: PDF file path is invalid
**What to do**: Check file path in logs

### No logs appearing when clicking "Open File"
**What it means**: Event handler not firing
**What to do**: Copy logs showing startup - check if console initialized

---

## Testing Steps

### Test 1: Check Console is Working

1. Launch app
2. Look at bottom of window
3. You should see:
   ```
   [INFO] [MainWindow] Debug console initialized - real-time logging enabled
   [INFO] [MainWindow] Click 'Copy Last 50' to copy recent logs to clipboard
   [INFO] [MainWindow] Click 'Download Log' to save full log file
   ```

✅ If you see these logs → Console is working!
❌ If console is empty → Console initialization failed (report this!)

### Test 2: Open File

1. Click "Open File" or press Ctrl+O
2. **IMMEDIATELY look at debug console** - logs appear within 500ms
3. Select a PDF file
4. Watch the console scroll as operations happen

**What to report:**
- Copy last 50 logs (Click "Copy Last 50" button)
- Paste in issue report
- Tell me exactly what you saw (success or error)

### Test 3: Download Full Log

1. Click "Download Log" button
2. Save the file (e.g., `fluentpdf-debug-20260129-142345.log`)
3. Open in text editor
4. You'll see ALL logs with timestamps

---

## Why This is Better Than Before

### Before ❌
- Had to check REST API with curl
- Needed to run PowerShell scripts
- No visual feedback
- Manual debugging required

### Now ✅
- **See everything in real-time**
- **Logs visible in the UI** - no external tools
- **Copy/download with one click**
- **Color-coded for easy scanning**
- **Auto-scroll to latest** - never miss anything

---

## Troubleshooting

### Console Not Visible
- Check if window is tall enough (console needs 200px height)
- Click the toggle button (▼) - might be collapsed
- Look for the "Debug Console" header at bottom

### Logs Not Updating
- Check if timer is running (should update every 500ms)
- Try clicking "Clear" to reset
- Restart app if frozen

### Can't Copy Logs
- Make sure clipboard permissions are working
- Try "Download Log" instead
- Check if app has system permissions

---

## Next Steps

1. **Launch the app right now**
2. **Try to open a file**
3. **Copy the last 50 logs** (button at bottom)
4. **Paste them here** so I can see exactly what's failing

The debug console shows **EVERYTHING** - no more guessing!

**This is true "no UAT" debugging - you can SEE the logs in real-time!** 🎯
