using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler("Grapple")]
    public class GrappleSpell : MasterLevelSpellHandling
    {
        private int check = 0;

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (selectedTarget is GameNPC == true)
            {
                MessageToCaster("This spell works only on realm enemys.", EChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                if (player.EffectList.GetOfType<NfRaChargeEffect>() == null && player != null)
                {
                    effect.Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, effect, 0);
                    player.Client.Out.SendUpdateMaxSpeed();
                    check = 1;
                }

                effect.Owner.attackComponent.StopAttack();
                effect.Owner.StopCurrentSpellcast();
                effect.Owner.DisarmedTime = effect.Owner.CurrentRegion.Time + Spell.Duration;
            }

            base.OnEffectStart(effect);
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            return false;
        }

        public override void FinishSpellCast(GameLiving target)
        {
            if (m_spell.SubSpellID > 0)
            {
                Spell spell = SkillBase.GetSpellByID(m_spell.SubSpellID);
                if (spell != null && spell.SubSpellID == 0)
                {
                    ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(m_caster, spell,
                        SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler.StartSpell(Caster);
                }
            }

            base.FinishSpellCast(target);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner == null) return 0;

            base.OnEffectExpires(effect, noMessages);

            GamePlayer player = effect.Owner as GamePlayer;

            if (check > 0 && player != null)
            {
                effect.Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, effect);
                player.Client.Out.SendUpdateMaxSpeed();
            }

            //effect.Owner.IsDisarmed = false;
            return 0;
        }

        /// <summary>
        /// Do not trigger SubSpells
        /// </summary>
        /// <param name="target"></param>
        public override void CastSubSpells(GameLiving target)
        {
        }

        public GrappleSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}