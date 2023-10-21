using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Scripts;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Lich Lord Ilron
public class LichLordIlronBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static bool spawnimages = true;

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            spawnimages = true;
            foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
            {
                if (npc.Brain is IlronImagesBrain)
                    npc.RemoveFromWorld();
            }
        }
        base.Think();
    }


    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (spawnimages)
        {
            Spawn(); // spawn images
            foreach (GameNpc mob_c in Body.GetNPCsInRadius(2000))
            {
                if (mob_c?.Brain is IlronImagesBrain && mob_c.IsAlive && mob_c.IsAvailable)
                {
                    AddAggroListTo(mob_c.Brain as IlronImagesBrain);
                }
            }
            spawnimages = false; // check to avoid spawning adds multiple times
        }
        base.OnAttackedByEnemy(ad);
    }

    public void Spawn()
    {
        foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
        {
            if (npc.Brain is IlronImagesBrain)
            {
                return;
            }
        }

        for (int i = 0; i < 4; i++) // Spawn 5 images
        {
            IlronImages Add = new IlronImages();
            Add.X = Body.X + Util.Random(-100, 100);
            Add.Y = Body.Y + Util.Random(-100, 100);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.IsWorthReward = false;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }

        spawnimages = false;
    }
}
#endregion Lich Lord Ilron

#region Ilron Images
public class IlronImagesBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public IlronImagesBrain()
    {
        AggroLevel = 100;
        AggroRange = 450;
    }

    #region pbaoe mezz

    /// <summary>
    /// The Bomb spell. Override this property in your Aros Epic summonedGuard implementation
    /// and assign the spell to m_breathSpell.
    /// </summary>
    ///
    /// 
    protected Spell m_mezSpell;

    /// <summary>
    /// The Bomb spell.
    /// </summary>
    protected Spell Mezz
    {
        get
        {
            if (m_mezSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.ClientEffect = 1681;
                spell.Icon = 1685;
                spell.Damage = 0;
                spell.Name = "Mesmerized";
                spell.Range = 1500;
                spell.Radius = 300;
                spell.SpellID = 99999;
                spell.Duration = 30;
                spell.Target = "Enemy";
                spell.Type = "Mesmerize";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Spirit; //Spirit DMG Type
                m_mezSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
            }

            return m_mezSpell;
        }
    }

    #endregion

    public override void Think()
    {
        if (Util.Chance(3))
        {
            Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.Think();
    }
}
#endregion Ilron Images