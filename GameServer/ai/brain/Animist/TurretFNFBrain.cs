using System.Collections.Generic;
using System.Linq;
using DOL.GS;
using DOL.GS.ServerProperties;

namespace DOL.AI.Brain
{
    public class TurretFNFBrain : TurretBrain
    {
        private List<GameLiving> _filteredAggroList = [];

        public TurretFNFBrain(GameLiving owner) : base(owner) { }

        protected override bool CheckLosBeforeCastingOffensiveSpells => Properties.CHECK_LOS_BEFORE_AGGRO_FNF;

        public override void Think()
        {
            CheckProximityAggro();

            if (!CheckSpells(eCheckSpellType.Offensive))
                CheckSpells(eCheckSpellType.Defensive);
        }

        public override bool CheckProximityAggro()
        {
            // FnF turrets need to add all players and NPCs to their aggro list to be able to switch target randomly and effectively.
            CheckPlayerAggro();
            CheckNpcAggro();
            return HasAggro;
        }

        protected override void CheckPlayerAggro()
        {
            // Copy paste of 'base.CheckPlayerAggro()' except we add all players in range.
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (!CanAggroTarget(player))
                    continue;

                if (player.IsStealthed || player.Steed != null)
                    continue;

                if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Shade))
                    continue;

                if (Properties.CHECK_LOS_BEFORE_AGGRO_FNF)
                    SendLosCheckForAggro(player, player);
                else
                    AddToAggroList(player, 1);
            }
        }

        protected override void CheckNpcAggro()
        {
            // Copy paste of 'base.CheckNPCAggro()' except we add all NPCs in range.
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort) AggroRange))
            {
                if (!CanAggroTarget(npc))
                    continue;

                if (npc is GameTaxi or GameTrainingDummy)
                    continue;

                if (Properties.CHECK_LOS_BEFORE_AGGRO_FNF)
                {
                    if (npc.Brain is ControlledMobBrain theirControlledNpcBrain && theirControlledNpcBrain.GetPlayerOwner() is GamePlayer theirOwner)
                    {
                        SendLosCheckForAggro(theirOwner, npc);
                        continue;
                    }
                    else if (GetPlayerOwner() is GamePlayer ourOwner)
                    {
                        SendLosCheckForAggro(ourOwner, npc);
                        continue;
                    }
                }

                AddToAggroList(npc, 1);
            }
        }

        protected override bool CanAddToAggroListFromMultipleLosChecks => true;

        protected override bool ShouldBeIgnoredFromAggroList(GameLiving living)
        {
            // We always return true because we don't care about what `CleanUpAggroListAndGetHighestModifiedThreat` returns.
            // This is just an opportunity to build a filtered aggro list, to be used by `CalculateNextAttackTarget`.
            if (LivingHasEffect(living, ((TurretPet) Body).TurretSpell) ||
                living.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) ||
                base.ShouldBeIgnoredFromAggroList(living))
            {
                return true;
            }

            _filteredAggroList.Add(living);
            return true;
        }

        protected override GameLiving CleanUpAggroListAndGetHighestModifiedThreat()
        {
            _filteredAggroList.Clear();
            return base.CleanUpAggroListAndGetHighestModifiedThreat();
        }

        protected override GameLiving CalculateNextAttackTarget()
        {
            CleanUpAggroListAndGetHighestModifiedThreat();

            // Prioritize targets that don't already have our effect and aren't immune to it.
            // If there's none, allow them to be attacked again but only if our spell does damage.
            if (_filteredAggroList.Count > 0)
                return _filteredAggroList[Util.Random(_filteredAggroList.Count - 1)];
            else if ((Body as TurretPet).TurretSpell.Damage > 0)
            {
                List<GameLiving> tempAggroList = AggroList.Keys.ToList();

                if (tempAggroList.Count != 0)
                    return tempAggroList[Util.Random(tempAggroList.Count - 1)];
            }

            return null;
        }

        public override void UpdatePetWindow() { }
        public override void OnAttackedByEnemy(AttackData ad) { }
    }
}
