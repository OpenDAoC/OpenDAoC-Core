using System;
using System.Text.RegularExpressions;
using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

public class GameEpicBoss : GameNpc
{
    public GameEpicBoss() : base()
    {
        ScalingFactor = 80;
        OrbsReward = ServerProperty.EPICBOSS_ORBS;
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        base.ReturnToSpawnPoint(Math.Max((short) 350, speed));
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;
        if (IsAlive && keyName == AbilityConstants.ConfusionImmunity)
            return true;
        if (IsAlive && keyName == AbilityConstants.NSImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override void Die(GameObject killer)//current orb reward for epic boss is 1500
    {
        try
        {
            if (this is Legion)//Legion
                OrbsReward = 5000;

            if (this is HibCuuldurach)//Hib dragon
                OrbsReward = 5000;

            if (this is MidGjalpinulva)//Mid dragon
                OrbsReward = 5000;

            if (this is AlbGolestandt)//Alb dragon
                OrbsReward = 5000;

            if (this is Xanxicar)//Alb dragon SI, he is weaker than realm dragons
                OrbsReward = 3000;

            if (this is Nosdoden)//Mid mutated dragon SI, he is weaker than realm dragons
                OrbsReward = 3000;

            if (this is Myrddraxis)//Hib dragon SI, he is weaker than realm dragons
                OrbsReward = 3000;

            if (MaxHealth <= 40000 && MaxHealth > 30000)// 750 orbs for normal nameds
                OrbsReward = ServerProperty.EPICBOSS_ORBS / 2;

            if (MaxHealth <= 30000 && MaxHealth >= 10000)// 375 orbs for normal nameds
                OrbsReward = ServerProperty.EPICBOSS_ORBS / 4;

            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            if (killer is GameSummonedPet pet) killer = pet.Owner; 
            
            var playerKiller = killer as GamePlayer;
            
            var achievementMob = Regex.Replace(Name, @"\s+", "");
            
            var killerBG = playerKiller?.TempProperties.GetProperty<BattleGroupUtil>(BattleGroupUtil.BATTLEGROUP_PROPERTY, null);
            
            if (killerBG != null)
            {
                lock (killerBG.Members)
                {
                    foreach (GamePlayer bgPlayer in killerBG.Members.Keys)
                    {
                        if (bgPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                        {
                            if (bgPlayer.Level < 45) continue;
                            CoreRogMgr.GenerateReward(bgPlayer,OrbsReward);
                            CoreRogMgr.GenerateBeetleCarapace(bgPlayer);
                            bgPlayer.Achieve($"{achievementMob}-Credit");
                        }
                    } 
                }
            }
            else if (playerKiller?.Group != null)
            {
                foreach (var groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    if (groupPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE))
                    {
                        if (groupPlayer.Level < 45) continue;
                        CoreRogMgr.GenerateReward(groupPlayer,OrbsReward);
                        CoreRogMgr.GenerateBeetleCarapace(groupPlayer);
                        groupPlayer.Achieve($"{achievementMob}-Credit");
                    }
                }
            }
            else if (playerKiller != null)
            {
                if (playerKiller.Level >= 45)
                {
                    CoreRogMgr.GenerateReward(playerKiller,OrbsReward);
                    CoreRogMgr.GenerateBeetleCarapace(playerKiller);
                    playerKiller.Achieve($"{achievementMob}-Credit");;
                }
            }
        }
        finally
        {
            base.Die(killer);
        }
    }
}

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
            //remove effects: dots and bleeds
            if (Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime) && Body.IsAlive)
            {
                var effect = EffectListService.GetEffectOnTarget(Body, EEffect.DamageOverTime);
                if (effect != null)
                    EffectService.RequestImmediateCancelEffect(effect);//remove dot effect
            }
            if (Body.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed) && Body.IsAlive)
            {
                var effect2 = EffectListService.GetEffectOnTarget(Body, EEffect.Bleed);
                if (effect2 != null)
                    EffectService.RequestImmediateCancelEffect(effect2);//remove dot effect
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