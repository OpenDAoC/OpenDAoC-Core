using DOL.GS;

namespace DOL.AI.Brain
{
	/// <summary>
	/// A brain for the commanders
	/// </summary>
	public class CommanderPetBrain : ControlledNpcBrain
	{
		public CommanderPetBrain(GameLiving owner) : base(owner) { }

        public bool MinionsAssisting => Body is CommanderPet commander && commander.MinionsAssisting;

        /// <summary>
        /// Determines if a given controlled brain is part of the commanders subpets
        /// </summary>
        /// <param name="brain">The brain to check</param>
        /// <returns>True if found, else false</returns>
        public bool FindPet(IControlledBrain brain)
		{
			if (Body.ControlledNpcList != null)
			{
				lock (Body.ControlledNpcList)
				{
					foreach (IControlledBrain icb in Body.ControlledNpcList)
						if (brain == icb)
							return true;
				}
			}
			return false;
		}

		public override void OnOwnerAttacked(AttackData ad)
		{
			if (Body.ControlledNpcList != null)
			{
				foreach (var controlledBrain in Body.ControlledNpcList)
				{
					if (controlledBrain is SubPetBrain bdPetBrain)
						bdPetBrain.OnOwnerAttacked(ad);
				}
			}
		
			// react only on these attack results
			switch (ad.AttackResult)
			{
				case EAttackResult.Blocked:
				case EAttackResult.Evaded:
				case EAttackResult.Fumbled:
				case EAttackResult.HitStyle:
				case EAttackResult.HitUnstyled:
				case EAttackResult.Missed:
				case EAttackResult.Parried:
					AddToAggroList(ad.Attacker, ad.Attacker.EffectiveLevel + ad.Damage + ad.CriticalDamage);
					break;
			}

			if (FiniteStateMachine.GetState(EFSMStateType.AGGRO) != FiniteStateMachine.GetCurrentState())
				FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO);

			AttackMostWanted();
		}

		/// <summary>
		/// Attack the target on command
		/// </summary>
		/// <param name="target">The target to attack</param>
		public override void Attack(GameObject target)
		{
			base.Attack(target);
			CheckAbilities();

			if (MinionsAssisting && Body.ControlledNpcList != null)
			{
				lock (Body.ControlledNpcList)
				{
					foreach (SubPetBrain icb in Body.ControlledNpcList)
						icb?.Attack(target);
				}
			}
		}

		public override void Disengage()
		{
			base.Disengage();

			if (Body.ControlledNpcList != null)
			{
				lock (Body.ControlledNpcList)
				{
					foreach (SubPetBrain icb in Body.ControlledNpcList)
					{
						if (icb != null)
							icb.Disengage();
					}
				}
			}
		}

		/// <summary>
		/// Defend a minion that is being attacked
		/// </summary>
		/// <param name="ad"></param>
		public void DefendMinion(GameLiving attacker)
		{
			AddToAggroList(attacker, 1);
			AttackMostWanted();
		}

		/// <summary>
		/// Make sure the subpets are following the commander
		/// </summary>
		/// <param name="target"></param>
		public override void Follow(GameObject target)
		{
			base.Follow(target);
			SubpetFollow();
		}

		/// <summary>
		/// Direct all the sub pets to follow the commander
		/// </summary>
		private void SubpetFollow()
		{
			if (Body.ControlledNpcList != null)
			{
				lock (Body.ControlledNpcList)
				{
					foreach (SubPetBrain icb in Body.ControlledNpcList)
					{
						if (icb != null)
							icb.FollowOwner();
					}
				}
			}
		}

		/// <summary>
		/// Direct all the sub pets to follow the commander
		/// </summary>
		public override void Stay()
		{
			base.Stay();
			SubpetFollow();
		}

		/// <summary>
		/// Direct all the sub pets to follow the commander
		/// </summary>
		public override void ComeHere()
		{
			base.ComeHere();
			SubpetFollow();
		}

		/// <summary>
		/// Direct all the sub pets to follow the commander
		/// </summary>
		/// <param name="target"></param>
		public override void Goto(GameObject target)
		{
			base.Goto(target);
			SubpetFollow();
		}

		public override void SetAggressionState(EAggressionState state)
		{
			base.SetAggressionState(state);
			if (Body.ControlledNpcList != null)
			{
				lock (Body.ControlledNpcList)
				{
					foreach (SubPetBrain icb in Body.ControlledNpcList)
					{
						if (icb != null)
							icb.SetAggressionState(state);
					}
				}
			}
		}

		/// <summary>
		/// Checks if any spells need casting
		/// </summary>
		/// <param name="type">Which type should we go through and check for?</param>
		/// <returns></returns>
		public override bool CheckSpells(ECheckSpellType type)
		{
			bool casted = false;

			if (type == ECheckSpellType.Offensive && Body is CommanderPet pet
				&& pet.PreferredSpell != CommanderPet.eCommanderPreferredSpell.None
				&& !pet.IsCasting && !pet.IsBeingInterrupted && pet.TargetObject is GameLiving living && living.IsAlive)

			{
				Spell spellDamage = pet.CommSpellDamage;
				Spell spellDamageDebuff = pet.CommSpellDamageDebuff;
				Spell spellDot = pet.CommSpellDot;
				Spell spellDebuff = pet.CommSpellDebuff;
				Spell spellOther = pet.CommSpellOther;

				Spell cast = null;
				switch (pet.PreferredSpell)
				{
					case CommanderPet.eCommanderPreferredSpell.Debuff:
						if (spellDebuff != null && !living.HasEffect(spellDebuff))
							cast = spellDebuff;
						break;
					case CommanderPet.eCommanderPreferredSpell.Other:
						cast = spellOther;
						break;
				}

				if (cast == null)
				{
					// Pick a damage spell
					if (spellDot != null && !living.HasEffect(spellDot))
						cast = spellDot;
					else if (spellDamageDebuff != null && (!living.HasEffect(spellDamageDebuff) || spellDamage == null))
						cast = spellDamageDebuff;
					else if (spellDamage != null)
						cast = spellDamage;
				}

				if (cast != null)
					casted = CheckOffensiveSpells(cast);
			}

			if (casted)
			{
				// Check instant spells, but only cast one to prevent spamming
				if (Body.CanCastInstantHarmfulSpells)
					foreach (Spell spell in Body.InstantHarmfulSpells)
					{
						if (CheckOffensiveSpells(spell))
							break;
					}
			}
			else
				// Only call base method if we didn't cast anything, 
				//	otherwise it tries to cast a second offensive spell
				casted = base.CheckSpells(type);

			return casted;
		}

		public override int ModifyDamageWithTaunt(int damage)
		{
			// TODO: Move 'CommanderPet.Taunting' to the brain.
			if (Body is CommanderPet commanderPet)
			{
				int tauntScale = GS.ServerProperties.Properties.PET_BD_COMMANDER_TAUNT_VALUE;

				if (commanderPet.Taunting && tauntScale > 100)
					damage = (int)(damage * tauntScale / 100.0);
			}

			return base.ModifyDamageWithTaunt(damage);
		}
	}
}
