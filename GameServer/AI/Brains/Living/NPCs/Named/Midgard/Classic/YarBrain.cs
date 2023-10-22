using System;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.AI.Brains;

#region Yar
public class YarBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public YarBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }

    private bool _isSpawning = true;
    
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    public override void Think()
    {
        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            Body.Health = Body.MaxHealth;
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc.Brain is YarAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }

            _isSpawning = true;
        }

        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
            {
                if (npc.Name.ToLower().Contains("drakulv"))
                {
                    foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                    {
                        npc.StartAttack(player);
                    }
                }
            }
        }
        base.Think();
    }

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);

        if (e == GameObjectEvent.TakeDamage && Body.HealthPercent <= 30)
        {
            if (_isSpawning)
            {
                SpawnAdds();
                _isSpawning = false;
            }
        }

        if (e == GameLivingEvent.Dying)
        {
            _isSpawning = true;
        }
    }

    public void SpawnAdds()
    {
        for (int i = 0; i < 5; i++)
        {
            switch (Util.Random(1, 3))
            {
                case 1:
                    YarAdd add = new YarAdd();
                    add.X = Body.X + Util.Random(-50, 80);
                    add.Y = Body.Y + Util.Random(-50, 80);
                    add.Z = Body.Z;
                    add.CurrentRegion = Body.CurrentRegion;
                    add.Heading = Body.Heading;
                    add.AddToWorld();
                    break;
                case 2:
                    YarAdd2 add2 = new YarAdd2();
                    add2.X = Body.X + Util.Random(-50, 80);
                    add2.Y = Body.Y + Util.Random(-50, 80);
                    add2.Z = Body.Z;
                    add2.CurrentRegion = Body.CurrentRegion;
                    add2.Heading = Body.Heading;
                    add2.AddToWorld();
                    break;
                case 3:
                    YarAdd3 add3 = new YarAdd3();
                    add3.X = Body.X + Util.Random(-50, 80);
                    add3.Y = Body.Y + Util.Random(-50, 80);
                    add3.Z = Body.Z;
                    add3.CurrentRegion = Body.CurrentRegion;
                    add3.Heading = Body.Heading;
                    add3.AddToWorld();
                    break;
                default:
                    break;
            }
            
        }
    }
}
#endregion Yar

#region Yar adds
public class YarAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public YarAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Yar adds