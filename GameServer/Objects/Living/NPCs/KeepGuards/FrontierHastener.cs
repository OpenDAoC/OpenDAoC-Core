using System.Collections;
using DOL.Language;

namespace DOL.GS.Keeps
{
	/// <summary>
	/// Represents a keep hastener
	/// </summary>
	public class FrontierHastener : GameKeepGuard
	{
		public override eFlags Flags
		{
			get { return eFlags.PEACE; }
		}

		protected override void SetModel()
		{
			if (!ServerProperties.ServerProperties.AUTOMODEL_GUARDS_LOADED_FROM_DB && !LoadedFromScript)
			{
				return;
			}
			switch (Realm)
			{
				case ERealm.None:
				case ERealm.Albion:
					{
						Model = (ushort)ELivingModel.AlbionHastener;
						Size = 45;
						break;
					}
				case ERealm.Midgard:
					{
						Model = (ushort)ELivingModel.MidgardHastener;
						Size = 50;
						Flags ^= eFlags.GHOST;
						break;
					}
				case ERealm.Hibernia:
					{
						Model = (ushort)ELivingModel.HiberniaHastener;
						Size = 45;
						break;
					}
			}
			return;
		}

		#region Examine/Interact Message

		/// <summary>
		/// Adds messages to ArrayList which are sent when object is targeted
		/// </summary>
		/// <param name="player">GamePlayer that is examining this object</param>
		/// <returns>list with string messages</returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
			IList list = new ArrayList();
			list.Add("You examine " + GetName(0, false) + ".  " + GetPronoun(0, true) + " is " + GetAggroLevelString(player, false) + " and is a hastener.");
			return list;
		}

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			if (player.Realm != Realm)
				return false;

			TurnTo(player, 5000);
			this.CastSpellOnOwnerAndPets(player, SkillBase.GetSpellByID(GameHastener.SPEEDOFTHEREALMID), SkillBase.GetSpellLine(GlobalSpellsLines.Realm_Spells), false);
			player.Out.SendSpellEffectAnimation(this, player, SkillBase.GetSpellByID(935).ClientEffect, 0, false, 1);
			return true;
		}
		#endregion Examine/Interact Message

		protected override void SetRespawnTime()
		{
				RespawnInterval = 5000;
		}

		protected override void SetName()
		{
			Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "SetGuardName.Hastener");
			return;
		}
	}
}