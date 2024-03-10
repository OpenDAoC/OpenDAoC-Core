using System.Collections.Generic;
using System.Linq;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain
{
    public class TurretFNFBrain : TurretBrain
    {
        private List<GameLiving> _filteredAggroList = new();

        public TurretFNFBrain(GameLiving owner) : base(owner)
        {
            // Forced to aggressive, otherwise 'CheckProximityAggro()' won't be called.
            AggressionState = eAggressionState.Aggressive;
        }

        public override bool CheckProximityAggro()
        {
            // FnF turrets need to add all players and NPCs to their aggro list to be able to switch target randomly and effectively.
            CheckPlayerAggro();
            CheckNPCAggro();
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

                if (GS.ServerProperties.Properties.FNF_TURRETS_REQUIRE_LOS_TO_AGGRO)
                    player.Out.SendCheckLos(Body, player, new CheckLosResponse(LosCheckForAggroCallback));
                else
                    AddToAggroList(player, 0);
            }
        }

        protected override void CheckNPCAggro()
        {
            // Copy paste of 'base.CheckNPCAggro()' except we add all NPCs in range.
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort) AggroRange))
            {
                if (!CanAggroTarget(npc))
                    continue;

                if (npc is GameTaxi or GameTrainingDummy)
                    continue;

                if (GS.ServerProperties.Properties.FNF_TURRETS_REQUIRE_LOS_TO_AGGRO)
                {
                    if (npc.Brain is ControlledNpcBrain theirControlledNpcBrain && theirControlledNpcBrain.GetPlayerOwner() is GamePlayer theirOwner)
                    {
                        theirOwner.Out.SendCheckLos(Body, npc, new CheckLosResponse(LosCheckForAggroCallback));
                        continue;
                    }
                    else if (this is ControlledNpcBrain ourControlledNpcBrain && ourControlledNpcBrain.GetPlayerOwner() is GamePlayer ourOwner)
                    {
                        ourOwner.Out.SendCheckLos(Body, npc, new CheckLosResponse(LosCheckForAggroCallback));
                        continue;
                    }
                }

                AddToAggroList(npc, 0);
            }
        }

        protected override void LosCheckForAggroCallback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            // Copy paste of 'base.LosCheckForAggroCallback()' except we don't care if we already have aggro.
            if (response is eLosCheckResponse.TRUE)
            {
                GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

                if (gameObject is GameLiving gameLiving)
                    AddToAggroList(gameLiving, 0);
            }
        }

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
            if (_filteredAggroList.Any())
                return _filteredAggroList[Util.Random(_filteredAggroList.Count - 1)];
            else if (((TurretPet) Body).TurretSpell.Damage > 0)
            {
                List<GameLiving> tempAggroList = AggroList.Keys.ToList();

                if (tempAggroList.Any())
                    return tempAggroList[Util.Random(tempAggroList.Count - 1)];
            }

            return null;
        }

        public override void UpdatePetWindow() { }
    }
}