﻿using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Silencer : GameEpicBoss
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Silencer()
            : base()
        {
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override double AttackDamage(DbInventoryItems weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100  * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }

        public static List<GamePlayer> attackers = new List<GamePlayer>();
        public static int attackers_count = 0;
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                attackers.Add(source as GamePlayer);
                attackers_count = attackers.Count / 10;

                if (Util.Chance(attackers_count))
                {
                    if (resist_timer == false)
                    {
                        BroadcastMessage(String.Format(this.Name + " becomes almost immune to any damage for short time!"));
                        new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(ResistTime), 2000);
                        resist_timer = true;
                    }
                }
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        public override int GetResist(eDamageType damageType)
        {
            if (get_resist)
            {
                switch (damageType)
                {
                    case eDamageType.Slash:
                    case eDamageType.Crush:
                    case eDamageType.Thrust: return 99; //99% dmg reduction for melee dmg
                    default: return 99; // 99% reduction for rest resists
                }
            }
            else
            {
                switch (damageType)
                {
                    case eDamageType.Slash: return 30;
                    case eDamageType.Crush: return 30;
                    case eDamageType.Thrust: return 30; //30% dmg reduction for melee dmg
                    default: return 50; // 50% reduction for rest resists
                }
            }
        }

        public static bool get_resist = false; //set resists
        public static bool resist_timer = false;
        public static bool resist_timer_end = false;
        public static bool spam1 = false;

        public int ResistTime(ECSGameTimer timer)
        {
            get_resist = true;
            spam1 = false;
            if (resist_timer == true && resist_timer_end == false)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendSpellEffectAnimation(this, this, 9103, 0, false, 0x01);
                }
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(ResistTimeEnd), 20000); //20s resist 99%
                resist_timer_end = true;
            }
            return 0;
        }
        public int ResistTimeEnd(ECSGameTimer timer)
        {
            get_resist = false;
            resist_timer = false;
            resist_timer_end = false;
            attackers.Clear();
            attackers_count = 0;
            if (spam1 == false)
            {
                BroadcastMessage(String.Format(this.Name + " resists fades away!"));
                spam1 = true;
            }
            return 0;
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166029);
            LoadTemplate(npcTemplate);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            attackers_count = 0;
            get_resist = false;
            resist_timer = false;
            resist_timer_end = false;
            spam1 = false;

            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            SilencerBrain adds = new SilencerBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class SilencerBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SilencerBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 5000;
            CanBAF = false;
        }
        private bool ClearAttackers = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                Silencer.attackers_count = 0;
                //Silencer silencer = new Silencer();
                if (!ClearAttackers)
                {
                    if (Silencer.attackers.Count > 0)
                    {
                        Silencer.attackers.Clear();
                        ClearAttackers = true;
                    }
                }
            }
            if (HasAggro && Body.TargetObject != null)
                ClearAttackers = false;
            if (Body.IsOutOfTetherRange)
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                Body.Model = 934;
            }
            base.Think();
        }
    }
}