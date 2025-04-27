using UnityEngine;

/// <summary>
/// Represents a playing card with rank, suit, and associated sprite for rendering.
/// </summary>
public class Card
{
    /// <summary>
    /// Numeric rank of the card: 2–10 for pip cards, 11=J, 12=Q, 13=K, 14=A.
    /// </summary>
    public int Rank;

    /// <summary>
    /// Suit of the card (hearts, diamonds, clubs, spades).
    /// </summary>
    public Suit Suit;

    /// <summary>
    /// Identifier for the sprite asset, e.g. "card_spades_A".
    /// </summary>
    public string spriteName;

    /// <summary>
    /// Constructs a card given a rank, suit, and optional sprite name.
    /// If no spriteName is provided, one is generated using the rank and suit.
    /// </summary>
    /// <param name="rank">Numeric rank: 2–14 (where 11=J, 12=Q, 13=K, 14=A).</param>
    /// <param name="suit">Suit enum value.</param>
    /// <param name="spriteName">Optional custom sprite identifier.</param>
    public Card(int rank, Suit suit, string spriteName = null)
    {
        Rank = rank;
        Suit = suit;
        string suitStr = suit.ToString();
        string rankStr = rank switch
        {
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _  => rank.ToString()
        };
        this.spriteName = spriteName ?? $"card_{suitStr}_{rankStr}";
    }

    /// <summary>
    /// Constructs a placeholder card from a sprite name only (used for face-down cards).
    /// Rank and Suit are left at defaults.
    /// </summary>
    /// <param name="spriteName">Sprite identifier for the card image.</param>
    public Card(string spriteName)
    {
        Rank = 0;
        Suit = Suit.clubs;
        this.spriteName = spriteName;
    }

    /// <summary>
    /// Loads and returns the Sprite asset for this card from Resources/Cards.
    /// </summary>
    /// <returns>The loaded Sprite, or null if not found.</returns>
    public Sprite ToSprite()
    {
        return Resources.Load<Sprite>($"Cards/{spriteName}");
    }
}
