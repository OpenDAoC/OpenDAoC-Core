﻿using System;
using Core.GS.AI;
using Core.GS.Effects;
using Core.GS.Events;
using Core.GS.Expansions.TrialsOfAtlantis;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

[SpellHandler("AstralPetSummon")]
public class SummonAstralPetSpell : SummonSpellHandler
{
    public override void ApplyEffectOnTarget(GameLiving target)
    {
        base.ApplyEffectOnTarget(target);

        m_pet.TempProperties.SetProperty("target", target);
        (m_pet.Brain as IOldAggressiveBrain).AddToAggroList(target, 1);
        (m_pet.Brain as ProcPetBrain).Think();

    }

    protected override GameSummonedPet GetGamePet(INpcTemplate template) { return new AstralPet(template); }
    protected override IControlledBrain GetPetBrain(GameLiving owner) { return new ProcPetBrain(owner); }
    protected override void SetBrainToOwner(IControlledBrain brain) {}

    protected override void OnNpcReleaseCommand(CoreEvent e, object sender, EventArgs arguments)
    {
        if (!(sender is GameNpc) || !((sender as GameNpc).Brain is IControlledBrain))
            return;
        GameNpc pet = sender as GameNpc;
        IControlledBrain brain = pet.Brain as IControlledBrain;

        GameEventMgr.RemoveHandler(pet, GameLivingEvent.PetReleased, new CoreEventHandler(OnNpcReleaseCommand));

        GameSpellEffect effect = FindEffectOnTarget(pet, this);
        if (effect != null)
            effect.Cancel(false);
    }

    protected override void GetPetLocation(out int x, out int y, out int z, out ushort heading, out Region region)
    {
        base.GetPetLocation(out x, out y, out z, out heading, out region);
        heading = Caster.Heading;
    }

     public SummonAstralPetSpell(GameLiving caster, Spell spell, SpellLine line)
        : base(caster, spell, line) { }
}