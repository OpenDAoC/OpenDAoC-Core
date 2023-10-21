using System.Reflection;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.Players.Classes;
using log4net;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Flurry)]
	public class FlurryAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The reuse time in milliseconds for flurry ability
		/// </summary>
		protected const int REUSE_TIMER = 60 * 2000; // 2 minutes


		/// <summary>
		/// Execute the ability
		/// </summary>
		/// <param name="ab">The ability executed</param>
		/// <param name="player">The player that used the ability</param>
		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in FlurryAbilityHandler.");
				return;
			}

			#region precheck
			if (!player.IsAlive)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
			if (player.TargetObject == null)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseNoTarget"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (!(player.TargetObject is GamePlayer || player.TargetObject is GameKeepGuard))
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Flurry.TargetNotPlayerOrGuards"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (!GameServer.ServerRules.IsAllowedToAttack(player, (GameLiving)player.TargetObject, true))
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotAttackTarget", player.TargetObject.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (!player.IsObjectInFront(player.TargetObject, 180) || !player.TargetInView)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotSeeTarget", player.TargetObject.Name), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (!player.IsWithinRadius(player.TargetObject, 135)) //Didn't use AttackRange cause of the fact that player could use a Bow
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.TargetIsWithinRadius"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			/*
			if (player.TargetObject is GamePlayer && SpellHandler.FindEffectOnTarget((GamePlayer)player.TargetObject, "Phaseshift") != null)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.TargetIsPhaseshifted", player.TargetObject.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                return;
			}*/
			if(player.TargetObject is GamePlayer)
			{
				NfRaSputinsLegacyEffect SputinLegacy = (player.TargetObject as GamePlayer).EffectList.GetOfType<NfRaSputinsLegacyEffect>();
				if(SputinLegacy != null)
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.TargetIsUnderSputinLegacy", player.TargetObject.Name), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);
                    return;
				}
			}
			#endregion

			GameLiving target = (GameLiving)player.TargetObject;
			int damage = 0;
			int specc = (player.PlayerClass is ClassBlademaster) ?
				player.GetModifiedSpecLevel(Specs.Celtic_Dual) : player.GetModifiedSpecLevel(Specs.Dual_Wield);

			//damage = base HP / 100 * DWspec / 2.7 that would be the original calculation
			if (target is GamePlayer)
			{
				damage = (int)(target.MaxHealth / 100 * specc / 4.5);  // prev 3.5 
			}
			else
			{ damage = (int)(target.MaxHealth / 100 * specc / 4.6); } // prev 3.6

			#region Resists
			int primaryResistModifier = target.GetResist(EDamageType.Slash);

			//Using the resist BuffBonusCategory2 - its unused in ResistCalculator
			int secondaryResistModifier = target.SpecBuffBonusCategory[(int)EProperty.Resist_Slash];

			int resistModifier = 0;
			//primary resists
			resistModifier += (int)(damage * (double)primaryResistModifier * -0.01);
			//secondary resists
			resistModifier += (int)((damage + (double)resistModifier) * (double)secondaryResistModifier * -0.01);
			//apply resists
			damage += resistModifier;

			#endregion

			//flurry is slash damage
			target.TakeDamage(player, EDamageType.Slash, damage, 0);
			/*
			GameSpellEffect mez = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (mez != null)
                mez.Cancel(false);
			*/
			//sending spell effect
			foreach (GamePlayer effPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				effPlayer.Out.SendSpellEffectAnimation(player, target, 7103, 0, false, 0x01);

            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Flurry.YouHit", target.GetName(0, false), damage), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                    
            if (target is GamePlayer)
                (target as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((target as GamePlayer).Client, "Skill.Ability.Flurry.HitYou", player.Name, damage), EChatType.CT_Damaged, EChatLoc.CL_SystemWindow);

			player.LastAttackTickPvP = player.CurrentRegion.Time;
			target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;

			player.DisableSkill(ab, REUSE_TIMER);

		}
	}
}