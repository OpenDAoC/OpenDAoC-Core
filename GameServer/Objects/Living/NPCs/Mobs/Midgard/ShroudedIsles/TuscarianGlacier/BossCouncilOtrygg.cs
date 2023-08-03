﻿using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class BossCouncilOtrygg : GameEpicBoss
    {
        public BossCouncilOtrygg() : base()
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
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
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
        public override void Die(GameObject killer)
        {
            foreach (GameNpc npc in GetNPCsInRadius(8000))
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
            RespawnInterval = ServerProperties.ServerProperties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            OtryggAdd.PetsCount = 0;
            OtryggBrain sbrain = new OtryggBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class OtryggBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OtryggBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
                if (!RemoveAdds)
                {
                    foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
                    {
                        if (npc == null) continue;
                        if (!npc.IsAlive) continue;
                        if (npc.Brain is OtryggAddBrain)
                        {
                            npc.Die(Body);
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if (OtryggAdd.PetsCount is < 8 and >= 0)
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
            for (int i = 0; i < 8; i++) // Spawn 8 pets
            {
                OtryggAdd Add = new OtryggAdd();
                Add.X = Body.SpawnPoint.X + UtilCollection.Random(-50, 80);
                Add.Y = Body.SpawnPoint.Y + UtilCollection.Random(-50, 80);
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
            Add.X = Body.SpawnPoint.X + UtilCollection.Random(-50, 80);
            Add.Y = Body.SpawnPoint.Y + UtilCollection.Random(-50, 80);
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
    public class OtryggAdd : GameNpc
    {
        public OtryggAdd() : base()
        {
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 20;// dmg reduction for melee dmg
                case EDamageType.Crush: return 20;// dmg reduction for melee dmg
                case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
                default: return 20;// dmg reduction for rest resists
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 50 * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
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
            get { return 10000; }
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
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 100; }
        public override short Strength { get => base.Strength; set => base.Strength = 50; }
        #endregion
        public override bool AddToWorld()
        {
            Model = (byte)UtilCollection.Random(241,244);
            MeleeDamageType = EDamageType.Crush;
            Name = "summoned pet";
            RespawnInterval = -1;

            RoamingRange = 120;
            Size = 50;
            Level = 62;
            MaxSpeedBase = 250;

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            BodyType = 6;
            Realm = ERealm.None;

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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OtryggAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1600; //big aggro range
        }
        public override void Think()
        {
            if (HasAggro && Body.TargetObject != null)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc == null) continue;
                    if (!npc.IsAlive) continue;
                    if (npc.Brain is OtryggAddBrain brain)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro && Body != npc && Body.Brain != npc.Brain && target != null && target.IsAlive)
                            brain.AddToAggroList(target, 10); //if one pet aggro all will aggro
                    }
                }
            }
            base.Think();
        }
    }
}