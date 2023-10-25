using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI;

public class GnatBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public GnatBrain() : base()
    {
    }

    public static bool spawnants = true;

    public override void Think()
    {
        if (Body.InCombat == true && Body.IsAlive && HasAggro)
        {
            if (Body.TargetObject != null)
            {
                if (Body.HealthPercent < 95 && spawnants == true)
                {
                    Spawn(); //spawn adds here
                    spawnants = false; //we check here to avoid spawning adds multiple times
                    foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
                    {
                        player.Out.SendMessage("Lets loose a high pitch whistle.", EChatType.CT_Say,
                            EChatLoc.CL_SystemWindow);
                    }
                }

                foreach (GameNpc mob_c in Body.GetNPCsInRadius(2000))
                {
                    if (mob_c != null)
                    {
                        if ((mob_c.Name.ToLower() == "fiery ant") && mob_c.IsAlive && mob_c.IsAvailable)
                        {
                            if (mob_c.Brain is GnatAntsBrain && mob_c.RespawnInterval == -1)
                            {
                                AddAggroListTo(mob_c.Brain as GnatAntsBrain); //add ants to boss agrro brain
                            }
                        }
                    }
                }
            }
        }

        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            spawnants = true; //reset so he can actually spawn adds if player decide to leave combat
        }

        base.Think();
    }

    public void Spawn() // We define here adds
    {
        for (int i = 0; i < 7; i++) //Spawn 8 ants
        {
            GnatAnts Add = new GnatAnts();
            Add.X = Body.X + Util.Random(50, 80);
            Add.Y = Body.Y + Util.Random(50, 80);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.IsWorthReward = false;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }
    }
}

public class GnatAntsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public GnatAntsBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 450;
    }

    public override void Think()
    {
        Body.IsWorthReward = false;
        base.Think();
    }
}