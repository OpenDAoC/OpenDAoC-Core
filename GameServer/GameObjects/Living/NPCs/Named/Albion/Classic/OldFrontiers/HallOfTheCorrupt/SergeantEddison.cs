using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Styles;

namespace Core.GS
{
    public class SergeantEddison : GameNpc
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
        public int Range(EcsGameTimer timer)
        {
            this.Strength = 250;
            this.SwitchToRanged(this.TargetObject);
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RangeEnd), 9500);
            IsRanged = true;
            return 0;
        }
        public int RangeEnd(EcsGameTimer timer)
        {
            IsRanged = false;
            return 0;
        }
        public static bool IsRanged = false;
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (IsRanged == false)
            {
                this.SwitchWeapon(EActiveWeaponSlot.Standard);
                this.VisibleActiveWeaponSlots = 16;

                if (ad != null && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                {
                    this.Strength = 10;
                    this.styleComponent.NextCombatStyle = slam;
                }
                if (ad != null && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                {
                    this.Strength = 10;
                    this.styleComponent.NextCombatStyle = Taunt;
                }
            }
            if(ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 228 && ad.Style.ClassID == 2)
            {
                if (IsRanged == false)
                {
                    this.styleComponent.NextCombatStyle = null;
                    new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Range), 200);
                }
            }
            if(IsRanged)
            {
                return;
            }
            else
            base.OnAttackEnemy(ad);
        }
        public override void SwitchWeapon(EActiveWeaponSlot slot)
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
                            truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System,
                                EChatLoc.CL_ChatWindow);
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
            if (this.IsAlive && keyName == GS.Abilities.CCImmunity)
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
            BodyType = (ushort)EBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 51, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 53, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 52, 0);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 80, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 54, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 653, 0, 0);
            template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1042, 0, 0);
            template.AddNPCEquipment(EInventorySlot.DistanceWeapon, 132, 0, 0);
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
            MeleeDamageType = EDamageType.Thrust;
            SergeantEddisonBrain sbrain = new SergeantEddisonBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNpc[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Sergeant Eddison", 277, (ERealm)0);
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