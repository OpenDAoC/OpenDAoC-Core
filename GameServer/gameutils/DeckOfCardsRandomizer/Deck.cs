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

            //Fisher-Yates shuffle algorithm
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
            int first = Pop();
            int second = Util.CryptoNextInt(100); //just use a simple random for the .XX values
            double result = first + second / 100.0;
            result /= 100.0;
            return result;
        }

        public string SaveDeckToJSON()
        {
            lock (_lock)
            {
                string json = JsonConvert.SerializeObject(_cards.Reverse());
                return json;
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
