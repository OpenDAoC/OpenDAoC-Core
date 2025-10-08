using System;
using System.Buffers;
using System.Threading;

namespace DOL.GS
{
    public class RandomDeck
    {
        private const int NUM_BUCKETS = 10; // Amount of bucket in a deck. Affects performance a lot, and should not be too high.
        private const int NUM_STRATA = 10;  // Must evenly divide CARD_RANGE.
        private const int CARD_MIN = 0;
        private const int CARD_MAX = 99;
        private const int CARD_RANGE = CARD_MAX - CARD_MIN + 1;
        private const int STRATUM_SIZE = CARD_RANGE / NUM_STRATA;

        private readonly int[] _cards = new int[CARD_RANGE];
        private int _index = CARD_RANGE;    // Forces initialization on first draw.
        private readonly Lock _cardsLock = new();

        public RandomDeck()
        {
            // Pre-initialize the deck with all possible cards.
            for (int i = CARD_MIN; i < CARD_MAX + 1; i++)
                _cards[i] = i;
        }

        public int Draw()
        {
            lock (_cardsLock)
            {
                if (_index >= _cards.Length)
                {
                    InitializeDeck();
                    _index = 0;
                }

                return _cards[_index++];
            }
        }

        private void InitializeDeck()
        {
            CardAssignment[] cardAssignments = ArrayPool<CardAssignment>.Shared.Rent(_cards.Length);
            double[] bucketWeights = ArrayPool<double>.Shared.Rent(NUM_BUCKETS);
            int[] bucketCounts = ArrayPool<int>.Shared.Rent(NUM_BUCKETS);
            int[] bucketNextWriteIndices = ArrayPool<int>.Shared.Rent(NUM_BUCKETS);
            int[] bucketStratumCounts = ArrayPool<int>.Shared.Rent(NUM_BUCKETS * NUM_STRATA);

            try
            {
                // Clear arrays that require it.
                Array.Clear(bucketCounts, 0, NUM_BUCKETS);
                Array.Clear(bucketStratumCounts, 0, NUM_BUCKETS * NUM_STRATA);

                // Shuffle the cards. This ensures the order of processing cards is random.
                for (int i = _cards.Length - 1; i > 0; i--)
                {
                    int j = Util.Random(i);
                    (_cards[j], _cards[i]) = (_cards[i], _cards[j]);
                }

                // Distribute cards into buckets using weighted distribution, preferring buckets with averages further away from the card value.
                for (int i = 0; i < _cards.Length; i++)
                {
                    int card = _cards[i];
                    int stratumIndex = (card - CARD_MIN) / STRATUM_SIZE;
                    double totalWeight = 0;

                    // Calculate weight for each bucket.
                    for (int j = 0; j < NUM_BUCKETS; j++)
                    {
                        int countInBucket = bucketStratumCounts[j * NUM_STRATA + stratumIndex];
                        double baseWeight = 1.0 / (countInBucket + 1.0);
                        double weight = baseWeight * baseWeight * baseWeight;
                        bucketWeights[j] = weight;
                        totalWeight += weight;
                    }

                    // Perform weighted random choice.
                    double cumulativeWeight = 0;
                    double randValue = Util.RandomDouble() * totalWeight;

                    for (int j = 0; j < NUM_BUCKETS; j++)
                    {
                        cumulativeWeight += bucketWeights[j];

                        if (randValue > cumulativeWeight)
                            continue;

                        // Record the assignment and update counts for the next card.
                        cardAssignments[i] = new(card, j);
                        bucketCounts[j]++;
                        bucketStratumCounts[j * NUM_STRATA + stratumIndex]++;
                        break;
                    }
                }

                bucketNextWriteIndices[0] = 0;

                // Calculate the starting write indices for each bucket
                int currentWriteIndex = 0;

                for (int i = 0; i < NUM_BUCKETS; i++)
                {
                    bucketNextWriteIndices[i] = currentWriteIndex;
                    currentWriteIndex += bucketCounts[i];
                }

                // Place cards into their final positions.
                for (int i = 0; i < _cards.Length; i++)
                {
                    ref readonly CardAssignment assignment = ref cardAssignments[i];
                    int bucketIndex = assignment.BucketIndex;
                    int writeIndex = bucketNextWriteIndices[bucketIndex];
                    _cards[writeIndex] = assignment.Card;
                    bucketNextWriteIndices[bucketIndex]++;
                }
            }
            finally
            {
                ArrayPool<CardAssignment>.Shared.Return(cardAssignments);
                ArrayPool<double>.Shared.Return(bucketWeights);
                ArrayPool<int>.Shared.Return(bucketCounts);
                ArrayPool<int>.Shared.Return(bucketNextWriteIndices);
                ArrayPool<int>.Shared.Return(bucketStratumCounts);
            }
        }

        private readonly struct CardAssignment
        {
            public readonly int Card;
            public readonly int BucketIndex;

            public CardAssignment(int card, int bucketIndex)
            {
                Card = card;
                BucketIndex = bucketIndex;
            }
        }
    }

    public enum RandomDeckEvent
    {
        Intercept,              // Primarily used by Spiritmaster pets.
        Evade,
        Parry,
        Block,                  // Includes guard.
        Miss,                   // Physical and magical attacks (resists).
        DualWield,              // Off-hand attacks for CD/DW/H2H.
        OffensiveProcChance,    // Weapon and spell based offensive procs.
        DefensiveProcChance,    // Armor and spell based defensive procs.
        DamageVariance,         // Physical and magical attacks, heals.
        CriticalChance,         // Physical and magical attacks, heals, DoTs, debuffs.
        CriticalVariance        // Physical and magical attacks, heals, DoTs, debuffs.
    }
}
