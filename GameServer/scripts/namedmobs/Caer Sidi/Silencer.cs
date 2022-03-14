using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS
{
    public class Silencer : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Silencer()
            : base()
        {
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public virtual int COifficulty
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
        public List<GamePlayer> attackers = new List<GamePlayer>();
        public static int attackers_count = 0;

        public static eDamageType dmg_type = eDamageType.Natural;
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                attackers.Add(source as GamePlayer);
                attackers_count = attackers.Count / 2;

                if (Util.Chance(attackers_count))
                {
                    if (resist_timer == false)
                    {
                        BroadcastMessage(String.Format(this.Name + " becomes almost immune to any damage for short time!"));
                        new RegionTimer(this, new RegionTimerCallback(ResistTime), 2000);
                        resist_timer = true;
                    }
                }
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
        public override int GetResist(eDamageType damageType)
        {
            if (get_resist == true)
            {
                switch (damageType)
                {
                    case eDamageType.Slash:
                    case eDamageType.Crush:
                    case eDamageType.Thrust: return 99;//99% dmg reduction for melee dmg
                    default: return 99;// 99% reduction for rest resists
                }
            }
            return 0;//without resists
        }
        public static bool get_resist = false;//set resists
        public static bool resist_timer = false;
        public static bool resist_timer_end = false;
        public static bool spam1 = false;
        public int ResistTime(RegionTimer timer)
        {
            get_resist = true;
            spam1 = false;
            if (resist_timer == true && resist_timer_end == false)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendSpellEffectAnimation(this, this, 9103, 0, false, 0x01);
                }
                new RegionTimer(this, new RegionTimerCallback(ResistTimeEnd), 20000);//20s resist 99%
                resist_timer_end = true;
            }
            return 0;
        }
        public int ResistTimeEnd(RegionTimer timer)
        {
            get_resist = false;
            resist_timer = false;
            resist_timer_end = false;
            attackers.Clear();
            attackers_count = 0;
            if (spam1 == false)
            {
                BroadcastMessage(String.Format(this.Name + " resists fades away!"));
                spam1 = true;
            }
            return 0;
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166029);
            LoadTemplate(npcTemplate);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            SilencerBrain adds = new SilencerBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
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
                    AtlasROGManager.GenerateOrbAmount(groupPlayer, 5000);//5k orbs for every player in group
                }
            }
            DropLoot(killer);
            base.Die(killer);
        }
        
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Silencer", 60, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Silencer  not found, creating it...");

                log.Warn("Initializing Silencer...");
                Silencer CO = new Silencer();
                CO.Name = "Silencer";
                CO.Model = 934;
                CO.Realm = 0;
                CO.Level = 81;
                CO.Size = 220;
                CO.CurrentRegionID = 60;//caer sidi

                CO.Strength = 500;
                CO.Intelligence = 220;
                CO.Piety = 220;
                CO.Dexterity = 200;
                CO.Constitution = 200;
                CO.Quickness = 125;
                CO.BodyType = 5;
                CO.MeleeDamageType = eDamageType.Slash;
                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

                CO.X = 31035;
                CO.Y = 36323;
                CO.Z = 15620;
                CO.MaxDistance = 2000;
                CO.TetherRange = 1700;
                CO.MaxSpeedBase = 300;
                CO.Heading = 3035;

                SilencerBrain ubrain = new SilencerBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 600;
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Silencer exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class SilencerBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public SilencerBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 5000;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                Silencer.attackers_count = 0;
                Silencer silencer = new Silencer();
                silencer.attackers.Clear();
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                Body.Model = 934;
            }
            if(Body.InCombat && HasAggro)
            {
            }
            base.Think();
        }
    }
}