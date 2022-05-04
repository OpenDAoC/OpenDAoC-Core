using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.Language;
using System;
using System.Collections;
using DOL.GS.RealmAbilities;
using DOL.Database;
using DOL.AI.Brain;
using DOL.GS.Spells;
using System.Collections.Generic;

namespace DOL.GS.Effects
{
    public class AtlasOF_VolleyECSEffect : ECSGameAbilityEffect
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public AtlasOF_VolleyECSEffect(ECSGameEffectInitParams initParams)
             : base(initParams)
        {
            EffectType = eEffect.Volley;
            EffectService.RequestStartEffect(this);
        }
        private IPoint3D sol = null;
        private const ushort radiusToCheck = 350;                   //ground target radius
        private int nbShoot = 0;                                    //arrows to shot
        private const int VOLLEY_SHOT_ENDURANCE = 15;               //Endurance
        private GamePlayer m_player;                                // Effect owner
        public override ushort Icon { get { return 4281; } }        //3083,7080,3079(icons)
        public override string Name { get { return "Volley"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;
            m_player = OwnerPlayer;
            if (m_player != null)
            {
                nbShoot = 5;

                base.OnStartEffect();
                m_player.Out.SendMessage("Your " + Name + " is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                m_player.attackComponent.LivingStopAttack();    //stop all attacks
                m_player.StopCurrentSpellcast();                //stop all casts

                GameEventMgr.AddHandler(m_player, GamePlayerEvent.Quit, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(m_player, GameLivingEvent.Moving, new DOLEventHandler(PlayerMoving));
                GameEventMgr.AddHandler(m_player, GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseVolley));
                GameEventMgr.AddHandler(m_player, GamePlayerEvent.TakeDamage, new DOLEventHandler(AttackedByEnemy));
            }
        }
        public override void OnStopEffect()
        {
            GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Quit, new DOLEventHandler(PlayerLeftWorld));
            GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseVolley));
            GameEventMgr.RemoveHandler(m_player, GameLivingEvent.Moving, new DOLEventHandler(PlayerMoving));
            GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.TakeDamage, new DOLEventHandler(AttackedByEnemy));
            base.OnStopEffect();
        }
        public void Cancel(bool playerCancel)
        {           
            EffectService.RequestImmediateCancelEffect(this, playerCancel);
            OnStopEffect();
        }
        protected IList SelectTargets()
        {
            ArrayList list = new ArrayList();

            foreach (GamePlayer VolleePlayerTarget in WorldMgr.GetPlayersCloseToSpot(m_player.CurrentRegionID, m_player.GroundTarget.X, m_player.GroundTarget.Y, m_player.GroundTarget.Z, radiusToCheck))
            {
                if (VolleePlayerTarget != null)
                {
                    if (VolleePlayerTarget.IsAlive && VolleePlayerTarget.Client.Account.PrivLevel == 1 && !list.Contains(VolleePlayerTarget))
                    {
                        if (Util.Chance(50))
                            list.Add(VolleePlayerTarget);//add player to list of potentional targets
                    }
                }
            }
            foreach (GameNPC VolleeNPCTarget in WorldMgr.GetNPCsCloseToSpot(m_player.CurrentRegionID, m_player.GroundTarget.X, m_player.GroundTarget.Y, m_player.GroundTarget.Z, radiusToCheck))
            {
                if (VolleeNPCTarget == null) break;
                if (VolleeNPCTarget is GameSiegeWeapon) continue;
                if (VolleeNPCTarget.ObjectState != GameObject.eObjectState.Active) continue;
                if (!GameServer.ServerRules.IsAllowedToAttack(m_player, VolleeNPCTarget, false)) continue;
                if (Util.Chance(50) && !list.Contains(VolleeNPCTarget))
                    list.Add(VolleeNPCTarget);//add mob to list of potentional targets
            }
            return list;
        }
        public void decNbShoot()
        {
            nbShoot -= 1;
            if (nbShoot == 0)
            {
                m_player.Out.SendMessage("Your " + Name + " is finished!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Cancel(false);
                AtlasOF_Volley volle = m_player.GetAbility<AtlasOF_Volley>();
                m_player.DisableSkill(volle, AtlasOF_Volley.DISABLE_DURATION);
            }
        }
        public void LaunchVolley(GamePlayer player, int slot, int type)
        {
            InventoryItem ammo = player.rangeAttackComponent.RangeAttackAmmo;
            sol = new Point3D(player.GroundTarget.X, player.GroundTarget.Y, player.GroundTarget.Z);
            double attackrangeMin = player.AttackRange * 0.66;//minimum attack range
            double attackrangeMax = player.AttackRange / 0.66;//maximum attack range

            m_player.attackComponent.LivingStopAttack();
            m_player.StopCurrentSpellcast();
            if (ammo == null)
            {
                player.Out.SendMessage("You need to be equipped with a arrows for use this ability!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }
            if (player.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.NoRangedWeapons"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }
            // Check if selected ammo is compatible for ranged attack
            if (!player.rangeAttackComponent.CheckRangedAmmoCompatibilityWithActiveWeapon())
            {
                player.Out.SendMessage("You need to be equipped with a arrows for use this ability!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                return;
            }
            if (sol == null)
            {
                player.Out.SendMessage("You need to have a ground target for use this ability!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }
            if (player.IsWithinRadius(player.GroundTarget, (int)attackrangeMin))
            {
                player.Out.SendMessage("You ground target is too close to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (!player.IsWithinRadius(player.GroundTarget, (int)attackrangeMax))
            {
                player.Out.SendMessage("You ground target is too far away to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            int speedtodisplay = m_player.AttackSpeed(m_player.AttackWeapon) / 100;
            m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client.Account.Language, "GamePlayer.StartAttack.YouPrepare.Volley", Name, speedtodisplay / 10, speedtodisplay % 10), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            foreach (GamePlayer playerS in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                int weaponspeed = m_player.AttackSpeed(m_player.AttackWeapon);
                byte prepareTime = (byte)(weaponspeed / 100);
                m_player.Out.SendSpellCastAnimation(m_player, 7454, prepareTime);
            }
            if (CanLaunch == false)
            {
                new ECSGameTimer(OwnerPlayer, new ECSGameTimer.ECSTimerCallback(MakeAnimation), 2500);
                CanLaunch = true;
            }
            return;
        }
        private void DamageTarget(GameLiving target, GamePlayer archer)
        {
            InventoryItem attackWeapon = archer.AttackWeapon;
            eDamageType damagetype = archer.attackComponent.AttackDamageType(attackWeapon);
            int modifier = archer.RealmLevel;
            int baseDamage = (archer.Level + modifier) * 3;
            double damageResisted = baseDamage * target.GetResist(damagetype) * -0.01;

            GamePlayer targetPlayer = target as GamePlayer;
            if (targetPlayer != null)
            {
                if (targetPlayer.IsStealthed)
                    targetPlayer.Stealth(false);
            }

            AttackData ad = new AttackData();
            ad.AttackResult = eAttackResult.HitUnstyled;
            ad.Attacker = m_player;
            ad.Target = target;
            ad.DamageType = damagetype;
            ad.AttackType = AttackData.eAttackType.Ranged;
            ad.Damage = (int)(baseDamage + damageResisted);
            ad.Modifier = (int)damageResisted;
            ad.CriticalDamage = archer.attackComponent.AttackCriticalChance(attackWeapon);
            //ad.AnimationId = 10;
            //target.OnAttackedByEnemy(ad);
            target.TakeDamage(ad);
            archer.DealDamage(ad);
            foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);//being attacked animation
            }
            if (m_player != null)
            {
                m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client.Account.Language, "Effects.VolleyEffect.MBHitsExtraDamage", target.GetName(0, false), ad.Damage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                GamePlayer playerTarget = target as GamePlayer;
                if (playerTarget != null)
                {
                    playerTarget.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client.Account.Language, "Effects.VolleyEffect.XMBExtraDamageToYou", playerTarget.GetName(0, false), ad.Damage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                }
            }
        }
        private void ShowVolleyEffect()//create volley mob on player groundtarget location
        {
            VolleyMob mob = new VolleyMob();
            mob.X = m_player.GroundTarget.X;
            mob.Y = m_player.GroundTarget.Y;
            mob.Z = m_player.GroundTarget.Z;
            mob.Level = m_player.Level;
            mob.CurrentRegion = m_player.CurrentRegion;
            mob.RespawnInterval = -1;
            mob.AddToWorld();
        }
        private bool CanLaunch = false;
        private void PlayerUseVolley(DOLEvent e, object sender, EventArgs args)
        {
            UseSlotEventArgs useArgs = args as UseSlotEventArgs;
            GamePlayer player = sender as GamePlayer;
            if (player == null) return;
            int slot = useArgs.Slot;
            int type = useArgs.Type;
            if (CanLaunch == false)
                LaunchVolley(player, slot, type);
            else
            {
                player.Out.SendMessage("You can't fire your arrow yet!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }
        }
        private int MakeAnimation(ECSGameTimer timer)
        {
            var player = m_player;
            decNbShoot();

            InventoryItem ammo = player.rangeAttackComponent.RangeAttackAmmo;
            InventoryItem attackWeapon = player.AttackWeapon;
            eDamageType damagetype = player.attackComponent.AttackDamageType(attackWeapon);
            int speed = player.AttackSpeed(attackWeapon);
            byte attackSpeed = (byte)(speed / 1000);
            player.Out.SendMessage("You launch a " + Name + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            AtlasOF_ArrowSalvaging abArrowSalv = player.GetAbility<AtlasOF_ArrowSalvaging>();
            Boolean remove = true;
            if (abArrowSalv != null)
            {
                int rnd;
                Random r = new Random();
                rnd = r.Next(0, 100);
                if ((rnd >= 0) && (rnd <= abArrowSalv.Amount))
                {
                    remove = false;
                }
            }

            if (remove)
            {
                player.Inventory.RemoveCountFromStack(ammo, 1);
            }
            else
            {
                player.Out.SendMessage("Your ability " + abArrowSalv.Name + " save you an arrow", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }
            player.Endurance -= VOLLEY_SHOT_ENDURANCE;

            if (player.IsStealthed)
                player.Stealth(false);

            //GetTarget 
            IList targets = SelectTargets();
            foreach (GameLiving target in targets)
            {
                player.Out.SendMessage("Target: " + target.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            ShowVolleyEffect();

            if (targets.Count > 0)
            {
                foreach (GameLiving livingaffected in targets)
                {
                    if (livingaffected != null)
                    {
                        DamageTarget(livingaffected, player);
                        if (livingaffected is GamePlayer)
                        {
                            if (livingaffected.IsStealthed)
                                ((GamePlayer)livingaffected).Stealth(false);
                        }
                    }
                    else
                    {
                        player.Out.SendMessage("You do not touch any target!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }

            if (nbShoot >= 1)
            {
                player.Out.SendMessage("You have " + nbShoot + " arrows to be drawn!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            targets.Clear();
            CanLaunch = false;
            return 0;
        }
        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        private void PlayerLeftWorld(DOLEvent e, object sender, EventArgs args)
        {
            Cancel(false);
        }
        /// <summary>
        /// Called when a player move
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        private void PlayerMoving(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = (GamePlayer)sender;
            if (player == null) return;
            if (e == GamePlayerEvent.Moving)
            {
                player.Out.SendAttackMode(false);
                Cancel(false);
                AtlasOF_Volley volle = m_player.GetAbility<AtlasOF_Volley>();
                m_player.DisableSkill(volle, AtlasOF_Volley.DISABLE_DURATION);
                player.Out.SendMessage("You move and interrupt your " + Name + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                foreach (GamePlayer i_player in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    i_player.Out.SendInterruptAnimation(player);
                }
            }
        }
        private void AttackedByEnemy(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer player = sender as GamePlayer;
            if (player == null) return; 
            if (e == GamePlayerEvent.TakeDamage)
            {
                Cancel(false);
                AtlasOF_Volley volle = m_player.GetAbility<AtlasOF_Volley>();
                m_player.DisableSkill(volle, AtlasOF_Volley.DISABLE_DURATION);
                player.Out.SendMessage("You have been attacked and your " + Name + " is interrupted!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                foreach (GamePlayer i_player in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    i_player.Out.SendInterruptAnimation(player);
                }
            }
        }
    }
}
/// <summary>
/// //////////////////////////////Volley mob to show player actuall volley location hit and nice effect for ability
/// </summary>
#region Volley Effect Mob
namespace DOL.GS
{
    public class VolleyMob : GameNPC
    {
        public VolleyMob() : base()
        {
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool AddToWorld()
        {
            Model = 665;
            Size = 80;
            MaxSpeedBase = 0;
            Name = "Volley Effect";

            Flags ^= eFlags.PEACE;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.CANTTARGET;
            RespawnInterval = -1;

            StandardMobBrain volley = new StandardMobBrain();
            SetOwnBrain(volley);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 100);
            }
            return success;
        }
        private protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player != null)
                        player.Out.SendSpellEffectAnimation(this, this, 7454, 0, false, 0x01);
                }
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveVolleyMob), 3000);
            }
            return 0;
        }
        private protected int RemoveVolleyMob(ECSGameTimer timer)
        {
            if (IsAlive)
                RemoveFromWorld();
            return 0;
        }
    }
}
#endregion