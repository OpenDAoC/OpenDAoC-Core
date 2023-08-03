using System;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Handler for Stealth Spec clicks
	/// </summary>
	[SkillHandlerAttribute(Abilities.Camouflage)]
	public class CamouflageHandler : IAbilityActionHandler
	{
		public const int DISABLE_DURATION = 300000; //atlas 5min cooldown

		/// <summary>
		/// Executes the stealth ability
		/// </summary>
		/// <param name="ab"></param>
		/// <param name="player"></param>
		public void Execute(AbilityUtil ab, GamePlayer player)
		{
			if (!player.IsStealthed)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Camouflage.NotStealthed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			 
			CamouflageEcsEffect camouflage = (CamouflageEcsEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.Camouflage);
			
			if (camouflage != null)
			{				
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Camouflage.UseCamo"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			
			new CamouflageEcsEffect(new ECSGameEffectInitParams(player, 0, 1));
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Camouflage.UseCamo"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
	}
}