using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.SkillHandler
{
    [SkillHandler(Abilities.BolsteringRoar)]
    public class BolsteringRoarAbilityHandler : SpellCastingAbilityHandler
    {
		public override long Preconditions
		{
			get
			{
				return DEAD | SITTING | MEZZED | STUNNED | NOTINGROUP;
			}
		}
 		public override int SpellID
		{
			get
			{
				return 14376;
			}
		}  
 		public override bool CheckPreconditions(GameLiving living, long bitmask)
 		{ 			 
             lock (living.EffectList)
             {
                foreach (IGameEffect effect in living.EffectList)
                {
                    if (effect is GameSpellEffect)
                    {
                        GameSpellEffect oEffect = (GameSpellEffect)effect;
                        if (oEffect.Spell.SpellType.ToString().ToLower().IndexOf("speeddecrease") != -1 && oEffect.Spell.Value != 99)
                        {            
                        	GamePlayer player = living as GamePlayer;
                            if (player != null)
                                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseSnared"), EChatType.CT_System, EChatLoc.CL_SystemWindow);      
                            return true;
                        }
                    }
                }
            }
             return base.CheckPreconditions(living, bitmask);
 		}
    }
}
