using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CalendarHandbook;

public sealed class CalendarHandbookModSystem : ModSystem
{
    private GuiDialogCalendarHandbook? dialog;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        api.Input.RegisterHotKey(
            GuiDialogCalendarHandbook.HotkeyCode,
            Lang.Get("calendarhandbook:open-calendar"),
            GlKeys.L,
            HotkeyType.GUIOrOtherControls
        );

        dialog = new GuiDialogCalendarHandbook(api);
        api.Gui.RegisterDialog(dialog);
        api.Logger.Notification("[calendarhandbook] Client started. Press L to open the year calendar.");
    }

    public override void Dispose()
    {
        if (dialog != null)
        {
            if (dialog.IsOpened())
            {
                dialog.TryClose();
            }

            dialog.Dispose();
            dialog = null;
        }

        base.Dispose();
    }
}
