using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the main game UI elements: pot display, player action buttons,
/// community cards, raise panel, and updates player panels each turn.
/// </summary>
public class UIManager : MonoBehaviour {
    public TextMeshProUGUI potText;            // Current pot size
    public Button foldBtn, callBtn, raiseBtn;  // The buttons for player control
    public PlayerPanel[] playerPanels;         // The various players in the game
    public Image[] communityCards;             // The cards in play
    public GameObject raisePanel;              // Panel toggle which gives raise input
    public TMP_InputField raiseInput;          // Input field for raies
    public Button confirmRaiseBtn;             // Raise button
    public TextMeshProUGUI errorText;          // Error text incase something illegal is done

    private bool isRaisePanelShowing = false;  // Whether the raise panel is showing or not

    void Start() 
    {
        // Handle Fold and Call
        foldBtn.onClick.AddListener(() => OnPlayerAction("FOLD"));
        callBtn.onClick.AddListener(() => OnPlayerAction("CALL"));

        // Handle Raise
        raiseBtn.onClick.AddListener(ShowRaisePanel);
        confirmRaiseBtn.onClick.AddListener(OnConfirmRaise);
        raisePanel.SetActive(false);
        if (errorText != null) errorText.text = "";
    }

    void OnPlayerAction(string action) 
    {
        GameLogic.Instance.HandlePlayerAction(action);
    }

    /// <summary>
    /// Toggles the raise input panel, disables other buttons while visible,
    /// and prepares the input field for user entry.
    /// </summary>
    void ShowRaisePanel()
    {
        isRaisePanelShowing = !isRaisePanelShowing;
        // Show the input, disable other buttons so you can’t click off
        raisePanel.SetActive(isRaisePanelShowing);
        raiseInput.text = "";
        raiseInput.ActivateInputField();

        foldBtn.interactable = !isRaisePanelShowing;
        callBtn.interactable = !isRaisePanelShowing;

        if (errorText != null) errorText.text = "";
    }

    /// <summary>
    /// Validates the entered raise amount and sends a RAISE action if valid.
    /// Displays an error message if validation fails.
    /// </summary>
    void OnConfirmRaise()
    {
        string raw = raiseInput.text.Trim();
        if (!int.TryParse(raw, out int amt) || amt <= 0)
        {
            if (errorText != null) errorText.text = "Enter a positive number.";
            return;
        }

        // Get human player's chips (assume seat 0)
        var human = GameLogic.Instance.getCurrentState().players[0];
        if (amt > human.chips)
        {
            if (errorText != null)
                errorText.text = $"You only have {human.chips} chips.";
            else
                Debug.LogWarning($"Raise of {amt} exceeds your stack of {human.chips}.");
            return;
        }

        // Passed validation—send the action
        GameLogic.Instance.HandlePlayerAction($"RAISE {amt}");

        // Reset UI
        raisePanel.SetActive(false);
        foldBtn.interactable = true;
        callBtn.interactable = true;
        raiseBtn.interactable = true;
        if (errorText != null) errorText.text = "";
    }

    /// <summary>
    /// Updates pot text, community card sprites, and each player panel from the current state.
    /// </summary>
    /// <param name="state">The latest GameState containing pot, community cards, and player info.</param>
    public void UpdateGameUI(GameState state) 
    {
        potText.text = $"Pot: {state.pot}";
        // Update community cards
        for(int i=0; i<communityCards.Length; i++) {
            if (i < state.communityCards.Count)
                communityCards[i].sprite = state.communityCards[i].ToSprite();
            else
                communityCards[i].sprite = Resources.Load<Sprite>($"Cards/card_back"); // face-down sprite
        }
        // Update each player panel
        for(int i=0; i<playerPanels.Length; i++) {
            playerPanels[i].SetFromState(state.players[i]);
        }
    }
}