using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Quickcast)]
	public class QuickCastAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// The ability disable duration in milliseconds
		/// </summary>
		public const int DISABLE_DURATION = 30000;

		/// <summary>
		/// Executes the ability
		/// </summary>
		/// <param name="ab">The used ability</param>
		/// <param name="player">The player that used the ability</param>
		public void Execute(Ability ab, GamePlayer player)
		{									
			// Cannot change QC state if already casting a spell (can't turn it off!)
			if(player.CurrentSpellHandler != null)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.QuickCast.CannotUseIsCasting"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			QuickCastEcsAbilityEffect quickcast = (QuickCastEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.QuickCast);
			if (quickcast!=null)
			{
				quickcast.Cancel(true);
				return;
			}			

			// Dead can't quick cast
			if(!player.IsAlive)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.QuickCast.CannotUseDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			// Can't quick cast if in attack mode
			if(player.attackComponent.AttackState)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.QuickCast.CannotUseInMeleeCombat"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			long quickcastChangeTick = player.TempProperties.GetProperty<long>(GamePlayer.QUICK_CAST_CHANGE_TICK);
			long changeTime = player.CurrentRegion.Time - quickcastChangeTick;
			if(changeTime < DISABLE_DURATION)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.QuickCast.CannotUseChangeTick", ((DISABLE_DURATION - changeTime) / 1000)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                //30 sec is time between 2 quick cast 
				return;
			}

			//TODO: more checks in this order

			//player.DisableSkill(ab,DURATION / 10);

			new QuickCastEcsAbilityEffect(new EcsGameEffectInitParams(player, QuickCastEcsAbilityEffect.DURATION, 1));
		}
	}
}
