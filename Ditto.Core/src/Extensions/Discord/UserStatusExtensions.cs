using Discord;
using Ditto.Data.Discord;

namespace Ditto.Extensions
{
    public static class UserStatusExtensions
    {
        public static UserStatus ToUserStatus(this UserStatusEx userStatus)
        {
            switch (userStatus)
            {
                case UserStatusEx.Online:
                    return UserStatus.Online;
                case UserStatusEx.Invisible:
                    return UserStatus.Invisible;
                case UserStatusEx.Idle:
                    return UserStatus.AFK;
                case UserStatusEx.Dnd:
                    return UserStatus.DoNotDisturb;
            }
            return UserStatus.Online;
        }

        public static UserStatusEx ToUserStatusEx(this UserStatus userStatus)
        {
            switch (userStatus)
            {
                case UserStatus.Online:
                    return UserStatusEx.Online;

                case UserStatus.Invisible:
                case UserStatus.Offline:
                    return UserStatusEx.Offline;
                case UserStatus.Idle:
                    return UserStatusEx.Idle;
                case UserStatus.AFK:
                    return UserStatusEx.Afk;
                case UserStatus.DoNotDisturb:
                    return UserStatusEx.Dnd;
            }
            return UserStatusEx.Online;
        }

    }
}
