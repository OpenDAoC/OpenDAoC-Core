using System.Reflection;
using System.Collections;
using System.Linq;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_SpeedOfSound : TimedRealmAbility
	{
		public AtlasOF_SpeedOfSound(DBAbility dba, int level) : base(dba, level) { }

		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		int m_range = 2000;
		int m_duration = 30000; // Your group moves at twice normal speed for 30 seconds

		public override int MaxLevel { get { return 1; } }
		public override int GetReUseDelay(int level) { return 1800; } // 1800 = 30 min
		public override int CostForUpgrade(int level) { return 10; }

		public override ushort Icon { get { return 3020; } }

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer player = living as GamePlayer;
			/* if (player.IsSpeedWarped)
			 {
				 player.Out.SendMessage("You cannot use this ability while speed warped!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				 return;
			 }*/

			if (player.TempProperties.getProperty("Charging", false)
				|| player.effectListComponent.GetSpellEffects().FirstOrDefault(x => x.Name.Equals("Speed Of Sound")) != null)
			{
				player.Out.SendMessage("You already an effect of that type!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return;
			}

			DisableSkill(living);

			ArrayList targets = new ArrayList();

			if (player.Group == null)
				targets.Add(player);
			else
			{
				foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
				{
					if (player.IsWithinRadius( p, m_range ) && p.IsAlive)
						targets.Add(p);
				}
			}

			bool success;
			foreach (GamePlayer target in targets)
			{
				//send spelleffect
				success = target.EffectList.CountOfType<SpeedOfSoundEffect>() == 0;
				foreach (GamePlayer visPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					visPlayer.Out.SendSpellEffectAnimation(player, target, 7021, 0, false, CastSuccess(success));
				if (success)
				{
					//GameSpellEffect speed = Spells.SpellHandler.FindEffectOnTarget(target, "SpeedEnhancement");
					//if (speed != null)
                    //{
					//	speed.Cancel(false);
					//}
					//log.InfoFormat("Starting speed for player {0} with duration {1}", target, m_duration);
					new SpeedOfSoundECSEffect(new ECSGameEffectInitParams(target, m_duration, 1));
				}
			}

		}
		private byte CastSuccess(bool suc)
		{
			if (suc)
				return 1;
			else
				return 0;
		}
		
	}
}
