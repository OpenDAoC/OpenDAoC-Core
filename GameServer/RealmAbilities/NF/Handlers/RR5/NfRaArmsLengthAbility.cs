using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	public class NfRaArmsLengthAbility : Rr5RealmAbility
	{
		public NfRaArmsLengthAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer player = living as GamePlayer;

			if (player != null)
			{
				if (player.TempProperties.GetProperty("Charging", false)
					|| player.EffectList.CountOfType(typeof(NfRaSpeedOfSoundEffect), typeof(NfRaArmsLengthEffect), typeof(NfRaChargeEffect)) > 0)
				{
					player.Out.SendMessage("You already an effect of that type!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
					return;
				}

				GameSpellEffect speed = Spells.SpellHandler.FindEffectOnTarget(player, "SpeedEnhancement");
				if (speed != null)
					speed.Cancel(false);
				new NfRaArmsLengthEffect().Start(player);
				SendCasterSpellEffectAndCastMessage(player, 7068, true);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("10 second unbreakable burst of extreme speed.");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 10 sec");
			list.Add("Casting time: instant");
		}
	}
}
