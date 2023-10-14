using DOL.AI.Brain;
using DOL.GS;

public class MlBrain : RealmGuardBrain
{
    public MlBrain() : base() { }

    public override int AggroRange
    {
        get { return 400; }
    }
    protected override void CheckNPCAggro()
    {
        //Check if we are already attacking, return if yes
        if (Body.attackComponent.AttackState)
            return;

        foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
        {
            if (AggroTable.ContainsKey(npc))
                continue; // add only new npcs
            if ((npc.Flags & GameNPC.eFlags.FLYING) != 0)
                continue; // let's not try to attack flying mobs
            if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
                continue;
            if (!npc.IsWithinRadius(Body, AggroRange))
                continue;

            if (!(npc.Brain is IControlledBrain || npc is GameGuard))
                continue;

            AddToAggroList(npc, npc.Level << 1);
            return;
        }
    }
}