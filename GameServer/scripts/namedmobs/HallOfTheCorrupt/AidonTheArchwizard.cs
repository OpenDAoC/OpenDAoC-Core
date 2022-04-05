using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class AidonTheArchwizard : GameEpicBoss
    {
        public AidonTheArchwizard() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 75; // dmg reduction for melee dmg
                case eDamageType.Crush: return 75; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 75; // dmg reduction for melee dmg
                default: return 50; // dmg reduction for rest resists
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
            return 1000;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7721);
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
            RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            AidonTheArchwizardBrain.IsPulled = false;
            AidonTheArchwizardBrain.CanCast = false;

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            AidonTheArchwizardBrain sbrain = new AidonTheArchwizardBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Aidon the Archwizard", 277, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Aidon the Archwizard found, creating it...");

                log.Warn("Initializing Aidon the Archwizard...");
                AidonTheArchwizard HOC = new AidonTheArchwizard();
                HOC.Name = "Aidon the Archwizard";
                HOC.Model = 61;
                HOC.Realm = 0;
                HOC.Level = 75;
                HOC.Size = 60;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.MeleeDamageType = eDamageType.Crush;
                HOC.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 31353;
                HOC.Y = 37634;
                HOC.Z = 14873;
                HOC.Heading = 2070;
                AidonTheArchwizardBrain ubrain = new AidonTheArchwizardBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Aidon the Archwizard exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class AidonTheArchwizardBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AidonTheArchwizardBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 1500;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public static bool IsPulled = false;
        public static bool spawn_copies = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (spawn_copies == false)
            {
                SpawnCopies();
                spawn_copies = true;
            }
            if (IsPulled == false)
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if(npc.Brain is AidonCopyAirBrain)
                                AddAggroListTo(npc.Brain as AidonCopyAirBrain);

                            if (npc.Brain is AidonCopyFireBrain)
                                AddAggroListTo(npc.Brain as AidonCopyFireBrain);

                            if (npc.Brain is AidonCopyIceBrain)
                                AddAggroListTo(npc.Brain as AidonCopyIceBrain);

                            if (npc.Brain is AidonCopyEarthBrain)
                                AddAggroListTo(npc.Brain as AidonCopyEarthBrain);

                            IsPulled = true;
                        }                      
                    }
                }
            }

            base.OnAttackedByEnemy(ad);
        }
        public static bool CanCast = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                IsPulled = false;
                spawn_copies = false;
                CanCast = false;
                AidonCopyFire.CopyCountFire = 0;
                AidonCopyIce.CopyCountIce = 0;
                AidonCopyAir.CopyCountAir = 0;
                AidonCopyEarth.CopyCountEarth = 0;
                foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && (npc.Brain is AidonCopyFireBrain || npc.Brain is AidonCopyAirBrain || npc.Brain is AidonCopyIceBrain || npc.Brain is AidonCopyEarthBrain))
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat && HasAggro)
            {
                if (AidonCopyAir.CopyCountAir == 0)
                {
                    AidonCopyAir Add3 = new AidonCopyAir();
                    Add3.X = 31080;
                    Add3.Y = 37974;
                    Add3.Z = 14866;
                    Add3.CurrentRegionID = 277;
                    Add3.Heading = 3059;
                    Add3.AddToWorld();
                }
                else if (AidonCopyFire.CopyCountFire == 0)
                {
                    AidonCopyFire Add1 = new AidonCopyFire();
                    Add1.X = 31649;
                    Add1.Y = 37316;
                    Add1.Z = 14866;
                    Add1.CurrentRegionID = 277;
                    Add1.Heading = 1015;
                    Add1.AddToWorld();
                }
                else if (AidonCopyEarth.CopyCountEarth == 0)
                {
                    AidonCopyEarth Add4 = new AidonCopyEarth();
                    Add4.X = 31637;
                    Add4.Y = 37968;
                    Add4.Z = 14869;
                    Add4.CurrentRegionID = 277;
                    Add4.Heading = 1019;
                    Add4.AddToWorld();
                }
                else if (AidonCopyIce.CopyCountIce == 0)
                {
                    AidonCopyIce Add2 = new AidonCopyIce();
                    Add2.X = 31083;
                    Add2.Y = 37323;
                    Add2.Z = 14869;
                    Add2.CurrentRegionID = 277;
                    Add2.Heading = 3008;
                    Add2.AddToWorld();
                }
                if (!Body.effectListComponent.ContainsEffectForEffectType(eEffect.DamageReturn))
                {
                    GameLiving oldTarget = Body.TargetObject as GameLiving;
                    Body.StopFollowing();
                    if (Body.TargetObject != Body)
                    {
                        Body.TargetObject = Body;
                        Body.CastSpell(FireDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                        if (oldTarget != null)
                            Body.TargetObject = oldTarget;
                    }
                }
                if (CanCast == false)
                {
                    new RegionTimer(Body, new RegionTimerCallback(CastDD), Util.Random(10000, 15000));
                    CanCast = true;
                }
            }
            base.Think();
        }
        public int CastDD(RegionTimer Timer)
        {
            if (Body.IsAlive)
            {
                Body.CastSpell(AidonBoss_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            CanCast = false;
            return 0;
        }
        public void SpawnCopies()
        {
            if (AidonCopyFire.CopyCountFire == 0)
            {
                AidonCopyFire Add1 = new AidonCopyFire();
                Add1.X = 31649;
                Add1.Y = 37316;
                Add1.Z = 14866;
                Add1.CurrentRegionID = 277;
                Add1.Heading = 1015;
                Add1.AddToWorld();
            }
            if (AidonCopyIce.CopyCountIce == 0)
            {
                AidonCopyIce Add2 = new AidonCopyIce();
                Add2.X = 31083;
                Add2.Y = 37323;
                Add2.Z = 14869;
                Add2.CurrentRegionID = 277;
                Add2.Heading = 3008;
                Add2.AddToWorld();
            }
            if (AidonCopyAir.CopyCountAir == 0)
            {
                AidonCopyAir Add3 = new AidonCopyAir();
                Add3.X = 31080;
                Add3.Y = 37974;
                Add3.Z = 14866;
                Add3.CurrentRegionID = 277;
                Add3.Heading = 3059;
                Add3.AddToWorld();
            }
            if (AidonCopyEarth.CopyCountEarth == 0)
            {
                AidonCopyEarth Add4 = new AidonCopyEarth();
                Add4.X = 31637;
                Add4.Y = 37968;
                Add4.Z = 14869;
                Add4.CurrentRegionID = 277;
                Add4.Heading = 1019;
                Add4.AddToWorld();
            }
        }
        public Spell m_AidonBoss_DD;
        public Spell AidonBoss_DD
        {
            get
            {
                if (m_AidonBoss_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 360;
                    spell.Icon = 360;
                    spell.TooltipId = 360;
                    spell.Damage = 1000;
                    spell.Name = "Aidons's Fire";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11771;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
                    m_AidonBoss_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AidonBoss_DD);
                }

                return m_AidonBoss_DD;
            }
        }
        private Spell m_FireDS;
        private Spell FireDS
        {
            get
            {
                if (m_FireDS == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 60;
                    spell.ClientEffect = 57;
                    spell.Icon = 57;
                    spell.Damage = 100;
                    spell.Duration = 60;
                    spell.Name = "Aidon's Damage Shield";
                    spell.TooltipId = 57;
                    spell.SpellID = 11770;
                    spell.Target = "Self";
                    spell.Type = "DamageShield";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
                    m_FireDS = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireDS);
                }
                return m_FireDS;
            }
        }
    }
}
///////////////////////////////////////////////////////////////////Aidon Copies///////////////////////////////////////////////////////
namespace DOL.GS
{
    public class AidonCopyFire : GameNPC
    {
        public AidonCopyFire() : base()
        {
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public static int CopyCountFire = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountFire;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = eDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = eFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            ++CopyCountFire;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = eRealm.None;

            Strength = 100;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            AidonCopyFireBrain adds = new AidonCopyFireBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class AidonCopyFireBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AidonCopyFireBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }

        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            base.Think();
        }
        private Spell m_Aidon_DD;

        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 360;
                    spell.Icon = 360;
                    spell.TooltipId = 360;
                    spell.Damage = 500;
                    spell.Name = "Aidons's Fire";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11766;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
                    m_Aidon_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aidon_DD);
                }

                return m_Aidon_DD;
            }
        }
    }
}
/// <summary>
/// ///////////////////////////////////////////////////////////////Ice//////////////////////////////////////
/// </summary>
namespace DOL.GS
{
    public class AidonCopyIce : GameNPC
    {
        public AidonCopyIce() : base()
        {
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public static int CopyCountIce = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountIce;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = eDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = eFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            ++CopyCountIce;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = eRealm.None;

            Strength = 100;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            AidonCopyIceBrain adds = new AidonCopyIceBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class AidonCopyIceBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AidonCopyIceBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }

        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            base.Think();
        }
        private Spell m_Aidon_DD;

        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 161;
                    spell.Icon = 161;
                    spell.TooltipId = 360;
                    spell.Damage = 500;
                    spell.Value = 45;
                    spell.Duration = 20;
                    spell.Name = "Aidons's Ice";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11767;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DamageSpeedDecreaseNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_Aidon_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aidon_DD);
                }

                return m_Aidon_DD;
            }
        }
    }
}
///////////////////////////////////////////////////////////////////Air//////////////////////////////////
namespace DOL.GS
{
    public class AidonCopyAir : GameNPC
    {
        public AidonCopyAir() : base()
        {
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public static int CopyCountAir = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountAir;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = eDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = eFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            ++CopyCountAir;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = eRealm.None;

            Strength = 100;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            AidonCopyAirBrain adds = new AidonCopyAirBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class AidonCopyAirBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AidonCopyAirBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }

        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            base.Think();
        }
        private Spell m_Aidon_DD;

        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 479;
                    spell.Icon = 479;
                    spell.TooltipId = 360;
                    spell.Damage = 500;
                    spell.Name = "Aidons's Air";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11768;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit;
                    m_Aidon_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aidon_DD);
                }

                return m_Aidon_DD;
            }
        }
    }
}
//////////////////////////////////////////////////////////////////////////////////////Earth///////////////////////////////////////////////////
namespace DOL.GS
{
    public class AidonCopyEarth : GameNPC
    {
        public AidonCopyEarth() : base()
        {
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public static int CopyCountEarth = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountEarth;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = eDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = eFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            ++CopyCountEarth;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = eRealm.None;

            Strength = 100;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 150;
            Intelligence = 150;

            AidonCopyEarthBrain adds = new AidonCopyEarthBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class AidonCopyEarthBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AidonCopyEarthBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }

        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            base.Think();
        }
        private Spell m_Aidon_DD;

        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 219;
                    spell.Icon = 219;
                    spell.TooltipId = 360;
                    spell.Damage = 500;
                    spell.Name = "Aidons's Earth";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11769;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Matter;
                    m_Aidon_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aidon_DD);
                }

                return m_Aidon_DD;
            }
        }
    }
}

