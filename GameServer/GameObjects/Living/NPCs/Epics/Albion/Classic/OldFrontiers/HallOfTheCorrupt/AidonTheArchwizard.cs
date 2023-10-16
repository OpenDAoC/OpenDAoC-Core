using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class AidonTheArchwizard : GameEpicBoss
    {
        public AidonTheArchwizard() : base()
        {
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
                ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)EBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            AidonTheArchwizardBrain.IsPulled = false;
            AidonTheArchwizardBrain.CanCast = false;

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            AidonTheArchwizardBrain sbrain = new AidonTheArchwizardBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Aidon the Archwizard", 277, (ERealm)0);
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
                HOC.MeleeDamageType = EDamageType.Crush;
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
                player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
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
            if (Body.IsAlive)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
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
                        }                      
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public static bool CanCast = false;
        public bool SpawnCopiesAgain = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
                spawn_copies = false;
                CanCast = false;
                SpawnCopiesAgain = false;
                AidonCopyFire.CopyCountFire = 0;
                AidonCopyIce.CopyCountIce = 0;
                AidonCopyAir.CopyCountAir = 0;
                AidonCopyEarth.CopyCountEarth = 0;
                if (!RemoveAdds)
                {
                    foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && (npc.Brain is AidonCopyFireBrain || npc.Brain is AidonCopyAirBrain || npc.Brain is AidonCopyIceBrain || npc.Brain is AidonCopyEarthBrain))
                                npc.RemoveFromWorld();
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (Body.IsOutOfTetherRange)
                Body.Health = Body.MaxHealth;

            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
                Body.Health = Body.MaxHealth;

            if (Body.TargetObject != null && HasAggro)
            {
                if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.DamageReturn))
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
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDD), Util.Random(10000, 15000));
                    CanCast = true;
                }
                if (SpawnCopiesAgain == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMoreCopies), Util.Random(30000, 45000));
                    SpawnCopiesAgain = true;
                }
            }
            base.Think();
        }
        public int CastDD(EcsGameTimer Timer)
        {
            if (Body.IsAlive)
                Body.CastSpell(AidonBoss_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

            CanCast = false;
            return 0;
        }
        private int SpawnMoreCopies(EcsGameTimer timer)
        {
            if (HasAggro && AidonCopyFire.CopyCountFire == 0 && AidonCopyIce.CopyCountIce == 0 && AidonCopyAir.CopyCountAir == 0 && AidonCopyEarth.CopyCountEarth == 0)
            {
                switch (Util.Random(1, 4))
                {
                    case 1:
                        AidonCopyAir Add3 = new AidonCopyAir();
                        Add3.X = 31080;
                        Add3.Y = 37974;
                        Add3.Z = 14866;
                        Add3.CurrentRegionID = 277;
                        Add3.Heading = 3059;
                        Add3.AddToWorld();
                        break;
                    case 2:
                        AidonCopyFire Add1 = new AidonCopyFire();
                        Add1.X = 31649;
                        Add1.Y = 37316;
                        Add1.Z = 14866;
                        Add1.CurrentRegionID = 277;
                        Add1.Heading = 1015;
                        Add1.AddToWorld();
                        break;
                    case 3:
                        AidonCopyEarth Add4 = new AidonCopyEarth();
                        Add4.X = 31637;
                        Add4.Y = 37968;
                        Add4.Z = 14869;
                        Add4.CurrentRegionID = 277;
                        Add4.Heading = 1019;
                        Add4.AddToWorld();
                        break;
                    case 4:
                        AidonCopyIce Add2 = new AidonCopyIce();
                        Add2.X = 31083;
                        Add2.Y = 37323;
                        Add2.Z = 14869;
                        Add2.CurrentRegionID = 277;
                        Add2.Heading = 3008;
                        Add2.AddToWorld();
                        break;
                }
            }
            SpawnCopiesAgain = false;
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
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 360;
                    spell.Icon = 360;
                    spell.TooltipId = 360;
                    spell.Damage = 500;
                    spell.Name = "Aidons's Fire";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11771;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Heat;
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
                    DbSpell spell = new DbSpell();
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
                    spell.DamageType = (int)EDamageType.Heat;
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
    public class AidonCopyFire : GameNpc
    {
        public AidonCopyFire() : base()
        {
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public static int CopyCountFire = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountFire;
            base.Die(killer);
        }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 200; }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = EDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = ENpcFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            ++CopyCountFire;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = ERealm.None;
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
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);

            base.Think();
        }
        private Spell m_Aidon_DD;
        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 360;
                    spell.Icon = 360;
                    spell.TooltipId = 360;
                    spell.Damage = 300;
                    spell.Name = "Aidons's Fire";
                    spell.Radius = 350;
                    spell.Range = 2500;
                    spell.SpellID = 11766;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Heat;
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
    public class AidonCopyIce : GameNpc
    {
        public AidonCopyIce() : base()
        {
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public static int CopyCountIce = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountIce;
            base.Die(killer);
        }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 200; }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = EDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = ENpcFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            ++CopyCountIce;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = ERealm.None;
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
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);

            base.Think();
        }
        private Spell m_Aidon_DD;
        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 161;
                    spell.Icon = 161;
                    spell.TooltipId = 360;
                    spell.Damage = 300;
                    spell.Value = 45;
                    spell.Duration = 20;
                    spell.Name = "Aidons's Ice";
                    spell.Radius = 350;
                    spell.Range = 2500;
                    spell.SpellID = 11767;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DamageSpeedDecreaseNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Cold;
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
    public class AidonCopyAir : GameNpc
    {
        public AidonCopyAir() : base()
        {
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public static int CopyCountAir = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountAir;
            base.Die(killer);
        }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 200; }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = EDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = ENpcFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            ++CopyCountAir;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = ERealm.None;
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
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);

            base.Think();
        }
        private Spell m_Aidon_DD;
        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 479;
                    spell.Icon = 479;
                    spell.TooltipId = 360;
                    spell.Damage = 300;
                    spell.Name = "Aidons's Air";
                    spell.Radius = 350;
                    spell.Range = 2500;
                    spell.SpellID = 11768;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Spirit;
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
    public class AidonCopyEarth : GameNpc
    {
        public AidonCopyEarth() : base()
        {
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
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public static int CopyCountEarth = 0;
        public override void Die(GameObject killer)
        {
            --CopyCountEarth;
            base.Die(killer);
        }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 200; }
        public override bool AddToWorld()
        {
            Model = 61;
            MeleeDamageType = EDamageType.Crush;
            Name = "Illusion of Aidon the Archwizard";
            RespawnInterval = -1;
            Flags = ENpcFlags.GHOST;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            ++CopyCountEarth;

            Size = 55;
            Level = 75;
            MaxSpeedBase = 0;//copies not moves

            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = 6;
            Realm = ERealm.None;
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
                Body.CastSpell(Aidon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            base.Think();
        }
        private Spell m_Aidon_DD;
        private Spell Aidon_DD
        {
            get
            {
                if (m_Aidon_DD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 219;
                    spell.Icon = 219;
                    spell.TooltipId = 360;
                    spell.Damage = 300;
                    spell.Name = "Aidons's Earth";
                    spell.Radius = 350;
                    spell.Range = 2500;
                    spell.SpellID = 11769;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Matter;
                    m_Aidon_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aidon_DD);
                }
                return m_Aidon_DD;
            }
        }
    }
}
