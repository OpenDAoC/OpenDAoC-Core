using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.Database;
using Newtonsoft.Json;

namespace DOL.GS.Utils
{
    public class PlayerDeck
    {
        private const int DECKS_COUNT = 1;
        private const int CARDS_PER_DECK_COUNT = 100;

        private GamePlayer _player;
        private ConcurrentStack<int> _cards = new();
        private readonly Lock _cardsLock = new();

        public PlayerDeck()
        {
            InitializeDeck();
        }

        public PlayerDeck(GamePlayer player)
        {
            // We only want one deck even if for some reason multiple rows are returned or were loaded.

            _player = player;

            if (TryUsePreLoadedDeck())
                return;

            if (TryLoadExistingDeck())
                return;

            InitializeDeck();

            bool TryUsePreLoadedDeck()
            {
                string serializedDeck = _player.DBCharacter.RandomNumberDeck?.FirstOrDefault()?.Deck;

                if (string.IsNullOrEmpty(serializedDeck))
                    return false;

                DeserializeCards(serializedDeck);
                return true;
            }

            bool TryLoadExistingDeck()
            {
                IList<DbCoreCharacterXDeck> dbCoreCharacterXDecks = DOLDB<DbCoreCharacterXDeck>.SelectObjects(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId));

                if (dbCoreCharacterXDecks == null || dbCoreCharacterXDecks.Count == 0)
                    return false;

                _player.DBCharacter.RandomNumberDeck = dbCoreCharacterXDecks.ToArray();
                string serializedDeck = dbCoreCharacterXDecks[0].Deck;

                if (string.IsNullOrEmpty(serializedDeck))
                    return false;

                DeserializeCards(serializedDeck);
                return true;
            }

            void DeserializeCards(string serializedDeck)
            {
                _cards = JsonConvert.DeserializeObject<ConcurrentStack<int>>(serializedDeck);
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
                    return JsonConvert.SerializeObject(_cards.Reverse());
                }
            }
        }

        private void InitializeDeck()
        {
            lock (_cardsLock)
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
                    int j = Util.Random(i);
                    (tempCards[j], tempCards[i]) = (tempCards[i], tempCards[j]);
                }

                foreach (int card in tempCards)
                    _cards.Push(card);
            }
        }

        private int Pop()
        {
            int result;

            lock (_cardsLock)
            {
                while (!_cards.TryPop(out result))
                {
                    if (_cards.IsEmpty)
                        InitializeDeck();
                }
            }

            return result;
        }
    }
}
