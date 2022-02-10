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
    public class SpindlerBroodmother : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SpindlerBroodmother()
            : base()
        {
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
            get
            {
                return 10000 * Constitution / 100;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
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


        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Spindler Broodmother", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Spindler Broodmother not found, creating it...");

                log.Warn("Initializing Spindler Broodmother...");
                SpindlerBroodmother SB = new SpindlerBroodmother();
                SB.Name = "Spindler Broodmother";
                SB.Model = 904;
                SB.Realm = 0;
                SB.Level = 81;
                SB.Size = 125;
                SB.CurrentRegionID = 191;//galladoria

                SB.Strength = 310;
                SB.Intelligence = 220;
                SB.Piety = 220;
                SB.Dexterity = 200;
                SB.Constitution = 100;
                SB.Quickness = 125;
                SB.BodyType = 5;
                SB.MeleeDamageType = eDamageType.Slash;
                SB.Faction = FactionMgr.GetFactionByID(96);
                SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                SB.X = 21283;
                SB.Y = 51707;
                SB.Z = 10876;
                SB.MaxDistance = 2000;
                SB.TetherRange = 2500;
                SB.MaxSpeedBase = 300;
                SB.Heading = 0;

                SpindlerBroodmotherBrain ubrain = new SpindlerBroodmotherBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                SB.LoadedFromScript = false;
                SB.SetOwnBrain(ubrain);
                SB.AddToWorld();
                SB.Brain.Start();
                SB.SaveIntoDatabase();
            }
            else
                log.Warn("Spindler Broodmother exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class SpindlerBroodmotherBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public SpindlerBroodmotherBrain()
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
            set { if (value >= 0 && value <= 10) m_stage = value; }
        }
        #region spawnadds checks
        public static bool spawnadds1 = true;
        public static bool spawnadds2 = true;
        public static bool spawnadds3 = true;
        public static bool spawnadds4 = true;
        public static bool spawnadds5 = true;
        public static bool spawnadds6 = true;
        public static bool spawnadds7 = true;
        public static bool spawnadds8 = true;
        public static bool spawnadds9 = true;
        public static bool spawnadds10 = true;
        public static bool spawnadds11 = true;
        public static bool spawnadds12 = true;
        public static bool spawnadds13 = true;
        public static bool spawnadds14 = true;
        public static bool spawnadds15 = true;
        public static bool spawnadds16 = true;
        public static bool spawnadds17 = true;
        public static bool spawnadds18 = true;
        public static bool spawnadds19 = true;
        #endregion spawnadds checks

        #region reset checks
        public void ResetChecks()
        {
            spawnadds1 = true;
            spawnadds2 = true;
            spawnadds3 = true;
            spawnadds4 = true;
            spawnadds5 = true;
            spawnadds6 = true;
            spawnadds7 = true;
            spawnadds8 = true;
            spawnadds9 = true;
            spawnadds10 = true;
            spawnadds11 = true;
            spawnadds12 = true;
            spawnadds13 = true;
            spawnadds14 = true;
            spawnadds15 = true;
            spawnadds16 = true;
            spawnadds17 = true;
            spawnadds18 = true;
            spawnadds19 = true;
        }
        #endregion reset checks
        public override void Think()
        {
            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                ResetChecks();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                ResetChecks();
            }

            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is SBAddsBrain)
                    {
                        npc.RemoveFromWorld();
                        ResetChecks();
                    }
                }
            }
            int health = Body.HealthPercent / Body.Charisma;
            if (Body.TargetObject != null && Body.InCombat)
            {
                #region check boss health and spawn adds
                if (Body.HealthPercent <= 95 && Body.HealthPercent > 90 && spawnadds1==true)
                {
                    Spawn();
                    spawnadds1 = false;
                }
                if (Body.HealthPercent <= 90 && Body.HealthPercent > 85 && spawnadds2 == true)
                {
                    Spawn();
                    spawnadds2 = false;
                }
                if (Body.HealthPercent <= 85 && Body.HealthPercent > 80 && spawnadds3 == true)
                {
                    Spawn();
                    spawnadds3 = false;
                }
                if (Body.HealthPercent <= 80 && Body.HealthPercent > 75 && spawnadds4 == true)
                {
                    Spawn();
                    spawnadds4 = false;
                }
                if (Body.HealthPercent <= 75 && Body.HealthPercent > 70 && spawnadds5 == true)
                {
                    Spawn();
                    spawnadds5 = false;
                }
                if (Body.HealthPercent <= 70 && Body.HealthPercent > 65 && spawnadds6 == true)
                {
                    Spawn();
                    spawnadds6 = false;
                }
                if (Body.HealthPercent <= 65 && Body.HealthPercent > 60 && spawnadds7 == true)
                {
                    Spawn();
                    spawnadds7 = false;
                }
                if (Body.HealthPercent <= 60 && Body.HealthPercent > 55 && spawnadds8 == true)
                {
                    Spawn();
                    spawnadds8 = false;
                }
                if (Body.HealthPercent <= 55 && Body.HealthPercent > 50 && spawnadds9 == true)
                {
                    Spawn();
                    spawnadds9 = false;
                }
                if (Body.HealthPercent <= 50 && Body.HealthPercent > 45 && spawnadds10 == true)
                {
                    Spawn();
                    spawnadds10 = false;
                }
                if (Body.HealthPercent <= 45 && Body.HealthPercent > 40 && spawnadds11 == true)
                {
                    Spawn();
                    spawnadds11 = false;
                }
                if (Body.HealthPercent <= 40 && Body.HealthPercent > 35 && spawnadds12 == true)
                {
                    Spawn();
                    spawnadds12 = false;
                }
                if (Body.HealthPercent <= 35 && Body.HealthPercent > 30 && spawnadds13 == true)
                {
                    Spawn();
                    spawnadds13 = false;
                }
                if (Body.HealthPercent <= 30 && Body.HealthPercent > 25 && spawnadds14 == true)
                {
                    Spawn();
                    spawnadds14 = false;
                }
                if (Body.HealthPercent <= 25 && Body.HealthPercent > 20 && spawnadds15 == true)
                {
                    Spawn();
                    spawnadds15 = false;
                }
                if (Body.HealthPercent <= 20 && Body.HealthPercent > 15 && spawnadds16 == true)
                {
                    Spawn();
                    spawnadds16 = false;
                }
                if (Body.HealthPercent <= 15 && Body.HealthPercent > 10 && spawnadds17 == true)
                {
                    Spawn();
                    spawnadds17 = false;
                }
                if (Body.HealthPercent <= 10 && Body.HealthPercent > 5 && spawnadds18 == true)
                {
                    Spawn();
                    spawnadds18 = false;
                }
                if (Body.HealthPercent <= 5 && Body.HealthPercent > 1 && spawnadds19 == true)
                {
                    Spawn();
                    spawnadds19 = false;
                }
                #endregion check boss health and spawn adds
            }

            base.Think();
        }
        public void Spawn()
        {
            for (int i = 0; i < Util.Random(15, 20); i++) // Spawn 15-20 adds
            {
                SBAdds Add = new SBAdds();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.IsWorthReward = false;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
        }
    }
}

/////////////////////////////////////////Minions here//////////////////////////////////
namespace DOL.GS
{
    public class SBAdds : GameNPC
    {
        public SBAdds() : base() { }
        public static GameNPC SI_Gnatants = new GameNPC();
        public override int MaxHealth
        {
            get { return 800; }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override bool AddToWorld()
        {
            Model = 904;
            Name = "Broodmother's swarm";
            MeleeDamageType = eDamageType.Slash;
            RespawnInterval = -1;
            MaxDistance = 2500;
            TetherRange = 2000;
            Strength = 100;
            IsWorthReward = false;//worth no reward
            Size = (byte)Util.Random(50, 60);
            Level = (byte)Util.Random(56, 59);
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            Realm = 0;
            SBAddsBrain adds = new SBAddsBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            base.Die(null);//null to not gain experience
        }
    }
}
namespace DOL.AI.Brain
{
    public class SBAddsBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public SBAddsBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            Body.IsWorthReward = false;
            foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if (player != null && player.IsAlive)
                {
                    if (player.CharacterClass.ID is 48 or 47 or 42 or 46)//bard,druid,menta,warden
                    {
                        if (Body.TargetObject != player)
                        {
                            Body.TargetObject = player;
                            Body.StartAttack(player);
                        }
                    }
                    else
                    {
                        Body.TargetObject = player;
                        Body.StartAttack(player);
                    }
                }
            }
            base.Think();
        }
    }
}