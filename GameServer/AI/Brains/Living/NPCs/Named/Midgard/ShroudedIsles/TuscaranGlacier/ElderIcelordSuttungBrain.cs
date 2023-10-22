using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

#region Elder Icelord Suttung
public class ElderIcelordSuttungBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ElderIcelordSuttungBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    public static bool IsBerserker = false;

    public int BerserkerPhase(EcsGameTimer timer)
    {
        if (Body.IsAlive && IsBerserker == true && Body.InCombat && HasAggro)
        {
            BroadcastMessage(String.Format(Body.Name + " goes into berserker stance!"));
            Body.Emote(EEmote.MidgardFrenzy);
            Body.Strength = 850;
            Body.MaxSpeedBase = 200; //slow under zerk mode
            Body.Size = 75;
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(EndBerserkerPhase),Util.Random(10000, 20000)); //10-20s in berserk stance
        }
        return 0;
    }

    public int EndBerserkerPhase(EcsGameTimer timer)
    {
        if (Body.IsAlive)
        {
            BroadcastMessage(String.Format(Body.Name + " berserker stance fades away!"));
            Body.Strength = Body.NPCTemplate.Strength;
            Body.Size = Convert.ToByte(Body.NPCTemplate.Size);
            Body.MaxSpeedBase = Body.NPCTemplate.MaxSpeed;
            IsBerserker = false;
        }

        return 0;
    }

    public static bool message1 = false;
    public static bool message2 = false;
    public static bool AggroText = false;

    public override void Think()
    {
        Point3D point = new Point3D(31088, 53870, 11886);
        if(Body.IsAlive)
        {
            foreach(GamePlayer player in Body.GetPlayersInRadius(8000))
            {
                if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !message2 && player.IsWithinRadius(point, 400))
                    message2=true;
            }
            if(message2 && !message1)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Announce), 200);
                message1 = true;
            }
        }
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            AggroText = false;
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

        if (HasAggro)
        {
            if(!AggroText)
            {
                BroadcastMessage(String.Format(Body.Name + " says, 'The price of your invading our frozen fortress is death!" +
                " Death to you and your allies! Your presence here mocks the pacifist philosophy of my opponents on the Council." +
                " I weep for no council member who has perished!'"));
                AggroText = true;
            }
            if (IsBerserker == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(BerserkerPhase), Util.Random(20000, 35000));
                IsBerserker = true;
            }
        }
        if(HasAggro && Body.TargetObject != null)
        {
            if (Util.Chance(55))
                Body.CastSpell(IcelordHjalmar_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
        }
        base.Think();
    }
    private int Announce(EcsGameTimer timer)
    {
        BroadcastMessage("An otherworldly howling sound suddenly becomes perceptible. The sound quickly grows louder but it is not accompanied by a word. Moments after it begins, the howling sound is gone, replace by the familiar noises of the slowly shifting glacier.");
        return 0;
    }
    private Spell m_IcelordHjalmar_aoe;
    private Spell IcelordHjalmar_aoe
    {
        get
        {
            if (m_IcelordHjalmar_aoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 15;
                spell.ClientEffect = 208;
                spell.Icon = 208;
                spell.TooltipId = 208;
                spell.Damage = 450;
                spell.Name = "Hjalmar's Ice Blast";
                spell.Range = 0;
                spell.Radius = 440;
                spell.SpellID = 11901;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_IcelordHjalmar_aoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IcelordHjalmar_aoe);
            }
            return m_IcelordHjalmar_aoe;
        }
    }
}
#endregion Elder Icelord Suttung

#region Hjalmar and Suttung Controller
public class HjalmarSuttungControllerBrain : APlayerVicinityBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HjalmarSuttungControllerBrain()
        : base()
    {
        ThinkInterval = 1000;
    }
    public static bool Spawn_Boss = false;
    public override void Think()
    {
        int respawn = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;
        if (Body.IsAlive)
        {
            if (ElderIcelordSuttung.SuttungCount == 1 || ElderIcelordHjalmar.HjalmarCount == 1)//one of them is up
            {
                //log.Warn("Suttung or Hjalmar is around");
            }
            if(ElderIcelordSuttung.SuttungCount == 0 && ElderIcelordHjalmar.HjalmarCount == 0)//noone of them is up
            {
                if (!Spawn_Boss)
                {
                    //log.Warn("Trying to respawn Suttung or Hjalmar");
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnBoss), respawn);
                    Spawn_Boss = true;
                }
            }
        }
    }

    public override void KillFSM()
    {
        
    }

    private int SpawnBoss(EcsGameTimer timer)
    {
        switch(Util.Random(1,2))
        {
            case 1: SpawnSuttung(); break;
            case 2: SpawnHjalmar(); break;
        }
        return 0;
    }
    private void SpawnSuttung()
    {
        if (ElderIcelordSuttung.SuttungCount == 0)
        {
            ElderIcelordSuttung boss = new ElderIcelordSuttung();
            boss.X = 32055;
            boss.Y = 54253;
            boss.Z = 11883;
            boss.Heading = 2084;
            boss.CurrentRegion = Body.CurrentRegion;
            boss.AddToWorld();
            Spawn_Boss = false;
        }
    }
    private void SpawnHjalmar()
    {
        if (ElderIcelordHjalmar.HjalmarCount == 0)
        {
            ElderIcelordHjalmar boss = new ElderIcelordHjalmar();
            boss.X = 32079;
            boss.Y = 53415;
            boss.Z = 11885;
            boss.Heading = 21;
            boss.CurrentRegion = Body.CurrentRegion;
            boss.AddToWorld();
            Spawn_Boss = false;
        }
    }
}
#endregion