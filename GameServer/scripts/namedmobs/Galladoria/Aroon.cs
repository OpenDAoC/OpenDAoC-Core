using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS
{
    public class Aroon : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Aroon()
            : base()
        {
        }
        public static bool Aroon_slash = false;
        public static bool Aroon_crush = false;
        public static bool Aroon_thrust = false;
        public static bool Aroon_body = false;
        public static bool Aroon_cold = false;
        public static bool Aroon_energy = false;
        public static bool Aroon_heat = false;
        public static bool Aroon_matter = false;
        public static bool Aroon_spirit = false;

        #region Aroon resist damage checks
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon_slash == false && Aroon_thrust == false && Aroon_crush == false && Aroon_body == false && Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                    && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == false && Aroon_crush == false && Aroon_body == false && Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                    && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == false && Aroon_body == false && Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                    && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == false && Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                     && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true && Aroon_cold == false && Aroon_energy == false && Aroon_heat == false
                    && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true && Aroon_cold == true && Aroon_energy == false && Aroon_heat == false
                    && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true && Aroon_cold == true && Aroon_energy == true && Aroon_heat == false
                     && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Heat || damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true && Aroon_cold == true && Aroon_energy == true && Aroon_heat == true
                    && Aroon_matter == false && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true && Aroon_cold == true && Aroon_energy == true && Aroon_heat == true
                     && Aroon_matter == true && Aroon_spirit == false)
                {
                    if (damageType == eDamageType.Spirit)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                    else
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                if (Aroon_slash == true && Aroon_thrust == true && Aroon_crush == true && Aroon_body == true && Aroon_cold == true && Aroon_energy == true && Aroon_heat == true
                    && Aroon_matter == true && Aroon_spirit == true)
                {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        #endregion Aroon resist damage checks

        public virtual int COifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
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
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158075);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;

            Aroon_slash = false;
            Aroon_thrust = false;
            Aroon_crush = false;
            Aroon_body = false;
            Aroon_cold = false;
            Aroon_energy = false;
            Aroon_heat = false;
            Aroon_matter = false;
            Aroon_spirit = false;

            CorpScaithBrain.switch_target = false;
            SpioradScaithBrain.switch_target = false;
            RopadhScaithBrain.switch_target = false;
            DamhnaScaithBrain.switch_target = false;
            FuinneamgScaithBrain.switch_target = false;
            BruScaithBrain.switch_target = false;
            FuarScaithBrain.switch_target = false;
            TaesScaithBrain.switch_target = false;
            ScorScaithBrain.switch_target = false;

            AroonBrain.spawn_guardians = false;
            return base.AddToWorld();
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Aroon the Urlamhai", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Aroon not found, creating it...");

                log.Warn("Initializing Aroon the Urlamhai...");
                Aroon CO = new Aroon();
                CO.Name = "Aroon the Urlamhai";
                CO.Model = 767;
                CO.Realm = 0;
                CO.Level = 81;
                CO.Size = 175;
                CO.CurrentRegionID = 191;//galladoria

                CO.Strength = 500;
                CO.Intelligence = 220;
                CO.Piety = 220;
                CO.Dexterity = 200;
                CO.Constitution = 200;
                CO.Quickness = 125;
                CO.BodyType = 5;
                CO.MeleeDamageType = eDamageType.Slash;
                CO.Faction = FactionMgr.GetFactionByID(96);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                CO.X = 51478;
                CO.Y = 43359;
                CO.Z = 10369;
                CO.MaxDistance = 2000;
                CO.TetherRange = 2500;
                CO.MaxSpeedBase = 250;
                CO.Heading = 11;

                AroonBrain ubrain = new AroonBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 600;
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Aroon the Urlamhai exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class AroonBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public AroonBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                Aroon.Aroon_slash = false;
                Aroon.Aroon_thrust = false;
                Aroon.Aroon_crush = false;
                Aroon.Aroon_body = false;
                Aroon.Aroon_cold = false;
                Aroon.Aroon_energy = false;
                Aroon.Aroon_heat = false;
                Aroon.Aroon_matter = false;
                Aroon.Aroon_spirit = false;

                CorpScaithBrain.switch_target = false;
                SpioradScaithBrain.switch_target = false;
                RopadhScaithBrain.switch_target = false;
                DamhnaScaithBrain.switch_target = false;
                FuinneamgScaithBrain.switch_target = false;
                BruScaithBrain.switch_target = false;
                FuarScaithBrain.switch_target = false;
                TaesScaithBrain.switch_target = false;
                ScorScaithBrain.switch_target = false;

                spawn_guardians = false;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is CorpScaithBrain || npc.Brain is SpioradScaithBrain || npc.Brain is RopadhScaithBrain || npc.Brain is DamhnaScaithBrain
                        || npc.Brain is FuinneamgScaithBrain || npc.Brain is BruScaithBrain || npc.Brain is FuarScaithBrain || npc.Brain is TaesScaithBrain
                        || npc.Brain is ScorScaithBrain)
                    {
                            npc.RemoveFromWorld();
                    }
                }
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 11);
                this.Body.Health = this.Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 11);
                this.Body.Health = this.Body.MaxHealth;
            }

            if(Body.InCombat && HasAggro)
            {
                if(spawn_guardians==false)
                {
                    SpawnGuardians();
                    spawn_guardians = true;
                }
                if(Util.Chance(10))
                {
                    Body.CastSpell(AroonRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }

            base.Think();
        }

        public static bool spawn_guardians = false;
        public void SpawnGuardians()
        {
            CorpScaith Add = new CorpScaith();
            Add.X = Body.X + Util.Random(-100, 150);
            Add.Y = Body.Y + Util.Random(-100, 150);
            Add.Z = Body.Z;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();

            SpioradScaith Add2 = new SpioradScaith();
            Add2.X = Body.X + Util.Random(-100, 150);
            Add2.Y = Body.Y + Util.Random(-100, 150);
            Add2.Z = Body.Z;
            Add2.CurrentRegion = Body.CurrentRegion;
            Add2.Heading = Body.Heading;
            Add2.AddToWorld();

            RopadhScaith Add3 = new RopadhScaith();
            Add3.X = Body.X + Util.Random(-100, 150);
            Add3.Y = Body.Y + Util.Random(-100, 150);
            Add3.Z = Body.Z;
            Add3.CurrentRegion = Body.CurrentRegion;
            Add3.Heading = Body.Heading;
            Add3.AddToWorld();

            DamhnaScaith Add4 = new DamhnaScaith();
            Add4.X = Body.X + Util.Random(-100, 150);
            Add4.Y = Body.Y + Util.Random(-100, 150);
            Add4.Z = Body.Z;
            Add4.CurrentRegion = Body.CurrentRegion;
            Add4.Heading = Body.Heading;
            Add4.AddToWorld();

            FuinneamgScaith Add5 = new FuinneamgScaith();
            Add5.X = Body.X + Util.Random(-100, 150);
            Add5.Y = Body.Y + Util.Random(-100, 150);
            Add5.Z = Body.Z;
            Add5.CurrentRegion = Body.CurrentRegion;
            Add5.Heading = Body.Heading;
            Add5.AddToWorld();

            BruScaith Add6 = new BruScaith();
            Add6.X = Body.X + Util.Random(-100, 150);
            Add6.Y = Body.Y + Util.Random(-100, 150);
            Add6.Z = Body.Z;
            Add6.CurrentRegion = Body.CurrentRegion;
            Add6.Heading = Body.Heading;
            Add6.AddToWorld();

            FuarScaith Add7 = new FuarScaith();
            Add7.X = Body.X + Util.Random(-100, 150);
            Add7.Y = Body.Y + Util.Random(-100, 150);
            Add7.Z = Body.Z;
            Add7.CurrentRegion = Body.CurrentRegion;
            Add7.Heading = Body.Heading;
            Add7.AddToWorld();

            TaesScaith Add8 = new TaesScaith();
            Add8.X = Body.X + Util.Random(-100, 150);
            Add8.Y = Body.Y + Util.Random(-100, 150);
            Add8.Z = Body.Z;
            Add8.CurrentRegion = Body.CurrentRegion;
            Add8.Heading = Body.Heading;
            Add8.AddToWorld();

            ScorScaith Add9 = new ScorScaith();
            Add9.X = Body.X + Util.Random(-100, 150);
            Add9.Y = Body.Y + Util.Random(-100, 150);
            Add9.Z = Body.Z;
            Add9.CurrentRegion = Body.CurrentRegion;
            Add9.Heading = Body.Heading;
            Add9.AddToWorld();
        }

        private Spell m_AroonRoot;
        private Spell AroonRoot
        {
            get
            {
                if (m_AroonRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 25;
                    spell.ClientEffect = 5208;
                    spell.Icon = 5208;
                    spell.TooltipId = 5208;
                    spell.Name = "Root";
                    spell.Value = 99;
                    spell.Duration = 60;
                    spell.Radius = 1200;
                    spell.Range = 1500;
                    spell.SpellID = 117230;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Matter;
                    m_AroonRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AroonRoot);
                }
                return m_AroonRoot;
            }
        }
    }
}
///////////////////////////////////////Guardians/////////////////////

#region Slash Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Slash guardian
/// </summary>
namespace DOL.GS
{
    public class CorpScaith : GameNPC
    {
        public CorpScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)//slash resist
        {
            Aroon.Aroon_slash = true;
            base.Die(null); // null to not gain experience
        }
        
        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Corp Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            CorpScaithBrain adds = new CorpScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class CorpScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public CorpScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            { 
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Thrust Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Thrust guardian
/// </summary>
namespace DOL.GS
{
    public class SpioradScaith : GameNPC//thrust resist
    {
        public SpioradScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_thrust = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Spiorad Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            SpioradScaithBrain adds = new SpioradScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class SpioradScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public SpioradScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Crush Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Crush guardian
/// </summary>
namespace DOL.GS
{
    public class RopadhScaith : GameNPC//crush resist
    {
        public RopadhScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_crush = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Ropadh Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            RopadhScaithBrain adds = new RopadhScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class RopadhScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public RopadhScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Body Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Body guardian
/// </summary>
namespace DOL.GS
{
    public class DamhnaScaith : GameNPC//Body resist
    {
        public DamhnaScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_body = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust == false && Aroon.Aroon_crush==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Damhna Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            DamhnaScaithBrain adds = new DamhnaScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class DamhnaScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public DamhnaScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Cold Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Cold guardian
/// </summary>
namespace DOL.GS
{
    public class FuinneamgScaith : GameNPC//Cold resist
    {
        public FuinneamgScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_cold = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust == false && Aroon.Aroon_crush == false && Aroon.Aroon_body==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Fuinneamg Scaith";
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            RespawnInterval = -1;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            FuinneamgScaithBrain adds = new FuinneamgScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class FuinneamgScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FuinneamgScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Energy Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Energy guardian
/// </summary>
namespace DOL.GS
{
    public class BruScaith : GameNPC//Energy resist
    {
        public BruScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_energy = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust == false && Aroon.Aroon_crush == false && Aroon.Aroon_body == false && Aroon.Aroon_cold==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Bru Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            BruScaithBrain adds = new BruScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class BruScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public BruScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000,20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Heat Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Heat guardian
/// </summary>
namespace DOL.GS
{
    public class FuarScaith : GameNPC//Heat resist
    {
        public FuarScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_heat = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust == false && Aroon.Aroon_crush == false && Aroon.Aroon_body == false && Aroon.Aroon_cold == false && Aroon.Aroon_energy==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Fuar Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            FuarScaithBrain adds = new FuarScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class FuarScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FuarScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Matter Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Matter guardian
/// </summary>
namespace DOL.GS
{
    public class TaesScaith : GameNPC//Matter resist
    {
        public TaesScaith() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_matter = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust == false && Aroon.Aroon_crush == false && Aroon.Aroon_body == false && Aroon.Aroon_cold == false && Aroon.Aroon_energy == false
                    && Aroon.Aroon_heat==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Taes Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            TaesScaithBrain adds = new TaesScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class TaesScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TaesScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Spirit Guardian
/// <summary>
/// ////////////////////////////////////////////////////////////////Spirit guardian
/// </summary>
namespace DOL.GS
{
    public class ScorScaith : GameNPC//Spirit resist
    {
        public ScorScaith() : base() { }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
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
            get { return 15000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            Aroon.Aroon_spirit = true;
            base.Die(null); // null to not gain experience
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Aroon.Aroon_slash == false && Aroon.Aroon_thrust == false && Aroon.Aroon_crush == false && Aroon.Aroon_body == false && Aroon.Aroon_cold == false && Aroon.Aroon_energy == false
                    && Aroon.Aroon_heat == false && Aroon.Aroon_matter==false)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to this damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

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

        public override bool AddToWorld()
        {
            Model = (ushort)Util.Random(889, 890);
            Name = "Scor Scaith";
            RespawnInterval = -1;
            Strength = 350;
            Dexterity = 200;
            Quickness = 125;
            MaxDistance = 2500;
            TetherRange = 3000;
            Size = 155;
            Level = 77;
            MaxSpeedBase = 220;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            ScorScaithBrain adds = new ScorScaithBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class ScorScaithBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ScorScaithBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 5000;
        }
        public static bool switch_target = false;
        private GamePlayer randomtarget = null;
        private GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public int RandomAttackTarget(RegionTimer timer)
        {
            //IList enemies = new ArrayList(m_aggroTable.Keys);
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)];
                m_aggroTable.Clear();
                m_aggroTable.Add(RandomTarget, 500);
                switch_target = false;
            }

            return 0;
        }

        public override void Think()
        {
            if (Body.InCombat)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!PlayersToAttack.Contains(player))
                            {
                                PlayersToAttack.Add(player);
                            }
                        }
                    }
                }
                if (Util.Chance(15))
                {
                    if (switch_target == false)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(RandomAttackTarget), Util.Random(10000, 20000));
                        switch_target = true;
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion