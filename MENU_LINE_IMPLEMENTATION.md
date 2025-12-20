# Menu Line & Features Implementation Summary

## Overview
Successfully implemented a bottom navigation menu with four sections (Zeiten, Urlaub, Gesamt, Settings) and added vacation tracking, overtime calculation, and user settings management.

## Implementation Date
December 20, 2025

## Backend Changes

### New Database Entities
1. **VacationDay** (`Data/Entities/VacationDay.cs`)
   - Properties: Id, UserId, Date, CreatedAt, UpdatedAt, SyncedAt
   - Unique constraint on UserId + Date

2. **User Entity Updates** (`Data/Entities/User.cs`)
   - Added: VacationDaysPerYear (default: 30)
   - Added: WorkHoursPerWeek (default: 40)

### Database Migration
- Created migration: `AddVacationDaysAndUserSettings`
- Updated ApplicationDbContext with VacationDay DbSet

### DTOs Created
1. `VacationDayDto.cs` - Vacation day data transfer
2. `UserSettingsDto.cs` - User settings data transfer
3. `OvertimeSummaryDto.cs` - Overtime calculation results

### Services Created
1. **VacationDayService** - CRUD operations for vacation days
2. **UserSettingsService** - Get/Update user settings
3. **OvertimeCalculationService** - Weekly overtime calculation logic

### Endpoints Created (7 total)
**VacationDays:**
1. `CreateVacationDayEndpoint` - POST /v1/vacation-days
2. `GetVacationDaysEndpoint` - GET /v1/vacation-days
3. `DeleteVacationDayEndpoint` - DELETE /v1/vacation-days/{id}
4. `SyncVacationDaysEndpoint` - POST /v1/vacation-days/sync

**Settings:**
5. `GetUserSettingsEndpoint` - GET /v1/settings
6. `UpdateUserSettingsEndpoint` - PUT /v1/settings
7. `GetOvertimeSummaryEndpoint` - GET /v1/overtime-summary

### Validators Created
1. `VacationDayValidator.cs` - Validation for vacation day requests
2. `UserSettingsValidator.cs` - Validation for settings updates

## Frontend Changes

### Models Created
1. `VacationDayModel.cs` - Frontend vacation day model
2. `UserSettingsModel.cs` - Frontend settings model
3. `OvertimeSummaryModel.cs` - Overtime summary with monthly breakdown

### API Clients (Refit)
1. `IVacationApiClient.cs` - Vacation endpoints interface
2. `ISettingsApiClient.cs` - Settings endpoints interface

### Services Created
1. **VacationSyncService** - Local storage + backend sync for vacation days
   - Similar pattern to WorkSession sync
   - Periodic sync every 30 seconds
   - Event-driven sync completion notification

### Navigation Components
1. `TrackBottomNavigation.razor` - Bottom navigation bar
2. `TrackNavItem.razor` - Individual navigation item with icons

### Page Components
1. **Vacation.razor** (`/urlaub`)
   - Calendar component for selecting vacation days
   - List view of selected days
   - Sync functionality
   - Delete vacation days

2. **Overview.razor** (`/gesamt`)
   - Total overtime display
   - Vacation days taken/remaining
   - Monthly overtime breakdown table
   - Year selector

3. **Settings.razor** (`/settings`)
   - Form to update vacation days per year
   - Form to update work hours per week
   - Save/Reset functionality

### Reusable Components
1. `VacationCalendar.razor` - Calendar picker for vacation days
2. `OvertimeCard.razor` - Display card for overtime statistics

### Layout Updates
- Updated `MainLayout.razor` to include bottom navigation
- Updated `TimeTracker.razor` to support `/zeiten` route

### Service Registration
- Registered all new Refit clients in `Program.cs`
- Registered VacationSyncService

## Features Implemented

### 1. Zeiten (Time Tracking)
- Existing time tracking functionality
- Now accessible via bottom navigation
- Routes: `/`, `/tracker`, `/zeiten`

### 2. Urlaub (Vacation)
- Calendar-based vacation day selection
- Individual day selection (not ranges)
- Local storage with backend sync
- Visual feedback for pending sync
- German day names and formatting

### 3. Gesamt (Overview)
- Total overtime calculation (weekly basis)
- Monthly overtime breakdown
- Vacation days taken vs. remaining
- Year selector for historical data
- Color-coded positive/negative overtime

### 4. Settings
- Configure vacation days per year
- Configure work hours per week
- Validation (1-365 days, 0-168 hours/week)
- Information about overtime calculation

## Technical Decisions

### Overtime Calculation
- **Basis**: Weekly (e.g., 40h/week standard)
- **Working Days**: Monday-Friday (weekends excluded)
- **Vacation Days**: Excluded from expected hours calculation
- **Formula**: Overtime = Worked Hours - Expected Hours

### Data Storage
- **Vacation Days**: Backend database with sync (survives device changes)
- **User Settings**: Backend in User entity (persists across devices)
- **Sync Pattern**: Similar to WorkSessions (local-first with periodic sync)

### UI/UX
- **Navigation**: Always visible bottom bar (mobile-first)
- **Calendar**: Individual day selection for flexibility
- **Colors**: 
  - Blue for time tracking
  - Green for vacation
  - Orange for overview
  - Gray for settings

## Files Created

### Backend (18 files)
- 1 Entity
- 3 DTOs
- 5 Services (3 new + 2 interfaces)
- 7 Endpoints
- 2 Validators
- 1 Migration

### Frontend (13 files)
- 3 Models
- 2 API Clients
- 2 Services
- 2 Navigation Components
- 3 Pages
- 2 Reusable Components

### Files Modified
- Backend: `User.cs`, `ApplicationDbContext.cs`, `Program.cs`
- Frontend: `MainLayout.razor`, `TimeTracker.razor`, `Program.cs`

## Testing Recommendations

1. **Backend Testing**
   - Test vacation day CRUD operations
   - Test sync endpoint with various scenarios
   - Test overtime calculation with different work patterns
   - Test settings validation

2. **Frontend Testing**
   - Test navigation between all four sections
   - Test calendar day selection/deselection
   - Test vacation day sync
   - Test overtime summary display
   - Test settings save/reset

3. **Integration Testing**
   - Test full vacation day lifecycle (create, sync, delete)
   - Test overtime calculation with real work sessions
   - Test settings changes affecting overtime calculation

## Known Considerations

1. **Overtime Calculation**: Assumes 5-day work week (Mon-Fri)
2. **Holidays**: Not yet implemented (only weekends and vacation days excluded)
3. **Partial Days**: Vacation days are full days only
4. **Time Zones**: All times stored in UTC

## Next Steps (Optional Enhancements)

1. Add public holidays support
2. Add half-day vacation option
3. Add vacation day approval workflow
4. Add export functionality for overtime reports
5. Add notifications for low vacation days
6. Add historical overtime trends chart

