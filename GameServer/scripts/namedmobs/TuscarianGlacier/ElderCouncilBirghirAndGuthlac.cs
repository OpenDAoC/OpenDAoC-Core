using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Birghir : GameEpicBoss
    {
        public Birghir() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 200000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160391);
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

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            BirghirBrain.IsTargetPicked = false;
            BirghirBrain.message1 = false;
            BirghirBrain.IsPulled = false;

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            BirghirBrain sbrain = new BirghirBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Elder Council Birghir", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Elder Council Birghir not found, creating it...");

                log.Warn("Initializing Elder Council Birghir ...");
                Birghir TG = new Birghir();
                TG.Name = "Elder Council Birghir";
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

                TG.X = 34595;
                TG.Y = 55711;
                TG.Z = 11884;
                TG.Heading = 1039;
                BirghirBrain ubrain = new BirghirBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn(
                    "Elder Council Birghir exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class BirghirBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BirghirBrain()
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
        public static GamePlayer randomtarget = null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public int PickPlayer(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                IList enemies = new ArrayList(m_aggroTable.Keys);
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!m_aggroTable.ContainsKey(player))
                                m_aggroTable.Add(player, 1);
                        }
                    }
                }
                if (enemies.Count == 0)
                {/*do nothing*/}
                else
                {
                    List<GameLiving> damage_enemies = new List<GameLiving>();
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (enemies[i] == null)
                            continue;
                        if (!(enemies[i] is GameLiving))
                            continue;
                        if (!(enemies[i] as GameLiving).IsAlive)
                            continue;
                        GameLiving living = null;
                        living = enemies[i] as GameLiving;
                        if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
                        {
                            damage_enemies.Add(enemies[i] as GameLiving);
                        }
                    }
                    if (damage_enemies.Count > 0)
                    {
                        GamePlayer Target = (GamePlayer)damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                        RandomTarget = Target; //randomly picked target is now RandomTarget
                        if (RandomTarget.IsVisibleTo(Body) && Body.TargetInView && RandomTarget != null && RandomTarget.IsAlive)
                        {
                            GamePlayer oldTarget = (GamePlayer)Body.TargetObject; //old target
                            Body.TargetObject = RandomTarget; //set target to randomly picked
                            Body.TurnTo(RandomTarget);
                            switch (Util.Random(1, 2)) //pick one of 2 spells to cast
                            {
                                case 1:
                                    {
                                        Body.CastSpell(Icelord_Bolt,SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); //bolt
                                    }
                                    break;
                                case 2:
                                    {
                                        Body.CastSpell(Icelord_dd,SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); //dd cold
                                    }
                                    break;
                            }

                            RandomTarget = null; //reset random target to null
                            if (oldTarget != null) Body.TargetObject = oldTarget; //return to old target
                            Body.StartAttack(oldTarget); //start attack old target
                            IsTargetPicked = false;
                        }
                    }
                }
            }
            return 0;
        }
        public static bool IsTargetPicked = false;
        public static bool message1 = false;
        public static bool IsPulled = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is GuthlacBrain)
                        {
                            AddAggroListTo(npc.Brain as GuthlacBrain);
                            IsPulled = true;
                        }
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                message1 = false;
                IsTargetPicked = false;
                IsPulled = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
                Body.Health = Body.MaxHealth;

            if (Body.InCombat && HasAggro)
            {
                if (message1 == false)
                {
                    BroadcastMessage(String.Format(
                        Body.Name + " says, 'The Ice Lords were right... They were right..." +
                        " We should have destroyed you all, all your people, before you could invade us! We should have... killed you all!'"));
                    message1 = true;
                }
                if (IsTargetPicked == false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickPlayer), Util.Random(15000, 20000));
                    IsTargetPicked = true;
                }
            }
            base.Think();
        }
        private Spell m_Icelord_Bolt;
        private Spell Icelord_Bolt
        {
            get
            {
                if (m_Icelord_Bolt == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 4559;
                    spell.Icon = 4559;
                    spell.TooltipId = 4559;
                    spell.Damage = 400;
                    spell.Name = "Frost Sphere";
                    spell.Range = 1800;
                    spell.SpellID = 11749;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.Bolt.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_Icelord_Bolt = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_Bolt);
                }
                return m_Icelord_Bolt;
            }
        }
        private Spell m_Icelord_dd;
        private Spell Icelord_dd
        {
            get
            {
                if (m_Icelord_dd == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 161;
                    spell.Icon = 161;
                    spell.TooltipId = 161;
                    spell.Damage = 650;
                    spell.Name = "Frost Strike";
                    spell.Range = 1800;
                    spell.SpellID = 11750;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_Icelord_dd = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_dd);
                }
                return m_Icelord_dd;
            }
        }
    }
}

//////////////////////////////////////////////////////////////////Elder Council Guthlac//////////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class Guthlac : GameEpicBoss
    {
        public Guthlac() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 200000; }
        }
        public override void Die(GameObject killer) //on kill generate orbs
        {
            foreach (GameNPC npc in this.GetNPCsInRadius(5000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is FrozenBombBrain)
                        npc.Die(this);
                }
            }
            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160392);
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

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            GuthlacBrain.message1 = false;
            GuthlacBrain.IsBombUp = false;
            GuthlacBrain.RandomTarget = null;
            GuthlacBrain.IsPulled2 = false;

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            GuthlacBrain sbrain = new GuthlacBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Elder Council Guthlac", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Elder Council Guthlac not found, creating it...");

                log.Warn("Initializing Elder Council Guthlac ...");
                Guthlac TG = new Guthlac();
                TG.Name = "Elder Council Guthlac";
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

                TG.X = 34595;
                TG.Y = 55711;
                TG.Z = 11884;
                TG.Heading = 1039;
                GuthlacBrain ubrain = new GuthlacBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Elder Council Guthlac exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class GuthlacBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public GuthlacBrain()
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
        public static bool message1 = false;
        public static bool IsBombUp = false;
        public static GameLiving randomtarget = null;
        public static GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        List<GamePlayer> PlayersToDD = new List<GamePlayer>();
        public static bool IsPulled2 = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled2 == false)
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is BirghirBrain)
                        {
                            AddAggroListTo(npc.Brain as BirghirBrain);
                            IsPulled2 = true;
                        }
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled2 = false;
                RandomTarget = null;
                FrozenBomb.FrozenBombCount = 0;
                message1 = false;
                IsBombUp = false;
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is FrozenBombBrain)
                            npc.Die(Body);
                    }
                }
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
                Body.Health = Body.MaxHealth;

            if (Body.InCombat && HasAggro)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(4500))
                {
                    if (player == null) break;
                    if (player.IsAlive)
                    {
                        if (!PlayersToDD.Contains(player))
                            PlayersToDD.Add(player);
                    }
                }
                if (IsBombUp == false && FrozenBomb.FrozenBombCount == 0)
                {
                    if (PlayersToDD.Count > 0)
                    {
                        GamePlayer ptarget = PlayersToDD[Util.Random(0, PlayersToDD.Count - 1)];
                        RandomTarget = ptarget;
                    }
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnBombTimer), Util.Random(35000, 60000)); //spawn frozen bomb every 35s-60s
                    IsBombUp = true;
                }
                if (message1 == false)
                {
                    BroadcastMessage(String.Format(
                        Body.Name +
                        " says, 'I didn't think it was possible that our home could fall victim to an invasion!" +
                        " The Ice Lords were right! We should have wiped out all dangerous creatures on this island! And we're going to do that today!'"));
                    message1 = true;
                }
            }
            base.Think();
        }

        public int SpawnBombTimer(ECSGameTimer timer)
        {
            if (FrozenBomb.FrozenBombCount == 0)
                SpawnFrozenBomb();

            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetBomb), 5000);
            return 0;
        }
        public int ResetBomb(ECSGameTimer timer)
        {
            RandomTarget = null;
            IsBombUp = false;
            return 0;
        }
        public void SpawnFrozenBomb()
        {
            FrozenBomb npc = new FrozenBomb();
            npc.Name = "Ice Spike";
            if (RandomTarget != null)
            {
                npc.X = RandomTarget.X;
                npc.Y = RandomTarget.Y;
                npc.Z = RandomTarget.Z;
                BroadcastMessage(String.Format(npc.Name + " appears on " + RandomTarget.Name +", It's unstable form will soon errupt."));
            }
            else
            {
                npc.X = Body.X;
                npc.Y = Body.Y;
                npc.Z = Body.Z;
                BroadcastMessage(String.Format(npc.Name + " appears nearby, It's unstable form will soon errupt."));
            }

            npc.RespawnInterval = -1;
            npc.Heading = Body.Heading;
            npc.CurrentRegion = Body.CurrentRegion;
            npc.AddToWorld();
        }
    }
}

/////////////////////////////////////////////////////////////Guthalc deadly ice spike/////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class FrozenBomb : GameNPC
    {
        public FrozenBomb() : base()
        {
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 15; // dmg reduction for melee dmg
                case eDamageType.Crush: return 15; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 15; // dmg reduction for melee dmg
                case eDamageType.Cold: return 99; // almost immune to cold dmg
                default: return 15; // dmg reduction for rest resists
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                foreach (GamePlayer player in GetPlayersInRadius(8000))
                {
                    if (player != null)
                        player.Out.SendSpellEffectAnimation(this, this, 177, 0, false, 0x01);
                }
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
            }
            return 0;
        }
        protected int DoCast(ECSGameTimer timer)
        {
            if (IsAlive)
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
            return 0;
        }
        protected int Explode(ECSGameTimer timer)
        {
            if (IsAlive && TargetObject != null)
            {
                //SetGroundTarget(X, Y, Z);
                CastSpell(GuthlacIceSpike_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(KillBomb), 1000);
            }
            return 0;
        }
        public int KillBomb(ECSGameTimer timer)
        {
            if (IsAlive)
                Die(this);
            return 0;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public static int FrozenBombCount = 0;
        public override void Die(GameObject killer)
        {
            FrozenBombCount = 0;
            base.Die(null);
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override bool AddToWorld()
        {
            Model = 665;
            Size = 100;
            MaxSpeedBase = 0;
            FrozenBombCount = 1;
            Name = "Ice Spike";
            Level = (byte)Util.Random(62, 66);

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            Realm = eRealm.None;
            RespawnInterval = -1;

            FrozenBombBrain adds = new FrozenBombBrain();
            SetOwnBrain(adds);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Explode), 25000); //25 seconds until this will explode and deal heavy cold dmg
            }
            return success;
        }

        private Spell m_GuthlacIceSpike_aoe;
        private Spell GuthlacIceSpike_aoe
        {
            get
            {
                if (m_GuthlacIceSpike_aoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 20;
                    spell.ClientEffect = 208;
                    spell.Icon = 208;
                    spell.TooltipId = 208;
                    spell.Damage = 2500;
                    spell.Name = "Ice Bomb";
                    spell.Radius = 3000; //very big radius to make them feel pain lol
                    spell.Range = 0;
                    spell.SpellID = 11751;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_GuthlacIceSpike_aoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GuthlacIceSpike_aoe);
                }
                return m_GuthlacIceSpike_aoe;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class FrozenBombBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public FrozenBombBrain()
            : base()
        {
            AggroLevel = 0;
            AggroRange = 0;
            ThinkInterval = 1500;
        }
        public override void Think()
        {
            if (Body.IsAlive)
            {
                //FSM.SetCurrentState(eFSMStateType.AGGRO);
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!AggroTable.ContainsKey(player))
                                AggroTable.Add(player, 100);
                        }
                    }
                }
            }
            base.Think();
        }
    }
}
