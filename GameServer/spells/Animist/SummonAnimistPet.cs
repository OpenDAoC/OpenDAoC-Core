using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Summon an animist pet.
	/// </summary>
	public abstract class SummonAnimistPet : SummonSpellHandler
	{
		protected SummonAnimistPet(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		/// <summary>
		/// Check whether it's possible to summon a pet.
		/// </summary>
		/// <param name="selectedTarget"></param>
		/// <returns></returns>
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!Caster.GroundTarget.IsValid)
			{
				if (Caster is GamePlayer)
					MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNull"), eChatType.CT_SpellResisted);
				return false;
			}

			if (!Caster.GroundTargetInView)
			{
				if (Caster is GamePlayer)
					MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInView"), eChatType.CT_SpellResisted);
				return false;
			}

			if (!Caster.IsWithinRadius(Caster.GroundTarget, Spell.CalculateEffectiveRange(Caster)))
			{
				if (Caster is GamePlayer)
					MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInSpellRange"), eChatType.CT_SpellResisted);
				return false;
			}

			return base.CheckBeginCast(selectedTarget);
		}

		public override void FinishSpellCast(GameLiving target)
		{
			if (!Caster.GroundTarget.IsValid)
			{
				if (Caster is GamePlayer)
					MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNull"), eChatType.CT_SpellResisted);
				return;
			}

			if (!Caster.GroundTargetInView)
			{
				if (Caster is GamePlayer)
					MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInView"), eChatType.CT_SpellResisted);
				return;
			}

			if (!Caster.IsWithinRadius(Caster.GroundTarget, Spell.CalculateEffectiveRange(Caster)))
			{
				if (Caster is GamePlayer)
					MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInSpellRange"), eChatType.CT_SpellResisted);
				return;
			}

			base.FinishSpellCast(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);

			//m_pet.Name = Spell.Name;

			if (m_pet is TurretPet)
			{
				//[Ganrod] Nidel: Set only one spell.
				if (m_pet.Spells != null && m_pet.Spells.Count > 0)
				{
					(m_pet as TurretPet).TurretSpell = m_pet.Spells[0] as Spell;
				}
			}
		}

		//[Ganrod] Nidel: use TurretPet
		protected override GameSummonedPet GetGamePet(INpcTemplate template)
		{
			return new TurretPet(template);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new TurretBrain(owner);
		}

		protected override void GetPetLocation(out int x, out int y, out int z, out ushort heading, out Region region)
		{
			x = Caster.GroundTarget.X;
			y = Caster.GroundTarget.Y;
			z = Caster.GroundTarget.Z;
			heading = Caster.Heading;
			region = Caster.CurrentRegion;
		}
		
		/// <summary>
		/// Do not trigger SubSpells
		/// </summary>
		/// <param name="target"></param>
		public override void CastSubSpells(GameLiving target)
		{
		}
	}
}
