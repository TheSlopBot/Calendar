using System;
using System.Collections;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace CalendarHandbook;

public sealed class YearCalendarModel
{
    /// <summary>May / Second Seed: new worlds spawn here, so the month grids start on this month.</summary>
    public const int DisplayStartMonth = 5;

    private readonly ICoreClientAPI capi;

    public int DaysPerMonth { get; }
    public int DaysPerYear { get; }
    public int DayOfYear { get; }
    public int Month { get; }
    public int DayOfMonth { get; }
    public int Year { get; }
    public EnumMonth MonthName { get; }
    public EnumHemisphere Hemisphere { get; }
    public EnumSeason CurrentSeason { get; }
    public string LocalizedCurrentMonth { get; }
    public float HourOfDay { get; }
    public float HoursPerDay { get; }
    public string TimeOfDay { get; }
    public string DateHeader { get; }

    public YearCalendarModel(ICoreClientAPI capi)
    {
        this.capi = capi;
        IGameCalendar calendar = capi.World.Calendar;
        DaysPerMonth = Math.Max(1, calendar.DaysPerMonth);
        DaysPerYear = Math.Max(DaysPerMonth * 12, calendar.DaysPerYear);
        DayOfYear = Math.Clamp(calendar.DayOfYear, 0, Math.Max(0, DaysPerYear - 1));
        Month = Math.Clamp((DayOfYear / DaysPerMonth) + 1, 1, 12);
        MonthName = (EnumMonth)Month;
        Year = calendar.Year;
        DayOfMonth = (DayOfYear % DaysPerMonth) + 1;

        BlockPos? pos = capi.World.Player?.Entity?.Pos?.AsBlockPos;
        if (pos == null)
        {
            Hemisphere = EnumHemisphere.North;
            CurrentSeason = EnumSeason.Spring;
        }
        else
        {
            Hemisphere = calendar.GetHemisphere(pos);
            CurrentSeason = calendar.GetSeason(pos);
        }

        HoursPerDay = Math.Max(1f, calendar.HoursPerDay);
        HourOfDay = calendar.HourOfDay;
        TimeOfDay = FormatTimeOfDay(HourOfDay, HoursPerDay);

        LocalizedCurrentMonth = GetLocalizedMonth(MonthName);
        DateHeader = Lang.Get("calendarhandbook:current-date", DayOfMonth, LocalizedCurrentMonth, Year);
    }

    public static string FormatTimeOfDay(double hourOfDay, double hoursPerDay)
    {
        int minutesPerDay = Math.Max(1, (int)Math.Round(hoursPerDay)) * 60;
        int totalMinutes = (int)Math.Floor(hourOfDay * 60.0);
        totalMinutes = ((totalMinutes % minutesPerDay) + minutesPerDay) % minutesPerDay;
        return string.Format("{0:D2}:{1:D2}", totalMinutes / 60, totalMinutes % 60);
    }

    public static string GetLocalizedMonth(EnumMonth monthName)
    {
        return Lang.Get("month-" + monthName);
    }

    public static string GetLocalizedMonth(int month)
    {
        EnumMonth monthName = (EnumMonth)Math.Clamp(month, 1, 12);
        return GetLocalizedMonth(monthName);
    }

    public CalendarSeason GetSeasonForMonth(int month)
    {
        return SeasonVisuals.GetSeasonForMonth(month, Hemisphere);
    }

    /// <summary>March in the north, September in the south — first month of spring.</summary>
    public int SeasonalYearStartMonth => Hemisphere == EnumHemisphere.South ? 9 : 3;

    public CalendarSeason CurrentCalendarSeason => GetSeasonForMonth(Month);

    /// <summary>Winter normally; spring while it is already winter.</summary>
    public CalendarSeason CountdownTargetSeason =>
        CurrentCalendarSeason == CalendarSeason.Winter ? CalendarSeason.Spring : CalendarSeason.Winter;

    /// <summary>
    /// First month of the season in seasonal-year order (spring start), so winter is
    /// December in the north — not January, which is mid-winter on a Jan→Dec scan.
    /// </summary>
    public int GetFirstMonthOfSeason(CalendarSeason season)
    {
        for (int i = 0; i < 12; i++)
        {
            int month = ((SeasonalYearStartMonth - 1 + i) % 12) + 1;
            if (GetSeasonForMonth(month) == season)
            {
                return month;
            }
        }

        return SeasonalYearStartMonth;
    }

    public CalendarSeason[] GetDisplaySeasonOrder()
    {
        return SeasonVisuals.GetSeasonOrderFromMonth(SeasonalYearStartMonth, Hemisphere);
    }

    public int GetMonthAtDisplayIndex(int index)
    {
        return ((DisplayStartMonth - 1 + Math.Clamp(index, 0, 11)) % 12) + 1;
    }

    public int GetDisplayIndex(int month)
    {
        return (Math.Clamp(month, 1, 12) - DisplayStartMonth + 12) % 12;
    }

    public bool IsPastDay(int month, int day)
    {
        int displayIndex = GetDisplayIndex(month);
        int currentIndex = GetDisplayIndex(Month);
        if (displayIndex < currentIndex) return true;
        if (displayIndex > currentIndex) return false;
        return day < DayOfMonth;
    }

    public bool IsToday(int month, int day)
    {
        return month == Month && day == DayOfMonth;
    }

    public double YearProgress
    {
        get { return (double)ElapsedDays / DaysPerYear; }
    }

    public int ElapsedDays
    {
        get
        {
            int startDayIndex = (SeasonalYearStartMonth - 1) * DaysPerMonth;
            int wrapped = (DayOfYear - startDayIndex + DaysPerYear) % DaysPerYear;
            return wrapped + 1;
        }
    }

    public int TargetSeasonStartDayOfYear
    {
        get
        {
            int month = GetFirstMonthOfSeason(CountdownTargetSeason);
            return (month - 1) * DaysPerMonth;
        }
    }

    public int DaysUntilTargetSeason
    {
        get
        {
            int delta = (TargetSeasonStartDayOfYear - DayOfYear + DaysPerYear) % DaysPerYear;
            return delta;
        }
    }

    public double RemainingGameDaysUntilTargetSeason
    {
        get
        {
            IGameCalendar calendar = capi.World.Calendar;
            double dayOfYearf = calendar.DayOfYearf;
            double delta = TargetSeasonStartDayOfYear - dayOfYearf;
            if (delta <= 0)
            {
                delta += DaysPerYear;
            }

            return delta;
        }
    }

    public double RealSecondsUntilTargetSeason
    {
        get
        {
            IGameCalendar calendar = capi.World.Calendar;
            double hoursPerDay = Math.Max(0.0001, calendar.HoursPerDay);
            // SpeedOfTime is the sum of all modifiers. Sleep alone can add up to +17000, which
            // would make "Spring in" show ~15 minutes for a full season. ETA must use baseline play speed.
            double speed = Math.Max(0.0001, GetBaselineTimeSpeed(calendar) * calendar.CalendarSpeedMul);
            return RemainingGameDaysUntilTargetSeason * hoursPerDay * 3600.0 / speed;
        }
    }

    /// <summary>
    /// Normal calendar tempo only (default 60). Ignores temporary modifiers such as sleep acceleration.
    /// </summary>
    public static float GetBaselineTimeSpeed(IGameCalendar calendar)
    {
        const float fallback = 60f;
        try
        {
            PropertyInfo? prop = calendar.GetType().GetProperty("TimeSpeedModifiers");
            if (prop?.GetValue(calendar) is IDictionary modifiers
                && modifiers.Contains("baseline")
                && modifiers["baseline"] is IConvertible convertible)
            {
                float baseline = Convert.ToSingle(convertible);
                if (baseline > 0f)
                {
                    return baseline;
                }
            }
        }
        catch
        {
            // Concrete calendar type is not part of the public API; fall back to the game default.
        }

        return fallback;
    }

    public string FormatDaysUntilLabel()
    {
        CalendarSeason target = CountdownTargetSeason;
        string key = target == CalendarSeason.Spring
            ? "calendarhandbook:days-until-spring"
            : "calendarhandbook:days-until-winter";
        return Lang.Get(key, DaysUntilTargetSeason);
    }

    public string FormatCountdownLabel()
    {
        CalendarSeason target = CountdownTargetSeason;
        string key = target == CalendarSeason.Spring
            ? "calendarhandbook:countdown-spring"
            : "calendarhandbook:countdown-winter";
        return Lang.Get(key, FormatCountdown(RealSecondsUntilTargetSeason));
    }

    public static string FormatCountdown(double totalSeconds)
    {
        if (double.IsNaN(totalSeconds) || double.IsInfinity(totalSeconds) || totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        long seconds = (long)Math.Floor(totalSeconds);
        long hours = seconds / 3600;
        long minutes = (seconds % 3600) / 60;
        long secs = seconds % 60;
        return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, secs);
    }
}
