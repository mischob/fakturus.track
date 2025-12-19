using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.DTOs;
using Ical.Net;

namespace Fakturus.Track.Backend.Services;

public class CalendarService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ApplicationDbContext context,
    ILogger<CalendarService> logger)
    : ICalendarService
{
    public async Task<List<CalendarEventDto>> GetCalendarEventsAsync(string userId)
    {
        try
        {
            // Get calendar URL - first check user table, then fall back to appsettings
            var calendarUrl = await GetCalendarUrlForUserAsync(userId);
            
            if (string.IsNullOrEmpty(calendarUrl))
            {
                logger.LogWarning("No calendar URL configured for user {UserId}", userId);
                return new List<CalendarEventDto>();
            }

            // Convert webcal:// to https://
            if (calendarUrl.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase))
            {
                calendarUrl = "https://" + calendarUrl.Substring(9);
            }

            // Fetch the iCal feed
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(calendarUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch calendar feed. Status: {StatusCode}", response.StatusCode);
                return new List<CalendarEventDto>();
            }

            var icalContent = await response.Content.ReadAsStringAsync();
            
            // Parse the iCal content with error handling for malformed data
            var events = new List<CalendarEventDto>();
            
            try
            {
                var calendar = Calendar.Load(icalContent);

                foreach (var calendarEvent in calendar.Events)
                {
                    try
                    {
                        // Only include future events and events from the last 30 days
                        if (calendarEvent.Start == null)
                        {
                            continue;
                        }

                        var eventStart = calendarEvent.Start.AsUtc;
                        if (eventStart < DateTime.UtcNow.AddDays(-30))
                        {
                            continue;
                        }

                        var eventEnd = calendarEvent.End?.AsUtc ?? eventStart.AddHours(1);

                        events.Add(new CalendarEventDto
                        {
                            Uid = calendarEvent.Uid ?? Guid.NewGuid().ToString(),
                            Summary = calendarEvent.Summary ?? "Untitled Event",
                            StartTime = eventStart,
                            EndTime = eventEnd,
                            Description = calendarEvent.Description,
                            Location = calendarEvent.Location
                        });
                    }
                    catch (Exception eventEx)
                    {
                        // Log but continue processing other events
                        logger.LogWarning(eventEx, "Failed to parse calendar event, skipping");
                    }
                }
            }
            catch (Exception parseEx)
            {
                logger.LogWarning(parseEx, "Standard parsing failed, attempting with cleaned content");
                
                try
                {
                    // Try to clean up common iCal formatting issues
                    var cleanedContent = CleanICalContent(icalContent);
                    var calendar = Calendar.Load(cleanedContent);

                    foreach (var calendarEvent in calendar.Events)
                    {
                        try
                        {
                            if (calendarEvent.Start == null)
                            {
                                continue;
                            }

                            var eventStart = calendarEvent.Start.AsUtc;
                            if (eventStart < DateTime.UtcNow.AddDays(-30))
                            {
                                continue;
                            }

                            var eventEnd = calendarEvent.End?.AsUtc ?? eventStart.AddHours(1);

                            events.Add(new CalendarEventDto
                            {
                                Uid = calendarEvent.Uid ?? Guid.NewGuid().ToString(),
                                Summary = calendarEvent.Summary ?? "Untitled Event",
                                StartTime = eventStart,
                                EndTime = eventEnd,
                                Description = calendarEvent.Description,
                                Location = calendarEvent.Location
                            });
                        }
                        catch (Exception eventEx)
                        {
                            logger.LogWarning(eventEx, "Failed to parse calendar event in cleaned content, skipping");
                        }
                    }
                }
                catch (Exception cleanEx)
                {
                    logger.LogWarning(cleanEx, "Cleaned parsing also failed, attempting manual parsing");
                    
                    // If cleaned parsing also fails, try to extract events manually
                    events = ParseICalManually(icalContent);
                    logger.LogInformation("Manual parsing extracted {Count} events", events.Count);
                }
            }

            // Sort by start time descending (most recent first)
            return events.OrderByDescending(e => e.StartTime).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching calendar events for user {UserId}", userId);
            return new List<CalendarEventDto>();
        }
    }

    private async Task<string?> GetCalendarUrlForUserAsync(string userId)
    {
        // First, check if user exists in database and has a calendar URL
        var user = await context.Users.FindAsync(userId);
        if (user?.CalendarUrl != null)
        {
            return user.CalendarUrl;
        }

        // Fall back to appsettings configuration
        var enabledUserId = configuration["Calendar:EnabledUserId"];
        if (enabledUserId == userId)
        {
            return configuration["Calendar:PublicCalendarUrl"];
        }

        return null;
    }

    private string CleanICalContent(string icalContent)
    {
        // Remove or fix common malformed patterns
        var lines = icalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleanedLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Handle END:VEVENT that has been corrupted with attendee data
            if (line.StartsWith("END:VEVENT") && line.Length > 10)
            {
                logger.LogDebug("Cleaning corrupted END:VEVENT line: {Line}", line);
                cleanedLines.Add("END:VEVENT");
                continue;
            }
            
            // Handle BEGIN/END statements that might be corrupted
            if (line.StartsWith("BEGIN:") || line.StartsWith("END:"))
            {
                // Extract just the BEGIN/END statement, ignore any trailing garbage
                var parts = line.Split(new[] { ';', ':', '/' }, 2);
                if (parts.Length > 0 && (parts[0].StartsWith("BEGIN:") || parts[0].StartsWith("END:")))
                {
                    cleanedLines.Add(parts[0]);
                    if (line != parts[0])
                    {
                        logger.LogDebug("Cleaned line from '{Original}' to '{Cleaned}'", line, parts[0]);
                    }
                    continue;
                }
            }
            
            // Skip lines that are clearly malformed (missing colon, invalid format)
            if (!string.IsNullOrWhiteSpace(line) && 
                !line.StartsWith("BEGIN:") && 
                !line.StartsWith("END:") && 
                !line.Contains(':'))
            {
                logger.LogDebug("Skipping malformed line: {Line}", line);
                continue;
            }

            cleanedLines.Add(line);
        }

        return string.Join("\r\n", cleanedLines);
    }

    private List<CalendarEventDto> ParseICalManually(string icalContent)
    {
        var events = new List<CalendarEventDto>();
        
        try
        {
            var lines = icalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            CalendarEventDto? currentEvent = null;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("BEGIN:VEVENT"))
                {
                    currentEvent = new CalendarEventDto();
                }
                else if (line.StartsWith("END:VEVENT") && currentEvent != null)
                {
                    // Only add events with valid start time
                    if (currentEvent.StartTime != default && 
                        currentEvent.StartTime >= DateTime.UtcNow.AddDays(-30))
                    {
                        events.Add(currentEvent);
                    }
                    currentEvent = null;
                }
                else if (currentEvent != null && line.Contains(':'))
                {
                    var colonIndex = line.IndexOf(':');
                    var key = line.Substring(0, colonIndex);
                    var value = line.Substring(colonIndex + 1);

                    try
                    {
                        if (key.StartsWith("DTSTART"))
                        {
                            currentEvent.StartTime = ParseICalDateTime(value);
                        }
                        else if (key.StartsWith("DTEND"))
                        {
                            currentEvent.EndTime = ParseICalDateTime(value);
                        }
                        else if (key == "SUMMARY")
                        {
                            currentEvent.Summary = value;
                        }
                        else if (key == "DESCRIPTION")
                        {
                            currentEvent.Description = value;
                        }
                        else if (key == "UID")
                        {
                            currentEvent.Uid = value;
                        }
                        else if (key == "LOCATION")
                        {
                            currentEvent.Location = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Failed to parse iCal property {Key}", key);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Manual iCal parsing failed");
        }

        return events;
    }

    private DateTime ParseICalDateTime(string value)
    {
        // Remove timezone info if present (e.g., "TZID=Europe/Berlin:")
        if (value.Contains(':'))
        {
            value = value.Substring(value.LastIndexOf(':') + 1);
        }

        // iCal format: 20231225T120000Z or 20231225T120000
        if (value.EndsWith("Z"))
        {
            value = value.TrimEnd('Z');
        }

        if (DateTime.TryParseExact(value, "yyyyMMddTHHmmss", 
            System.Globalization.CultureInfo.InvariantCulture, 
            System.Globalization.DateTimeStyles.AssumeUniversal, 
            out var result))
        {
            return result.ToUniversalTime();
        }

        return default;
    }
}
