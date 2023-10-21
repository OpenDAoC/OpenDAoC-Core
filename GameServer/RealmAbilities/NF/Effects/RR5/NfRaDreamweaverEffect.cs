using System.Collections.Generic;
using Core.GS.GameUtils;

namespace Core.GS.Effects
{
	public class NfRaDreamweaverEffect : TimedEffect
	{
		private GameLiving owner;
		/// <summary>
		/// </summary>
		public NfRaDreamweaverEffect() : base(300000) { }

		/// <summary>
		/// Start the effect on player
		/// </summary>
		/// <param>The effect target</param>
		public override void Start(GameLiving target)
		{
			base.Start(target);
			owner = target;
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				player.Model = GetRandomMorph();
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
		public override string Name { get { return "Dreamweaver"; } }

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon { get { return 3051; } }

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Transforms you into a random morph for 5 minutes.");
				return list;
			}
		}

		public static ushort GetRandomMorph()
		{
			return (ushort)Util.Random(1649, 1668);
			// Returns random morph/gender (like live..)
		}

	}
}