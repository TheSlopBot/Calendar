using System;
using System.Text;
using CalendarHandbook;

namespace CalendarHandbook.Smoke;

internal static class Program
{
    private static int failures;

    public static int Main()
    {
        GregorianYearAndLeapDay();
        DeserializeQuirkKeepsTrailingMonths();
        LeapPeriodQuirkKeepsTrailingCycle();
        RejectsUnusableConfig();
        Base64RoundTripMatchesCreateFromJson();

        if (failures == 0)
        {
            Console.WriteLine("Smoke tests passed.");
            return 0;
        }

        Console.WriteLine($"Smoke tests failed: {failures}");
        return 1;
    }

    private static void GregorianYearAndLeapDay()
    {
        SolarYearsSolver? solver = SolarYearsSolver.CreateFromJson(GregorianJson(duplicate: false, extraLeapFalse: false));
        Expect(solver != null, "parse default Solar Years config");
        if (solver == null)
        {
            return;
        }

        ExpectEq(12, solver.MonthCount, "month count");
        ExpectEq(31, solver.GetDaysInMonth(1, 1), "January");
        ExpectEq(28, solver.GetDaysInMonth(2, 1), "February in a non-leap year");
        ExpectEq(29, solver.GetDaysInMonth(2, 0), "February in a leap year");
        ExpectEq(30, solver.GetDaysInMonth(4, 1), "April");
        ExpectEq(31, solver.GetDaysInMonth(12, 1), "December");
        ExpectEq(0, solver.GetStartDayOfYear(1, 1), "January starts at day 0");
        ExpectEq(31 + 28, solver.GetStartDayOfYear(3, 1), "March 1 in a non-leap year");
        ExpectEq(31 + 29, solver.GetStartDayOfYear(3, 0), "March 1 in a leap year");
        ExpectEq(365, DaysInYear(solver, 1), "365-day common year");
        ExpectEq(366, DaysInYear(solver, 0), "366-day leap year");
        ExpectEq("May", solver.GetMonthDisplayName(5), "May title from config");
    }

    private static void DeserializeQuirkKeepsTrailingMonths()
    {
        SolarYearsSolver? solver = SolarYearsSolver.CreateFromJson(GregorianJson(duplicate: true, extraLeapFalse: false));
        Expect(solver != null, "parse duplicated 24-month config");
        if (solver == null)
        {
            return;
        }

        ExpectEq(31, solver.GetDaysInMonth(1, 1), "trailing January after 24-month quirk");
        ExpectEq("January", solver.GetMonthDisplayName(1), "trailing January name");
    }

    private static void LeapPeriodQuirkKeepsTrailingCycle()
    {
        string json = GregorianJson(duplicate: false, extraLeapFalse: true);
        SolarYearsSolver? solver = SolarYearsSolver.CreateFromJson(json);
        Expect(solver != null, "parse 8-entry leap period");
        if (solver == null)
        {
            return;
        }

        ExpectEq(29, solver.GetDaysInMonth(2, 0), "year 0 still leap after period quirk");
        ExpectEq(28, solver.GetDaysInMonth(2, 1), "year 1 still common after period quirk");
    }

    private static void RejectsUnusableConfig()
    {
        Expect(SolarYearsSolver.CreateFromJson("") == null, "empty json is rejected");
        Expect(SolarYearsSolver.CreateFromJson("{not json") == null, "invalid json is rejected");
        Expect(SolarYearsSolver.CreateFromJson("{\"Months\":[]}") == null, "zero months is rejected");
        Expect(SolarYearsSolver.CreateFromJson("{\"Months\":" + ElevenMonthsJson() + "}") == null, "11 months is rejected");
    }

    private static void Base64RoundTripMatchesCreateFromJson()
    {
        string json = GregorianJson(duplicate: false, extraLeapFalse: false);
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        SolarYearsSolver? solver = SolarYearsSolver.CreateFromJson(decoded);
        Expect(solver != null, "base64 world-config payload round-trips");
        ExpectEq(31, solver!.GetDaysInMonth(1, 1), "January after base64 round-trip");
    }

    private static int DaysInYear(ICalendarSolver solver, int year)
    {
        int sum = 0;
        for (int month = 1; month <= solver.MonthCount; month++)
        {
            sum += solver.GetDaysInMonth(month, year);
        }

        return sum;
    }

    private static string GregorianJson(bool duplicate, bool extraLeapFalse)
    {
        string months = string.Join(",",
            Month("January", 31),
            Month("February", 28),
            Month("March", 31),
            Month("April", 30),
            Month("May", 31),
            Month("June", 30),
            Month("July", 31),
            Month("August", 31),
            Month("September", 30),
            Month("October", 31),
            Month("November", 30),
            Month("December", 31)
        );

        if (duplicate)
        {
            string dummy = string.Join(",",
                Month("Dummy1", 1),
                Month("Dummy2", 1),
                Month("Dummy3", 1),
                Month("Dummy4", 1),
                Month("Dummy5", 1),
                Month("Dummy6", 1),
                Month("Dummy7", 1),
                Month("Dummy8", 1),
                Month("Dummy9", 1),
                Month("Dummy10", 1),
                Month("Dummy11", 1),
                Month("Dummy12", 1)
            );
            months = dummy + "," + months;
        }

        string period = extraLeapFalse
            ? "false,false,false,false,true,false,false,false"
            : "true,false,false,false";

        return "{\"Months\":[" + months + "],\"LeapSystem\":{\"Period\":[" + period + "],\"MonthToAdd\":\"February\",\"DaysToAdd\":1}}";
    }

    private static string ElevenMonthsJson()
    {
        return "[" + string.Join(",",
            Month("January", 31),
            Month("February", 28),
            Month("March", 31),
            Month("April", 30),
            Month("May", 31),
            Month("June", 30),
            Month("July", 31),
            Month("August", 31),
            Month("September", 30),
            Month("October", 31),
            Month("November", 30)
        ) + "]";
    }

    private static string Month(string name, int length)
    {
        return "{\"Name\":\"" + name + "\",\"Length\":" + length + "}";
    }

    private static void Expect(bool condition, string name)
    {
        if (condition)
        {
            Console.WriteLine("ok   " + name);
            return;
        }

        failures++;
        Console.WriteLine("FAIL " + name);
    }

    private static void ExpectEq<T>(T expected, T actual, string name)
    {
        if (Equals(expected, actual))
        {
            Console.WriteLine("ok   " + name + " = " + actual);
            return;
        }

        failures++;
        Console.WriteLine("FAIL " + name + ": expected " + expected + ", got " + actual);
    }
}
