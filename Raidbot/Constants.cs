using Discord;
using System.Globalization;

namespace Raidbot
{
    public static class Constants
    {
        public static string DateFormat = "dd.MM.yyyy hh:mm";
        public static CultureInfo Culture = new CultureInfo("de-DE");
        public static CultureInfo UsCulture = new CultureInfo("en-US");
        public static NumberStyles style = NumberStyles.AllowDecimalPoint;
        public static Emoji SignOffEmoji = new Emoji("\u274C");
        public static Emoji SignOnEmoji = new Emoji("\u2705");
        public static Emoji UnsureEmoji = new Emoji("\u2754");
        public static Emoji BackupEmoji = new Emoji("\uD83C\uDCCF");
        public static Emoji FlexEmoji = new Emoji("\u2755");
        public const string SAVEFOLDER = "data";
        public const string ACCOUNT_REGEX = "^[a-zA-z ]{3,27}\\.[0-9]{4}$";
        public const int MaxFlexRoles = 2;
        public enum Availability { SignedUp, Maybe, Backup, Flex };

        /*
        readonly IEmote[] reactions = new IEmote[] { //Emote.Parse("<:warrior_spellbreaker:666957477690081290>"),
                                            //Emote.Parse("<:warrior_berserker:666957477987876904>"),
                                            Emote.Parse("<:warrior:666957477576704031>"),
                                            //Emote.Parse("<:thief_deadeye:666957477891538965>"),
                                            //Emote.Parse("<:thief_daredevil:666957478201786378>"),
                                            Emote.Parse("<:thief:666957478193397770>"),
                                            //Emote.Parse("<:revenant_renegade:666957477253742603>"),
                                           // Emote.Parse("<:revenant_herald:666957477459525634>"),
                                            Emote.Parse("<:revenant:666957477337759744>"),
                                            //Emote.Parse("<:ranger_soulbeast:666957477618778132>"),
                                            //Emote.Parse("<:ranger_druid:666957477903859712>"),
                                            Emote.Parse("<:ranger:666957478138740746>"),
                                           //Emote.Parse("<:necro_scourge:666957478000328715>"),
                                            //Emote.Parse("<:necro_reaper:666957478059180061>"),
                                            Emote.Parse("<:necro:666957476998021131>"),
                                           // Emote.Parse("<:Mirage_tango_icon_200px:666957477983682589>"),
                                            //Emote.Parse("<:mesmer_chronomancer:666957477727830026>"),
                                            Emote.Parse("<:mesmer:666957477467652107>"),
                                            //Emote.Parse("<:guardian_firebrand:666957477421776926>"),
                                            //Emote.Parse("<:guardian_dragonhunter_:666957477186633738>"),
                                            Emote.Parse("<:guardian:666957477476040714>"),
                                            //Emote.Parse("<:engineer_scrapper:666957477702533132>"),
                                           // Emote.Parse("<:engineer_holosmith:666957478063505408>"),
                                            Emote.Parse("<:engineer:666957477006540801>"),
                                           // Emote.Parse("<:ele_weaver:666957477383766026>"),
                                           // Emote.Parse("<:ele_tempest:666957477832687627>"),
                                            Emote.Parse("<:ele:666957477169856533>")
        };*/
    }
}
