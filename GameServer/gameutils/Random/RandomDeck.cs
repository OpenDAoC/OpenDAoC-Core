using System;
using System.Buffers;
using System.Threading;

namespace DOL.GS
{
    public class RandomDeck
    {
        private const int NUM_BUCKETS = 10;                       // Number of buckets in a deck. Affects performance a lot, and should not be too high.
        private const int NUM_STRATA = 10;                        // Must evenly divide CARD_RANGE.
        private const int CARD_MIN = 0;
        private const int CARD_MAX = 99;
        private const int CARD_RANGE = CARD_MAX - CARD_MIN + 1;
        private const int STRATUM_SIZE = CARD_RANGE / NUM_STRATA; // Should be equal to NUM_BUCKETS for each bucket to receive exactly one card per stratum.

        private readonly int[] _cards = new int[CARD_RANGE];
        private int _index = CARD_RANGE;                          // Forces initialization on first draw.
        private readonly Lock _cardsLock = new();

        public RandomDeck()
        {
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
            int[] bucketCounts = ArrayPool<int>.Shared.Rent(NUM_BUCKETS);
            int[] bucketNextWriteIndices = ArrayPool<int>.Shared.Rent(NUM_BUCKETS);
            int[] stratumBags = ArrayPool<int>.Shared.Rent(NUM_STRATA * NUM_BUCKETS);
            int[] stratumBagPositions = ArrayPool<int>.Shared.Rent(NUM_STRATA);

            try
            {
                Array.Clear(bucketCounts, 0, NUM_BUCKETS);
                Array.Clear(stratumBagPositions, 0, NUM_STRATA);

                // Fill and shuffle the bags.
                // As cards from a stratum are processed, bucket indices are drawn from that stratum's shuffled bag.
                // When STRATUM_SIZE == NUM_BUCKETS, each bag is consumed exactly once, ensuring every bucket receives exactly one card from each stratum.
                for (int i = 0; i < NUM_STRATA; i++)
                    RefillBag(stratumBags, i);

                // Process cards in a random order.
                Shuffle(_cards, 0, _cards.Length);

                for (int i = 0; i < _cards.Length; i++)
                {
                    int card = _cards[i];
                    int stratumIndex = (card - CARD_MIN) / STRATUM_SIZE;

                    // If we've exhausted the bag for this stratum (happens if STRATUM_SIZE > NUM_BUCKETS), refill it.
                    if (stratumBagPositions[stratumIndex] >= NUM_BUCKETS)
                    {
                        RefillBag(stratumBags, stratumIndex);
                        stratumBagPositions[stratumIndex] = 0;
                    }

                    int bucket = stratumBags[stratumIndex * NUM_BUCKETS + stratumBagPositions[stratumIndex]];
                    stratumBagPositions[stratumIndex]++;

                    cardAssignments[i] = new(card, bucket);
                    bucketCounts[bucket]++;
                }

                // Calculate the starting write indices for each bucket.
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
                ArrayPool<int>.Shared.Return(bucketCounts);
                ArrayPool<int>.Shared.Return(bucketNextWriteIndices);
                ArrayPool<int>.Shared.Return(stratumBags);
                ArrayPool<int>.Shared.Return(stratumBagPositions);
            }
        }

        private static void RefillBag(int[] stratumBags, int stratumIndex)
        {
            int offset = stratumIndex * NUM_BUCKETS;

            for (int i = 0; i < NUM_BUCKETS; i++)
                stratumBags[offset + i] = i;

            Shuffle(stratumBags, offset, NUM_BUCKETS);
        }

        private static void Shuffle(int[] array, int offset, int length)
        {
            for (int i = length - 1; i > 0; i--)
            {
                int j = Util.Random(i);
                (array[offset + j], array[offset + i]) = (array[offset + i], array[offset + j]);
            }
        }

        private readonly record struct CardAssignment(int Card, int BucketIndex);
    }
}
