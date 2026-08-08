using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CalendarHandbook;

public enum CalendarSeason
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public static class SeasonVisuals
{
    public static readonly double[] SpringColor = { 0.43, 0.74, 0.31, 1.0 };
    public static readonly double[] SummerColor = { 0.95, 0.73, 0.14, 1.0 };
    public static readonly double[] AutumnColor = { 0.90, 0.54, 0.17, 1.0 };
    public static readonly double[] WinterColor = { 0.70, 0.82, 0.92, 1.0 };

    public static CalendarSeason GetSeasonForMonth(int month, EnumHemisphere hemisphere)
    {
        CalendarSeason northern = month switch
        {
            12 or 1 or 2 => CalendarSeason.Winter,
            3 or 4 or 5 => CalendarSeason.Spring,
            6 or 7 or 8 => CalendarSeason.Summer,
            _ => CalendarSeason.Autumn
        };

        if (hemisphere != EnumHemisphere.South)
        {
            return northern;
        }

        return northern switch
        {
            CalendarSeason.Spring => CalendarSeason.Autumn,
            CalendarSeason.Summer => CalendarSeason.Winter,
            CalendarSeason.Autumn => CalendarSeason.Spring,
            _ => CalendarSeason.Summer
        };
    }

    public static CalendarSeason[] GetSeasonOrder(EnumHemisphere hemisphere)
    {
        return hemisphere == EnumHemisphere.South
            ? new[] { CalendarSeason.Summer, CalendarSeason.Autumn, CalendarSeason.Winter, CalendarSeason.Spring }
            : new[] { CalendarSeason.Winter, CalendarSeason.Spring, CalendarSeason.Summer, CalendarSeason.Autumn };
    }

    public static CalendarSeason[] GetSeasonOrderFromMonth(int startMonth, EnumHemisphere hemisphere)
    {
        CalendarSeason[] order = GetSeasonOrder(hemisphere);
        CalendarSeason startSeason = GetSeasonForMonth(startMonth, hemisphere);
        int offset = 0;
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == startSeason)
            {
                offset = i;
                break;
            }
        }

        CalendarSeason[] rotated = new CalendarSeason[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            rotated[i] = order[(i + offset) % order.Length];
        }

        return rotated;
    }

    public static double[] GetColor(CalendarSeason season)
    {
        return season switch
        {
            CalendarSeason.Spring => SpringColor,
            CalendarSeason.Summer => SummerColor,
            CalendarSeason.Autumn => AutumnColor,
            _ => WinterColor
        };
    }

    public static string GetMarker(CalendarSeason season)
    {
        return season switch
        {
            CalendarSeason.Spring => "+",
            CalendarSeason.Summer => "*",
            CalendarSeason.Autumn => "~",
            _ => "o"
        };
    }

    public static string GetLangKey(CalendarSeason season)
    {
        return season switch
        {
            CalendarSeason.Spring => "calendarhandbook:season-spring",
            CalendarSeason.Summer => "calendarhandbook:season-summer",
            CalendarSeason.Autumn => "calendarhandbook:season-autumn",
            _ => "calendarhandbook:season-winter"
        };
    }

    public static string GetLocalizedName(CalendarSeason season)
    {
        return Lang.Get(GetLangKey(season));
    }

    public static string GetLabeledName(CalendarSeason season)
    {
        return GetMarker(season) + " " + GetLocalizedName(season);
    }
}
