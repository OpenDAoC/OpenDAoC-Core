using System;
using System.Collections.Generic;
using System.Linq;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Spells
{
	/// <summary>
	/// Pet summon spell handler
	///
	/// Spell.LifeDrainReturn is used for pet ID.
	///
	/// Spell.Value is used for hard pet level cap
	/// Spell.Damage is used to set pet level:
	/// less than zero is considered as a percent (0 .. 100+) of target level;
	/// higher than zero is considered as level value.
	/// Resulting value is limited by the Byte field type.
	/// Spell.DamageType is used to determine which type of pet is being cast:
	/// 0 = melee
	/// 1 = healer
	/// 2 = mage
	/// 3 = debuffer
	/// 4 = Buffer
	/// 5 = Range
	/// </summary>
	[SpellHandler("SummonMinion")]
	public class SummonSubPetSpell : SummonSpellHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public SummonSubPetSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line) { }

		/// <summary>
		/// All checks before any casting begins
		/// </summary>
		/// <param name="selectedTarget"></param>
		/// <returns></returns>
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (Caster is GamePlayer && ((GamePlayer)Caster).ControlledBrain == null)
			{
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonMinionHandler.CheckBeginCast.Text1"), EChatType.CT_SpellResisted);
                return false;
			}

			if (Caster is GamePlayer && (((GamePlayer)Caster).ControlledBrain.Body.ControlledNpcList == null || ((GamePlayer)Caster).ControlledBrain.Body.PetCount >= ((GamePlayer)Caster).ControlledBrain.Body.ControlledNpcList.Length))
			{
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonMinionHandler.CheckBeginCast.Text2"), EChatType.CT_SpellResisted);

                return false;
			}
			
			if (Caster is GamePlayer && ((GamePlayer) Caster).ControlledBrain != null &&
			    ((GamePlayer) Caster).ControlledBrain.Body.ControlledNpcList != null)
			{
				int cumulativeLevel = 0;
				foreach (var petBrain in ((GamePlayer) Caster).ControlledBrain.Body.ControlledNpcList)
				{
					cumulativeLevel += petBrain != null && petBrain.Body != null ? petBrain.Body.Level : 0;
				}

				byte newpetlevel = (byte)(Caster.Level * m_spell.Damage * -0.01);
				if (newpetlevel > m_spell.Value)
					newpetlevel = (byte)m_spell.Value;

				if (cumulativeLevel + newpetlevel > 75)
				{
					MessageToCaster("Your commander is not powerful enough to control a subpet of this level.", EChatType.CT_SpellResisted);
					return false;
				}
			}
			return base.CheckBeginCast(selectedTarget);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (Caster == null || Caster.ControlledBrain == null)
				return;

			GameNpc temppet = Caster.ControlledBrain.Body;
			//Lets let NPC's able to cast minions.  Here we make sure that the Caster is a GameNPC
			//and that m_controlledNpc is initialized (since we aren't thread safe).
			if (temppet == null)
			{
				if (Caster is GameNpc)
				{
					temppet = (GameNpc)Caster;
					//We'll give default NPCs 2 minions!
					if (temppet.ControlledNpcList == null)
						temppet.InitControlledBrainArray(2);
				}
				else
					return;
			}

			base.ApplyEffectOnTarget(target);

			if (m_pet.Brain is SubPetBrain brain && !brain.MinionsAssisting)
				brain.SetAggressionState(EAggressionState.Passive);

			// Assign weapons
			if (m_pet is SubPet subPet)
				switch (subPet.Brain)
				{
					case ArcherSubPetBrain archer:
						subPet.MinionGetWeapon(CommanderPet.eWeaponType.OneHandSword);
						subPet.MinionGetWeapon(CommanderPet.eWeaponType.Bow);
						break;
					case DebufferSubPetBrain debuffer:
						subPet.MinionGetWeapon(CommanderPet.eWeaponType.OneHandHammer);
						break;
					case BufferSubPetBrain buffer:
					case CasterSubPetBrain caster:
						subPet.MinionGetWeapon(CommanderPet.eWeaponType.Staff);
						break;
					case MeleeSubPetBrain melee:
						if(Util.Chance(60))
							subPet.MinionGetWeapon(CommanderPet.eWeaponType.TwoHandAxe);
						else
							subPet.MinionGetWeapon(CommanderPet.eWeaponType.OneHandAxe);
						break;
				}
		}

		/// <summary>
		/// Called when owner release NPC
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected override void OnNpcReleaseCommand(CoreEvent e, object sender, EventArgs arguments)
		{
			GameNpc pet = sender as GameNpc;
			if (pet == null)
				return;

			GameEventMgr.RemoveHandler(pet, GameLivingEvent.PetReleased, new CoreEventHandler(OnNpcReleaseCommand));

			//GameSpellEffect effect = FindEffectOnTarget(pet, this);
			//if (effect != null)
			//	effect.Cancel(false);
			if (pet.effectListComponent.Effects.TryGetValue(EEffect.Pet, out var petEffect))
				EffectService.RequestImmediateCancelEffect(petEffect.FirstOrDefault());
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if ((effect.Owner is BonedancerPet) && ((effect.Owner as BonedancerPet).Brain is IControlledBrain) && (((effect.Owner as BonedancerPet).Brain as IControlledBrain).Owner is CommanderPet))
			{
				BonedancerPet pet = effect.Owner as BonedancerPet;
				CommanderPet commander = (pet.Brain as IControlledBrain).Owner as CommanderPet;
				commander.RemoveControlledNpc(pet.Brain as IControlledBrain);
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			IControlledBrain controlledBrain = null;
			ESubPetType type = (ESubPetType)(byte)this.Spell.DamageType;
			owner = owner.ControlledBrain.Body;

			switch (type)
			{
				//Melee
				case ESubPetType.Melee:
					controlledBrain = new MeleeSubPetBrain(owner);
					break;
				//Healer
				case ESubPetType.Healer:
					controlledBrain = new HealerSubPetBrain(owner);
					break;
				//Mage
				case ESubPetType.Caster:
					controlledBrain = new CasterSubPetBrain(owner);
					break;
				//Debuffer
				case ESubPetType.Debuffer:
					controlledBrain = new DebufferSubPetBrain(owner);
					break;
				//Buffer
				case ESubPetType.Buffer:
					controlledBrain = new BufferSubPetBrain(owner);
					break;
				//Range
				case ESubPetType.Archer:
					controlledBrain = new ArcherSubPetBrain(owner);
					break;
				//Other
				default:
					controlledBrain = new ControlledNpcBrain(owner);
					break;
			}

			return controlledBrain;
		}

		protected override GameSummonedPet GetGamePet(INpcTemplate template)
		{
			return new SubPet(template);
		}

		protected override void SetBrainToOwner(IControlledBrain brain)
		{
			Caster.ControlledBrain.Body.AddControlledNpc(brain);
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delve = new List<string>();
                delve.Add(String.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonMinionHandler.DelveInfo.Text1", Spell.Target)));
                delve.Add(String.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonMinionHandler.DelveInfo.Text2", Math.Abs(Spell.Power))));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonMinionHandler.DelveInfo.Text3", (Spell.CastTime / 1000).ToString("0.0## " + LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "Effects.DelveInfo.Seconds"))));
				return delve;
			}
		}
	}
}
