# Quick Start - Autonomous Testing

## ✅ Status: FULLY OPERATIONAL

Autonomous E2E testing via REST API is **100% functional**!

## 🐛 UI Freeze Issue - FIXED (2026-01-29)

**Problem**: Normal app startup (without `--api-server`) caused UI freeze
**Root Cause**: Window shown before UI message loop started
**Fix**: Removed explicit `.Show()` calls, call `base.OnFrameworkInitializationCompleted()` at correct time
**Details**: See `UI_FREEZE_FIX.md`

✅ Both normal and API server modes now work correctly!

## Run Test (1 Command)

```powershell
pwsh tools/test-fully-autonomous.ps1
```

**Expected Result**:
```
RESULT: ✅ SUCCESS
File opened successfully! All 8 steps completed.
Document loaded! Pages: 4
```

## What It Tests

1. **App Launch** - Starts FluentPDF with API server
2. **Health Check** - Verifies API is responding
3. **File Load** - Triggers PDF open via REST API
4. **Real-Time Monitoring** - Watches logs for progress
5. **Verification** - Confirms document loaded successfully

## REST API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check + version info |
| GET | `/api/logs` | Real-time logs (last 100 entries) |
| GET | `/api/logs/level/{level}` | Filter logs by level |
| GET | `/api/gui/state` | Current GUI state (tabs, active document) |
| POST | `/api/gui/action/open-file` | Load a PDF document |
| GET | `/api/health/watchdog` | Operation monitoring status |

## Manual API Testing

```bash
# Start app with API server
.\src\FluentPDF.Avalonia\bin\Release\net8.0\FluentPDF.Avalonia.exe --api-server --port 5000

# Health check
curl http://localhost:5000/api/health

# Load document
curl -X POST http://localhost:5000/api/gui/action/open-file \
  -H "Content-Type: application/json" \
  -d '{"filePath":"C:/Users/ryosu/repos/FluentPDF/tests/Fixtures/sample-with-text.pdf"}'

# Get logs
curl http://localhost:5000/api/logs

# Get errors only
curl http://localhost:5000/api/logs/level/Error
```

## Success Indicators

✅ **Test Passes When**:
- API server starts within 30 seconds
- File load request completes successfully
- Document detected with pages > 0
- No critical errors in logs

❌ **Test Fails When**:
- API server doesn't respond to health check
- File load times out (>30 seconds)
- Document not loaded after request
- Critical errors detected

## Troubleshooting

**Problem**: API server doesn't start
- **Check**: Port 5000 not already in use
- **Fix**: Use custom port: `test-fully-autonomous.ps1 -Port 8080`

**Problem**: File load times out
- **Check**: PDF file exists at specified path
- **Fix**: Verify file path in test script

**Problem**: "Window must be visible" error
- **Cause**: Avalonia UI thread requires visible window
- **Fix**: Test script already handles this (removes -WindowStyle Hidden)

## Key Files

| File | Purpose |
|------|---------|
| `tools/test-fully-autonomous.ps1` | Main test script |
| `src/FluentPDF.Avalonia/Services/LogBufferService.cs` | Log capture |
| `src/FluentPDF.Avalonia/Services/OperationWatchdog.cs` | Hang detection |
| `src/FluentPDF.Avalonia/Api/Endpoints/GuiStateEndpoints.cs` | GUI REST API |

## Documentation

- **Complete Details**: See `AUTONOMOUS_TESTING_SUCCESS.md`
- **All Issues Fixed**: See `FINAL_FIX_NEEDED.md` (now resolved!)
- **Troubleshooting**: See `UI_THREAD_DEADLOCK_ANALYSIS.md`

## Next Steps

1. ✅ **DONE** - Autonomous testing works!
2. Run test before commits: `pwsh tools/test-fully-autonomous.ps1`
3. Add to CI/CD pipeline for automated verification
4. Extend with more test scenarios as needed

---

**Status**: ✅ All issues fixed and autonomous testing operational!

Last test run: 2026-01-29 15:04:38
Result: ✅ SUCCESS - Document loaded! Pages: 4
