using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;


namespace DOL.GS
{
    public class Kvasir : GameEpicBoss
    {
        public Kvasir() : base()
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

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (damageType == eDamageType.Cold) //take no damage
                {
                    this.Health += this.MaxHealth / 5; //heal himself if damage is cold
                    BroadcastMessage(String.Format("Icelord Kvasir says, 'aahhhh thank you " + source.Name +" for healing me !'"));
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else //take dmg
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
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override bool AddToWorld()
        {
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc.Brain is TunnelsBrain)
                    npc.RemoveFromWorld();
            }
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162348);
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

            KvasirBrain sbrain = new KvasirBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            SpawnAnnouncer();
            if (killer is GamePlayer)
            {
                GamePlayer player = killer as GamePlayer;
                if(player != null)
                    BroadcastMessage(String.Format("my kind will avenge me! You won't make out of here alive " + player.CharacterClass.Name+ "!"));
            }
            var prepareMezz = TempProperties.getProperty<ECSGameTimer>("kvasir_prepareMezz");//cancel message
            if (prepareMezz != null)
            {
                prepareMezz.Stop();
                TempProperties.removeProperty("kvasir_prepareMezz");
            }
            base.Die(killer);
        }
        private void SpawnAnnouncer()
        {
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc.Brain is TunnelsBrain)
                    return;
            }
            Tunnels announcer = new Tunnels();
            announcer.X = 21088;
            announcer.Y = 52022;
            announcer.Z = 10880;
            announcer.Heading = 1006;
            announcer.CurrentRegion = CurrentRegion;
            announcer.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class KvasirBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public KvasirBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }

        public static bool IsPulled = false;
        private bool StartMezz = false;
        private bool AggroText = false;
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if(!AggroText && Body.TargetObject != null)
            {
                if (Body.TargetObject is GamePlayer)
                {
                    GamePlayer player = Body.TargetObject as GamePlayer;
                    if (player != null && player.IsAlive)
                    {
                        BroadcastMessage(String.Format("To come this far... only to die a horrible death! Huh! Do you not wish that you were taking on a safer endavour at this moment? You realize of course that all of your efforts will come to naught as you are about to die " + player.CharacterClass.Name + "?"));
                        AggroText = true;
                    }
                }
            }
            if (IsPulled == false)
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "KvasirBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);
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
                IsPulled = false;
                StartMezz = false;
                AggroText = false;
                var prepareMezz = Body.TempProperties.getProperty<ECSGameTimer>("kvasir_prepareMezz");//cancel message
                if (prepareMezz != null)
                {
                    prepareMezz.Stop();
                    Body.TempProperties.removeProperty("kvasir_prepareMezz");
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

            if (HasAggro && Body.TargetObject != null)
            {
                if (!StartMezz)
                {
                   ECSGameTimer prepareMezz = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PrepareMezz), Util.Random(45000, 60000));
                    Body.TempProperties.setProperty("kvasir_prepareMezz", prepareMezz);
                    StartMezz = true;
                }
                if(!Body.IsCasting && Util.Chance(5))
                    Body.CastSpell(IssoRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            }
            base.Think();
        }
        private int PrepareMezz(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("{0} lets loose a primal scream so intense that it resonates in the surrounding ice for several seconds. Many in the immediate vicinite are stunned by the sound!", Body.Name));
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastMezz), 2000);
            return 0;
        }
        private int CastMezz(ECSGameTimer timer)
        {
            if (HasAggro && Body.TargetObject != null)
                Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            StartMezz = false;
            return 0;
        }

        #region Spells
        private Spell m_mezSpell;
        private Spell Mezz
        {
            get
            {
                if (m_mezSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 2619;
                    spell.Icon = 2619;
                    spell.Name = "Mesmerize";
                    spell.Range = 0;
                    spell.Radius = 800;
                    spell.SpellID = 11928;
                    spell.Duration = 60;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = "Mesmerize";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Spirit; //Spirit DMG Type
                    m_mezSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
                }
                return m_mezSpell;
            }
        }
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
                    spell.RecastDelay = Util.Random(45,55);
                    spell.ClientEffect = 277;
                    spell.Icon = 277;
                    spell.Duration = 60;
                    spell.Range = 1800;
                    spell.Radius = 1000;
                    spell.Value = 99;
                    spell.Name = "Kvasir's Root";
                    spell.TooltipId = 277;
                    spell.SpellID = 11741;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_IssoRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IssoRoot);
                }
                return m_IssoRoot;
            }
        }
        #endregion
    }
}
#region Tunnels Announcer
namespace DOL.GS
{
    public class Tunnels : GameNPC
    {
        public Tunnels() : base()
        {
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override bool AddToWorld()
        {
            Model = 665;
            Name = "Tunnels Announce";
            GuildName = "DO NOT REMOVE";
            RespawnInterval = 5000;
            Flags = (GameNPC.eFlags)28;

            Size = 50;
            Level = 50;
            MaxSpeedBase = 0;
            TunnelsBrain.message1 = false;
            TunnelsBrain.message2 = false;

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

            TunnelsBrain adds = new TunnelsBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class TunnelsBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TunnelsBrain()
            : base()
        {
            AggroLevel = 0;
            AggroRange = 0;
        }
        public static bool message1 = false;
        public static bool message2 = false;
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Think()
        {
            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(10000))
                {
                    if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !message2 && player.IsWithinRadius(Body, 400))
                        message2 = true;
                }
                if (message2 && !message1)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Announce), 200);
                    message1 = true;
                }
            }
            base.Think();
        }
        private int Announce(ECSGameTimer timer)
        {
            BroadcastMessage("A low rumble echoes throughout the Tuscarian Glacier! Icicles resonating with the sound break off from the ceiling and shatter on the floors!" +
                            "The rumble grows louder causing small cracks to form in the walls! It sounds as though there is a swarm of giants on the move somewhere in the glacier!");
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RemoveMob), 300);
            return 0;
        }
        private int RemoveMob(ECSGameTimer timer)
        {
            if (Body.IsAlive)
                Body.RemoveFromWorld();
            return 0;
        }
    }
}
#endregion