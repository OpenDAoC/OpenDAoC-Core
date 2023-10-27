﻿using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Spells;

namespace Core.GS;

public class TurretPet : GameSummonedPet
{
    public TurretPet(INpcTemplate template) : base(template) { }

    public Spell TurretSpell;

    public override int Health { get => base.Health; set => base.Health = value; }

    protected override void BuildAmbientTexts()
    {
        base.BuildAmbientTexts();

        if (ambientTexts.Count>0)
        {
            foreach (DbMobXAmbientBehavior ambientText in ambientTexts)
                ambientText.Chance /= 5;
        }
    }

    public override void StartAttack(GameObject attackTarget)
    {
        if (attackTarget == null)
            return;

        if (attackTarget is GameLiving livingTarget && GameServer.ServerRules.IsAllowedToAttack(this, livingTarget, true) == false)
            return;

        if (Brain is IControlledBrain brain)
        {
            if (brain.AggressionState == EAggressionState.Passive)
                return;
        }

        TargetObject = attackTarget;

        if (TargetObject.Realm == 0 || Realm == 0)
            m_lastAttackTickPvE = GameLoopMgr.GameLoopTime;
        else
            m_lastAttackTickPvP = GameLoopMgr.GameLoopTime;

        if (Brain is TurretMainPetTankBrain)
            attackComponent.RequestStartAttack(TargetObject);
    }

    public override void StartInterruptTimer(int duration, EAttackType attackType, GameLiving attacker)
    {
        // Don't interrupt turrets (1.90 EU).
        return;
    }
}