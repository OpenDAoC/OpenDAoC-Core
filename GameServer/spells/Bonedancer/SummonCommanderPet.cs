using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.Language;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell handler to summon a bonedancer pet.
	/// </summary>
	/// <author>IST</author>
	[SpellHandler("SummonCommander")]
	public class SummonCommanderPet : SummonSpellHandler
	{
		public SummonCommanderPet(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line) { }

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (Caster is GamePlayer && ((GamePlayer)Caster).ControlledBrain != null)
			{
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.CheckBeginCast.Text"), eChatType.CT_SpellResisted);
                return false;
            }
			return base.CheckBeginCast(selectedTarget);
		}

		protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments)
		{
			if (!(sender is CommanderPet))
				return;

			CommanderPet pet = sender as CommanderPet;

			if (pet.ControlledNpcList != null)
			{
				foreach (BdPetBrain cnpc in pet.ControlledNpcList)
				{
					if (cnpc != null)
						GameEventMgr.Notify(GameLivingEvent.PetReleased, cnpc.Body);
				}
			}
			base.OnNpcReleaseCommand(e, sender, arguments);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new BdCommanderBrain(owner);
		}

		protected override GameSummonedPet GetGamePet(INpcTemplate template)
		{
			return new CommanderPet(template);
		}

		/// <summary>
		/// Delve info string.
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
                var delve = new List<string>();
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text1"));
                delve.Add("");
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text2"));
                delve.Add("");
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text3", Spell.Target));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text4", Math.Abs(Spell.Power)));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonCommanderPet.DelveInfo.Text5", (Spell.CastTime / 1000).ToString("0.0## " + (LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "Effects.DelveInfo.Seconds")))));
                return delve;
            }
		}
	}
}
