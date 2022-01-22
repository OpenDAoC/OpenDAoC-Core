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

    public int GetCard()
    {
        if (_cards.Count == 0)
            ResetDeck();

        return _cards.Pop();
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