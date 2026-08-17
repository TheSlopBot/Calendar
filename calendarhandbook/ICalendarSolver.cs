using System;
using System.Reflection;
using Vintagestory.API.Common;

namespace CalendarHandbook;

public interface ICalendarSolver
{
    int MonthCount { get; }

    int GetDaysInMonth(int month, int year);

    int GetStartDayOfYear(int month, int year);

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
