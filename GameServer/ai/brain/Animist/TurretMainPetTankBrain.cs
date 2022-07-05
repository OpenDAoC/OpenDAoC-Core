using System;
using DOL.Events;
using DOL.GS;

namespace DOL.AI.Brain;

public class TurretMainPetTankBrain : TurretMainPetCasterBrain
{
    public TurretMainPetTankBrain(GameLiving owner) : base(owner)
    {
    }

    public override void Notify(DOLEvent e, object sender, EventArgs args)
    {
        if (AggressionState != eAggressionState.Passive)
            if (e == GameLivingEvent.CastFinished || e == GameLivingEvent.AttackFinished)
            {
                var pet = sender as TurretPet;
                if (pet == null || pet != Body || !(pet.Brain is TurretMainPetTankBrain))
                    return;

                if (e == GameLivingEvent.CastFinished)
                    if (Body.TargetObject != null)
                    {
                        //Force to stop spell casting
                        if (Body.IsCasting) Body.StopCurrentSpellcast();
                        if (Body.SpellTimer != null && Body.SpellTimer.IsAlive) Body.SpellTimer.Stop();
                        if (Body.TargetObject.IsWithinRadius(Body, Body.AttackRange))
                            Body.StartAttack(Body.TargetObject);
                    }

                if (e == GameLivingEvent.AttackFinished) Body.StopAttack();
                return; //Do not notify the base
            }

        base.Notify(e, sender, args);
    }

    public override void AttackMostWanted()
    {
        // Force to wait body attack before casting.
        if (Body.AttackState) return;
        CheckSpells(eCheckSpellType.Offensive);
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (AggressionState != eAggressionState.Passive) AddToAggroList(ad.Attacker, (ad.Attacker.Level + 1) << 1);
    }
}