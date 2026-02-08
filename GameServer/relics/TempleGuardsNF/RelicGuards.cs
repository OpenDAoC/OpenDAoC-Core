using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.AI.Brain;
using DOL.Logging;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    // New Frontiers Relic Temple Guards
    // You have to set the realm to the mob and reload the mob
    public class RelicGuardsTest : GameNPC
    {
        protected const byte RelicGuardsLevel = 65;
        protected const int RelicGuardsRespawnInterval = 900000; // 15min

        public override bool AddToWorld()
        {
            ushort[] modelsAlb = { 14, 1008 }; // Briton & Half Ogre
            ushort[] modelsMid = { 137, 153 }; // Norse & Troll
            ushort[] modelsHib = { 318, 286 }; // Lurikeen & Firbolg
            switch (Realm)
            {
                case eRealm.Albion:
                    Name = "Armsman";
                    Model = modelsAlb[Util.Random(0, modelsAlb.Length - 1)];
                    break;
            
                case eRealm.Midgard:
                    Name = "Huscarl";
                    Model = modelsMid[Util.Random(0, modelsMid.Length - 1)];
                    break;
                    
                case eRealm.Hibernia:
                    Name = "Guardian";
                    Model = modelsHib[Util.Random(0, modelsHib.Length - 1)];
                    break;
            }

            Level = RelicGuardsLevel;
            Flags = 0; // Remove Peace flag on new mob
            RespawnInterval = RelicGuardsRespawnInterval;

            SetOwnBrain(new RelicGuardsTestBrain());

            SetupByRealm(Realm);

            return base.AddToWorld();
        }

        private void SetupByRealm(eRealm realm)
        {
            switch (realm)
            {
                case eRealm.Albion:
                    this.LoadEquipmentTemplateFromDatabase("relic_temple_lord_alb");
                    break;
                case eRealm.Midgard:
                    this.LoadEquipmentTemplateFromDatabase("relic_temple_lord_mid");
                    break;
                case eRealm.Hibernia:
                    this.LoadEquipmentTemplateFromDatabase("relic_temple_lord_hib");
                    break;
            }
        }

        public override void Die(GameObject killer)
        {
            int count = TempleRelicPadsLoader.GetEnemiesNearby(this);
            TempleRelicPadsLoader.SendTempleMessage($"{Name} has been killed with {count} enemies in the area.");
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class RelicGuardsTestBrain : StandardMobBrain
    {
        protected const int MaxDistance = 2000;

        public RelicGuardsTestBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 1000;
        }

        /*public override void Think()
        {
            if (Body == null || !Body.IsAlive || Body.IsReturningToSpawnPoint) return;

            // Überprüfung der Leine (Leash)
            if (Body.GetDistanceTo(Body.SpawnPoint) > MaxDistance)
            {
                ResetCaster();
                return;
            }

            base.Think();
        }

        private void ResetCaster()
        {
            Body.StopAttack();
            ClearAggroList();
            Body.Health = Body.MaxHealth;
            Body.Mana = Body.MaxMana;
            Body.ReturnToSpawnPoint(Body.MaxSpeed);
        }

        public override bool CanAggroTarget(GameLiving target)
        {
            if (Body == null || target == null) return false;
            return GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
        }*/
    }
}