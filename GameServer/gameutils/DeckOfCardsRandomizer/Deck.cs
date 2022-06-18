using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json.Serialization;
using DOL.GS;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DOL.GS.Utils;

public class PlayerDeck
{
    private const int NUM_BONUS_DECKS = 1;
    private const int NUM_NORMAL_DECKS = 1;
    private const int BONUS_DECK_SIZE = 25;
    private const int PLAYER_DECK_SIZE = NUM_NORMAL_DECKS * 100 + NUM_BONUS_DECKS * BONUS_DECK_SIZE;

    private Stack<int> _cards;

    public PlayerDeck()
    {
        _cards = new Stack<int>();
        ResetDeck();
    }

    private void InitializeDeck()
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

    private void ResetDeck()
    {
        InitializeDeck();
        //shuffle thrice for good luck?
        Shuffle();
        Shuffle();
        Shuffle();
        //Console.WriteLine($"deck reset");
    }
    
    private void Shuffle()
    {
        int[] preshuffle = _cards.ToArray();
        //Fisher-Yates shuffle algorithm
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        int cardsLength = _cards.Count;
        while (cardsLength > 1)
        {
            cardsLength--;
            int k = Util.CryptoNextInt(cardsLength + 1);
            var currentItem = preshuffle[k];
            preshuffle[k] = preshuffle[cardsLength];
            preshuffle[cardsLength] = currentItem;
        }
        /*
        //randomly order the contents of the array, then reassign the array
        int[] shuffled = _cards.ToArray().OrderBy(x => Util.CryptoNextInt(cardsLength-1)).ToArray();
        */
        _cards.Clear();
        foreach (var i in preshuffle)
        {
            _cards.Push(i);
        }
    }

    public int GetInt()
    {
        if (_cards.Count == 0)
            ResetDeck();

        return _cards.Pop();
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
        int first = _cards.Pop() - 1;
        int second = Util.CryptoNextInt(99); //just use a simple random for the .XX values

        //append our ints together
        //if we are unable to parse numbers for any reason, use a 0
        int append;
        if (!int.TryParse(first.ToString() + second.ToString("D2"), out append)) append = 0;
            
        
        //divide by max possible value to simulate 0-1 output of doubles
        double pseudoDouble = append / (double)9999;
        return pseudoDouble;
    }

    public string SaveDeckToJSON()
    {
        string json = JsonConvert.SerializeObject(_cards.Reverse());
        return json;
    }

    public void LoadDeckFromJSON(string json)
    {
        _cards = JsonConvert.DeserializeObject<Stack<int>>(json);
    }
}