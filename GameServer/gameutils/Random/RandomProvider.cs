using System.Collections.Generic;
using System.Threading;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public interface IRandomProvider
    {
        bool Chance(RandomContext ctx, int chancePercent);
        bool Chance(RandomContext ctx, double chancePercent);
        double GetPseudoDouble(RandomContext ctx);
        double GetPseudoDoubleIncl(RandomContext ctx);
    }

    public sealed class DefaultRandomProvider : IRandomProvider
    {
        public static DefaultRandomProvider Instance { get; } = new();

        private DefaultRandomProvider() { }

        public bool Chance(RandomContext ctx, int chancePercent)
        {
            return Util.Chance(chancePercent);
        }

        public bool Chance(RandomContext ctx, double chancePercent)
        {
            return Util.Chance(chancePercent);
        }

        public double GetPseudoDouble(RandomContext ctx)
        {
            return Util.RandomDouble();
        }

        public double GetPseudoDoubleIncl(RandomContext ctx)
        {
            return Util.RandomDoubleIncl();
        }
    }

    public sealed class DeckRandomProvider : IRandomProvider
    {
        private readonly Dictionary<RandomDeckKey, RandomDeck> _randomDecks = new();
        private readonly Lock _randomDecksLock = new();

        private RandomDeck GetOrCreateDeck(RandomContext ctx)
        {
            RandomDeckKey key = new(ctx.RandomEvent, ctx.SequenceIndex);

            lock (_randomDecksLock)
            {
                if (!_randomDecks.TryGetValue(key, out RandomDeck deck))
                {
                    deck = new();
                    _randomDecks[key] = deck;
                }

                return deck;
            }
        }

        public bool Chance(RandomContext ctx, int chancePercent)
        {
            if (ctx.RandomPolicy is RandomPolicy.ForceTrueRandom)
                return DefaultRandomProvider.Instance.Chance(ctx, chancePercent);

            return GetOrCreateDeck(ctx).Draw() < chancePercent;
        }

        public bool Chance(RandomContext ctx, double chance)
        {
            return GetPseudoDouble(ctx) < chance;
        }

        public double GetPseudoDouble(RandomContext ctx)
        {
            if (ctx.RandomPolicy is RandomPolicy.ForceTrueRandom)
                return DefaultRandomProvider.Instance.GetPseudoDouble(ctx);

            return (GetOrCreateDeck(ctx).Draw() + Util.RandomDouble()) * 0.01;
        }

        public double GetPseudoDoubleIncl(RandomContext ctx)
        {
            if (ctx.RandomPolicy is RandomPolicy.ForceTrueRandom)
                return DefaultRandomProvider.Instance.GetPseudoDoubleIncl(ctx);

            return (GetOrCreateDeck(ctx).Draw() + Util.RandomDoubleIncl()) * 0.01;
        }

        private readonly record struct RandomDeckKey(RandomEvent Event, byte SequenceIndex);
    }

    public static class RandomProviderFactory
    {
        public static IRandomProvider GetDefaultRandomProvider()
        {
            return DefaultRandomProvider.Instance;
        }

        public static IRandomProvider GetDeckRandomProvider()
        {
            return Properties.OVERRIDE_DECK_RNG ? GetDefaultRandomProvider() : new DeckRandomProvider();
        }
    }
}
