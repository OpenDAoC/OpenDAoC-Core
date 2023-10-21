using System.Collections.Generic;
using Core.GS.SpellEffects;

namespace Core.GS.Effects
{
    //ECS Effect Class to be created Post Spell to be passed to EffectService
    public class EffectEntity
    {
        public List<IEffectComponent> _effectComponents = new List<IEffectComponent>();
    }
}