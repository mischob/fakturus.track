# Calendar Import Feature - Configuration Guide

## Prerequisites

1. An iCloud calendar with a public sharing link (webcal URL)
2. Azure AD B2C user ID for the user who should have access to the calendar import feature

## Step 1: Get Your iCloud Calendar URL

### For iCloud Calendar:

1. Open iCloud Calendar in a web browser (https://www.icloud.com/calendar)
2. Select the calendar you want to share
3. Click the share icon next to the calendar name
4. Enable "Public Calendar"
5. Copy the webcal URL (it will look like: `webcal://p171-caldav.icloud.com/published/2/...`)

### For Google Calendar:

1. Open Google Calendar settings
2. Select the calendar you want to share
3. Scroll to "Integrate calendar"
4. Copy the "Public address in iCal format" URL
5. Replace `https://` with `webcal://` in the URL

## Step 2: Get Your Azure AD B2C User ID

Your user ID is the unique identifier from Azure AD B2C. You can find it in several ways:

### Option 1: From the Browser Console (Easiest)

1. Log in to the Fakturus Track application
2. Open browser developer tools (F12)
3. Go to the Console tab
4. Type: `localStorage.getItem('msal.account.keys')`
5. Copy the value (it's your user ID)

### Option 2: From Azure Portal

1. Go to Azure Portal (https://portal.azure.com)
2. Navigate to Azure AD B2C
3. Go to "Users"
4. Find your user
5. Copy the "Object ID"

### Option 3: From JWT Token

1. Log in to the application
2. Open browser developer tools (F12)
3. Go to Network tab
4. Make any API request
5. Look at the Authorization header
6. Copy the JWT token
7. Decode it at https://jwt.io
8. Look for the `oid` or `sub` claim - that's your user ID

## Step 3: Configure the Backend

### Development Environment

Edit `appsettings.Development.json`:

```json
{
  "Calendar": {
    "EnabledUserId": "your-user-id-from-step-2",
    "PublicCalendarUrl": "webcal://your-calendar-url-from-step-1"
  }
}
```

### Production Environment (Azure Key Vault)

Add the following secrets to your Azure Key Vault:

1. `Calendar--EnabledUserId` = your user ID
2. `Calendar--PublicCalendarUrl` = your webcal URL

Note: Use double dashes (`--`) in Key Vault secret names to represent nested configuration sections.

## Step 4: Restart the Application

1. Stop the backend if it's running
2. Start the backend again
3. **The database migration will be applied automatically** at startup
4. Check the logs to confirm migrations were applied successfully
5. The calendar import feature should now be available

The backend will automatically:
- Detect pending migrations
- Apply them to the database
- Log the migration status
- Fail to start if migrations cannot be applied (ensuring database schema is correct)

## Step 5: Test the Feature

1. Log in to the application with the configured user
2. Click "Import from Calendar" button on the Time Tracker page
3. You should see a list of calendar events
4. Select events to import
5. Click "Import Selected"
6. The events should be converted to work sessions

## Troubleshooting

### "Calendar access is not enabled for your account"

- Check that your user ID in appsettings matches your actual Azure AD B2C user ID
- Verify the configuration is loaded correctly (check logs)

### "Failed to load calendar events"

- Verify the webcal URL is correct and publicly accessible
- Try accessing the URL in a browser (replace `webcal://` with `https://`)
- Check backend logs for detailed error messages

### No events showing up

- Check that your calendar has events in the last 30 days or future
- Verify the calendar is set to "Public" in iCloud/Google Calendar settings
- Check that the iCal feed is not empty (access the URL in a browser)

### Events are duplicated

- The duplicate prevention should handle this automatically (±5 minutes tolerance)
- If duplicates still appear, check the backend logs for errors
- Verify the `FindDuplicateWorkSessionAsync` method is working correctly

## Security Considerations

### Current Implementation (Single User)

- Only one user can access the calendar import feature
- The calendar URL is stored in configuration (not in the database)
- This is suitable for personal use or small teams

### Future Enhancement (Multi-User)

To enable calendar import for multiple users:

1. Create a user management UI
2. Allow users to configure their own calendar URLs
3. Store calendar URLs in the `Users` table
4. Remove the `EnabledUserId` check from the endpoint
5. Update the `CalendarService` to use the user's stored calendar URL

## Example Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=fakturus_track;Username=postgres;Password=yourpassword"
  },
  "AzureAdB2C": {
    "Instance": "https://fakturus.b2clogin.com/",
    "Domain": "fakturus.onmicrosoft.com",
    "TenantId": "17c44991-367b-4d16-b818-1c268d2faed5",
    "ClientId": "74fd0ed2-8865-4bad-b002-7d867ad8791a",
    "Audience": "74fd0ed2-8865-4bad-b002-7d867ad8791a",
    "SignUpSignInPolicyId": "B2C_1_fakt_sign_in"
  },
  "Calendar": {
    "EnabledUserId": "12345678-1234-1234-1234-123456789abc",
    "PublicCalendarUrl": "webcal://p171-caldav.icloud.com/published/2/MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI"
  }
}
```

## Support

If you encounter any issues:

1. Check the backend logs for detailed error messages
2. Verify all configuration values are correct
3. Test the calendar URL directly in a browser
4. Ensure the database migration was applied successfully
5. Check that the user is authenticated and has the correct user ID
