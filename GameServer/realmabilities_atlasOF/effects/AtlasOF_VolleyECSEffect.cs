using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.Language;

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
        private const ushort radiusToCheck = 350;                   //ground target radius
        private int nbShoot = 0;                                    //arrows to shot
        private const int VOLLEY_SHOT_ENDURANCE = 15;               //Endurance
        private GamePlayer m_player;                                // Effect owner
        public override ushort Icon { get { return 4281; } }        //3083,7080,3079(icons)
        public override string Name { get { return "Volley"; } }
        public override bool HasPositiveEffect { get { return true; } }
        private bool m_isreadytofire = false;
        private bool IsReadyToFire
        {
            get { return m_isreadytofire; }
            set { m_isreadytofire = value; }
        }
        private bool IsReadyToFireAgain;
        private bool BowPreparation = false;
        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            m_player = OwnerPlayer;
            nbShoot = 5;//Max arrows we set here
            IsReadyToFireAgain = false;//make sure it's false at beginning

            if (m_player.IsStealthed)//cancel stealth if player use Volley
                m_player.Stealth(false);

            m_player.attackComponent.StopAttack();
            m_player.StopCurrentSpellcast();

            base.OnStartEffect();
            #region Cancel running timers
            var readyTimerContinue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerContinue");
            if (readyTimerContinue != null)
            {
                readyTimerContinue.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimerContinue");
            }
            var readyTimerAgain = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerAgain");
            if (readyTimerAgain != null)
            {
                readyTimerAgain.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimerAgain");
            }
            #endregion
            InventoryItem attackWeapon = m_player.ActiveWeapon;//define user weapon
            byte HoldAttack = 255;//0x1E;//30 seconds
            int speed = attackWeapon.SPD_ABS * 100;//weapon speed used to timer
            ECSGameTimer readyTimer = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(ReadyToFire), speed);//timer to prepare bow
            ECSGameTimer tiredTimer = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(TooTired1stShoot), 32500);//timer too tired
            m_player.TempProperties.setProperty("volley_readyTimer", readyTimer);
            m_player.TempProperties.setProperty("volley_tiredTimer", tiredTimer);              
            m_player.Out.SendMessage("You prepare to unleash a volley of arrows!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            m_player.Out.SendMessage(String.Format("You prepare to fire. ({0}s to fire)", (double)speed/1000), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            int model = (m_player.ActiveWeapon == null ? 0 : m_player.ActiveWeapon.Model);

            if (!BowPreparation)
            {
                foreach (GamePlayer player in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendCombatAnimation(m_player, null, (ushort)model, 0x00, player.Out.BowPrepare, HoldAttack, 0x00, m_player.HealthPercent);

                BowPreparation = true;
            }
            
            GameEventMgr.AddHandler(m_player, GamePlayerEvent.Quit, new DOLEventHandler(OnPlayerLeftWorld));
            GameEventMgr.AddHandler(m_player, GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseVolley));
        }
        public override void OnStopEffect()
        {
            #region Stop timers properties
            var readyTimer = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimer");
            if (readyTimer != null)
            {
                readyTimer.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimer");
            }
            var tiredTimer = m_player.TempProperties.getProperty<ECSGameTimer>("volley_tiredTimer");
            if (tiredTimer != null)
            {
                tiredTimer.Stop();
                m_player.TempProperties.removeProperty("volley_tiredTimer");
            }
            /////////////////////////////Prepare Bow Again
            var readyTimerAgain = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerAgain");
            if (readyTimerAgain != null)
            {
                readyTimerAgain.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimerAgain");
            }
            var shot1Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot1Again");
            if (shot1Again != null)
            {
                shot1Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot1Again");
            }
            var shot2Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot2Again");
            if (shot2Again != null)
            {
                shot2Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot2Again");
            }
            var shot3Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot3Again");
            if (shot3Again != null)
            {
                shot3Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot3Again");
            }
            var shot4Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot4Again");
            if (shot4Again != null)
            {
                shot4Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot4Again");
            }
            var shot5Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot5Again");
            if (shot5Again != null)
            {
                shot5Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot5Again");
            }
            /////////////////////////Shot continue
            var readyTimerContinue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerContinue");
            if (readyTimerContinue != null)
            {
                readyTimerContinue.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimerContinue");
            }
            var shot2Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot2Continue");
            if (shot2Continue != null)
            {
                shot2Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot2Continue");
            }
            var shot3Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot3Continue");
            if (shot3Continue != null)
            {
                shot3Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot3Continue");
            }
            var shot4Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot4Continue");
            if (shot4Continue != null)
            {
                shot4Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot4Continue");
            }
            var shot5Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot5Continue");
            if (shot5Continue != null)
            {
                shot5Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot5Continue");
            }
            #endregion

            GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.Quit, new DOLEventHandler(OnPlayerLeftWorld));
            GameEventMgr.RemoveHandler(m_player, GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseVolley));
            base.OnStopEffect();
        }

        public void Cancel(bool playerCancel)
        {
            EffectService.RequestImmediateCancelEffect(this, playerCancel);
        }

        private void PrepareBowAgain()//Bow prepare again incase player hold too long volley
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            InventoryItem attackWeapon = m_player.ActiveWeapon;
            byte HoldAttack = 0x1E;//30 seconds
            int speed = attackWeapon.SPD_ABS * 100;//weapon speed used to timer
            #region Properties and Timers for each shot
            if (IsReadyToFireAgain)
                return;
            ECSGameTimer readyTimerAgain = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(ReadyToFire), speed);//timer to prepare bow
            m_player.TempProperties.setProperty("volley_readyTimerAgain", readyTimerAgain);
            if (nbShoot == 5 && volley != null)
            {
                ECSGameTimer shot1 = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(TooTired1stShoot), 32500);
                m_player.TempProperties.setProperty("volley_shot1Again", shot1);
            }
            if (nbShoot == 4 && volley != null)
            {
                ECSGameTimer shot2 = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(TooTired2ndShoot), 32500);
                m_player.TempProperties.setProperty("volley_shot2Again", shot2);
            }
            if (nbShoot == 3 && volley != null)
            {
                ECSGameTimer shot3 = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(TooTired3thShoot), 32500);
                m_player.TempProperties.setProperty("volley_shot3Again", shot3);
            }
            if (nbShoot == 2 && volley != null)
            {
                ECSGameTimer shot4 = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(TooTired4thShoot), 32500);
                m_player.TempProperties.setProperty("volley_shot4Again", shot4);
            }
            if (nbShoot == 1 && volley != null)
            {
                ECSGameTimer shot5 = new ECSGameTimer(m_player, new ECSGameTimer.ECSTimerCallback(TooTired5thShoot), 32500);
                m_player.TempProperties.setProperty("volley_shot5Again", shot5);
            }
            #endregion
            m_player.Out.SendMessage(String.Format("You prepare to fire. ({0}s to fire)", (double)speed / 1000), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            int model = (m_player.ActiveWeapon == null ? 0 : m_player.ActiveWeapon.Model);

            //m_player.attackComponent.LivingStopAttack();    //stop all attacks
            //m_player.StopCurrentSpellcast();                //stop all casts

            foreach (GamePlayer playerInRadius in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                playerInRadius.Out.SendCombatAnimation(m_player, null, (ushort)model, 0x00, playerInRadius.Out.BowPrepare, HoldAttack, 0x00, 0x00);//bow animation
            }
        }
        #region Timers for each volley shoot && ReadyToFire
        private protected bool AbortShot = false;
        private protected int TooTired1stShoot(ECSGameTimer timer)//1st shot
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            var tiredTimer = m_player.TempProperties.getProperty<ECSGameTimer>("volley_tiredTimer");
            var shot1Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot1Again");
            if (volley != null && nbShoot == 5 && m_player.IsAlive && shot1Again != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                BowPreparation = false;
                shot1Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot1Again");
            }
            if (volley != null && nbShoot == 5 && m_player.IsAlive && tiredTimer != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                BowPreparation = false;
                tiredTimer.Stop();
                m_player.TempProperties.removeProperty("volley_tiredTimer");
            }
            return 0;
        }
        private protected int TooTired2ndShoot(ECSGameTimer timer)//2nd shot
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            var shot2Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot2Again");
            if (volley != null && nbShoot == 4 && m_player.IsAlive && shot2Again != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot2Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot2Again");
            }
            var shot2Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot2Continue");
            if (volley != null && nbShoot == 4 && m_player.IsAlive && shot2Continue != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot2Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot2Continue"); 
            }
            return 0;
        }
        private protected int TooTired3thShoot(ECSGameTimer timer)//3th shot
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            var shot3Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot3Again");
            if (volley != null && nbShoot == 3 && m_player.IsAlive && shot3Again != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot3Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot3Again");
            }
            var shot3Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot3Continue");
            if (volley != null && nbShoot == 3 && m_player.IsAlive && shot3Continue != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot3Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot3Continue");
            }
            return 0;
        }
        private protected int TooTired4thShoot(ECSGameTimer timer)//4th shot
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            var shot4Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot4Again");
            if (volley != null && nbShoot == 2 && m_player.IsAlive && shot4Again != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot4Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot4Again");
            }
            var shot4Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot4Continue");
            if (volley != null && nbShoot == 2 && m_player.IsAlive && shot4Continue != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot4Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot4Continue");
            }
            return 0;
        }
        private protected int TooTired5thShoot(ECSGameTimer timer)//5th shot
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            var shot5Again = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot5Again");
            if (volley != null && nbShoot == 1 && m_player.IsAlive && shot5Again != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot5Again.Stop();
                m_player.TempProperties.removeProperty("volley_shot5Again");
            }
            var shot5Continue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_shot5Continue");
            if (volley != null && nbShoot == 1 && m_player.IsAlive && shot5Continue != null)
            {
                m_player.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //Cancel(false);
                AbortShot = true;
                shot5Continue.Stop();
                m_player.TempProperties.removeProperty("volley_shot5Continue");
            }
            return 0;
        }
        private int ReadyToFire(ECSGameTimer timer)//Can shot now, is ready to fire
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            var readyTimerAgain = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerAgain");
            var readyTimer = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimer");
            var readyTimerContinue = m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerContinue");          
            if (volley != null && m_player.IsAlive && readyTimerAgain != null)
            {
                m_player.Out.SendMessage("You are ready to shoot!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                IsReadyToFireAgain = false;
                IsReadyToFire = true;
                AbortShot = false;
                readyTimerAgain.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimerAgain");
            }
            
            if (volley != null && m_player.IsAlive && readyTimer != null)
            {
                m_player.Out.SendMessage("You are ready to shoot!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                IsReadyToFire = true;
                BowPreparation = false;               
                readyTimer.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimer");
            }
            
            if (volley != null && m_player.IsAlive && readyTimerContinue != null)
            {
                m_player.Out.SendMessage("You are ready to shoot!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                IsReadyToFire = true;
                readyTimerContinue.Stop();
                m_player.TempProperties.removeProperty("volley_readyTimerContinue");
            }
            //m_player.TempProperties.setProperty("volley_IsReadyToFire", IsReadyToFire);
            return 0;
        }
        #endregion

        #region IList SelectTarget / decNbShoot
        protected IList SelectTargets()
        {
            ArrayList list = new ArrayList();

            foreach (GamePlayer VolleePlayerTarget in WorldMgr.GetPlayersCloseToSpot(m_player.CurrentRegionID, m_player.GroundTarget.X, m_player.GroundTarget.Y, m_player.GroundTarget.Z, radiusToCheck))
            {
                if (VolleePlayerTarget != null)
                {
                    if (VolleePlayerTarget.IsAlive && VolleePlayerTarget.Client.Account.PrivLevel == 1 && !list.Contains(VolleePlayerTarget) && (VolleePlayerTarget.Realm != m_player.Realm || m_player.DuelTarget == VolleePlayerTarget))
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
                m_player.Out.SendMessage("Your volley is finished!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Cancel(false);
                AtlasOF_Volley volley = m_player.GetAbility<AtlasOF_Volley>();
                m_player.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            }
        }
        #endregion

        #region LaunchVolley
        public void LaunchVolley(GamePlayer player)
        {
            if (player.IsBeingInterrupted)
            {
                Cancel(false);
                return;
            }

            if (player.rangeAttackComponent.UpdateAmmo(player.ActiveWeapon) == null)
            {
                player.Out.SendMessage("You need arrows to use Volley!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.CriticalShot.NoRangedWeapons"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!player.rangeAttackComponent.IsAmmoCompatible)
            {
                player.Out.SendMessage("You need arrows to use Volley!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.GroundTarget == null)
            {
                player.Out.SendMessage("You must have a ground target to use Volley!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            if (AbortShot)
            {
                if (!IsReadyToFireAgain)
                {
                    PrepareBowAgain();
                    IsReadyToFireAgain = true;
                }

                return;
            }

            int model = m_player.ActiveWeapon == null ? 0 : m_player.ActiveWeapon.Model;

            foreach (GamePlayer playerInRadius in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (playerInRadius == null) 
                    continue;

                playerInRadius.Out.SendCombatAnimation(m_player, null, (ushort)model, 0x00, playerInRadius.Out.BowShoot, 0, 0x00, 0x00);
            }

            IsReadyToFire = false;
            int ticksToTarget = m_player.GetDistanceTo(m_player.GroundTarget) * 1000 / RangeAttackComponent.PROJECTILE_FLIGHT_SPEED;
            new ECSGameTimer(OwnerPlayer, new ECSGameTimer.ECSTimerCallback(MakeAnimation), ticksToTarget);
            player.Out.SendMessage("Your shot arcs into the sky!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.Endurance -= VOLLEY_SHOT_ENDURANCE;
            int arrowRecoveryChance = player.GetModified(eProperty.ArrowRecovery);

            if (arrowRecoveryChance == 0 || Util.Chance(100 - arrowRecoveryChance))
                player.Inventory.RemoveCountFromStack(player.rangeAttackComponent.Ammo, 1);

            if (player.IsStealthed)
                player.Stealth(false);

            decNbShoot();

            if (m_player.TempProperties.getProperty<ECSGameTimer>("volley_readyTimerAgain") == null)
            {
                ECSGameTimer readyTimerContinue = new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(ReadyToFire), 1500);
                m_player.TempProperties.setProperty("volley_readyTimerContinue", readyTimerContinue);

                if (nbShoot == 4)
                {
                    ECSGameTimer shot2Continue = new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(TooTired2ndShoot), 32500);
                    m_player.TempProperties.setProperty("volley_shot2Continue", shot2Continue);
                }
                else if (nbShoot == 3)
                {
                    ECSGameTimer shot3Continue = new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(TooTired3thShoot), 32500);
                    m_player.TempProperties.setProperty("volley_shot3Continue", shot3Continue);
                }
                else if (nbShoot == 2)
                {
                    ECSGameTimer shot4Continue = new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(TooTired4thShoot), 32500);
                    m_player.TempProperties.setProperty("volley_shot4Continue", shot4Continue);
                }
                else if (nbShoot == 1)
                {
                    ECSGameTimer shot5Continue = new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(TooTired5thShoot), 32500);
                    m_player.TempProperties.setProperty("volley_shot5Continue", shot5Continue);
                }
            }

            if (nbShoot >= 1)
            {
                player.Out.SendMessage("You have " + nbShoot + " arrows to be drawn!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                foreach (GamePlayer playerInRadius in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    playerInRadius.Out.SendCombatAnimation(player, null, (ushort)model, 0x00, playerInRadius.Out.BowPrepare, 0x1E, 0x00, 0x00);
            }
        }

        #endregion
        #region DamageTarget
        private void DamageTarget(GameLiving target, GamePlayer archer)
        {
            InventoryItem attackWeapon = archer.ActiveWeapon;
            eDamageType damagetype = archer.attackComponent.AttackDamageType(attackWeapon);
            int modifier = archer.RealmLevel;
            double effectivenes = 1;
            double weaponspeed = attackWeapon.SPD_ABS * 0.1;
            double ClampedDamage = 1.2 + archer.Level * 0.3;
            double WeaponBonus2H = 1.1 + (0.005 * archer.WeaponSpecLevel(attackWeapon));
            double SlowWeaponBonus = 1 + ((weaponspeed - 2) * 0.03);
            double meleeRelicBonus = 1.0 + RelicMgr.GetRelicBonusModifier(archer.Realm, eRelicType.Strength);
            double WeaponDPS = attackWeapon.DPS_AF * 0.1;
            double TargetAF = 1;
            if (target is GamePlayer)
            {
                TargetAF = 20;//get player AF
                TargetAF = (TargetAF + target.GetArmorAF(eArmorSlot.TORSO))/
                           (1 - target.GetArmorAbsorb(eArmorSlot.TORSO));
                if (TargetAF <= 0) TargetAF = 0.1;
            }
                
            else
            {
                //if (target.GetModified(eProperty.ArmorFactor) > 0)
                //   TargetAF = target.GetModified(eProperty.ArmorFactor) * 6;//calculate AF for NPc
                //else
                // {
                //if (target.GetModified(eProperty.ArmorFactor) == 0)
                //{
                    if (target.Level == 0)
                        TargetAF = 1 / 0.08;//set af if npc somehow got AF = 0
                    else
                        TargetAF = target.Level / 0.08;
                //}
               // }                   
            }

            double TargetABS;
            double AcherWeaponSkill = archer.GetWeaponSkill(attackWeapon);
            double baseDamage = WeaponDPS * (AcherWeaponSkill / TargetAF) * meleeRelicBonus  * SlowWeaponBonus * WeaponBonus2H * weaponspeed;//calculate dmg
            
            // DEBUG
            // Console.WriteLine($"base dmg {baseDamage} dps {WeaponDPS} ws {AcherWeaponSkill} af {TargetAF} divvy {AcherWeaponSkill/TargetAF} slowwep {SlowWeaponBonus} 2hbon {WeaponBonus2H} speed {weaponspeed}");

            switch ((archer.rangeAttackComponent.Ammo?.SPD_ABS) & 0x3)//switch dmg based of arrow type
            {
                case 0:
                    baseDamage *= 0.85;
                    break; //Blunt  (light) -15%
                //case 1:
                //  damage *= 1;
                //  break; //Bodkin (medium) 0%
                case 2:
                    baseDamage *= 1.15;
                    break; //doesn't exist on live
                case 3:
                    baseDamage *= 1.25;
                    break; //Broadhead (X-heavy) +25%
            }

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
            ad.Weapon= attackWeapon;
            ad.DamageType = damagetype;
            ad.AttackType = AttackData.eAttackType.Ranged;                   
            ad.CriticalDamage = archer.attackComponent.GetMeleeCriticalDamage(ad, attackWeapon);
            ad.Attacker.GetModified(eProperty.CriticalArcheryHitChance);
            ad.CausesCombat = true;

            if (target is GamePlayer)
                ad.ArmorHitLocation = ((GamePlayer)target).CalculateArmorHitLocation(ad);

            InventoryItem armor = null;
            if (target.Inventory != null)
                armor = target.Inventory.GetItem((eInventorySlot)ad.ArmorHitLocation);

            if (target is GamePlayer)
            {
                TargetABS = 1.0 - Math.Min(0.85, target.GetArmorAbsorb(ad.ArmorHitLocation));//set player abs
                baseDamage *= TargetABS;
            }
            else
            {
                if (target.Level == 0)
                    TargetABS = 1.0 - (1 * 0.005);//mob with lvl 0 
                else
                    TargetABS = 1.0 - (target.Level * 0.005);//calculate mob abs based of it's lvl

                baseDamage *= TargetABS;
            }

            if (baseDamage > 550)
                baseDamage = 550;//cap dmg for volley just incase we see weird numbers
            if (baseDamage < 50)
                baseDamage = 50;//minimum volley damage;

            double vulnerable = 1.10;
            double resistant = 0.90;
            double neutral = 1.0;
            //Here we calculate damage based of armor resist rules
            if(target is GamePlayer && armor != null && target != null && target.IsAlive)
            {
                switch(target.Realm)
                {
                    #region Albion armor resists
                    case eRealm.Albion:
                        {
                            switch (damagetype)
                            {
                                case eDamageType.Crush:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Studded: baseDamage *= resistant; break;
                                            case (int)eObjectType.Chain: baseDamage *= resistant; break;
                                            case (int)eObjectType.Plate: baseDamage *= vulnerable; break;
                                        }
                                    }
                                    break;
                                case eDamageType.Slash:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= neutral; break;
                                            case (int)eObjectType.Studded: baseDamage *= neutral; break;
                                            case (int)eObjectType.Chain: baseDamage *= neutral; break;
                                            case (int)eObjectType.Plate: baseDamage *= neutral; break;
                                        }
                                    }
                                    break;
                                case eDamageType.Thrust:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= resistant; break;
                                            case (int)eObjectType.Studded: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Chain: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Plate: baseDamage *= resistant; break;
                                        }
                                    }
                                    break;
                            }
                        }break;
                    #endregion
                    #region Midgard armor resists
                    case eRealm.Midgard:
                        {
                            switch(damagetype)
                            {
                                case eDamageType.Crush:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= neutral; break;
                                            case (int)eObjectType.Studded: baseDamage *= neutral; break;
                                            case (int)eObjectType.Chain: baseDamage *= neutral; break;
                                        }
                                    }
                                    break;
                                case eDamageType.Slash:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Studded: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Chain: baseDamage *= resistant; break;
                                        }
                                    }
                                    break;
                                case eDamageType.Thrust:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= resistant; break;
                                            case (int)eObjectType.Studded: baseDamage *= resistant; break;
                                            case (int)eObjectType.Chain: baseDamage *= vulnerable; break;
                                        }
                                    }
                                    break;
                            }
                        }break;
                    #endregion
                    #region Hibernia armor resists
                    case eRealm.Hibernia:
                        {
                            switch(damagetype)
                            {
                                case eDamageType.Crush:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Reinforced: baseDamage *= vulnerable; break;
                                            case (int)eObjectType.Scale: baseDamage *= resistant; break;
                                        }
                                    }
                                    break;
                                case eDamageType.Slash:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= resistant; break;
                                            case (int)eObjectType.Reinforced: baseDamage *= resistant; break;
                                            case (int)eObjectType.Scale: baseDamage *= vulnerable; break;
                                        }
                                    }
                                    break;
                                case eDamageType.Thrust:
                                    {
                                        switch (armor.Object_Type)
                                        {
                                            case (int)eObjectType.Cloth: baseDamage *= neutral; break;
                                            case (int)eObjectType.Leather: baseDamage *= neutral; break;
                                            case (int)eObjectType.Reinforced: baseDamage *= neutral; break;
                                            case (int)eObjectType.Scale: baseDamage *= neutral; break;
                                        }
                                    }
                                    break;
                            }
                        }break;
                    #endregion
                }
            }
            if (target is GamePlayer)
                baseDamage += (int)(baseDamage * (target.GetResist(damagetype) + SkillBase.GetArmorResist(armor, damagetype)) * -0.007);//calculate dmg based on armor resists
            else
                baseDamage += (int)(baseDamage * (target.GetResist(damagetype) * -0.009));

            if(target is GamePlayer)
                effectivenes *= 1.06;
            if (target is GameNPC)
                effectivenes *= 1.06;
            if (target is Keeps.GameKeepGuard)
                effectivenes *= 1.06;
            if (target is DPSDummy)
                Effectiveness *= 1.06;
            ad.Damage = (int)((baseDamage + ad.CriticalDamage) * effectivenes);

            if (ad.Damage > 550)
                ad.Damage = 550;//cap dmg for volley just incase we see weird numbers
            if (ad.Damage < 50)
                ad.Damage = 50;//minimum volley damage;

            // DEBUG
            //archer.Out.SendMessage("weaponspeed = " + weaponspeed +
            //     "WeaponDPS = " + WeaponDPS +
            //     " ad.Modifier = " + ((int)(baseDamage * (target.GetResist(damagetype) + SkillBase.GetArmorResist(armor, damagetype)) * -0.007)) +
            //     " TargetABS = " + TargetABS +
            //     " ArcherSpec = "+ archer.WeaponSpecLevel(attackWeapon) +
            //     " TargetAF = " + TargetAF +
            //     " DisplayedWeaponSkill = " + AcherWeaponSkill +
            //     " meleeRelicBonus = " + meleeRelicBonus +
            //     " WeaponBonus2H = " + WeaponBonus2H +
            //     " SlowWeaponBonus = "+ SlowWeaponBonus+
            //     " ArmorHitLocation = " + ad.ArmorHitLocation +
            //     " DamageResistReduct = "+ (baseDamage * target.GetResist(damagetype) * 0.01), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        
            foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);//being attacked animation
            }
            if (m_player != null)
            {
                m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client.Account.Language, "Effects.VolleyEffect.MBHitsExtraDamage", target.GetName(0, false), ad.Damage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                if(ad.CriticalDamage > 0)
                    m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client.Account.Language, "Effects.VolleyEffect.MBHitsExtraDamageCrit", target.GetName(0, false), ad.CriticalDamage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                GamePlayer playerTarget = target as GamePlayer;
                if (playerTarget != null)
                {
                    playerTarget.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client.Account.Language, "Effects.VolleyEffect.XMBExtraDamageToYou", archer.GetName(0, false),Convert.ToString(ad.ArmorHitLocation).ToLower(), ad.Damage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    if(ad.CriticalDamage > 0)
                        playerTarget.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client.Account.Language, "Effects.VolleyEffect.XMBExtraDamageToYouCrit", archer.GetName(0, false), ad.CriticalDamage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                }
            }
            archer.DealDamage(ad);
            if (target is GamePlayer && target != null)//combat timer and interrupt for target
            {
                target.LastAttackTickPvP = GameLoop.GameLoopTime;
                target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                target.StartInterruptTimer(ServerProperties.Properties.SPELL_INTERRUPT_DURATION, ad.AttackType, ad.Attacker);
            }
            if (archer is GamePlayer && archer != null)//combat timer for archer
            {
                archer.LastAttackTickPvP = GameLoop.GameLoopTime;
                archer.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
            }
            if(ad.Damage > 0)
            {
                if (ad.Target.effectListComponent.Effects.ContainsKey(eEffect.Mez)) //cancel mezz 
                {
                    var effect = EffectListService.GetEffectOnTarget(ad.Target, eEffect.Mez);

                    if (effect != null)
                        EffectService.RequestImmediateCancelEffect(effect);
                }
                if (ad.Target.effectListComponent.Effects.ContainsKey(eEffect.MovementSpeedDebuff))//cancel root
                {
                    var effect = EffectListService.GetEffectOnTarget(ad.Target, eEffect.MovementSpeedDebuff);

                    if (effect != null)
                        EffectService.RequestImmediateCancelEffect(effect);
                }
                if (ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedBuff))//cancel speed on target
                {
                    var effect = EffectListService.GetEffectOnTarget(ad.Target, eEffect.MovementSpeedBuff);

                    if (effect != null)
                        EffectService.RequestImmediateCancelEffect(effect);
                }
                if (ad.Attacker.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedBuff))//cancel speed on archer
                {
                    var effect = EffectListService.GetEffectOnTarget(ad.Attacker, eEffect.MovementSpeedBuff);

                    if (effect != null)
                        EffectService.RequestImmediateCancelEffect(effect);
                }
            }
        }
        #endregion

        #region ShowVolleyEffect / PlayerUseVolley / MakeAnimation
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

        private void PlayerUseVolley(DOLEvent e, object sender, EventArgs args)//player click bow slow/arrow
        {
            UseSlotEventArgs useArgs = args as UseSlotEventArgs;
            GamePlayer player = sender as GamePlayer;
            if (player == null) return;
            int slot = useArgs.Slot;
            int type = useArgs.Type;
            double attackrangeMin = 2000 * 0.66;//minimum attack range
            double attackrangeMax = 4000;//maximum attack range
            if (player.Realm == eRealm.Albion)
            {
                attackrangeMin = 2200 * 0.66;//minimum attack range
                attackrangeMax = 4400;//maximum attack range
            }
            if (player.Realm == eRealm.Hibernia)
            {
                attackrangeMin = 2100 * 0.66;//minimum attack range
                attackrangeMax = 4300;//maximum attack range
            }
            if (player.Realm == eRealm.Midgard)
            {
                attackrangeMin = 2000 * 0.66;//minimum attack range
                attackrangeMax = 4200;//maximum attack range
            }

            if (slot == (int)eInventorySlot.FirstQuiver)//player can't use quiver as use button anymore
                return;
            if (slot == (int)eInventorySlot.SecondQuiver)
                return;
            if (slot == (int)eInventorySlot.ThirdQuiver)
                return;
            if (slot == (int)eInventorySlot.FourthQuiver)
                return;

            if (player.IsWithinRadius(player.GroundTarget, (int)attackrangeMin))
            {
                player.Out.SendMessage("You ground target is too close to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!player.IsWithinRadius(player.GroundTarget, (int)attackrangeMax))
            {
                player.Out.SendMessage("You ground target is too far away to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (IsReadyToFire)
            {
                ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
                if (volley != null)//make sure player does have volley effect
                {
                    LaunchVolley(player);
                }
                else
                {
                    //player.Out.SendMessage("You can't fire your arrow yet!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return;
                }
            }
            else
            {
                //player.Out.SendMessage("Your volley is not yet ready!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }
        }

        private int MakeAnimation(ECSGameTimer timer)
        {
            ShowVolleyEffect();
            IList targets = SelectTargets();

            if (targets.Count > 0)
            {
                // Pick only 1 target from list.
                GameLiving target = (GameLiving)targets[Util.Random(0, targets.Count - 1)];

                if (target != null && target.IsAlive)
                {
                    DamageTarget(target, m_player);
                    if (target is GamePlayer playerTarget)
                    {
                        if (playerTarget.IsStealthed)
                            playerTarget.Stealth(false);
                    }
                }
            }
            else
                m_player.Out.SendMessage("Your shot sails clear of all targets!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            return 0;
        }
        #endregion

        private void OnPlayerLeftWorld(DOLEvent e, object sender, EventArgs arguments)
        {
            Cancel(false);
        }

        public void OnPlayerMoved()
        {
            Cancel(false);
            AtlasOF_Volley volley = m_player.GetAbility<AtlasOF_Volley>();
            m_player.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            m_player.Out.SendMessage("You move and interrupt your volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            foreach (GamePlayer playerInRadius in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendInterruptAnimation(m_player);
        }

        public void OnPlayerSwitchedWeapon()
        {
            Cancel(false);
            AtlasOF_Volley volley = m_player.GetAbility<AtlasOF_Volley>();
            m_player.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            m_player.Out.SendMessage("You put away your bow and interrupt your volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            foreach (GamePlayer playerInRadius in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendInterruptAnimation(m_player);
        }

        public void OnAttacked()
        {
            Cancel(false);
            AtlasOF_Volley volley = m_player.GetAbility<AtlasOF_Volley>();
            m_player.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            m_player.Out.SendMessage("You have been attacked and your volley is interrupted!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            foreach (GamePlayer playerInRadius in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendInterruptAnimation(m_player);
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