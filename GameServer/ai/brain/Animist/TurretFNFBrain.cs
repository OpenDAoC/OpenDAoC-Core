using System;
using DOL.GS;
using DOL.GS.ServerProperties;

namespace DOL.AI.Brain
{
    public class TurretFNFBrain : TurretBrain
    {
        public override int ThinkInterval => 1000;
        protected override bool CanAddToAggroListFromMultipleLosChecks => true;

        public TurretFNFBrain(GameLiving owner) : base(owner) { }

        public override void Think()
        {
            CheckProximityAggro();

            if (!CheckSpells(eCheckSpellType.Offensive))
                CheckSpells(eCheckSpellType.Defensive);
        }

        public override bool CheckProximityAggro()
        {
            // FnF turrets need to add all players and NPCs to their aggro list to be able to switch target randomly and effectively.
            _playerAggroLosChecksThisTick = 0;
            _npcAggroLosChecksThisTick = 0;
            CheckPlayerAggro();
            CheckNpcAggro();
            return HasAggro;
        }

        protected override void CheckPlayerAggro()
        {
            // Copy paste of 'base.CheckPlayerAggro()' except we add all players in range.

            foreach (var player in BuildPlayerAggroCandidateLoop())
            {
                if (!CanAggroTarget(player))
                    continue;

                if (player.IsStealthed || player.Steed != null)
                    continue;

                if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Shade))
                    continue;

                if (Properties.CHECK_LOS_BEFORE_AGGRO_FNF)
                    SendPlayerAggroLosCheck(player, player);
                else
                    AddToAggroList(player);
            }
        }

        protected override void CheckNpcAggro()
        {
            // Copy paste of 'base.CheckNPCAggro()' except we add all NPCs in range.

            foreach (var npc in BuildNpcAggroCandidateLoop())
            {
                if (!CanAggroTarget(npc))
                    continue;

                if (npc is GameTaxi or GameTrainingDummy)
                    continue;

                if (Properties.CHECK_LOS_BEFORE_AGGRO_FNF)
                {
                    if (npc.Brain is ControlledMobBrain theirControlledNpcBrain && theirControlledNpcBrain.GetPlayerOwner() is GamePlayer theirOwner)
                    {
                        SendNpcAggroLosCheck(theirOwner, npc);
                        continue;
                    }
                    else if (GetPlayerOwner() is GamePlayer ourOwner)
                    {
                        SendNpcAggroLosCheck(ourOwner, npc);
                        continue;
                    }
                }

                AddToAggroList(npc);
            }
        }

        protected override bool TrustCast(Spell spell, eCheckSpellType type, GameLiving target, bool checkLos)
        {
            // Turn towards the target we're attempting to cast on if not already casting.
            if (base.TrustCast(spell, type, target, checkLos))
            {
                if (!Body.IsCasting)
                    Body.TurnTo(target);

                return true;
            }

            return false;
        }

        protected override AggroTable BuildAggroTable()
        {
            return new(new FnfTurretThreatStrategy(this));
        }

        protected override GameLiving CalculateNextAttackTarget()
        {
            GameLiving target = CleanUpAggroListAndGetHighestModifiedThreat();
            Body.attackComponent.AttackState = target != null;
            return target;
        }

        public override void UpdatePetWindow() { }
        public override void OnAttackedByEnemy(AttackData ad) { }

        protected class FnfTurretThreatStrategy : ControlledNpcThreatStrategy
        {
            public FnfTurretThreatStrategy(StandardMobBrain owner) : base(owner) { }

            public override GameLiving SelectTarget(ReadOnlySpan<AggroTable.TargetCandidate> candidates)
            {
                if (candidates.Length == 0 ||
                    _owner.Body is not TurretPet turretPet ||
                    turretPet.Brain is not StandardMobBrain brain)
                {
                    return null;
                }

                Spell turretSpell = turretPet.TurretSpell;
                int randomIndex = Util.Random(candidates.Length - 1);
                GameLiving selectedFallback = candidates[randomIndex].Living;
                GameLiving selectedPrimary = null;

                // Prioritize targets that don't already have our effect and aren't immune to it.
                // If there's none, allow them to be attacked again but only if our spell does damage.
                if (turretSpell != null)
                {
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        int index = (randomIndex + i) % candidates.Length;
                        GameLiving living = candidates[index].Living;

                        if (!brain.LivingHasEffect(living, turretSpell) &&
                            !living.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity))
                        {
                            selectedPrimary = living;
                            break;
                        }
                    }
                }

                if (selectedPrimary != null)
                    return selectedPrimary;

                if (turretSpell != null && turretSpell.Damage > 0)
                    return selectedFallback;

                return null;
            }
        }
    }
}
