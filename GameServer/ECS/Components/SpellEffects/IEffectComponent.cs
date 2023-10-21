namespace Core.GS.ECS;

public interface IEffectComponent
{
    ESpellEffect Type { get; set; }
    ushort SpellEffectId { get; set; }
}