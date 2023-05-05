namespace Ditto.Bot.Database.Data
{
    // Links are saved in the database and their values cannot be edited unless a full purge or upgrade is done.
    public enum LinkType
    {
        RSS             = 0,
        Reddit          = 1,
        Twitch          = 4,
        Twitter         = 5,

        // Black Desert Online (BDO).
        BDO             = 2,
        BDO_Maintenance = 3,

        // Update information, Last git commit hash.
        Update          = 6,

        // Discord channel linking
        Discord         = 7,

        // Role menu link
        RoleMenu        = 8,

        // Translation
        Translation     = 9,

        // Solo Leveling
        SoloLeveling    = 10,

        // Pixiv
        Pixiv           = 11,
    }
}
