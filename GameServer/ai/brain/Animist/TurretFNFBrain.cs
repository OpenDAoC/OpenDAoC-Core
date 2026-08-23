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
            // FnFs prioritize entities by placing them in different buckets based on distance.
            // This is a custom logic to make them more likely to target an entity they can actually cast on.

            // Fractions of AggroRange marking bucket boundaries (must be in ascending order).
            private static readonly double[] _distanceBucketThresholds = [0.4];

            private static int DistanceBucketCount => _distanceBucketThresholds.Length + 1;

            public FnfTurretThreatStrategy(StandardMobBrain owner) : base(owner) { }

            public override long CalculateEffectiveAggro(long baseAggro, GameLiving target, out double distance)
            {
                // Fnf turrets don't care about effective aggro, target selection is random.
                // We're repurposing it to bucket entities by distance.
                distance = _owner.Body.GetDistanceTo(target);
                return GetDistanceBucket(distance);
            }

            private int GetDistanceBucket(double distance)
            {
                for (int i = 0; i < _distanceBucketThresholds.Length; i++)
                {
                    if (distance < _owner.AggroRange * _distanceBucketThresholds[i])
                        return i;
                }

                return _distanceBucketThresholds.Length; // Farthest bucket.
            }

            public override GameLiving SelectTarget(ReadOnlySpan<AggroTable.TargetCandidate> candidates)
            {
                if (candidates.Length == 0 ||
                    _owner.Body is not TurretPet turretPet ||
                    turretPet.Brain is not StandardMobBrain brain)
                {
                    return null;
                }

                Spell turretSpell = turretPet.TurretSpell;

                if (turretSpell == null)
                    return null;

                int randomIndex = Util.Random(candidates.Length - 1);
                int slotCount = DistanceBucketCount * 2; // Primary block, then fallback block.
                Span<int> bestIndexByPriority = stackalloc int[slotCount];
                bestIndexByPriority.Fill(-1);

                for (int i = 0; i < candidates.Length; i++)
                {
                    int index = (randomIndex + i) % candidates.Length;
                    int priority = GetPriority(candidates[index], brain, turretSpell);

                    if (priority == -1)
                        continue;

                    ref int slot = ref bestIndexByPriority[priority];

                    if (slot == -1)
                        slot = index;

                    if (priority == 0)
                        break; // Closest + untouched, the best possible match. No need to look further.
                }

                foreach (int index in bestIndexByPriority)
                {
                    if (index != -1)
                        return candidates[index].Living;
                }

                return null;
            }

            public override bool ShouldBeRemoved(GameLiving target)
            {
                return base.ShouldBeRemoved(target) || !_owner.Body.IsWithinRadius(target, _owner.AggroRange);
            }

            private static int GetPriority(AggroTable.TargetCandidate candidate, StandardMobBrain brain, Spell turretSpell)
            {
                // Lower value = higher priority.
                // Primary (untouched) candidates occupy [0, bucketCount), ordered closest-first.
                // Fallback candidates occupy [bucketCount, 2*bucketCount), ordered closest-first.
                // Returns -1 if the candidate isn't a valid target at all.

                GameLiving living = candidate.Living;
                int distanceBucket = (int) candidate.EffectiveAggro;
                bool untouched = !brain.LivingHasEffect(living, turretSpell) &&
                    !living.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity);

                if (!untouched && turretSpell.Damage <= 0)
                    return -1; // No damage, fallback tiers don't apply.

                int fallbackOffset = untouched ? 0 : DistanceBucketCount;
                return fallbackOffset + distanceBucket;
            }
        }
    }
}
