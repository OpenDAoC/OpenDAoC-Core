using System;
using System.Collections.Generic;
using System.Numerics;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using OpenDAoC.Pathing;
using static DOL.GS.Pathfinder;

namespace DOL.GS
{
    public class LordSanguis : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public LordSanguis()
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

        public override int MaxHealth
        {
            get { return 100000; }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override int MeleeAttackRange => 450;
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
            Spawn_Lich_Lord = false;
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive)
                    {
                        if (npc.Brain is LichLordSanguisBrain)
                            npc.RemoveFromWorld();
                    }
                }
            }
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163412);
            LoadTemplate(npcTemplate);

            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            LordSanguisBrain sbrain = new LordSanguisBrain();
            SetOwnBrain(sbrain);
            base.AddToWorld();
            return true;
        }

        public override void ProcessDeath(GameObject killer)
        {
            if (Spawn_Lich_Lord == false)
            {
                BroadcastMessage(String.Format(this.Name + " comes back to life as Lich Lord Sanguis!"));
                SpawnMages();
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnLich), 6000);
                Spawn_Lich_Lord = true;
            }

            base.ProcessDeath(killer);
        }

        public static bool Spawn_Lich_Lord = false;

        public int SpawnLich(ECSGameTimer timer)
        {
            LichLordSanguis Add = new LichLordSanguis();
            Add.X = X;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegion = CurrentRegion;
            Add.Heading = Heading;
            Add.AddToWorld();
            return 0;
        }

        public void SpawnMages()
        {
            Vector3 position = new(X, Y, Z);
            Zone zone = CurrentZone;
            bool usePathfinding = zone != null && zone.IsPathfindingEnabled;
            EDtPolyFlags[] filters = usePathfinding ? PathfindingProvider.Instance.DefaultFilters : null;

            for (int i = 0; i < Util.Random(2, 4); i++) // Spawn 2-4 mages
            {
                // Pick positions on the navmesh whenever possible, so that mages can't spawn inside walls.
                Vector3 spawnPoint = usePathfinding ?
                    PathfindingProvider.Instance.GetRandomPoint(zone, position, 80, filters) ?? position :
                    new(X + Util.Random(-50, 80), Y + Util.Random(-50, 80), Z);

                BloodMage Add = new BloodMage();
                Add.X = (int) spawnPoint.X;
                Add.Y = (int) spawnPoint.Y;
                Add.Z = (int) spawnPoint.Z;
                Add.CurrentRegion = CurrentRegion;
                Add.Heading = Heading;
                Add.AddToWorld();
            }
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lord Sanguis", 60, (eRealm) 0);
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
                CO.CurrentRegionID = 60; //caer sidi

                CO.MeleeDamageType = eDamageType.Crush;
                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

                CO.X = 34080;
                CO.Y = 32919;
                CO.Z = 14518;
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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LordSanguisBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        private bool RemoveAdds = false;
        private readonly List<GameNPC> _mages = new List<GameNPC>();
        private int AliveMageCount
        {
            get
            {
                _mages.RemoveAll(npc => npc == null || !npc.IsAlive || npc.ObjectState is not GameObject.eObjectState.Active);
                return _mages.Count;
            }
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                _mages.Clear();
                if (!RemoveAdds)
                {
                    foreach (GameNPC mages in Body.GetNPCsInRadius(5000))
                    {
                        if (mages != null)
                        {
                            if (mages.IsAlive && mages.Brain is BloodMageBrain)
                                mages.RemoveFromWorld();
                        }
                    }
                    RemoveAdds= true;
                }
            }
            if (Body.TargetObject != null && HasAggro)
            {
                RemoveAdds = false;
                if (Util.Chance(10))
                {
                    if (AliveMageCount < 2)
                        SpawnMages();
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
            _mages.Add(Add);
        }
    }
}

///////////////////////////////////////////////////////Lich Lord Sanguis/////////////////////////////////////
namespace DOL.GS
{
    public class LichLordSanguis : GameEpicBoss
    {
        public LichLordSanguis() : base()
        {
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
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163267);
            LoadTemplate(npcTemplate);
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
            LichLordSanguisBrain.set_flag = false;

            TetherRange = 2000;
            Size = 100;
            Level = 81;
            MaxSpeedBase = 250;

            Faction = FactionMgr.GetFactionByID(64);
            BodyType = 8;
            Realm = eRealm.None;
            LichLordSanguisBrain.set_flag = false;
            LichLordSanguisBrain adds = new LichLordSanguisBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }

        public override void Die(GameObject killer) //on kill generate orbs
        {
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class LichLordSanguisBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LichLordSanguisBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public static bool set_flag = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.GHOST;
                set_flag = false;
            }
            if (Body.HealthPercent <= 5)
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
    public class BloodMage : GameNPC //thrust resist
    {
        private const int DESPAWN_DELAY = 180000; // Death-spawned adds despawn if they're left alone.
        private const int DESPAWN_RETRY_INTERVAL = 30000;

        public BloodMage() : base()
        {
        }



        public override int MeleeAttackRange => 350;

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
            get { return 8000; }
        }

        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 150; }   
        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 798, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 67);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 67);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 96, 67);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 442, 67);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            Model = (byte) Util.Random(61, 68);
            IsCloakHoodUp = true;
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            Name = "blood mage";
            RespawnInterval = -1;

            TetherRange = 3000;
            RoamingRange = 120;
            Size = 50;
            Level = 70;
            MaxSpeedBase = 200;

            Faction = FactionMgr.GetFactionByID(64);
            BodyType = 6;
            Realm = eRealm.None;

            BloodMageBrain adds = new BloodMageBrain();
            SetOwnBrain(adds);
            new ECSGameTimer(this, Despawn, DESPAWN_DELAY);
            base.AddToWorld();
            return true;
        }

        private int Despawn(ECSGameTimer timer)
        {
            if (!IsAlive)
                return 0;

            // Don't despawn mid fight.
            if (InCombat || Brain is StandardMobBrain { HasAggro: true })
                return DESPAWN_RETRY_INTERVAL;

            RemoveFromWorld();
            return 0;
        }
    }
}

namespace DOL.AI.Brain
{
    public class BloodMageBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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