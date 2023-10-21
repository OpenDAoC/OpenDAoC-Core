using System.Collections.Generic;

namespace Core.GS.ECS;

//ECS Effect Class to be created Post Spell to be passed to EffectService
public class EffectEntity
{
    public List<IEffectComponent> _effectComponents = new List<IEffectComponent>();
}