﻿using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
    public class HighLordOro : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162132);
            LoadTemplate(npcTemplate);

            // demon
            BodyType = 2;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);

            OroBrain sBrain = new OroBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override int MeleeAttackRange => 450;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

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
                        AddAggroListTo(npc.Brain as OroCloneBrain); // add to aggro mobs with CryptLordBaf PackageID
                        isPulled = true;
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
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
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HighLordOroClone()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 45; // dmg reduction for melee dmg
                case eDamageType.Crush: return 45; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 45; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }

        public override int MaxHealth
        {
            get { return 30000; }
        }
        public override int MeleeAttackRange => 450;
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 250;
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

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);

            OroCloneBrain sBrain = new OroCloneBrain();
            SetOwnBrain(sBrain);

            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class OroCloneBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        bool isPulled;
        bool isPulled2;

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
                    if (npc?.Brain is OroCloneBrain)
                    {
                        if (npc.InCombat || !npc.IsAlive) continue;
                        AddAggroListTo(npc.Brain as OroCloneBrain); // add to aggro mobs with CryptLordBaf PackageID
                        isPulled = true;
                    }
                }
            }
            if(!isPulled2)
            {
                foreach (GameNPC npc2 in Body.GetNPCsInRadius(2500))
                {
                    if (npc2?.Brain is OroBrain)
                    {
                        if (npc2.InCombat || !npc2.IsAlive) continue;
                        AddAggroListTo(npc2.Brain as OroBrain); // add to aggro mobs with CryptLordBaf PackageID
                        isPulled2 = true;
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                isPulled = false;
                isPulled2 = false;
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
