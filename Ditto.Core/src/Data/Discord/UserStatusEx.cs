using Discord;

namespace Ditto.Data.Discord
{
    public enum UserStatusEx
    {
        // Online (green)
        Online = UserStatus.Online,
        On = UserStatus.Online,

        // Invisible (grey)
        Invisible = UserStatus.Invisible,
        Invis = UserStatus.Invisible,
        Offline = UserStatus.Offline,
        Off = UserStatus.Offline,

        // Away (orange)
        Idle = UserStatus.Idle,
        Away = UserStatus.Idle,
        Afk = UserStatus.Idle,

        // Busy (red)
        Dnd = UserStatus.DoNotDisturb,
        DoNotDisturb = UserStatus.DoNotDisturb,
        Busy = UserStatus.DoNotDisturb
    }
}
