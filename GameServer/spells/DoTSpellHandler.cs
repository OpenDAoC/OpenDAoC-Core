using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using static DOL.GS.GameObject;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.DamageOverTime)]
    public class DoTSpellHandler : SpellHandler
    {
        private const int UNITIALIZED_CRIT_PERCENT = -1;

        private Dictionary<GameLiving, double> _criticalDamagePercentByTarget = new();

        public override string ShortDescription => $"Inflicts {Spell.Damage} {Spell.DamageTypeToString()} damage every {Spell.Frequency / 1000.0} seconds for {Spell.Duration / 1000.0} seconds.";

        public DoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return ECSGameEffectFactory.Create(initParams, static (in i) => new DamageOverTimeECSGameEffect(i));
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override double CalculateDamageVarianceOffsetFromLevelDifference(GameLiving caster, GameLiving target)
        {
            return 0;
        }

        protected override double CalculateDistanceFallOff(int distance, int radius)
        {
            return 0;
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.Spell.EffectGroup;

            if (Spell.SpellType != compare.Spell.SpellType)
                return false;

            // For Cabalist, Bonedancer and Mentalist, we know that one baseline and one specline DoT can coexist.
            // However, this doesn't apply to Shaman's DoTs before 1.90. Meaning a simple check on the spell line is not enough.
            // Checking the frequency instead preserves the ability for those classes to have one baseline and one specline DoT coexist, excepted Shaman.
            // Other rules are unknown: Poison vs spell DoTs vs procs vs other specializations lines...
            // Damage type appears to be irrelevant and simply a guess based on the fact that some weapon procs should stack with poisons.
            return Spell.Frequency == compare.Spell.Frequency;
        }

        public override AttackData CalculateDamageToTarget(GameLiving target)
        {
            AttackData ad = base.CalculateDamageToTarget(target);

            if (SpellLine.KeyName is GlobalSpellsLines.Mundane_Poisons && Caster.effectListComponent.ContainsEffectForEffectType(eEffect.Viper))
                ad.Damage *= 2;

            /* GameSpellEffect iWarLordEffect = FindEffectOnTarget(target, "CleansingAura");
            if (iWarLordEffect != null)
                ad.Damage *= (int) (1.00 - iWarLordEffect.Spell.Value * 0.01);*/

            return ad;
        }

        public override void SendDamageMessages(AttackData ad)
        {
            GamePlayer player = null;

            if (m_caster is GamePlayer)
                player = m_caster as GamePlayer;
            else if (m_caster is GameNPC npc && npc.Brain is IControlledBrain brain)
                player = brain.GetPlayerOwner();

            if (player == null)
                return;

            if (SpellLine.KeyName is GlobalSpellsLines.Item_Effects)
                MessageToCaster(string.Format(LanguageMgr.GetTranslation(player.Client, "DoTSpellHandler.SendDamageMessages.YouHitFor", ad.Target.GetName(0, false), ad.Damage)), eChatType.CT_Spell);
            else
                MessageToCaster(string.Format(LanguageMgr.GetTranslation(player.Client, "DoTSpellHandler.SendDamageMessages.YourHitsFor", Spell.Name, ad.Target.GetName(0, false), ad.Damage)), eChatType.CT_Spell);

            if (ad.CriticalDamage > 0)
                MessageToCaster($"You critically hit for an additional {ad.CriticalDamage} damage! ({m_caster.DebuffCriticalChance}%)", eChatType.CT_Spell);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);
            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new GameSpellEffect(this, m_spell.Duration, m_spell.Frequency, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);

            if (effect.Owner.IsAlive)
            {
                // An acidic cloud surrounds you!
                MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
                // {0} is surrounded by an acidic cloud!
                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);
                OnDirectEffect(effect.Owner);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);

            if (!noMessages)
            {
                // The acidic mist around you dissipates.
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
                // The acidic mist around {0} dissipates.
                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
            }

            return 0;
        }

        public override List<GameLiving> SelectTargets(GameObject castTarget)
        {
            // Intercept the target list for critical damage percent calculation, which will be reused on each tick.
            List<GameLiving> _targets = base.SelectTargets(castTarget);
            _criticalDamagePercentByTarget = new();

            foreach (GameLiving target in _targets)
                _criticalDamagePercentByTarget[target] = UNITIALIZED_CRIT_PERCENT;

            return _targets;
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null || !target.IsAlive || target.ObjectState is not eObjectState.Active)
                return;

            AttackData ad = CalculateDamageToTarget(target);

            if (!_criticalDamagePercentByTarget.TryGetValue(target, out double criticalDamagePercent))
                return; // Shouldn't happen.

            if (criticalDamagePercent == UNITIALIZED_CRIT_PERCENT)
            {
                // First tick.
                criticalDamagePercent = CalculateCriticalDamagePercent(ad);
                _criticalDamagePercentByTarget[target] = criticalDamagePercent;
                ad.CausesCombat = true;
            }
            else
                ad.CausesCombat = false;

            ad.CriticalDamage = (int) (ad.Damage * criticalDamagePercent);
            SendDamageMessages(ad);
            DamageTarget(ad, false);
        }

        public bool IsFirstTick(GameLiving target)
        {
            return _criticalDamagePercentByTarget.TryGetValue(target, out double criticalDamagePercent) && criticalDamagePercent == UNITIALIZED_CRIT_PERCENT;
        }

        private double CalculateCriticalDamagePercent(AttackData ad)
        {
            ad.CriticalChance = Math.Min(50, Caster.DebuffCriticalChance);

            if (!Caster.Chance(RandomDeckEvent.CriticalChance, ad.CriticalChance))
                return 0;

            // Crit damage for DoTs is up to 100% against players too.
            return 0.1 + Caster.GetPseudoDoubleIncl(RandomDeckEvent.CriticalVariance) * 0.9;
        }

        protected override double CalculateBuffDebuffEffectiveness()
        {
            return 1.0; // Unused by DoTs.
        }
    }
}
