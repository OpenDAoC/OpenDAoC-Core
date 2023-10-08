using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class LordGildas : GameEpicBoss
    {
        public LordGildas() : base()
        {
        }
        public static int TauntID = 66;
        public static int TauntClassID = 2;
        public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int SlamID = 228;
        public static int SlamClassID = 2;
        public static Style slam = SkillBase.GetStyleByID(SlamID, SlamClassID);

        public static int BackStyleID = 113;
        public static int BackStyleClassID = 2;
        public static Style BackStyle = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);

        public static int AfterStyleID = 97;
        public static int AfterStyleClassID = 2;
        public static Style AfterStyle = SkillBase.GetStyleByID(AfterStyleID, AfterStyleClassID);

        public static int PoleAnytimerID = 93;
        public static int PoleAnytimerClassID = 2;
        public static Style PoleAnytimer = SkillBase.GetStyleByID(PoleAnytimerID, PoleAnytimerClassID);

        public static int Taunt2hID = 103;
        public static int Taunt2hClassID = 2;
        public static Style Taunt2h = SkillBase.GetStyleByID(Taunt2hID, Taunt2hClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            base.OnAttackEnemy(ad);
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 30; // dmg reduction for melee dmg
                case EDamageType.Crush: return 30; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 30; // dmg reduction for melee dmg
                default: return 40; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 40000; }
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (IsOutOfTetherRange)
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
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7719);
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
            RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
            template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1077, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.Standard);
            LordGildasBrain.Stage2 = false;
            LordGildasBrain.CanWalk = false;
            LordGildasBrain.Reset_Gildas = false;
            if (!Styles.Contains(taunt))
                Styles.Add(taunt);
            if (!Styles.Contains(slam))
                Styles.Add(slam);
            if (!Styles.Contains(BackStyle))
                Styles.Add(BackStyle);
            if (!Styles.Contains(AfterStyle))
                Styles.Add(AfterStyle);
            if (!Styles.Contains(PoleAnytimer))
                Styles.Add(PoleAnytimer);
            if (!Styles.Contains(Taunt2h))
                Styles.Add(Taunt2h);
            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = EDamageType.Slash;
            LordGildasBrain sbrain = new LordGildasBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lord Gildas", 277, (ERealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Lord Gildas found, creating it...");

                log.Warn("Initializing Lord Gildas...");
                LordGildas HOC = new LordGildas();
                HOC.Name = "Lord Gildas";
                HOC.Model = 40;
                HOC.Realm = 0;
                HOC.Level = 75;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.MeleeDamageType = EDamageType.Slash;
                HOC.RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 29015;
                HOC.Y = 41910;
                HOC.Z = 12933;
                HOC.Heading = 2063;
                LordGildasBrain ubrain = new LordGildasBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Lord Gildas exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class LordGildasBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LordGildasBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 500;
        }
        public static bool CanWalk = false;
        public static bool Stage2 = false;
        public static bool Reset_Gildas = false;
        public int ResetGildas(ECSGameTimer timer)
        {
            Reset_Gildas = false;
            return 0;
        }
        public override void Think()
        {       
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                CanWalk = false;                              
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                if (Reset_Gildas == false)
                {
                    INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7719);
                    GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
                    template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
                    template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
                    template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
                    template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
                    template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
                    template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
                    template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1077, 0, 0);
                    template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
                    Body.Inventory = template.CloseTemplate();
                    Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                    Body.VisibleActiveWeaponSlots = 16;
                    if (!Body.Styles.Contains(LordGildas.slam))
                        Body.Styles.Add(LordGildas.slam);
                    if (!Body.Styles.Contains(LordGildas.taunt))
                        Body.Styles.Add(LordGildas.taunt);
                    if (!Body.Styles.Contains(LordGildas.BackStyle))
                        Body.Styles.Add(LordGildas.BackStyle);
                    if (!Body.Styles.Contains(LordGildas.Taunt2h))
                        Body.Styles.Remove(LordGildas.Taunt2h);
                    Body.Strength = npcTemplate.Strength;
                    Body.ParryChance = npcTemplate.ParryChance;
                    Body.BlockChance = npcTemplate.BlockChance;
                    Stage2 = false;
                    Body.styleComponent.NextCombatStyle = LordGildas.taunt;
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetGildas), 7000);
                    Reset_Gildas = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (Body.TargetObject != null)
                {
                    if (Stage2 == false)
                    {
                        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7719);
                        float angle = Body.TargetObject.GetAngle(Body);
                        if (angle >= 160 && angle <= 200)
                        {
                            Body.Strength = 400;
                            Body.ParryChance = 60;
                            Body.BlockChance = 0;
                            Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Body.VisibleActiveWeaponSlots = 34;
                            Body.styleComponent.NextCombatBackupStyle = LordGildas.Taunt2h;
                            Body.styleComponent.NextCombatStyle = LordGildas.BackStyle;//do backstyle when angle allow it
                        }
                        else
                        {
                            Body.Strength = npcTemplate.Strength;
                            Body.ParryChance = 25;
                            Body.BlockChance = 75;
                            Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                            Body.VisibleActiveWeaponSlots = 16;
                            Body.styleComponent.NextCombatStyle = LordGildas.taunt;//if not backstyle for angle then do taunt
                        }
                        if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                        {
                            Body.Strength = npcTemplate.Strength;
                            Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                            Body.VisibleActiveWeaponSlots = 16;
                            Body.ParryChance = 25;
                            Body.BlockChance = 80;
                            Body.styleComponent.NextCombatStyle = LordGildas.slam;//check if target has stun or immunity if not slam
                        }
                        if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                        {
                            if (CanWalk == false)
                            {
                                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkBack), 500);//if target got stun then start timer to run behind it
                                CanWalk = true;
                            }
                        }
                        if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                        {
                            CanWalk = false;//reset flag so can slam again
                        }
                    }
                }
                if(Body.HealthPercent < 50 && Stage2==false)//boss change to polearm armsman
                {
                    GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                    template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
                    template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
                    template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
                    template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
                    template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
                    template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
                    template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 70, 0, 0);
                    Body.Inventory = template.CloseTemplate();
                    if(Body.Styles.Contains(LordGildas.slam))
                        Body.Styles.Remove(LordGildas.slam);
                    if (Body.Styles.Contains(LordGildas.taunt))
                        Body.Styles.Remove(LordGildas.taunt);
                    if (Body.Styles.Contains(LordGildas.BackStyle))
                        Body.Styles.Remove(LordGildas.BackStyle);
                    if (Body.Styles.Contains(LordGildas.Taunt2h))
                        Body.Styles.Remove(LordGildas.Taunt2h);
                    Stage2 = true;
                }
                if(Stage2 == true)
                {
                    Body.styleComponent.NextCombatBackupStyle = LordGildas.PoleAnytimer;
                    Body.styleComponent.NextCombatStyle = LordGildas.AfterStyle;
                    Body.Strength = 340;
                    Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                    Body.MeleeDamageType = EDamageType.Crush;
                    Body.VisibleActiveWeaponSlots = 34;
                    Body.ParryChance = 60;
                    Body.BlockChance = 0;
                }
            }
            base.Think();
        }
        public int WalkBack(ECSGameTimer timer)
        {
            if (Body.InCombat && HasAggro && Body.TargetObject != null && Stage2==false)
            {
                if (Body.TargetObject is GameLiving)
                {
                    GameLiving living = Body.TargetObject as GameLiving;
                    float angle = living.GetAngle(Body);
                    Point2D positionalPoint;
                    positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (180 * (4096.0 / 360.0))), 65);
                    //Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
                    Body.X = positionalPoint.X;
                    Body.Y = positionalPoint.Y;
                    Body.Z = living.Z;
                    Body.Heading = 1250;
                }
            }
            return 0;
        }
    }
}
