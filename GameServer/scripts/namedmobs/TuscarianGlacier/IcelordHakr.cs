using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;


namespace DOL.GS
{
    public class Hakr : GameEpicBoss
    {
        public Hakr() : base()
        {
        }

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 80; // dmg reduction for melee dmg
                case eDamageType.Crush: return 80; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 80; // dmg reduction for melee dmg
                default: return 60; // dmg reduction for rest resists
            }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 800;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }

        public override int MaxHealth
        {
            get { return 20000; }
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
            RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
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
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Hakr", 160, (eRealm) 0);
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
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval =
                    ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                    60000; //1min is 60000 miliseconds
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
                Add1.X = this.X + Util.Random(-100, 100);
                Add1.Y = this.Y + Util.Random(-100, 100);
                Add1.Z = this.Z;
                Add1.CurrentRegion = this.CurrentRegion;
                Add1.Heading = this.Heading;
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
                Add2.CurrentRegion = this.CurrentRegion;
                Add2.Heading = this.Heading;
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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is HakrAddBrain && npc.PackageID == "HakrBaf")
                        {
                            AddAggroListTo(npc.Brain as HakrAddBrain);
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
                IList enemies = new ArrayList(m_aggroTable.Keys);
                foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!m_aggroTable.ContainsKey(player))
                            {
                                m_aggroTable.Add(player, 1);
                            }
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
                            BroadcastMessage(String.Format("Icelord Hakr says, '" + PortTarget.Name +
                                                           " Touchdown! That's a really cool way of putting it!'"));
                            PortTarget = null;
                        }
                    }
                }
            }
        }

        public int PortTimer(RegionTimer timer)
        {
            new RegionTimer(Body, new RegionTimerCallback(DoPortTimer), 2000);
            return 0;
        }

        public int DoPortTimer(RegionTimer timer)
        {
            TeleportPlayer();
            spam_teleport = false;
            return 0;
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
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

            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                IsPulled = false;
            }

            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
                IsPulled = false;
            }

            if (Body.InCombat || HasAggro || Body.AttackState == true)
            {
                if (spam_teleport == false && Body.TargetObject != null && HakrAdd.IceweaverCount > 0)
                {
                    int rand = Util.Random(10000, 20000);
                    new RegionTimer(Body, new RegionTimerCallback(PortTimer), rand);
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
    public class HakrAdd : GameNPC
    {
        public HakrAdd() : base()
        {
        }

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 35; // dmg reduction for melee dmg
                case eDamageType.Crush: return 35; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }

        public override int MaxHealth
        {
            get { return 10000; }
        }

        public static int IceweaverCount = 0;

        public override void Die(GameObject killer)
        {
            --IceweaverCount;
            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            Model = 766;
            MeleeDamageType = eDamageType.Thrust;
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
            Realm = eRealm.None;

            Strength = 100;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is HakrAddBrain && npc.PackageID == "HakrBaf")
                        {
                            AddAggroListTo(npc.Brain as HakrAddBrain);
                            IsPulled = true;
                        }
                    }
                }
            }

            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                IsPulled = false;
            }

            if (Body.InCombat || HasAggro)
            {
                if (Body.TargetObject != null)
                {
                    if (Body.TargetObject.IsWithinRadius(Body, Body.AttackRange))
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
                        {
                            if (Util.Chance(25))
                            {
                                Body.CastSpell(IceweaverPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                            }
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
                    DBSpell spell = new DBSpell();
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
                    spell.Type = eSpellType.DamageOverTime.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Body;
                    m_IceweaverPoison = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IceweaverPoison);
                }

                return m_IceweaverPoison;
            }
        }
    }
}