# Sync Implementation Testing Guide

This guide provides step-by-step instructions to test the new sync implementation according to the requirements in `base/concepts/sync/syncReq.md`.

## Prerequisites

1. Backend and Frontend applications running
2. At least one user account for testing
3. Access to browser developer console for logs
4. Optionally: Two browser windows/profiles for multi-client testing

## Test Scenarios

### ✅ Test 1: Client-Generated UUID Preservation (Requirement #1, #2)

**Objective**: Verify that UUIDs generated locally are preserved in the backend database.

**Steps**:
1. Open the Time Tracker page
2. Click "Start New Session"
3. Open browser console and note the UUID in the logs (e.g., `Session abc123...`)
4. Click "Start" to set start time
5. Wait for background sync (30 seconds) or click "Sync" button
6. Check backend database - the session should have the same UUID
7. Verify in console logs: "Synced pending sessions, received X sessions from backend"

**Expected Result**: 
- Local UUID matches backend UUID
- Session marked as synced (no "Pending Sync" badge)

---

### ✅ Test 2: Background Sync Starts Automatically (Requirement #3)

**Objective**: Verify that background sync starts when pending sessions exist.

**Steps**:
1. Open Time Tracker page
2. Create a new session (it becomes pending)
3. Check console logs for: "Starting background sync with 30s interval"
4. Wait 30 seconds
5. Check console logs for: "SyncService.SyncAsync: Found X pending sessions to sync"
6. Verify session is synced after 30 seconds

**Expected Result**:
- Background sync starts automatically
- Sync occurs every 30 seconds
- Pending session is synced to backend

---

### ✅ Test 3: Background Sync Stops When No Pending Sessions (Requirement #7)

**Objective**: Verify that background sync stops when all sessions are synced.

**Steps**:
1. Create a new session (background sync starts)
2. Wait for sync to complete (check "Pending Sync" badge disappears)
3. Check console logs for: "No pending sessions, stopping background sync"
4. Wait another 30 seconds
5. Verify no more sync attempts in console logs

**Expected Result**:
- Background sync stops after all sessions are synced
- No unnecessary sync calls when nothing is pending

---

### ✅ Test 4: New Backend Entries Appear Locally (Requirement #4)

**Objective**: Verify that sessions created on another client appear in the current client.

**Steps**:
1. Open Time Tracker in Browser A
2. Open Time Tracker in Browser B (same user, different browser/incognito)
3. In Browser B, create a new session and wait for sync
4. In Browser A, click "Sync" button
5. Verify the new session from Browser B appears in Browser A

**Expected Result**:
- Sessions from other clients appear after sync
- All sessions are visible across clients

---

### ✅ Test 5: Pending Sync Badge Display (Requirement #5)

**Objective**: Verify that unsynced sessions show "Pending Sync" badge.

**Steps**:
1. Create a new session
2. Immediately check for "Pending Sync" badge on the session card
3. Wait for sync (30s or manual)
4. Verify badge disappears after successful sync

**Expected Result**:
- "Pending Sync" badge visible for unsynced sessions
- Badge disappears after sync completes

---

### ✅ Test 6: Deleted Backend Sessions Removed Locally (Requirement #6)

**Objective**: Verify that sessions deleted on backend are removed from local UI.

**Steps**:
1. Create and sync a session in Browser A
2. Note the session ID
3. In Browser B (or via API), delete that session from backend
4. In Browser A, click "Sync" button
5. Verify the deleted session is removed from Browser A's UI

**Expected Result**:
- Sessions deleted on backend are removed from local storage
- UI updates to reflect deletion

---

### ✅ Test 7: Manual Sync Works Identically to Background Sync (Requirement #8)

**Objective**: Verify manual and background sync perform the same operations.

**Steps**:
1. Create a pending session
2. Click "Sync" button manually (don't wait for background sync)
3. Verify session is synced
4. Create another pending session
5. Wait for background sync (30s)
6. Verify session is synced identically

**Expected Result**:
- Manual sync and background sync produce identical results
- Both sync pending sessions and fetch backend changes

---

### ✅ Test 8: Network Error Handling

**Objective**: Verify sync handles network errors gracefully.

**Steps**:
1. Create a pending session
2. Stop the backend server or disconnect network
3. Wait for sync attempt (30s)
4. Check console for error: "Background sync error: ..."
5. Verify session remains pending (not lost)
6. Restart backend/reconnect network
7. Wait for next sync attempt
8. Verify session syncs successfully

**Expected Result**:
- Network errors don't crash the app
- Pending sessions are preserved for retry
- Sync succeeds on next attempt when network is available

---

### ✅ Test 9: Multiple Pending Sessions

**Objective**: Verify multiple pending sessions sync correctly.

**Steps**:
1. Create 3 new sessions with different times
2. Verify all show "Pending Sync" badge
3. Wait for background sync or click "Sync"
4. Verify all 3 sessions are synced
5. Verify all UUIDs are preserved
6. Check backend database for all 3 sessions

**Expected Result**:
- All pending sessions sync in one batch
- All UUIDs preserved
- All sessions marked as synced

---

### ✅ Test 10: Edit Synced Session

**Objective**: Verify editing a synced session marks it as pending again.

**Steps**:
1. Create and sync a session
2. Edit the session (change start/stop time)
3. Save changes
4. Verify "Pending Sync" badge appears
5. Wait for sync
6. Verify changes are reflected in backend

**Expected Result**:
- Edited sessions marked as pending
- Changes sync to backend
- UUID remains the same (upsert, not create)

---

## Console Log Checklist

When testing, look for these key console messages:

### Sync Start
```
SyncService.StartPeriodicSyncAsync: Starting background sync with 30s interval
```

### Sync Execution
```
SyncService.SyncAsync: Found X local sessions
SyncService.SyncAsync: Found X pending sessions to sync
SyncService.SyncAsync: Synced pending sessions, received X sessions from backend
SyncService.SyncAsync: Saving X merged sessions to local storage
```

### Sync Stop
```
SyncService.SyncAsync: No pending sessions, stopping background sync
SyncService.StopPeriodicSync: Stopping background sync
```

### Errors
```
Background sync error: [error message]
Sync error: [error message]
```

---

## Database Verification

To verify backend database state:

1. Connect to your database
2. Query the `WorkSessions` table
3. Check that:
   - UUIDs match client-generated UUIDs
   - `SyncedAt` timestamps are set
   - All expected sessions exist
   - Deleted sessions are removed

---

## Known Behaviors

1. **30-second interval**: Background sync runs every 30 seconds when pending sessions exist
2. **Auto-stop**: Background sync automatically stops when no pending sessions remain
3. **Auto-start**: Background sync starts when:
   - Page loads with pending sessions
   - New session is created
   - Session is edited
   - Manual sync completes with pending sessions still remaining

4. **Backend is source of truth**: After sync, backend sessions override local synced sessions
5. **Pending sessions preserved**: Local pending sessions are kept even if sync fails (for retry)

---

## Troubleshooting

### Background sync not starting
- Check console for "No pending syncs, not starting background sync"
- Verify sessions have `IsPendingSync = true` and `IsSynced = false`

### Sessions not syncing
- Check network connectivity
- Verify backend is running
- Check console for error messages
- Verify authentication token is valid

### UUIDs not preserved
- Check that `CreateWorkSessionRequest` includes `Id` property
- Verify backend uses `request.Id` instead of `Guid.NewGuid()`
- Check validator allows `Id` field

### Background sync not stopping
- Check if there are still pending sessions
- Verify sync completed successfully
- Check console logs for sync status

---

## Success Criteria

All tests pass when:
- ✅ Client UUIDs are preserved in backend
- ✅ Background sync starts/stops automatically
- ✅ Pending sessions sync within 30 seconds
- ✅ Sessions from other clients appear after sync
- ✅ Deleted backend sessions are removed locally
- ✅ "Pending Sync" badges display correctly
- ✅ Manual and background sync work identically
- ✅ Network errors don't break the app
- ✅ Multiple sessions sync correctly
- ✅ Edited sessions re-sync with same UUID
