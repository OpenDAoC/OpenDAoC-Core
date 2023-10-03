using System;
using System.Reflection;
using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public abstract class GameEpicAros : GameEpicBoss
    {
        private static new readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Set Aros the Spiritmaster difficulty in percent of its max abilities
        /// 100 = full strength
        /// </summary>
        public virtual int ArosDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }
        /// <summary>
        /// Announcements for Bomb, BigBomb, Debuff and Death.
        /// </summary>
        protected String[] m_BombAnnounce;

        protected String m_BigBombAnnounce;
        protected String m_DebuffAnnounce;
        protected String m_SummonAnnounce;
        protected String[] m_DeathAnnounce;

        //check if he is doing spells
        private bool isBombing = true;
        private bool isBigBombing = true;
        private bool isSummoning = true;
        private bool isDebuffing = true;

        /// <summary>
        /// Creates a new instance of GameEpicAros.
        /// </summary>
        public GameEpicAros()
            : base()
        {
            m_BombAnnounce = new String[]
            {
                "{0} begins to perform a ritual!",
                "{0} is powerful and begins a threatening attack!",
                "Feeling strong and powerful, {0} prepares a deadly spell.",
                "{0} begins a magic of mental destruction!"
            };
            m_BigBombAnnounce = "{0} withdraws all souls around him in order to cast a powerful spell.";
            m_DebuffAnnounce = "{0} weakens {1} and everyone around!";
            m_SummonAnnounce = "{0} uses his power to summon a protective spirit!";
            m_DeathAnnounce = new String[]
            {
                "{0} trips and falls on the hard stone floor.",
                "'You will remember my name! {0}!'"
            };
            MaxDistance = 2500;
            TetherRange = 3500;
            SetOwnBrain(new ArosBrain());
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
            get { return 40000; }
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsReturningToSpawnPoint && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        /// <summary>
        /// Invoked when Aros the Spiritmaster dies.
        /// </summary>
        /// <param name="killer">The living that got the killing blow.</param>
        public override void Die(GameObject killer)
        {
            if (killer == null)
                log.Error("Aros The Spiritmaster Killed: killer is null!");
            else
                log.Debug("Aros The Spiritmaster Killed: killer is " + killer.Name + ", attackers:");
            base.StopCurrentSpellcast();
            base.Die(killer);

            foreach (String message in m_DeathAnnounce)
            {
                BroadcastMessage(String.Format(message, Name));
            }
            foreach (GameNPC npc in this.GetNPCsInRadius(4000))
            {
                if (npc.Name.Contains("Guardian of Aros"))
                {
                    npc.Die(killer);
                }

                if (npc.Name.Contains("Summoned Guardian"))
                {
                    npc.Die(killer);
                }
            }
        }

        #region Damage & Heal Events

        /// <summary>
        /// Take some amount of damage inflicted by another GameObject.
        /// </summary>
        /// <param name="source">The object inflicting the damage.</param>
        /// <param name="damageType">The type of damage.</param>
        /// <param name="damageAmount">The amount of damage inflicted.</param>
        /// <param name="criticalAmount">The critical amount of damage inflicted</param>
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            m_healthPercentOld = HealthPercent;
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            Brain.Notify(GameObjectEvent.TakeDamage, this,
                new TakeDamageEventArgs(source, damageType, damageAmount, criticalAmount));
        }

        /// <summary>
        /// Take action upon someone healing the enemy.
        /// </summary>
        /// <param name="enemy">The living that was healed.</param>
        /// <param name="healSource">The source of the heal.</param>
        /// <param name="changeType">The way the living was healed.</param>
        /// <param name="healAmount">The amount that was healed.</param>
        public override void EnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType,
            int healAmount)
        {
            base.EnemyHealed(enemy, healSource, changeType, healAmount);
            Brain.Notify(GameLivingEvent.EnemyHealed, this,
                new EnemyHealedEventArgs(enemy, healSource, changeType, healAmount));
        }

        #endregion

        /// <summary>
        /// Return to spawn point, Aros the Spiritmaster can't be attacked while it's
        /// on it's way.
        /// </summary>
        public override void ReturnToSpawnPoint(short speed)
        {
            base.ReturnToSpawnPoint(MaxSpeed);
        }
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsReturningToSpawnPoint)
                return;

            base.OnAttackedByEnemy(ad);
        }

        #region Health

        private int m_healthPercentOld = 100;

        /// <summary>
        /// The amount of health before the most recent attack.
        /// </summary>
        public int HealthPercentOld
        {
            get { return m_healthPercentOld; }
            protected set { m_healthPercentOld = value; }
        }

        #endregion

        #region Custom Methods

        /// <summary>
        /// Broadcast relevant messages to the raid.
        /// </summary>
        /// <param name="message">The message to be broadcast.</param>
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
            if (sender == this)
            {
                GameLiving source = sender as GameLiving;
                if (e == GameObjectEvent.TakeDamage)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
                    {
                        CheckDebuff(player);
                        CheckChanceBomb(player);
                    }
                }
            }
        }

        #endregion

        public void SpawnGuardian(GamePlayer enemy)
        {
            GameNPC summonedGuard = new GameNPC();
            SetVariables(summonedGuard);
            summonedGuard.AddToWorld();
            summonedGuard.StartAttack(enemy);
        }
        public void SetVariables(GameNPC summonedGuardian)
        {
            summonedGuardian.LoadEquipmentTemplateFromDatabase("Summoned_Guardian");
            summonedGuardian.Health = summonedGuardian.MaxHealth;
            summonedGuardian.X = this.X + 200;
            summonedGuardian.Y = this.Y;
            summonedGuardian.Z = this.Z;
            summonedGuardian.CurrentRegion = this.CurrentRegion;
            summonedGuardian.Heading = this.Heading;
            summonedGuardian.Level = 65;
            summonedGuardian.Realm = this.Realm;
            summonedGuardian.Faction = FactionMgr.GetFactionByID(779);
            summonedGuardian.Name = "Guardian of Aros";
            summonedGuardian.GuildName = "Summoned Ghost";
            summonedGuardian.Model = 140;
            summonedGuardian.Size = 65;
            summonedGuardian.AttackRange = 200;
            summonedGuardian.Flags |= eFlags.GHOST;
            summonedGuardian.MeleeDamageType = eDamageType.Spirit;
            summonedGuardian.RespawnInterval = -1; // dont respawn
            summonedGuardian.RoamingRange = this.RoamingRange;
            summonedGuardian.MaxDistance = 2000;
            summonedGuardian.MaxSpeedBase = this.MaxSpeedBase;

            // also copies the stats
            summonedGuardian.CurrentSpeed = 200;

            summonedGuardian.Strength = this.Strength;
            summonedGuardian.Constitution = this.Constitution;
            summonedGuardian.Dexterity = this.Dexterity;
            summonedGuardian.Quickness = this.Quickness;
            summonedGuardian.Intelligence = this.Intelligence;
            summonedGuardian.Empathy = this.Empathy;
            summonedGuardian.Piety = this.Piety;
            summonedGuardian.Charisma = this.Charisma;

            if (summonedGuardian.Inventory != null)
                summonedGuardian.SwitchWeapon(this.ActiveWeaponSlot);

            ABrain mobBrain = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                mobBrain = (ABrain) assembly.CreateInstance(this.Brain.GetType().FullName, true);
                if (mobBrain != null)
                    break;
            }

            if (mobBrain == null)
            {
                summonedGuardian.SetOwnBrain(new StandardMobBrain());
            }
            else if (mobBrain is StandardMobBrain)
            {
                StandardMobBrain sbrain = (StandardMobBrain) mobBrain;
                StandardMobBrain tbrain = (StandardMobBrain) Brain;
                sbrain.AggroLevel = tbrain.AggroLevel;
                sbrain.AggroRange = tbrain.AggroRange;
                summonedGuardian.SetOwnBrain(sbrain);
            }
        }

        #region Bomb & Resist Debuff
        protected Spell m_BombSpell;
        protected Spell m_BigBombSpell;
        /// <summary>
        /// The Bomb spell. Override this property in your Aros Epic summonedGuard implementation
        /// and assign the spell to m_breathSpell.
        /// </summary>
        protected abstract Spell Bomb { get; }
        /// <summary>
        /// The Bomb spell. Override this property in your Aros Epic summonedGuard implementation
        /// and assign the spell to m_breathSpell.
        /// </summary>
        protected abstract Spell BigBomb { get; }
        private const int m_BombChance = 3;
        private GameLiving m_BombTarget;
        /// <summary>
        /// Chance to cast Summon when a potential target has been detected.
        /// </summary>
        protected int BombChance
        {
            get { return m_BombChance; }
        }
        /// <summary>
        /// The target for the next Summon attack.
        /// </summary>
        private GameLiving BombTarget
        {
            get { return m_BombTarget; }
            set
            {
                m_BombTarget = value;
                PrepareToBombChance();
            }
        }
        /// <summary>
        /// Check whether or not to cast Bomb.
        /// </summary>
        public bool CheckChanceBomb(GameLiving target)
        {
            if (target == null || BombTarget != null) return false;
            bool success = Util.Chance(BombChance);
            if (success)
                BombTarget = target;
            return success; // Has a 3% chance to cast.
        }
        /// <summary>
        /// Announce the Debuff and start the 1 second timer.
        /// </summary>
        private void PrepareToBombChance()
        {
            if (BombTarget == null) return;
            TurnTo(BombTarget);
            WalkTo(BombTarget, 250);
            int messageNo = Util.Random(1, m_BombAnnounce.Length) - 1;
            BroadcastMessage(String.Format(m_BombAnnounce[messageNo], Name));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CastBomb), 5000);
        }
        /// <summary>
        /// Check whether or not to cast Bomb.
        /// </summary>
        public bool CheckBomb()
        {
            PrepareToBomb(); // Has a 100% chance to cast.
            return true;
        }
        /// <summary>
        /// Check whether or not to cast the Big Bomb.
        /// </summary>
        public bool CheckBigBomb()
        {
            PrepareToBigBomb();
            return false;
        }
        /// <summary>
        /// Announce the Bomb attack and start the 4 second timer.
        /// </summary>
        private void PrepareToBomb()
        {
            // Prevent brain from casting this over and over.
            HealthPercentOld = HealthPercent;
            int messageNo = Util.Random(1, m_BombAnnounce.Length) - 1;
            BroadcastMessage(String.Format(m_BombAnnounce[messageNo], Name));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CastBomb), 5000);
        }
        /// <summary>
        /// Announce the Big Bomb and start the 5 second timer.
        /// </summary>
        private void PrepareToBigBomb()
        {
            HealthPercentOld = HealthPercent;
            BroadcastMessage(String.Format(m_BigBombAnnounce, Name));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CastBigBomb), 5000);
        }
        /// <summary>
        /// Cast Breath on the raid (AoE damage and AoE resist debuff).
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastBomb(ECSGameTimer timer)
        {
            CastSpell(Bomb, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        /// <summary>
        /// Cast Bomb on the raid (AoE damage and AoE resist debuff).
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastBigBomb(ECSGameTimer timer)
        {
            CastSpell(BigBomb, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        #endregion

        #region Debuff
        protected Spell m_DebuffSpell;
        /// <summary>
        /// The Debuff spell. Override this property in your Aros the Spiritmaster implementation
        /// and assign the spell to m_DebuffSpell.
        /// </summary>
        protected abstract Spell Debuff { get; }
        private const int m_DebuffChance = 3;
        /// <summary>
        /// Chance to cast Debuff when a potential target has been detected.
        /// </summary>
        protected int DebuffChance
        {
            get { return m_DebuffChance; }
        }
        private GameLiving m_DebuffTarget;
        /// <summary>
        /// The target for the next Debuff attack.
        /// </summary>
        private GameLiving DebuffTarget
        {
            get { return m_DebuffTarget; }
            set
            {
                m_DebuffTarget = value;
                PrepareToDebuff();
            }
        }
        /// <summary>
        /// Check whether or not to Debuff at this target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns>Whether or not the spell was cast.</returns>
        public bool CheckDebuff(GameLiving target)
        {
            if (target == null || DebuffTarget != null) return false;
            bool success = Util.Chance(DebuffChance);
            if (success)
                DebuffTarget = target;
            return success;
        }
        /// <summary>
        /// Announce the Debuff and start the 1 second timer.
        /// </summary>
        private void PrepareToDebuff()
        {
            if (DebuffTarget == null) return;
            TurnTo(DebuffTarget);
            BroadcastMessage(String.Format(m_DebuffAnnounce, Name, DebuffTarget.Name));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CastDebuff), 1000);
        }
        /// <summary>
        /// Cast Debuff on the target.
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastDebuff(ECSGameTimer timer)
        {
            // Turn around to the target and cast Debuff, then go back to the original
            // target, if one exists.
            GameObject oldTarget = TargetObject;
            TargetObject = DebuffTarget;
            Z = SpawnPoint.Z; // this is a fix to correct Z errors that sometimes happen during Aros fights
            TurnTo(DebuffTarget);
            CastSpell(Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            DebuffTarget = null;
            if (oldTarget != null) TargetObject = oldTarget;
            return 0;
        }
        #endregion

        #region Pet
        protected Spell m_SummonSpell;
        /// <summary>
        /// The Debuff spell. Override this property in your Aros the Spiritmaster implementation
        /// and assign the spell to m_SummonSpell.
        /// </summary>
        protected abstract Spell Summon { get; }
        private const int m_SummonChance = 100;
        /// <summary>
        /// Chance to cast Summon when a potential target has been detected.
        /// </summary>
        protected int SummonChance
        {
            get { return m_SummonChance; }
        }
        private GameLiving m_SummonTarget;
        /// <summary>
        /// The target for the next Summon attack.
        /// </summary>
        private GameLiving SummonTarget
        {
            get { return m_SummonTarget; }
            set
            {
                m_SummonTarget = value;
                PrepareToSummon();
            }
        }
        /// <summary>
        /// Check whether or not to Summon the Pet.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns>Whether or not the spell was cast.</returns>
        public bool CheckSummon()
        {
            PrepareToSummon();
            return true;
        }
        /// <summary>
        /// Announce the Summon and start the 2 second timer.
        /// </summary>
        private void PrepareToSummon()
        {
            HealthPercentOld = HealthPercent;
            BroadcastMessage(String.Format(m_SummonAnnounce, Name));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CastSummon), 2000);
        }
        /// <summary>
        /// Cast Summon.
        /// </summary>
        /// <param name="timer">The timer that started this cast.</param>
        /// <returns></returns>
        private int CastSummon(ECSGameTimer timer)
        {
            CastSpell(Summon, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            GameLiving target = null;
            foreach (GamePlayer player in GetPlayersInRadius(1500))
            {
                if (player == null) continue;

                if (player != null)
                {
                    if (IsCasting)
                    {
                        if (GameServer.ServerRules.IsAllowedToAttack(this, player, false) && isSummoning)
                        {
                            SpawnGuardian(player);
                            target = player;
                            break;
                        }
                        isSummoning = false;
                    }
                    else
                    {
                        isSummoning = true;
                    }
                }
                if (target == null || Summon == null)
                    return 1;
            }
            return 0;
        }
        #endregion
    }
}
