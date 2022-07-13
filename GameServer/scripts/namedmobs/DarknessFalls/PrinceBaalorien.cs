using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS
{
    public class PrinceBaalorien : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Prince Ba'alorien initialized..");
        }
        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
        }
        public PrinceBaalorien()
            : base()
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
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165031);
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
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            BaalorienBrain sBrain = new BaalorienBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
    }
}

namespace DOL.AI.Brain
{
    public class BaalorienBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public BaalorienBrain()
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
            else
            {
                foreach (GameNPC pet in Body.GetNPCsInRadius(2000))
                {
                    if (pet.Brain is not IControlledBrain) continue;
                    Body.Health += pet.MaxHealth;
                    foreach (GamePlayer player in pet.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendSpellEffectAnimation(Body, pet, 368, 0, false, 1);
                    pet.Die(Body);
                }
            }
            base.Think();
        }
    }
}