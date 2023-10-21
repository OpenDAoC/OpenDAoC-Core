using System;
using System.Linq;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS.Spells
{
    /// <summary>
    /// Summon a fnf animist pet.
    /// </summary>
    [SpellHandler("SummonAnimistFnF")]
	public class SummonAnimistFnfTurretSpell : SummonAnimistPetSpell
	{
		public SummonAnimistFnfTurretSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			int nCount = 0;

			Region rgn = WorldMgr.GetRegion(Caster.CurrentRegion.ID);

			if (rgn == null || rgn.GetZone(Caster.GroundTarget.X, Caster.GroundTarget.Y) == null)
			{
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistFnF.CheckBeginCast.NoGroundTarget"), EChatType.CT_SpellResisted);
                return false;
			}

			//Limit the height of FnF Shrooms if in a keep area
			foreach (AbstractArea area in rgn.GetAreasOfSpot(Caster.GroundTarget))
			{
				if (area is KeepArea)
				{
					if (Caster.GroundTarget.Z - Caster.Z > 200)
					{
						if (Caster is GamePlayer)
                    		MessageToCaster("Cannot summon a turret this high near a keep!", EChatType.CT_SpellResisted);
						return false;
					}
					
				}
			}

			foreach (GameNpc npc in Caster.CurrentRegion.GetNPCsInRadius(Caster.GroundTarget, (ushort) Properties.TURRET_AREA_CAP_RADIUS))
			{
				if (npc.Brain is TurretFnfBrain)
					nCount++;
			}

			if (nCount >= Properties.TURRET_AREA_CAP_COUNT)
			{
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistFnF.CheckBeginCast.TurretAreaCap"), EChatType.CT_SpellResisted);
                return false;
			}

			if (Caster.PetCount >= Properties.TURRET_PLAYER_CAP_COUNT)
			{
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistFnF.CheckBeginCast.TurretPlayerCap"), EChatType.CT_SpellResisted);
                return false;
			}

			return base.CheckBeginCast(selectedTarget);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);

			if (Spell.SubSpellID > 0 && m_pet.Spells != null && SkillBase.GetSpellByID(Spell.SubSpellID) != null)
			{
				m_pet.Spells.Add(SkillBase.GetSpellByID(Spell.SubSpellID));
			}

			if (m_pet.Spells.Count > 0)
			{
				//[Ganrod] Nidel: Set only one spell.
				(m_pet as TurretPet).TurretSpell = m_pet.Spells[0] as Spell;
			}

			(m_pet.Brain as TurretBrain).IsMainPet = false;
			(m_pet.Brain as TurretBrain).Think();
			Caster.UpdatePetCount(true);
		}

		protected override void SetBrainToOwner(IControlledBrain brain)
		{
		}

		/// <summary>
		/// [Ganrod] Nidel: Can remove TurretFNF
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected override void OnNpcReleaseCommand(CoreEvent e, object sender, EventArgs arguments)
		{
			m_pet = sender as GameSummonedPet;
			if (m_pet == null)
				return;

			if ((m_pet.Brain as TurretFnfBrain) == null)
				return;

			if (Caster.ControlledBrain == null)
			{
				((GamePlayer)Caster).Out.SendPetWindow(null, EPetWindowAction.Close, 0, 0);
			}

			GameEventMgr.RemoveHandler(m_pet, GameLivingEvent.PetReleased, OnNpcReleaseCommand);

			//GameSpellEffect effect = FindEffectOnTarget(m_pet, this);
			//if (effect != null)
			//	effect.Cancel(false);
			if (m_pet.effectListComponent.Effects.TryGetValue(EEffect.Pet, out var petEffect))
				EffectService.RequestImmediateCancelEffect(petEffect.FirstOrDefault());
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			Caster.UpdatePetCount(false);
			return base.OnEffectExpires(effect, noMessages);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new TurretFnfBrain(owner);
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
