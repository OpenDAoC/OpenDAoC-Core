using System.Reflection;
using log4net;
using System;
using System.Collections;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS;
using DOL.Events;
using System.Collections.Specialized;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Sojourner
    //no shared timer
    #region Sojourner-1
    //Gameplayer - MaxEncumbrance
    #endregion

    //ML2 Unending Breath - already handled in another area

    //ML3 Reveal Crystalseed - already handled in another area

    //no shared timer
    #region Sojourner-4
    [SpellHandlerAttribute("UnmakeCrystalseed")]
    public class UnmakeCrystalseedHandler : SpellHandler
    {
        /// <summary>
        /// Execute unmake crystal seed spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        /// <summary>
        /// execute non duration spell effect on target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            base.OnDirectEffect(target, effectiveness);
            if (target == null || !target.IsAlive)
                return;

            foreach (GameNpc item in target.GetNPCsInRadius((ushort)m_spell.Radius))
            {
                if (item != null && item is GameMine)
                {
                    (item as GameMine).Delete();
                }
            }
        }

        public UnmakeCrystalseedHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Sojourner-5
    [SpellHandlerAttribute("AncientTransmuter")]
    public class AncientTransmuterHandler : SpellHandler
    {
        private GameMerchant merchant;
        /// <summary>
        /// Execute Acient Transmuter summon spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            if (effect.Owner == null || !effect.Owner.IsAlive)
                return;

            merchant.AddToWorld();
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (merchant != null) merchant.Delete();
            return base.OnEffectExpires(effect, noMessages);
        }
        public AncientTransmuterHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            if (caster is GamePlayer)
            {
                GamePlayer casterPlayer = caster as GamePlayer;
                merchant = new GameMerchant();
                //Fill the object variables
                merchant.X = casterPlayer.X + UtilCollection.Random(20, 40) - UtilCollection.Random(20, 40);
                merchant.Y = casterPlayer.Y + UtilCollection.Random(20, 40) - UtilCollection.Random(20, 40);
                merchant.Z = casterPlayer.Z;
                merchant.CurrentRegion = casterPlayer.CurrentRegion;
                merchant.Heading = (ushort)((casterPlayer.Heading + 2048) % 4096);
                merchant.Level = 1;
                merchant.Realm = casterPlayer.Realm;
                merchant.Name = "Ancient Transmuter";
                merchant.Model = 993;
                merchant.CurrentSpeed = 0;
                merchant.MaxSpeedBase = 0;
                merchant.GuildName = "";
                merchant.Size = 50;
                merchant.Flags |= GameNpc.eFlags.PEACE;
                merchant.TradeItems = new MerchantTradeItems("ML_transmuteritems");
            }
        }
    }
    #endregion

    //no shared timer
    #region Sojourner-6
    [SpellHandlerAttribute("Port")]
    public class PortHandler : MasterLevelHandling
    {
        // constructor
        public PortHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = Caster as GamePlayer;

            if (player != null)
            {
                if (!player.InCombat && !GameStaticRelic.IsPlayerCarryingRelic(player))
                {
                    SendEffectAnimation(player, 0, false, 1);
					player.MoveToBind();
                }
            }
        }
    }
    #endregion

    //no shared timer
	#region Sojourner-7
	[SpellHandlerAttribute("EssenceResist")]
	public class EssenceResistHandler : AbstractResistBuff
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
		public override EProperty Property1 { get { return EProperty.Resist_Natural; } }
		public EssenceResistHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
	#endregion Sojourner-7

    //no shared timer
    #region Sojourner-8
    [SpellHandlerAttribute("Zephyr")]
    public class FzHandler : MasterLevelHandling
    {
        protected ECSGameTimer m_expireTimer;
        protected GameNpc m_npc;
        protected GamePlayer m_target;
		protected IPoint3D m_loc;

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;
            GamePlayer player = target as GamePlayer;
            if (player != null && player.IsAlive)
            {
                Zephyr(player);
            }
        }

        public override bool CheckBeginCast(GameLiving target)
        {
            if (target == null)
            {
                MessageToCaster("You must select a target for this spell!", EChatType.CT_SpellResisted);
                return false;
            }

            if (target is GameNpc == true)
                return false;

            if (!GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
                return false;

            return base.CheckBeginCast(target);
        }

        private void Zephyr(GamePlayer target)
        {
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            GameNpc npc = new GameNpc();

            m_npc = npc;

            npc.Realm = Caster.Realm;
            npc.Heading = Caster.Heading;
            npc.Model = 1269;
            npc.Y = Caster.Y;
            npc.X = Caster.X;
            npc.Z = Caster.Z;
            npc.Name = "Forceful Zephyr";
            npc.MaxSpeedBase = 400;
            npc.Level = 55;
            npc.CurrentRegion = Caster.CurrentRegion;
            npc.Flags |= GameNpc.eFlags.PEACE;
            npc.Flags |= GameNpc.eFlags.DONTSHOWNAME;
			npc.Flags |= GameNpc.eFlags.CANTTARGET;
            BlankBrain brain = new BlankBrain();
            npc.SetOwnBrain(brain);
            npc.AddToWorld();
            npc.TempProperties.setProperty("target", target);
            GameEventMgr.AddHandler(npc, GameNpcEvent.ArriveAtTarget, new CoreEventHandler(ArriveAtTarget));
            npc.Follow(target, 10, 1500);

            m_target = target;

            StartTimer();

        }

        protected virtual void StartTimer()
        {
            StopTimer();
            m_expireTimer = new ECSGameTimer(m_npc, new ECSGameTimer.ECSTimerCallback(ExpiredCallback), 10000);
        }

        protected virtual int ExpiredCallback(ECSGameTimer callingTimer)
        {
            m_target.IsStunned = false;
			m_target.DismountSteed(true);
            m_target.DebuffCategory[(int)EProperty.SpellFumbleChance]-=100;
            GameEventMgr.RemoveHandler(m_target, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
            m_npc.StopMoving();
            m_npc.RemoveFromWorld();
			//sometimes player can't move after zephyr :
			m_target.Out.SendUpdateMaxSpeed();
            return 0;
        }

        protected virtual void StopTimer()
        {

            if (m_expireTimer != null)
            {
                m_expireTimer.Stop();
                m_expireTimer = null;
            }

        }

        private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
        {
            GameLiving living = sender as GameLiving;
            if (living == null) return;
            AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
            AttackData ad = null;
            if (attackedByEnemy != null)
                ad = attackedByEnemy.AttackData;

            double absorbPercent = 100;
            int damageAbsorbed = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));
            int spellAbsorbed = (int)(0.01 * absorbPercent * Spell.Damage);

            ad.Damage -= damageAbsorbed;
            ad.Damage -= spellAbsorbed;

            MessageToLiving(ad.Target, string.Format("You're in a Zephyr and can't be attacked!"), EChatType.CT_Spell);
            MessageToLiving(ad.Attacker, string.Format("Your target is in a Zephyr and can't be attacked!"), EChatType.CT_Spell);
        }

        private void ArriveAtTarget(CoreEvent e, object obj, EventArgs args)
        {
            GameNpc npc = obj as GameNpc;

            if (npc == null) return;

            GamePlayer target = npc.TempProperties.getProperty<object>("target", null) as GamePlayer;

            if (target == null || !target.IsAlive) return;
            GameEventMgr.RemoveHandler(npc, GameNpcEvent.ArriveAtTarget, new CoreEventHandler(ArriveAtTarget));

            GamePlayer player = target as GamePlayer;
            if (player == null) return;
            if (!player.IsAlive) return;

            player.IsStunned = true;
            //player.IsSilenced = true;
            player.DebuffCategory[(int)EProperty.SpellFumbleChance]+=100;
            player.attackComponent.StopAttack();
            player.StopCurrentSpellcast();
            player.MountSteed(npc, true);
            GameEventMgr.AddHandler(player, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));

            player.Out.SendMessage("You are picked up by a forceful zephyr!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            npc.StopFollowing();

            if (Caster is GamePlayer)
            {
                //Calculate random target
                m_loc = GetTargetLoc();
				(Caster as GamePlayer).Out.SendCheckLOS((Caster as GamePlayer), m_npc, new CheckLOSResponse(ZephyrCheckLOS));
            }
        }
		public void ZephyrCheckLOS(GamePlayer player, ushort response, ushort targetOID)
        {
            if ((response & 0x100) == 0x100)
				m_npc.WalkTo(m_loc.X, m_loc.Y, m_loc.Z, 100);
        }

        public virtual IPoint3D GetTargetLoc()
        {
            double targetX = m_npc.X + UtilCollection.Random(-1500, 1500);
            double targetY = m_npc.Y + UtilCollection.Random(-1500, 1500);

            return new Point3D((int)targetX, (int)targetY, m_npc.Z);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public FzHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Sojourner-9
    [SpellHandlerAttribute("Phaseshift")]
    public class PhaseshiftHandler : MasterLevelHandling
    {
        private int endurance;

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            endurance = (Caster.MaxEndurance * 50) / 100;

            if (Caster.Endurance < endurance)
            {
                MessageToCaster("You need 50% endurance for this spell!!", EChatType.CT_System);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameEventMgr.AddHandler(Caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
            Caster.Endurance -= endurance;
        }

        private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
        {
            GameLiving living = sender as GameLiving;
            if (living == null) return;
            AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
            AttackData ad = null;
            if (attackedByEnemy != null)
                ad = attackedByEnemy.AttackData;

            if (ad.Attacker is GamePlayer)
            {
                ad.Damage = 0;
                ad.CriticalDamage = 0;
                GamePlayer player = ad.Attacker as GamePlayer;
                player.Out.SendMessage(living.Name + " is Phaseshifted and can't be attacked!", EChatType.CT_Missed, EChatLoc.CL_SystemWindow);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
            return base.OnEffectExpires(effect, noMessages);
        }

        public override bool HasPositiveEffect
        {
            get
            {
                return false;
            }
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public PhaseshiftHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Sojourner-10
    [SpellHandlerAttribute("Groupport")]
    public class GroupPortHandler : MasterLevelHandling
    {
        public GroupPortHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is GamePlayer && Caster.CurrentRegionID == 51 && ((GamePlayer)Caster).BindRegion == 51)
            {
                if (Caster.CurrentRegionID == 51)
                {
                    MessageToCaster("You can't use this Ability here", EChatType.CT_SpellResisted);
                    return false;
                }
                else
                {
                    MessageToCaster("Bind in another Region to use this Ability", EChatType.CT_SpellResisted);
                    return false;
                }
            }
            return base.CheckBeginCast(selectedTarget);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = Caster as GamePlayer;
            if ((player != null) && (player.Group != null))
            {
                if (player.Group.IsGroupInCombat())
                {
                    player.Out.SendMessage("You can't teleport a group that is in combat!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return;
                }
                else
                {
                    foreach (GamePlayer pl in player.Group.GetPlayersInTheGroup())
                    {
                        if (pl != null)
                        {
                            SendEffectAnimation(pl, 0, false, 1);
                            pl.MoveTo((ushort)player.BindRegion, player.BindXpos, player.BindYpos, player.BindZpos, (ushort)player.BindHeading);
                        }
                    }
                }
            }
            else
            {
                player.Out.SendMessage("You are not a part of a group!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }
        }
    }
    #endregion
}