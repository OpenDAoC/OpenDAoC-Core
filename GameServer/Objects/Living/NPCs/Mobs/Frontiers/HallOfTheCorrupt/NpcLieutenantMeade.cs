﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class NpcLieutenantMeade : GameNpc
    {
        public NpcLieutenantMeade() : base()
        {
        }
        public static int TauntID = 357;
        public static int TauntClassID = 19;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int SideID = 362;
        public static int SideClassID = 19;
        public static Style Side = SkillBase.GetStyleByID(SideID, SideClassID);

        public static int SlamID = 228;
        public static int SlamClassID = 19;
        public static Style slam = SkillBase.GetStyleByID(SlamID, SlamClassID);
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7716);
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
            BodyType = (ushort)NpcTemplateMgr.EBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 186, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 188, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 187, 0);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 189, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 190, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 865, 0, 0);
            template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 1041, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.Standard);
            if (!this.Styles.Contains(Side))
            {
                Styles.Add(Side);
            }
            if (!this.Styles.Contains(Taunt))
            {
                Styles.Add(Taunt);
            }
            if (!Styles.Contains(slam))
            {
                Styles.Add(slam);
            }
            LieutenantMeadeBrain.CanWalk = false;
            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = EDamageType.Slash;
            LieutenantMeadeBrain sbrain = new LieutenantMeadeBrain();
            SetOwnBrain(sbrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNpc[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lieutenant Meade", 277, (ERealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Lieutenant Meade not found, creating it...");

                log.Warn("Initializing Lieutenant Meade...");
                NpcLieutenantMeade HOC = new NpcLieutenantMeade();
                HOC.Name = "Lieutenant Meade";
                HOC.Model = 48;
                HOC.Realm = 0;
                HOC.Level = 50;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 31874;
                HOC.Y = 34994;
                HOC.Z = 15366;
                HOC.Heading = 9;
                LieutenantMeadeBrain ubrain = new LieutenantMeadeBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Lieutenant Meade exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class LieutenantMeadeBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LieutenantMeadeBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 1500;
        }
        public static bool CanWalk = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;              
                CanWalk = false;
                lock (Body.effectListComponent.EffectsLock)
                {
                    var effects = Body.effectListComponent.GetAllPulseEffects();
                    for (int i = 0; i < effects.Count; i++)
                    {
                        EcsPulseEffect effect = effects[i];
                        if (effect == null)
                            continue;

                        if (effect == null)
                            continue;
                        if (effect.SpellHandler.Spell.Pulse == 1)
                        {
                            EffectService.RequestCancelConcEffect(effect);//cancel here all pulse effect
                        }
                    }
                }
            }
            if (Body.InCombat && HasAggro)
            {
                Body.CastSpell(Meade_Pulse, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                if (Body.TargetObject != null)
                {
                    GameLiving living = Body.TargetObject as GameLiving;
                    float angle = Body.TargetObject.GetAngle(Body);
                    if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                    {
                        Body.styleComponent.NextCombatStyle = NpcLieutenantMeade.slam;//check if target has stun or immunity if not slam
                    }
                    if (living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                    {
                        if (CanWalk == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WalkSide), 500);
                            CanWalk = true;
                        }
                    }
                    if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
                    {
                        Body.styleComponent.NextCombatStyle = NpcLieutenantMeade.Side;
                    }
                    if(!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                    {
                        Body.styleComponent.NextCombatStyle = NpcLieutenantMeade.Taunt;
                    }
                    if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                    {
                        CanWalk = false;//reset flag 
                    }
                }
            }
            base.Think();
        }
        public int WalkSide(ECSGameTimer timer)
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
        private Spell m_Meade_Pulse;

        private Spell Meade_Pulse
        {
            get
            {
                if (m_Meade_Pulse == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 8;
                    spell.ClientEffect = 9637;
                    spell.Icon = 9637;
                    spell.TooltipId = 9637;
                    spell.Value = 21;
                    spell.Name = "Aching Curse";
                    spell.Description = "Target does 21% less damage with melee attacks.";
                    spell.Message1 = "Your attacks lose effectiveness as your will to fight is sapped!";
                    spell.Message2 = "{0} seems to have lost some will to fight!";
                    spell.Pulse = 1;
                    spell.Duration = 10;
                    spell.Frequency = 100;
                    spell.Radius = 350;
                    spell.Range = 0;
                    spell.SpellID = 11782;
                    spell.Target = ESpellTarget.Enemy.ToString();
                    spell.Type = ESpellType.MeleeDamageDebuff.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Spirit;
                    m_Meade_Pulse = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Meade_Pulse);
                }
                return m_Meade_Pulse;
            }
        }
    }
}





