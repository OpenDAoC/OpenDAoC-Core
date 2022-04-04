using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class BlackLady : GameEpicBoss
    {
        public BlackLady() : base()
        {
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Black Lady initialized..");
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

        public override int MaxHealth
        {
            get { return 20000; }
        }

        public override int AttackRange
        {
            get { return 450; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
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

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 70;
                case eDamageType.Crush: return 70;
                case eDamageType.Thrust: return 70; 
                default: return 55; 
            }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8817);
            LoadTemplate(npcTemplate);

            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Gender = eGender.Female;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 43, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 380, 43, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 379, 43);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 381, 43, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 382, 43, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 443, 43, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 468, 43, 91);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            // humanoid
            IsOakUp = false;
            if (IsOakUp == false)
            {
                SpawnOak();
                IsOakUp = true;
            }
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            BodyType = 6;
            IsCloakHoodUp = true;
            Ogress.OgressCount = 0;
            BlackLadyBrain blackladybrain = new BlackLadyBrain();
            SetOwnBrain(blackladybrain);
            base.AddToWorld();
            return true;
        }
        public static bool IsOakUp = false;
        public void SpawnOak()
        {
                AncientBlackOak Add1 = new AncientBlackOak();
                Add1.X = 30091;
                Add1.Y = 37620;
                Add1.Z = 15049;
                Add1.CurrentRegionID = 276;
                Add1.RespawnInterval = -1;
                Add1.Heading = 2053;
                Add1.AddToWorld();
        }
        public override void Die(GameObject killer)
        {
            base.Die(killer);
            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(this.CurrentRegionID))
            {
                if (npc.Brain is OgressBrain)
                {
                    npc.RemoveFromWorld();
                    Ogress.OgressCount = 0;
                }
            }
            foreach (GameNPC npc2 in WorldMgr.GetNPCsFromRegion(this.CurrentRegionID))
            {
                if (npc2.Brain is AncientBlackOakBrain)
                {
                    npc2.Die(npc2);
                }
            }
        }
    }
}
namespace DOL.AI.Brain
{
    class BlackLadyBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public BlackLadyBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            if(!HasAggressionTable())
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is OgressBrain)
                        {
                            npc.RemoveFromWorld();
                            Ogress.OgressCount = 0;
                        }
                    }
                }
            }
            if(Body.InCombat && HasAggro)
            {
                Body.CastSpell(BlackLady_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

                if(Util.Chance(20))
                {
                    SpawnOgress();
                }
            }
            base.Think();
        }
        public void SpawnOgress()
        {
            if (Ogress.OgressCount < 5)
            {
                switch (Util.Random(1, 2))
                {
                    case 1:
                        {
                            Ogress Add1 = new Ogress();
                            Add1.X = 29654;
                            Add1.Y = 37219;
                            Add1.Z = 14953;
                            Add1.CurrentRegionID = 276;
                            Add1.RespawnInterval = -1;
                            Add1.Heading = 3618;
                            Add1.AddToWorld();
                        }
                        break;
                    case 2:
                        {
                            Ogress Add2 = new Ogress();
                            Add2.X = 29646;
                            Add2.Y = 38028;
                            Add2.Z = 14967;
                            Add2.CurrentRegionID = 276;
                            Add2.RespawnInterval = -1;
                            Add2.Heading = 2114;
                            Add2.AddToWorld();
                        }
                        break;
                }
            }
        }
        public Spell m_BlackLady_DD;
        public Spell BlackLady_DD
        {
            get
            {
                if (m_BlackLady_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3.5;
                    spell.RecastDelay = Util.Random(10,20);
                    spell.ClientEffect = 4568;
                    spell.Icon = 4568;
                    spell.TooltipId = 4568;
                    spell.Damage = 400;
                    spell.Name = "Void Strike";
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11787;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_BlackLady_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BlackLady_DD);
                }

                return m_BlackLady_DD;
            }
        }
    }
}

/// <summary>
/// //////////////////////////////////////////////////////////////////////Ogress/////////////////////////////////////////////
/// </summary>
namespace DOL.GS
{ 
    public class Ogress : GameNPC
    {
        public Ogress() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash:
                case eDamageType.Crush:
                case eDamageType.Thrust: return 35;
                default: return 25;
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 700;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.45;
        }
        public override int MaxHealth
        {
            get { return 2000; }
        }
        public static int OgressCount = 0;
        public override void Die(GameObject killer)
        {
            --OgressCount;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            Model = 402;
            Level = (byte)Util.Random(50, 55);
            Name = "Ogress";
            Size = (byte)Util.Random(40, 50);
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            RespawnInterval = -1;
            MaxSpeedBase = 200;
            Realm = eRealm.None;
            MaxDistance = 0;
            TetherRange = 0;

            ++OgressCount;
            Strength = 50;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            OgressBrain ogressbrain = new OgressBrain();
            SetOwnBrain(ogressbrain);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    class OgressBrain : StandardMobBrain
    {
        public OgressBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1200;
        }

        public override void Think()
        {
            foreach(GamePlayer player in Body.GetPlayersInRadius(2000))
            {
                if(player != null)
                {
                    if(player.IsAlive && player.IsVisibleTo(Body) && player.Client.Account.PrivLevel == 1 && (player.CharacterClass.ID == 6 || player.CharacterClass.ID == 10 || player.CharacterClass.ID == 48
                    || player.CharacterClass.ID == 46 || player.CharacterClass.ID == 47 || player.CharacterClass.ID == 42 || player.CharacterClass.ID == 28 || player.CharacterClass.ID == 26))
                    {
                        if(!AggroTable.ContainsKey(player))
                        {
                            AggroTable.Add(player, 150);
                            Body.StartAttack(player);
                        }
                    }
                    else
                    {
                        if (!AggroTable.ContainsKey(player))
                        {
                            AggroTable.Add(player, 10);
                            Body.StartAttack(player);
                        }
                    }
                }
            }
            base.Think();
        }
    }
}
