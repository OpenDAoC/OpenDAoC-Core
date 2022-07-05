using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain;

public class TurretFNFBrain : TurretBrain
{
    private static ILog log =
        LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public TurretFNFBrain(GameLiving owner)
        : base(owner)
    {
    }

    /// <summary>
    ///     Get a random target from aggro table
    /// </summary>
    /// <returns></returns>
    protected override GameLiving CalculateNextAttackTarget()
    {
        var newTargets = new List<GameLiving>();
        var oldTargets = new List<GameLiving>();

        base.CalculateNextAttackTarget();

        lock ((m_aggroTable as ICollection).SyncRoot)
        {
            foreach (var living in m_aggroTable.Keys)
            {
                if (living == null)
                    continue;

                if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion ||
                    living.ObjectState != GameObject.eObjectState.Active)
                    continue;

                //if (living.IsMezzed || living.IsStealthed)
                if (living.IsStealthed)
                    continue;

                if (!Body.IsWithinRadius(living, MAX_AGGRO_DISTANCE, true))
                    continue;

                if (!Body.IsWithinRadius(living, ((TurretPet) Body).TurretSpell.Range, true))
                    continue;

                /*if (((TurretPet)Body).TurretSpell.SpellType != "SpeedDecrease" && SpellHandler.FindEffectOnTarget(living, "SpeedDecrease") != null)
                    continue;

                if (((TurretPet)Body).TurretSpell.SpellType == "SpeedDecrease" && living.HasAbility(Abilities.RootImmunity))
                    continue;*/

                if (living is GameNPC && Owner.IsObjectGreyCon(living) &&
                    !Body.attackComponent.Attackers.Contains(living))
                {
                    if ((living as GameNPC).Brain is IControlledBrain)
                    {
                        if (((living as GameNPC).Brain as IControlledBrain).GetPlayerOwner() != null)
                        {
                            newTargets.Add(living);
                        }
                        else
                        {
                            if ((Body.Brain as IControlledBrain).GetPlayerOwner() != null &&
                                (Body.Brain as IControlledBrain).GetPlayerOwner().attackComponent.Attackers
                                .Contains(living) == false)
                                continue;
                        }
                    }
                    else
                    {
                        if ((Body.Brain as IControlledBrain).GetPlayerOwner() != null &&
                            (Body.Brain as IControlledBrain).GetPlayerOwner().attackComponent.Attackers
                            .Contains(living) == false)
                            continue;
                    }
                }

                newTargets.Add(living);
            }
        }

        foreach (GamePlayer living in Body.GetPlayersInRadius((ushort) ((TurretPet) Body).TurretSpell.Range, false))
        {
            if (living == null)
                continue;

            if (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
                continue;

            if (living.IsInvulnerableToAttack)
                continue;

            if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion ||
                living.ObjectState != GameObject.eObjectState.Active)
                continue;

            //if (living.IsMezzed || living.IsStealthed)
            if (living.IsStealthed)
                continue;

            /*if (((TurretPet)Body).TurretSpell.SpellType != "SpeedDecrease" && SpellHandler.FindEffectOnTarget(living, "SpeedDecrease") != null)
                continue;*/

            if (LivingHasEffect(living, ((TurretPet) Body).TurretSpell))
                oldTargets.Add(living);
            else
                newTargets.Add(living);
        }

        foreach (GameNPC living in Body.GetNPCsInRadius((ushort) ((TurretPet) Body).TurretSpell.Range, false))
        {
            if (living == null)
                continue;

            if (!GameServer.ServerRules.IsAllowedToAttack(Body, living, true))
                continue;

            if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion ||
                living.ObjectState != GameObject.eObjectState.Active)
                continue;

            //if (living.IsMezzed || living.IsStealthed)
            if (living.IsStealthed)
                continue;

            if (living is GameNPC && Owner.IsObjectGreyCon(living) &&
                !Body.attackComponent.Attackers.Contains(living))
            {
                if (living.Brain is IControlledBrain)
                {
                    if ((living.Brain as IControlledBrain).GetPlayerOwner() != null)
                    {
                        newTargets.Add(living);
                    }
                    else
                    {
                        if ((Body.Brain as IControlledBrain).GetPlayerOwner() != null &&
                            (Body.Brain as IControlledBrain).GetPlayerOwner().attackComponent.Attackers
                            .Contains(living) == false)
                            continue;
                    }
                }
                else
                {
                    if ((Body.Brain as IControlledBrain).GetPlayerOwner() != null &&
                        (Body.Brain as IControlledBrain).GetPlayerOwner().attackComponent.Attackers
                        .Contains(living) == false)
                        continue;
                }
            }

            /*if (((TurretPet)Body).TurretSpell.SpellType != "SpeedDecrease" && SpellHandler.FindEffectOnTarget(living, "SpeedDecrease") != null)
                continue;

            if (((TurretPet)Body).TurretSpell.SpellType == "SpeedDecrease" && (living.HasAbility(Abilities.RootImmunity) || living.HasAbility(Abilities.DamageImmunity)))
                continue;*/

            if (LivingHasEffect(living, ((TurretPet) Body).TurretSpell))
                oldTargets.Add(living);
            else
                newTargets.Add(living);
        }

        if (newTargets.Count > 0)
            return newTargets[Util.Random(newTargets.Count - 1)];
        if (oldTargets.Count > 0) return oldTargets[Util.Random(oldTargets.Count - 1)];

        m_aggroTable.Clear();
        return null;
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
        AddToAggroList(ad.Attacker, (ad.Attacker.Level + 1) << 1);
    }

    /// <summary>
    ///     Updates the pet window
    /// </summary>
    public override void UpdatePetWindow()
    {
    }
}