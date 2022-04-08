using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS
{
    public class Easmarach : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Easmarach()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 75; // dmg reduction for melee dmg
                case eDamageType.Crush: return 75; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 75; // dmg reduction for melee dmg
                default: return 55; // dmg reduction for rest resists
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
            return 850;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            EasmarachBrain sBrain = new EasmarachBrain();
            SetOwnBrain(sBrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in this.GetNPCsInRadius(4000))
            {
                if (npc.Brain is EasmarachAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }

            base.Die(killer);
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Easmarach", 191, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Easmarach not found, creating it...");

                log.Warn("Initializing Easmarach...");
                Easmarach CO = new Easmarach();
                CO.Name = "Easmarach";
                CO.Model = 816;
                CO.Realm = 0;
                CO.Level = 81;
                CO.Size = 250;
                CO.CurrentRegionID = 191; //galladoria

                CO.Strength = 500;
                CO.Intelligence = 150;
                CO.Piety = 150;
                CO.Dexterity = 200;
                CO.Constitution = 200;
                CO.Quickness = 125;
                CO.BodyType = 5;
                CO.MeleeDamageType = eDamageType.Slash;
                CO.Faction = FactionMgr.GetFactionByID(96);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                CO.X = 37913;
                CO.Y = 50298;
                CO.Z = 10943;
                CO.MaxDistance = 3500;
                CO.TetherRange = 3800;
                CO.MaxSpeedBase = 300;
                CO.Heading = 3060;

                EasmarachBrain ubrain = new EasmarachBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                CO.SetOwnBrain(ubrain);
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
                CO.LoadTemplate(npcTemplate);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Easmarach exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class EasmarachBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public EasmarachBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        private int m_stage = 10;

        /// <summary>
        /// This keeps track of the stage the encounter is in.
        /// </summary>
        public int Stage
        {
            get { return m_stage; }
            set
            {
                if (value >= 0 && value <= 10) m_stage = value;
            }
        }

        public static bool restphase = false;
        public static bool dontattack = false;
        public static bool message = false;

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void AttackMostWanted()
        {
            if (dontattack == true)
                return;
            else
            {
                if (ECS.Debug.Diagnostics.AggroDebugEnabled)
                {
                    PrintAggroTable();
                }

                Body.TargetObject = CalculateNextAttackTarget();

                if (Body.TargetObject != null)
                {
                    if (!CheckSpells(eCheckSpellType.Offensive))
                    {
                        Body.StartAttack(Body.TargetObject);
                    }
                }
            }

            base.AttackMostWanted();
        }

        public void ReturnToWaterfall()
        {
            Point3D point1 = new Point3D();
            point1.X = 37811;
            point1.Y = 50342;
            point1.Z = 10758;

            if (Body.HealthPercent <= 50 && restphase == false)
            {
                Body.StopAttack();
                Body.StopFollowing();
                AggroTable.Clear();
                ClearAggroList();

                if (Body.IsWithinRadius(point1, 80))
                {
                    Body.Health += Body.MaxHealth / 6;
                    restphase = true;
                    dontattack = false;
                    Stage = 7;
                }
                else
                {
                    if (!Body.IsMoving)
                    {
                        Body.WalkTo(point1, 200);
                        dontattack = true;
                        if (message == false)
                        {
                            BroadcastMessage(String.Format(Body.Name + " is retreating to waterfall!"));
                            message = true;
                        }
                    }
                }
            }
        }

        public override void Think()
        {
            ReturnToWaterfall();

            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
                restphase = false;
                dontattack = false;
                message = false;
                Stage = 10;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is EasmarachAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }

            if (Body.InCombat && HasAggro)
            {
                int health = Body.HealthPercent / 10;
                if (Body.TargetObject != null && health < Stage)
                {
                    switch (health)
                    {
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        {
                            Spawn();
                        }
                            break;
                    }

                    Stage = health;
                }
            }

            base.Think();
        }

        public void Spawn() // We define here adds
        {
            for (int i = 0; i < Util.Random(2, 3); i++) //Spawn 2 or 3 adds
            {
                EasmarachAdd Add = new EasmarachAdd();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
        }
    }
}

////////////////////////////////////////adds//////////////////////
namespace DOL.GS
{
    public class EasmarachAdd : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public EasmarachAdd()
            : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get { return 7000; }
        }

        public override int AttackRange
        {
            get { return 450; }
            set { }
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
            Model = 816;
            Name = "lurker";
            Size = 80;
            Level = 77;
            Realm = 0;
            CurrentRegionID = 191; //galladoria

            Strength = 180;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;

            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            BodyType = 5;
            EasmarachAddBrain sBrain = new EasmarachAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class EasmarachAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public EasmarachAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(5) && Body.TargetObject != null)
                {
                    if (LurkerStun.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
                    {
                        new RegionTimer(Body, new RegionTimerCallback(CastAOEDD), 3000);
                    }
                }
            }

            base.Think();
        }

        public int CastAOEDD(RegionTimer timer)
        {
            Body.CastSpell(LurkerStun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }

        private Spell m_LurkerStun;

        private Spell LurkerStun
        {
            get
            {
                if (m_LurkerStun == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 35;
                    spell.ClientEffect = 4125;
                    spell.Icon = 4125;
                    spell.Name = "Stun";
                    spell.TooltipId = 4125;
                    spell.Duration = 9;
                    spell.SpellID = 11712;
                    spell.Target = "Enemy";
                    spell.Type = "Stun";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Energy;
                    m_LurkerStun = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LurkerStun);
                }

                return m_LurkerStun;
            }
        }
    }
}