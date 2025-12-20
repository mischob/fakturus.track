using Nager.Date;

namespace Fakturus.Track.Backend.Services;

public class HolidayService : IHolidayService
{
    public List<DateOnly> GetHolidaysForYear(string bundesland, int year)
    {
        var countryCode = CountryCode.DE;
        var countyCode = MapBundeslandToCountyCode(bundesland);

        var holidays = DateSystem.GetPublicHolidays(year, countryCode)
            .Where(h => h.Counties == null || h.Counties.Contains(countyCode))
            .Select(h => DateOnly.FromDateTime(h.Date))
            .ToList();

        return holidays;
    }

    public bool IsHoliday(DateOnly date, string bundesland)
    {
        var holidays = GetHolidaysForYear(bundesland, date.Year);
        return holidays.Contains(date);
    }

    private string MapBundeslandToCountyCode(string bundesland)
    {
        return bundesland switch
        {
            "BW" => "DE-BW",
            "BY" => "DE-BY",
            "BE" => "DE-BE",
            "BB" => "DE-BB",
            "HB" => "DE-HB",
            "HH" => "DE-HH",
            "HE" => "DE-HE",
            "MV" => "DE-MV",
            "NI" => "DE-NI",
            "NW" => "DE-NW",
            "RP" => "DE-RP",
            "SL" => "DE-SL",
            "SN" => "DE-SN",
            "ST" => "DE-ST",
            "SH" => "DE-SH",
            "TH" => "DE-TH",
            _ => "DE-NW" // Default
        };
    }
}