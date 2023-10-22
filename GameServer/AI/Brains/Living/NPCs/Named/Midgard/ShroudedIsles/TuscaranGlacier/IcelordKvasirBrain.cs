using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI.Brains;

#region Icelord Kvasir
public class IcelordKvasirBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public IcelordKvasirBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 2000;
    }

    public static bool IsPulled = false;
    private bool StartMezz = false;
    private bool AggroText = false;
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if(!AggroText && Body.TargetObject != null)
        {
            if (Body.TargetObject is GamePlayer)
            {
                GamePlayer player = Body.TargetObject as GamePlayer;
                if (player != null && player.IsAlive)
                {
                    BroadcastMessage(String.Format("To come this far... only to die a horrible death! Huh! Do you not wish that you were taking on a safer endavour at this moment? You realize of course that all of your efforts will come to naught as you are about to die " + player.PlayerClass.Name + "?"));
                    AggroText = true;
                }
            }
        }
        if (IsPulled == false)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.PackageID == "KvasirBaf")
                    {
                        AddAggroListTo(npc.Brain as StandardMobBrain);
                        IsPulled = true;
                    }
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
            StartMezz = false;
            AggroText = false;
            var prepareMezz = Body.TempProperties.GetProperty<EcsGameTimer>("kvasir_prepareMezz");//cancel message
            if (prepareMezz != null)
            {
                prepareMezz.Stop();
                Body.TempProperties.RemoveProperty("kvasir_prepareMezz");
            }
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }

        if (HasAggro && Body.TargetObject != null)
        {
            if (!StartMezz)
            {
               EcsGameTimer prepareMezz = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PrepareMezz), Util.Random(45000, 60000));
                Body.TempProperties.SetProperty("kvasir_prepareMezz", prepareMezz);
                StartMezz = true;
            }
            if(!Body.IsCasting && Util.Chance(5))
                Body.CastSpell(IssoRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
        }
        base.Think();
    }
    private int PrepareMezz(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("{0} lets loose a primal scream so intense that it resonates in the surrounding ice for several seconds. Many in the immediate vicinite are stunned by the sound!", Body.Name));
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastMezz), 2000);
        return 0;
    }
    private int CastMezz(EcsGameTimer timer)
    {
        if (HasAggro && Body.TargetObject != null)
            Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        StartMezz = false;
        return 0;
    }

    #region Spells
    private Spell m_mezSpell;
    private Spell Mezz
    {
        get
        {
            if (m_mezSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.ClientEffect = 2619;
                spell.Icon = 2619;
                spell.Name = "Mesmerize";
                spell.Range = 0;
                spell.Radius = 800;
                spell.SpellID = 11928;
                spell.Duration = 60;
                spell.Target = ESpellTarget.ENEMY.ToString();
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
    private Spell m_IssoRoot;
    private Spell IssoRoot
    {
        get
        {
            if (m_IssoRoot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = Util.Random(45,55);
                spell.ClientEffect = 277;
                spell.Icon = 277;
                spell.Duration = 60;
                spell.Range = 1800;
                spell.Radius = 1000;
                spell.Value = 99;
                spell.Name = "Kvasir's Root";
                spell.TooltipId = 277;
                spell.SpellID = 11741;
                spell.Target = "Enemy";
                spell.Type = "SpeedDecrease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_IssoRoot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IssoRoot);
            }
            return m_IssoRoot;
        }
    }
    #endregion
}
#endregion Icelord Kvasir

#region Tunnels Announcer
public class TunnelsBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public TunnelsBrain()
        : base()
    {
        AggroLevel = 0;
        AggroRange = 0;
    }
    public static bool message1 = false;
    public static bool message2 = false;
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Think()
    {
        if (Body.IsAlive)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(10000))
            {
                if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !message2 && player.IsWithinRadius(Body, 400))
                    message2 = true;
            }
            if (message2 && !message1)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Announce), 200);
                message1 = true;
            }
        }
        base.Think();
    }
    private int Announce(EcsGameTimer timer)
    {
        BroadcastMessage("A low rumble echoes throughout the Tuscarian Glacier! Icicles resonating with the sound break off from the ceiling and shatter on the floors!" +
                        "The rumble grows louder causing small cracks to form in the walls! It sounds as though there is a swarm of giants on the move somewhere in the glacier!");
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMob), 300);
        return 0;
    }
    private int RemoveMob(EcsGameTimer timer)
    {
        if (Body.IsAlive)
            Body.RemoveFromWorld();
        return 0;
    }
}
#endregion Tunnels Announcer