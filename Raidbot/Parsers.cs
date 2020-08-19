using System;
using System.Globalization;

namespace Raidbot
{
    public static class Parsers
    {
        public static bool TryParseDateTime(string message, out DateTime startTime)
        {
            return DateTime.TryParse(message, Constants.Culture, DateTimeStyles.None, out startTime);
        }

        public static bool TryParseDouble(string message, out double number)
        {
            if (double.TryParse(message, Constants.style, Constants.Culture, out number)) return true;
            return double.TryParse(message, Constants.style, Constants.UsCulture, out number);
        }

        public static bool TryParseRole(string message, out int noPositions, out string roleName, out string roleDescription)
        {
            roleDescription = string.Empty;
            roleName = string.Empty;
            noPositions = 0;
            if (!message.Contains(":")) return false;

            if (!int.TryParse(message.Substring(0, message.IndexOf(":")).Trim(), out noPositions))
            {
                return false;
            }
            roleName = message.Substring(message.IndexOf(":") + 1).Trim();
            if (roleName.Contains(":"))
            {
                roleDescription = roleName.Substring(roleName.IndexOf(":") + 1).Trim();
                roleName = roleName.Substring(0, roleName.IndexOf(":")).Trim();
            }
            return true;
        }
    }
}
