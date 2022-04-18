using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
namespace DOL.GS
{
    public class LadyDarra : GameEpicBoss
    {
        public LadyDarra() : base()
        {
        }
        public static int TauntID = 66;
        public static int TauntClassID = 1; //pala
        public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int AfterParryID = 246;
        public static int AfterParryClassID = 44;
        public static Style after_parry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

        public static int AfterBlockID = 229;
        public static int AfterBlockClassID = 1;//pala
        public static Style after_block = SkillBase.GetStyleByID(AfterBlockID, AfterBlockClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            if (ad != null && ad.AttackResult == eAttackResult.Blocked)
            {
                this.styleComponent.NextCombatBackupStyle = taunt;
                this.styleComponent.NextCombatStyle = after_block; 
            }
            if (ad != null && ad.AttackResult == eAttackResult.Parried)
            {
                this.styleComponent.NextCombatBackupStyle = taunt; 
                this.styleComponent.NextCombatStyle = after_parry; 
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
                this.styleComponent.NextCombatStyle = taunt; //boss hit unstyled/styled so taunt
            }
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
            return base.AttackDamage(weapon) * Strength / 100;
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
            return 800;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7720);
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
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 48, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 47, 0);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 49, 0, 0, 4);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 50, 0, 0, 5);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 4, 0, 0);
            template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 1077, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.Standard);
            Styles.Add(taunt);
            Styles.Add(after_block);
            LadyDarraBrain.reset_darra = false;
            spawn_palas = false;

            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = eDamageType.Slash;
            LadyDarraBrain sbrain = new LadyDarraBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            bool success = base.AddToWorld();
            if (success)
            {
                SpawnPaladins();
            }
            return success;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lady Darra", 277, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Lady Darra found, creating it...");

                log.Warn("Initializing Lady Darra...");
                LadyDarra HOC = new LadyDarra();
                HOC.Name = "Lady Darra";
                HOC.Model = 35;
                HOC.Realm = 0;
                HOC.Level = 68;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.MeleeDamageType = eDamageType.Slash;
                HOC.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 29551;
                HOC.Y = 31554;
                HOC.Z = 13941;
                HOC.Heading = 3014;
                LadyDarraBrain ubrain = new LadyDarraBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Lady Darra exist ingame, remove it and restart server if you want to add by script code.");
        }
        public static bool spawn_palas = false;
        public void SpawnPaladins()
        {
            if (SpectralPaladin.paladins_count == 0 && spawn_palas==false)
            {
                SpectralPaladin Add1 = new SpectralPaladin();
                Add1.X = 30000;
                Add1.Y = 31057;
                Add1.Z = 13893;
                Add1.CurrentRegionID = 277;
                Add1.Heading = 479;
                Add1.RespawnInterval = -1;
                Add1.AddToWorld();

                SpectralPaladin Add2 = new SpectralPaladin();
                Add2.X = 29134;
                Add2.Y = 31054;
                Add2.Z = 13893;
                Add2.CurrentRegionID = 277;
                Add2.Heading = 3565;
                Add2.RespawnInterval = -1;
                Add2.AddToWorld();

                SpectralPaladin Add3 = new SpectralPaladin();
                Add3.X = 29128;
                Add3.Y = 31924;
                Add3.Z = 13893;
                Add3.CurrentRegionID = 277;
                Add3.Heading = 2552;
                Add3.RespawnInterval = -1;
                Add3.AddToWorld();

                SpectralPaladin Add4 = new SpectralPaladin();
                Add4.X = 30004;
                Add4.Y = 31928;
                Add4.Z = 13893;
                Add4.CurrentRegionID = 277;
                Add4.Heading = 1520;
                Add4.RespawnInterval = -1;
                Add4.AddToWorld();
                spawn_palas = true;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class LadyDarraBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LadyDarraBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 1500;
        }
        public static bool reset_darra = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;              
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
                if (reset_darra == false)
                {
                    if (SpectralPaladin.paladins_count <= 3)
                    {
                        LadyDarra.spawn_palas = false;
                        foreach (GameNPC pala in Body.GetNPCsInRadius(2000))
                        {
                            if (pala != null)
                            {
                                if (pala.IsAlive && pala.Brain is SpectralPaladinBrain)
                                {
                                    pala.Die(Body);
                                }
                            }
                        }
                        LadyDarra darra = new LadyDarra();
                        darra.SpawnPaladins();
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDarra), 7000);
                        reset_darra = true;
                    }
                }
            }
            if (Body.InCombat && HasAggro)
            {
            }
            base.Think();
        }
        public long ResetDarra(ECSGameTimer timer)
        {
            reset_darra = false;
            return 0;
        }
    }
}
//////////////////////////////////////////////////////////////////////////Spectral Paladins///////////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class SpectralPaladin : GameNPC
    {
        public SpectralPaladin() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 25; // dmg reduction for melee dmg
                case eDamageType.Crush: return 25; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 25; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 60;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 400;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public static int paladins_count = 0;
        public override void Die(GameObject killer)
        {
            --paladins_count;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            RespawnInterval = -1;
            Flags = eFlags.GHOST;
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7710);
            LoadTemplate(npcTemplate);
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 46, 43, 0, 6);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 48, 43);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 47, 43);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 49, 43, 0, 4);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 50, 43, 0, 5);
            template.AddNPCEquipment(eInventorySlot.HeadArmor, 93, 43, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 57, 430, 0, 0);
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 4, 43, 0);
            template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 1077, 43, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.Standard);
            VisibleActiveWeaponSlots = 16;
            ++paladins_count;

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            SpectralPaladinBrain adds = new SpectralPaladinBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class SpectralPaladinBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SpectralPaladinBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (Body.IsAlive)
            {
                foreach(GameNPC Darra in Body.GetNPCsInRadius(2000))
                {
                    if(Darra != null)
                    {
                        if(Darra.IsAlive && Darra.Brain is LadyDarraBrain)
                        {
                            if (Darra.HealthPercent < 100)
                            {
                                if (Body.TargetObject != Darra)
                                {
                                    Body.TargetObject = Darra;
                                }
                                if (!Body.IsCasting)
                                {
                                    Body.StopFollowing();
                                    Body.TurnTo(Darra);
                                    Body.CastSpell(Paladin_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                                }
                            }
                        }
                    }
                }                
            }

            base.Think();
        }
        private Spell m_Paladin_Heal;

        private Spell Paladin_Heal
        {
            get
            {
                if (m_Paladin_Heal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 4;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 1358;
                    spell.Icon = 1358;
                    spell.TooltipId = 360;
                    spell.Name = "Spectral Heal";
                    spell.Value = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11776;
                    spell.Target = eSpellTarget.Realm.ToString();
                    spell.Type = eSpellType.Heal.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_Paladin_Heal = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Paladin_Heal);
                }

                return m_Paladin_Heal;
            }
        }
    }
}