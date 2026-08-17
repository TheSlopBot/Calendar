using System;
using System.Reflection;
using Vintagestory.API.Common;

namespace CalendarHandbook;

/// <summary>
/// Describes the year picture: how long each month is. Does not replace "what day is it now."
/// </summary>
public interface ICalendarSolver
{
    int MonthCount { get; }

    int GetDaysInMonth(int month, int year);

    /// <summary>
    /// 0-based day of year of the 1st of <paramref name="month"/>.
    /// </summary>
    int GetStartDayOfYear(int month, int year);

    /// <summary>
    /// Config/calendar month title, or null to use vanilla <c>month-*</c> lang.
    /// </summary>
    string? GetMonthDisplayName(int month);
}

internal static class GameCalendarValues
{
    public static int ReadDayOfMonth(IGameCalendar calendar, int daysPerMonth)
    {
        int fallback = (Math.Max(0, calendar.DayOfYear) % daysPerMonth) + 1;
        try
        {
            PropertyInfo? prop = calendar.GetType().GetProperty(
                "DayOfMonth",
                BindingFlags.Instance | BindingFlags.Public
            );
            if (prop?.GetValue(calendar) is IConvertible convertible)
            {
                int day = Convert.ToInt32(convertible);
                if (day > 0)
                {
                    return day;
                }
            }
        }
        catch (Exception)
        {
        }

        return fallback;
    }
}
