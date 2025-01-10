using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.DamageAdd)]
    public class DamageAddSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : AbstractDamageAddSpellHandler(caster, spell, spellLine)
    {
        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new DamageAddECSEffect(initParams);
        }

        public override void Handle(AttackData attackData, double effectiveness)
        {
            if (!AreArgumentsValid(attackData, out GameLiving attacker, out GameLiving target))
                return;

            double damage;

            if (Spell.Damage > 0)
            {
                CalculateDamageVariance(target, out double minVariance, out double maxVariance);
                double variance = minVariance + Util.RandomDoubleIncl() * (maxVariance - minVariance);
                effectiveness *= 1 + Caster.GetModified(eProperty.BuffEffectiveness) * 0.01;
                damage = Spell.Damage * variance * effectiveness * attackData.Interval * 0.001;
            }
            else
                damage = attackData.Damage * Spell.Damage / -100.0;

            AttackData ad = CreateAttackData(damage, attacker, target);

            if (ad.Attacker is GameNPC npcAttacker && npcAttacker.Brain is IControlledBrain brain)
            {
                GamePlayer owner = brain.GetPlayerOwner();

                if (owner != null)
                    MessageToLiving(owner, string.Format(LanguageMgr.GetTranslation(owner.Client, "DamageAddAndShield.EventHandlerDA.YourHitFor"), ad.Attacker.Name, target.GetName(0, false), ad.Damage), eChatType.CT_Spell);
            }
            else if (attacker is GamePlayer attackerPlayer)
                MessageToLiving(attacker, string.Format(LanguageMgr.GetTranslation(attackerPlayer.Client, "DamageAddAndShield.EventHandlerDA.YouHitExtra"), target.GetName(0, false), ad.Damage), eChatType.CT_Spell);

            if (target is GamePlayer targetPlayer)
                MessageToLiving(target, string.Format(LanguageMgr.GetTranslation(targetPlayer.Client, "DamageAddAndShield.EventHandlerDA.DamageToYou"), attacker.GetName(0, false), ad.Damage), eChatType.CT_Spell);

            target.OnAttackedByEnemy(ad);
            attacker.DealDamage(ad);

            foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);
        }
    }

    [SpellHandler(eSpellType.DamageShield)]
    public class DamageShieldSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : AbstractDamageAddSpellHandler(caster, spell, spellLine)
    {
        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new DamageShieldECSEffect(initParams);
        }

        public override void Handle(AttackData attackData, double effectiveness)
        {
            // Inverts attacker and target.
            if (!AreArgumentsValid(attackData, out GameLiving target, out GameLiving attacker))
                return;

            double damage;

            if (Spell.Damage > 0)
            {
                CalculateDamageVariance(target, out double minVariance, out double maxVariance);
                double variance = minVariance + Util.RandomDoubleIncl() * (maxVariance - minVariance);
                effectiveness *= 1 + Caster.GetModified(eProperty.BuffEffectiveness) * 0.01;
                damage = Spell.Damage * variance * effectiveness * attackData.Interval * 0.001;
            }
            else
                damage = attackData.Damage * Spell.Damage / -100.0;

            AttackData ad = CreateAttackData(damage, attacker, target);

            if (attacker is GamePlayer playerAttacker)
                MessageToLiving(attacker, string.Format(LanguageMgr.GetTranslation(playerAttacker.Client, "DamageAddAndShield.EventHandlerDS.YouHitFor"), target.GetName(0, false), ad.Damage), eChatType.CT_Spell);
            else if (attacker is GameNPC attackerNpc && attackerNpc.Brain is IControlledBrain brain)
            {
                GamePlayer owner = brain.GetPlayerOwner();

                if (owner != null)
                    MessageToLiving(owner, string.Format(LanguageMgr.GetTranslation(owner.Client, "DamageAddAndShield.EventHandlerDS.YourHitFor"), ad.Attacker.Name, target.GetName(0, false), ad.Damage ), eChatType.CT_Spell);
            }

            target.OnAttackedByEnemy(ad);
            attacker.DealDamage(ad);

            foreach (GamePlayer player in attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x14, target.HealthPercent);
        }
    }

    public abstract class AbstractDamageAddSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : SpellHandler(caster, spell, spellLine)
    {
        public abstract void Handle(AttackData attackData, double effectiveness);

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            // Placeholder. According to live tests, this should be (or close to) the best variance possible.
            // Damage adds are supposed to scale with weaponskill / armor, but independently from the attack's damage.
            // Damage shields are supposed to scale with spec to some extent.
            // Lower bound is supposed to be allowed to be below 1 for both.
            // Upper bound is equal to lower*(5/3) most of the time.
            // A similar effectiveness bonus as buffs may be used.
            // In practice, a damage add generally does less damage than a damage shield of the same value.
            // We may want to take level difference into account despite not being live like.
            min = 1;
            max = min * (5 / 3.0);
        }

        protected static bool AreArgumentsValid(AttackData attackData, out GameLiving attacker, out GameLiving target)
        {
            attacker = null;
            target = null;

            if (attackData.AttackResult is not eAttackResult.HitUnstyled and not eAttackResult.HitStyle)
                return false;

            target = attackData.Target;

            if (target == null ||
                target.ObjectState is not GameObject.eObjectState.Active ||
                !target.IsAlive ||
                target is GameKeepComponent or GameKeepDoor)
            {
                return false;
            }

            attacker = attackData.Attacker;

            if (attacker == null ||
                attacker.ObjectState is not GameObject.eObjectState.Active ||
                !attacker.IsAlive)
            {
                return false;
            }

            return true;
        }

        protected AttackData CreateAttackData(double damage, GameLiving attacker, GameLiving target)
        {
            // Damage adds and shields are unaffected by resistances.
            return new()
            {
                Attacker = attacker,
                Target = target,
                Damage = (int) damage,
                DamageType = Spell.DamageType,
                SpellHandler = this,
                AttackType = AttackData.eAttackType.Spell,
                AttackResult = eAttackResult.HitUnstyled
            };
        }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = Spell.Duration;
            duration *= 1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01;
            return (int) duration;
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);

            // "Your weapon is blessed by the gods!"
            // "{0}'s weapon glows with the power of the gods!"
            eChatType chatType = eChatType.CT_SpellPulse;

            if (Spell.Pulse == 0)
                chatType = eChatType.CT_Spell;

            bool upperCase = Spell.Message2.StartsWith("{0}");
            MessageToLiving(effect.Owner, Spell.Message1, chatType);
            Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, upperCase)), chatType, effect.Owner);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (!noMessages && Spell.Pulse == 0)
            {
                // "Your weapon returns to normal."
                // "{0}'s weapon returns to normal."
                bool upperCase = Spell.Message4.StartsWith("{0}");
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, upperCase)), eChatType.CT_SpellExpires, effect.Owner);
            }

            return 0;
        }

        public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
        {
            return OnEffectExpires(effect, noMessages);
        }

        public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            DbPlayerXEffect eff = new()
            {
                Var1 = Spell.ID,
                Duration = e.RemainingTime,
                IsHandler = true,
                SpellLine = SpellLine.KeyName
            };

            return eff;
        }
    }
}
