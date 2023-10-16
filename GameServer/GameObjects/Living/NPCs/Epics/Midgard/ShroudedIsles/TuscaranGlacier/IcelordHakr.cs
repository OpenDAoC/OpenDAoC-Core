﻿using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Hakr : GameEpicBoss
    {
        public Hakr() : base()
        {
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 40;// dmg reduction for melee dmg
                case EDamageType.Crush: return 40;// dmg reduction for melee dmg
                case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override void Die(GameObject killer) //on kill generate orbs
        {
            Spawn_Snakes = false;
            HakrBrain.spam_message1 = false;
            base.Die(killer);
        }
        public static bool Spawn_Snakes = false;
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162347);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Spawn_Snakes = false;
            HakrBrain.spam_message1 = false;
            if (Spawn_Snakes == false)
            {
                SpawnSnakes();
                Spawn_Snakes = true;
            }
            HakrBrain sbrain = new HakrBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNpc[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Hakr", 160, (ERealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Icelord Hakr not found, creating it...");

                log.Warn("Initializing Icelord Hakr ...");
                Hakr TG = new Hakr();
                TG.Name = "Icelord Hakr";
                TG.Model = 918;
                TG.Realm = 0;
                TG.Level = 82;
                TG.Size = 70;
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = EDamageType.Crush;
                TG.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

                TG.X = 25405;
                TG.Y = 57241;
                TG.Z = 11359;
                TG.Heading = 1939;
                HakrBrain ubrain = new HakrBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Icelord Hakr exist ingame, remove it and restart server if you want to add by script code.");
        }
        public void SpawnSnakes()
        {
            for (int i = 0; i < 2; i++)
            {
                HakrAdd Add1 = new HakrAdd();
                Add1.X = X + Util.Random(-100, 100);
                Add1.Y = Y + Util.Random(-100, 100);
                Add1.Z = Z;
                Add1.CurrentRegion = CurrentRegion;
                Add1.Heading = Heading;
                Add1.PackageID = "HakrBaf";
                Add1.AddToWorld();
                ++HakrAdd.IceweaverCount;
            }
            for (int i = 0; i < 2; i++)
            {
                HakrAdd Add2 = new HakrAdd();
                Add2.X = 30008 + Util.Random(-100, 100);
                Add2.Y = 56329 + Util.Random(-100, 100);
                Add2.Z = 11894;
                Add2.CurrentRegion = CurrentRegion;
                Add2.Heading = Heading;
                Add2.AddToWorld();
                ++HakrAdd.IceweaverCount;
            }
        }
    }
}
namespace DOL.AI.Brain
{
    public class HakrBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public HakrBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 1500;
        }
        public static bool IsPulled = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is HakrAddBrain brain && npc.PackageID == "HakrBaf")
                        {
                            GameLiving target = Body.TargetObject as GameLiving;
                            if (!brain.HasAggro && Body != npc && target != null && target.IsAlive)
                                brain.AddToAggroList(target, 10);
                            IsPulled = true;
                        }
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public void TeleportPlayer()
        {
            if (HakrAdd.IceweaverCount > 0)
            {
                IList enemies = new ArrayList(AggroTable.Keys);
                foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!AggroTable.ContainsKey(player))
                                AggroTable.Add(player, 1);
                        }
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
                        if (PortTarget.IsVisibleTo(Body) && Body.TargetInView)
                        {
                            PortTarget.MoveTo(Body.CurrentRegionID, Body.X + Util.Random(-50, 50),
                            Body.Y + Util.Random(-50, 50), Body.Z + 220, Body.Heading);
                            BroadcastMessage(String.Format("Icelord Hakr says, '" + PortTarget.Name +" Touchdown! That's a really cool way of putting it!'"));
                            PortTarget = null;
                        }
                    }
                }
            }
        }
        public int PortTimer(EcsGameTimer timer)
        {
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DoPortTimer), 2000);
            return 0;
        }
        public int DoPortTimer(EcsGameTimer timer)
        {
            TeleportPlayer();
            spam_teleport = false;
            return 0;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
            }
        }
        public static bool spam_teleport = false;
        public static bool spam_message1 = false;
        public override void Think()
        {
            if (HakrAdd.IceweaverCount == 0 && spam_message1 == false && Body.IsAlive)
            {
                BroadcastMessage(String.Format("Magic barrier fades away from Icelord Hakr!"));
                spam_message1 = true;
            }
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
                spam_message1 = false;
                spam_teleport = false;
            }
            if (HasAggro)
            {
                if (spam_teleport == false && Body.TargetObject != null && HakrAdd.IceweaverCount > 0)
                {
                    int rand = Util.Random(10000, 20000);
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PortTimer), rand);
                    spam_teleport = true;
                }
            }
            base.Think();
        }
    }
}
////////////////////////////////////////////////////////////////////Adds-snakes////////////////////////////////////////////
namespace DOL.GS
{
    public class HakrAdd : GameNpc
    {
        public HakrAdd() : base()
        {
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 35; // dmg reduction for melee dmg
                case EDamageType.Crush: return 35; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public static int IceweaverCount = 0;
        public override void Die(GameObject killer)
        {
            --IceweaverCount;
            base.Die(killer);
        }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 250; }
        public override bool AddToWorld()
        {
            Model = 766;
            MeleeDamageType = EDamageType.Thrust;
            Name = "Royal Iceweaver";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3800;
            Size = 60;
            Level = 78;
            MaxSpeedBase = 270;

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            BodyType = 1;
            Realm = ERealm.None;

            HakrAddBrain adds = new HakrAddBrain();
            SetOwnBrain(adds);
            LoadedFromScript = true;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class HakrAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public HakrAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public static bool IsPulled = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is HakrAddBrain brain && npc.PackageID == "HakrBaf")
                        {
                            GameLiving target = Body.TargetObject as GameLiving;
                            if (!brain.HasAggro && Body != npc && target != null && target.IsAlive)
                                brain.AddToAggroList(target, 10);
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
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                IsPulled = false;
            }
            if (HasAggro)
            {
                if (Body.TargetObject != null)
                {
                    if (Body.TargetObject.IsWithinRadius(Body, Body.AttackRange))
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
                        {
                            if (Util.Chance(25))
                                Body.CastSpell(IceweaverPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                        }
                    }
                }
            }
            base.Think();
        }
        public Spell m_IceweaverPoison;
        public Spell IceweaverPoison
        {
            get
            {
                if (m_IceweaverPoison == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 3411;
                    spell.TooltipId = 3411;
                    spell.Icon = 3411;
                    spell.Damage = 100;
                    spell.Duration = 30;
                    spell.Name = "Iceweaver's Poison";
                    spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.Frequency = 30;
                    spell.Range = 400;
                    spell.SpellID = 11746;
                    spell.Target = "Enemy";
                    spell.Type = ESpellType.DamageOverTime.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) EDamageType.Body;
                    m_IceweaverPoison = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IceweaverPoison);
                }
                return m_IceweaverPoison;
            }
        }
    }
}