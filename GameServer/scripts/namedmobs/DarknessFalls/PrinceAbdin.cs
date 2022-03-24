using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using log4net;

namespace DOL.GS
{
    public class PrinceAbdin : GameEpicBoss
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
            if (log.IsInfoEnabled)
				log.Info("Prince Abdin initialized..");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{

        }
        
        public PrinceAbdin()
            : base()
        {
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165029);
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
            
            AbdinBrain sBrain = new AbdinBrain();
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
            get
            {
                return 20000;
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
            return 1000;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        
        public override byte ParryChance => 70;
        

        public override void Die(GameObject killer)
        {
            
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,ServerProperties.Properties.EPIC_ORBS);
                }
            }
            base.Die(killer);
        }
        
    }
}

namespace DOL.AI.Brain
{
    public class AbdinBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AbdinBrain()
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