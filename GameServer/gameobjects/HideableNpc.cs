namespace DOL.GS
{
    /// <summary>
    /// Base class for NPCs that can be visually hidden without mutating their persisted flags or model.
    /// </summary>
    public abstract class HideableNpc : GameNPC
    {
        public const eFlags DEFAULT_HIDDEN_FLAGS = eFlags.CANTTARGET | eFlags.DONTSHOWNAME | eFlags.PEACE;

        private bool _hidden;
        private bool _suppressMask;

        public bool IsHidden => _hidden;
        protected virtual eFlags HiddenFlags => DEFAULT_HIDDEN_FLAGS;
        protected virtual ushort HiddenModel => 1;

        public override eFlags Flags => _hidden && !_suppressMask ? base.Flags | HiddenFlags : base.Flags;
        public override ushort Model => _hidden && !_suppressMask ? HiddenModel : base.Model;

        /// <summary>
        /// Sets the hidden state. Returns true if the call actually changed the hidden state.
        /// </summary>
        public bool SetHidden(bool hidden)
        {
            if (_hidden == hidden)
                return false;

            _hidden = hidden;

            if (ObjectState == eObjectState.Active)
                ClientService.CreateObjectForPlayers(this);

            return true;
        }

        public override void SaveIntoDatabase()
        {
            _suppressMask = true;

            try
            {
                base.SaveIntoDatabase();
            }
            finally
            {
                _suppressMask = false;
            }
        }
    }
}
