using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class SergeantDarryn : GameNPC
    {
        public SergeantDarryn() : base()
        {
        }
        public static int AfterParryID = 108;
        public static int AfterParryClassID = 2;
        public static Style AfterParry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

        public static int TauntID = 103;
        public static int TauntClassID = 2;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int ParryFollowUpID = 112;
        public static int ParryFollowUpClassID = 2;
        public static Style ParryFollowUp = SkillBase.GetStyleByID(ParryFollowUpID, ParryFollowUpClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            if (ad != null && ad.AttackResult == EAttackResult.Parried)
            {
                this.styleComponent.NextCombatBackupStyle = SergeantDarryn.ParryFollowUp;
                this.styleComponent.NextCombatStyle = SergeantDarryn.AfterParry;
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitStyle || ad.AttackResult == EAttackResult.HitUnstyled))
            {
                this.styleComponent.NextCombatBackupStyle = Taunt; //taunt as backup style
                this.styleComponent.NextCombatStyle = ParryFollowUp; //after parry style as main
            }
            base.OnAttackEnemy(ad);
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 35; // dmg reduction for melee dmg
                case EDamageType.Crush: return 35; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }

        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (this.IsOutOfTetherRange)
                {
                    if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                        damageType == EDamageType.Energy || damageType == EDamageType.Heat
                        || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                        damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                        || damageType == EDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System,
                                eChatLoc.CL_ChatWindow);
                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 150;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 400;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7714);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            if (!this.Styles.Contains(AfterParry))
            {
                Styles.Add(AfterParry);
            }
            if (!this.Styles.Contains(Taunt))
            {
                Styles.Add(Taunt);
            }
            if (!this.Styles.Contains(ParryFollowUp))
            {
                Styles.Add(ParryFollowUp);
            }
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Slash;
            SergeantDarrynBrain sbrain = new SergeantDarrynBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Sergeant Darryn", 277, (ERealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Sergeant Darryn not found, creating it...");

                log.Warn("Initializing Sergeant Darryn...");
                SergeantDarryn HOC = new SergeantDarryn();
                HOC.Name = "Sergeant Darryn";
                HOC.Model = 40;
                HOC.Realm = 0;
                HOC.Level = 50;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 28153;
                HOC.Y = 33328;
                HOC.Z = 14941;
                HOC.Heading = 3075;
                SergeantDarrynBrain ubrain = new SergeantDarrynBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Sergeant Darryn exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class SergeantDarrynBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SergeantDarrynBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 1500;
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat && HasAggro)
            {
                if (Body.TargetObject != null)
                {
                    GameLiving living = Body.TargetObject as GameLiving;
                }
            }
            base.Think();
        }
    }
}




