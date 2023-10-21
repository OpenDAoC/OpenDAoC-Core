using System.Collections.Generic;
using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Distraction)]
	public class DistractionAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		/// <summary>
		/// The ability reuse time in milliseconds
		/// </summary>
		protected const int REUSE_TIMER = 10000;

		/// <summary>
		/// The ability effect duration in milliseconds
		/// </summary>
		public const int DURATION = 4000;

		private List<GameNpc> m_distractedNPCs = new List<GameNpc>();

		/// <summary>
		/// Execute dirtytricks ability
		/// </summary>
		/// <param name="ab">The used ability</param>
		/// <param name="player">The player that used the ability</param>
		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in DistractionAbilityHandler.");
				return;
			}

			if (!player.IsAlive)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                return;
			}

			if (player.IsMezzed)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseMezzed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (player.IsStunned)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStunned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (player.IsSitting)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStanding"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			var GameLoc = player.GroundTarget;
			if (GameLoc == null)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SummonAnimistPet.CheckBeginCast.GroundTargetNull"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (!player.GroundTargetInView)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInView"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (GameLoc.GetDistance(player) > 750)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInSpellRange"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			m_distractedNPCs = new List<GameNpc>();

			foreach (GameNpc npc in player.GetNPCsInRadius(1500))
			{
				if (npc.GetDistanceTo(GameLoc) < 400 && GameServer.ServerRules.IsAllowedToAttack(player, npc, true) && !(npc is GameTrainingDummy))
				{
					m_distractedNPCs.Add(npc);
				}
			}

			foreach (var distractedNpC in m_distractedNPCs)
			{
				distractedNpC.TurnTo(GameLoc.X, GameLoc.Y);
			}
			
			var DistractTimer = new EcsGameTimer(player, TurnBackToNormal, DURATION);
			DistractTimer.Start();

			player.DisableSkill(ab, REUSE_TIMER);
			//new DirtyTricksECSGameEffect(new ECSGameEffectInitParams(player, DURATION * 1000, 1));
		}
		
		protected virtual int TurnBackToNormal(EcsGameTimer timer)
		{
			foreach (var mDistractedNpC in m_distractedNPCs)
			{
				mDistractedNpC.TurnTo(mDistractedNpC.SpawnHeading);
			}
			m_distractedNPCs.Clear();
			return 0;
		}
	}
	
}
