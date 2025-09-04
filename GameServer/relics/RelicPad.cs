using System.Threading;

namespace DOL.GS.Relics
{
    /// <summary>
    /// Class representing a relic pad.
    /// </summary>
    public class RelicPad : GameObject
    {
        /// <summary>
        /// The pillar this pad triggers.
        /// </summary>
        private RelicPillar _relicPillar;
        private int _playersOnPad;
        private Lock _lock = new();

        static public int Radius => 200;
        public override eGameObjectType GameObjectType => eGameObjectType.KEEP_COMPONENT;

        public RelicPad(RelicPillar relicPillar)
        {
            _relicPillar = relicPillar;
        }

        public void OnPlayerEnter(GamePlayer player)
        {
            lock (_lock)
            {
                _playersOnPad++;
                CheckPadState();
            }
        }

        public void OnPlayerLeave(GamePlayer player)
        {
            lock (_lock)
            {
                _playersOnPad--;
                CheckPadState();
            }
        }

        private void CheckPadState()
        {
            if (_playersOnPad >= ServerProperties.Properties.RELIC_PLAYERS_REQUIRED_ON_PAD && _relicPillar.State is eDoorState.Closed)
                _relicPillar.Open();
            else if (_relicPillar.State is eDoorState.Open && _playersOnPad <= 0)
                _relicPillar.Close();
        }

        /// <summary>
        /// Class to register players entering or leaving the pad.
        /// </summary>
        public class Surface : Area.Circle
        {
            private RelicPad _relicPad;

            public Surface(RelicPad relicPad) : base("", relicPad.X, relicPad.Y, relicPad.Z, RelicPad.Radius)
            {
                _relicPad = relicPad;
            }

            public override void OnPlayerEnter(GamePlayer player)
            {
                _relicPad.OnPlayerEnter(player);
            }

            public override void OnPlayerLeave(GamePlayer player)
            {
                _relicPad.OnPlayerLeave(player);
            }
        }
    }
}
