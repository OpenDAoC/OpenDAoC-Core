using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;
using DOL.GS.Scripts;
using log4net;

namespace DOL.GS.Scripts
{
    public class Legion : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Legion()
            : base()
        {
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13333);
            LoadTemplate(npcTemplate);
            
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            // demon
            BodyType = 2;
            
            LegionBrain sBrain = new LegionBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
        
        

        public virtual int LegionDifficulty
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
                return 20000 * LegionDifficulty;
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500 * LegionDifficulty;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.65 * LegionDifficulty;
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc.Brain is LegionAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
            
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,5000);
                }
            }
            DropLoot(killer);
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class LegionBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static IArea Legion_Area = null;
        private static GameLocation Legion_Lair = new GameLocation("Legion\'s Lair", 249, 45066, 51731, 15468, 2053);

        public LegionBrain()
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

        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.Brain is LegionAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            
            if (HasAggro && Body.InCombat)
            {
                if (Body.TargetObject != null)
                {
                    // 5% chance to spawn 15-20 zombies
                    if (Util.Chance(5))
                    {
                        SpawnAdds();
                    }
                }
            }
            else
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.Brain is LegionAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            base.Think();
        }
        public void SpawnAdds()
        {
            for (int i = 0; i < Util.Random(15, 20); i++)
            {
                LegionAdd add = new LegionAdd();
                add.X = Body.X;
                add.Y = Body.Y;
                add.Z = Body.Z;
                add.CurrentRegion = Body.CurrentRegion;
                add.Heading = Body.Heading;
                add.IsWorthReward = false;
                int level = Util.Random(52, 58);
                add.Level = (byte) level;
                add.AddToWorld();
                break;
            }
        }
        
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Legion initializing ...");
            
            #region defineAreas
            Legion_Area = WorldMgr.GetRegion(Legion_Lair.RegionID).AddArea(new Area.Circle("Legion Lair", Legion_Lair.X, Legion_Lair.Y, Legion_Lair.Z, 530));
            Legion_Area.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
            #endregion
        }
        
        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            #region defineAreas
            Legion_Area = WorldMgr.GetRegion(Legion_Lair.RegionID).AddArea(new Area.Circle("Legion Lair", Legion_Lair.X, Legion_Lair.Y, Legion_Lair.Z, 530));
            Legion_Area.RegisterPlayerLeave(new DOLEventHandler(PlayerEnterLegionArea));
            #endregion
        }
        
        private static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
        {
            AreaEventArgs aargs = args as AreaEventArgs;
            GamePlayer player = aargs?.GameObject as GamePlayer;

            if (player == null)
                return;
            
            if (Util.Chance(50))
            {
                foreach (GamePlayer portPlayer in player.GetPlayersInRadius(250))
                {
                    if (portPlayer.IsAlive)
                    {
                        portPlayer.MoveTo(249, 48117, 49573, 20833, 1006);
                        portPlayer.BroadcastUpdate();
                    }
                }
                player.MoveTo(249, 48117, 49573, 20833, 1006);
                player.BroadcastUpdate();
            }
        }
        
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);

            if (e == GameLivingEvent.Dying && sender is GamePlayer)
            {
                GamePlayer player = sender as GamePlayer;
                Body.Health += player.MaxHealth;
                Body.UpdateHealthManaEndu();
            }
        }
    }
}

namespace DOL.GS
{
    public class LegionAdd : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LegionAdd()
            : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 1500;
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 150;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.50;
        }
        public override bool AddToWorld()
        {
            Model = 660;
            Name = "graspering soul";
            Size = 50;
            Realm = 0;

            Strength = 60;
            Intelligence = 60;
            Piety = 60;
            Dexterity = 60;
            Constitution = 60;
            Quickness = 60;
            RespawnInterval = -1;

            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;

            BodyType = 2;
            LegionAddBrain sBrain = new LegionAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 800;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class LegionAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public LegionAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}