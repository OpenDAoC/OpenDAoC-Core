﻿using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
    public class Jailer : GameEpicBoss
    {
        public Jailer() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }


        public override int MeleeAttackRange => 350;
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
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162583);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(140);
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            JailerBrain sbrain = new JailerBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Jailer Vifil", 160, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Jailer Vifil not found, creating it...");

                log.Warn("Initializing Jailer Vifil...");
                Jailer TG = new Jailer();
                TG.Name = "Jailer Vifil";
                TG.Model = 918;
                TG.Realm = 0;
                TG.Level = 82;
                TG.Size = 70;
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval =
                    ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                    60000; //1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);

                TG.X = 27086;
                TG.Y = 61284;
                TG.Z = 10349;
                TG.Heading = 990;
                JailerBrain ubrain = new JailerBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Jailer Vifil exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class JailerBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public JailerBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }

        public static bool IsPulled = false;

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                SpawnTunnelGuardians();
                IsPulled = true;
            }
            base.OnAttackedByEnemy(ad);
        }

        public void TeleportPlayer()
        {
            List<GameLiving> enemies = AggroList.Keys.ToList();
            foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        AggroList.TryAdd(player, new());
                }
            }
            if (enemies.Count == 0)
                return;
            else
            {
                List<GameLiving> damage_enemies = new List<GameLiving>();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    if (!(enemies[i] is GameLiving))
                        continue;
                    if (!(enemies[i] as GameLiving).IsAlive)
                        continue;
                    GameLiving living = null;
                    living = enemies[i] as GameLiving;
                    if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
                    {
                        damage_enemies.Add(enemies[i] as GameLiving);
                    }
                }
                if (damage_enemies.Count > 0)
                {
                    GamePlayer PortTarget = (GamePlayer) damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                    if (PortTarget.IsVisibleTo(Body) && Body.TargetInView && PortTarget != null && PortTarget.IsAlive)
                    {
                        AggroList.TryRemove(PortTarget, out _);
                        PortTarget.MoveTo(Body.CurrentRegionID, 16631, 58683, 10858, 2191);
                        PortTarget = null;
                    }
                }
            }
        }
        public int PortTimer(ECSGameTimer timer)
        {
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoPortTimer), 5000);
            return 0;
        }
        public int DoPortTimer(ECSGameTimer timer)
        {
            TeleportPlayer();
            spam_teleport = false;
            return 0;
        }
        private bool RemoveAdds = false;
        public static bool spam_teleport = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                IsPulled = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(160))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && !npc.InCombat)
                            {
                                if (npc.Brain is JailerAddBrain && npc.RespawnInterval == -1)
                                    npc.RemoveFromWorld();
                            }
                        }
                    }
                    RemoveAdds = true;
                }
            }

            if (Body.TargetObject != null && HasAggro)
            {
                RemoveAdds = false;
                if (spam_teleport == false && Body.TargetObject != null)
                {
                    int rand = Util.Random(25000, 45000);
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PortTimer), rand);
                    spam_teleport = true;
                }
            }
            base.Think();
        }

        public void SpawnTunnelGuardians()
        {
            JailerAdd Add1 = new JailerAdd();
            Add1.X = 16709;
            Add1.Y = 58973;
            Add1.Z = 10879;
            Add1.CurrentRegion = Body.CurrentRegion;
            Add1.Heading = 2088;
            Add1.AddToWorld();

            JailerAdd Add2 = new JailerAdd();
            Add2.X = 16379;
            Add2.Y = 58954;
            Add2.Z = 10885;
            Add2.CurrentRegion = Body.CurrentRegion;
            Add2.Heading = 2048;
            Add2.AddToWorld();
        }
    }
}

////////////////////////////////////////////////////////////////////Spawn Adds on tunnel entrance////////////////////////////////////////////
namespace DOL.GS
{
    public class JailerAdd : GameNPC
    {
        public JailerAdd() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 45; // dmg reduction for melee dmg
                case eDamageType.Crush: return 45; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 45; // dmg reduction for melee dmg
                default: return 30; // dmg reduction for rest resists
            }
        }


        public override int MeleeAttackRange => 350;

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 200;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }

        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 250; }
        public override bool AddToWorld()
        {
            Model = 918;
            MeleeDamageType = eDamageType.Crush;
            Name = "hrimathursa tormentor";
            RespawnInterval = -1;

            TetherRange = 5800;
            Size = 50;
            Level = 78;
            MaxSpeedBase = 270;

            Faction = FactionMgr.GetFactionByID(140);
            BodyType = 1;
            Realm = eRealm.None;

            JailerAddBrain adds = new JailerAddBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class JailerAddBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public JailerAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}
