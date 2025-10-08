using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Heal)]
    public class HealSpellHandler : SpellHandler
    {
        public override string ShortDescription =>
            Spell.Value > 0 ?
            $"Heals the target for {Spell.Value} hit points." :
            $"Heals the target for {Math.Abs(Spell.Value)}% hit points.";

        public HealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool StartSpell(GameLiving target)
        {
            if (target is null && Spell.Target is eSpellTarget.PET)
                target = Caster;

            var targets = SelectTargets(target);

            if (targets.Count <= 0)
                return false;

            double spellValue = m_spell.Value;
            double min, max;

            if (spellValue < 0)
            {
                spellValue = spellValue / -100.0 * m_caster.MaxHealth;
                min = max = 1.0;
            }
            else
                CalculateDamageVariance(null, out min, out max);

            bool healed = false;

            foreach (GameLiving healTarget in targets)
            {
                double variance = min + Caster.GetPseudoDoubleIncl(RandomDeckEvent.DamageVariance) * (max - min);
                healed |= HealTarget(healTarget, spellValue * variance, true);
            }

            // Group heals seem to use full power even if no heal happens.
            if (!healed && Spell.Target is eSpellTarget.REALM)
                m_caster.Mana -= PowerCost(target) / 2; // Only half if no heal.
            else
                m_caster.Mana -= PowerCost(target);

            if (Spell.Pulse == 0)
            {
                foreach (GameLiving healTarget in targets)
                {
                    if (healTarget.IsAlive)
                        SendEffectAnimation(healTarget, 0, false, healed ? (byte) 1 : (byte) 0);
                }
            }

            if (!healed && Spell.CastTime == 0)
                m_startReuseTimer = false;

            return true;
        }

        public virtual bool HealTarget(GameLiving target, double amount, bool affectedByDisease)
        {
            if (target == null || target.ObjectState is not GameObject.eObjectState.Active)
                return false;

            if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
                return false;

            if (!target.IsAlive)
            {
                MessageToCaster($"{target.GetName(0, true)} is dead!", eChatType.CT_SpellResisted);
                return false;
            }

            if (affectedByDisease && target.IsDiseased)
            {
                MessageToCaster("Your target is diseased!", eChatType.CT_SpellResisted);
                amount *= 0.5;
            }

            GamePlayer playerCaster = Caster as GamePlayer;

            // [Atlas - Takii] Disabling MOC effectiveness scaling in OF.
            /*double mocFactor = 1.0;
            MasteryofConcentrationEffect moc = Caster.EffectList.GetOfType<MasteryofConcentrationEffect>();

            if (moc != null)
            {
                GamePlayer playerCaster = Caster as GamePlayer;
                AtlasOF_MasteryofConcentration ra = playerCaster.GetAbility<AtlasOF_MasteryofConcentration>();

                if (ra != null)
                    mocFactor = (double) ra.GetAmountForLevel(ra.Level) / 100.0;

                amount = amount * mocFactor;
            }*/

            double effectiveness;

            if (playerCaster != null)
                effectiveness = Caster.Effectiveness + Caster.GetModified(eProperty.HealingEffectiveness) * 0.01;
            else
                effectiveness = 1.0;

            if (Caster is GamePlayer spellCaster && spellCaster.UseDetailedCombatLog && effectiveness != 1)
                spellCaster.Out.SendMessage($"heal effectiveness: {effectiveness:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

            amount *= 1.0 + RelicMgr.GetRelicBonusModifier(Caster.Realm, eRelicType.Magic);
            amount *= effectiveness;

            /*if (playerTarget != null)
            {
                GameSpellEffect HealEffect = SpellHandler.FindEffectOnTarget(playerTarget, "EfficientHealing");

                if (HealEffect != null)
                {
                    double HealBonus = amount * ((int) HealEffect.Spell.Value * 0.01);
                    amount += (int) HealBonus;
                    playerTarget.Out.SendMessage($"Your Efficient Healing buff grants you a additional {HealBonus} in the Heal!", eChatType.CT_Spell, eChatLoc.CL_ChatWindow);
                }

                GameSpellEffect EndEffect = SpellHandler.FindEffectOnTarget(playerTarget, "EfficientEndurance");

                if (EndEffect != null)
                {
                    double EndBonus = amount * ((int) EndEffect.Spell.Value * 0.01);
                    //600 / 10 = 60end
                    playerTarget.Endurance += (int) EndBonus;
                    playerTarget.Out.SendMessage("Your Efficient Endurance buff grants you {EndBonus} Endurance from the Heal!", eChatType.CT_Spell, eChatLoc.CL_ChatWindow);
                }
            }*/

            /*GameSpellEffect flaskHeal = FindEffectOnTarget(target, "HealFlask");

            if (flaskHeal != null)
                amount += (int) ((amount * flaskHeal.Spell.Value) * 0.01);*/

            double criticalAmount = 0;
            double preCriticalAmount = amount;
            int criticalChance = Caster.GetModified(eProperty.CriticalHealHitChance);

            if (Caster.Chance(RandomDeckEvent.CriticalChance, criticalChance))
            {
                double min = 0.1;
                double max = 1.0;
                double criticalMod = min + Caster.GetPseudoDoubleIncl(RandomDeckEvent.CriticalVariance) * (max - min);
                criticalAmount = amount * criticalMod;
                amount += criticalAmount;
            }

            amount = (int) Math.Floor(amount);

            // Heal the pet of a necromancer instead of the shade.
            if (target.EffectList.GetOfType<NecromancerShadeEffect>() != null && target.ControlledBrain?.Body is NecromancerPet necromancerPet)
                target = necromancerPet;

            int effectiveAmount = target.ChangeHealth(Caster, eHealthChangeType.Spell, (int) amount);

            if (effectiveAmount <= 0)
            {
                if (Spell.Pulse == 0)
                {
                    if (ShouldSendMessageAsSelfHeal(Caster, target))
                        MessageToCaster("You are fully healed.", eChatType.CT_SpellResisted);
                    else
                        MessageToCaster($"{target.GetName(0, true)} is fully healed.", eChatType.CT_SpellResisted);
                }

                return false;
            }

            if (ShouldSendMessageAsSelfHeal(Caster, target))
            {
                MessageToCaster($"You heal yourself for {preCriticalAmount:0} hit points.", eChatType.CT_Spell);

                if (effectiveAmount < amount)
                    MessageToCaster("You are fully healed.", eChatType.CT_Spell);
            }
            else
            {
                MessageToCaster($"You heal {target.GetName(0, false)} for {preCriticalAmount:0} hit points!", eChatType.CT_Spell);
                MessageToLiving(target, $"You are healed by {m_caster.GetName(0, false)} for {effectiveAmount:0} hit points.", eChatType.CT_Spell);

                if (effectiveAmount < amount)
                    MessageToCaster($"{target.GetName(0, true)} is fully healed.", eChatType.CT_Spell);
            }

            if (effectiveAmount > 0 && criticalAmount > 0)
                MessageToCaster($"You heal for an extra {criticalAmount:0} hit points! ({criticalChance:0.##}%)", eChatType.CT_Spell);

            foreach (GameLiving attacker in target.attackComponent.AttackerTracker.Attackers)
            {
                if (attacker is GameNPC npc)
                {
                    AttackData ad = new()
                    {
                        Attacker = Caster,
                        Target = target,
                        AttackType = AttackData.eAttackType.Spell,
                        SpellHandler = this,
                        AttackResult = eAttackResult.HitUnstyled,
                        IsSpellResisted = false,
                        Damage = effectiveAmount,
                        DamageType = Spell.DamageType,
                        CausesCombat = false
                    };

                    // Reduced aggro generation from heals. Just for balance reasons. May not be live-accurate at all.
                    if (npc.Brain is StandardMobBrain mobBrain)
                        mobBrain.AddToAggroList(Caster, (long) (ad.Damage * 0.5));

                    npc.AddXPGainer(Caster, ad.Damage);
                }
            }

            playerCaster?.Statistics.AddToHitPointsHealed((uint) effectiveAmount);
            return true;
        }

        public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            if (m_spellLine.KeyName is GlobalSpellsLines.Item_Effects)
            {
                min = 0.75;
                max = 1.25;
                return;
            }

            if (m_spellLine.KeyName is GlobalSpellsLines.Potions_Effects)
            {
                min = 1.00;
                max = 1.25;
                return;
            }

            if (m_spellLine.KeyName is GlobalSpellsLines.Combat_Styles_Effect)
            {
                min = max = 1.25;
                return;
            }

            if (m_spellLine.KeyName is GlobalSpellsLines.Reserved_Spells)
            {
                min = max = 1.0;
                return;
            }

            if (Caster is GamePlayer)
            {
                double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);

                if (lineSpec < 1)
                    lineSpec = 1;

                min = 0.25;

                if (Spell.Level > 0)
                {
                    min += (lineSpec - 1.0) / Spell.Level;

                    if (min > 1.25)
                        min = 1.25;
                }
            }
            else
                min = 1.25;

            max = 1.25;
            return;
        }

        private static bool ShouldSendMessageAsSelfHeal(GameLiving caster, GameLiving target)
        {
            // Important to ignore pets healing themselves.
            if (target is not GamePlayer)
                return false;

            // A player healing itself.
            if (caster == target)
                return true;

            // A pet healing its owner.
            return caster is GameNPC npcCaster && npcCaster.Brain is IControlledBrain npcCasterBrain && npcCasterBrain.GetPlayerOwner() == target;
        }
    }
}
