using System;
using DOL.AI.Brain;

namespace DOL.GS
{
    public static class EpicNpcArmorFactor
    {
        // Legacy per-attacker reduction, formerly computed by AttackerTracker's epic NPC timer.
        public static double Calculate(double defaultFactor, int petCap, int playerCount, int petCount)
        {
            double factor = defaultFactor - 0.04 * playerCount - 0.01 * Math.Min(petCount, petCap);
            return Math.Max(0.4, factor);
        }

        // Single dispatch seam: an active raid encounter owns the armor factor, the legacy formula otherwise.
        public static double Resolve(GameNPC npc, IGameEpicNpc epicNpc)
        {
            return (npc.Brain as StandardMobBrain)?.RaidEncounter is { Active: true } encounter
                ? encounter.CalculateArmorFactorScalingFactor(epicNpc.DefaultArmorFactorScalingFactor, encounter.GetActiveAttackerCount())
                : Calculate(epicNpc.DefaultArmorFactorScalingFactor, epicNpc.ArmorFactorScalingFactorPetCap,
                      npc.attackComponent.AttackerTracker.PlayerCount, npc.attackComponent.AttackerTracker.PetCount);
        }
    }
}
