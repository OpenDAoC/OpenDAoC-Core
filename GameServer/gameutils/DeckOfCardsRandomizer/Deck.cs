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
    private const int PLAYER_DECK_SIZE = 500;

    private Stack<int> _cards = new Stack<int>(PLAYER_DECK_SIZE);

    public PlayerDeck()
    {
        ResetDeck();
    }

    private void InitializeDeck()
    {
        _cards.Clear();

        for (int i = 0; i < PLAYER_DECK_SIZE; i++)
        {
            //mod by 100 to only generate numbers 0-99
            //then offset by 1 to only generate 'cards' with values 1-100
            _cards.Push((i % 100) + 1);
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
        //randomly order the contents of the array, then reassign the array
        int[] shuffled = _cards.ToArray().OrderBy(x => Util.CryptoNextInt(PLAYER_DECK_SIZE-1)).ToArray();
        _cards.Clear();
        foreach (var i in shuffled)
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
        int second = _cards.Pop() - 1;

        //append our ints together
        //if we are unable to parse numbers for any reason, use a 0
        int append;
        if (!int.TryParse(first.ToString() + second.ToString(), out append)) append = 0;
            
        
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