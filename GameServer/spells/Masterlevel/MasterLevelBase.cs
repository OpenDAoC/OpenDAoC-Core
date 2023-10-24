using System;
using System.Collections.Generic;
using System.Drawing;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS.Spells
{
    #region MasterlevelBase
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public class MasterlevelHandling : SpellHandler
    {
        public override bool HasPositiveEffect
        {
            get { return false; }
        }

        public override bool IsUnPurgeAble
        {
            get { return true; }
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        #region Targets

        public static IList<GameLiving> SelectTargets(SpellHandler spellHandler, GameLiving target)
        {
            var list = new List<GameLiving>(8);
            Spell spell = spellHandler.Spell;
            GameLiving caster = spellHandler.Caster;

            switch (spell.Target)
            {
                //GTAoE
                case eSpellTarget.AREA:
                {
                    if (spell.Radius > 0)
                    {
                        foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(caster.CurrentRegionID, caster.GroundTarget.X, caster.GroundTarget.Y, caster.GroundTarget.Z, (ushort) spell.Radius))
                        {
                            if (GameServer.ServerRules.IsAllowedToAttack(caster, player, true))
                                list.Add(player);
                        }
                    }

                    break;
                }
                case eSpellTarget.PET:
                {
                    if (caster is GamePlayer player)
                    {
                        IControlledBrain npc = player.ControlledBrain;

                        if (npc != null)
                            list.Add(npc.Body);
                    }

                    break;
                }
                case eSpellTarget.ENEMY:
                {
                    if (spell.Radius > 0)
                    {
                        target = caster;
                        foreach (GamePlayer player in target.GetPlayersInRadius((ushort) spell.Radius))
                        {
                            if (GameServer.ServerRules.IsAllowedToAttack(caster, player, true))
                                list.Add(player);
                        }
                    }
                    else
                    {
                        if (target != null && GameServer.ServerRules.IsAllowedToAttack(caster, target, true))
                            list.Add(target);
                    }

                    break;
                }
                case eSpellTarget.REALM:
                {
                    if (spell.Radius > 0)
                    {
                        if (target == null || spell.Range == 0)
                            target = caster;

                        foreach (GamePlayer player in target.GetPlayersInRadius((ushort) spell.Radius))
                        {
                            if (GameServer.ServerRules.IsAllowedToAttack(caster, player, true) == false)
                                list.Add(player);
                        }
                    }
                    else
                    {
                        if (target != null && GameServer.ServerRules.IsAllowedToAttack(caster, target, true) == false)
                            list.Add(target);
                    }

                    break;
                }
                case eSpellTarget.SELF:
                {
                    if (spell.Radius > 0)
                    {
                        if (target == null || spell.Range == 0)
                            target = caster;

                        foreach (GamePlayer player in target.GetPlayersInRadius((ushort) spell.Radius))
                        {
                            if (GameServer.ServerRules.IsAllowedToAttack(caster, player, true) == false)
                                list.Add(player);
                        }
                    }
                    else
                        list.Add(caster);

                    break;
                }
                case eSpellTarget.GROUP:
                {
                    Group group = caster.Group;
                    int spellRange = spellHandler.CalculateSpellRange();

                    if (spellRange == 0)
                        spellRange = spell.Radius;

                    if (group == null)
                    {
                        list.Add(caster);
                        IControlledBrain npc = caster.ControlledBrain;

                        if (npc != null)
                        {
                            if (caster.IsWithinRadius(npc.Body, spellRange))
                                list.Add(npc.Body);
                        }
                    }
                    else
                    {
                        foreach (GameLiving living in group.GetMembersInTheGroup())
                        {
                            // only players in range
                            if (caster.IsWithinRadius(living, spellRange))
                                list.Add(living);

                            IControlledBrain npc = living.ControlledBrain;

                            if (npc != null)
                            {
                                if (living.IsWithinRadius(npc.Body, spellRange))
                                    list.Add(npc.Body);
                            }
                        }
                    }

                    break;
                }
            }

            return list;
        }

        /// <summary>
        /// Select all targets for this spell
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            return SelectTargets(this, castTarget as GameLiving);
        }
        #endregion

        /// <summary>
        /// Current depth of delve info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                return list;
            }
        }

        public MasterlevelHandling(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region Stylhandler
    [SpellHandlerAttribute("MLStyleHandler")]
    public class MLStyleHandler : MasterlevelHandling
    {
        public MLStyleHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region MasterlevelDebuff
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public abstract class MasterlevelDebuffHandling : SingleStatDebuff
    {
        // bonus category
        public override eBuffBonusCategory BonusCategory1 { get { return eBuffBonusCategory.Debuff; } }

        public override bool HasPositiveEffect
        {
            get { return false; }
        }

        public override bool IsUnPurgeAble
        {
            get { return true; }
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        #region Targets
        /// <summary>
        /// Select all targets for this spell
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            return MasterlevelHandling.SelectTargets(this, castTarget as GameLiving);
        }
        #endregion

        /// <summary>
        /// Current depth of delve info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                return list;
            }
        }

        public MasterlevelDebuffHandling(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region MasterlevelBuff
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public abstract class MasterlevelBuffHandling : SingleStatBuff
    {
        // bonus category
        public override eBuffBonusCategory BonusCategory1 { get { return eBuffBonusCategory.BaseBuff; } }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        #region Targets
        /// <summary>
        /// Select all targets for this spell
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            return MasterlevelHandling.SelectTargets(this, castTarget as GameLiving);
        }
        #endregion

        /// <summary>
        /// Current depth of delve info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                return list;
            }
        }

        protected MasterlevelBuffHandling(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region MasterlevelDualBuff
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public abstract class MasterlevelDualBuffHandling : DualStatBuff
    {
        // bonus category
        public override eBuffBonusCategory BonusCategory1 { get { return eBuffBonusCategory.BaseBuff; } }
        public override eBuffBonusCategory BonusCategory2 { get { return eBuffBonusCategory.SpecBuff; } }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        #region Targets
        /// <summary>
        /// Select all targets for this spell
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            return MasterlevelHandling.SelectTargets(this, castTarget as GameLiving);
        }
        #endregion

        /// <summary>
        /// Current depth of delve info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                return list;
            }
        }

        protected MasterlevelDualBuffHandling(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region BanelordSnare
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public class BanelordSnare : UnbreakableSpeedDecreaseSpellHandler
    {
        private const string EFFECT_PROPERTY = "BanelordSnareProperty";

        public override bool HasPositiveEffect
        {
            get { return false; }
        }

        public override bool IsUnPurgeAble
        {
            get { return true; }
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            ECSGameTimer timer = effect.Owner.TempProperties.GetProperty<ECSGameTimer>(EFFECT_PROPERTY, null);
            effect.Owner.TempProperties.RemoveProperty(EFFECT_PROPERTY);
            timer.Stop();

            effect.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, effect);

            SendUpdates(effect.Owner);
            MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), eChatType.CT_SpellExpires, effect.Owner);

            return 0;
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        #region Targets
        /// <summary>
        /// Select all targets for this spell
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            return MasterlevelHandling.SelectTargets(this, castTarget as GameLiving);
        }
        #endregion

        /// <summary>
        /// Current depth of delve info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                return list;
            }
        }

        public BanelordSnare(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region Fontbase
    public class FontSpellHandler : SpellHandler
    {
        protected GameFont font;
        protected DbSpell dbs;
        protected Spell s;
        protected SpellLine sl;
        protected ISpellHandler fontSpell;
        protected bool ApplyOnNPC = false;
        protected bool ApplyOnCombat = false;
        protected bool Friendly = true;
        protected ushort sRadius = 350;

        protected uint m_pulseFrequency;
        int currentTick = 0;
        int currentPulse = 0;
        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            return false;
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            if(font != null)
            {
                font.AddToWorld();
                neweffect.Start(font);
                new ECSGameTimer(font, new ECSGameTimer.ECSTimerCallback(PulseTimer), 1000);
                GameEventMgr.AddHandler(m_caster, GamePlayerEvent.RemoveFromWorld, new DOLEventHandler(PlayerLeftWorld));
            }
        }
        protected virtual int PulseTimer(ECSGameTimer timer)
        {
            if (currentTick >= Spell.Duration/1000 || m_caster == null || font == null || font.ObjectState == GameObject.eObjectState.Deleted)
            {
                font.RemoveFromWorld();
                font.Delete();
                timer.Stop();
                timer = null;
                return 0;
            }
            if (currentTick % 3 == 0)
            {
                currentPulse++;
                foreach (GamePlayer target in font.GetPlayersInRadius(sRadius))
                {
                    if (fontSpell.HasPositiveEffect)
                    {
                        if (!GameServer.ServerRules.IsAllowedToAttack(font, target, true))
                        {
                            CastSpell(target);

                        }
                    }
                    else
                    {
                        if (GameServer.ServerRules.IsAllowedToAttack(font, target, true))
                        {
                            CastSpell(target);
                        }
                    }
                }
                if (ApplyOnNPC)
                {
                    foreach (GameNPC npc in font.GetNPCsInRadius(sRadius))
                    {
                        if (fontSpell.HasPositiveEffect)
                        {
                            if (!GameServer.ServerRules.IsAllowedToAttack(font, npc, true))
                            {
                                CastSpell(npc);

                            }
                        }
                        else
                        {
                            if (GameServer.ServerRules.IsAllowedToAttack(font, npc, true))
                            {
                                CastSpell(npc);
                            }
                        }
                    }
                }
            }

            currentTick++;
            return 1000;
        }
        protected virtual void PlayerLeftWorld(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = (GamePlayer)sender;
            if (this.m_caster == player)
            {
                currentTick = Spell.Duration;
            }
        }

        protected virtual void CastSpell(GameLiving target)
        {
            fontSpell.Target = target;
            fontSpell.StartSpell(target);
        }

        // constructor
        public FontSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }	
    #endregion

    #region Trapbase
    public class MineSpellHandler : SpellHandler
    {
        protected GameMine mine;
        protected ISpellHandler trap;
        protected GameSpellEffect m_effect;
        protected DbSpell dbs;
        protected Spell s;
        protected SpellLine sl;
        protected bool Unstealth = true;
        protected bool DestroyOnEffect = true;
        protected ushort sRadius = 350;
		private Area.Circle traparea;
		private ECSGameTimer ticktimer;
		private ushort region;


		private int onTick(ECSGameTimer timer)
		{
			removeHandlers();
			return 0;
		}

		protected void EventHandler(DOLEvent e, Object sender, EventArgs arguments)
		{
			AreaEventArgs args = arguments as AreaEventArgs;
			if (args == null)
				return;
			GameLiving living = args.GameObject as GameLiving;
			if (living == null)
				return;
			if (!GameServer.ServerRules.IsAllowedToAttack(Caster, living, true))
				return;
			getTargets();
		}


		private void removeHandlers()
		{
			mine.CurrentRegion.RemoveArea(traparea);
			if (mine != null) mine.Delete();
			GameEventMgr.RemoveHandler(traparea, AreaEvent.PlayerEnter, new DOLEventHandler(EventHandler));
		}


		private void getTargets()
		{
			foreach (GamePlayer target in WorldMgr.GetPlayersCloseToSpot(region, traparea.X, traparea.Y, traparea.Z, sRadius))
			{
				if (GameServer.ServerRules.IsAllowedToAttack(mine, target, true))
				{
					triggerSpell(target);
				}
			}
		}

		private void triggerSpell(GameLiving target)
		{
			if (!GameServer.ServerRules.IsAllowedToAttack(mine, target, true))
				return;
			if (!target.IsAlive)
				return;
			if (ticktimer.IsAlive)
			{
				ticktimer.Stop();
				removeHandlers();
			}
			bool wasstealthed = ((GamePlayer)Caster).IsStealthed;


			trap.StartSpell((GameLiving)target);
			if (!Unstealth)
				((GamePlayer)Caster).Stealth(wasstealthed);
			if (DestroyOnEffect)
				removeHandlers();
			return;
		}

		public override void ApplyEffectOnTarget(GameLiving target)
        {
            mine.AddToWorld();

			traparea = new Area.Circle(s.Name, Caster.X, Caster.Y, Caster.Z, 75);

			Caster.CurrentRegion.AddArea(traparea);
			region = Caster.CurrentRegionID;

			GameEventMgr.AddHandler(traparea, AreaEvent.PlayerEnter, new DOLEventHandler(EventHandler));
			ticktimer = new ECSGameTimer(Caster);
			ticktimer.Callback = new ECSGameTimer.ECSTimerCallback(onTick);
			ticktimer.Start(600000);
			getTargets();
		}

        public override void OnEffectStart(GameSpellEffect effect)
        {
            if (mine == null) return;
            base.OnEffectStart(effect);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (mine != null) mine.Delete();
            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }
        // constructor
        public MineSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region Stormbase
    public class StormSpellHandler : SpellHandler
    {
        protected GameStorm storm;
        protected DbSpell dbs;
        protected Spell s;
        protected SpellLine sl;
        protected ISpellHandler tempest;
        protected ushort sRadius = 350;

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            if (storm == null || storm.ObjectState == GameObject.eObjectState.Deleted)
            {
                effect.Cancel(false);
                return;
            }
            if (tempest == null || s == null)
            {
                return;
            }
            int ranged = storm.GetDistanceTo(new Point3D((int)effect.Owner.X, (int)effect.Owner.Y, (int)effect.Owner.Z));
            if (ranged > 3000) return;

            if (s.Name == "Dazzling Array")
            {
                foreach (GamePlayer player in storm.GetPlayersInRadius(sRadius))
                {
                    tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
                    if ((player.IsAlive) && (GameServer.ServerRules.IsSameRealm(storm, player, true))) tempest.StartSpell((GameLiving)player);
                }
            }
            else
            {
                foreach (GamePlayer player in storm.GetPlayersInRadius(sRadius))
                {
                    tempest = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
                    if ((player.IsAlive) && (GameServer.ServerRules.IsAllowedToAttack(storm, player, true))) tempest.StartSpell((GameLiving)player);
                }
            }
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            storm.AddToWorld();
            neweffect.Start(storm.Owner);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (storm != null) storm.Delete();
            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        // constructor
        public StormSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region SummonItemBase
    public class SummonItemSpellHandler : MasterlevelHandling
    {
        protected IList<DbInventoryItem> items;

        /// <summary>
        /// Execute create item spell
        /// </summary>
        /// <param name="target"></param>
        /// 
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override bool HasPositiveEffect
        {
            get { return true; }
        }

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
            if (target == null || !target.IsAlive)
                return;

            if (target is GamePlayer && items != null)
            {
                GamePlayer targetPlayer = target as GamePlayer;

                foreach (DbInventoryItem item in items)
                {
                    if (targetPlayer.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
                    {

                        InventoryLogging.LogInventoryAction(Caster, targetPlayer, eInventoryActionType.Other, item.Template, item.Count);
                        targetPlayer.Out.SendMessage("Item created: " + item.GetName(0, false), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }

        }

        public SummonItemSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            items = new List<DbInventoryItem>();
        }
    }
    #endregion

    #region TargetModifier
    [SpellHandlerAttribute("TargetModifier")]
    public class TargetModifierSpellHandler : MasterlevelHandling
    {
        public override bool HasPositiveEffect
        {
            get { return true; }
        }
        public TargetModifierSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region Passive
    [SpellHandlerAttribute("PassiveSpell")]
    public class PassiveSpellHandler : MasterlevelHandling
    {
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            return false;
        }

        public PassiveSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion
}

namespace DOL.GS
{
    #region Decoy
    public class GameDecoy : GameNPC
    {
        public GameDecoy()
        {
            SetOwnBrain(new BlankBrain());
            this.MaxSpeedBase = 0;
        }
        public override void Die(GameObject killer)
        {
            DeleteFromDatabase();
            Delete();
        }
        private GamePlayer m_owner;
        public GamePlayer Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
        public override int MaxHealth
        {
            get { return 1; }
        }
    }
    #endregion

    #region Gamefont
    public class GameFont : GameMovingObject
    {
        public GameFont()
        {
            SetOwnBrain(new BlankBrain());
            this.Realm = 0;
            this.Level = 1;
            this.MaxSpeedBase = 0;
            this.Flags |= GameNPC.eFlags.DONTSHOWNAME;
            this.Health = this.MaxHealth;
        }

        private GamePlayer m_owner;
        public GamePlayer Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
        public override int MaxHealth
        {
            get { int hp=500; if(Name.ToLower().IndexOf("speed")>=0) hp=100; return hp; }
        }
        public virtual int CalculateToHitChance(GameLiving target)
        {
            int spellLevel = m_owner.Level;
            GameLiving caster = m_owner as GameLiving;
            int spellbonus = m_owner.GetModified(eProperty.SpellLevel);
            spellLevel += spellbonus;
            if (spellLevel > 50)
                spellLevel = 50;
            int hitchance = 85 + ((spellLevel - target.Level) / 2);
            return hitchance;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (damageType == eDamageType.Slash || damageType == eDamageType.Crush || damageType == eDamageType.Thrust)
            {
                damageAmount /= 10;
                criticalAmount /= 10;
            }
            else
            {
                damageAmount /= 25;
                criticalAmount /= 25;
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        public override void Die(GameObject killer)
        {
            DeleteFromDatabase();
            Delete();
        }
    }
    #endregion

    #region Gametrap
    public class GameMine : GameMovingObject
    {
        public GameMine()
        {
            this.Realm = 0;
            this.Level = 1;
            this.Health = this.MaxHealth;
            this.MaxSpeedBase = 0;
        }

        private GamePlayer m_owner;
        public GamePlayer Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
        public virtual int CalculateToHitChance(GameLiving target)
        {
            int spellLevel = m_owner.Level;
            GameLiving caster = m_owner as GameLiving;
            int spellbonus = m_owner.GetModified(eProperty.SpellLevel);
            spellLevel += spellbonus;
            if (spellLevel > 50)
                spellLevel = 50;
            int hitchance = 85 + ((spellLevel - target.Level) / 2);
            return hitchance;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                damageAmount = 0;
                criticalAmount = 0;
            }
            if (Health - damageAmount - criticalAmount <= 0)
                this.Delete();
            else
                Health = Health - damageAmount - criticalAmount;

        }
    }
    #endregion

    #region GameStorm
    public class GameStorm : GameMovingObject
    {
        public GameStorm()
        {
            SetOwnBrain(new BlankBrain());
            this.Realm = 0;
            this.Level = 60;
            this.MaxSpeedBase = 191;
            this.Model = 3457;
            this.Name = "Storm";
            this.Flags |= GameNPC.eFlags.DONTSHOWNAME;
            this.Flags |= GameNPC.eFlags.CANTTARGET;
            this.Movable = true;
        }

        private GamePlayer m_owner;
        public GamePlayer Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }

        private bool m_movable;
        public bool Movable
        {
            get { return m_movable; }
            set { m_movable = value; }
        }

        public override int MaxHealth
        { 
            get { return 10000; } 
        }

        public override void Die(GameObject killer)
        {
            DeleteFromDatabase();
            Delete();
        }
        public virtual int CalculateToHitChance(GameLiving target)
        {
            int spellLevel = m_owner.Level;
            GameLiving caster = m_owner as GameLiving;
            int spellbonus = m_owner.GetModified(eProperty.SpellLevel);
            spellLevel += spellbonus;
            if (spellLevel > 50)
                spellLevel = 50;
            int hitchance = 85 + ((spellLevel - target.Level) / 2);
            return hitchance;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
        }
    }
    #endregion
}
