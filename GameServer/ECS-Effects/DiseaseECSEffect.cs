using System;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;

namespace DOL.GS
{
    [ECSGameEffectAttribute("Disease")]
    public class DiseaseECSGameEffect : ECSGameEffect
    {
        public DiseaseECSGameEffect(ISpellHandler handler, GameLiving target, int duration, double effectiveness)
            : base(handler, target, duration, effectiveness) { }

        public override void OnStartEffect()
        {
            Console.WriteLine("Disease Start");
            Owner.Disease(true);
            Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, EffectType, 1.0 - 0.15);
            Owner.BuffBonusMultCategory1.Set((int)eProperty.Strength, EffectType, 1.0 - 0.075);

            (SpellHandler as DiseaseSpellHandler).SendUpdates(this);

            (SpellHandler as DiseaseSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, eChatType.CT_Spell);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)), eChatType.CT_System, Owner);

            Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
            if (Owner is GameNPC)
            {
                IOldAggressiveBrain aggroBrain = ((GameNPC)Owner).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(SpellHandler.Caster, 1);
            }
        }
        public override void OnStopEffect()
        {
            Console.WriteLine("Disease Stop");
            Owner.Disease(false);
            Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, EffectType);
            Owner.BuffBonusMultCategory1.Remove((int)eProperty.Strength, EffectType);

            (SpellHandler as DiseaseSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message4, Owner.GetName(0, true)), eChatType.CT_SpellExpires, Owner);

            (SpellHandler as DiseaseSpellHandler).SendUpdates(this);
        }
    }
}