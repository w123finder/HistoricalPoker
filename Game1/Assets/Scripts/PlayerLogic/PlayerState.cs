using UnityEngine;

/// <summary>
/// Represents the full state of a single player in the poker game,
/// including their name, chip count, current hand, and status flags.
/// </summary>
public class PlayerState
{
    /// <summary>
    /// The display name of the player (e.g., "You" or AI names).
    /// </summary>
    public string name;

    /// <summary>
    /// The total chips the player currently has available.
    /// </summary>
    public int chips;

    /// <summary>
    /// The two hole cards dealt to the player for the current hand.
    /// </summary>
    public Card[] hole = new Card[2];

    /// <summary>
    /// Whether it is currently this player's turn to act.
    /// </summary>
    public bool isActive = false;

    /// <summary>
    /// Tracks if the player has folded in the current betting round.
    /// </summary>
    public bool hasFolded = false;

    /// <summary>
    /// The amount the player has put into the pot during this betting round.
    /// Resets to zero at the start of each new round.
    /// </summary>
    public int currentBet = 0;

    /// <summary>
    /// The current emotional expression of the player's avatar,
    /// e.g., "happy", "sad", "deceitful", etc., for UI animations.
    /// </summary>
    public string expression;

    /// <summary>
    /// Indicates whether the player has gone bust (lost all their chips)
    /// at any point in the game. Once set, they no longer participate.
    /// </summary>
    public bool hasGoneBust;
}