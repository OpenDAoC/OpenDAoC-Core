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
    public class LordSanguis : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LordSanguis()
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
                return 20000;
            }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
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
        public override bool AddToWorld()
        {
            Spawn_Lich_Lord = false;
            foreach(GameNPC npc in this.GetNPCsInRadius(4000))
            {
                if(npc != null)
                {
                    if(npc.IsAlive)
                    {
                        if(npc.Brain is LichLordSanguisBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163412);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            LordSanguisBrain sbrain = new LordSanguisBrain();
            SetOwnBrain(sbrain);
            base.AddToWorld();
            return true;

        }
        public override void Die(GameObject killer)
        {
            if(Spawn_Lich_Lord==false)
            {
                BroadcastMessage(String.Format(this.Name + " comes back to life as Lich Lord Sanguis!"));
                SpawnMages();
                new RegionTimer(this, new RegionTimerCallback(SpawnLich), 6000);
                Spawn_Lich_Lord= true;
            }
            base.Die(killer);
        }
        public static bool Spawn_Lich_Lord = false;
        public int SpawnLich(RegionTimer timer)
        {
            LichLordSanguis Add = new LichLordSanguis();
            Add.X = this.X;
            Add.Y = this.Y;
            Add.Z = this.Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = this.Heading;
            Add.AddToWorld();
            return 0;
        }
        public void SpawnMages()
        {
            for (int i = 0; i < Util.Random(2, 4); i++) // Spawn 2-4 mages
            {
                BloodMage Add = new BloodMage();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lord Sanguis", 60, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Lord Sanguis  not found, creating it...");

                log.Warn("Initializing Lord Sanguis...");
                LordSanguis CO = new LordSanguis();
                CO.Name = "Lord Sanguis";
                CO.Model = 952;
                CO.Realm = 0;
                CO.Level = 81;
                CO.Size = 100;
                CO.CurrentRegionID = 60;//caer sidi

                CO.MeleeDamageType = eDamageType.Crush;
                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

                CO.X = 34080;
                CO.Y = 32919;
                CO.Z = 14518;
                CO.MaxDistance = 2000;
                CO.TetherRange = 2000;
                CO.MaxSpeedBase = 250;
                CO.Heading = 4079;

                LordSanguisBrain ubrain = new LordSanguisBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Lord Sanguis exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class LordSanguisBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public LordSanguisBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
            }
            if (Body.InCombat && HasAggro)
            {
                if(Util.Chance(10))
                {
                    if(BloodMage.MageCount<2)
                    {
                        SpawnMages();
                    }
                }
            }
            base.Think();
        }
        public void SpawnMages()
        {
            BloodMage Add = new BloodMage();
            Add.X = Body.X + Util.Random(-50, 80);
            Add.Y = Body.Y + Util.Random(-50, 80);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }       
    }
}
///////////////////////////////////////////////////////Lich Lord Sanguis/////////////////////////////////////
namespace DOL.GS
{
    public class LichLordSanguis : GameNPC//thrust resist
    {
        public LichLordSanguis() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 75;// dmg reduction for melee dmg
                case eDamageType.Crush: return 0;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 0;// dmg reduction for melee dmg
                default: return 75;// dmg reduction for rest resists
            }
        }      
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163267);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            ParryChance = npcTemplate.ParryChance;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 442, 67);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            Model = 952;
            Flags = eFlags.GHOST;
            Name = "Lich Lord Sanguis";
            ParryChance = 35;
            RespawnInterval = -1;

            MaxDistance = 2000;
            TetherRange = 2000;
            Size = 100;
            Level = 81;
            MaxSpeedBase = 250;

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 8;
            Realm = eRealm.None;
            LichLordSanguisBrain.set_flag = false;
            LichLordSanguisBrain adds = new LichLordSanguisBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
        
        public override void Die(GameObject killer)//on kill generate orbs
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer, 5000);//5k orbs for every player in group
                }
            }
            
            base.Die(killer);
        }
        
    }
}

namespace DOL.AI.Brain
{
    public class LichLordSanguisBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public LichLordSanguisBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public static bool set_flag = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.GHOST;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
            }
            if(Body.HealthPercent <= 10)
            {
                if (set_flag == false)
                {
                    BroadcastMessage(String.Format(Body.Name + " becomes almost untouchable in his last act of agony!"));
                    Body.Flags ^= GameNPC.eFlags.CANTTARGET;
                    set_flag = true;
                }
            }
            base.Think();
        }
    }
}
/////////////////////////////Blood Mages///////////////////////
namespace DOL.GS
{
    public class BloodMage : GameNPC//thrust resist
    {
        public BloodMage() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 4000; }
        }
        public override void Die(GameObject killer)
        {
            --MageCount;
            base.Die(killer);
        }

        public static int MageCount = 0;
        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 798, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141,67);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140,67);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 96,67);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 442,67);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            Model = (byte)Util.Random(61,68);
            IsCloakHoodUp = true;
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            Name = "blood mage";
            RespawnInterval = -1;

            MaxDistance = 2500;
            TetherRange = 3000;
            RoamingRange = 120;
            Size = 50;
            Level = 75;
            MaxSpeedBase = 200;           

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 6;
            Realm = eRealm.None;
            ++MageCount;

            Strength = 25;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            BloodMageBrain adds = new BloodMageBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class BloodMageBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public BloodMageBrain()
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