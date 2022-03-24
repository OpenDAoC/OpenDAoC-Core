using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.ServerRules;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;
using Timer = System.Timers.Timer;
using System.Timers;

namespace DOL.GS
{
    public class Torst : GameEpicBoss
    {
        public Torst() : base() { }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 85;// dmg reduction for melee dmg
                case eDamageType.Crush: return 85;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 85;// dmg reduction for melee dmg
                default: return 55;// dmg reduction for rest resists
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
        public override void Die(GameObject killer)//on kill generate orbs
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,ServerProperties.Properties.EPIC_ORBS);//5k orbs for every player in group
                }
            }
            base.Die(killer);
        }

        public override void WalkToSpawn()
        {
            if (this.CurrentRegionID == 160)//if region is tuscaran glacier
            {
                if (IsAlive)
                    return;
            }
            base.WalkToSpawn();
        }
        public override bool AddToWorld()
        {
            Strength = 5;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 100;
            Intelligence = 100;
            Empathy = 280;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            MaxSpeedBase = 250;
            Flags = eFlags.FLYING;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

            TorstBrain sbrain = new TorstBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Torst", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Torst not found, creating it...");

                log.Warn("Initializing Torst...");
                Torst TG = new Torst();
                TG.Name = "Torst";
                TG.Model = 696;
                TG.Realm = 0;
                TG.Level = 85;
                TG.Size = 90;
                TG.CurrentRegionID = 160;//tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
                TG.Flags = eFlags.FLYING;

                TG.X = 50778;
                TG.Y = 35997;
                TG.Z = 16154;
                TG.Heading = 3154;
                TorstBrain ubrain = new TorstBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Torst exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class TorstBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TorstBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2000;
        }
        public List<GamePlayer> PlayersToAttack= new List<GamePlayer>();
        public static bool point1check = false;
        public static bool point2check = false;
        public static bool point3check = false;
        public static bool point4check = false;
        public static bool point5check = false;
        public static bool point6check = false;
        public static bool walkback = false;
        public void TorstFlyingPath()
        {
            Point3D point1 = new Point3D();
            point1.X = 51166; point1.Y = 37442; point1.Z = 17331;
            Point3D point2 = new Point3D();
            point2.X = 53201; point2.Y = 39956; point2.Z = 16314;
            Point3D point3 = new Point3D();
            point3.X = 55178; point3.Y = 38616; point3.Z = 17901;
            Point3D point4 = new Point3D();
            point4.X = 54852; point4.Y = 36185; point4.Z = 17859;
            Point3D point5 = new Point3D();
            point5.X = 53701; point5.Y = 35635; point5.Z = 17859;
            Point3D point6 = new Point3D();
            point6.X = 52118; point6.Y = 36114; point6.Z = 17265;
            Point3D spawn = new Point3D();
            spawn.X = 50897; spawn.Y = 36006; spawn.Z = 16659;

            if (!Body.InCombat && !HasAggro)
            {
                if (Body.CurrentRegionID == 160)//tuscaran glacier
                {
                    if (!Body.IsWithinRadius(point1, 30) && point1check == false)
                    {
                        Body.WalkTo(point1, 200);
                    }
                    else
                    {
                        point1check = true;
                        walkback = false;
                        if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
                        {
                            Body.WalkTo(point2, 200);
                        }
                        else
                        {
                            point2check = true;
                            if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true && point3check == false)
                            {
                                Body.WalkTo(point3, 200);
                            }
                            else
                            {
                                point3check = true;
                                if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true && point3check == true && point4check == false)
                                {
                                    Body.WalkTo(point4, 200);
                                }
                                else
                                {
                                    point4check = true;
                                    if (!Body.IsWithinRadius(point5, 30) && point1check == true && point2check == true && point3check == true && point4check == true && point5check == false)
                                    {
                                        Body.WalkTo(point5, 200);
                                    }
                                    else
                                    {
                                        point5check = true;
                                        if (!Body.IsWithinRadius(point6, 30) && point1check == true && point2check == true && point3check == true && point4check == true && point5check == true && point6check == false)
                                        {
                                            Body.WalkTo(point6, 200);
                                        }
                                        else
                                        {
                                            point6check = true;
                                            if (!Body.IsWithinRadius(spawn, 30) && point1check == true && point2check == true && point3check == true && point4check == true && point5check == true && point6check == true && walkback == false)
                                            {
                                                Body.WalkTo(spawn, 200);
                                            }
                                            else
                                            {
                                                walkback = true;
                                                point1check = false;
                                                point2check = false;
                                                point3check = false;
                                                point4check = false;
                                                point5check = false;
                                                point6check = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else//not TG
                {
                    //mob will not roam
                }
            }
        }
        public override void Think()
        {
            TorstFlyingPath();
            if (HasAggressionTable() && Body.IsWithinRadius(Body.TargetObject,Body.AttackRange) && Body.InCombat)
            {
                Body.Flags = 0;//dont fly
            }
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.FLYING;//fly
            }

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat && HasAggro)
            {
                foreach (GamePlayer gamePlayer in Body.GetPlayersInRadius(1500))
                {
                    if (gamePlayer != null)
                    {
                        if (gamePlayer.IsAlive)
                        {
                            if (gamePlayer.Client.Account.PrivLevel == 1)
                            {
                                if (!PlayersToAttack.Contains(gamePlayer))
                                {
                                    PlayersToAttack.Add(gamePlayer);
                                }
                            }
                        }
                    }
                }
                PickNotRottedTarget();
                if (Body.TargetObject != null)
                {
                    if (Util.Chance(10))
                    {
                        Body.CastSpell(TorstRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                    if (Util.Chance(15))
                    {
                        Body.CastSpell(TorstDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
            }
            base.Think();
        }
        public GameLiving randomtarget = null;
        public GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public void PickNotRottedTarget()
        {
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                if (Body.TargetObject != null)
                {
                    GameLiving target = Body.TargetObject as GameLiving;//mob target
                    RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];//mob next random target
                    if (target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff))//if target got root
                    {                       
                        Body.StopAttack();
                        m_aggroTable.Clear();//clear aggro list
                        if (RandomTarget != null)
                        {
                            m_aggroTable.Add(RandomTarget, 50);//add to aggro list our new random target
                            Body.StartAttack(RandomTarget);
                        }
                    }
                }
            }
        }
        private Spell m_TorstRoot;
        private Spell TorstRoot
        {
            get
            {
                if (m_TorstRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 277;
                    spell.Icon = 277;
                    spell.Duration = 60;
                    spell.Value = 99;
                    spell.Name = "Torst Root";
                    spell.TooltipId = 277;
                    spell.SpellID = 11742;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_TorstRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TorstRoot);
                }
                return m_TorstRoot;
            }
        }

        public Spell m_TorstDD;
        public Spell TorstDD
        {
            get
            {
                if (m_TorstDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(25,45);
                    spell.ClientEffect = 228;
                    spell.Icon = 208;
                    spell.TooltipId = 479;
                    spell.Damage = 550;
                    spell.Range = 1500;
                    spell.Radius = 400;
                    spell.SpellID = 11743;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamageNoVariance";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_TorstDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TorstDD);
                }
                return m_TorstDD;
            }
        }
    }
}
////////////////////////////////////////////////////////////Hurika///////////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class Hurika : GameEpicBoss
    {
        public Hurika() : base() { }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 95;// dmg reduction for melee dmg
                case eDamageType.Crush: return 95;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 95;// dmg reduction for melee dmg
                default: return 75;// dmg reduction for rest resists
            }
        }
        public override void WalkToSpawn()
        {
            if (this.CurrentRegionID == 160)//if region is tuscaran glacier
            {
                if (IsAlive)
                    return;
            }
            base.WalkToSpawn();
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
        public override void Die(GameObject killer)//on kill generate orbs
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,ServerProperties.Properties.EPIC_ORBS);//5k orbs for every player in group
                }
            }
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162284);
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
            Flags = eFlags.FLYING;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

            HurikaBrain sbrain = new HurikaBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Hurika", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Hurika not found, creating it...");

                log.Warn("Initializing Hurika...");
                Hurika TG = new Hurika();
                TG.Name = "Hurika";
                TG.PackageID = "Hurika";
                TG.Model = 696;
                TG.Realm = 0;
                TG.Level = 85;
                TG.Size = 90;
                TG.CurrentRegionID = 160;//tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
                TG.Flags = eFlags.FLYING;

                TG.X = 50781;
                TG.Y = 35680;
                TG.Z = 16154;
                TG.Heading = 3154;
                HurikaBrain ubrain = new HurikaBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Hurika exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class HurikaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public HurikaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2000;
        }
        public static bool point_1 = false;
        public static bool point_2 = false;
        public static bool point_3 = false;
        public static bool point_4 = false;
        public static bool point_5 = false;
        public void HurikaFlyingPath()
        {
            Point3D point1 = new Point3D();
            point1.X = 54652; point1.Y = 36348; point1.Z = 18279;
            Point3D point2 = new Point3D();
            point2.X = 55113; point2.Y = 38549; point2.Z = 16679;
            Point3D point3 = new Point3D();
            point3.X = 53370; point3.Y = 40527; point3.Z = 16268;
            Point3D point4 = new Point3D();
            point4.X = 51711; point4.Y = 38978; point4.Z = 17130;
            Point3D point5 = new Point3D();
            point5.X = 51519; point5.Y = 37213; point5.Z = 17046;

            if (!Body.InCombat && !HasAggro)
            {
                if (Body.CurrentRegionID == 160)//tuscaran glacier
                {
                    if (!Body.IsWithinRadius(point1, 30) && point_1 == false)
                    {
                        Body.WalkTo(point1, 200);
                    }
                    else
                    {
                        point_1 = true;
                        point_5 = false;
                        if (!Body.IsWithinRadius(point2, 30) && point_1 == true && point_2 == false)
                        {
                            Body.WalkTo(point2, 200);
                        }
                        else
                        {
                            point_2 = true;
                            if (!Body.IsWithinRadius(point3, 30) && point_1 == true && point_2 == true && point_3 == false)
                            {
                                Body.WalkTo(point3, 200);
                            }
                            else
                            {
                                point_3 = true;
                                if (!Body.IsWithinRadius(point4, 30) && point_1 == true && point_2 == true && point_3 == true && point_4 == false)
                                {
                                    Body.WalkTo(point4, 200);
                                }
                                else
                                {
                                    point_4 = true;
                                    if (!Body.IsWithinRadius(point5, 30) && point_1 == true && point_2 == true && point_3 == true && point_4 == true && point_5 == false)
                                    {
                                        Body.WalkTo(point5, 200);
                                    }
                                    else
                                    {
                                        point_5 = true;
                                        point_1 = false;
                                        point_2 = false;
                                        point_3 = false;
                                        point_4 = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else//not TG
                {
                    //mob will not roam
                }
            }
        }
        public override void Think()
        {
            HurikaFlyingPath();
            if (HasAggressionTable() && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange) && Body.InCombat)
            {
                Body.Flags = 0;//dont fly
            }
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.FLYING;//fly
            }
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat || HasAggro || Body.AttackState == true)
            {
            }
            base.Think();
        }
    }
}