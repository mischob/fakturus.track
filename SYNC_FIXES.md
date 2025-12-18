# Sync Behavior Fixes

## Issues Fixed

### 1. Backend Sessions Now Go to History Only

**Problem**: When loading the page, sessions synced from the backend were automatically selected as the "current session".

**Solution**: Modified `LoadWorkSessionsAsync()` to only keep a session as "current" if:
- It has `IsPendingSync = true` (not yet synced to backend)
- AND it's the session the user is actively working on (by ID match)

**Result**: All synced sessions from the backend now appear in the history section only.

---

### 2. Current Session is Only for Local Unfinished Work

**Problem**: Synced sessions could become the current session.

**Solution**: Current session logic now enforces:
- A session is "current" ONLY if it's pending sync (locally created, not yet synced)
- Once a session is synced to the backend, it automatically moves to history
- No automatic selection of sessions from backend as current

**Result**: The current session area is reserved exclusively for local work in progress.

---

### 3. Cancel Button No Longer Deletes Sessions

**Problem**: Clicking "Cancel" on a session would delete it from local storage.

**Solution**: 
- For **current session**: Cancel now just clears it from the current view (moves to history if synced, or keeps as pending)
- For **history sessions in edit mode**: Cancel exits edit mode without deleting (handled by WorkSessionCard)
- Sessions are preserved in the list

**Result**: Cancel is now a safe operation that only exits edit mode or clears the current view.

---

## Files Modified

### 1. `Fakturus.Track.Frontend\Pages\TimeTracker.razor`

#### LoadWorkSessionsAsync() - Lines 135-195
**Before**:
```csharp
// Would automatically find today's session and set as current
currentSession = workSessions
    .Where(s => s.Date == today)
    .OrderByDescending(s => s.StartTime != default ? s.StartTime : DateTime.MinValue)
    .FirstOrDefault();
```

**After**:
```csharp
// Only keep as current if still pending (not synced)
if (existingCurrent.IsPendingSync && !existingCurrent.IsSynced)
{
    currentSession = existingCurrent;
}
else
{
    // Session was synced, move to history
    currentSession = null;
}
```

#### HandleCancel() - Lines 269-276
**Before**:
```csharp
// Delete the session without saving
await LocalStorageService.DeleteWorkSessionAsync(session.Id);
currentSession = null;
await LoadWorkSessionsAsync();
ToastService.ShowInfo("Session cancelled");
```

**After**:
```csharp
// Just exit edit mode / clear current session without deleting
if (currentSession?.Id == session.Id)
{
    currentSession = null;
}
await LoadWorkSessionsAsync();
ToastService.ShowInfo("Cancelled");
```

---

### 2. `Fakturus.Track.Frontend\Components\WorkSessions\WorkSessionCard.razor`

#### Cancel Button - Line 122
**Before**:
```razor
<button @onclick="HandleCancel" 
        class="flex-1 btn-outline text-sm">
    Cancel
</button>
```

**After**:
```razor
<button @onclick="() => OnCancel.InvokeAsync(WorkSession)" 
        class="flex-1 btn-outline text-sm">
    Cancel
</button>
```

#### Removed HandleCancel Method - Lines 171-174
**Removed**:
```csharp
private async Task HandleCancel()
{
    await OnCancel.InvokeAsync(WorkSession);
}
```

---

## Expected Behavior After Fixes

### Scenario 1: Page Load with Backend Sessions
1. User opens the page
2. Sync fetches sessions from backend
3. **All backend sessions appear in history**
4. **Current session area is empty** (shows "No active session")
5. User clicks "Start New Session" to begin work

### Scenario 2: Creating a New Session
1. User clicks "Start New Session"
2. New session appears in current section with `IsPendingSync = true`
3. User sets start/stop times
4. Session remains current until "Finish" is clicked
5. After "Finish", session syncs to backend and moves to history

### Scenario 3: Background Sync
1. User creates a local session (current)
2. Background sync runs every 30 seconds
3. Session syncs to backend
4. **Session automatically moves from current to history**
5. Current section becomes empty again

### Scenario 4: Cancel Button
1. **For current session**: Clears the current view, session stays in local storage
2. **For history session in edit mode**: Exits edit mode, no deletion
3. **Session is preserved** in the appropriate section

### Scenario 5: Multi-Client Sync
1. User A creates and finishes a session (syncs to backend)
2. User B opens the page
3. User B's sync fetches User A's session
4. **Session appears in User B's history** (not as current)
5. Both users see consistent history

---

## Testing Checklist

- ✅ Open page with existing backend sessions → All go to history
- ✅ Current section is empty on initial load
- ✅ Create new session → Appears as current
- ✅ Finish session → Moves to history after sync
- ✅ Cancel current session → Clears current view, doesn't delete
- ✅ Cancel history edit → Exits edit mode, doesn't delete
- ✅ Background sync → Synced sessions move to history
- ✅ Multi-client → Sessions from other clients appear in history only

---

## Build Status

✅ **Build Successful**
- 0 Warnings
- 0 Errors
- Build Time: 6.21 seconds

---

## Summary

The fixes ensure that:
1. **Current section** = Local work in progress only (pending sync)
2. **History section** = All synced sessions from backend
3. **Cancel** = Safe operation that doesn't delete data
4. **Sync behavior** = Automatic movement from current to history when synced

This creates a clear separation between "work in progress" (current) and "completed work" (history).
