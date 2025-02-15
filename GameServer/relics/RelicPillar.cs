namespace DOL.GS.Relics
{
    /// <summary>
    /// Class representing a relic pillar.
    /// </summary>
    /// <author>Aredhel</author>
    public class RelicPillar : GameDoorBase
    {
        /// <summary>
        /// Creates a new relic pillar.
        /// </summary>
        public RelicPillar() : base()
        {
            Realm = 0;
            Close();
        }

        /// <summary>
        /// Pillars behave like regular doors.
        /// </summary>
        public override uint Flag
        {
            get => 0;
            set { }
        }

        /// <summary>
        /// Make the pillar start moving down.
        /// </summary>
        public override void Open(GameLiving opener = null)
        {
            State = eDoorState.Open;
        }

        /// <summary>
        /// Reset pillar.
        /// </summary>
        public override void Close(GameLiving closer = null)
        {
            State = eDoorState.Closed;
        }
    }
}
