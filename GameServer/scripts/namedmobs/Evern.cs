using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Evern : GameEpicBoss
    {
        public Evern() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;
            if(IsReturningToSpawnPoint && keyName == GS.Abilities.DamageImmunity)
                return true;
            return base.HasAbility(keyName);
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (IsOutOfTetherRange)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
                        damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
                        damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System,
                                eChatLoc.CL_ChatWindow);
                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else //take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160628);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            EvernBrain.spawnfairy = false;
            //Idle = false;
            MaxSpeedBase = 300;

            Faction = FactionMgr.GetFactionByID(81);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(81));
            EvernBrain sbrain = new EvernBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Evern", 200, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Evern not found, creating it...");

                log.Warn("Initializing Evern...");
                Evern CO = new Evern();
                CO.Name = "Evern";
                CO.Model = 400;
                CO.Realm = 0;
                CO.Level = 75;
                CO.Size = 120;
                CO.CurrentRegionID = 200; //OF breifine

                CO.Strength = 5;
                CO.Intelligence = 150;
                CO.Piety = 150;
                CO.Dexterity = 200;
                CO.Constitution = 100;
                CO.Quickness = 125;
                CO.Empathy = 300;
                CO.BodyType = (ushort) NpcTemplateMgr.eBodyType.Magical;
                CO.MeleeDamageType = eDamageType.Slash;

                CO.X = 429840;
                CO.Y = 380396;
                CO.Z = 2328;
                CO.MaxDistance = 3500;
                CO.TetherRange = 3800;
                CO.MaxSpeedBase = 250;
                CO.Heading = 4059;

                EvernBrain ubrain = new EvernBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 600;
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Evern exist ingame, remove it and restart server if you want to add by script code.");
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc == null) break;
                if (npc.Brain is EvernFairyBrain)
                {
                    if (npc.RespawnInterval == -1)
                        npc.Die(npc); //we kill all fairys if boss die
                }
            }
            base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
    public class EvernBrain : EpicBossBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public EvernBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 1500;
        }
        public static bool spawnfairy = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
                spawnfairy = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4500))
                    {
                        if (npc == null) break;
                        if (npc.Brain is EvernFairyBrain)
                        {
                            if (npc.RespawnInterval == -1)
                                npc.Die(npc); //we kill all fairys if boss reset
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (Body.IsAlive && HasAggro)
            {
                RemoveAdds = false;
                if (Body.TargetObject != null)
                {
                    if (Body.HealthPercent < 100)
                    {
                        if (spawnfairy == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoSpawn), Util.Random(10000, 20000));
                            spawnfairy = true;
                        }
                    }
                }
            }
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
            {
                Body.Health = Body.MaxHealth;

                foreach (GameNPC npc in Body.GetNPCsInRadius(4500))
                {
                    if (npc == null) break;
                    if (npc.Brain is EvernFairyBrain)
                    {
                        if (npc.RespawnInterval == -1)
                            npc.Die(npc); //we kill all fairys if boss reset
                    }
                }
            }
            base.Think();
        }
        private int DoSpawn(ECSGameTimer timer)
        {
            Spawn();
            spawnfairy = false;
            return 0;
        }
        public void Spawn() // We define here adds
        {
            for (int i = 0; i < Util.Random(2, 5); i++)
            {
                EvernFairy Add = new EvernFairy();
                Add.X = 429764 + Util.Random(-100, 100);
                Add.Y = 380398 + Util.Random(-100, 100);
                Add.Z = 2726;
                Add.CurrentRegionID = 200;
                Add.Heading = 3889;
                Add.AddToWorld();
            }
        }
    }
}
///////////////////////////////////Evern Fairys//////////////////////////////////23stones
namespace DOL.GS
{
    public class EvernFairy : GameNPC
    {
        public EvernFairy() : base()
        {
        }
        public override long ExperienceValue => 0;
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 400;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 25; // dmg reduction for melee dmg
                case eDamageType.Crush: return 25; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 25; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }
        public override int MaxHealth
        {
            get { return 2000; }
        }
        public override void DropLoot(GameObject killer)
        {
        }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 120; }
        public override bool AddToWorld()
        {
            Model = 603;
            Name = "Wraith Fairy";
            MeleeDamageType = eDamageType.Thrust;
            RespawnInterval = -1;
            Size = 50;
            Flags = eFlags.FLYING;
            Faction = FactionMgr.GetFactionByID(81);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(81));
            Level = (byte) Util.Random(50, 55);
            Gender = eGender.Female;
            EvernFairyBrain adds = new EvernFairyBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class EvernFairyBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public EvernFairyBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 0;
        }
        private protected void IsAtPillar(IPoint3D target)
        {
            Body.MaxSpeedBase = 0;
            Body.StopMovingAt(target);
            Body.IsReturningHome = false;
            Body.CancelWalkToSpawn();
            foreach(GameNPC evern in Body.GetNPCsInRadius(2500))
            {
                if(evern != null)
                {
                    if(evern.IsAlive && evern.Brain is EvernBrain && evern.HealthPercent < 100)
                    {
                        Body.TargetObject = evern;
                        Body.TurnTo(evern);
                        Body.CastSpell(Fairy_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                    }
                }
            }
        }
        public override void Think()
        {
            Point3D point1 = new Point3D(430333, 379905, 2463);
            Point3D point2 = new Point3D(429814, 379895, 2480);
            Point3D point3 = new Point3D(429309, 379894, 2454);
            Point3D point4 = new Point3D(430852, 380150, 2444);
            Point3D point5 = new Point3D(428801, 380156, 2428);
            Point3D point6 = new Point3D(430854, 380680, 2472);
            Point3D point7 = new Point3D(429186, 380418, 2478);
            Point3D point8 = new Point3D(430462, 380411, 2443);
            Point3D point9 = new Point3D(430468, 430468, 2474);
            Point3D point10 = new Point3D(429057, 380920, 2452);

            if (Body.IsAlive)
            {
                #region PickRandomLandSpot
                switch (Util.Random(1, 10))
                {
                    case 1: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point1, 80); break;
                    case 2: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point2, 80); break;
                    case 3: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point3, 80); break;
                    case 4: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point4, 80); break;
                    case 5: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point5, 80); break;
                    case 6: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point6, 80); break;
                    case 7: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point7, 80); break;
                    case 8: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point8, 80); break;
                    case 9: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point9, 80); break;
                    case 10: if (!Body.IsMoving && !HasAggro) Body.WalkTo(point10, 80); break;
                }

                if (Body.IsWithinRadius(point1, 15)) IsAtPillar(point1);
                if (Body.IsWithinRadius(point2, 15)) IsAtPillar(point1);
                if (Body.IsWithinRadius(point3, 15)) IsAtPillar(point3);
                if (Body.IsWithinRadius(point4, 15)) IsAtPillar(point4);
                if (Body.IsWithinRadius(point5, 15)) IsAtPillar(point5);
                if (Body.IsWithinRadius(point6, 15)) IsAtPillar(point6);
                if (Body.IsWithinRadius(point7, 15)) IsAtPillar(point7);
                if (Body.IsWithinRadius(point8, 15)) IsAtPillar(point8);
                if (Body.IsWithinRadius(point9, 15)) IsAtPillar(point9);
                if (Body.IsWithinRadius(point10, 15)) IsAtPillar(point10);
                #endregion
            }
            base.Think();
        }
        #region Spells: Fairy Heal
        private Spell m_Fairy_Heal;
        private Spell Fairy_Heal
        {
            get
            {
                if (m_Fairy_Heal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 4858;
                    spell.Icon = 4858;
                    spell.TooltipId = 4858;
                    spell.Value = 1000;
                    spell.Name = "Heal";
                    spell.Range = 2500;
                    spell.SpellID = 11891;
                    spell.Target = "Realm";
                    spell.Type = "Heal";
                    m_Fairy_Heal = new Spell(spell, 70);
                    spell.Uninterruptible = true;
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Fairy_Heal);
                }
                return m_Fairy_Heal;
            }
        }
        #endregion
    }
}