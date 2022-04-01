using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class SoulReckoner : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SoulReckoner()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 55; // dmg reduction for melee dmg
                case eDamageType.Crush: return 55; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 55; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }
        public virtual int COifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get { return 20000; }
        }

        public override int AttackRange
        {
            get { return 450; }
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
            return 1000;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166369);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;

            MeleeDamageType = eDamageType.Spirit;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            ReckonedSoul.SoulCount = 0;
            SoulReckonerBrain adds = new SoulReckonerBrain();
            SetOwnBrain(adds);
            if (CurrentRegionID == 60)
            {
                if (spawn_souls == false)
                {
                    SpawnSouls();
                    spawn_souls = true;
                }
            }
            else
            {
                if (spawn_souls == false)
                {
                    SpawnSouls();
                    spawn_souls = true;
                }
            }

            base.AddToWorld();
            return true;
        }

        public static bool spawn_souls = false;

        public void SpawnSouls()
        {
            for (int i = 0; i < Util.Random(4, 6); i++) // Spawn 4-6 souls
            {
                ReckonedSoul Add = new ReckonedSoul();
                Add.X = this.X + Util.Random(-50, 80);
                Add.Y = this.Y + Util.Random(-50, 80);
                Add.Z = this.Z;
                Add.CurrentRegion = this.CurrentRegion;
                Add.Heading = this.Heading;
                Add.AddToWorld();
            }
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Soul Reckoner", 60, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Soul Reckoner not found, creating it...");

                log.Warn("Initializing Soul Reckoner...");
                SoulReckoner CO = new SoulReckoner();
                CO.Name = "Soul Reckoner";
                CO.Model = 1676;
                CO.Realm = 0;
                CO.Level = 81;
                CO.Size = 190;
                CO.CurrentRegionID = 60; //caer sidi

                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

                CO.X = 28891;
                CO.Y = 35855;
                CO.Z = 15370;
                CO.Heading = 966;
                CO.Flags = eFlags.GHOST;

                SoulReckonerBrain ubrain = new SoulReckonerBrain();
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Soul Reckoner exist ingame, remove it and restart server if you want to add by script code.");
        }

        public override void Die(GameObject killer) //on kill generate orbs
        {
            foreach (GameNPC npc in this.GetNPCsInRadius(4000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive)
                    {
                        if (npc.Brain is ReckonedSoulBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }

            spawn_souls = false;
            base.Die(killer);
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (ReckonedSoul.SoulCount > 0 || SoulReckonerBrain.InRoom == false) //take no damage
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " brushes off your attack!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else //take dmg
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage("The " + this.Name + " flickers briefly", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }

        public override void EnemyKilled(GameLiving enemy)
        {
            this.Health += this.MaxHealth / 5; //heals if boss kill enemy
            base.EnemyKilled(enemy);
        }
    }
}

namespace DOL.AI.Brain
{
    public class SoulReckonerBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SoulReckonerBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
        }

        public static bool InRoom = false;

        public void AwayFromRoom()
        {
            Point3D room_radius = new Point3D();
            room_radius.X = 28472;
            room_radius.Y = 35857;
            room_radius.Z = 15370; //room middle point

            if (Body.CurrentRegionID == 60)
            {
                if (Body.IsWithinRadius(room_radius, 900)) //if is in room
                {
                    InRoom = true;
                }
                else //is out of room
                {
                    InRoom = false;
                }
            }
            else
            {
                Point3D spawnpoint = new Point3D();
                spawnpoint.X = Body.SpawnPoint.X;
                spawnpoint.Y = Body.SpawnPoint.Y;
                spawnpoint.Z = Body.SpawnPoint.Z;
                if (Body.IsWithinRadius(spawnpoint, 900)) //if is in radius of spawnpoint
                {
                    InRoom = true;
                }
                else //is out of room
                {
                    InRoom = false;
                }
            }
        }

        public static bool BafMobs = false;

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                BafMobs = false;
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 11);
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
                Spawn_Souls = false;
            }

            if (Body.InCombat || HasAggro || Body.AttackState == true)
            {
                AwayFromRoom();

                if (Util.Chance(15)) // 15% chance to cast lifetap dmg
                {
                    if (Body.TargetObject != null)
                    {
                        Body.TurnTo(Body.TargetObject);
                        Body.CastSpell(Reckoner_Lifetap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }

                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "SoulReckonerBaf" && npc.Brain is ReckonedSoulBrain brain)
                            {
                                AddAggroListTo(
                                    brain); // add to aggro mobs with CryptLordBaf PackageID
                                BafMobs = true;
                            }
                        }
                    }
                }
            }

            base.Think();
        }

        public static bool Spawn_Souls = false;

        public void SpawnSouls()
        {
            for (int i = 0; i < Util.Random(4, 6); i++) // Spawn 4-6 souls
            {
                ReckonedSoul Add = new ReckonedSoul();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
        }

        public Spell m_Reckoner_Lifetap;

        public Spell Reckoner_Lifetap
        {
            get
            {
                if (m_Reckoner_Lifetap == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 20;
                    spell.ClientEffect = 9191;
                    spell.Icon = 710;
                    spell.Damage = 350;
                    spell.Value = -90;
                    spell.LifeDrainReturn = 90;
                    spell.Name = "Drain Life Essence";
                    spell.Range = 1800;
                    spell.SpellID = 11733;
                    spell.Target = "Enemy";
                    spell.Type = "Lifedrain";
                    spell.MoveCast = true;
                    spell.Uninterruptible = true;
                    spell.DamageType = (int) eDamageType.Body; //Body DMG Type
                    m_Reckoner_Lifetap = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Reckoner_Lifetap);
                }

                return m_Reckoner_Lifetap;
            }
        }
    }
}

///////////////////////////////////////////////////Reckoned Soul//////////////////////////////
namespace DOL.GS
{
    public class ReckonedSoul : GameNPC
    {
        public ReckonedSoul() : base()
        {
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
            return 800;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.65;
        }

        public override int MaxHealth
        {
            get { return 10000; }
        }

        public override void Die(GameObject killer)
        {
            --SoulCount;
            base.Die(killer);
        }

        public static int SoulCount = 0;

        public override bool AddToWorld()
        {
            Model = 1772;
            MeleeDamageType = eDamageType.Spirit;
            Name = "reckoned soul";
            PackageID = "SoulReckonerBaf";
            RespawnInterval = -1;

            MaxDistance = 2500;
            TetherRange = 3000;
            RoamingRange = 120;
            Size = 70;
            Level = 75;
            MaxSpeedBase = 200;
            Flags = eFlags.GHOST;

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 6;
            Realm = eRealm.None;
            ++SoulCount;

            Strength = 80;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            ReckonedSoulBrain adds = new ReckonedSoulBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class ReckonedSoulBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ReckonedSoulBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            base.Think();
        }
    }
}