using Fakturus.Track.Backend.Data;
using Fakturus.Track.Backend.DTOs;
using Ical.Net;
using System.Text.RegularExpressions;

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
            icalContent = SanitizeIcs(icalContent);
            icalContent = RemoveBrokenLines(icalContent);

            // Parse the iCal content with error handling for malformed data
            var events = new List<CalendarEventDto>();
            
            try
            {
                var calendar = Calendar.Load(icalContent);

                foreach (var calendarEvent in calendar.Events)
                {
                    try
                    {
                        // Only include future events and events from the last 600 days
                        if (calendarEvent.Start == null)
                        {
                            continue;
                        }

                        var eventStart = calendarEvent.Start.AsUtc;
                        if (eventStart < DateTime.UtcNow.AddDays(-600))
                        {
                            continue;
                        }

                        if (eventStart > DateTime.UtcNow.AddDays(2))
                        {
                            continue;
                        }

                        if (calendarEvent.Summary != null && (!calendarEvent.Summary.ToLowerInvariant().Contains("arbeit") && !calendarEvent.Summary.ToLowerInvariant().Contains("kirsten")))
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

    static string RemoveBrokenLines(string ics)
    {
        // Normalisiere Zeilenenden
        ics = ics.Replace("\r\n", "\n").Replace("\r", "\n");

        var lines = ics.Split('\n');
        var kept = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            var s = line.TrimEnd();

            // drop the known broken line pattern
            if (s.StartsWith("SCHÖNAICH:", StringComparison.OrdinalIgnoreCase) ||
                (s.Contains(":geo:", StringComparison.OrdinalIgnoreCase) && !s.StartsWith("GEO:", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            kept.Add(s);
        }

        return string.Join("\r\n", kept);
    }

    static string SanitizeIcs(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        // 1) Normalisiere Newlines
        raw = raw.Replace("\r\n", "\n").Replace("\r", "\n");

        // 2) RFC5545 Unfold: Zeilen, die mit SPACE oder TAB beginnen, an vorherige Zeile anhängen
        var lines = raw.Split('\n');
        var unfolded = new List<string>(lines.Length);
        foreach (var l in lines)
        {
            if ((l.StartsWith(" ") || l.StartsWith("\t")) && unfolded.Count > 0)
                unfolded[unfolded.Count - 1] += l.TrimStart(' ', '\t');
            else
                unfolded.Add(l);
        }

        // 3) END:VEVENT/END:VCALENDAR/END:... müssen "clean" sein (manche Feeds hängen Mist an)
        for (int i = 0; i < unfolded.Count; i++)
        {
            var s = unfolded[i].TrimEnd();
            if (s.StartsWith("END:VEVENT", StringComparison.OrdinalIgnoreCase)) unfolded[i] = "END:VEVENT";
            if (s.StartsWith("END:VCALENDAR", StringComparison.OrdinalIgnoreCase)) unfolded[i] = "END:VCALENDAR";
            if (s.StartsWith("END:VTODO", StringComparison.OrdinalIgnoreCase)) unfolded[i] = "END:VTODO";
        }

        return string.Join("\r\n", unfolded);
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
            // Example: "END:VEVENT;PARTSTAT=ACCEPTED;..." should become just "END:VEVENT"
            if (line.StartsWith("END:VEVENT"))
            {
                if (line.Length > 10)
                {
                    logger.LogDebug("Cleaning corrupted END:VEVENT line: {Line}", line.Substring(0, Math.Min(50, line.Length)));
                }
                cleanedLines.Add("END:VEVENT");
                continue;
            }
            
            // Handle BEGIN:VEVENT that might be corrupted
            if (line.StartsWith("BEGIN:VEVENT"))
            {
                if (line.Length > 12)
                {
                    logger.LogDebug("Cleaning corrupted BEGIN:VEVENT line: {Line}", line.Substring(0, Math.Min(50, line.Length)));
                }
                cleanedLines.Add("BEGIN:VEVENT");
                continue;
            }
            
            // Handle other BEGIN/END statements
            if (line.StartsWith("BEGIN:") || line.StartsWith("END:"))
            {
                // Extract just the BEGIN/END statement up to any semicolon or slash
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var afterColon = line.Substring(colonIndex + 1);
                    // Find the first semicolon or slash after the colon
                    var endIndex = afterColon.IndexOfAny(new[] { ';', '/' });
                    if (endIndex > 0)
                    {
                        var cleaned = line.Substring(0, colonIndex + 1 + endIndex);
                        logger.LogDebug("Cleaned line from '{Original}' to '{Cleaned}'", 
                            line.Substring(0, Math.Min(50, line.Length)), cleaned);
                        cleanedLines.Add(cleaned);
                        continue;
                    }
                }
                cleanedLines.Add(line);
                continue;
            }
            
            // Skip lines that are clearly malformed (missing colon, invalid format)
            // But allow empty lines and lines starting with space (continuation lines)
            if (!string.IsNullOrWhiteSpace(line) && 
                !line.StartsWith(" ") &&
                !line.StartsWith("\t") &&
                !line.Contains(':'))
            {
                logger.LogDebug("Skipping malformed line: {Line}", line.Substring(0, Math.Min(50, line.Length)));
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
