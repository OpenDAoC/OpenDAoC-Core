using System;
using System.Buffers;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.SpreadHeal)]
    public class SpreadhealSpellHandler : HealSpellHandler
    {
        public override string ShortDescription => $"Heals group members for {Spell.Value}, prioritizing the most injured ones.";

        public SpreadhealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        private readonly struct InjuredTarget
        {
            public readonly GameLiving Living;
            public readonly double HealthPercent;

            public InjuredTarget(GameLiving living, double healthPercent)
            {
                Living = living;
                HealthPercent = healthPercent;
            }
        }

        public override bool HealTarget(GameLiving target, double amount, bool affectedByDisease)
        {
            InjuredTarget[] injuredTargetsPool = ArrayPool<InjuredTarget>.Shared.Rent(Properties.GROUP_MAX_MEMBER);
            (GameLiving Target, int UncappedHeal)[] healAmountsPool = null;

            try
            {
                Span<InjuredTarget> injuredTargets = new(injuredTargetsPool, 0, Properties.GROUP_MAX_MEMBER);
                int injuredCount = 0;

                GameLiving mostInjuredLiving = null;
                double mostInjuredPercent = 1.0;

                CalculateDamageVariance(null, out double minHealVariance, out double maxHealVariance);

                int targetHealCap = (minHealVariance >= maxHealVariance) ?
                    (int) maxHealVariance :
                    (int) (minHealVariance + Util.RandomDoubleIncl() * (maxHealVariance - minHealVariance));

                int groupHealCap = targetHealCap;
                Group group = target.Group;

                if (group is null)
                {
                    double healthPercent = target.Health / (double) target.MaxHealth;

                    if (healthPercent < 1.0)
                    {
                        injuredTargets[injuredCount++] = new(target, healthPercent);
                        mostInjuredLiving = target;
                        mostInjuredPercent = healthPercent;
                    }
                }
                else
                {
                    groupHealCap *= group.MemberCount;
                    targetHealCap *= 2;

                    foreach (GameLiving living in group.GetMembersInTheGroup())
                    {
                        if (!living.IsAlive || !target.IsWithinRadius(living, m_spell.Range))
                            continue;

                        double livingHealthPercent = living.Health / (double)living.MaxHealth;

                        if (livingHealthPercent >= 1.0)
                            continue;

                        injuredTargets[injuredCount++] = new(living, livingHealthPercent);

                        if (livingHealthPercent < mostInjuredPercent)
                        {
                            mostInjuredLiving = living;
                            mostInjuredPercent = livingHealthPercent;
                        }
                    }
                }

                if (mostInjuredLiving is null)
                {
                    SendEffectAnimation(target, 0, false, 0);
                    MessageToCaster("Your group is already fully healed!", eChatType.CT_SpellResisted);
                    return false;
                }

                healAmountsPool = ArrayPool<(GameLiving, int)>.Shared.Rent(injuredCount);
                Span<(GameLiving, int)> healAmounts = new(healAmountsPool, 0, injuredCount);
                double totalHealed = 0;

                // Calculate initial heal for all targets.
                for (int i = 0; i < injuredCount; i++)
                {
                    InjuredTarget injured = injuredTargets[i];
                    double targetHealPercent = targetHealCap / (double) mostInjuredLiving.MaxHealth + mostInjuredPercent - injured.HealthPercent;
                    int targetHeal = (int) (injured.Living.MaxHealth * targetHealPercent);

                    if (targetHeal > 0)
                    {
                        totalHealed += targetHeal;
                        healAmounts[i] = (injured.Living, targetHeal);
                    }
                    else
                        healAmounts[i] = (injured.Living, 0);
                }

                if (totalHealed <= 0)
                    return false;

                bool isCasterDiseased = affectedByDisease && Caster.IsDiseased;

                if (isCasterDiseased)
                    MessageToCaster("Your healing is reduced by disease!", eChatType.CT_SpellResisted);

                // Reduce healed hp according to groupHealCap and apply the heal.
                foreach (var (healTarget, uncappedHeal) in healAmounts)
                {
                    if (uncappedHeal <= 0)
                        continue;

                    double healRatio = groupHealCap / totalHealed;
                    int reducedHeal = (int) Math.Min(targetHealCap, uncappedHeal * healRatio);

                    if (isCasterDiseased)
                        reducedHeal /= 2;

                    if (reducedHeal > 0)
                        base.HealTarget(healTarget, reducedHeal, false);
                }

                return true;
            }
            finally
            {
                ArrayPool<InjuredTarget>.Shared.Return(injuredTargetsPool);

                if (healAmountsPool != null)
                    ArrayPool<(GameLiving, int)>.Shared.Return(healAmountsPool);
            }
        }
    }
}
