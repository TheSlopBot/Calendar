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

    private static readonly SolarMonthDto[] ConstructorMonths =
    [
        new() { Name = "January", Length = 31 },
        new() { Name = "February", Length = 28 },
        new() { Name = "March", Length = 31 },
        new() { Name = "April", Length = 30 },
        new() { Name = "May", Length = 31 },
        new() { Name = "June", Length = 30 },
        new() { Name = "July", Length = 31 },
        new() { Name = "August", Length = 31 },
        new() { Name = "September", Length = 30 },
        new() { Name = "October", Length = 31 },
        new() { Name = "November", Length = 30 },
        new() { Name = "December", Length = 31 }
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string[] monthNames;
    private readonly int[] monthSizes;
    private readonly int[] monthStarts;

    private SolarYearsSolver(string[] monthNames, int[] monthSizes, int[] monthStarts)
    {
        this.monthNames = monthNames;
        this.monthSizes = monthSizes;
        this.monthStarts = monthStarts;
    }

    public int MonthCount => monthSizes.Length;

    public static bool SolarYearsInstalled(ICoreAPI api)
    {
        return api.ModLoader.IsModEnabled("solaryears");
    }

    public static SolarYearsSolver? Create(ICoreClientAPI capi)
    {
        try
        {
            string? encoded = capi.World?.Config?.GetString(ConfigKey);
            if (encoded == null)
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

        if (config == null)
        {
            return null;
        }

        List<SolarMonthDto> months = DropConstructorPrefix(ConstructorMonths, config.Months, 12);
        if (months.Count != 12)
        {
            return null;
        }

        var names = new string[12];
        var lengths = new int[12];
        for (int i = 0; i < 12; i++)
        {
            SolarMonthDto month = months[i];
            if (month?.Name == null)
            {
                return null;
            }

            names[i] = month.Name;
            lengths[i] = month.Length;
        }

        string monthToAdd = config.LeapSystem?.MonthToAdd ?? "February";
        int daysToAdd = config.LeapSystem?.DaysToAdd ?? 1;
        int leapMonth = FindLeapMonthIndex(names, monthToAdd);
        if (leapMonth < 0)
        {
            return null;
        }

        lengths[leapMonth] += daysToAdd;

        return new SolarYearsSolver(names, lengths, CumulativeSum(lengths));
    }

    public int GetDaysInMonth(int month, int year)
    {
        _ = year;
        int index = Math.Clamp(month, 1, MonthCount) - 1;
        return monthSizes[index];
    }

    public int GetStartDayOfYear(int month, int year)
    {
        _ = year;
        int index = Math.Clamp(month, 1, MonthCount) - 1;
        return monthStarts[index];
    }

    public string? GetMonthDisplayName(int month)
    {
        int index = Math.Clamp(month, 1, MonthCount) - 1;
        return monthNames[index];
    }

    private static List<T> DropConstructorPrefix<T>(IReadOnlyList<T> constructorItems, List<T>? jsonItems, int prefix)
    {
        var merged = new List<T>(constructorItems);
        if (jsonItems != null)
        {
            merged.AddRange(jsonItems);
        }

        if (merged.Count < prefix)
        {
            return new List<T>();
        }

        return merged.GetRange(prefix, merged.Count - prefix);
    }

    private static int[] CumulativeSum(int[] monthSizes)
    {
        var starts = new int[monthSizes.Length];
        int acc = 0;
        for (int i = 0; i < monthSizes.Length; i++)
        {
            starts[i] = acc;
            acc += monthSizes[i];
        }

        return starts;
    }

    private static int FindLeapMonthIndex(string[] names, string? monthToAdd)
    {
        if (monthToAdd == null)
        {
            return -1;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Equals(monthToAdd))
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
    public int? DaysToAdd { get; set; }
}
