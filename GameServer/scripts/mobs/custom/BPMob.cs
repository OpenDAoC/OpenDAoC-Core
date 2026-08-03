using System;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class BPMob : GameNPC
	{
		private const ushort FARM_REGION_ID = 249;
		private const long RR7_REALM_POINTS = 1755250;
		private const int POPULATION_LIMIT = 50;
		private const int MAX_GROUP_SHARE = 8;

		public override void ProcessDeath(GameObject killer)
		{
			GamePlayer player = GetPlayerSource(killer);

			if (player != null && RewardStatus is RewardEligibility.Eligible)
				DistributeReward(player);

			base.ProcessDeath(killer);
		}

		private static GamePlayer GetPlayerSource(GameObject killer)
		{
			if (killer is GamePlayer player)
				return player;

			if (killer is GameNPC npc && npc.Brain is IControlledBrain brain)
				return brain.GetPlayerOwner();

			return null;
		}

		private void DistributeReward(GamePlayer player)
		{
			int multiplier = Util.Random(2, 3);
			bool isJackpot = Util.Random(1, 25) == 25;
			int reward = GetBaseBountyPoints() + Util.Random(1, 3);

			if (isJackpot)
				reward *= multiplier;

			int playersOnline = ClientService.Instance.GetNonGmPlayers().Count;
			Group group = player.Group;

			if (group == null)
			{
				GiveReward(player, reward, isJackpot, multiplier, playersOnline);
				return;
			}

			int share = reward / Math.Min((int) group.MemberCount, MAX_GROUP_SHARE);

			foreach (GameLiving member in group.GetMembersInTheGroup())
			{
				if (member is GamePlayer recipient)
					GiveReward(recipient, share, isJackpot, multiplier, playersOnline);
			}
		}

		private int GetBaseBountyPoints()
		{
			return Level switch
			{
				<= 44 => 5,
				45 => 10,
				46 => 15,
				47 => 20,
				48 => 25,
				49 => 30,
				_ => 35
			};
		}

		private static void GiveReward(GamePlayer player, int amount, bool isJackpot, int multiplier, int playersOnline)
		{
			if (player.CurrentRegionID != FARM_REGION_ID)
				return;

			if (player.RealmPoints >= RR7_REALM_POINTS)
			{
				KickFromFarmZone(player, "You are RR7 or higher, you will not be rewarded here anymore!");
				return;
			}

			if (playersOnline >= POPULATION_LIMIT && player.Client.Account.PrivLevel == 1)
			{
				KickFromFarmZone(player, $"There are {playersOnline} players online and you're in the farm zone, why don't you go play with them!");
				return;
			}

			player.GainBountyPoints(amount);

			if (isJackpot)
			{
				player.Out.SendMessage("JACKPOT!!!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendPlaySound(eSoundType.Craft, 0x04);
				player.Out.SendMessage($"You just got {multiplier}x multiplier bonus points!  Woot!", eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
			}
		}

		private static void KickFromFarmZone(GamePlayer player, string message)
		{
			player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			player.MoveTo(79, 32401, 12245, 17413, 1902);
		}
	}
}
