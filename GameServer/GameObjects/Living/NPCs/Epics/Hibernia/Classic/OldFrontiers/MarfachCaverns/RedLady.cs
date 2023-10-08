using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class RedLady : GameEpicBoss
    {
        public RedLady() : base()
        {
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Red Lady initialized..");
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
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 20; // dmg reduction for melee dmg
                case EDamageType.Crush: return 20; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
                default: return 30; // dmg reduction for rest resists
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
            get { return 30000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8819);
            LoadTemplate(npcTemplate);
            
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            SpecialInnocent.InnocentCount = 0;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 67, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 380, 67, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 379, 67);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 381, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 382, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 443, 67, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 468, 67, 94);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            RedLadyBrain redladybrain = new RedLadyBrain();
            SetOwnBrain(redladybrain);
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            base.Die(killer);

            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
            {
                if (npc.Brain is SpecialInnocentBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
        }
    }  
}

namespace DOL.AI.Brain
{
    class RedLadyBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public RedLadyBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
        }
        private bool CanSpawnAdds = false;
        private int SpawnAdd(EcsGameTimer timer)
        {
            for (int i = 0; i < 8; i++)
            {
                if (SpecialInnocent.InnocentCount < 9)
                {
                    SpecialInnocent add = new SpecialInnocent();
                    add.X = Body.X + Util.Random(-100, 100);
                    add.Y = Body.Y + Util.Random(-100, 100);
                    add.Z = Body.Z;
                    add.CurrentRegionID = 276;
                    add.RespawnInterval = -1;
                    add.Heading = Body.Heading;
                    add.AddToWorld();
                }
            }
            CanSpawnAdds = false;
            return 0;
        }
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                SpecialInnocent.InnocentCount = 0;
                CanSpawnAdds = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc.Brain is SpecialInnocentBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.InCombat && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if(SpecialInnocent.InnocentCount<9 && CanSpawnAdds == false)
                {
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdd), Util.Random(20000, 30000));
                    CanSpawnAdds=true;
                }
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc.Brain is SpecialInnocentBrain)
                    {
                        AddAggroListTo(npc.Brain as SpecialInnocentBrain);
                    }
                }
                Body.CastSpell(RedLady_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.Think();
        }
        public Spell m_RedLady_DD;
        public Spell RedLady_DD
        {
            get
            {
                if (m_RedLady_DD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = Util.Random(25, 35);
                    spell.ClientEffect = 4445;
                    spell.Icon = 4445;
                    spell.TooltipId = 4445;
                    spell.Damage = 100;
                    spell.Duration = 30;
                    spell.Frequency = 30;
                    spell.Name = "Soul Drain";
                    spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.Radius = 350;
                    spell.Range = 1500;
                    spell.SpellID = 11790;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.DamageOverTime.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Matter;
                    m_RedLady_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RedLady_DD);
                }
                return m_RedLady_DD;
            }
        }
    }
}
/// <summary>
/// ////////////////////////////////////////////////////////////////Innocents//////////////////////////////////////////////
/// </summary>
namespace DOL.GS
{
    public class SpecialInnocent : GameNPC
    {
        public SpecialInnocent() : base()
        {
        }
        public override void OnAttackEnemy(AttackData ad)
        {
            if (Util.Chance(5))
            {
                if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
                {
                    CastSpell(Innocent_Disease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            base.OnAttackEnemy(ad);
        }
        public static int InnocentCount = 0;
        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(442, 446);
            Size = 50;
            Level = (byte)Util.Random(34, 38);
            Name = "summoned innocent";
            Realm = ERealm.None;
            MaxDistance = 0;
            TetherRange = 0;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            ++InnocentCount;
            Strength = 50;
            Dexterity = 120;
            Constitution = 100;
            Quickness = 98;
            SpecialInnocentBrain innocentbrain = new SpecialInnocentBrain();
            SetOwnBrain(innocentbrain);
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            --InnocentCount;
            base.Die(killer);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override long ExperienceValue => 0;
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash:
                case EDamageType.Crush:
                case EDamageType.Thrust: return 25;
                default: return 15;
            }
        }
        public override int MaxHealth
        {
            get { return 1000; }
        }
        public Spell m_Innocent_Disease;
        public Spell Innocent_Disease
        {
            get
            {
                if (m_Innocent_Disease == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 4375;
                    spell.Icon = 4375;
                    spell.TooltipId = 4375;
                    spell.Duration = 120;
                    spell.Name = "Disease";
                    spell.Radius = 100;
                    spell.Range = 1500;
                    spell.SpellID = 11789;
                    spell.Target = ESpellTarget.ENEMY.ToString();
                    spell.Type = ESpellType.Disease.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Matter;
                    m_Innocent_Disease = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Innocent_Disease);
                }
                return m_Innocent_Disease;
            }
        }
    }
}
namespace DOL.AI.Brain
{
    public class SpecialInnocentBrain : StandardMobBrain
    {
        public SpecialInnocentBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 700;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}
