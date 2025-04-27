using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a standard 52-card deck for Texas Hold'em, with
/// methods to reset, shuffle, and draw cards.
/// </summary>
public class Deck
{
    /// <summary>
    /// The list of remaining cards in the deck, with index 0 as the top.
    /// </summary>
    private List<Card> cards;

    /// <summary>
    /// Constructs a new Deck instance and initializes it to a shuffled 52-card deck.
    /// </summary>
    public Deck()
    {
        Reset();
    }

    /// <summary>
    /// Reinitializes this deck to a full 52-card set and shuffles it.
    /// </summary>
    public void Reset()
    {
        cards = new List<Card>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            for (int rank = 2; rank <= 14; rank++)
            {
                // e.g. "A♠" becomes "AS", or "10H" etc.
                string spriteName = GetSpriteName(rank, suit);
                cards.Add(new Card(rank, suit, spriteName));
            }
        }
        Shuffle();
    }

    /// <summary>
    /// Shuffles the deck using the Fisher–Yates algorithm.
    /// </summary>
    public void Shuffle()
    {
        int n = cards.Count;
        for (int i = 0; i < n; i++)
        {
            int r = UnityEngine.Random.Range(i, n);
            var tmp = cards[i];
            cards[i] = cards[r];
            cards[r] = tmp;
        }
    }

    /// <summary>
    /// Draws the top card from the deck, removing it.
    /// Logs a warning and returns null if the deck is empty.
    /// </summary>
    /// <returns>The next Card, or null if none remain.</returns>
    public Card Draw()
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning("Deck is empty!");
            return null;
        }
        var top = cards[0];
        cards.RemoveAt(0);
        return top;
    }

    /// <summary>
    /// Helper to build a sprite name based on rank and suit.
    /// </summary>
    /// <param name="rank">Numeric rank 2-14 (where 11=J, 12=Q,13=K,14=A).</param>
    /// <param name="suit">Enum value for the card suit.</param>
    /// <returns>String identifier for the card sprite resource.</returns>
    private string GetSpriteName(int rank, Suit suit)
    {
        string rankStr = rank switch
        {
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _  => rank.ToString()
        };
        string suitStr = suit.ToString();
        return $"card_{suitStr}_{rankStr}";
    }
}
