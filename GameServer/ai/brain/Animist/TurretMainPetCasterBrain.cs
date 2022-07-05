using System.Collections;
using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain;

public class TurretMainPetCasterBrain : TurretBrain
{
    public TurretMainPetCasterBrain(GameLiving owner) : base(owner)
    {
    }

    public override void Attack(GameObject target)
    {
        var defender = target as GameLiving;
        if (defender == null) return;

        if (!GameServer.ServerRules.IsAllowedToAttack(Body, defender, true)) return;

        if (AggressionState == eAggressionState.Passive)
        {
            AggressionState = eAggressionState.Defensive;
            UpdatePetWindow();
        }

        m_orderAttackTarget = defender;
        //AttackMostWanted();

        Body.StartAttack(m_orderAttackTarget);
    }

    protected override GameLiving CalculateNextAttackTarget()
    {
        var newTargets = new List<GameLiving>();
        var oldTargets = new List<GameLiving>();

        var normal = base.CalculateNextAttackTarget();

        if (AggressionState != eAggressionState.Aggressive || normal != null)
            return normal;

        var livingList = new List<GameLiving>();
        lock ((m_aggroTable as ICollection).SyncRoot)
        {
            foreach (var living in m_aggroTable.Keys)
            {
                if (!living.IsAlive || living.CurrentRegion != Body.CurrentRegion ||
                    living.ObjectState != GameObject.eObjectState.Active)
                    continue;

                if (!Body.IsWithinRadius(living, MAX_AGGRO_DISTANCE, true))
                    continue;

                if (!Body.IsWithinRadius(living, ((TurretPet) Body).TurretSpell.Range, true))
                    continue;

                if (living.IsStealthed)
                    continue;
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

            if (living.IsStealthed)
                continue;

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

            if (living.IsStealthed)
                continue;

            if (living is GameNPC && Owner.IsObjectGreyCon(living) && !Body.attackComponent.Attackers.Contains(living))
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
                    if ((Body.Brain as IControlledBrain).GetPlayerOwner() != null && (Body.Brain as IControlledBrain)
                        .GetPlayerOwner().attackComponent.Attackers.Contains(living) == false)
                        continue;
                }
            }

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

    public override void CheckNPCAggro()
    {
        if (AggressionState == eAggressionState.Aggressive) base.CheckNPCAggro();
    }

    public override void CheckPlayerAggro()
    {
        if (AggressionState == eAggressionState.Aggressive) base.CheckPlayerAggro();
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (AggressionState != eAggressionState.Passive) AddToAggroList(ad.Attacker, (ad.Attacker.Level + 1) << 1);
    }
}