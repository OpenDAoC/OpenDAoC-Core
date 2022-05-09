using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class Suttung : GameEpicBoss
    {
        public Suttung() : base()
        {
        }

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 80; // dmg reduction for melee dmg
                case eDamageType.Crush: return 80; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 80; // dmg reduction for melee dmg
                default: return 60; // dmg reduction for rest resists
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
            if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160395);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Giant;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 573, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.Standard);
            SuttungBrain.message1 = false;

            VisibleActiveWeaponSlots = 16;
            SuttungBrain sbrain = new SuttungBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Elder Icelord Suttung", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Elder Icelord Suttung not found, creating it...");

                log.Warn("Initializing Elder Icelord Suttung ...");
                Suttung TG = new Suttung();
                TG.Name = "Elder Icelord Suttung";
                TG.Model = 918;
                TG.Realm = 0;
                TG.Level = 81;
                TG.Size = 65;
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval =
                    ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                    60000; //1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
                TG.BodyType = (ushort)NpcTemplateMgr.eBodyType.Giant;

                TG.X = 32090;
                TG.Y = 54204;
                TG.Z = 11884;
                TG.Heading = 2056;
                SuttungBrain ubrain = new SuttungBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn(
                    "Elder Icelord Suttung exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class SuttungBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SuttungBrain()
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

        public static bool IsBerserker = false;

        public int BerserkerPhase(ECSGameTimer timer)
        {
            if (Body.IsAlive && IsBerserker == true && Body.InCombat && HasAggro)
            {
                BroadcastMessage(String.Format(Body.Name + " goes into berserker stance!"));
                Body.Emote(eEmote.MidgardFrenzy);
                Body.Empathy = 340;
                Body.MaxSpeedBase = 200; //slow under zerk mode
                Body.Size = 75;
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(EndBerserkerPhase),
                    Util.Random(10000, 20000)); //10-20s in berserk stance
            }

            return 0;
        }

        public int EndBerserkerPhase(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                BroadcastMessage(String.Format(Body.Name + " berserker stance fades away!"));
                Body.Empathy = Body.NPCTemplate.Empathy;
                Body.Size = Convert.ToByte(Body.NPCTemplate.Size);
                Body.MaxSpeedBase = Body.NPCTemplate.MaxSpeed;
                IsBerserker = false;
            }

            return 0;
        }

        public static bool message1 = false;

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                message1 = false;
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (Body.InCombat && HasAggro)
            {
                if (message1 == false)
                {
                    BroadcastMessage(String.Format(
                        Body.Name + " says, 'The price of your invading our frozen fortress is death!" +
                        " Death to you and your allies! Your presence here mocks the pacifist philosophy of my opponents on the Council." +
                        " I weep for no council member who has perished!'"));
                    message1 = true;
                }

                if (IsBerserker == false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(BerserkerPhase), Util.Random(20000, 35000));
                    IsBerserker = true;
                }
            }
            if(HasAggro && Body.TargetObject != null)
            {
                if (Util.Chance(55))
                    Body.CastSpell(IcelordHjalmar_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
            base.Think();
        }
        private Spell m_IcelordHjalmar_aoe;
        private Spell IcelordHjalmar_aoe
        {
            get
            {
                if (m_IcelordHjalmar_aoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 15;
                    spell.ClientEffect = 208;
                    spell.Icon = 208;
                    spell.TooltipId = 208;
                    spell.Damage = 450;
                    spell.Name = "Hjalmar's Ice Blast";
                    spell.Range = 0;
                    spell.Radius = 440;
                    spell.SpellID = 11901;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_IcelordHjalmar_aoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IcelordHjalmar_aoe);
                }
                return m_IcelordHjalmar_aoe;
            }
        }
    }
}

///////////////////////////////////////////////Elder Icelord Hjalmar/////////////////////////////////////////
namespace DOL.GS
{
    public class Hjalmar : GameEpicBoss
    {
        public Hjalmar() : base()
        {
        }

        public static int TauntID = 292;
        public static int TauntClassID = 44;
        public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int BackStyleID = 304;
        public static int BackStyleClassID = 44;
        public static Style back_style = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 80; // dmg reduction for melee dmg
                case eDamageType.Crush: return 80; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 80; // dmg reduction for melee dmg
                default: return 60; // dmg reduction for rest resists
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
            if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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

        public override void Die(GameObject killer) //on kill generate orbs
        {
            base.Die(killer);
        }

        public override void OnAttackEnemy(AttackData ad)
        {
            if (ad.AttackResult == eAttackResult.HitStyle)
            {
                if (Util.Chance(25))
                {
                    SpawnAdds();
                }
            }
            base.OnAttackEnemy(ad);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160394);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Giant;
            Styles.Add(taunt);
            Styles.Add(back_style);

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 572, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            HjalmarBrain.message2 = false;

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Slash;
            HjalmarBrain sbrain = new HjalmarBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Elder Icelord Hjalmar", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Elder Icelord Hjalmar not found, creating it...");

                log.Warn("Initializing Elder Icelord Hjalmar ...");
                Hjalmar TG = new Hjalmar();
                TG.Name = "Elder Icelord Hjalmar";
                TG.Model = 918;
                TG.Realm = 0;
                TG.Level = 81;
                TG.Size = 65;
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
                TG.BodyType = (ushort)NpcTemplateMgr.eBodyType.Giant;

                TG.X = 32073;
                TG.Y = 53569;
                TG.Z = 11886;
                TG.Heading = 33;
                HjalmarBrain ubrain = new HjalmarBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn(
                    "Elder Icelord Hjalmar exist ingame, remove it and restart server if you want to add by script code.");
        }

        public void SpawnAdds()
        {
            Morkimma npc = new Morkimma();
            npc.X = TargetObject.X + Util.Random(-100, 100);
            npc.Y = TargetObject.Y + Util.Random(-100, 100);
            npc.Z = TargetObject.Z;
            npc.RespawnInterval = -1;
            npc.Heading = Heading;
            npc.CurrentRegion = CurrentRegion;
            npc.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class HjalmarBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HjalmarBrain()
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

        public static bool message2 = false;

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                message2 = false;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4500))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is MorkimmaBrain)
                            {
                                npc.Die(Body);
                            }
                        }
                    }
                }
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (Body.InCombat && HasAggro)
            {
                if (message2 == false)
                {
                    BroadcastMessage(String.Format(
                        Body.Name +
                        " says, I have warned the Council that if we do not destroy those who threaten us before they destroy us, we will perish." +
                        " You deserve this fate more than I do. I will not mourn her death beyond the grave!"));
                    message2 = true;
                }

                if (Body.TargetObject != null)
                {
                    float angle = Body.TargetObject.GetAngle(Body);
                    GameLiving living = Body.TargetObject as GameLiving;
                    if (Util.Chance(100))
                    {
                        if (angle >= 150 && angle < 210)
                        {
                            Body.Empathy = 240;
                            Body.styleComponent.NextCombatStyle = Hjalmar.back_style;
                        }
                        else
                        {
                            Body.Empathy = 200;
                            Body.styleComponent.NextCombatStyle = Hjalmar.taunt;
                        }
                    }
                }
            }

            base.Think();
        }
    }
}

///////////////////////////////////////////////////////////Hjalmar adds/////////////////////////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class Morkimma : GameNPC
    {
        public Morkimma() : base()
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
            return base.AttackDamage(weapon) * Strength / 50;
        }

        protected int Show_Effect(ECSGameTimer timer)
        {
            if (this.IsAlive)
            {
                foreach (GamePlayer player in this.GetPlayersInRadius(8000))
                {
                    if (player != null)
                    {
                        player.Out.SendSpellEffectAnimation(this, this, 4323, 0, false, 0x01);
                    }
                }

                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
            }

            return 0;
        }

        protected int DoCast(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
            }

            return 0;
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
            get { return 2000; }
        }

        public override bool AddToWorld()
        {
            Model = 665;
            Size = 50;
            Strength = 100;
            Quickness = 100;
            Dexterity = 180;
            Constitution = 100;
            MaxSpeedBase = 220;
            Name = "Morkimma";
            Level = (byte)Util.Random(50, 55);

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            Realm = eRealm.None;
            RespawnInterval = -1;

            MorkimmaBrain adds = new MorkimmaBrain();
            SetOwnBrain(adds);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }

            return success;
        }
    }
}

namespace DOL.AI.Brain
{
    public class MorkimmaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MorkimmaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
            ThinkInterval = 1500;
        }

        public override void Think()
        {
            base.Think();
        }
    }
}
