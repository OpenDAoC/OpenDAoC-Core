using System.Collections;
using System.Linq;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_SpeedOfSound : TimedRealmAbility
	{
		public AtlasOF_SpeedOfSound(DbAbility dba, int level) : base(dba, level) { }

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

			if (player.TempProperties.GetProperty("Charging", false)
				|| player.effectListComponent.GetAllEffects().FirstOrDefault(x => x.GetType() == typeof(SpeedOfSoundECSEffect)) != null)
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

					if (p.ControlledBrain != null && p.ControlledBrain.Body != null)
						targets.Add(p.ControlledBrain.Body);
				}
			}

			bool success;
			foreach (GameLiving target in targets)
			{
				//send spelleffect
				foreach (GamePlayer visPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					visPlayer.Out.SendSpellEffectAnimation(player, target, 7021, 0, false, CastSuccess(true));
				new SpeedOfSoundECSEffect(new ECSGameEffectInitParams(target, m_duration, 1));
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
