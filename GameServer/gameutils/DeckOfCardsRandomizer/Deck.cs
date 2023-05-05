using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;

namespace DOL.GS.Utils;

public class PlayerDeck
{
    private const int NUM_BONUS_DECKS = 1;
    private const int NUM_NORMAL_DECKS = 1;
    private const int BONUS_DECK_SIZE = 25;
    private const int PLAYER_DECK_SIZE = NUM_NORMAL_DECKS * 100 + NUM_BONUS_DECKS * BONUS_DECK_SIZE;

    private ConcurrentStack<int> _cards;

    public PlayerDeck()
    {
        _cards = new ConcurrentStack<int>();
        ResetDeck();
    }

    private void InitializeDeck()
    {
        lock (_cardLock)
        {
            _cards.Clear();

            for (int i = 0; i < NUM_NORMAL_DECKS; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    _cards.Push(j+1); //offset by 1 to only generate 'cards' with values 1-100
                }
            }

            for (int i = 0; i < NUM_BONUS_DECKS; i++)
            {
                for (int j = (100-BONUS_DECK_SIZE); j < 100; j++)
                {
                    //add a "bonus deck" of high numbers X-99
                    _cards.Push(j);
                }
            }
        }
    }

    private void ResetDeck()
    {
        InitializeDeck();
        //shuffle thrice for good luck?
        Shuffle();
        Shuffle();
        Shuffle();
        //Console.WriteLine($"deck reset");
    }

    private object _cardLock = new object();
    
    private void Shuffle()
    {
        int[] preshuffle = null;
        lock (_cardLock)
        {
            preshuffle = _cards.ToArray();
        }

        //Fisher-Yates shuffle algorithm
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        int cardsLength = _cards.Count - 1;
        while (cardsLength > 1)
        {
            cardsLength--;
            int k = Util.CryptoNextInt(cardsLength + 1);
            (preshuffle[k], preshuffle[cardsLength]) = (preshuffle[cardsLength], preshuffle[k]);
        }
        /*
        //randomly order the contents of the array, then reassign the array
        int[] shuffled = _cards.ToArray().OrderBy(x => Util.CryptoNextInt(cardsLength-1)).ToArray();
        */
        lock (_cardLock)
        {
            _cards.Clear();
            foreach (var i in preshuffle)
            {
                _cards.Push(i);
            }
        }
    }

    public int GetInt()
    {
        if (_cards.Count < 2)
            ResetDeck();

        _cards.TryPop(out int output);
        return output;
    }

    public double GetPseudoDouble()
    {
        if (_cards.Count < 2)
        {
            ResetDeck();
            Shuffle(); //shuffle it for fun
        }

        //we append two ints together to simulate more accuracy on the double
        //subtract 1 to only generate values 0-99
        //useful to get outputs of 0-9999 instead of 11-100100

        _cards.TryPop(out int first);
        first--;
        int second = Util.CryptoNextInt(100); //just use a simple random for the .XX values

        //append our ints together
        //if we are unable to parse numbers for any reason, use a 0
        if (!int.TryParse(first.ToString() + second.ToString("D2"), out int append))
            append = 0;

        //divide by max possible value to simulate 0-1 output of doubles
        double pseudoDouble = append / 9999.0;
        return pseudoDouble;
    }

    public string SaveDeckToJSON()
    {
        string json = JsonConvert.SerializeObject(_cards.Reverse());
        return json;
    }

    public void LoadDeckFromJSON(string json)
    {
        _cards = JsonConvert.DeserializeObject<ConcurrentStack<int>>(json);
    }
}