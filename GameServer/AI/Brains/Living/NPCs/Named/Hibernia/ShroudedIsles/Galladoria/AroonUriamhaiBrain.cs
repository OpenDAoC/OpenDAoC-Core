using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

#region Aroon the Uriamhai
public class AroonUriamhaiBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public AroonUriamhaiBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            AroonUriamhai.Aroon_slash = false;
            AroonUriamhai.Aroon_thrust = false;
            AroonUriamhai.Aroon_crush = false;
            AroonUriamhai.Aroon_body = false;
            AroonUriamhai.Aroon_cold = false;
            AroonUriamhai.Aroon_energy = false;
            AroonUriamhai.Aroon_heat = false;
            AroonUriamhai.Aroon_matter = false;
            AroonUriamhai.Aroon_spirit = false;

            CorpScaithBrain.switch_target = false;
            SpioradScaithBrain.switch_target = false;
            RopadhScaithBrain.switch_target = false;
            DamhnaScaithBrain.switch_target = false;
            FuinneamgScaithBrain.switch_target = false;
            BruScaithBrain.switch_target = false;
            FuarScaithBrain.switch_target = false;
            TaesScaithBrain.switch_target = false;
            ScorScaithBrain.switch_target = false;

            spawn_guardians = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is CorpScaithBrain || npc.Brain is SpioradScaithBrain ||
                        npc.Brain is RopadhScaithBrain || npc.Brain is DamhnaScaithBrain
                        || npc.Brain is FuinneamgScaithBrain || npc.Brain is BruScaithBrain ||
                        npc.Brain is FuarScaithBrain || npc.Brain is TaesScaithBrain
                        || npc.Brain is ScorScaithBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (Body.TargetObject != null && HasAggro)
        {
            RemoveAdds = false;
            if (spawn_guardians == false)
            {
                BroadcastMessage(String.Format(Body.Name + " summons the Scaths to do his bidding!"));
                SpawnGuardians();
                spawn_guardians = true;
            }
            if (Util.Chance(10))
            {
                Body.CastSpell(AroonRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.Think();
    }

    public static bool spawn_guardians = false;
    public void SpawnGuardians()
    {
        CorpScaith Add = new CorpScaith();
        Add.X = Body.X + Util.Random(-100, 150);
        Add.Y = Body.Y + Util.Random(-100, 150);
        Add.Z = Body.Z;
        Add.CurrentRegion = Body.CurrentRegion;
        Add.Heading = Body.Heading;
        Add.AddToWorld();

        SpioradScaith Add2 = new SpioradScaith();
        Add2.X = Body.X + Util.Random(-100, 150);
        Add2.Y = Body.Y + Util.Random(-100, 150);
        Add2.Z = Body.Z;
        Add2.CurrentRegion = Body.CurrentRegion;
        Add2.Heading = Body.Heading;
        Add2.AddToWorld();

        RopadhScaith Add3 = new RopadhScaith();
        Add3.X = Body.X + Util.Random(-100, 150);
        Add3.Y = Body.Y + Util.Random(-100, 150);
        Add3.Z = Body.Z;
        Add3.CurrentRegion = Body.CurrentRegion;
        Add3.Heading = Body.Heading;
        Add3.AddToWorld();

        DamhnaScaith Add4 = new DamhnaScaith();
        Add4.X = Body.X + Util.Random(-100, 150);
        Add4.Y = Body.Y + Util.Random(-100, 150);
        Add4.Z = Body.Z;
        Add4.CurrentRegion = Body.CurrentRegion;
        Add4.Heading = Body.Heading;
        Add4.AddToWorld();

        FuinneamgScaith Add5 = new FuinneamgScaith();
        Add5.X = Body.X + Util.Random(-100, 150);
        Add5.Y = Body.Y + Util.Random(-100, 150);
        Add5.Z = Body.Z;
        Add5.CurrentRegion = Body.CurrentRegion;
        Add5.Heading = Body.Heading;
        Add5.AddToWorld();

        BruScaith Add6 = new BruScaith();
        Add6.X = Body.X + Util.Random(-100, 150);
        Add6.Y = Body.Y + Util.Random(-100, 150);
        Add6.Z = Body.Z;
        Add6.CurrentRegion = Body.CurrentRegion;
        Add6.Heading = Body.Heading;
        Add6.AddToWorld();

        FuarScaith Add7 = new FuarScaith();
        Add7.X = Body.X + Util.Random(-100, 150);
        Add7.Y = Body.Y + Util.Random(-100, 150);
        Add7.Z = Body.Z;
        Add7.CurrentRegion = Body.CurrentRegion;
        Add7.Heading = Body.Heading;
        Add7.AddToWorld();

        TaesScaith Add8 = new TaesScaith();
        Add8.X = Body.X + Util.Random(-100, 150);
        Add8.Y = Body.Y + Util.Random(-100, 150);
        Add8.Z = Body.Z;
        Add8.CurrentRegion = Body.CurrentRegion;
        Add8.Heading = Body.Heading;
        Add8.AddToWorld();

        ScorScaith Add9 = new ScorScaith();
        Add9.X = Body.X + Util.Random(-100, 150);
        Add9.Y = Body.Y + Util.Random(-100, 150);
        Add9.Z = Body.Z;
        Add9.CurrentRegion = Body.CurrentRegion;
        Add9.Heading = Body.Heading;
        Add9.AddToWorld();
    }

    private Spell m_AroonRoot;
    private Spell AroonRoot
    {
        get
        {
            if (m_AroonRoot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 25;
                spell.ClientEffect = 5208;
                spell.Icon = 5208;
                spell.TooltipId = 5208;
                spell.Name = "Root";
                spell.Value = 99;
                spell.Duration = 60;
                spell.Radius = 1200;
                spell.Range = 1500;
                spell.SpellID = 117230;
                spell.Target = "Enemy";
                spell.Type = "SpeedDecrease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Matter;
                m_AroonRoot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AroonRoot);
            }

            return m_AroonRoot;
        }
    }
}
#endregion Aroon the Uriamhai

#region Aroon Guardians

#region Slash Guardian (Corp Scaith)
public class CorpScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public CorpScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }
    public static bool Message1 = false;
    public override void Think()
    {
        if(Message1==false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message1 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Slash Guardian (Corp Scaith)

#region Thrust Guardian (Spiorad Scaith)
public class SpioradScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SpioradScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;
    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();
    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }
        return 0;
    }
    public static bool Message2 = false;
    public override void Think()
    {
        if(AroonUriamhai.Aroon_slash && Message2==false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message2 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }
            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }
        base.Think();
    }
}
#endregion Thrust Guardian (Spiorad Scaith)

#region Crush Guardian (Ropadh Scaith)
public class RopadhScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public RopadhScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }
    public static bool Message3 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_thrust && Message3 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message3 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Crush Guardian (Ropadh Scaith)

#region Body Guardian (Damhna Scaith)
public class DamhnaScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public DamhnaScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }
    public static bool Message4 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_crush && Message4 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message4 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Body Guardian (Damhna Scaith)

#region Cold Guardian (Fuinneamg Scaith)
public class FuinneamgScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public FuinneamgScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }

    public static bool Message5 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_body && Message5 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message5 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Cold Guardian (Fuinneamg Scaith)

#region Energy Guardian (Bru Scaith)
public class BruScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BruScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }

    public static bool Message6 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_cold && Message6 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message6 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Energy Guardian (Bru Scaith)

#region Heat Guardian (Fuar Scaith)
public class FuarScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public FuarScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }

    public static bool Message7 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_energy && Message7 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message7 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Heat Guardian (Fuar Scaith)

#region Matter Guardian (Taes Scaith)
public class TaesScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public TaesScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }

    public static bool Message8 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_heat && Message8 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message8 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Matter Guardian (Taes Scaith)

#region Spirit Guardian (Scor Scaith)
public class ScorScaithBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ScorScaithBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
        ThinkInterval = 5000;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public static bool switch_target = false;
    private GamePlayer randomtarget = null;

    private GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

    public int RandomAttackTarget(EcsGameTimer timer)
    {
        //IList enemies = new ArrayList(AggroTable.Keys);
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
            AggroTable.Clear();
            AggroTable.Add(RandomTarget, 500);
            switch_target = false;
        }

        return 0;
    }

    public static bool Message9 = false;
    public override void Think()
    {
        if (AroonUriamhai.Aroon_heat && Message9 == false)
        {
            BroadcastMessage(String.Format(Body.Name + " eyes are glowing, indicating he's being controlled by Aroon."));
            Message9 = true;
        }
        if (Body.InCombat)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(player))
                        {
                            PlayersToAttack.Add(player);
                        }
                    }
                }
            }

            if (Util.Chance(15))
            {
                if (switch_target == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                    switch_target = true;
                }
            }
        }

        base.Think();
    }
}
#endregion Spirit Guardian (Scor Scaith)

#endregion Aroon Guardians