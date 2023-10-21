using System.Collections.Generic;

namespace Core.GS.Effects
{
    public class NfRaAllureOfDeathEffect : TimedEffect
    {
        private GameLiving owner;
        /// <summary>
        /// </summary>
        public NfRaAllureOfDeathEffect() : base(60000) { }

        public const int ccchance = 75;
        public const int nschance = 100;

        /// <summary>
        /// Start the effect on player
        /// </summary>
        /// <param name="target">The effect target</param>
        public override void Start(GameLiving target)
        {
            base.Start(target);
            owner = target;
            GamePlayer player = target as GamePlayer;
            if (player != null)
            {
                player.Model = 1669;
            }
        }

        public override void Stop()
        {
            base.Stop();
            GamePlayer player = owner as GamePlayer;
            if (player is GamePlayer)
            {
                player.Model = (ushort)player.CreationModel;
            }
        }

        /// <summary>
        /// Name of the effect
        /// </summary>
        public override string Name { get { return "Allure of Death"; } }

        /// <summary>
        /// Icon to show on players, can be id
        /// </summary>
        public override ushort Icon { get { return 3075; } }

        /// <summary>
        /// Delve Info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add("Changes your Skin for 30 seconds and grantz you 75% CC Imunity.");
                return list;
            }
        }
    }
}