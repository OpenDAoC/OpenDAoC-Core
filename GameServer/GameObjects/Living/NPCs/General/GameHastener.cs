using System.Collections;
using Core.Events;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
	public class GameHastener : GameNpc
	{
		public GameHastener() : base() { }
		public GameHastener(INpcTemplate template) : base(template) { }

		public const int SPEEDOFTHEREALMID = 2430;
		private const int STROFTHEREALMID = 2431;

		public override bool Interact(GamePlayer player)
		{
			if (player == null || player.InCombat)
				return false;
			
			if (player.Client.Account.PrivLevel == 1 && !IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.Interact.TooFarAway", GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
				return false;
			}


			if (!base.Interact(player))
				return false;

			// just give out speed without asking
			GameNpcHelper.CastSpellOnOwnerAndPets(this, player, SkillBase.GetSpellByID(GameHastener.SPEEDOFTHEREALMID), SkillBase.GetSpellLine(GlobalSpellsLines.Realm_Spells), false);
			player.Out.SendSpellEffectAnimation(this, player, SkillBase.GetSpellByID(935).ClientEffect, 0, false, 1);


			if (player.CurrentRegion.IsCapitalCity)
				SayTo(player, string.Format("{0} {1}. {2}",
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.Greeting"),
					player.PlayerClass.Name,
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.CityMovementOffer")));
					// LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.StrengthOffer")));
			else if (IsShroudedIslesStartZone(player.CurrentZone.ID))
				SayTo(player, string.Format("{0} {1}. {2}",
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.Greeting"),
					player.PlayerClass.Name,
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.CityMovementOffer")));
			else if(!player.CurrentRegion.IsRvR)//default message outside of RvR
				SayTo(player, string.Format("{0} {1}. {2}",
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.Greeting"),
					player.PlayerClass.Name,
					LanguageMgr.GetTranslation(player.Client.Account.Language, "GameHastener.DefaultMovementOffer")));
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (base.WhisperReceive(source, str))
			{
				GamePlayer player = source as GamePlayer;
				if (player == null || player.InCombat)
					return false;

				if (GameServer.ServerRules.IsSameRealm(this, player, true))
				{
					switch (str.ToLower())
					{
						case "movement":
							if (!player.CurrentRegion.IsRvR || player.Realm == Realm)
								GameNpcHelper.CastSpellOnOwnerAndPets(this, player, SkillBase.GetSpellByID(GameHastener.SPEEDOFTHEREALMID), SkillBase.GetSpellLine(GlobalSpellsLines.Realm_Spells), false);
							break;
						// disabled until we figure out how to disable it on port outside of capital cities
						// case "strength":
						// 	if (player.CurrentRegion.IsCapitalCity)
						// 	{
						// 		TargetObject = player;
						// 		CastSpell(SkillBase.GetSpellByID(STROFTHEREALMID), SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
						// 	}
						// 	break;
					}
				}

				return true;
			}

			return false;
		}

		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list = new ArrayList();
			list.Add(string.Format("You examine {0}. {1} is {2}.", GetName(0, false), GetPronoun(0, true), GetAggroLevelString(player, false)));
			return list;
		}

		private bool IsShroudedIslesStartZone(int zoneID)
		{
			switch (zoneID)
			{
				case 51: //Isle of Glass
				case 151: //Aegir's Landing
				case 181: //Domnann
					return true;
			}
			return false;
		}
	}
}