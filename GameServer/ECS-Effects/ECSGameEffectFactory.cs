using DOL.GS.Spells;

namespace DOL.GS
{
    public readonly struct ECSGameEffectInitParams
    {
        public readonly GameLiving Target { get; }
        public readonly int Duration { get; }
        public readonly double Effectiveness { get; }
        public readonly ISpellHandler SpellHandler { get; }

        public ECSGameEffectInitParams(GameLiving target, int duration, double effectiveness, ISpellHandler spellHandler = null)
        {
            Target = target;
            Duration = duration;
            Effectiveness = effectiveness;
            SpellHandler = spellHandler;
        }
    }

    public static class ECSGameEffectFactory
    {
        public delegate T CreatorDelegate<T>(in ECSGameEffectInitParams initParams) where T : ECSGameEffect;
        public delegate T CreatorDelegate<T, TArg1>(in ECSGameEffectInitParams initParams, TArg1 arg1) where T : ECSGameEffect;
        public delegate T CreatorDelegate<T, TArg1, TArg2>(in ECSGameEffectInitParams initParams, TArg1 arg1, TArg2 arg2) where T : ECSGameEffect;

        public static T Create<T>(in ECSGameEffectInitParams initParams, CreatorDelegate<T> creator)
            where T : ECSGameEffect
        {
            T effect = creator(initParams);
            effect.Start();
            return effect;
        }

        public static T Create<T, TArg1>(in ECSGameEffectInitParams initParams, TArg1 arg1, CreatorDelegate<T, TArg1> creator)
            where T : ECSGameEffect
        {
            T effect = creator(initParams, arg1);
            effect.Start();
            return effect;
        }

        public static T Create<T, TArg1, TArg2>(in ECSGameEffectInitParams initParams, TArg1 arg1, TArg2 args2, CreatorDelegate<T, TArg1, TArg2> creator)
            where T : ECSGameEffect
        {
            T effect = creator(initParams, arg1, args2);
            effect.Start();
            return effect;
        }
    }
}
