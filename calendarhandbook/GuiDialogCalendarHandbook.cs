using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CalendarHandbook;

public sealed class GuiDialogCalendarHandbook : GuiDialog
{
    public const string HotkeyCode = "calendarhandbook";
    private const string ComposerName = "calendarhandbook";
    private const string ClockKey = "timeofday";
    private const string DaysUntilKey = "daysuntil";
    private const string CountdownKey = "seasoncountdown";

    /// <summary>Unscaled px between the date header and the clock, roughly two spaces at the header font size.</summary>
    private const int ClockGap = 14;

    private static readonly double[] PastTextColor = { 0.55, 0.50, 0.42, 0.9 };
    private static readonly double[] TodayTextColor = { 0.98, 0.86, 0.45, 1.0 };
    private static readonly double[] FutureTextColor = { 0.95, 0.92, 0.84, 1.0 };
    private static readonly double[] ClockTextColor = { 0.96, 0.89, 0.73, 1.0 };

    private YearCalendarModel? model;
    private double countdownSeconds;
    private float countdownRefreshTimer;
    private string lastClockText = "";
    private string lastDaysUntilText = "";
    private string lastCountdownText = "";

    public GuiDialogCalendarHandbook(ICoreClientAPI capi) : base(capi)
    {
    }

    public override string ToggleKeyCombinationCode => HotkeyCode;

    public override bool TryOpen()
    {
        if (capi.World?.Calendar == null)
        {
            return false;
        }

        return base.TryOpen();
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        ComposeDialog();
    }

    public override void OnRenderGUI(float deltaTime)
    {
        base.OnRenderGUI(deltaTime);

        if (!IsOpened() || SingleComposer == null || capi.World?.Calendar == null)
        {
            return;
        }

        countdownSeconds = Math.Max(0, countdownSeconds - deltaTime);
        countdownRefreshTimer += deltaTime;

        if (countdownRefreshTimer < 1f)
        {
            return;
        }

        countdownRefreshTimer = 0f;

        // While paused the calendar is frozen, so keep the last clock reading and only run the countdown down.
        if (!capi.IsGamePaused)
        {
            YearCalendarModel updated = new YearCalendarModel(capi);

            // A day rollover moves the highlighted cell and can resize the date header, so rebuild the whole layout.
            if (model == null || updated.DayOfYear != model.DayOfYear || updated.Year != model.Year)
            {
                ComposeDialog();
                return;
            }

            model = updated;
            countdownSeconds = model.RealSecondsUntilTargetSeason;
        }

        UpdateInfoTexts(force: true);
    }

    private void ComposeDialog()
    {
        model = new YearCalendarModel(capi);
        countdownSeconds = model.RealSecondsUntilTargetSeason;
        countdownRefreshTimer = 0f;
        lastClockText = "";
        lastDaysUntilText = "";
        lastCountdownText = "";

        const int left = 24;
        const int dateHeight = 28;
        const int barHeight = 20;
        const int seasonLabelHeight = 24;
        const int infoRowHeight = 22;
        const int afterDateGap = 8;
        const int afterBarGap = 8;
        const int afterSeasonGap = 6;
        const int dialogWidth = 900;
        const int monthWidth = 204;
        const int monthGapX = 10;
        const int monthGapY = 10;
        const int monthsPerRow = 4;
        const int monthRows = 3;
        const int dayCell = 24;
        const int dayGap = 2;
        const int monthTitleHeight = 22;

        int titleBarHeight = (int)GuiStyle.TitleBarHeight;
        int topInset = 46;
        int dateY = titleBarHeight + topInset;
        int barY = dateY + dateHeight + afterDateGap;
        int seasonY = barY + barHeight + afterBarGap;
        int infoY = seasonY + seasonLabelHeight + afterSeasonGap;
        int graphBottom = infoY + infoRowHeight;
        int gridsY = graphBottom + dateY;

        int dayRows = Math.Max(1, (int)Math.Ceiling(model.DaysPerMonth / 7.0));
        int monthInnerHeight = monthTitleHeight + 6 + dayRows * (dayCell + dayGap);
        int gridsWidth = monthsPerRow * monthWidth + (monthsPerRow - 1) * monthGapX;
        int gridsHeight = monthRows * monthInnerHeight + (monthRows - 1) * monthGapY;
        int dayTextPad = Math.Max(1, (dayCell - (int)GuiStyle.DetailFontSize) / 2);

        CairoFont titleFont = CairoFont.WhiteSmallishText();
        CairoFont clockFont = CairoFont.WhiteSmallishText().WithColor(ClockTextColor);
        CairoFont detailFont = CairoFont.WhiteDetailText().WithColor(FutureTextColor);
        CairoFont countdownFont = CairoFont.WhiteDetailText().WithColor(FutureTextColor).WithOrientation(EnumTextOrientation.Right);
        CairoFont pastDayFont = CairoFont.WhiteDetailText().WithColor(PastTextColor);
        CairoFont todayDayFont = CairoFont.WhiteDetailText().WithColor(TodayTextColor);
        CairoFont futureDayFont = CairoFont.WhiteDetailText().WithColor(FutureTextColor);

        // Shrink the date box to its text so the clock can sit right after it regardless of month name length.
        ElementBounds dateBounds = ElementBounds.Fixed(left, dateY, gridsWidth - 180, dateHeight);
        titleFont.AutoBoxSize(model.DateHeader, dateBounds, false);
        dateBounds.fixedHeight = dateHeight;

        ElementBounds clockBounds = ElementBounds.Fixed(left + dateBounds.fixedWidth + ClockGap, dateY, 140, dateHeight);
        ElementBounds progressLabelBounds = ElementBounds.Fixed(left + gridsWidth - 170, dateY + 6, 170, 22);
        ElementBounds barBounds = ElementBounds.Fixed(left, barY, gridsWidth, barHeight);
        ElementBounds daysUntilBounds = ElementBounds.Fixed(left, infoY, gridsWidth / 2 - 8, infoRowHeight);
        ElementBounds countdownBounds = ElementBounds.Fixed(left + gridsWidth - 250, infoY, 250, infoRowHeight);

        int bgHeight = gridsY + gridsHeight + 20;
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        ElementBounds bgBounds = ElementBounds.Fixed(0, 0, dialogWidth, bgHeight);

        SingleComposer?.Dispose();

        GuiComposer composer = capi.Gui
            .CreateCompo(ComposerName, dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get("calendarhandbook:dialog-title"), OnTitleBarClose)
            .AddStaticText(model.DateHeader, titleFont, dateBounds, "dateheader")
            .AddDynamicText(model.TimeOfDay, clockFont, clockBounds, ClockKey)
            .AddStaticText(
                Lang.Get("calendarhandbook:year-progress", model.ElapsedDays, model.DaysPerYear),
                detailFont,
                EnumTextOrientation.Right,
                progressLabelBounds,
                "yearprogresslabel"
            )
            .AddStatbar(barBounds, SeasonVisuals.GetColor(model.GetSeasonForMonth(model.Month)), false, "yearbar");

        CalendarSeason[] seasons = model.GetDisplaySeasonOrder();
        int seasonColW = gridsWidth / seasons.Length;
        for (int i = 0; i < seasons.Length; i++)
        {
            CalendarSeason season = seasons[i];
            composer.AddStaticText(
                SeasonVisuals.GetLabeledName(season),
                CairoFont.WhiteSmallText().WithColor(SeasonVisuals.GetColor(season)),
                ElementBounds.Fixed(left + i * seasonColW, seasonY, seasonColW - 8, 24),
                "seasonlabel" + i
            );
        }

        composer
            .AddDynamicText(model.FormatDaysUntilLabel(), detailFont, daysUntilBounds, DaysUntilKey)
            .AddDynamicText(FormatCountdownLabel(model), countdownFont, countdownBounds, CountdownKey);

        for (int index = 0; index < 12; index++)
        {
            int month = model.GetMonthAtDisplayIndex(index);
            int col = index % monthsPerRow;
            int row = index / monthsPerRow;
            int mx = left + col * (monthWidth + monthGapX);
            int my = gridsY + row * (monthInnerHeight + monthGapY);
            CalendarSeason monthSeason = model.GetSeasonForMonth(month);

            composer.AddInset(ElementBounds.Fixed(mx, my, monthWidth, monthInnerHeight), 2);
            composer.AddStaticText(
                SeasonVisuals.GetMarker(monthSeason) + " " + YearCalendarModel.GetLocalizedMonth(month),
                CairoFont.WhiteSmallText().WithColor(SeasonVisuals.GetColor(monthSeason)),
                ElementBounds.Fixed(mx + 8, my + 4, monthWidth - 16, monthTitleHeight),
                "monthtitle" + month
            );

            for (int day = 1; day <= model.DaysPerMonth; day++)
            {
                int dayIndex = day - 1;
                int dayCol = dayIndex % 7;
                int dayRow = dayIndex / 7;
                int dx = mx + 8 + dayCol * (dayCell + dayGap);
                int dy = my + monthTitleHeight + 4 + dayRow * (dayCell + dayGap);

                composer.AddInset(ElementBounds.Fixed(dx, dy, dayCell, dayCell), 1);

                CairoFont dayFont = futureDayFont;
                if (model.IsToday(month, day))
                {
                    dayFont = todayDayFont;
                }
                else if (model.IsPastDay(month, day))
                {
                    dayFont = pastDayFont;
                }

                composer.AddStaticText(
                    day.ToString(),
                    dayFont,
                    EnumTextOrientation.Center,
                    ElementBounds.Fixed(dx, dy + dayTextPad, dayCell, dayCell - dayTextPad),
                    "day-" + month + "-" + day
                );
            }
        }

        SingleComposer = composer.Compose();
        SingleComposer.UnfocusOwnElements();

        GuiElementStatbar? bar = SingleComposer.GetStatbar("yearbar");
        if (bar != null)
        {
            bar.SetValues(model.ElapsedDays, 0, model.DaysPerYear);
            bar.SetLineInterval(Math.Max(1, model.DaysPerMonth * 3));
        }

        UpdateInfoTexts(force: true);
    }

    private void UpdateInfoTexts(bool force)
    {
        if (SingleComposer == null || model == null)
        {
            return;
        }

        string clockText = model.TimeOfDay;
        string daysText = model.FormatDaysUntilLabel();
        string countdownText = FormatCountdownLabel(model);

        if (force || clockText != lastClockText)
        {
            SingleComposer.GetDynamicText(ClockKey)?.SetNewText(clockText, false, true, false);
            lastClockText = clockText;
        }

        if (force || daysText != lastDaysUntilText)
        {
            SingleComposer.GetDynamicText(DaysUntilKey)?.SetNewText(daysText, false, true, false);
            lastDaysUntilText = daysText;
        }

        if (force || countdownText != lastCountdownText)
        {
            SingleComposer.GetDynamicText(CountdownKey)?.SetNewText(countdownText, false, true, false);
            lastCountdownText = countdownText;
        }
    }

    private string FormatCountdownLabel(YearCalendarModel currentModel)
    {
        CalendarSeason target = currentModel.CountdownTargetSeason;
        string key = target == CalendarSeason.Spring
            ? "calendarhandbook:countdown-spring"
            : "calendarhandbook:countdown-winter";
        return Lang.Get(key, YearCalendarModel.FormatCountdown(countdownSeconds));
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }
}
