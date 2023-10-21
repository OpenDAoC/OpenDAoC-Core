using System.Collections.Generic;
using System.Linq;
using Core.AI.Brain;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Effects.Old;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;

namespace Core.GS.AI.Brains;

public class TurretFnfBrain : TurretBrain
{
    public TurretFnfBrain(GameLiving owner) : base(owner)
    {
        // Forced to aggressive, otherwise 'CheckProximityAggro()' won't be called.
        AggressionState = EAggressionState.Aggressive;
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
        foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
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
        foreach (GameNpc npc in Body.GetNPCsInRadius((ushort) AggroRange))
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
            return !LivingHasEffect(livingPair.Key, ((TurretPet)Body).TurretSpell) && EffectListService.GetEffectOnTarget(livingPair.Key, EEffect.SnareImmunity) == null;
        }
    }

    public override void UpdatePetWindow() { }
}