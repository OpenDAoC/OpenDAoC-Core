using System;
using DOL.GS.PacketHandler;
using DOL.GS.SpellEffects;

namespace DOL.GS
{
    public static class EffectService
    {
        public static void Tick(long tick)
        {
            
            //Needs to be logic for each effect?
            foreach (var e in EntityManager.GetAllEffects())
            {
                foreach (var effect in e._effectComponents)
                {
                    if (effect is null)
                    {
                        continue;
                    }
                    
                    switch (effect.Type)
                    {
                        case eSpellEffect.Heal:
                            HandleHealEffect(effect is HealEffectComponent ? (HealEffectComponent) effect : default);
                            break;
                        default:
                            Console.WriteLine("No effect type handler!");
                            break;
                    }
                    EntityManager.RemoveEffect(e);
                }
            }
        }

        private static void HandleHealEffect(HealEffectComponent e)
        {

            int heal = e.Target.ChangeHealth(e.Caster, GameLiving.eHealthChangeType.Spell, (int)e.Value);

            if (e.Target == e.Caster && e.Target is GamePlayer pl)
            {
                pl.Out.SendMessage("You are healed by " + e.Caster.GetName(0, false) + " for " + heal + " hit points.", 
                    eChatType.CT_Spell,eChatLoc.CL_SystemWindow);
                pl.Out.SendSpellEffectAnimation(e.Caster, e.Target, e.SpellEffectId, 0, false, 0x01);
        
            }
            
            else if (e.Target is GamePlayer p)
            {
                p.Out.SendMessage("You are healed by " + e.Caster.GetName(0, false) + " for " + heal + " hit points.", 
                    eChatType.CT_Spell,eChatLoc.CL_SystemWindow);
            }

            else if (e.Caster is GamePlayer p2)
            {
                p2.Out.SendMessage("You heal " + e.Target.GetName(0, false) + " for " + heal + " hit points!", 
                    eChatType.CT_Spell,eChatLoc.CL_SystemWindow);
                p2.Out.SendSpellEffectAnimation(e.Caster, e.Target, e.SpellEffectId, 0, false, 0x01);
            }
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {
            
        }
        
    }
}