using System;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.Spells
{
	[SpellHandler("PowerDrainPet")]
	public class PowerDrainPetSpell : PowerDrainSpell
	{
		public override void DrainPower(AttackData ad)
		{
			if ( !(m_caster is NecromancerPet))
				return;
			
			base.DrainPower(ad);
		}
		
		/// The power channelled through this spell goes to the owner, not the pet
		protected override GameLiving Owner()
		{
			return ((Caster as NecromancerPet).Brain as IControlledBrain).Owner;
		}
		
		/// <summary>
		/// Message the pet's owner, not the pet
		/// </summary>
		/// <param name="message"></param>
		/// <param name="chatType"></param>
		protected override void MessageToOwner(String message, EChatType chatType)
		{
			GameNpc npc = Caster as GameNpc;
			if (npc != null)
			{
				ControlledNpcBrain brain = npc.Brain as ControlledNpcBrain;
				if (brain != null)
				{
					GamePlayer owner = brain.Owner as GamePlayer;
					if (owner != null)
						owner.Out.SendMessage(message, chatType, EChatLoc.CL_SystemWindow);
				}
			}
		}
		
		/// <summary>
		/// Create a new handler for the necro petpower drain spell.
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public PowerDrainPetSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line) { }
	}
}