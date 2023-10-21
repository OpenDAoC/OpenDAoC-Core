using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //no shared timer

    [SpellHandler("Zephyr")]
    public class ForcefulZephyrSpell : MasterLevelSpellHandling
    {
        protected EcsGameTimer m_expireTimer;
        protected GameNpc m_npc;
        protected GamePlayer m_target;
        protected IPoint3D m_loc;

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
            if (!target.IsAlive || target.ObjectState != GameObject.eObjectState.Active)
                return;

            GameNpc npc = new()
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

            npc.Flags |= ENpcFlags.PEACE;
            npc.Flags |= ENpcFlags.DONTSHOWNAME;
            npc.Flags |= ENpcFlags.CANTTARGET;
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
            m_expireTimer = new EcsGameTimer(m_npc, new EcsGameTimer.EcsTimerCallback(ExpiredCallback), 10000);
        }

        protected virtual int ExpiredCallback(EcsGameTimer callingTimer)
        {
            m_target.IsStunned = false;
            m_target.DismountSteed(true);
            m_target.DebuffCategory[(int)EProperty.SpellFumbleChance] -= 100;
            GameEventMgr.RemoveHandler(m_target, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
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
            MessageToLiving(ad.Attacker, string.Format("Your target is in a Zephyr and can't be attacked!"),
                EChatType.CT_Spell);
        }

        private void ArriveAtTarget(GameNpc zephyr)
        {
            GamePlayer playerTarget = zephyr.TargetObject as GamePlayer;

            if (playerTarget == null || !playerTarget.IsAlive)
                return;

            playerTarget.IsStunned = true;
            playerTarget.DebuffCategory[(int)EProperty.SpellFumbleChance] += 100;
            playerTarget.attackComponent.StopAttack();
            playerTarget.StopCurrentSpellcast();
            playerTarget.MountSteed(zephyr, true);
            GameEventMgr.AddHandler(playerTarget, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
            playerTarget.Out.SendMessage("You are picked up by a forceful zephyr!", EChatType.CT_System,
                EChatLoc.CL_SystemWindow);
            zephyr.StopMoving();

            if (Caster is GamePlayer playerCaster)
            {
                //Calculate random target
                m_loc = GetTargetLoc();
                playerCaster.Out.SendCheckLOS(playerCaster, m_npc, new CheckLOSResponse(ZephyrCheckLOS));
            }
        }

        public void ZephyrCheckLOS(GamePlayer player, ushort response, ushort targetOID)
        {
            if (targetOID == 0)
                return;

            if ((response & 0x100) == 0x100)
                m_npc.WalkTo(m_loc, 100);
        }

        public virtual IPoint3D GetTargetLoc()
        {
            double targetX = m_npc.X + Util.Random(-1500, 1500);
            double targetY = m_npc.Y + Util.Random(-1500, 1500);

            return new Point3D((int)targetX, (int)targetY, m_npc.Z);
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public ForcefulZephyrSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}