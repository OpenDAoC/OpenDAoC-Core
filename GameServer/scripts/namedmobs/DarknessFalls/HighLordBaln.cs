using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using log4net;

namespace DOL.GS
{
    public class HighLordBaln : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("High Lord Baln initialized..");
        }

        public HighLordBaln()
            : base()
        {
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162130);
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

            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            BaelerdothBrain sBrain = new BaelerdothBrain();
            SetOwnBrain(sBrain);

            base.AddToWorld();
            return true;
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        
        public override int MaxHealth
        {
            get { return 20000; }
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
            return 1000;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override void Die(GameObject killer)
        {

            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,OrbsReward);
                }
            }
            
            base.Die(killer);
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (!InCombat)
            {
                var mobs = GetNPCsInRadius(3000);
                foreach (GameNPC mob in mobs)
                {
                    if (!mob.InCombat)
                    {
                        mob.StartAttack(ad.Attacker);
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }

        public override void OnAttackEnemy(AttackData ad)
        {
            foreach (GamePlayer player in GetPlayersInRadius(2000))
            {
                if (GetDistanceTo(player) > 300)
                {
                    BalnMinion sMinion = new BalnMinion();
                    sMinion.X = player.X;
                    sMinion.Y = player.Y;
                    sMinion.Z = player.Z;
                    sMinion.CurrentRegion = player.CurrentRegion;
                    sMinion.Heading = player.Heading;
                    sMinion.AddToWorld();
                    sMinion.StartAttack(player);
                }
            }

            base.OnAttackEnemy(ad);
        }
    }
}

namespace DOL.AI.Brain
{
    public class BalnBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public BalnBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}

namespace DOL.GS
{
    public class BalnMinion : GameNPC
    {
        public override int MaxHealth
        {
            get { return 450 * Constitution / 100; }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162130);
            LoadTemplate(npcTemplate);
            Level = 58;
            Strength = 300;
            Size = 50;
            Name += "'s Minion";
            RoamingRange = 350;
            RespawnInterval = -1;
            MaxDistance = 1500;
            TetherRange = 2000;
            IsWorthReward = false; // worth no reward
            Realm = eRealm.None;
            BeliathanMinionBrain adds = new BeliathanMinionBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            base.AddToWorld();
            return true;
        }

        public override void DropLoot(GameObject killer) //no loot
        {
        }

        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }
        
    }
}

namespace DOL.AI.Brain
{
    public class BalnMinionBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BalnMinionBrain()
        {
            AggroLevel = 100;
            AggroRange = 450;
        }
    }
}