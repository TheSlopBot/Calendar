using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CalendarHandbook;

public sealed class CalendarHandbookModSystem : ModSystem
{
    private ICoreClientAPI? capi;
    private GuiDialogCalendarHandbook? dialog;
    private ICalendarSolver? solver;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        capi = api;

        api.Input.RegisterHotKey(
            GuiDialogCalendarHandbook.HotkeyCode,
            Lang.Get("calendarhandbook:open-calendar"),
            GlKeys.L,
            HotkeyType.GUIOrOtherControls
        );

        dialog = new GuiDialogCalendarHandbook(api, GetOrCreateSolver);
        api.Gui.RegisterDialog(dialog);
        api.Event.LeaveWorld += OnLeaveWorld;
        api.Logger.Notification("[calendarhandbook] Client started. Press L to open the year calendar.");
    }

    internal ICalendarSolver GetOrCreateSolver()
    {
        if (solver != null)
        {
            return solver;
        }

        ICoreClientAPI api = capi!;
        ICalendarSolver? created = null;
        bool solarYearsLoaded = SolarYearsSolver.SolarYearsInstalled(api);
        if (solarYearsLoaded)
        {
            created = SolarYearsSolver.Create(api);
            if (created == null)
            {
                int daysPerMonth = api.World?.Calendar?.DaysPerMonth ?? 0;
                api.Logger.Notification(
                    "[calendarhandbook] Solar Years is loaded but provided no calendar config (it only activates when the world has 30-day months; this world has {0}). Using ModuloSolver.",
                    daysPerMonth
                );
            }
        }

        if (created == null)
        {
            created = new ModuloSolver(api.World.Calendar!);
        }

        solver = created;
        api.Logger.Notification("[calendarhandbook] Using {0}.", solver.GetType().Name);
        return solver;
    }

    private void OnLeaveWorld()
    {
        solver = null;
    }

    public override void Dispose()
    {
        if (capi != null)
        {
            capi.Event.LeaveWorld -= OnLeaveWorld;
        }

        if (dialog != null)
        {
            if (dialog.IsOpened())
            {
                dialog.TryClose();
            }

            dialog.Dispose();
            dialog = null;
        }

        solver = null;
        capi = null;
        base.Dispose();
    }
}
