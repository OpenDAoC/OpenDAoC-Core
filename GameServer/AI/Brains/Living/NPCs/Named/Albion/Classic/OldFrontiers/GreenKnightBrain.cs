using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

#region Green Knight
public class GreenKnightBrain : EpicBossBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GreenKnightBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
    }
    public override void AttackMostWanted() // mob doesnt attack
    {
        if (IsWalking)
            return;
        else
            base.AttackMostWanted();
    }
    public override void OnAttackedByEnemy(AttackData ad) //another check to not attack enemys
    {
        if (IsWalking)
            return;
        else
            base.OnAttackedByEnemy(ad);
    }
    #region GK pick random healer
    public static GamePlayer randomtarget = null;
    public static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    List<GamePlayer> healer = new List<GamePlayer>();
    public int PickHeal(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            if (Body.InCombat && Body.IsAlive && HasAggro)
            {
                if (Body.TargetObject != null)
                {
                    foreach (GamePlayer ppl in Body.GetPlayersInRadius(2500))
                    {
                        if (ppl != null)
                        {
                            if (ppl.IsAlive && ppl.Client.Account.PrivLevel == 1)
                            {
                                //cleric, bard, healer, warden, friar, druid, mentalist, shaman
                                if (ppl.PlayerClass.ID is 6 or 48 or 26 or 46 or 10 or 47 or 42 or 28)
                                {
                                    if (!healer.Contains(ppl))
                                        healer.Add(ppl);
                                }
                            }
                        }
                    }
                    if (healer.Count > 0)
                    {
                        GamePlayer Target =(GamePlayer) healer[Util.Random(0, healer.Count - 1)]; //pick random target from list
                        RandomTarget = Target; //set random target to static RandomTarget
                        if (RandomTarget != null) //check if it's not null
                        {
                            ClearAggroList(); //clear aggro list or it may still stick to current target
                            AddToAggroList(RandomTarget, 550); //set that target big aggro so boss will attack him
                            Body.StartAttack(RandomTarget); //attack target
                        }
                        RandomTarget = null; //reset static ranmdomtarget to null
                        Pick_healer = false; //reset flag
                    }
                }
            }
        }
        return 0;
    }
    #endregion
    #region GK check flags & strings & PortPoints list
    public static bool Pick_healer = false;
    public static bool IsSpawningTrees = false;
    public static bool walk1 = false;
    public static bool walk2 = false;
    public static bool walk3 = false;
    public static bool walk4 = false;
    public static bool walk5 = false;
    public static bool walk6 = false;
    public static bool walk7 = false;
    public static bool walk8 = false;
    public static bool walk9 = false;

    public static bool IsWalking = false;
    public List<string> PortPoints = new List<string>();
    public static string string1 = "point1";
    public static string string2 = "point2";
    public static string string3 = "point3";
    public static string string4 = "point4";
    #endregion
    #region GK Teleport/Walk method
    public static bool PickPortPoint = false;
    public int GkTeleport(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            Point3D point1 = new Point3D(593193, 416481, 4833);
            Point3D point2 = new Point3D(593256, 420780, 5050);
            Point3D point3 = new Point3D(596053, 420171, 4918);
            Point3D point4 = new Point3D(590876, 418052, 4942);

            if (!PortPoints.Contains(string1) && CanHeal1 == false)
               PortPoints.Add(string1);
            if(!PortPoints.Contains(string2) && CanHeal2 == false)
               PortPoints.Add(string2);
            if(!PortPoints.Contains(string3) && CanHeal3 == false)
                PortPoints.Add(string3);  
            if(!PortPoints.Contains(string4) && CanHeal4 == false)
                PortPoints.Add(string4);

            if (PortPoints.Count > 0)
            {
                if (PickPortPoint == false)
                {
                    string stg = PortPoints[Util.Random(0, PortPoints.Count - 1)];
                    {
                        switch (stg)
                        {
                            case "point1":
                                if (!Body.IsWithinRadius(point1, 50))
                                {
                                    Body.StopAttack();
                                    Body.WalkTo(point1, 400);
                                    IsWalking = true;
                                }
                                break;
                            case "point2":
                                if (!Body.IsWithinRadius(point2, 50))
                                {
                                    Body.StopAttack();
                                    Body.WalkTo(point2, 400);
                                    IsWalking = true;
                                }
                                break;
                            case "point3":
                                if (!Body.IsWithinRadius(point3, 50))
                                {
                                    Body.StopAttack();
                                    Body.WalkTo(point3, 400);
                                    IsWalking = true;
                                }
                                break;
                            case "point4":
                                if (!Body.IsWithinRadius(point4, 50))
                                {
                                    Body.StopAttack();
                                    Body.WalkTo(point4, 400);
                                    IsWalking = true;
                                }
                                break;
                        }
                    }
                    PickPortPoint = true;
                }
            }
        }
        return 0;
    }
    #endregion
    public static bool CanHeal1 = false;
    public static bool CanHeal2 = false;
    public static bool CanHeal3 = false;
    public static bool CanHeal4 = false;
    public int StartHeal(EcsGameTimer timer)
    {
        IsWalking = false;
        return 0;
    }
    public override void Think()
    {
        Point3D point1 = new Point3D(593193, 416481, 4833);
        Point3D point2 = new Point3D(593256, 420780, 5050);
        Point3D point3 = new Point3D(596053, 420171, 4918);
        Point3D point4 = new Point3D(590876, 418052, 4942);

        if (Body.IsAlive && Body.HealthPercent < 25) //mobs slow down when they got low hp
            Body.CurrentSpeed = 400;

        if (Body.IsAlive)
        {
            #region GK walking and healing
            if (Body.IsWithinRadius(point1, 40) && CanHeal1 == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(StartHeal), 4000);
                Body.TargetObject = Body;
                if (Util.Chance(100))
                    Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                if (PortPoints.Contains(string1))
                    PortPoints.Remove(string1);

                PickPortPoint = false;
                CanHeal4 = false;
                CanHeal2 = false;
                CanHeal3 = false;
                CanHeal1 = true;
            }
            if (Body.IsWithinRadius(point2, 40) && CanHeal2 == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(StartHeal), 4000);
                Body.TargetObject = Body;
                if (Util.Chance(100))
                    Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                if (PortPoints.Contains(string2))
                    PortPoints.Remove(string2);
                PickPortPoint = false;
                CanHeal1 = false;
                CanHeal4 = false;
                CanHeal3 = false;
                CanHeal2 = true;
            }
            if (Body.IsWithinRadius(point3, 40) && CanHeal3 == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(StartHeal), 4000);
                Body.TargetObject = Body;
                if (Util.Chance(100))
                    Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                if (PortPoints.Contains(string3))
                    PortPoints.Remove(string3);
                PickPortPoint = false;
                CanHeal1 = false;
                CanHeal2 = false;
                CanHeal4 = false;
                CanHeal3 = true;                  
            }
            if (Body.IsWithinRadius(point4, 40) && CanHeal4 == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(StartHeal), 4000);
                Body.TargetObject = Body;
                if(Util.Chance(100))
                    Body.CastSpell(GreenKnightHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                if (PortPoints.Contains(string4))
                    PortPoints.Remove(string4);
                PickPortPoint = false;
                CanHeal1 = false;
                CanHeal2 = false;
                CanHeal3 = false;
                CanHeal4 = true;
            }
            if (Body.HealthPercent <= 90 && walk1 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk1 = true;
            }
            else if (Body.HealthPercent <= 80 && walk2 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk2 = true;
            }
            else if (Body.HealthPercent <= 70 && walk3 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk3 = true;
            }
            else if (Body.HealthPercent <= 60 && walk4 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk4 = true;
            }
            else if (Body.HealthPercent <= 50 && walk5 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk5 = true;
            }
            else if (Body.HealthPercent <= 40 && walk6 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk6 = true;
            }
            else if (Body.HealthPercent <= 30 && walk7 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk7 = true;
            }
            else if (Body.HealthPercent <= 20 && walk8 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk8 = true;
            }
            else if (Body.HealthPercent <= 10 && walk9 == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GkTeleport), 1000);
                walk9 = true;
            }
            #endregion
            if (Pick_healer == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickHeal), Util.Random(40000, 60000)); //40s-60s will try pick heal class
                Pick_healer = true;
            }
            if (IsSpawningTrees == false && HasAggro)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnTrees),Util.Random(25000, 35000)); //25s-35s will spawn trees
                IsSpawningTrees = true;
            }
            if (Body.TargetObject != null && HasAggro)
                Body.styleComponent.NextCombatStyle = GreenKnight.taunt;
        }
        //we reset him so he return to his orginal peace flag and max health and reseting pickheal phases
        if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
        {
            Body.Flags = ENpcFlags.PEACE;
            Body.Health = Body.MaxHealth;
            Body.ReturnToSpawnPoint(400); //move boss back to his spawn point
            foreach (GameNpc npc in Body.GetNPCsInRadius(6500))
            {
                if (npc.Brain is GreenKnightTreeBrain)
                    npc.RemoveFromWorld();//remove all trees
            }
            walk1 = false;
            walk2 = false;
            walk3 = false;
            walk4 = false;
            walk5 = false;
            Pick_healer = false;
            walk6 = false;
            IsSpawningTrees = false;
            walk7 = false;
            IsWalking = false;
            walk8 = false;
            walk9 = false;
            PickPortPoint = false;
            CanHeal1 = false;
            CanHeal2 = false;
            CanHeal3 = false;
            CanHeal4 = false;
        }
        base.Think();
    }

    public int SpawnTrees(EcsGameTimer timer) // We define here adds
    {
        if (Body.IsAlive && Body.InCombat && HasAggro)
        {
            //spawning each tree in radius of 4000 on every player
            List<GamePlayer> player = new List<GamePlayer>();
            foreach (GamePlayer ppl in Body.GetPlayersInRadius(4000))
            {
                player.Add(ppl);

                if (ppl.IsAlive)
                {
                    for (int i = 0; i <= player.Count - 1; i++)
                    {
                        GreenKnightTree add = new GreenKnightTree();
                        add.X = ppl.X;
                        add.Y = ppl.Y;
                        add.Z = ppl.Z;
                        add.CurrentRegion = Body.CurrentRegion;
                        add.Heading = ppl.Heading;
                        add.AddToWorld();
                    }
                }
                player.Clear();
            }
            IsSpawningTrees = false;
        }
        return 0;
    }
    private Spell m_GreenKnightHeal;
    private Spell GreenKnightHeal
    {
        get
        {
            if (m_GreenKnightHeal == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 0;
                spell.ClientEffect = 4811;
                spell.Icon = 4811;
                spell.TooltipId = 4811;
                spell.Value = 400;
                spell.Name = "Heal";
                spell.Range = 1500;
                spell.SpellID = 11889;
                spell.Target = "Self";
                spell.Type = "Heal";
                m_GreenKnightHeal = new Spell(spell, 70);
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GreenKnightHeal);
            }
            return m_GreenKnightHeal;
        }
    }
}
#endregion Green Knight

#region Green Knight Trees
public class GreenKnightTreeBrain : StandardMobBrain
{
    public GreenKnightTreeBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 1500;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);

        base.Think();
    }
}
#endregion Green Knight Trees