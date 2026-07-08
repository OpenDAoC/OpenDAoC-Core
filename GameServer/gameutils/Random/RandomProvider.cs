using System;
using System.Collections.Generic;
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
        private readonly Dictionary<RandomEvent, RandomDeck> _randomDecks = new();

        public DeckRandomProvider()
        {
            InitializeRandomDecks();
        }

        public bool Chance(RandomContext ctx, int chancePercent)
        {
            if (ctx.RandomPolicy is RandomPolicy.ForceTrueRandom || !_randomDecks.TryGetValue(ctx.RandomEvent, out RandomDeck deck))
                return DefaultRandomProvider.Instance.Chance(ctx, chancePercent);

            return deck.Draw() < chancePercent;
        }

        public bool Chance(RandomContext ctx, double chance)
        {
            return GetPseudoDouble(ctx) < chance;
        }

        public double GetPseudoDouble(RandomContext ctx)
        {
            if (ctx.RandomPolicy is RandomPolicy.ForceTrueRandom || !_randomDecks.TryGetValue(ctx.RandomEvent, out RandomDeck deck))
                return DefaultRandomProvider.Instance.GetPseudoDouble(ctx);

            return (deck.Draw() + Util.RandomDouble()) * 0.01;
        }

        public double GetPseudoDoubleIncl(RandomContext ctx)
        {
            if (ctx.RandomPolicy is RandomPolicy.ForceTrueRandom || !_randomDecks.TryGetValue(ctx.RandomEvent, out RandomDeck deck))
                return DefaultRandomProvider.Instance.GetPseudoDoubleIncl(ctx);

            return (deck.Draw() + Util.RandomDoubleIncl()) * 0.01;
        }

        public void InitializeRandomDecks()
        {
            foreach (RandomEvent deckEvent in Enum.GetValues<RandomEvent>())
                _randomDecks[deckEvent] = new();
        }
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
