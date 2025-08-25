using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.SummonAnimistFnF)]
    public class SummonAnimistFnF : SummonAnimistPet
    {
        public SummonAnimistFnF(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            int count = 0;
            Region region = WorldMgr.GetRegion(Caster.CurrentRegion.ID);

            if (region == null || region.GetZone(Caster.GroundTarget.X, Caster.GroundTarget.Y) == null)
            {
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistFnF.CheckBeginCast.NoGroundTarget"), eChatType.CT_SpellResisted);

                return false;
            }

            // Limit the height of FnF Shrooms if in a keep area.
            foreach (AbstractArea area in region.GetAreasOfSpot(Caster.GroundTarget))
            {
                if (area is KeepArea)
                {
                    if (Caster.GroundTarget.Z - Caster.Z > 200)
                    {
                        if (Caster is GamePlayer)
                            MessageToCaster("Cannot summon a turret this high near a keep!", eChatType.CT_SpellResisted);

                        return false;
                    }
                }
            }

            if (Properties.TURRET_AREA_CAP_COUNT > 0)
            {
                foreach (GameNPC npc in Caster.CurrentRegion.GetNPCsInRadius(Caster.GroundTarget, (ushort) Properties.TURRET_AREA_CAP_RADIUS))
                {
                    if (npc.Brain is TurretFNFBrain && ++count >= Properties.TURRET_AREA_CAP_COUNT)
                    {
                        if (Caster is GamePlayer)
                            MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistFnF.CheckBeginCast.TurretAreaCap"), eChatType.CT_SpellResisted);

                        return false;
                    }
                }
            }

            if (Properties.TURRET_PLAYER_CAP_COUNT > 0 & Caster.PetCount >= Properties.TURRET_PLAYER_CAP_COUNT)
            {
                if (Caster is GamePlayer)
                    MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonAnimistFnF.CheckBeginCast.TurretPlayerCap"), eChatType.CT_SpellResisted);

                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

            if (Spell.SubSpellID > 0 && m_pet.Spells != null && SkillBase.GetSpellByID(Spell.SubSpellID) != null)
                m_pet.Spells.Add(SkillBase.GetSpellByID(Spell.SubSpellID));

            if (m_pet.Spells.Count > 0)
                (m_pet as TurretPet).TurretSpell = m_pet.Spells[0];

            (m_pet.Brain as TurretBrain).IsMainPet = false;
            Caster.UpdatePetCount(m_pet, true);
        }

        protected override void SetBrainToOwner(IControlledBrain brain) { }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            Caster.UpdatePetCount(m_pet, false);
            return base.OnEffectExpires(effect, noMessages);
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template)
        {
            return new TurretFnfPet(template);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new TurretFNFBrain(owner);
        }

        public override void CastSubSpells(GameLiving target) { }
    }
}
