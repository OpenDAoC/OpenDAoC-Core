using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    #region MasterlevelBase
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public class MasterLevelSpellHandling : SpellHandler
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
                case ESpellTarget.AREA:
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
                case ESpellTarget.PET:
                {
                    if (caster is GamePlayer player)
                    {
                        IControlledBrain npc = player.ControlledBrain;

                        if (npc != null)
                            list.Add(npc.Body);
                    }

                    break;
                }
                case ESpellTarget.ENEMY:
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
                case ESpellTarget.REALM:
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
                case ESpellTarget.SELF:
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
                case ESpellTarget.GROUP:
                {
                    GroupUtil group = caster.Group;
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

        public MasterLevelSpellHandling(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region Stylhandler
    [SpellHandler("MLStyleHandler")]
    public class MLStyleHandler : MasterLevelSpellHandling
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
        public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.Debuff; } }

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
            return MasterLevelSpellHandling.SelectTargets(this, castTarget as GameLiving);
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
    public abstract class MasterLevelBuffHandling : SingleStatBuff
    {
        // bonus category
        public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }

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
            return MasterLevelSpellHandling.SelectTargets(this, castTarget as GameLiving);
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

        protected MasterLevelBuffHandling(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
    #endregion

    #region MasterlevelDualBuff
    /// <summary>
    /// Contains all common code for Banelord Spells
    /// </summary>
    public abstract class MasterlevelDualBuffHandling : DualStatBuff
    {
        // bonus category
        public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
        public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.SpecBuff; } }

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
            return MasterLevelSpellHandling.SelectTargets(this, castTarget as GameLiving);
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
    public class BanelordSnare : UnbreakableSpeedDecreaseSpell
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
            EcsGameTimer timer = effect.Owner.TempProperties.GetProperty<EcsGameTimer>(EFFECT_PROPERTY, null);
            effect.Owner.TempProperties.RemoveProperty(EFFECT_PROPERTY);
            timer.Stop();

            effect.Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, effect);

            SendUpdates(effect.Owner);
            MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
            MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), EChatType.CT_SpellExpires, effect.Owner);

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
            return MasterLevelSpellHandling.SelectTargets(this, castTarget as GameLiving);
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
    public class FontSpellHandler : DamageOverTimeSpell
    {
        protected GameFont font;
        protected DbSpell dbs;
        protected Spell s;
        protected SpellLine sl;
        protected ISpellHandler heal;
        protected bool ApplyOnNPC = false;
        protected bool ApplyOnCombat = false;
        protected bool Friendly = true;
        protected ushort sRadius = 350;

        public override bool IsOverwritable(EcsGameSpellEffect compare)
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
            }
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            if (font == null || font.ObjectState == GameObject.eObjectState.Deleted)
            {
                effect.Cancel(false);
                return;
            }

            if (heal == null || s == null) return;
            foreach (GamePlayer player in font.GetPlayersInRadius(sRadius))
            {
                if (!Friendly
                    && player.IsAlive
                    && GameServer.ServerRules.IsAllowedToAttack(Caster, player, true)
                    && (!player.InCombat
                    || ApplyOnCombat))
                        heal.StartSpell((GameLiving)player);
                else if (Friendly && player.IsAlive && (!player.InCombat || ApplyOnCombat))
                    heal.StartSpell((GameLiving)player);
            }
            if (!ApplyOnNPC) return;
            foreach (GameNpc npc in font.GetNPCsInRadius(sRadius))
            {
                if (!Friendly && npc.IsAlive && GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true) && (!npc.InCombat || ApplyOnCombat))
                    heal.StartSpell((GameLiving)npc);
                if (Friendly && npc.IsAlive && (!npc.InCombat || ApplyOnCombat))
                    heal.StartSpell((GameLiving)npc);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (font != null) font.Delete();
            effect.Owner.EffectList.Remove(effect);
            return base.OnEffectExpires(effect, noMessages);
        }

        // constructor
        public FontSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }	
    #endregion

    #region Trapbase
    public class MineSpellHandler : DamageOverTimeSpell
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
        
        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            return false;
        }
        public override void OnEffectPulse(GameSpellEffect effect)
        {
            if (mine == null || mine.ObjectState == GameObject.eObjectState.Deleted)
            {
                effect.Cancel(false);
                return;
            }

            if (trap == null || s == null) return;
            bool wasstealthed = ((GamePlayer)Caster).IsStealthed;
            foreach (GamePlayer player in mine.GetPlayersInRadius(sRadius))
            {
                if (player.IsAlive && GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
                {
                    trap.StartSpell((GameLiving)player);
                    if (!Unstealth) 
                        ((GamePlayer)Caster).Stealth(wasstealthed);
                    if (DestroyOnEffect) 
                        OnEffectExpires(effect, true);
                    return;
                }
            }
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
            mine.AddToWorld();
            neweffect.Start(mine);
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
    public class SummonItemSpellHandler : MasterLevelSpellHandling
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
                    if (targetPlayer.Inventory.AddItem(EInventorySlot.FirstEmptyBackpack, item))
                    {

                        InventoryLogging.LogInventoryAction(Caster, targetPlayer, EInventoryActionType.Other, item.Template, item.Count);
                        targetPlayer.Out.SendMessage("Item created: " + item.GetName(0, false), EChatType.CT_System, EChatLoc.CL_SystemWindow);
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
    [SpellHandler("TargetModifier")]
    public class TargetModifierSpellHandler : MasterLevelSpellHandling
    {
        public override bool HasPositiveEffect
        {
            get { return true; }
        }
        public TargetModifierSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region Passive
    [SpellHandler("PassiveSpell")]
    public class PassiveSpellHandler : MasterLevelSpellHandling
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
    public class GameDecoy : GameNpc
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
            this.Flags |= ENpcFlags.DONTSHOWNAME;
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
            int spellbonus = m_owner.GetModified(EProperty.SpellLevel);
            spellLevel += spellbonus;
            if (spellLevel > 50)
                spellLevel = 50;
            int hitchance = 85 + ((spellLevel - target.Level) / 2);
            return hitchance;
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (damageType == EDamageType.Slash || damageType == EDamageType.Crush || damageType == EDamageType.Thrust)
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
            int spellbonus = m_owner.GetModified(EProperty.SpellLevel);
            spellLevel += spellbonus;
            if (spellLevel > 50)
                spellLevel = 50;
            int hitchance = 85 + ((spellLevel - target.Level) / 2);
            return hitchance;
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
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
            this.Flags |= ENpcFlags.DONTSHOWNAME;
            this.Flags |= ENpcFlags.CANTTARGET;
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
            int spellbonus = m_owner.GetModified(EProperty.SpellLevel);
            spellLevel += spellbonus;
            if (spellLevel > 50)
                spellLevel = 50;
            int hitchance = 85 + ((spellLevel - target.Level) / 2);
            return hitchance;
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
        }
    }
    #endregion
}
