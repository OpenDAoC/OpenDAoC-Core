using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.AI.Brain
{
    public class DragonBrain : StandardMobBrain
    {
        private const int BREATH_BAND_COUNT = 9;
        private const ushort THROW_RADIUS = 2000;
        private const ushort ROAM_GLARE_RADIUS = 5000;
        private const short ROAM_SPEED = 350;
        private const short ROAM_MAX_SPEED_BASE = 400;
        private const int ROAM_COMBAT_GUARD = 300000;
        private const ushort ROAM_SOUND_EFFECT = 2467;
        private const uint ROAM_START_HOUR = 21;
        private const uint ROAM_END_HOUR = 23;

        private readonly TimedAbility[] _abilities;
        private readonly TimedAbility _roamGlare;

        private DragonConfig Config { get; }

        internal bool _encounterStarted;
        internal bool _isRoaming;
        internal bool _roamDoneToday;
        internal int _breathIndex;
        private short _speedBeforeRoam;

        public DragonBrain(DragonConfig config)
        {
            Config = config;
            AggroLevel = 100;
            AggroRange = 800;
            ThinkInterval = 5000;
            FSM.Add(new RoamState(this));

            _abilities =
            [
                new()
                {
                    RollCooldown = () => Util.Random(Config.GlareCooldownMin, Config.GlareCooldownMax),
                    CanFire = () => !Body.IsCasting && CollectVictims((ushort) GlareSpell.Range, null).Count > 0,
                    Execute = ExecuteGlare
                },
                new()
                {
                    RollCooldown = () => Util.Random(Config.StunCooldownMin, Config.StunCooldownMax),
                    CanFire = () => Body.HealthPercent <= 75 && !Body.IsCasting && Body.TargetObject is GameLiving { IsAlive: true },
                    Execute = ExecuteStun
                },
                new()
                {
                    RollCooldown = () => Util.Random(Config.ThrowCooldownMin, Config.ThrowCooldownMax),
                    CanFire = () => Body.HealthPercent > 10 && CollectVictims(THROW_RADIUS, Body.TargetObject).Count > 0,
                    Execute = ExecuteThrow
                },
                new()
                {
                    RollCooldown = () => Util.Random(Config.MessengerWaveCooldownMin, Config.MessengerWaveCooldownMax),
                    CanFire = () => Body.HealthPercent <= 50,
                    Execute = ExecuteMessengerWave
                }
            ];

            _roamGlare = new()
            {
                RollCooldown = () => Util.Random(Config.RoamGlareCooldownMin, Config.RoamGlareCooldownMax),
                CanFire = () => !Body.IsCasting && CollectVictims(ROAM_GLARE_RADIUS, null).Count > 0,
                Execute = ExecuteRoamGlare
            };
        }

        public override void Think()
        {
            if (!_isRoaming)
            {
                if (ShouldStartRoam())
                    FSM.SetCurrentState(eFSMStateType.ROAMING);
                else if (HasAggro && Body.TargetObject != null)
                {
                    _encounterStarted = true;
                    TryLeashReset();
                    CheckBreathPhase();

                    foreach (TimedAbility ability in _abilities)
                        TickAbility(ability);
                }
                else if (_encounterStarted)
                    ResetEncounter();
            }

            if (CurrentGameHour < ROAM_START_HOUR)
                _roamDoneToday = false;

            base.Think();
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (_isRoaming)
                return;

            base.OnAttackedByEnemy(ad);
        }

        public override bool Stop()
        {
            if (_isRoaming)
                EndRoam();

            return base.Stop();
        }

        private void ResetEncounter()
        {
            _encounterStarted = false;
            _breathIndex = 0;

            foreach (TimedAbility ability in _abilities)
                ability.NextFireAt = 0;

            RaidEncounter?.DespawnAdds();
            Body.Health = Body.MaxHealth;
        }

        #region Ability scheduler

        private sealed class TimedAbility
        {
            public required Func<int> RollCooldown;
            public required Func<bool> CanFire;
            public required Action Execute;
            public long NextFireAt;
        }

        private static void TickAbility(TimedAbility ability)
        {
            long now = GameLoop.GameLoopTime;

            if (ability.NextFireAt == 0)
                ability.NextFireAt = now;

            if (now < ability.NextFireAt || !ability.CanFire())
                return;

            ability.Execute();
            ability.NextFireAt = now + ability.RollCooldown();
        }

        #endregion

        #region Breath

        private void CheckBreathPhase()
        {
            if (_breathIndex >= BREATH_BAND_COUNT || Body.IsCasting || Body.HealthPercent > 90 - _breathIndex * 10)
                return;

            GamePlayer anchor = PickVictim((ushort) Config.BreathConeRange, null);

            if (anchor == null)
                return;

            _breathIndex++;
            Message.MessageToArea(Body, string.Format(Config.BreathTexts[Util.Random(Config.BreathTexts.Length - 1)], Body.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
            Message.MessageToArea(Body, string.Format(Config.BreathTelegraphText, Body.Name, anchor.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
            anchor.Out.SendMessage(string.Format(Config.BreathAnchorText, Body.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            PulseMark(anchor, Config.BreathMarkEffect, WorldMgr.VISIBILITY_DISTANCE, 5);

            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSoundEffect(ROAM_SOUND_EFFECT, 0, 0, 0, 0, 0);

            Body.TargetObject = anchor;
            Body.TurnTo(anchor);
            Body.CastSpell(BreathSpell, m_mobSpellLine, false);
            _ = new ECSGameTimer(Body, _ => CastBreathDebuff(anchor), 6100);
        }

        private int CastBreathDebuff(GamePlayer anchor)
        {
            if (!Body.IsAlive || !HasAggro || _isRoaming || Body.IsCasting || !anchor.IsAlive || anchor.ObjectState is not GameObject.eObjectState.Active)
                return 0;

            Body.TurnTo(anchor);
            Body.CastSpell(ResistDebuffSpell, m_mobSpellLine, false);
            return 0;
        }

        #endregion

        #region Glare

        private void ExecuteGlare()
        {
            GamePlayer victim = PickVictim((ushort) GlareSpell.Range, null);

            if (victim == null)
                return;

            Message.MessageToArea(Body, string.Format(Config.GlareTelegraphText, Body.Name, victim.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
            BroadcastEffect(Body, Config.GlareDragonEffect, WorldMgr.VISIBILITY_DISTANCE);
            PulseMark(victim, Config.GlareMarkEffect, WorldMgr.VISIBILITY_DISTANCE, 5);
            _ = new ECSGameTimer(Body, _ => CastGlare(victim), 6000);
        }

        private int CastGlare(GamePlayer victim)
        {
            if (_isRoaming || !HasAggro || !Body.IsAlive || Body.IsCasting || !victim.IsAlive || victim.ObjectState is not GameObject.eObjectState.Active || !victim.IsWithinRadius(Body, GlareSpell.Range))
                return 0;

            Body.TargetObject = victim;
            Body.TurnTo(victim);
            Body.CastSpell(GlareSpell, m_mobSpellLine);
            victim.Out.SendMessage(string.Format(Config.GlareTexts[Util.Random(Config.GlareTexts.Length - 1)], Body.Name, victim.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            return 0;
        }

        private void ExecuteRoamGlare()
        {
            GamePlayer victim = PickVictim(ROAM_GLARE_RADIUS, null);

            if (victim == null || !victim.IsWithinRadius(Body, RoamGlareSpell.Range))
                return;

            Message.MessageToArea(Body, string.Format(Config.GlareTelegraphText, Body.Name, victim.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, ROAM_GLARE_RADIUS);
            BroadcastEffect(Body, Config.GlareDragonEffect, ROAM_GLARE_RADIUS);
            PulseMark(victim, Config.GlareMarkEffect, ROAM_GLARE_RADIUS, 2);
            _ = new ECSGameTimer(Body, _ => CastRoamGlare(victim), 3000);
        }

        private int CastRoamGlare(GamePlayer victim)
        {
            if (!_isRoaming || !Body.IsAlive || Body.IsCasting || !victim.IsAlive || victim.ObjectState is not GameObject.eObjectState.Active || !victim.IsWithinRadius(Body, RoamGlareSpell.Range))
                return 0;

            Body.TargetObject = victim;
            Body.TurnTo(victim);
            Body.CastSpell(RoamGlareSpell, m_mobSpellLine, false);
            victim.Out.SendMessage(string.Format(Config.GlareTexts[Util.Random(Config.GlareTexts.Length - 1)], Body.Name, victim.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            return 0;
        }

        #endregion

        #region Stun

        private void ExecuteStun()
        {
            Message.MessageToArea(Body, string.Format(Config.StunTelegraphText, Body.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellCastAnimation(Body, (ushort) Config.StunClientEffect, 60);

            _ = new ECSGameTimer(Body, CastStun, 6000);
        }

        private int CastStun(ECSGameTimer timer)
        {
            if (!_isRoaming && HasAggro && Body.IsAlive)
                Body.CastSpell(StunSpell, m_mobSpellLine);

            return 0;
        }

        #endregion

        #region Throw

        private void ExecuteThrow()
        {
            List<GamePlayer> victims = CollectVictims(THROW_RADIUS, Body.TargetObject);
            int count = Math.Min(Util.Random(2, 5), victims.Count);

            for (int i = 0; i < count; i++)
            {
                int pick = Util.Random(i, victims.Count - 1);
                (victims[i], victims[pick]) = (victims[pick], victims[i]);
                GamePlayer victim = victims[i];
                DragonConfig.ThrowDestination destination = Config.ThrowDestinations[Util.Random(Config.ThrowDestinations.Length - 1)];
                victim.Out.SendMessage(string.Format(Config.ThrowText, Body.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                victim.MoveTo(Body.CurrentRegionID, destination.X, destination.Y, destination.Z, destination.Heading);
                MovePetsWithPlayer(victim, Body.CurrentRegionID, destination);
                RemoveFromAggroList(victim);
            }
        }

        private static void MovePetsWithPlayer(GamePlayer player, ushort regionId, DragonConfig.ThrowDestination destination)
        {
            if (player.ControlledBrain?.Body is not GameNPC pet)
                return;

            if (pet.IsAlive && pet.ObjectState is GameObject.eObjectState.Active)
                pet.MoveTo(regionId, destination.X, destination.Y, destination.Z, destination.Heading);
        }

        #endregion

        #region Messengers

        private void ExecuteMessengerWave()
        {
            int count = RaidEncounter != null
                ? RaidEncounter.ScaleUnitCount(Config.PlayersPerMessenger, Config.MessengerWaveMaxCount)
                : Math.Max(1, Properties.RAID_SCALING_BASELINE_SIZE / Config.PlayersPerMessenger);

            Message.MessageToArea(Body, string.Format(Config.MessengerWaveTelegraphText, Body.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

            GameNPC anchor = new();
            anchor.Model = 665;
            anchor.Name = "dragon summons";
            anchor.Flags = GameNPC.eFlags.PEACE | GameNPC.eFlags.CANTTARGET | GameNPC.eFlags.DONTSHOWNAME;
            anchor.X = Config.MessengerSpawnPoint.X;
            anchor.Y = Config.MessengerSpawnPoint.Y;
            anchor.Z = Config.MessengerSpawnPoint.Z;
            anchor.CurrentRegion = Body.CurrentRegion;
            anchor.MaxSpeedBase = 0;
            anchor.RespawnInterval = -1;
            anchor.LoadedFromScript = true;
            anchor.SetOwnBrain(new BlankBrain());
            anchor.AddToWorld();
            RaidEncounter?.RegisterAdd(anchor);

            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(anchor, anchor, (ushort) Config.WaveEffect, 0, false, 1);
                player.Out.SendSoundEffect(ROAM_SOUND_EFFECT, 0, 0, 0, 0, 0);
            }

            _ = new ECSGameTimer(Body, _ => SpawnMessengerWave(anchor, count), 3000);
        }

        private int SpawnMessengerWave(GameNPC anchor, int count)
        {
            anchor.RemoveFromWorld();

            if (!Body.IsAlive || !HasAggro || _isRoaming)
                return 0;

            for (int i = 0; i < count; i++)
            {
                DragonMessenger messenger = Config.CreateMessenger(Util.Random(Config.MessengerPaths.Length - 1));
                messenger.OwnerBrain = this;
                messenger.X = Config.MessengerSpawnPoint.X + Util.Random(-100, 100);
                messenger.Y = Config.MessengerSpawnPoint.Y + Util.Random(-100, 100);
                messenger.Z = Config.MessengerSpawnPoint.Z;
                messenger.Heading = Body.Heading;
                messenger.CurrentRegion = Body.CurrentRegion;
                messenger.AddToWorld();
            }

            return 0;
        }

        #endregion

        #region Roaming

        private uint CurrentGameHour => Body.CurrentRegion.GameTime / 1000 / 60 / 60;

        private bool ShouldStartRoam()
        {
            return Body.IsAlive &&
                !HasAggro &&
                !_encounterStarted &&
                CurrentGameHour is >= ROAM_START_HOUR and < ROAM_END_HOUR &&
                !_roamDoneToday &&
                !Body.InCombatInLast(ROAM_COMBAT_GUARD);
        }

        private void StartRoam()
        {
            _isRoaming = true;
            _roamDoneToday = true;
            _roamGlare.NextFireAt = 0;

            foreach (GamePlayer player in ClientService.Instance.GetPlayersOfZone(Body.CurrentZone))
                player.Out.SendSoundEffect(ROAM_SOUND_EFFECT, 0, 0, 0, 0, 0);

            Message.MessageToZone(Body.CurrentZone, string.Format(Config.RoamStartText, Body.Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);

            _speedBeforeRoam = Body.MaxSpeedBase;
            Body.Flags |= GameNPC.eFlags.FLYING;
            Body.MaxSpeedBase = ROAM_MAX_SPEED_BASE;
            Body.CurrentPathPoint = MovementMgr.CreatePath(EPathType.Once, ROAM_SPEED, Config.RoamPath);
            Body.MoveOnPath(ROAM_SPEED);
        }

        private void EndRoam()
        {
            if (!_isRoaming)
                return;

            _isRoaming = false;
            Body.Flags &= ~GameNPC.eFlags.FLYING;
            Body.MaxSpeedBase = _speedBeforeRoam;
            Body.TargetObject = null;
        }

        private sealed class RoamState : StandardMobState
        {
            private readonly DragonBrain _dragonBrain;

            public override eFSMStateType StateType => eFSMStateType.ROAMING;

            public RoamState(DragonBrain brain) : base(brain)
            {
                _dragonBrain = brain;
            }

            public override void Enter()
            {
                _dragonBrain.StartRoam();
                base.Enter();
            }

            public override void Think()
            {
                if (!_dragonBrain.Body.IsMovingOnPath)
                {
                    _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                    return;
                }

                TickAbility(_dragonBrain._roamGlare);
                base.Think();
            }

            public override void Exit()
            {
                _dragonBrain.EndRoam();
                base.Exit();
            }
        }

        #endregion

        #region Targeting

        private List<GamePlayer> CollectVictims(ushort radius, GameObject excluded)
        {
            List<GamePlayer> victims = new();

            foreach (GamePlayer player in Body.GetPlayersInRadius(radius))
            {
                if (player != excluded && player.IsAlive && player.Client.Account.PrivLevel == (int) ePrivLevel.Player)
                    victims.Add(player);
            }

            return victims;
        }

        private GamePlayer PickVictim(ushort radius, GameObject excluded)
        {
            List<GamePlayer> victims = CollectVictims(radius, excluded);
            return victims.Count > 0 ? victims[Util.Random(victims.Count - 1)] : null;
        }

        private void BroadcastEffect(GameObject target, int effectId, ushort radius)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(radius))
                player.Out.SendSpellEffectAnimation(target, target, (ushort) effectId, 0, false, 1);
        }

        private void PulseMark(GamePlayer target, int effectId, ushort radius, int pulses)
        {
            BroadcastEffect(target, effectId, radius);

            int done = 0;

            _ = new ECSGameTimer(Body, _ =>
            {
                if (!Body.IsAlive || !target.IsAlive || target.ObjectState is not GameObject.eObjectState.Active)
                    return 0;

                BroadcastEffect(target, effectId, radius);
                return ++done >= pulses ? 0 : 1000;
            }, 1000);
        }

        #endregion

        #region Spells

        private Spell RoamGlareSpell => ScriptSpells.GetOrCreate($"{Config.SpellKeyPrefix}RoamGlare", 70, spell =>
        {
            spell.CastTime = 0;
            spell.RecastDelay = 0;
            spell.ClientEffect = Config.GlareClientEffect;
            spell.Icon = Config.GlareClientEffect;
            spell.TooltipId = (ushort) Config.GlareClientEffect;
            spell.Damage = 2000;
            spell.Name = Config.GlareSpellName;
            spell.Range = 5000;
            spell.Radius = 400;
            spell.SpellID = Config.RoamGlareSpellId;
            spell.Target = eSpellTarget.ENEMY.ToString();
            spell.Type = eSpellType.DirectDamageNoVariance.ToString();
            spell.Uninterruptible = true;
            spell.DamageType = (int) Config.SpellDamageType;
        });

        private Spell GlareSpell => ScriptSpells.GetOrCreate($"{Config.SpellKeyPrefix}Glare", 70, spell =>
        {
            spell.CastTime = 0;
            spell.RecastDelay = 0;
            spell.ClientEffect = Config.GlareClientEffect;
            spell.Icon = Config.GlareClientEffect;
            spell.TooltipId = (ushort) Config.GlareClientEffect;
            spell.Damage = 1500;
            spell.Name = Config.GlareSpellName;
            spell.Range = 1500;
            spell.Radius = 400;
            spell.SpellID = Config.GlareSpellId;
            spell.Target = eSpellTarget.ENEMY.ToString();
            spell.Type = eSpellType.DirectDamageNoVariance.ToString();
            spell.Uninterruptible = true;
            spell.DamageType = (int) Config.SpellDamageType;
        });

        private Spell BreathSpell => ScriptSpells.GetOrCreate($"{Config.SpellKeyPrefix}Breath", 70, spell =>
        {
            spell.CastTime = 6;
            spell.RecastDelay = 0;
            spell.ClientEffect = Config.BreathClientEffect;
            spell.Icon = Config.BreathClientEffect;
            spell.TooltipId = (ushort) Config.BreathClientEffect;
            spell.Damage = 2400;
            spell.Name = Config.BreathSpellName;
            spell.Range = Config.BreathConeRange;
            spell.Radius = Config.BreathConeArc;
            spell.SpellID = Config.BreathSpellId;
            spell.Target = eSpellTarget.CONE.ToString();
            spell.Type = eSpellType.DirectDamageNoVariance.ToString();
            spell.Uninterruptible = true;
            spell.DamageType = (int) Config.SpellDamageType;
        });

        private Spell StunSpell => ScriptSpells.GetOrCreate($"{Config.SpellKeyPrefix}Stun", 70, spell =>
        {
            spell.CastTime = 0;
            spell.RecastDelay = 0;
            spell.ClientEffect = Config.StunClientEffect;
            spell.Icon = Config.StunClientEffect;
            spell.TooltipId = (ushort) Config.StunClientEffect;
            spell.Duration = 30;
            spell.Name = Config.StunSpellName;
            spell.Range = 0;
            spell.Radius = 1000;
            spell.SpellID = Config.StunSpellId;
            spell.Target = eSpellTarget.ENEMY.ToString();
            spell.Type = eSpellType.Stun.ToString();
            spell.Uninterruptible = true;
            spell.DamageType = (int) eDamageType.Body;
        });

        private Spell ResistDebuffSpell => ScriptSpells.GetOrCreate($"{Config.SpellKeyPrefix}ResistDebuff", 70, spell =>
        {
            spell.CastTime = 0;
            spell.RecastDelay = 0;
            spell.ClientEffect = Config.ResistDebuffClientEffect;
            spell.Icon = Config.SpellClientEffect;
            spell.TooltipId = (ushort) Config.SpellClientEffect;
            spell.Duration = 120;
            spell.Value = 50;
            spell.Name = Config.ResistDebuffSpellName;
            spell.Description = Config.ResistDebuffDescription;
            spell.Range = Config.BreathConeRange;
            spell.Radius = Config.BreathConeArc;
            spell.SpellID = Config.ResistDebuffSpellId;
            spell.Target = eSpellTarget.CONE.ToString();
            spell.Type = Config.ResistDebuffSpellType.ToString();
            spell.Uninterruptible = true;
            spell.DamageType = (int) Config.SpellDamageType;
        });

        #endregion
    }
}
