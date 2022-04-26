using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class Otrygg : GameEpicBoss
    {
        public Otrygg() : base()
        {
        }

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 45; // dmg reduction for melee dmg
                case eDamageType.Crush: return 45; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 45; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
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
            if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc == null) continue;
                if (!npc.IsAlive) continue;
                if (npc.Brain is OtryggAddBrain)
                {
                    npc.Die(this);
                }
            }

            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159451);
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

            OtryggAdd.PetsCount = 0;
            OtryggBrain sbrain = new OtryggBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Council Otrygg", 160, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Council Otrygg not found, creating it...");

                log.Warn("Initializing Council Otrygg...");
                Otrygg TG = new Otrygg();
                TG.Name = "Council Otrygg";
                TG.PackageID = "Council Otrygg";
                TG.Model = 918;
                TG.Realm = 0;
                TG.Level = 77;
                TG.Size = 70;
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval =
                    ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                    60000; //1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

                TG.X = 23958;
                TG.Y = 42241;
                TG.Z = 13430;
                TG.Heading = 2084;
                OtryggBrain ubrain = new OtryggBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn(
                    "Council Otrygg exist in game, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class OtryggBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OtryggBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
                foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
                {
                    if (npc == null) continue;
                    if (!npc.IsAlive) continue;
                    if (npc.Brain is OtryggAddBrain)
                    {
                        npc.Die(Body);
                    }
                }
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (Body.InCombat || HasAggro || Body.AttackState == true)
            {
                if (OtryggAdd.PetsCount is < 15 and >= 0)
                {
                    SpawnPetsMore();
                }
            }

            base.Think();
        }

        public static bool IsPulled = false;

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                SpawnPets();
                IsPulled = true;
            }

            base.OnAttackedByEnemy(ad);
        }

        public void SpawnPets()
        {
            for (int i = 0; i < 14; i++) // Spawn 15 pets
            {
                OtryggAdd Add = new OtryggAdd();
                Add.X = Body.SpawnPoint.X + Util.Random(-50, 80);
                Add.Y = Body.SpawnPoint.Y + Util.Random(-50, 80);
                Add.Z = Body.SpawnPoint.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
                OtryggAdd.PetsCount++;
            }
        }

        public void SpawnPetsMore()
        {
            OtryggAdd Add = new OtryggAdd();
            Add.X = Body.SpawnPoint.X + Util.Random(-50, 80);
            Add.Y = Body.SpawnPoint.Y + Util.Random(-50, 80);
            Add.Z = Body.SpawnPoint.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
            OtryggAdd.PetsCount++;
        }
    }
}

/////////////////////////////////////////////////////////////Adds//////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class OtryggAdd : GameNPC
    {
        public OtryggAdd() : base()
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
            get { return 6000; }
        }

        public override void Die(GameObject killer)
        {
            PetsCount--;
            base.Die(killer);
        }

        public override void DropLoot(GameObject killer) //no loot
        {
        }

        public static int PetsCount = 0;

        public override bool AddToWorld()
        {
            Model = 126;
            MeleeDamageType = eDamageType.Cold;
            Name = "summoned pet";
            RespawnInterval = -1;

            MaxDistance = 5500;
            TetherRange = 5800;
            RoamingRange = 120;
            Size = 50;
            Level = 68;
            MaxSpeedBase = 250;

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            BodyType = 6;
            Realm = eRealm.None;

            Strength = 60;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            OtryggAddBrain adds = new OtryggAddBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class OtryggAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OtryggAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1600; //big aggro range
        }

        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc == null) continue;
                    if (!npc.IsAlive) continue;
                    if (npc.Brain is OtryggAddBrain brain)
                    {
                        AddAggroListTo(brain); //if one pet aggro all will aggro
                    }
                }
            }

            base.Think();
        }
    }
}