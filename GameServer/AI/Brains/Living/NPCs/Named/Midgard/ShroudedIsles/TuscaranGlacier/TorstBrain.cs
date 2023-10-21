using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Torst
public class TorstBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public TorstBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 2000;
    }

    public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();
    public static bool point1check = false;
    public static bool point2check = false;
    public static bool point3check = false;
    public static bool point4check = false;
    public static bool point5check = false;
    public static bool point6check = false;
    public static bool walkback = false;

    #region Torst Flying Path
    public void TorstFlyingPath()
    {
        Point3D point1 = new Point3D();
        point1.X = 51166;
        point1.Y = 37442;
        point1.Z = 17331;
        Point3D point2 = new Point3D();
        point2.X = 53201;
        point2.Y = 39956;
        point2.Z = 16314;
        Point3D point3 = new Point3D();
        point3.X = 55178;
        point3.Y = 38616;
        point3.Z = 17901;
        Point3D point4 = new Point3D();
        point4.X = 54852;
        point4.Y = 36185;
        point4.Z = 17859;
        Point3D point5 = new Point3D();
        point5.X = 53701;
        point5.Y = 35635;
        point5.Z = 17859;
        Point3D point6 = new Point3D();
        point6.X = 52118;
        point6.Y = 36114;
        point6.Z = 17265;
        Point3D spawn = new Point3D();
        spawn.X = 50897;
        spawn.Y = 36006;
        spawn.Z = 16659;

        if (!Body.InCombat && !HasAggro)
        {
            if (!Body.IsWithinRadius(point1, 30) && point1check == false)
            {
                Body.WalkTo(point1, 200);
            }
            else
            {
                point1check = true;
                walkback = false;
                if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
                {
                    Body.WalkTo(point2, 200);
                }
                else
                {
                    point2check = true;
                    if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
                        point3check == false)
                    {
                        Body.WalkTo(point3, 200);
                    }
                    else
                    {
                        point3check = true;
                        if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
                            point3check == true && point4check == false)
                        {
                            Body.WalkTo(point4, 200);
                        }
                        else
                        {
                            point4check = true;
                            if (!Body.IsWithinRadius(point5, 30) && point1check == true &&
                                point2check == true && point3check == true && point4check == true &&
                                point5check == false)
                            {
                                Body.WalkTo(point5, 200);
                            }
                            else
                            {
                                point5check = true;
                                if (!Body.IsWithinRadius(point6, 30) && point1check == true &&
                                    point2check == true && point3check == true && point4check == true &&
                                    point5check == true && point6check == false)
                                {
                                    Body.WalkTo(point6, 200);
                                }
                                else
                                {
                                    point6check = true;
                                    if (!Body.IsWithinRadius(spawn, 30) && point1check == true &&
                                        point2check == true && point3check == true && point4check == true &&
                                        point5check == true && point6check == true && walkback == false)
                                    {
                                        Body.WalkTo(spawn, 200);
                                    }
                                    else
                                    {
                                        walkback = true;
                                        point1check = false;
                                        point2check = false;
                                        point3check = false;
                                        point4check = false;
                                        point5check = false;
                                        point6check = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    private bool SpawnEddies = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        TorstFlyingPath();
        if (CheckProximityAggro() && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange) && Body.InCombat)
        {
            Body.Flags = 0; //dont fly
        }

        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            Body.Flags = ENpcFlags.FLYING; //fly
            SpawnEddies = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is TorstEddiesBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
        }

        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }

        if (HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if (!SpawnEddies)
            {
                CreateEddies();
                SpawnEddies = true;
            }
            foreach (GamePlayer gamePlayer in Body.GetPlayersInRadius(1500))
            {
                if (gamePlayer != null && gamePlayer.IsAlive && gamePlayer.Client.Account.PrivLevel == 1)
                {
                    if (!PlayersToAttack.Contains(gamePlayer))
                        PlayersToAttack.Add(gamePlayer);
                }
            }

            PickNotRottedTarget();

            if (Util.Chance(10))
                Body.CastSpell(TorstRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.Think();
    }
    private void CreateEddies()
    {
        BroadcastMessage(String.Format("{0}'s powerful wings stir swirling eddies of air that threaten to freeze anyone caught in their wake!",Body.Name));
        for (int i = 0; i < 5; i++)
        {
            TorstEddies add = new TorstEddies();
            add.X = Body.X + Util.Random(-200, 200);
            add.Y = Body.Y + Util.Random(-200, 200);
            add.Z = Body.Z;
            add.Heading = Body.Heading;
            add.CurrentRegion = Body.CurrentRegion;
            add.AddToWorld();
        }
    }

    public GameLiving randomtarget = null;
    public GameLiving RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    public void PickNotRottedTarget()
    {
        if (PlayersToAttack.Count == 0)
        {
            //do nothing
        }
        else
        {
            if (Body.TargetObject != null)
            {
                GameLiving target = Body.TargetObject as GameLiving; //mob target
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)]; //mob next random target
                if (target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff)) //if target got root
                {
                    Body.StopAttack();
                    AggroTable.Clear(); //clear aggro list
                    if (RandomTarget != null && RandomTarget.IsAlive)
                    {
                        AggroTable.Add(RandomTarget, 50); //add to aggro list our new random target
                        Body.StartAttack(RandomTarget);
                    }
                }
            }
        }
    }
    #region Spell root
    private Spell m_TorstRoot;
    private Spell TorstRoot
    {
        get
        {
            if (m_TorstRoot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 30;
                spell.ClientEffect = 277;
                spell.Icon = 277;
                spell.Duration = 60;
                spell.Value = 99;
                spell.Name = "Torst Root";
                spell.TooltipId = 277;
                spell.SpellID = 11742;
                spell.Target = "Enemy";
                spell.Type = "SpeedDecrease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Cold;
                m_TorstRoot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TorstRoot);
            }
            return m_TorstRoot;
        }
    }
    #endregion
}
#endregion Torst

#region Torst adds
public class TorstEddiesBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public TorstEddiesBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    private protected bool Point1check = false;
    private protected bool Point2check = false;
    bool SetNpcTarget = false;

    private protected static GameNpc trostnpc = null;
    private protected static GameNpc TrostNpc
    {
        get { return trostnpc; }
        set { trostnpc = value; }
    }
    public override void Think()
    {
        Body.CurrentSpeed = 300;
        if (!SetNpcTarget)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.Brain is TorstBrain)
                {
                    trostnpc = npc;
                    SetNpcTarget = true;
                }
            }
        }

        if (TrostNpc != null && TrostNpc.IsAlive)
        {
            Point3D oldPoint = new Point3D(TrostNpc.X + Util.Random(-200, 200), TrostNpc.Y + Util.Random(-200, 200), TrostNpc.Z + Util.Random(0, 100));
            Point3D newPoint = new Point3D(TrostNpc.X + Util.Random(-200, 200), TrostNpc.Y + Util.Random(-200, 200), TrostNpc.Z + Util.Random(0, 100));
            if (!Body.IsWithinRadius(oldPoint, 20) && !Point1check)
            {
                Body.WalkTo(oldPoint, 300);
            }
            else
            {
                Point1check = true;
                Point2check = false;
                if (!Body.IsWithinRadius(newPoint, 20) && Point1check && !Point2check)
                {
                    Body.WalkTo(newPoint, 300);
                }
                else
                {
                    Point2check = true;
                    Point1check = false;
                }
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            Body.CastSpell(ColdGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
        }
        base.Think();
    }
    private Spell m_ColdGroundDD;
    private Spell ColdGroundDD
    {
        get
        {
            if (m_ColdGroundDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = Util.Random(5,12);
                spell.ClientEffect = 161;
                spell.Icon = 161;
                spell.TooltipId = 368;
                spell.Name = "Cold Snap";
                spell.Damage = 110;
                spell.Range = 200;
                spell.Radius = 300;
                spell.SpellID = 11926;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_ColdGroundDD = new Spell(spell, 60);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ColdGroundDD);
            }
            return m_ColdGroundDD;
        }
    }
}
#endregion Torst adds