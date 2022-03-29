using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
namespace DOL.GS
{
    public class SergeantEddison : GameNPC
    {
        public SergeantEddison() : base()
        {
        }
        public static int SlamID = 228;
        public static int SlamClassID = 2;
        public static Style slam = SkillBase.GetStyleByID(SlamID, SlamClassID);

        public static int TauntID = 134;
        public static int TauntClassID = 2;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            base.OnAttackedByEnemy(ad);
        }
        public int Range(RegionTimer timer)
        {
            this.Strength = 250;
            this.SwitchToRanged(this.TargetObject);
            new RegionTimer(this, new RegionTimerCallback(RangeEnd), 9500);
            IsRanged = true;
            return 0;
        }
        public int RangeEnd(RegionTimer timer)
        {
            IsRanged = false;
            return 0;
        }
        public static bool IsRanged = false;
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (IsRanged == false)
            {
                this.SwitchWeapon(eActiveWeaponSlot.Standard);
                this.VisibleActiveWeaponSlots = 16;

                if (ad != null && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
                {
                    this.Strength = 10;
                    this.styleComponent.NextCombatStyle = slam;
                }
                if (ad != null && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
                {
                    this.Strength = 10;
                    this.styleComponent.NextCombatStyle = Taunt;
                }
            }
            if(ad.AttackResult == eAttackResult.HitStyle && ad.Style.ID == 228 && ad.Style.ClassID == 2)
            {
                if (IsRanged == false)
                {
                    this.styleComponent.NextCombatStyle = null;
                    new RegionTimer(this, new RegionTimerCallback(Range), 200);
                }
            }
            if(IsRanged)
            {
                return;
            }
            else
            base.OnAttackEnemy(ad);
        }
        public override void SwitchWeapon(eActiveWeaponSlot slot)
        {
            if (IsRanged)
            {
                return;
            }
            else
            {
                base.SwitchWeapon(slot);
            }
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 35; // dmg reduction for melee dmg
                case eDamageType.Crush: return 35; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (this.IsOutOfTetherRange)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
                        damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
                        damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
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
        public override double AttackDamage(InventoryItem weapon)
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 600;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.45;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7713);
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
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 51, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 53, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 52, 0);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 80, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 54, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 653, 0, 0);
            template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 1042, 0, 0);
            template.AddNPCEquipment(eInventorySlot.DistanceWeapon, 132, 0, 0);
            Inventory = template.CloseTemplate();
            IsRanged = false;
            if (!this.Styles.Contains(slam))
            {
                Styles.Add(slam);
            }
            if (!this.Styles.Contains(Taunt))
            {
                Styles.Add(Taunt);
            }
            MeleeDamageType = eDamageType.Thrust;
            SergeantEddisonBrain sbrain = new SergeantEddisonBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Sergeant Eddison", 277, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Sergeant Eddison not found, creating it...");

                log.Warn("Initializing Sergeant Eddison...");
                SergeantEddison HOC = new SergeantEddison();
                HOC.Name = "Sergeant Eddison";
                HOC.Model = 270;
                HOC.Realm = 0;
                HOC.Level = 50;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 31555;
                HOC.Y = 33788;
                HOC.Z = 15094;
                HOC.Heading = 3041;
                SergeantEddisonBrain ubrain = new SergeantEddisonBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Sergeant Eddison exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class SergeantEddisonBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SergeantEddisonBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
            ThinkInterval = 1500;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                SergeantEddison.IsRanged = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            base.Think();
        }
    }
}



