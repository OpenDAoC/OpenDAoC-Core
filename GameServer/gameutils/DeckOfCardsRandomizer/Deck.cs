using System;
using System.Collections.Concurrent;
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
            _player = player;

            if (TryUsePreLoadedDeck())
                return;

            DbCoreCharacterXDeck dbCoreCharacterXDeck;

            if (TryLoadExistingDeck())
                return;

            InitializeDeck();

            bool TryUsePreLoadedDeck()
            {
                if (_player.DBCharacter.RandomNumberDeck == null)
                    return false;

                _cards = JsonConvert.DeserializeObject<ConcurrentStack<int>>(_player.DBCharacter.RandomNumberDeck.Deck);
                return true;
            }

            bool TryLoadExistingDeck()
            {
                dbCoreCharacterXDeck = DOLDB<DbCoreCharacterXDeck>.SelectObject(DB.Column("DOLCharactersObjectId").IsEqualTo(player.ObjectId));

                if (dbCoreCharacterXDeck == null)
                    return false;

                _player.DBCharacter.RandomNumberDeck = dbCoreCharacterXDeck;
                _cards = JsonConvert.DeserializeObject<ConcurrentStack<int>>(_player.DBCharacter.RandomNumberDeck.Deck);
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
            return (Pop() + Util.CryptoNextDouble()) / 100.0;
        }

        public void SaveDeck()
        {
            if (_player == null)
                return;

            DbCoreCharacterXDeck dbCoreCharacterXDeck = _player.DBCharacter.RandomNumberDeck;

            if (dbCoreCharacterXDeck == null)
            {
                dbCoreCharacterXDeck = new()
                {
                    DOLCharactersObjectId = _player.ObjectId,
                    Deck = SerializeCards()
                };
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
                    int j = Util.CryptoNextInt(i + 1);
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
