using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

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
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
                if (!source.IsWithinRadius(spawn, TetherRange))//dont take any dmg 
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is too far away from waterfall and is immune to your damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else//take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
        public override void WalkToSpawn(short speed)
        {
            speed = 300;
            base.WalkToSpawn(speed);
        }
        public override void Die(GameObject killer)
        {
            foreach(GamePlayer player in GetPlayersInRadius(10000))
            {
                if(player != null)
                {
                    player.Out.SendMessage("With the death of the Easmarach, the current of the falls reduces significantly.", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                }
            }
            base.Die(killer);
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
            EasmarachBrain.restphase = false;
            EasmarachBrain.dontattack = false;

            EasmarachBrain sBrain = new EasmarachBrain();
            SetOwnBrain(sBrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
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
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public static bool restphase = false;
        public static bool dontattack = false;
        public override void AttackMostWanted()
        {
            if (dontattack==true)
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
        private int StartWalk(ECSGameTimer timer)
        {
            dontattack = true;
            return 0;
        }
        private int EndWalk(ECSGameTimer timer)
        {
            restphase = false;
            healBoss = false;
            return 0;
        }
        private bool healBoss = false;
        public override void Think()
        {
            Point3D point1 = new Point3D(37811, 50342, 10958);
            if (Body.HealthPercent <= 30 && !restphase)
            {
                ClearAggroList();
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(StartWalk), 100);
                BroadcastMessage(String.Format("{0} is retreating to waterfall!",Body.Name));
                restphase = true;
            }
            if (dontattack && !Body.IsWithinRadius(point1, 50) && restphase)
            {
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
                Body.MaxSpeedBase = npcTemplate.MaxSpeed;
                Body.Z = 10958;
                Body.WalkTo(point1, 200);
            }

            if (Body.IsWithinRadius(point1, 50) && !healBoss && restphase)
            {
                if(Body.HealthPercent <= 30)
                    Body.Health += Body.MaxHealth / 8;

                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(EndWalk), Util.Random(50000,80000));
                dontattack = false;
                healBoss = true;
            }

            if (Body.IsAlive)
            {
                Point3D nopass = new Point3D(37653, 52843, 10758);//you shall not pass!
                foreach(GamePlayer player in Body.GetPlayersInRadius(10000))
                {
                    if(player != null)
                    {
                        if(player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (player.IsWithinRadius(nopass, 1000))
                            { 
                                player.MoveTo(Body.CurrentRegionID, 40067, 50494, 11708, 1066);
                                player.Out.SendMessage("The strong current of the waterfall pushes you behind", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                            }
                        }
                    }
                }
            }
            if (Body.InCombatInLast(65 * 1000) == false && this.Body.InCombatInLast(70 * 1000))
            {
                Body.Health = Body.MaxHealth;
                restphase = false;
                dontattack = false;
                healBoss = false;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
                Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            }
            if (Body.IsOutOfTetherRange && !dontattack && Body.TargetObject != null)
            {
                Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
                GameLiving target = Body.TargetObject as GameLiving;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160317);
                if (target != null)
                {
                    if (!target.IsWithinRadius(spawn, Body.TetherRange))
                    {
                        Body.MaxSpeedBase = 0;
                    }
                    else
                        Body.MaxSpeedBase = npcTemplate.MaxSpeed;
                }
            }
            base.Think();
        }
    }
}