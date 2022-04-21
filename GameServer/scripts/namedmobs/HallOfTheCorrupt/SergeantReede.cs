using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
namespace DOL.GS
{
    public class SergeantReede : GameNPC
    {
        public SergeantReede() : base()
        {
        }
        public static int AfterEvadeID = 145;
        public static int AfterEvadeClassID = 9;
        public static Style AfterEvade = SkillBase.GetStyleByID(AfterEvadeID, AfterEvadeClassID);

        public static int TauntID = 119;
        public static int TauntClassID = 11;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int SideID = 126;//flank
        public static int SideClassID = 11;
        public static Style Side = SkillBase.GetStyleByID(SideID, SideClassID);

        public static int SideFollowUpID = 128;//shadow's rain flank followup
        public static int SideFollowUpClassID = 11;
        public static Style SideFollowUp = SkillBase.GetStyleByID(SideFollowUpID, SideFollowUpClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {

            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            base.OnAttackEnemy(ad);
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
            return 400;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
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
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7715);
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
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 186, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 188, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 187, 0);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 189, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 190, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 653, 0, 0);
            template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 25, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.Standard);
            if (!this.Styles.Contains(AfterEvade))
            {
                Styles.Add(AfterEvade);
            }
            if (!this.Styles.Contains(Taunt))
            {
                Styles.Add(Taunt);
            }
            if (!this.Styles.Contains(Side))
            {
                Styles.Add(Side);
            }
            if (!this.Styles.Contains(SideFollowUp))
            {
                Styles.Add(SideFollowUp);
            }
            SergeantReedeBrain.CanWalk = false;
            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = eDamageType.Thrust;
            SergeantReedeBrain sbrain = new SergeantReedeBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Sergeant Reede", 277, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Sergeant Reede not found, creating it...");

                log.Warn("Initializing Sergeant Reede...");
                SergeantReede HOC = new SergeantReede();
                HOC.Name = "Sergeant Reede";
                HOC.Model = 7;
                HOC.Realm = 0;
                HOC.Level = 50;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 32371;
                HOC.Y = 34757;
                HOC.Z = 15366;
                HOC.Heading = 46;
                SergeantReedeBrain ubrain = new SergeantReedeBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Sergeant Reede exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class SergeantReedeBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SergeantReedeBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 1500;
        }
        public static bool CanWalk = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                CanWalk = false;
            }
            if (Body.InCombat && HasAggro)
            {
                if (Body.TargetObject != null)
                {
                    GameLiving living = Body.TargetObject as GameLiving;
                    float angle = Body.TargetObject.GetAngle(Body);
                    if (living.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
                    {
                        if(CanWalk==false)
                        {
                            new RegionTimer(Body, new RegionTimerCallback(WalkSide), 500);
                            CanWalk = true;
                        }
                    }
                    if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
                    {
                        Body.styleComponent.NextCombatBackupStyle = SergeantReede.Side;
                        Body.styleComponent.NextCombatStyle = SergeantReede.SideFollowUp;
                    }
                    else
                    {
                        Body.styleComponent.NextCombatBackupStyle = SergeantReede.Taunt;
                        Body.styleComponent.NextCombatStyle = SergeantReede.AfterEvade;
                    }
                    if (!living.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) && !living.effectListComponent.ContainsEffectForEffectType(eEffect.Stun))
                    {
                        CanWalk = false;//reset flag 
                    }
                }
            }
            base.Think();
        }
        public int WalkSide(RegionTimer timer)
        {
            if (Body.InCombat && HasAggro && Body.TargetObject != null)
            {
                if (Body.TargetObject is GameLiving)
                {
                    GameLiving living = Body.TargetObject as GameLiving;
                    float angle = living.GetAngle(Body);
                    Point2D positionalPoint;
                    positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (90 * (4096.0 / 360.0))), 65);
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



