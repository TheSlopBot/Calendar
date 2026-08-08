using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CalendarHandbook;

public sealed class GuiDialogCalendarHandbook : GuiDialog
{
    public const string HotkeyCode = "calendarhandbook";
    private const string ComposerName = "calendarhandbook";
    private const string DaysUntilKey = "daysuntil";
    private const string CountdownKey = "seasoncountdown";

    private static readonly double[] PastTextColor = { 0.55, 0.50, 0.42, 0.9 };
    private static readonly double[] TodayTextColor = { 0.98, 0.86, 0.45, 1.0 };
    private static readonly double[] FutureTextColor = { 0.95, 0.92, 0.84, 1.0 };

    private YearCalendarModel? model;
    private long countdownTickListenerId = -1;

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
        StartCountdownTicker();
    }

    public override void OnGuiClosed()
    {
        StopCountdownTicker();
        base.OnGuiClosed();
    }

    private void ComposeDialog()
    {
        model = new YearCalendarModel(capi);

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

        ElementBounds dateBounds = ElementBounds.Fixed(left, dateY, gridsWidth - 180, dateHeight);
        ElementBounds progressLabelBounds = ElementBounds.Fixed(left + gridsWidth - 170, dateY + 6, 170, 22);
        ElementBounds barBounds = ElementBounds.Fixed(left, barY, gridsWidth, barHeight);
        ElementBounds daysUntilBounds = ElementBounds.Fixed(left, infoY, gridsWidth / 2 - 8, infoRowHeight);
        ElementBounds countdownBounds = ElementBounds.Fixed(left + gridsWidth - 320, infoY, 320, infoRowHeight);

        int bgHeight = gridsY + gridsHeight + 20;
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        ElementBounds bgBounds = ElementBounds.Fixed(0, 0, dialogWidth, bgHeight);

        CairoFont titleFont = CairoFont.WhiteSmallishText();
        CairoFont detailFont = CairoFont.WhiteDetailText().WithColor(FutureTextColor);
        CairoFont pastDayFont = CairoFont.WhiteDetailText().WithColor(PastTextColor);
        CairoFont todayDayFont = CairoFont.WhiteDetailText().WithColor(TodayTextColor);
        CairoFont futureDayFont = CairoFont.WhiteDetailText().WithColor(FutureTextColor);

        SingleComposer?.Dispose();

        GuiComposer composer = capi.Gui
            .CreateCompo(ComposerName, dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get("calendarhandbook:dialog-title"), OnTitleBarClose)
            .AddStaticText(model.DateHeader, titleFont, dateBounds, "dateheader")
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
            .AddDynamicText(model.FormatCountdownLabel(), detailFont, countdownBounds, CountdownKey);

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

        RefreshCountdownTexts();
    }

    private void StartCountdownTicker()
    {
        StopCountdownTicker();
        countdownTickListenerId = capi.World.RegisterGameTickListener(OnCountdownTick, 250);
    }

    private void StopCountdownTicker()
    {
        if (countdownTickListenerId >= 0)
        {
            capi.World.UnregisterGameTickListener(countdownTickListenerId);
            countdownTickListenerId = -1;
        }
    }

    private void OnCountdownTick(float dt)
    {
        if (!IsOpened())
        {
            StopCountdownTicker();
            return;
        }

        RefreshCountdownTexts();
    }

    private void RefreshCountdownTexts()
    {
        if (SingleComposer == null || capi.World?.Calendar == null)
        {
            return;
        }

        model = new YearCalendarModel(capi);
        SingleComposer.GetDynamicText(DaysUntilKey)?.SetNewText(model.FormatDaysUntilLabel(), false, false, false);
        SingleComposer.GetDynamicText(CountdownKey)?.SetNewText(model.FormatCountdownLabel(), false, false, false);
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }
}
