using DOL.AI;

namespace DOL.GS
{
    public abstract class HideableNpc : GameNPC
    {
        private const eFlags DEFAULT_HIDDEN_FLAGS = eFlags.CANTTARGET | eFlags.DONTSHOWNAME | eFlags.PEACE;

        protected virtual eFlags HiddenFlags => DEFAULT_HIDDEN_FLAGS;
        protected virtual ushort HiddenModel => 1;

        public bool IsHidden { get; private set; }
        public override eFlags Flags => IsHidden ? base.Flags | HiddenFlags : base.Flags;
        public override ushort Model => IsHidden ? HiddenModel : base.Model;

        protected HideableNpc() : base() { }
        protected HideableNpc(ABrain brain) : base(brain) { }

        public bool SetHidden(bool hidden)
        {
            if (IsHidden == hidden)
                return false;

            IsHidden = hidden;

            if (ObjectState is eObjectState.Active)
                ClientService.CreateObjectForPlayers(this);

            return true;
        }
    }
}
