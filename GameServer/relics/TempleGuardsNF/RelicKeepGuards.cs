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
            SetupByKeepName();

            return base.AddToWorld();
        }
        private void SetupByKeepName()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                LoadDefaultTemplate();
                return;
            }

            string customTemplateID = this.Name.ToLower().Replace(" ", "_");

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            if (template.LoadFromDatabase(customTemplateID))
            {
                this.Inventory = template;
                this.EquipmentTemplateID = customTemplateID;
            }
            else
            {
                LoadDefaultTemplate();
            }

            InitializeActiveWeaponFromInventory();
        }

        private void LoadDefaultTemplate()
        {
            string defaultID = "";
            switch (Realm)
            {
                case eRealm.Albion: defaultID = "relic_temple_lord_alb"; break;
                case eRealm.Midgard: defaultID = "relic_temple_lord_mid"; break;
                case eRealm.Hibernia: defaultID = "relic_temple_lord_hib"; break;
            }

            GameNpcInventoryTemplate defaultTemplate = new GameNpcInventoryTemplate();
            if (defaultTemplate.LoadFromDatabase(defaultID))
            {
                this.Inventory = defaultTemplate;
                InitializeActiveWeaponFromInventory();
            }
        }

        public override void Die(GameObject killer)
        {
            int count = TempleRelicPadsLoader.GetEnemiesNearby(this);
            TempleRelicPadsLoader.SendTempleMessage($"{Name} has been killed with {count} enemies in the area.");
            base.Die(killer);
        }

        public static void UpdateRelicKeepGuards(AbstractGameKeep keep)
        {
            if (keep == null) return;

            string guardName = "Relic Defender of " + keep.Name;

            var guards = WorldMgr.GetNPCsByNameFromRegion(guardName, 163, keep.OriginalRealm);

            foreach (GameNPC guard in guards)
            {
                if (guard is RelicKeepGuard relicGuard)
                {
                    string newGuildName = (keep.Guild != null) ? keep.Guild.Name : string.Empty;
                    int guildEmblem = (keep.Guild != null) ? keep.Guild.Emblem : 0;

                    relicGuard.GuildName = newGuildName;

                    if (relicGuard.Inventory != null)
                    {
                        eInventorySlot[] emblemSlots = { eInventorySlot.LeftHandWeapon, eInventorySlot.Cloak };
                        foreach (eInventorySlot slot in emblemSlots)
                        {
                            DbInventoryItem item = relicGuard.Inventory.GetItem(slot);
                            if (item != null && item.Emblem != guildEmblem)
                            {
                                item.Emblem = guildEmblem;
                            }
                        }
                    }
                    relicGuard.SaveIntoDatabase();
                    relicGuard.BroadcastLivingEquipmentUpdate(); // Visible update for players
                    if (relicGuard.Inventory is GameNpcInventoryTemplate template)
                    {
                        template.SaveIntoDatabase(relicGuard.EquipmentTemplateID);
                    }

                }
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class RelicKeepGuardBrain : StandardMobBrain
    {
        public RelicKeepGuardBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 1000;
        }
        public override bool CanAggroTarget(GameLiving target)
        {
            if (Body == null || target == null) return false;
            return GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
        }
    }
}