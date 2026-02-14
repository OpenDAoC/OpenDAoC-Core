using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Amnesia)]
    public class AmnesiaSpellHandler : SpellHandler
    {
        public override string ShortDescription => "Clears the monster's mind, causing it to forget who it was attacking. Negates any spells currently being cast by enemy player targets, but does not interrupt them unless resisted.";

        public AmnesiaSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in i) => new AmnesiaECSEffect(i));
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);

            if (target == null || !target.IsAlive)
                return;

            SendEffectAnimation(target, 0, false, 1);

            if (target is GamePlayer player)
            {
                player.styleComponent.NextCombatStyle = null;
                player.styleComponent.NextCombatBackupStyle = null;
            }
            else if (target is GameNPC npc && npc.Brain is StandardMobBrain brain)
            {
                if (Util.Chance(Spell.AmnesiaChance) && npc.TargetObject is GameLiving living)
                {
                    brain.ClearAggroList();
                    brain.ForceAddToAggroList(living); // Ensure we keep the current target even during confusion.
                }
            }

            // Amnesia only affects normal spells and not song activation (still affects pulses from songs though)
            if (target.CurrentSpellHandler?.Spell.InstrumentRequirement == 0)
                target.castingComponent.ClearSpellHandlers();

            target.rangeAttackComponent.AutoFireTarget = null;

            if (target is GamePlayer)
                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer).Client, "Amnesia.MessageToTarget"), eChatType.CT_Spell);
        }

        protected override void OnSpellNegated(GameLiving target, SpellNegatedReason reason)
        {
            base.OnSpellNegated(target, reason);

            // Start interrupt even for resisted instant amnesia.
            if (Spell.CastTime == 0)
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }
    }
}
