using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Heal)]
    public class HealSpellHandler : SpellHandler
    {
        public HealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override bool StartSpell(GameLiving target)
        {
            if (target is null && Spell.Target is eSpellTarget.PET)
                target = Caster;

            var targets = SelectTargets(target);

            if (targets.Count <= 0)
                return false;

            bool healed = false;
            CalculateHealVariance(out int minHeal, out int maxHeal);

            foreach (GameLiving healTarget in targets)
                healed |= HealTarget(healTarget, Util.Random(minHeal, maxHeal));

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

        public virtual bool HealTarget(GameLiving target, double amount)
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

            if (target.IsDiseased)
            {
                MessageToCaster("Your target is diseased!", eChatType.CT_SpellResisted);
                amount *= 0.5;
            }

            GamePlayer playerTarget = target as GamePlayer;
            GamePlayer playerCaster = Caster as GamePlayer;

            if (playerTarget != null && playerTarget.NoHelp && playerCaster != null && target != Caster)
            {
                if (playerTarget.Group == null ||
                    playerCaster.Group == null ||
                    playerCaster.Group != playerTarget.Group)
                {
                    MessageToCaster("That player does not want assistance", eChatType.CT_SpellResisted);
                    return false;
                }
            }

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

            if (Util.Chance(criticalChance))
            {
                double min = 0.1;
                double max = 1.0;
                double criticalModifier = min + Util.RandomDoubleIncl() * (max - min);
                criticalAmount = amount * criticalModifier;
                amount += criticalAmount;
            }

            amount = (int) Math.Floor(amount);

            // Heal the pet of a necromancer instead of the shade.
            if (target.EffectList.GetOfType<NecromancerShadeEffect>() != null && target.ControlledBrain?.Body is NecromancerPet necromancerPet)
                target = necromancerPet;

            int effectiveAmount = target.ChangeHealth(Caster, eHealthChangeType.Spell, (int) amount);

            if (effectiveAmount == 0)
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

            // Check for conquest activity.
            if (playerTarget != null)
            {
                if (ConquestService.ConquestManager.IsPlayerInConquestArea(playerTarget))
                    ConquestService.ConquestManager.AddContributor(playerTarget);
            }

            foreach (GameLiving attacker in target.attackComponent.Attackers.Keys)
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

                    if (npc.Brain is StandardMobBrain mobBrain)
                        mobBrain.AddToAggroList(Caster, ad.Damage);

                    npc.AddXPGainer(Caster, ad.Damage);
                }
            }

            return true;
        }

        /// <summary>
        /// Calculates heal variance based on spec
        /// </summary>
        /// <param name="min">store min variance here</param>
        /// <param name="max">store max variance here</param>
        public virtual void CalculateHealVariance(out int min, out int max)
        {
            double spellValue = m_spell.Value;

            if (m_spellLine.KeyName is GlobalSpellsLines.Item_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int) (spellValue * 0.75);
                    max = (int) (spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName is GlobalSpellsLines.Potions_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int) (spellValue * 1.00);
                    max = (int) (spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName is GlobalSpellsLines.Combat_Styles_Effect)
            {
                if (m_spell.Value > 0)
                {
                    if (UseMinVariance)
                        min = (int) (spellValue * 1.25);
                    else
                        min = (int) (spellValue * 0.75);

                    max = (int) (spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Reserved_Spells)
            {
                min = max = (int) spellValue;
                return;
            }

            if (spellValue < 0)
            {
                spellValue = spellValue / -100.0 * m_caster.MaxHealth;
                min = max = (int) spellValue;
                return;
            }

            max = (int) (spellValue * 1.25);

            if (max < 1)
                max = 1;

            double efficiency;

            if (Caster is GamePlayer)
            {
                double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);

                if (lineSpec < 1)
                    lineSpec = 1;

                efficiency = 0.25;

                if (Spell.Level > 0)
                {
                    efficiency += (lineSpec - 1.0) / Spell.Level;

                    if (efficiency > 1.25)
                        efficiency = 1.25;
                }
            }
            else
                efficiency = 1.25;

            min = (int) (spellValue * efficiency);
            min = Math.Clamp(min, 1, max);
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
