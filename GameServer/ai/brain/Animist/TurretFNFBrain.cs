/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Collections.Generic;
using System.Linq;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.AI.Brain
{
    public class TurretFNFBrain : TurretBrain
    {
        public TurretFNFBrain(GameLiving owner) : base(owner)
        {
            // Forced to aggressive, otherwise 'CheckProximityAggro()' won't be called.
            AggressionState = eAggressionState.Aggressive;
        }

        public override bool CheckProximityAggro()
        {
            // FnF turrets need to add all players and NPCs to their aggro list to be able to switch target randomnly and effectively.
            CheckPlayerAggro();
            CheckNPCAggro();

            return HasAggro;
        }

        protected override void CheckPlayerAggro()
        {
            // Copy paste of 'base.CheckPlayerAggro()' except we add all players in range.
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange, !Body.CurrentZone.IsDungeon))
            {
                if (!CanAggroTarget(player))
                    continue;

                if (player.IsStealthed || player.Steed != null)
                    continue;

                if (player.EffectList.GetOfType<NecromancerShadeEffect>() != null)
                    continue;

                if (GS.ServerProperties.Properties.FNF_TURRETS_REQUIRE_LOS_TO_AGGRO)
                    player.Out.SendCheckLOS(Body, player, new CheckLOSResponse(LosCheckForAggroCallback));
                else
                    AddToAggroList(player, 0);
            }
        }

        protected override void CheckNPCAggro()
        {
            // Copy paste of 'base.CheckNPCAggro()' except we add all NPCs in range.
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange, !Body.CurrentRegion.IsDungeon))
            {
                if (!CanAggroTarget(npc))
                    continue;

                if (npc is GameTaxi or GameTrainingDummy)
                    continue;

                if (GS.ServerProperties.Properties.FNF_TURRETS_REQUIRE_LOS_TO_AGGRO)
                {
                    if (npc.Brain is ControlledNpcBrain theirControlledNpcBrain && theirControlledNpcBrain.GetPlayerOwner() is GamePlayer theirOwner)
                    {
                        theirOwner.Out.SendCheckLOS(Body, npc, new CheckLOSResponse(LosCheckForAggroCallback));
                        continue;
                    }
                    else if (this is ControlledNpcBrain ourControlledNpcBrain && ourControlledNpcBrain.GetPlayerOwner() is GamePlayer ourOwner)
                    {
                        ourOwner.Out.SendCheckLOS(Body, npc, new CheckLOSResponse(LosCheckForAggroCallback));
                        continue;
                    }
                }

                AddToAggroList(npc, 0);
            }
        }

        protected override void LosCheckForAggroCallback(GamePlayer player, ushort response, ushort targetOID)
        {
            // Copy paste of 'base.LosCheckForAggroCallback()' except we don't care if we already have aggro.
            if (targetOID == 0)
                return;

            if ((response & 0x100) == 0x100)
            {
                GameObject gameObject = Body.CurrentRegion.GetObject(targetOID);

                if (gameObject is GameLiving gameLiving)
                    AddToAggroList(gameLiving, 0);
            }
        }

        protected override GameLiving CalculateNextAttackTarget()
        {
            Dictionary<GameLiving, long> tempAggroList = FilterOutInvalidLivingsFromAggroList();
            List<GameLiving> livingsWithoutEffect = tempAggroList.Where(IsLivingWithoutEffect).Select(x => x.Key).ToList();

            // Prioritize targets that don't already have our effect and aren't immune to it.
            // If there's none, allow them to be attacked again but only if our spell does damage.
            if (livingsWithoutEffect.Any())
                return livingsWithoutEffect[Util.Random(livingsWithoutEffect.Count - 1)];
            else if (tempAggroList.Count > 0 && ((TurretPet)Body).TurretSpell.Damage > 0)
                return tempAggroList.ElementAt(Util.Random(tempAggroList.Count - 1)).Key;

            return null;

            bool IsLivingWithoutEffect(KeyValuePair<GameLiving, long> livingPair)
            {
                return !LivingHasEffect(livingPair.Key, ((TurretPet)Body).TurretSpell) && EffectListService.GetEffectOnTarget(livingPair.Key, eEffect.SnareImmunity) == null;
            }
        }

        public override void UpdatePetWindow() { }
    }
}