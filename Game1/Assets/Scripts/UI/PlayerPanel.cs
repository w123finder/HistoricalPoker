using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles in-game chat between the human player and AI opponents.
/// Instantiates chat bubbles, maintains chat history, and triggers AI responses.
/// </summary>
[System.Serializable]
public class PlayerPanel {
    public Image avatarImage;  // Image of the player
    public Image card1, card2; // The two cards in the players possesion 
    public TextMeshProUGUI nameText, chipsText, betCount; // Name, num chips and current bet amount
    public Image highlightBorder; // Highlight border indicating player turn
    public GameObject turnIndicator; // Arrow indicating player turn
    public string expression;  // Current emotion of the Player 
    // possible emotions: (happy, sad, deceitful, neutral, inquisitive, happy_bluff, sad_bluff)

    /// <summary>
    /// Syncs the UI panel to reflect the given PlayerState values.
    /// </summary>
    /// <param name="ps">PlayerState containing name, chips, cards, etc.</param>
    public void SetFromState(PlayerState ps) {
        nameText.text   = ps.name;
        chipsText.text  = ps.chips.ToString();
        card1.sprite    = ps.hole[0].ToSprite();
        card2.sprite    = ps.hole[1].ToSprite();
        expression = ps.expression;
        if (!(nameText.text == "You")) avatarImage.sprite = SetExpression(ps.expression);
        highlightBorder.enabled = ps.isActive;
        turnIndicator.SetActive(ps.isActive);
    }

    /// <summary>
    /// Loads the sprite matching the given expression name from Resources
    /// and updates the avatar image. Returns the sprite used.
    /// </summary>
    /// <param name="exprName">Identifier for the avatar expression.</param>
    /// <returns>Sprite that was set, or null if none found.</returns>
    private Sprite SetExpression(string exprName) {
        expression = exprName;
        var sprites = Resources.LoadAll<Sprite>($"Avatars/{nameText.text}/");
        // assumes nameText.text matches folder (Player1, Player2…)
        foreach (var s in sprites) {
            if (s.name == exprName + "_0") {
                avatarImage.sprite = s;
                return s;
            }
        }
        Debug.LogWarning($"Expression {exprName} not found for {nameText.text}");
        return sprites[0];
    }
}