using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;

namespace DOL.GS.Utils
{
    public class PlayerDeck
    {
        private const int DECKS_COUNT = 1;
        private const int CARDS_PER_DECK_COUNT = 100;

        private ConcurrentStack<int> _cards = new();
        private object _lock = new();

        public PlayerDeck()
        {
            lock (_lock)
            {
                InitializeDeck();
            }
        }

        private void InitializeDeck()
        {
            _cards.Clear();
            int[] tempCards = new int[DECKS_COUNT * CARDS_PER_DECK_COUNT];

            for (int i = 0; i < DECKS_COUNT; i++)
            {
                for (int j = 0; j < CARDS_PER_DECK_COUNT; j++)
                    tempCards[j + i * CARDS_PER_DECK_COUNT] = j;
            }

            // Fisher-Yates shuffle algorithm.
            // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
            for (int i = tempCards.Length - 1; i > 0; i--)
            {
                int j = Util.CryptoNextInt(i + 1);
                (tempCards[j], tempCards[i]) = (tempCards[i], tempCards[j]);
            }

            foreach (int card in tempCards)
                _cards.Push(card);
        }

        private int Pop()
        {
            int result;

            while (!_cards.TryPop(out result))
            {
                lock (_lock)
                {
                    if (_cards.IsEmpty)
                        InitializeDeck();
                }
            }

            return result;
        }

        public int GetInt()
        {
            return Pop();
        }

        public double GetPseudoDouble()
        {
            // Just use a simple random for the fractional digits.
            return (Pop() + Util.CryptoNextDouble()) / 100.0;
        }

        public string SaveDeckToJSON()
        {
            lock (_lock)
            {
                return JsonConvert.SerializeObject(_cards.Reverse());
            }
        }

        public void LoadDeckFromJSON(string json)
        {
            lock (_lock)
            {
                _cards = JsonConvert.DeserializeObject<ConcurrentStack<int>>(json);
            }
        }
    }
}
