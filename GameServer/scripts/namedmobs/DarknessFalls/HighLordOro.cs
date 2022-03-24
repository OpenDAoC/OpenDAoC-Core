using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using log4net;

namespace DOL.GS
{
    public class HighLordOro : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("High Lord Oro initialized..");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
        }

        public HighLordOro()
            : base()
        {
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162132);
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

            OroBrain sBrain = new OroBrain();
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
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc.Brain is OroCloneBrain)
                {
                    npc.Die(killer);
                }
            }

            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class OroBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        bool isPulled;

        public OroBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (!isPulled)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
                {
                    if (npc?.Brain is OroCloneBrain && npc.IsAlive)
                    {
                        if (npc.InCombat) continue;
                        AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                        isPulled = true;
                    }
                }
            }

            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                isPulled = false;
            }
            else
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(350))
                {
                    player.Mana -= (int) (player.MaxMana * 0.05);
                    player.UpdateHealthManaEndu();
                }
            }

            base.Think();
        }
    }
}

namespace DOL.GS
{
    public class HighLordOroClone : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HighLordOroClone()
            : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get { return 1500; }
        }

        public override int AttackRange
        {
            get { return 450; }
            set { }
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
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(90162132);
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

            OroCloneBrain sBrain = new OroCloneBrain();
            SetOwnBrain(sBrain);

            IsWorthReward = false;

            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class OroCloneBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        bool isPulled;

        public OroCloneBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (!isPulled)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
                {
                    if (npc?.Brain is OroCloneBrain or OroBrain)
                    {
                        if (npc.InCombat || !npc.IsAlive) continue;
                        AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                        isPulled = true;
                    }
                }
            }

            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                isPulled = false;
            }
            else
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(350))
                {
                    player.Mana -= (int) (player.MaxMana * 0.05);
                    player.UpdateHealthManaEndu();
                }
            }

            base.Think();
        }
    }
}