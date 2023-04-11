using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private class WeaponActionData
        {
            public InventoryItem AttackWeapon { get; private set; }
            public int InterruptDuration { get; private set; }

            public WeaponActionData(InventoryItem attackWeapon, int interruptDuration)
            {
                AttackWeapon = attackWeapon;
                InterruptDuration = interruptDuration;
            }
        }

        private const ushort EFFECT_RADIUS = 350;

        public override ushort Icon => 4281;
        public override string Name => "Volley";
        public override bool HasPositiveEffect => true;

        private int _remainingShots = 5; // The code doesn't support more than 5.
        private bool _isReadyToShoot;
        ConcurrentDictionary<ECSGameTimer, WeaponActionData> _weaponActionData = new();

        public AtlasOF_VolleyECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.Volley;
            EffectService.RequestStartEffect(this);
        }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            base.OnStartEffect();

            if (OwnerPlayer.IsStealthed)
                OwnerPlayer.Stealth(false);

            OwnerPlayer.attackComponent.StopAttack();
            OwnerPlayer.StopCurrentSpellcast();
            OwnerPlayer.rangeAttackComponent.RangedAttackType = eRangedAttackType.Volley; // Used by 'RangeAttackComponent' to calculate endurance cost.
            GameEventMgr.AddHandler(OwnerPlayer, GamePlayerEvent.Quit, new DOLEventHandler(OnPlayerLeftWorld));
            GameEventMgr.AddHandler(OwnerPlayer, GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseVolley));
            PrepareBow(true);
        }

        public override void OnStopEffect()
        {
            ECSGameTimer readyTimer = OwnerPlayer.TempProperties.getProperty<ECSGameTimer>("volley_readyTimer");

            if (readyTimer != null)
            {
                readyTimer.Stop();
                OwnerPlayer.TempProperties.removeProperty("volley_readyTimer");
            }

            ECSGameTimer tiredTimer = OwnerPlayer.TempProperties.getProperty<ECSGameTimer>("volley_tiredTimer");

            if (tiredTimer != null)
            {
                tiredTimer.Stop();
                OwnerPlayer.TempProperties.removeProperty("volley_tiredTimer");
            }

            GameEventMgr.RemoveHandler(OwnerPlayer, GamePlayerEvent.Quit, new DOLEventHandler(OnPlayerLeftWorld));
            GameEventMgr.RemoveHandler(OwnerPlayer, GamePlayerEvent.UseSlot, new DOLEventHandler(PlayerUseVolley));
            OwnerPlayer.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
            base.OnStopEffect();
        }

        public void Cancel(bool playerCancel)
        {
            EffectService.RequestImmediateCancelEffect(this, playerCancel);

            foreach (GamePlayer playerInRadius in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendInterruptAnimation(OwnerPlayer);
        }

        private void PrepareBow(bool firstShot)
        {
            // Volley currently ignores Quickness and uses only the bow's speed for the first shot. Other shots have a 1.5 second preparation time.
            int speed;

            if (firstShot)
            {
                speed = OwnerPlayer.ActiveWeapon.SPD_ABS * 100;
                OwnerPlayer.Out.SendMessage("You prepare to unleash a volley of arrows!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                OwnerPlayer.Out.SendMessage($"You prepare to shoot. ({(double) speed / 1000}s)", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
            else
                speed = 1500;

            ECSGameTimer tiredTimer = new(OwnerPlayer, new ECSGameTimer.ECSTimerCallback(TooTired), RangeAttackComponent.MAX_DRAW_DURATION);
            OwnerPlayer.TempProperties.setProperty("volley_tiredTimer", tiredTimer);

            ECSGameTimer readyTimer = new(OwnerPlayer, new ECSGameTimer.ECSTimerCallback(ReadyToShoot), speed);
            OwnerPlayer.TempProperties.setProperty("volley_readyTimer", readyTimer);

            int model = OwnerPlayer.ActiveWeapon == null ? 0 : OwnerPlayer.ActiveWeapon.Model;

            foreach (GamePlayer playerInRadius in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                playerInRadius.Out.SendCombatAnimation(OwnerPlayer, null, (ushort) model, 0x00, playerInRadius.Out.BowPrepare, 0x1E, 0x00, 0x00);
        }

        private int TooTired(ECSGameTimer timer)
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(OwnerPlayer, eEffect.Volley);

            if (volley == null || !OwnerPlayer.IsAlive)
                return 0;

            Cancel(false);
            OwnerPlayer.Out.SendMessage("You are too tired to hold your volley any longer!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            OwnerPlayer.attackComponent.StopAttack();
            // TODO: Prepare normal attack?

            return 0;
        }

        private int ReadyToShoot(ECSGameTimer timer)
        {
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(OwnerPlayer, eEffect.Volley);

            if (volley == null || !OwnerPlayer.IsAlive)
                return 0;

            ECSGameTimer readyTimer = OwnerPlayer.TempProperties.getProperty<ECSGameTimer>("volley_readyTimer");

            if (readyTimer != null)
            {
                readyTimer.Stop();
                OwnerPlayer.TempProperties.removeProperty("volley_readyTimer");
            }

            _isReadyToShoot = true;
            OwnerPlayer.Out.SendMessage("You are ready to shoot!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            return 0;
        }

        protected List<GameLiving> SelectTargets()
        {
            List<GameLiving> potentialTargets = new();

            foreach (GamePlayer playerTarget in WorldMgr.GetPlayersCloseToSpot(OwnerPlayer.CurrentRegionID, OwnerPlayer.GroundTarget.X, OwnerPlayer.GroundTarget.Y, OwnerPlayer.GroundTarget.Z, EFFECT_RADIUS))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(OwnerPlayer, playerTarget, true))
                    continue;

                if (Util.Chance(50))
                    potentialTargets.Add(playerTarget);
            }

            foreach (GameNPC npcTarget in WorldMgr.GetNPCsCloseToSpot(OwnerPlayer.CurrentRegionID, OwnerPlayer.GroundTarget.X, OwnerPlayer.GroundTarget.Y, OwnerPlayer.GroundTarget.Z, EFFECT_RADIUS))
            {
                if (npcTarget is GameSiegeWeapon)
                    continue;

                if (npcTarget.ObjectState != GameObject.eObjectState.Active)
                    continue;

                if (!GameServer.ServerRules.IsAllowedToAttack(OwnerPlayer, npcTarget, true))
                    continue;

                if (Util.Chance(50))
                    potentialTargets.Add(npcTarget);
            }

            return potentialTargets;
        }

        public void DecideNextShoot()
        {
            _remainingShots -= 1;

            if (_remainingShots == 0)
            {
                OwnerPlayer.Out.SendMessage("Your volley is finished!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Cancel(false);
                AtlasOF_Volley volley = OwnerPlayer.GetAbility<AtlasOF_Volley>();
                OwnerPlayer.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            }
        }

        public void LaunchVolley(GamePlayer player)
        {
            _isReadyToShoot = false;

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

            ECSGameTimer tiredTimer = OwnerPlayer.TempProperties.getProperty<ECSGameTimer>("volley_tiredTimer");

            if (tiredTimer == null)
                return;

            tiredTimer.Stop();
            OwnerPlayer.TempProperties.removeProperty("volley_tiredTimer");

            foreach (GamePlayer playerInRadius in OwnerPlayer.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                int model = OwnerPlayer.ActiveWeapon == null ? 0 : OwnerPlayer.ActiveWeapon.Model;
                playerInRadius.Out.SendCombatAnimation(OwnerPlayer, null, (ushort) model, 0x00, playerInRadius.Out.BowShoot, 0, 0x00, 0x00);
            }

            OwnerPlayer.rangeAttackComponent.RemoveEnduranceAndAmmoOnShot();

            if (player.IsStealthed)
                player.Stealth(false);

            // Create a 'WeaponActionData' that will be used by 'MakeAttack', which will create and execute the actual 'WeaponAction' with the proper target.
            // The reason why we do this is because the player's active weapon and attack speed might change before the arrow hits something.
            int ticksToTarget = OwnerPlayer.GetDistanceTo(OwnerPlayer.GroundTarget) * 1000 / RangeAttackComponent.PROJECTILE_FLIGHT_SPEED;
            ECSGameTimer timer = new(OwnerPlayer, new ECSGameTimer.ECSTimerCallback(MakeAttack), ticksToTarget);
            WeaponActionData weaponActionData = new(player.ActiveWeapon, player.attackComponent.AttackSpeed(player.ActiveWeapon));
            _weaponActionData.TryAdd(timer, weaponActionData);

            player.Out.SendMessage("Your shot arcs into the sky!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            DecideNextShoot();

            if (_remainingShots > 0)
            {
                player.Out.SendMessage("You have " + _remainingShots + " arrows to be drawn!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                PrepareBow(false);
            }
        }

        private void ShowVolleyEffect()
        {
            VolleyMob mob = new();
            mob.X = OwnerPlayer.GroundTarget.X;
            mob.Y = OwnerPlayer.GroundTarget.Y;
            mob.Z = OwnerPlayer.GroundTarget.Z;
            mob.Level = OwnerPlayer.Level;
            mob.CurrentRegion = OwnerPlayer.CurrentRegion;
            mob.RespawnInterval = -1;
            mob.AddToWorld();
        }

        private void PlayerUseVolley(DOLEvent e, object sender, EventArgs args)
        {
            UseSlotEventArgs useArgs = args as UseSlotEventArgs;

            if (sender is not GamePlayer player)
                return;

            int slot = useArgs.Slot;

            if (slot == (int) eInventorySlot.FirstQuiver)
                return;
            else if (slot == (int) eInventorySlot.SecondQuiver)
                return;
            else if (slot == (int) eInventorySlot.ThirdQuiver)
                return;
            else if (slot == (int) eInventorySlot.FourthQuiver)
                return;

            if (player.IsWithinRadius(player.GroundTarget, AtlasOF_Volley.GetMinAttackRange(player.Realm)))
            {
                player.Out.SendMessage("You ground target is too close to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!player.IsWithinRadius(player.GroundTarget, AtlasOF_Volley.GetMaxAttackRange(player.Realm)))
            {
                player.Out.SendMessage("You ground target is too far away to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_isReadyToShoot)
            {
                ECSGameEffect volley = EffectListService.GetEffectOnTarget(OwnerPlayer, eEffect.Volley);

                if (volley != null)
                    LaunchVolley(player);
            }
        }

        private int MakeAttack(ECSGameTimer timer)
        {
            ShowVolleyEffect();
            List<GameLiving> potentialTargets = SelectTargets();
            _weaponActionData.TryRemove(timer, out WeaponActionData weaponActionData);

            if (potentialTargets.Count <= 0)
            {
                OwnerPlayer.Out.SendMessage("Your shot sails clear of all targets!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return 0;
            }

            if (weaponActionData == null)
                return 0;

            // Using 'WeaponActionData', create and add a new 'WeaponAction' to the attacker's 'AttackComponent'. A random target is chosen from the list.
            // This is a little dirty but it allow us to use the normal attack calculations from the attack component (miss chance will be ignored).
            // We clear it up once we're done using it because at this point the attack component isn't ticking.
            AttackComponent attackComponent = OwnerPlayer.attackComponent;
            attackComponent.weaponAction = new WeaponAction(OwnerPlayer, potentialTargets[Util.Random(0, potentialTargets.Count - 1)], weaponActionData.AttackWeapon, 1.0, weaponActionData.InterruptDuration, eRangedAttackType.Volley);
            attackComponent.weaponAction.Execute();
            attackComponent.weaponAction = null;
            return 0;
        }

        private void OnPlayerLeftWorld(DOLEvent e, object sender, EventArgs arguments)
        {
            Cancel(false);
        }

        public void OnPlayerMoved()
        {
            Cancel(false);
            AtlasOF_Volley volley = OwnerPlayer.GetAbility<AtlasOF_Volley>();
            OwnerPlayer.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            OwnerPlayer.Out.SendMessage("You move and interrupt your volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public void OnPlayerSwitchedWeapon()
        {
            Cancel(false);
            AtlasOF_Volley volley = OwnerPlayer.GetAbility<AtlasOF_Volley>();
            OwnerPlayer.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            OwnerPlayer.Out.SendMessage("You put away your bow and interrupt your volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public void OnAttacked()
        {
            Cancel(false);
            AtlasOF_Volley volley = OwnerPlayer.GetAbility<AtlasOF_Volley>();
            OwnerPlayer.DisableSkill(volley, AtlasOF_Volley.DISABLE_DURATION);
            OwnerPlayer.Out.SendMessage("You have been attacked and your volley is interrupted!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }

    /// <summary>
    /// Volley mob to show player actual volley location hit and nice effect for ability
    /// </summary>
    public class VolleyMob : GameNPC
    {
        public VolleyMob() : base() { }

        public override void StartAttack(GameObject target) { }

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

            StandardMobBrain volley = new();
            SetOwnBrain(volley);
            bool success = base.AddToWorld();

            if (success)
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 100);

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
