using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using System.Collections;

namespace DOL.GS
{
    public class Issorden : GameEpicBoss
    {
        public Issorden() : base()
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
            get { return 100000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162545);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            IssordenBrain.BafMobs = false;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

            IssordenBrain sbrain = new IssordenBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class IssordenBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public IssordenBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        public static bool IsTargetPicked = false;
        public static GamePlayer randomtarget = null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public static bool BafMobs = false;
        private bool PrepareBolt = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                BafMobs = false;
                PrepareBolt = false;
            }

            if (HasAggro && Body.TargetObject != null)
            {
                if (Util.Chance(10) && !Body.IsCasting && RandomTarget == null)
                   Body.CastSpell(IssoRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "IssordenBaf")
                            {
                                AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
                                BafMobs = true;
                            }
                        }
                    }
                }
                if(!PrepareBolt)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickPlayer), Util.Random(25000,35000));
                    PrepareBolt = true;
                }
            }
            base.Think();
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        #region Random Bolt Player
        public List<GameLiving> damage_enemies = new List<GameLiving>();
        public int PickPlayer(ECSGameTimer timer)
        {
            if (HasAggro)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!damage_enemies.Contains(player))
                                damage_enemies.Add(player);
                        }
                    }
                }
                if (damage_enemies.Count > 0)
                {
                    GamePlayer Target = (GamePlayer)damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                    RandomTarget = Target; //randomly picked target is now RandomTarget
                    BroadcastMessage(Body.Name + " turns his frosty stare on " + RandomTarget.Name + "! " + Body.Name + "'s hands begin to glow while a blue mist crawls from small cracks in the ice at his feet.");
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastBolt), 2000);
                }           
            }
            return 0;
        }
        private int CastBolt(ECSGameTimer timer)
        {
            if (HasAggro && RandomTarget != null && RandomTarget.IsAlive)
            {
                Body.TargetObject = RandomTarget; //set target to randomly picked
                Body.TurnTo(RandomTarget);
                Body.CastSpell(Isso_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false); //bolt        
                if(!Body.IsCasting)
                    Body.CastSpell(Isso_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false); //bolt    
            }
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetBolt), 3000);
            return 0;
        }
        private int ResetBolt(ECSGameTimer timer)
        {
            PrepareBolt = false;
            RandomTarget = null; //reset random target to null
            return 0;
        }
        #endregion
        #region Spells
        private Spell m_IssoRoot;
        private Spell IssoRoot
        {
            get
            {
                if (m_IssoRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 277;
                    spell.Icon = 277;
                    spell.Duration = 60;
                    spell.Radius = 1500;
                    spell.Value = 99;
                    spell.Name = "Issorden Root";
                    spell.TooltipId = 277;
                    spell.SpellID = 11741;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Cold;
                    m_IssoRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IssoRoot);
                }
                return m_IssoRoot;
            }
        }
        private Spell m_Isso_Bolt;
        private Spell Isso_Bolt
        {
            get
            {
                if (m_Isso_Bolt == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 2;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 4559;
                    spell.Icon = 4559;
                    spell.TooltipId = 4559;
                    spell.Damage = 400;
                    spell.Name = "Frost Sphere";
                    spell.Range = 1800;
                    spell.SpellID = 11921;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.Bolt.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_Isso_Bolt = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Isso_Bolt);
                }
                return m_Isso_Bolt;
            }
        }
        #endregion
    }
}