using static Raidbot.Constants;

namespace Raidbot.Models
{
	public class User
	{
		public User(string role, Availability availability, string nickname, string usedAccount, ulong userid)
		{
			Role = role;
			Availability = availability;
			DiscordId = userid;
			UsedAccount = usedAccount;
			Nickname = nickname;
		}

		public string Role { get; set; }
		public Availability Availability { get; set; }
		public string Nickname { get; set; }
		public string UsedAccount { get; set; }
		public ulong DiscordId { get; set; }
	}
}