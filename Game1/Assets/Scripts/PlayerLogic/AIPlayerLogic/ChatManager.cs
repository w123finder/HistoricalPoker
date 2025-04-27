using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections.Generic;

/// <summary>
/// Handles in-game chat between the human player and AI opponents.
/// Instantiates chat bubbles, maintains chat history, and triggers AI responses.
/// </summary>
public class ChatManager : MonoBehaviour {
    public Transform contentParent;      // ChatScroll/Viewport/Content
    public GameObject messagePrefab;     // ChatMessage.prefab
    public TMP_InputField inputField;    // ChatInput
    public Button sendButton;            // ChatSend
    public PlayerPanel[] playerPanels;   // from UIManager
    public ScrollRect chatScrollRect;    // Chat Autoscroll
    // keep the full chat history
    private List<ChatMessage> chatHistory = new List<ChatMessage>();

    void Start() {
        sendButton.onClick.AddListener(OnSendClicked);
    }


    /// <summary>
    /// Called when the Send button is clicked: submits the user's message.
    /// </summary>
    void OnSendClicked() {
        var text = inputField.text.Trim();
        if(string.IsNullOrEmpty(text)) return;
        AppendMessage("You", text);
        inputField.text = "";
    }

    /// <summary>
    /// Coroutine that requests an AI-generated chat response.
    /// </summary>
    /// <param name="aiIndex">Index of the AI to respond.</param>
    private IEnumerator RespondFromAI(int aiIndex)
	{
	    string raw = null;

	    // a) send the request
	    yield return StartCoroutine(
	        AIPlayerLogic.RequestAIReply(aiIndex, playerPanels, chatHistory, resp => raw = resp)
	    );

	    // b) process the response (no yield needed)
	    ProcessAIResponse(raw, aiIndex, playerPanels);
	}

    /// <summary>
    /// Parses and displays an AI's chat reply.
    /// </summary>
    /// <param name="rawApiResponse">Raw JSON from the LLM API.</param>
    /// <param name="aiIndex">Index of the AI who replied.</param>
    /// <param name="playerPanels">All player UI panels for context.</param>
	private void ProcessAIResponse(
	    string rawApiResponse,
	    int aiIndex,
	    PlayerPanel[] playerPanels
	)
	{
	    if (string.IsNullOrEmpty(rawApiResponse))
	    {
	        AppendMessage(playerPanels[aiIndex].nameText.text,
	                      "⚠️ Failed to get response.");
	        return;
	    }

	    // parse the top‐level response
	    var apiResp = JsonUtility.FromJson<GenerateMessageResponse>(rawApiResponse);
	    if (apiResp?.candidates == null || apiResp.candidates.Length == 0)
	    {
	        AppendMessage(playerPanels[aiIndex].nameText.text, "…no reply…");
	        return;
	    }

        // Extract the JSON blob
        LLMResponse decodedResp;
        try
        {
            decodedResp = AIPlayerLogic.DeserializeGeminiResponse(rawApiResponse);
        }
        catch
        {
            Debug.LogWarning("Malformed decision JSON: " + rawApiResponse);
            AppendMessage(playerPanels[aiIndex].nameText.text,
	                      "🤖 (malformed response)");
            return;
        }

	    // append text + update avatar
	    AppendMessage(playerPanels[aiIndex].nameText.text, decodedResp.text);
	}

    /// <summary>
    /// Instantiates a chat bubble, updates layout, and optionally triggers AI responses.
    /// </summary>
    /// <param name="who">Sender identifier (e.g. "You" or AI name).</param>
    /// <param name="text">Message text to display.</param>
    public void AppendMessage(string who, string text) {
	    // 1) Instantiate under Content
	    var go = Instantiate(messagePrefab, contentParent, false);
	    var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
	    if (tmp == null) {
	        Debug.LogError("ChatManager: Message prefab is missing a TextMeshProUGUI!");
	        return;
	    }
	    tmp.text = $"<b>{who}:</b> {text}";

	    // 2) Force a layout rebuild so Content resizes
	    Canvas.ForceUpdateCanvases();
	    LayoutRebuilder.ForceRebuildLayoutImmediate(
	        contentParent.GetComponent<RectTransform>()
	    );

	    // 3) Scroll to bottom (newest) – 0 = bottom, 1 = top
	    chatScrollRect.verticalNormalizedPosition = 0f;

	    // 4) Add Chat History
        chatHistory.Add(new ChatMessage(who, text));
        if (chatHistory.Count > 20) chatHistory.RemoveAt(0); 
        if (!text.Contains("ACTION - ") || text.Contains("ALL IN") || (Random.Range(1, 10) < 2)) TriggerAIResponses(who);
    }

    /// <summary>
    /// Randomly selects an AI to respond, skipping the sender.
    /// </summary>
    /// <param name="who">Name of the last message sender to skip them.</param>
    private void TriggerAIResponses(string who)
    {
        // For each AI (panels 1..N), start a response if they didn't send it
        for (int aiIndex = 1; aiIndex < playerPanels.Length; aiIndex++)
        {
            if (playerPanels[aiIndex].nameText.text == who) continue;
            if (Random.Range(1, 10) < 4) {
            	StartCoroutine(RespondFromAI(aiIndex));
            }
        }
    }

}
