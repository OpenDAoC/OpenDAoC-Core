using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class PlayerEffectListComponent : EffectListComponent
    {
        private GamePlayer _owner;

        private readonly Dictionary<int, ECSGameEffect> _effectIdToEffect = new();   // Dictionary of effects by their icon ID.
        private EffectHelper.PlayerUpdate _requestedPlayerUpdates;                   // Player updates requested by the effects, to be sent in the next tick.
        private int _lastUpdateEffectsCount;                                         // Number of effects sent in the last player update, used externally.
        private readonly Lock _playerUpdatesLock = new();

        public PlayerEffectListComponent(GamePlayer owner) : base(owner)
        {
            _owner = owner;
        }

        public override void BeginTick()
        {
            base.BeginTick();
            SendPlayerUpdates();
        }

        public override void RequestPlayerUpdate(EffectHelper.PlayerUpdate playerUpdate)
        {
            lock (_playerUpdatesLock)
            {
                _requestedPlayerUpdates |= playerUpdate;
                ServiceObjectStore.Add(this as EffectListComponent);
            }
        }

        public override ECSGameEffect TryGetEffectFromEffectId(int effectId)
        {
            ECSGameEffect effect;

            lock (_effectsLock)
            {
                _effectIdToEffect.TryGetValue(effectId, out effect);
            }

            return effect;
        }

        protected override void SetEffectIdToEffect(ECSGameEffect effect)
        {
            // `_effectsLock` is expected to be acquired already.
            _effectIdToEffect[effect.Icon] = effect;
        }

        protected override void RemoveEffectIdToEffect(ECSGameEffect effect)
        {
            // `_effectsLock` is expected to be acquired already.
            _effectIdToEffect.Remove(effect.Icon);
        }

        private void SendPlayerUpdates()
        {
            if (_requestedPlayerUpdates is EffectHelper.PlayerUpdate.None)
                return;

            EffectHelper.PlayerUpdate requestedUpdates;

            lock (_playerUpdatesLock)
            {
                if (_requestedPlayerUpdates is EffectHelper.PlayerUpdate.None)
                    return;

                requestedUpdates = _requestedPlayerUpdates;
                _requestedPlayerUpdates = EffectHelper.PlayerUpdate.None;
            }

            if ((requestedUpdates & EffectHelper.PlayerUpdate.Icons) != 0)
            {
                _owner.Group?.UpdateMember(_owner, true, false);
                _owner.Out.SendUpdateIcons(GetEffects(), ref _lastUpdateEffectsCount);
            }

            if ((requestedUpdates & EffectHelper.PlayerUpdate.Status) != 0)
                _owner.Out.SendStatusUpdate();

            if ((requestedUpdates & EffectHelper.PlayerUpdate.Stats) != 0)
                _owner.Out.SendCharStatsUpdate();

            if ((requestedUpdates & EffectHelper.PlayerUpdate.Resists) != 0)
                _owner.Out.SendCharResistsUpdate();

            if ((requestedUpdates & EffectHelper.PlayerUpdate.WeaponArmor) != 0)
                _owner.Out.SendUpdateWeaponAndArmorStats();

            if ((requestedUpdates & EffectHelper.PlayerUpdate.Encumbrance) != 0)
                _owner.UpdateEncumbrance();

            if ((requestedUpdates & EffectHelper.PlayerUpdate.Concentration) != 0)
                _owner.Out.SendConcentrationList();

            if ((requestedUpdates & EffectHelper.PlayerUpdate.PetWindow) != 0)
                _owner.ControlledBrain?.UpdatePetWindow();
        }
    }
}
