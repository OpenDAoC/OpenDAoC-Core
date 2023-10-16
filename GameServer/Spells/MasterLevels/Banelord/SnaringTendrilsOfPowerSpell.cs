using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    //shared timer 1

    [SpellHandler("MLUnbreakableSnare")]
    public class SnaringTendrilsOfPowerSpell : BanelordSnare
    {
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            int duration = Spell.Duration;
            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = (Spell.Duration * 4);
            return duration;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public SnaringTendrilsOfPowerSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }
    }

    [SpellHandler("UnrresistableNonImunityStun")]
    public class UnresistableNonImmunityStunSpell : MasterLevelSpellHandling
    {
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.CCImmunity) || target.HasAbility(Abilities.StunImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            effect.Owner.IsStunned = true;
            effect.Owner.attackComponent.StopAttack();
            effect.Owner.StopCurrentSpellcast();
            effect.Owner.DisableTurning(true);

            SendEffectAnimation(effect.Owner, 0, false, 1);

            MessageToLiving(effect.Owner, Spell.Message1, EChatType.CT_Spell);
            MessageToCaster(Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), EChatType.CT_Spell);
            MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)),
                EChatType.CT_Spell, effect.Owner, m_caster);

            GamePlayer player = effect.Owner as GamePlayer;
            if (player != null)
            {
                player.Client.Out.SendUpdateMaxSpeed();
                if (player.Group != null)
                    player.Group.UpdateMember(player, false, false);
            }
            else
            {
                effect.Owner.attackComponent.StopAttack();
            }

            base.OnEffectStart(effect);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.IsStunned = false;
            effect.Owner.DisableTurning(false);

            if (effect.Owner == null) return 0;

            GamePlayer player = effect.Owner as GamePlayer;

            if (player != null)
            {
                player.Client.Out.SendUpdateMaxSpeed();
                if (player.Group != null)
                    player.Group.UpdateMember(player, false, false);
            }
            else
            {
                GameNpc npc = effect.Owner as GameNpc;
                if (npc != null)
                {
                    IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
                    if (aggroBrain != null)
                        aggroBrain.AddToAggroList(Caster, 1);
                }
            }

            return 0;
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
            if (compare.SpellHandler.Spell.SpellType == ESpellType.UnrresistableNonImunityStun) return true;
            return base.IsOverwritable(compare);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override bool HasPositiveEffect
        {
            get { return false; }
        }

        public UnresistableNonImmunityStunSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
        }
    }
}