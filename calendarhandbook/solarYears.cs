using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CalendarHandbook;

public sealed class SolarYearsSolver : ICalendarSolver
{
    public const string ConfigKey = "SolarYearsConfiguration";

    public static bool SolarYearsInstalled(ICoreAPI api)
    {
        return api.ModLoader.IsModEnabled("solaryears");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string[] monthNames;
    private readonly int[] baseLengths;
    private readonly bool[] leapPeriod;
    private readonly int leapMonthIndex;
    private readonly int extraLeapDays;

    private SolarYearsSolver(
        string[] monthNames,
        int[] baseLengths,
        bool[] leapPeriod,
        int leapMonthIndex,
        int extraLeapDays
    )
    {
        this.monthNames = monthNames;
        this.baseLengths = baseLengths;
        this.leapPeriod = leapPeriod;
        this.leapMonthIndex = leapMonthIndex;
        this.extraLeapDays = extraLeapDays;
    }

    public int MonthCount => baseLengths.Length;

    public static SolarYearsSolver? Create(ICoreClientAPI capi)
    {
        try
        {
            string? encoded = capi.World?.Config?.GetString(ConfigKey);
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return null;
            }

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            return CreateFromJson(json);
        }
        catch (Exception e)
        {
            capi.Logger.Warning("[calendarhandbook] Could not read Solar Years calendar config: {0}", e.Message);
            return null;
        }
    }

    public static SolarYearsSolver? CreateFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        SolarYearsConfigDto? config;
        try
        {
            config = JsonSerializer.Deserialize<SolarYearsConfigDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (config?.Months == null)
        {
            return null;
        }

        List<SolarMonthDto> months = TakeRealMonths(config.Months);
        if (months.Count != 12)
        {
            return null;
        }

        var names = new string[12];
        var lengths = new int[12];
        for (int i = 0; i < 12; i++)
        {
            SolarMonthDto month = months[i];
            if (month == null || string.IsNullOrWhiteSpace(month.Name) || month.Length < 1)
            {
                return null;
            }

            names[i] = month.Name.Trim();
            lengths[i] = month.Length;
        }

        bool[] period = TakeLeapPeriod(config.LeapSystem?.Period);
        int extraDays = Math.Max(0, config.LeapSystem?.DaysToAdd ?? 0);
        int leapIndex = FindLeapMonthIndex(names, config.LeapSystem?.MonthToAdd);
        if (extraDays == 0 || leapIndex < 0)
        {
            extraDays = 0;
            leapIndex = -1;
        }

        return new SolarYearsSolver(names, lengths, period, leapIndex, extraDays);
    }

    public int GetDaysInMonth(int month, int year)
    {
        int index = Math.Clamp(month, 1, MonthCount) - 1;
        int days = baseLengths[index];
        if (IsLeapYear(year) && index == leapMonthIndex)
        {
            days += extraLeapDays;
        }

        return days;
    }

    public int GetStartDayOfYear(int month, int year)
    {
        int clamped = Math.Clamp(month, 1, MonthCount);
        int start = 0;
        for (int m = 1; m < clamped; m++)
        {
            start += GetDaysInMonth(m, year);
        }

        return start;
    }

    public string? GetMonthDisplayName(int month)
    {
        int index = Math.Clamp(month, 1, MonthCount) - 1;
        return monthNames[index];
    }

    private bool IsLeapYear(int year)
    {
        if (leapPeriod.Length == 0 || extraLeapDays <= 0 || leapMonthIndex < 0)
        {
            return false;
        }

        int index = year % leapPeriod.Length;
        if (index < 0)
        {
            index += leapPeriod.Length;
        }

        return leapPeriod[index];
    }

    private static List<SolarMonthDto> TakeRealMonths(List<SolarMonthDto> months)
    {
        if (months.Count > 12)
        {
            return months.GetRange(months.Count - 12, 12);
        }

        return months;
    }

    private static bool[] TakeLeapPeriod(List<bool>? period)
    {
        if (period == null || period.Count == 0)
        {
            return Array.Empty<bool>();
        }

        if (period.Count > 4)
        {
            return period.GetRange(period.Count - 4, 4).ToArray();
        }

        return period.ToArray();
    }

    private static int FindLeapMonthIndex(string[] names, string? monthToAdd)
    {
        if (string.IsNullOrWhiteSpace(monthToAdd))
        {
            return -1;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(names[i], monthToAdd.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}

internal sealed class SolarYearsConfigDto
{
    [JsonPropertyName("Months")]
    public List<SolarMonthDto>? Months { get; set; }

    [JsonPropertyName("LeapSystem")]
    public SolarLeapSystemDto? LeapSystem { get; set; }
}

internal sealed class SolarMonthDto
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Length")]
    public int Length { get; set; }
}

internal sealed class SolarLeapSystemDto
{
    [JsonPropertyName("Period")]
    public List<bool>? Period { get; set; }

    [JsonPropertyName("MonthToAdd")]
    public string? MonthToAdd { get; set; }

    [JsonPropertyName("DaysToAdd")]
    public int DaysToAdd { get; set; }
}
