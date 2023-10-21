using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Camouflage)]
	public class CamouflageSpecHandler : IAbilityActionHandler
	{
		public const int DISABLE_DURATION = 300000; // 1.65, 5min cooldown

		/// <summary>
		/// Executes the stealth ability
		/// </summary>
		/// <param name="ab"></param>
		/// <param name="player"></param>
		public void Execute(Ability ab, GamePlayer player)
		{
			if (!player.IsStealthed)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Camouflage.NotStealthed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			 
			CamouflageEcsAbilityEffect camouflage = (CamouflageEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.Camouflage);
			
			if (camouflage != null)
			{				
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Camouflage.UseCamo"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			
			new CamouflageEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1));
			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Camouflage.UseCamo"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
	}
}