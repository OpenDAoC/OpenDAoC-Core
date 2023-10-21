using Core.Database;
using Core.GS;

namespace Core.AI.Brain;

#region Soul Reckoner
public class SoulReckonerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SoulReckonerBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        CanBAF = false;
    }

    public static bool InRoom = false;

    public void AwayFromRoom()
    {
        Point3D room_radius = new Point3D();
        room_radius.X = 28472;
        room_radius.Y = 35857;
        room_radius.Z = 15370; //room middle point

        if (Body.CurrentRegionID == 60)
        {
            if (Body.IsWithinRadius(room_radius, 900)) //if is in room
            {
                InRoom = true;
            }
            else //is out of room
            {
                InRoom = false;
            }
        }
        else
        {
            Point3D spawnpoint = new Point3D();
            spawnpoint.X = Body.SpawnPoint.X;
            spawnpoint.Y = Body.SpawnPoint.Y;
            spawnpoint.Z = Body.SpawnPoint.Z;
            if (Body.IsWithinRadius(spawnpoint, 900)) //if is in radius of spawnpoint
            {
                InRoom = true;
            }
            else //is out of room
            {
                InRoom = false;
            }
        }
    }
    public static bool BafMobs = false;

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            BafMobs = false;
            Spawn_Souls = false;
        }

        if (HasAggro && Body.TargetObject != null)
        {
            AwayFromRoom();
            if (Util.Chance(50))
            {
                Body.TurnTo(Body.TargetObject);
                Body.CastSpell(Reckoner_Lifetap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
            if(!Spawn_Souls)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnSouls), Util.Random(10000, 15000));
                Spawn_Souls = true;
            }
            if (BafMobs == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "SoulReckonerBaf" && npc.Brain is ReckonedSoulBrain brain)
                        {
                            AddAggroListTo(brain); // add to aggro mobs with CryptLordBaf PackageID
                            BafMobs = true;
                        }
                    }
                }
            }
        }
        base.Think();
    }
    #region Spawn Soul
    public static bool Spawn_Souls = false;
    private int SpawnSouls(EcsGameTimer timer)
    {
        if (Body.IsAlive && HasAggro)
        {
            for (int i = 0; i < Util.Random(1, 2); i++)
            {
                ReckonedSoul Add = new ReckonedSoul();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetRespawnSouls), Util.Random(60000, 70000));
        return 0;
    }
    private int ResetRespawnSouls(EcsGameTimer timer)
    {
        Spawn_Souls = false;
        return 0;
    }
    #endregion

    #region Spell
    public Spell m_Reckoner_Lifetap;
    public Spell Reckoner_Lifetap
    {
        get
        {
            if (m_Reckoner_Lifetap == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = Util.Random(8,12);
                spell.ClientEffect = 9191;
                spell.Icon = 710;
                spell.Damage = 650;
                spell.Name = "Drain Life Essence";
                spell.Range = 1800;
                spell.SpellID = 11733;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.MoveCast = true;
                spell.Uninterruptible = true;
                spell.DamageType = (int) EDamageType.Body; //Body DMG Type
                m_Reckoner_Lifetap = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Reckoner_Lifetap);
            }
            return m_Reckoner_Lifetap;
        }
    }
    #endregion
}
#endregion Soul Reckoner

#region Reckoned Soul
public class ReckonedSoulBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ReckonedSoulBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        CanBAF = false;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Reckoned Soul