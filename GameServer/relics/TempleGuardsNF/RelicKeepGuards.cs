using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.AI.Brain;
using DOL.GS.Keeps;

namespace DOL.GS
{
    // Relic Temple Guards for New Frontiers
    // Guild names are updated based on the keep they are assigned to
    // Name must be Relic Defender of <Keep Name> for the brain to work
    public class RelicKeepGuard : GameNPC
    {
        protected const byte RelicKeepGuardsLevel = 65;
        protected const int RelicKeepGuardsRespawnInterval = 900000;
        public override bool AddToWorld()
        {
            ushort[] modelsAlb = { 14, 1008 };
            ushort[] modelsMid = { 137, 153 };
            ushort[] modelsHib = { 318, 286 };

            switch (Realm)
            {
                case eRealm.Albion:
                    Model = modelsAlb[Util.Random(0, modelsAlb.Length - 1)];
                    break;
                case eRealm.Midgard:
                    Model = modelsMid[Util.Random(0, modelsMid.Length - 1)];
                    break;
                case eRealm.Hibernia:
                    Model = modelsHib[Util.Random(0, modelsHib.Length - 1)];
                    break;
            }
            Level = RelicKeepGuardsLevel;
            Flags = 0;
            RespawnInterval = RelicKeepGuardsRespawnInterval;

            SetOwnBrain(new RelicKeepGuardBrain());
            SetupByRealm(Realm);

            return base.AddToWorld();
        }

        private void SetupByRealm(eRealm realm)
        {
            switch (realm)
            {
                case eRealm.Albion: LoadEquipmentTemplateFromDatabase("relic_temple_lord_alb"); break;
                case eRealm.Midgard: LoadEquipmentTemplateFromDatabase("relic_temple_lord_mid"); break;
                case eRealm.Hibernia: LoadEquipmentTemplateFromDatabase("relic_temple_lord_hib"); break;
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
    public class RelicKeepGuardBrain : StandardMobBrain
    {
        //protected const int MaxDistance = 2000;

        public RelicKeepGuardBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 1000;
        }

        public override void Think()
        {
            base.Think();
            GameNPC body = this.Body as GameNPC;
            if (body == null || body.CurrentRegion == null)
                return;

            string prefix = "Relic Defender of ";
            if (string.IsNullOrEmpty(body.Name) || !body.Name.StartsWith(prefix))
                return;

            string extractedKeepName = body.Name.Replace(prefix, "").Trim();

            var keepManager = GameServer.KeepManager as DefaultKeepManager;
            if (keepManager == null) return;

            AbstractGameKeep targetKeep = null;
            foreach (AbstractGameKeep keep in keepManager.Keeps.Values)
            {
                if (keep.Name.Equals(extractedKeepName, StringComparison.OrdinalIgnoreCase))
                {
                    targetKeep = keep;
                    break;
                }
            }

            if (targetKeep != null && targetKeep.Realm == body.Realm)
            {
                string newGuildName = (targetKeep.Guild != null) ? targetKeep.Guild.Name : string.Empty;

                if (body.GuildName != newGuildName)
                {
                    body.GuildName = newGuildName;
                }
            }
        }

        /*private void ResetCaster()
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