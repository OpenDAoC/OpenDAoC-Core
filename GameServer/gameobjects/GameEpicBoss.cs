using System;
using DOL.GS;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class GameEpicBoss : GameNPC, IGameEpicNpc
    {
        public override bool CanAwardKillCredit => true;
        public override double MaxHealthScalingFactor => 1.5;
        public double DefaultArmorFactorScalingFactor => 1.6;
        public int ArmorFactorScalingFactorPetCap => 24;
        public double ArmorFactorScalingFactor { get; set; }

        public GameEpicBoss() : base()
        {
            DamageFactor = 2.25;
            ArmorFactorScalingFactor = DefaultArmorFactorScalingFactor;
            OrbsReward = Properties.EPICBOSS_ORBS;
        }

        public override void ReturnToSpawnPoint(short speed)
        {
            base.ReturnToSpawnPoint(Math.Max((short) 350, speed));
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive)
            {
                if (keyName is GS.Abilities.CCImmunity or GS.Abilities.ConfusionImmunity or GS.Abilities.NSImmunity)
                    return true;
            }

            return base.HasAbility(keyName);
        }
    }
}

namespace DOL.AI.Brain
{
    public class EpicBossBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public EpicBossBrain()
            : base() {}
        public override void Think()
        {
            MoveToSpawnPoint();
            base.Think();
        }
        #region MoveToSpawnPoint()
        private bool Port_To_Spawn = false;
        private void MoveToSpawnPoint()
        {
            if (HasAggro && Body.IsAlive && Body.IsOutOfTetherRange && !Port_To_Spawn)
            {
                //heal to max HP 
                Body.Health = Body.MaxHealth; 
                //move to spawm point
                Body.X = Body.SpawnPoint.X;
                Body.Y = Body.SpawnPoint.Y;
                Body.Z = Body.SpawnPoint.Z;
                Body.Heading = Body.SpawnHeading;

                foreach (ECSGameEffect effect in Body.effectListComponent.GetAllEffects())
                {
                    if (effect.SpellHandler.Spell.IsHarmful)
                        EffectService.RequestImmediateCancelEffect(effect);
                }

                ClearAggroList();// clear aggro list
                Port_To_Spawn = true;
            }
            if (HasAggro && Body.TargetObject != null)
            {
                Port_To_Spawn = false; //enable this flag again so boss can port again if is too far.
            }
        }
        #endregion
    }
}
