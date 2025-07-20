using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.Database;
using Newtonsoft.Json;

namespace DOL.GS.Utils
{
    public class PlayerDeck
    {
        private const int DECKS_COUNT = 1;             // The final deck is a concatenation of 1 or more decks.
        private const int CARDS_PER_DECK_COUNT = 100;  // Should not be changed, this controls the max value of the cards too.
        private const int NUM_BUCKETS = 10;            // Amount of bucket in a deck. Should not be too high. Used for the anti-cluster mechanism.
        private const double AGGRESSIVENESS = 0.05;    // Higher aggressiveness forces cards to be more separated by value. Used for the anti-cluster mechanism.

        private GamePlayer _player;
        private int[] _cards = new int[DECKS_COUNT * CARDS_PER_DECK_COUNT];
        private int _index = DECKS_COUNT * CARDS_PER_DECK_COUNT;
        private List<int>[] _buckets = new List<int>[NUM_BUCKETS];
        private long[] _bucketSums = new long[NUM_BUCKETS];
        private double[] _bucketWeights = new double[NUM_BUCKETS];
        private readonly Lock _cardsLock = new();

        public PlayerDeck(GamePlayer player)
        {
            for (int i = 0; i < NUM_BUCKETS; i++)
                _buckets[i] = new();

            _player = player;

            if (TryUsePreLoadedDeck())
                return;

            if (TryLoadExistingDeck())
                return;

            InitializeDeck();

            bool TryUsePreLoadedDeck()
            {
                // We only want one deck, even if for some reason multiple rows are returned or were loaded.
                DbCoreCharacterXDeck dbDeck = _player.DBCharacter.RandomNumberDeck?.FirstOrDefault();
                return DeserializeCards(dbDeck);
            }

            bool TryLoadExistingDeck()
            {
                IList<DbCoreCharacterXDeck> dbCoreCharacterXDecks = DOLDB<DbCoreCharacterXDeck>.SelectObjects(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId));

                if (dbCoreCharacterXDecks == null || dbCoreCharacterXDecks.Count == 0)
                    return false;

                // We only want one deck, even if for some reason multiple rows are returned or were loaded.
                _player.DBCharacter.RandomNumberDeck = dbCoreCharacterXDecks.ToArray();
                return DeserializeCards(dbCoreCharacterXDecks[0]);
            }

            bool DeserializeCards(DbCoreCharacterXDeck dbDeck)
            {
                string serializedDeck = dbDeck.Deck;

                if (string.IsNullOrEmpty(serializedDeck))
                    return false;

                int[] deserialized = JsonConvert.DeserializeObject<int[]>(serializedDeck);

                if (deserialized == null || deserialized.Length == 0)
                    return false;

                // Place the deserialized array at the end of the deck.
                int startIndex = _cards.Length - Math.Min(deserialized.Length, _cards.Length);
                Array.Copy(deserialized, 0, _cards, startIndex, Math.Min(deserialized.Length, _cards.Length));
                _index = startIndex;
                return true;
            }
        }

        public int GetInt()
        {
            return Pop();
        }

        public double GetPseudoDouble()
        {
            // Just use a simple random for the fractional digits.
            return (Pop() + Util.RandomDouble()) / 100.0;
        }

        public void SaveDeck()
        {
            if (_player == null)
                return;

            DbCoreCharacterXDeck dbCoreCharacterXDeck = _player.DBCharacter.RandomNumberDeck?.FirstOrDefault();

            if (dbCoreCharacterXDeck == null)
            {
                dbCoreCharacterXDeck = new()
                {
                    DOLCharactersObjectId = _player.ObjectId,
                    Deck = SerializeCards()
                };
                _player.DBCharacter.RandomNumberDeck = [dbCoreCharacterXDeck];
            }
            else
                dbCoreCharacterXDeck.Deck = SerializeCards();

            if (dbCoreCharacterXDeck?.IsPersisted == true)
                GameServer.Database.SaveObject(dbCoreCharacterXDeck);
            else
                GameServer.Database.AddObject(dbCoreCharacterXDeck);

            string SerializeCards()
            {
                lock (_cardsLock)
                {
                    // Only serialize the remaining cards from _index to the end.
                    int remaining = _cards.Length - _index;
                    int[] cardsToSave = new int[remaining];
                    Array.Copy(_cards, _index, cardsToSave, 0, remaining);
                    return JsonConvert.SerializeObject(cardsToSave);
                }
            }
        }

        private void InitializeDeck()
        {
            lock (_cardsLock)
            {
                // Build a new deck.
                for (int i = 0; i < DECKS_COUNT; i++)
                {
                    for (int j = 0; j < CARDS_PER_DECK_COUNT; j++)
                        _cards[i + j] = j;
                }

                // Use the Fisher-Yates shuffle to randomize the order of card placement. This is critical to prevent bias.
                for (int i = _cards.Length - 1; i > 0; i--)
                {
                    int j = Util.Random(i);
                    (_cards[j], _cards[i]) = (_cards[i], _cards[j]);
                }

                // Clear various arrays for the next step.
                for (int i = 0; i < _buckets.Length; i++)
                {
                    _buckets[i].Clear();
                    _bucketSums[i] = 0;
                }

                // Value-aware distribution.
                foreach (int card in _cards)
                {
                    // Calculate the "attraction score" and weight for each bucket.
                    for (int i = 0; i < NUM_BUCKETS; i++)
                    {
                        // Use a neutral midpoint for empty buckets.
                        double average = _buckets[i].Count == 0 ? 50.0 : (double) _bucketSums[i] / _buckets[i].Count;
                        double attractionScore = Math.Abs(average - card);
                        _bucketWeights[i] = Math.Exp(attractionScore * AGGRESSIVENESS);
                    }

                    // Perform a weighted random choice to select a bucket.
                    int chosenIndex = 0;
                    double totalWeight = _bucketWeights.Sum();

                    if (totalWeight > 0)
                    {
                        double roll = Util.RandomDouble() * totalWeight;

                        for (int i = 0; i < NUM_BUCKETS; i++)
                        {
                            if (roll < _bucketWeights[i])
                            {
                                chosenIndex = i;
                                break;
                            }

                            roll -= _bucketWeights[i];
                        }
                    }
                    else
                        chosenIndex = Util.Random(NUM_BUCKETS - 1); // Fallback to random choice if all weights are zero.

                    // Add the card to the chosen bucket and update its sum.
                    _buckets[chosenIndex].Add(card);
                    _bucketSums[chosenIndex] += card;
                }

                // Repopulate the deck.
                int index = 0;

                foreach (var bucket in _buckets)
                {
                    foreach (int card in bucket)
                        _cards[index++] = card;
                }
            }
        }

        private int Pop()
        {
            int result;

            lock (_cardsLock)
            {
                // If we've exhausted the deck, re-initialize and reset index.
                if (_index >= _cards.Length)
                {
                    InitializeDeck();
                    _index = 0;
                }

                result = _cards[_index];
                _index++;
            }

            return result;
        }
    }
}
