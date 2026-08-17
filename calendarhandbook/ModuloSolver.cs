using System;
using Vintagestory.API.Common;

namespace CalendarHandbook;

public sealed class ModuloSolver : ICalendarSolver
{
    private readonly IGameCalendar calendar;
    private readonly int daysPerMonth;

    public ModuloSolver(IGameCalendar calendar)
    {
        this.calendar = calendar ?? throw new System.ArgumentNullException(nameof(calendar));
        daysPerMonth = Math.Max(1, calendar.DaysPerMonth);
    }

    public int MonthCount => 12;

    public int GetDaysInMonth(int month, int year)
    {
        _ = year;
        int currentMonth = Math.Clamp(calendar.Month, 1, 12);
        if (month != currentMonth)
        {
            return daysPerMonth;
        }

        int today = GameCalendarValues.ReadDayOfMonth(calendar, daysPerMonth);
        return Math.Max(daysPerMonth, today);
    }

    public int GetStartDayOfYear(int month, int year)
    {
        _ = year;
        int clamped = Math.Clamp(month, 1, MonthCount);
        return (clamped - 1) * daysPerMonth;
    }

    public string? GetMonthDisplayName(int month) => null;
}
