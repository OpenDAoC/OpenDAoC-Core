﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Yar : GameEpicBoss
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Yar()
            : base()
        {
        }
        public virtual int YarDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS / 100; }
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 20; // dmg reduction for melee dmg
                case EDamageType.Crush: return 20; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
                default: return 30; // dmg reduction for rest resists
            }
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
            get { return 40000; }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }      
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168093);
            LoadTemplate(npcTemplate);
            
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            
            Faction = FactionMgr.GetFactionByID(154);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
            RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

            YarBrain sBrain = new YarBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc.Brain is YarAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class YarBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public YarBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        private bool _isSpawning = true;
        
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.Brain is YarAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }

                _isSpawning = true;
            }

            if (Body.InCombat && HasAggro && Body.TargetObject != null)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(2000))
                {
                    if (npc.Name.ToLower().Contains("drakulv"))
                    {
                        foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                        {
                            npc.StartAttack(player);
                        }
                    }
                }
            }
            base.Think();
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameObjectEvent.TakeDamage && Body.HealthPercent <= 30)
            {
                if (_isSpawning)
                {
                    SpawnAdds();
                    _isSpawning = false;
                }
            }

            if (e == GameLivingEvent.Dying)
            {
                _isSpawning = true;
            }
        }

        public void SpawnAdds()
        {
            for (int i = 0; i < 5; i++)
            {
                switch (Util.Random(1, 3))
                {
                    case 1:
                        YarAdd add = new YarAdd();
                        add.X = Body.X + Util.Random(-50, 80);
                        add.Y = Body.Y + Util.Random(-50, 80);
                        add.Z = Body.Z;
                        add.CurrentRegion = Body.CurrentRegion;
                        add.Heading = Body.Heading;
                        add.AddToWorld();
                        break;
                    case 2:
                        YarAdd2 add2 = new YarAdd2();
                        add2.X = Body.X + Util.Random(-50, 80);
                        add2.Y = Body.Y + Util.Random(-50, 80);
                        add2.Z = Body.Z;
                        add2.CurrentRegion = Body.CurrentRegion;
                        add2.Heading = Body.Heading;
                        add2.AddToWorld();
                        break;
                    case 3:
                        YarAdd3 add3 = new YarAdd3();
                        add3.X = Body.X + Util.Random(-50, 80);
                        add3.Y = Body.Y + Util.Random(-50, 80);
                        add3.Z = Body.Z;
                        add3.CurrentRegion = Body.CurrentRegion;
                        add3.Heading = Body.Heading;
                        add3.AddToWorld();
                        break;
                    default:
                        break;
                }
                
            }
        }
    }
}

namespace DOL.GS
{
    public class YarAdd : GameNPC
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public YarAdd()
            : base()
        {
        }

        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 7000;
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 300;
        }

        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override bool AddToWorld()
        {
            Yar yar = new Yar();
            Model = 625;
            Name = "drakulv executioner";
            Size = 60;
            Level = 63;
            Realm = 0;
            CurrentRegionID = yar.CurrentRegionID;

            Strength = 180;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;

            Gender = EGender.Neutral;
            MeleeDamageType = EDamageType.Slash;

            Faction = FactionMgr.GetFactionByID(154);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
            
            BodyType = 5;
            YarAddBrain sBrain = new YarAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            base.AddToWorld();
            return true;
        }
    }
    public class YarAdd2 : GameNPC
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public YarAdd2()
            : base()
        {
        }

        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 7000;
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 300;
        }

        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override bool AddToWorld()
        {
            Yar yar = new Yar();
            Model = 624;
            Name = "drakulv soultrapper";
            Size = 58;
            Level = 62;
            Realm = 0;
            CurrentRegionID = yar.CurrentRegionID;

            Strength = 180;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;

            Gender = EGender.Neutral;
            MeleeDamageType = EDamageType.Slash;
            
            Faction = FactionMgr.GetFactionByID(154);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
            
            BodyType = 5;
            YarAddBrain sBrain = new YarAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            base.AddToWorld();
            return true;
        }
    }
    public class YarAdd3 : GameNPC
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public YarAdd3()
            : base()
        {
        }

        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 7000;
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 300;
        }

        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override bool AddToWorld()
        {
            Yar yar = new Yar();
            Model = 625;
            Name = "drakulv disciple";
            Size = 58;
            Level = 62;
            Realm = 0;
            CurrentRegionID = yar.CurrentRegionID;

            Strength = 180;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;
            Faction = FactionMgr.GetFactionByID(154);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
            
            Gender = EGender.Neutral;
            MeleeDamageType = EDamageType.Slash;

            BodyType = 5;
            YarAddBrain sBrain = new YarAddBrain();
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
    public class YarAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public YarAddBrain()
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