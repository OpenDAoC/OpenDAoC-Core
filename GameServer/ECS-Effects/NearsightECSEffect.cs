using DOL.GS.Spells;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class NearsightECSGameEffect : ECSGameSpellEffect
    {
        public NearsightECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            TriggersImmunity = true;
        }

        public override void OnStartEffect()
        {
            // percent category
            Owner.DebuffCategory[(int)eProperty.ArcheryRange] += (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[(int)eProperty.SpellRange] += (int)SpellHandler.Spell.Value;
            //Owner.StartInterruptTimer(Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
            (SpellHandler as NearsightSpellHandler).SendEffectAnimation(Owner, 0, false, 1);
            (SpellHandler as NearsightSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, eChatType.CT_Spell);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, false)), eChatType.CT_Spell, Owner);
        }

        public override void OnStopEffect()
        {
            // percent category
            Owner.DebuffCategory[(int)eProperty.ArcheryRange] -= (int)SpellHandler.Spell.Value;
            Owner.DebuffCategory[(int)eProperty.SpellRange] -= (int)SpellHandler.Spell.Value;

            (SpellHandler as NearsightSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message4, Owner.GetName(0, false)), eChatType.CT_SpellExpires, Owner);
        }
    }
}