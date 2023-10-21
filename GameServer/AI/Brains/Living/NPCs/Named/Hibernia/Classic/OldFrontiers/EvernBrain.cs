using Core.AI.Brain;
using Core.Database.Tables;

namespace Core.GS.AI.Brains;

#region Evern
public class EvernBrain : EpicBossBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public EvernBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public static bool spawnfairy = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            Body.Health = Body.MaxHealth;
            spawnfairy = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4500))
                {
                    if (npc == null) break;
                    if (npc.Brain is EvernFairyBrain)
                    {
                        if (npc.RespawnInterval == -1)
                            npc.Die(npc); //we kill all fairys if boss reset
                    }
                }
                RemoveAdds = true;
            }
        }
        if (Body.IsAlive && HasAggro)
        {
            RemoveAdds = false;
            if (Body.TargetObject != null)
            {
                if (Body.HealthPercent < 100)
                {
                    if (spawnfairy == false)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DoSpawn), Util.Random(10000, 20000));
                        spawnfairy = true;
                    }
                }
            }
        }
        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
        {
            Body.Health = Body.MaxHealth;

            foreach (GameNpc npc in Body.GetNPCsInRadius(4500))
            {
                if (npc == null) break;
                if (npc.Brain is EvernFairyBrain)
                {
                    if (npc.RespawnInterval == -1)
                        npc.Die(npc); //we kill all fairys if boss reset
                }
            }
        }
        base.Think();
    }
    private int DoSpawn(EcsGameTimer timer)
    {
        Spawn();
        spawnfairy = false;
        return 0;
    }
    public void Spawn() // We define here adds
    {
        for (int i = 0; i < Util.Random(2, 5); i++)
        {
            EvernFairy Add = new EvernFairy();
            Add.X = 429764 + Util.Random(-100, 100);
            Add.Y = 380398 + Util.Random(-100, 100);
            Add.Z = 2726;
            Add.CurrentRegionID = 200;
            Add.Heading = 3889;
            Add.AddToWorld();
        }
    }
}
#endregion Evern

#region Evern Fairies
public class EvernFairyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public EvernFairyBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 0;
    }
    private protected void IsAtPillar(IPoint3D target)
    {
        Body.MaxSpeedBase = 0;
        Body.MoveTo(Body.CurrentRegionID, target.X, target.Y, target.Z, Body.Heading);
        Body.CancelReturnToSpawnPoint();

        foreach(GameNpc evern in Body.GetNPCsInRadius(2500))
        {
            if(evern != null)
            {
                if(evern.IsAlive && evern.Brain is EvernBrain && evern.HealthPercent < 100)
                {
                    Body.TargetObject = evern;
                    Body.TurnTo(evern);
                    Body.CastSpell(Fairy_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                }
            }
        }
    }
    public override void Think()
    {
        Point3D point1 = new Point3D(430333, 379905, 2463);
        Point3D point2 = new Point3D(429814, 379895, 2480);
        Point3D point3 = new Point3D(429309, 379894, 2454);
        Point3D point4 = new Point3D(430852, 380150, 2444);
        Point3D point5 = new Point3D(428801, 380156, 2428);
        Point3D point6 = new Point3D(430854, 380680, 2472);
        Point3D point7 = new Point3D(429186, 380418, 2478);
        Point3D point8 = new Point3D(430462, 380411, 2443);
        Point3D point9 = new Point3D(430468, 430468, 2474);
        Point3D point10 = new Point3D(429057, 380920, 2452);

        if (Body.IsAlive)
        {
            #region PickRandomLandSpot
            switch (Util.Random(1, 10))
            {
                case 1: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point1, 80); break;
                case 2: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point2, 80); break;
                case 3: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point3, 80); break;
                case 4: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point4, 80); break;
                case 5: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point5, 80); break;
                case 6: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point6, 80); break;
                case 7: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point7, 80); break;
                case 8: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point8, 80); break;
                case 9: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point9, 80); break;
                case 10: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point10, 80); break;
            }

            if (Body.IsWithinRadius(point1, 15)) IsAtPillar(point1);
            if (Body.IsWithinRadius(point2, 15)) IsAtPillar(point1);
            if (Body.IsWithinRadius(point3, 15)) IsAtPillar(point3);
            if (Body.IsWithinRadius(point4, 15)) IsAtPillar(point4);
            if (Body.IsWithinRadius(point5, 15)) IsAtPillar(point5);
            if (Body.IsWithinRadius(point6, 15)) IsAtPillar(point6);
            if (Body.IsWithinRadius(point7, 15)) IsAtPillar(point7);
            if (Body.IsWithinRadius(point8, 15)) IsAtPillar(point8);
            if (Body.IsWithinRadius(point9, 15)) IsAtPillar(point9);
            if (Body.IsWithinRadius(point10, 15)) IsAtPillar(point10);
            #endregion
        }
        base.Think();
    }
    #region Spells: Fairy Heal
    private Spell m_Fairy_Heal;
    private Spell Fairy_Heal
    {
        get
        {
            if (m_Fairy_Heal == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 0;
                spell.ClientEffect = 4858;
                spell.Icon = 4858;
                spell.TooltipId = 4858;
                spell.Value = 1000;
                spell.Name = "Heal";
                spell.Range = 2500;
                spell.SpellID = 11891;
                spell.Target = "Realm";
                spell.Type = "Heal";
                m_Fairy_Heal = new Spell(spell, 70);
                spell.Uninterruptible = true;
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Fairy_Heal);
            }
            return m_Fairy_Heal;
        }
    }
    #endregion
}
#endregion Evern Fairies