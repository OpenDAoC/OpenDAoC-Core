using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

#region Torst
namespace DOL.GS
{
    public class Torst : GameEpicBoss
    {
        public Torst() : base()
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
            get { return 200000; }
        }
        public override void WalkToSpawn()
        {
            if (CurrentRegionID == 160) //if region is tuscaran glacier
            {
                if (IsAlive)
                    return;
            }
            base.WalkToSpawn();
        }
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 350; }
        #endregion
        public override bool AddToWorld()
        {
            Name = "Torst";
            Level = 80;
            Size = 90;
            Model = 696;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            MaxSpeedBase = 250;
            Flags = eFlags.FLYING;
            RespawnInterval =ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            TorstBrain sbrain = new TorstBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is TorstEddiesBrain)
                    npc.RemoveFromWorld();
            }
            base.Die(killer);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (Util.Chance(20))
            {
                if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
                    CastSpell(TorstDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackEnemy(ad);
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
                    spell.RecastDelay = Util.Random(25, 45);
                    spell.ClientEffect = 228;
                    spell.Icon = 208;
                    spell.TooltipId = 479;
                    spell.Damage = 550;
                    spell.Range = 500;
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

        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();
        public static bool point1check = false;
        public static bool point2check = false;
        public static bool point3check = false;
        public static bool point4check = false;
        public static bool point5check = false;
        public static bool point6check = false;
        public static bool walkback = false;

        #region Torst Flying Path
        public void TorstFlyingPath()
        {
            Point3D point1 = new Point3D();
            point1.X = 51166;
            point1.Y = 37442;
            point1.Z = 17331;
            Point3D point2 = new Point3D();
            point2.X = 53201;
            point2.Y = 39956;
            point2.Z = 16314;
            Point3D point3 = new Point3D();
            point3.X = 55178;
            point3.Y = 38616;
            point3.Z = 17901;
            Point3D point4 = new Point3D();
            point4.X = 54852;
            point4.Y = 36185;
            point4.Z = 17859;
            Point3D point5 = new Point3D();
            point5.X = 53701;
            point5.Y = 35635;
            point5.Z = 17859;
            Point3D point6 = new Point3D();
            point6.X = 52118;
            point6.Y = 36114;
            point6.Z = 17265;
            Point3D spawn = new Point3D();
            spawn.X = 50897;
            spawn.Y = 36006;
            spawn.Z = 16659;

            if (!Body.InCombat && !HasAggro)
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
                        if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
                            point3check == false)
                        {
                            Body.WalkTo(point3, 200);
                        }
                        else
                        {
                            point3check = true;
                            if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
                                point3check == true && point4check == false)
                            {
                                Body.WalkTo(point4, 200);
                            }
                            else
                            {
                                point4check = true;
                                if (!Body.IsWithinRadius(point5, 30) && point1check == true &&
                                    point2check == true && point3check == true && point4check == true &&
                                    point5check == false)
                                {
                                    Body.WalkTo(point5, 200);
                                }
                                else
                                {
                                    point5check = true;
                                    if (!Body.IsWithinRadius(point6, 30) && point1check == true &&
                                        point2check == true && point3check == true && point4check == true &&
                                        point5check == true && point6check == false)
                                    {
                                        Body.WalkTo(point6, 200);
                                    }
                                    else
                                    {
                                        point6check = true;
                                        if (!Body.IsWithinRadius(spawn, 30) && point1check == true &&
                                            point2check == true && point3check == true && point4check == true &&
                                            point5check == true && point6check == true && walkback == false)
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
        }
        #endregion

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        private bool SpawnEddies = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            TorstFlyingPath();
            if (HasAggressionTable() && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange) && Body.InCombat)
            {
                Body.Flags = 0; //dont fly
            }

            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.FLYING; //fly
                SpawnEddies = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                    {
                        if (npc != null && npc.IsAlive && npc.Brain is TorstEddiesBrain)
                            npc.RemoveFromWorld();
                    }
                    RemoveAdds = true;
                }
            }

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if (!SpawnEddies)
                {
                    CreateEddies();
                    SpawnEddies = true;
                }
                foreach (GamePlayer gamePlayer in Body.GetPlayersInRadius(1500))
                {
                    if (gamePlayer != null && gamePlayer.IsAlive && gamePlayer.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(gamePlayer))
                            PlayersToAttack.Add(gamePlayer);
                    }
                }

                PickNotRottedTarget();

                if (Util.Chance(10))
                    Body.CastSpell(TorstRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.Think();
        }
        private void CreateEddies()
        {
            BroadcastMessage(String.Format("{0}'s powerful wings stir swirling eddies of air that threaten to freeze anyone caught in their wake!",Body.Name));
            for (int i = 0; i < 5; i++)
            {
                TorstEddies add = new TorstEddies();
                add.X = Body.X + Util.Random(-200, 200);
                add.Y = Body.Y + Util.Random(-200, 200);
                add.Z = Body.Z;
                add.Heading = Body.Heading;
                add.CurrentRegion = Body.CurrentRegion;
                add.AddToWorld();
            }
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
                    GameLiving target = Body.TargetObject as GameLiving; //mob target
                    RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)]; //mob next random target
                    if (target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff)) //if target got root
                    {
                        Body.StopAttack();
                        m_aggroTable.Clear(); //clear aggro list
                        if (RandomTarget != null && RandomTarget.IsAlive)
                        {
                            m_aggroTable.Add(RandomTarget, 50); //add to aggro list our new random target
                            Body.StartAttack(RandomTarget);
                        }
                    }
                }
            }
        }
        #region Spell root
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
                    spell.DamageType = (int) eDamageType.Cold;
                    m_TorstRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TorstRoot);
                }
                return m_TorstRoot;
            }
        }
        #endregion
    }
}
#endregion

#region Hurika
namespace DOL.GS
{
    public class Hurika : GameEpicBoss
    {
        public Hurika() : base()
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

        public override void WalkToSpawn()
        {
            if (CurrentRegionID == 160) //if region is tuscaran glacier
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
            get { return 200000; }
        }

        public override void Die(GameObject killer) //on kill generate orbs
        {
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
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            HurikaBrain sbrain = new HurikaBrain();
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
    public class HurikaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        #region Hurika Flying Path
        public void HurikaFlyingPath()
        {
            Point3D point1 = new Point3D();
            point1.X = 54652;
            point1.Y = 36348;
            point1.Z = 18279;
            Point3D point2 = new Point3D();
            point2.X = 55113;
            point2.Y = 38549;
            point2.Z = 16679;
            Point3D point3 = new Point3D();
            point3.X = 53370;
            point3.Y = 40527;
            point3.Z = 16268;
            Point3D point4 = new Point3D();
            point4.X = 51711;
            point4.Y = 38978;
            point4.Z = 17130;
            Point3D point5 = new Point3D();
            point5.X = 51519;
            point5.Y = 37213;
            point5.Z = 17046;

            if (!Body.InCombat && !HasAggro)
            {
                if (Body.CurrentRegionID == 160) //tuscaran glacier
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
                            if (!Body.IsWithinRadius(point3, 30) && point_1 == true && point_2 == true &&
                                point_3 == false)
                            {
                                Body.WalkTo(point3, 200);
                            }
                            else
                            {
                                point_3 = true;
                                if (!Body.IsWithinRadius(point4, 30) && point_1 == true && point_2 == true &&
                                    point_3 == true && point_4 == false)
                                {
                                    Body.WalkTo(point4, 200);
                                }
                                else
                                {
                                    point_4 = true;
                                    if (!Body.IsWithinRadius(point5, 30) && point_1 == true && point_2 == true &&
                                        point_3 == true && point_4 == true && point_5 == false)
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
                else //not TG
                {
                    //mob will not roam
                }
            }
        }
        #endregion
        public List<GamePlayer> Port_Enemys = new List<GamePlayer>();
        public static bool IsTargetPicked = false;
        public static GamePlayer randomtarget = null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Think()
        {
            HurikaFlyingPath();
            if (HasAggressionTable() && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange) && Body.InCombat)
            {
                Body.Flags = 0; //dont fly
            }

            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.FLYING; //fly
                IsTargetPicked = false;
                RandomTarget = null;
                if (Port_Enemys.Count > 0)
                    Port_Enemys.Clear();
            }

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if(HasAggro && Body.TargetObject != null)
            {
                foreach(GamePlayer player in Body.GetPlayersInRadius(1000))
                {
                    if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !Port_Enemys.Contains(player))
                        Port_Enemys.Add(player);
                }
                if(Port_Enemys.Count > 0)
                {
                    GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                    RandomTarget = Target;
                    if (RandomTarget.IsAlive && RandomTarget != null && !IsTargetPicked)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TeleportPlayer), Util.Random(15000, 20000));
                        IsTargetPicked = true;
                    }
                }
            }

            base.Think();
        }
        private int TeleportPlayer(ECSGameTimer timer)
        {
            if (RandomTarget != null && RandomTarget.IsAlive && HasAggro && Body.IsAlive)
            {
                RandomTarget.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z + Util.Random(500, 700), Body.Heading);
                BroadcastMessage(String.Format("A powerful gust of wind generated by Hurika's wings sends {0} flying into the air!", RandomTarget.Name));
            }
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetPort), 3500);
            return 0;
        }
        private int ResetPort(ECSGameTimer timer)
        {
            RandomTarget = null;//reset random target to null
            IsTargetPicked = false;
            return 0;
        }
    }
}
#endregion

#region Torst eddies
namespace DOL.GS
{
    public class TorstEddies : GameNPC
    {
        public TorstEddies() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 15;// dmg reduction for melee dmg
                case eDamageType.Crush: return 15;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
                default: return 15;// dmg reduction for rest resists
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.10;
        }
        public override void StopFollowing()
        {
            if (IsAlive)
                return;
            base.StopFollowing();
        }
        public override void Follow(GameObject target, int minDistance, int maxDistance)
        {
            if (IsAlive)
                return;
            base.Follow(target, minDistance, maxDistance);
        }
        public override void FollowTargetInRange()
        {
            if (IsAlive)
                return;
            base.FollowTargetInRange();
        }
        public override void WalkToSpawn(short speed)
        {
            if (IsAlive)
                return;
            base.WalkToSpawn(speed);
        }
        public override void StartAttack(GameObject target)
        {
        }
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        #endregion
        public override bool AddToWorld()
        {
            Model = 665;
            Name = "eddie";
            Level = (byte)Util.Random(55, 58);
            Size = 50;
            RespawnInterval = -1;
            Flags = (GameNPC.eFlags)44;//noname notarget flying
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            MaxSpeedBase = 300;

            LoadedFromScript = true;
            TorstEddiesBrain sbrain = new TorstEddiesBrain();
            SetOwnBrain(sbrain);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }
            return success;
        }
        #region Effect
        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                Parallel.ForEach(GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
                {
                    player?.Out.SendSpellEffectAnimation(this, this, 4168, 0, false, 0x01);
                });

                return 1600;
            }
            return 0;
        }
      
        #endregion
        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
    public class TorstEddiesBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TorstEddiesBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
            ThinkInterval = 1500;
        }
        private protected bool Point1check = false;
        private protected bool Point2check = false;
        bool SetNpcTarget = false;

        private protected static GameNPC trostnpc = null;
        private protected static GameNPC TrostNpc
        {
            get { return trostnpc; }
            set { trostnpc = value; }
        }
        public override void Think()
        {
            Body.CurrentSpeed = 300;
            if (!SetNpcTarget)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is TorstBrain)
                    {
                        trostnpc = npc;
                        SetNpcTarget = true;
                    }
                }
            }

            if (TrostNpc != null && TrostNpc.IsAlive)
            {
                Point3D oldPoint = new Point3D(TrostNpc.X + Util.Random(-200, 200), TrostNpc.Y + Util.Random(-200, 200), TrostNpc.Z + Util.Random(0, 100));
                Point3D newPoint = new Point3D(TrostNpc.X + Util.Random(-200, 200), TrostNpc.Y + Util.Random(-200, 200), TrostNpc.Z + Util.Random(0, 100));
                if (!Body.IsWithinRadius(oldPoint, 20) && !Point1check)
                {
                    Body.WalkTo(oldPoint, 300);
                }
                else
                {
                    Point1check = true;
                    Point2check = false;
                    if (!Body.IsWithinRadius(newPoint, 20) && Point1check && !Point2check)
                    {
                        Body.WalkTo(newPoint, 300);
                    }
                    else
                    {
                        Point2check = true;
                        Point1check = false;
                    }
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                Body.CastSpell(ColdGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
            base.Think();
        }
        private Spell m_ColdGroundDD;
        private Spell ColdGroundDD
        {
            get
            {
                if (m_ColdGroundDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(5,12);
                    spell.ClientEffect = 161;
                    spell.Icon = 161;
                    spell.TooltipId = 368;
                    spell.Name = "Cold Snap";
                    spell.Damage = 110;
                    spell.Range = 200;
                    spell.Radius = 300;
                    spell.SpellID = 11926;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_ColdGroundDD = new Spell(spell, 60);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ColdGroundDD);
                }
                return m_ColdGroundDD;
            }
        }
    }
}
#endregion