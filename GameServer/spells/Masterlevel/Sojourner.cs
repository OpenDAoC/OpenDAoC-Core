using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

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
    [SpellHandler(eSpellType.UnmakeCrystalseed)]
    public class UnmakeCrystalseedSpellHandler : SpellHandler
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

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
            if (target == null || !target.IsAlive)
                return;

            foreach (GameNPC item in target.GetNPCsInRadius((ushort)m_spell.Radius))
            {
                if (item != null && item is GameMine)
                {
                    (item as GameMine).Delete();
                }
            }
        }

        public UnmakeCrystalseedSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Sojourner-5
    [SpellHandler(eSpellType.AncientTransmuter)]
    public class AncientTransmuterSpellHandler : SpellHandler
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
        public AncientTransmuterSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            if (caster is GamePlayer)
            {
                GamePlayer casterPlayer = caster as GamePlayer;
                merchant = new GameMerchant();
                //Fill the object variables
                merchant.X = casterPlayer.X + Util.Random(20, 40) - Util.Random(20, 40);
                merchant.Y = casterPlayer.Y + Util.Random(20, 40) - Util.Random(20, 40);
                merchant.Z = casterPlayer.Z;
                merchant.CurrentRegion = casterPlayer.CurrentRegion;
                merchant.Heading = (ushort)((casterPlayer.Heading + 2048) % 4096);
                merchant.Level = 1;
                merchant.Realm = casterPlayer.Realm;
                merchant.Name = "Ancient Transmuter";
                merchant.Model = 993;
                merchant.MaxSpeedBase = 0;
                merchant.GuildName = string.Empty;
                merchant.Size = 50;
                merchant.Flags |= GameNPC.eFlags.PEACE;
                merchant.TradeItems = new MerchantTradeItems("ML_transmuteritems");
            }
        }
    }
    #endregion

    //no shared timer
    #region Sojourner-6
    [SpellHandler(eSpellType.Port)]
    public class Port : MasterlevelHandling
    {
        // constructor
        public Port(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = Caster as GamePlayer;

            if (player != null)
            {
                if (!player.InCombat && !GameRelic.IsPlayerCarryingRelic(player))
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
    [SpellHandler(eSpellType.EssenceResist)]
    public class EssenceResistHandler : AbstractResistBuff
    {
        public override eBuffBonusCategory BonusCategory1 { get { return eBuffBonusCategory.BaseBuff; } }
        public override eProperty Property1 { get { return eProperty.Resist_Natural; } }
        public EssenceResistHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion Sojourner-7

    //no shared timer
    #region Sojourner-8
    [SpellHandler(eSpellType.Zephyr)]
    public class FZSpellHandler : MasterlevelHandling
    {
        protected ECSGameTimer m_expireTimer;
        protected GameNPC m_npc;
        protected GamePlayer m_target;
        protected Point3D m_loc;

        public override void OnDirectEffect(GameLiving target)
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
                MessageToCaster("You must select a target for this spell!", eChatType.CT_SpellResisted);
                return false;
            }

            if (target is GameNPC == true)
                return false;

            if (!GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
                return false;

            return base.CheckBeginCast(target);
        }

        private void Zephyr(GamePlayer target)
        {
            if (!target.IsAlive || target.ObjectState != GameObject.eObjectState.Active)
                return;

            GameNPC npc = new()
            {
                Realm = Caster.Realm,
                Heading = Caster.Heading,
                Model = 1269,
                Y = Caster.Y,
                X = Caster.X,
                Z = Caster.Z,
                Name = "Forceful Zephyr",
                MaxSpeedBase = 400,
                Level = 55,
                CurrentRegion = Caster.CurrentRegion,
                TargetObject = target
            };

            npc.Flags |= GameNPC.eFlags.PEACE;
            npc.Flags |= GameNPC.eFlags.DONTSHOWNAME;
            npc.Flags |= GameNPC.eFlags.CANTTARGET;
            npc.SetOwnBrain(new ZephyrBrain(ArriveAtTarget));
            npc.AddToWorld();
            npc.Follow(target, npc.movementComponent.FollowMinDistance, npc.movementComponent.FollowMaxDistance);
            m_npc = npc;
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
            m_target.DebuffCategory[(int)eProperty.SpellFumbleChance]-=100;
            GameEventMgr.RemoveHandler(m_target, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
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

        private void OnAttack(DOLEvent e, object sender, EventArgs arguments)
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

            MessageToLiving(ad.Target, string.Format("You're in a Zephyr and can't be attacked!"), eChatType.CT_Spell);
            MessageToLiving(ad.Attacker, string.Format("Your target is in a Zephyr and can't be attacked!"), eChatType.CT_Spell);
        }

        private void ArriveAtTarget(GameNPC zephyr)
        {
            GamePlayer playerTarget = zephyr.TargetObject as GamePlayer;

            if (playerTarget == null || !playerTarget.IsAlive)
                return;

            playerTarget.IsStunned = true;
            playerTarget.DebuffCategory[(int)eProperty.SpellFumbleChance]+=100;
            playerTarget.attackComponent.StopAttack();
            playerTarget.StopCurrentSpellcast();
            playerTarget.MountSteed(zephyr, true);
            GameEventMgr.AddHandler(playerTarget, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
            playerTarget.Out.SendMessage("You are picked up by a forceful zephyr!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            zephyr.StopMoving();

            if (Caster is GamePlayer playerCaster)
            {
                //Calculate random target
                m_loc = GetTargetLoc();
                playerCaster.Out.SendCheckLos(playerCaster, m_npc, new CheckLosResponse(ZephyrCheckLos));
            }
        }

        public void ZephyrCheckLos(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
        {
            if (response is eLosCheckResponse.TRUE)
                m_npc.WalkTo(m_loc, 100);
        }

        public virtual Point3D GetTargetLoc()
        {
            double targetX = m_npc.X + Util.Random(-1500, 1500);
            double targetY = m_npc.Y + Util.Random(-1500, 1500);

            return new Point3D((int)targetX, (int)targetY, m_npc.Z);
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public FZSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Sojourner-9
    [SpellHandler(eSpellType.Phaseshift)]
    public class PhaseshiftHandler : MasterlevelHandling
    {
        private int endurance;

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            endurance = (Caster.MaxEndurance * 50) / 100;

            if (Caster.Endurance < endurance)
            {
                MessageToCaster("You need 50% endurance for this spell!!", eChatType.CT_System);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameEventMgr.AddHandler(Caster, GamePlayerEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
            Caster.Endurance -= endurance;
        }

        private void OnAttack(DOLEvent e, object sender, EventArgs arguments)
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
                player.Out.SendMessage(living.Name + " is Phaseshifted and can't be attacked!", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
            return base.OnEffectExpires(effect, noMessages);
        }

        public override bool HasPositiveEffect
        {
            get
            {
                return false;
            }
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public PhaseshiftHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //no shared timer
    #region Sojourner-10
    [SpellHandler(eSpellType.Groupport)]
    public class Groupport : MasterlevelHandling
    {
        public Groupport(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is GamePlayer && Caster.CurrentRegionID == 51 && ((GamePlayer)Caster).BindRegion == 51)
            {
                if (Caster.CurrentRegionID == 51)
                {
                    MessageToCaster("You can't use this Ability here", eChatType.CT_SpellResisted);
                    return false;
                }
                else
                {
                    MessageToCaster("Bind in another Region to use this Ability", eChatType.CT_SpellResisted);
                    return false;
                }
            }
            return base.CheckBeginCast(selectedTarget);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = Caster as GamePlayer;
            if ((player != null) && (player.Group != null))
            {
                if (player.Group.IsGroupInCombat())
                {
                    player.Out.SendMessage("You can't teleport a group that is in combat!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                player.Out.SendMessage("You are not a part of a group!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
    #endregion
}
