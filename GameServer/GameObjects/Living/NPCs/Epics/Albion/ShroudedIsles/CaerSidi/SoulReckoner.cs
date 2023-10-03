﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class SoulReckoner : GameEpicBoss
    {
        private static new readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SoulReckoner()
            : base()
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
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
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
            SaveIntoDatabase();
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }

        public static bool spawn_souls = false;

        public void SpawnSouls()
        {
            for (int i = 0; i < Util.Random(4, 6); i++) // Spawn 4-6 souls
            {
                ReckonedSoul Add = new ReckonedSoul();
                Add.X = X + Util.Random(-50, 80);
                Add.Y = Y + Util.Random(-50, 80);
                Add.Z = Z;
                Add.CurrentRegion = CurrentRegion;
                Add.Heading = Heading;
                Add.AddToWorld();
            }
        }

        public override void Die(GameObject killer) //on kill generate orbs
        {
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc != null && npc.IsAlive)
                {
                    if (npc.Brain is ReckonedSoulBrain)
                        npc.RemoveFromWorld();
                }
            }
            spawn_souls = false;
            base.Die(killer);
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (ReckonedSoul.SoulCount > 0 || SoulReckonerBrain.InRoom == false) //take no damage
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " brushes off your attack!", eChatType.CT_System,eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else //take dmg
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage("The " + Name + " flickers briefly", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override void DealDamage(AttackData ad)
        {
            if(ad != null && ad.DamageType == eDamageType.Body)
                Health += ad.Damage;
            base.DealDamage(ad);
        }
        public override void EnemyKilled(GameLiving enemy)
        {
            Health += MaxHealth / 5; //heals if boss kill enemy
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
            CanBAF = false;
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
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                BafMobs = false;
                Spawn_Souls = false;
            }

            if (HasAggro && Body.TargetObject != null)
            {
                AwayFromRoom();
                if (Util.Chance(50))
                {
                    Body.TurnTo(Body.TargetObject);
                    Body.CastSpell(Reckoner_Lifetap, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                }
                if(!Spawn_Souls)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnSouls), Util.Random(10000, 15000));
                    Spawn_Souls = true;
                }
                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "SoulReckonerBaf" && npc.Brain is ReckonedSoulBrain brain)
                            {
                                AddAggroListTo(brain); // add to aggro mobs with CryptLordBaf PackageID
                                BafMobs = true;
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        #region Spawn Soul
        public static bool Spawn_Souls = false;
        private int SpawnSouls(ECSGameTimer timer)
        {
            if (Body.IsAlive && HasAggro)
            {
                for (int i = 0; i < Util.Random(1, 2); i++)
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
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetRespawnSouls), Util.Random(60000, 70000));
            return 0;
        }
        private int ResetRespawnSouls(ECSGameTimer timer)
        {
            Spawn_Souls = false;
            return 0;
        }
        #endregion

        #region Spell
        public Spell m_Reckoner_Lifetap;
        public Spell Reckoner_Lifetap
        {
            get
            {
                if (m_Reckoner_Lifetap == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = Util.Random(8,12);
                    spell.ClientEffect = 9191;
                    spell.Icon = 710;
                    spell.Damage = 650;
                    spell.Name = "Drain Life Essence";
                    spell.Range = 1800;
                    spell.SpellID = 11733;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.MoveCast = true;
                    spell.Uninterruptible = true;
                    spell.DamageType = (int) eDamageType.Body; //Body DMG Type
                    m_Reckoner_Lifetap = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Reckoner_Lifetap);
                }
                return m_Reckoner_Lifetap;
            }
        }
        #endregion
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

        public override double AttackDamage(DbInventoryItem weapon)
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
            return 150;
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

        public override void Die(GameObject killer)
        {
            --SoulCount;
            base.Die(killer);
        }
        public override void DropLoot(GameObject killer)
        {
        }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 150; }
        public static int SoulCount = 0;

        public override bool AddToWorld()
        {
            Model = 909;
            MeleeDamageType = eDamageType.Spirit;
            Name = "reckoned soul";
            PackageID = "SoulReckonerBaf";
            RespawnInterval = -1;

            MaxDistance = 2500;
            TetherRange = 3000;
            RoamingRange = 120;
            Size = 100;
            Level = 75;
            MaxSpeedBase = 230;
            Flags = eFlags.GHOST;           

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 6;
            Realm = eRealm.None;
            ++SoulCount;

            ReckonedSoulBrain adds = new ReckonedSoulBrain();
            SetOwnBrain(adds);
            LoadedFromScript = true;
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
            CanBAF = false;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}