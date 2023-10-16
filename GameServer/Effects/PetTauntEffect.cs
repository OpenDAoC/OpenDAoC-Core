using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Pet taunt effect. While active, the pet will keep trying
	/// to taunt its target in case it is attacking someone else.
	/// </summary>
	class PetTauntEffect : StaticEffect, IGameEffect
	{
		/// <summary>
		/// Creates a new taunt effect.
		/// </summary>
		public PetTauntEffect()
			: base() { }

		/// <summary>
		/// Start the effect.
		/// </summary>
		/// <param name="target"></param>
		public override void Start(GameLiving target)
		{
			base.Start(target);

			GamePlayer petOwner = null;
			if (target is GameNpc && (target as GameNpc).Brain is IControlledBrain)
				petOwner = ((target as GameNpc).Brain as IControlledBrain).Owner as GamePlayer;

			foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (player == null)
					continue;

				player.Out.SendSpellEffectAnimation(target, target, 1073, 0, false, 1);

				EChatType chatType = (player != null && player == petOwner)
					? EChatType.CT_Spell
					: EChatType.CT_System;

				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Effects.Necro.TauntEffect.SeemsChange", target.GetName(0, true)), chatType, EChatLoc.CL_SystemWindow);
			}
		}

		/// <summary>
		/// Stop the effect.
		/// </summary>
		public override void Stop()
		{
			base.Stop();

			GamePlayer petOwner = null;
			if (Owner is GameNpc && (Owner as GameNpc).Brain is IControlledBrain)
				petOwner = ((Owner as GameNpc).Brain as IControlledBrain).Owner as GamePlayer;

			foreach (GamePlayer player in Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (player == null)
					continue;

				EChatType chatType = (player == petOwner)
					? EChatType.CT_SpellExpires
					: EChatType.CT_System;

				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Effects.Necro.TauntEffect.SeemsLessAgg", Owner.GetName(0, true)), chatType, EChatLoc.CL_SystemWindow);
			}
		}
	}
}
