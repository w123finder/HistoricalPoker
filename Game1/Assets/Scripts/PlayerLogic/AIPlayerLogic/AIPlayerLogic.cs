using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Static helper class for AI player decision-making via Gemini LLM.
/// Manages API requests to generate betting actions and chat responses.
/// </summary>
public static class AIPlayerLogic
{

    /// <summary>
    /// API key for authenticating with the Gemini Generative Language endpoint.
    /// Must be set before calling any coroutines.
    /// </summary>
    private static string geminiApiKey = {Add Gemini Key here};

    /// <summary>
    /// Sends the current game state to Gemini and retrieves a betting decision.
    /// </summary>
    /// <param name="p">The AI player's state (hole cards, chips, etc.).</param>
    /// <param name="state">The overall game state (community cards, pot, bets).</param>
    /// <param name="aiIndex">Index of the AI in the player panel array.</param>
    /// <param name="callback">
    /// Invoked on completion with arguments: action string (FOLD/CALL/RAISE),
    /// raise amount, and new emotion label.
    /// </param>
    /// <param name="playerPanels">Reference to UI elements for this AI.</param>
    public static IEnumerator DecideCoroutine(
        PlayerState p,
        GameState   state,
        int         aiIndex,
        Action<string,int,string> callback,
        PlayerPanel playerPanels
    )
    {
    	yield return new WaitForSeconds(UnityEngine.Random.Range(3, 7));
        // 1) Build the conversation window: system + last 7 lines of chat + game state
        var messages = new List<Part>();

        // system instruction
        messages.Add(new Part ( $"You are poker AI who is currently playing as if you were {p.name}.  Your current emotion is: {p.expression}. " +
                "Given the state of a Texas Hold'em hand, decide your next move.  " +
                "Also try and send back in the response the emotion you're feeling with these cards, and with all the table parameters, " +
                "Try and have a different emotion than the one you're feeling right now." + 
                "Output EXACTLY a JSON object: {\"action\":...,\"raiseAmount\":...," +
                "\"text\":...,\"emotion\":...} " +
                "where action is one of \"FOLD\", \"CALL\", \"RAISE\", and raiseAmount is an integer of the amount you want to raise." +
                "text is \"\" always, " +
                "emotion is one of: \"happy\", \"sad\", \"neutral\", \"inquisitive\", \"deceitful\", \"sad_bluff\", \"happy_bluff\")." +
                BuildStateDescription(p, state)
    	));

		Content singleContent = new Content {
            parts = messages.ToArray()
        };
        GeminiRequest request = new GeminiRequest
        {
            contents = new Content[] { singleContent }
        };

        string jsonPayload = JsonUtility.ToJson(request);

        // 2) Fire the HTTP POST
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={geminiApiKey}";
        using var uwr = new UnityWebRequest(url, "POST")
        {
            uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        // 3) Error handling
        if (uwr.result is UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError($"Gemini API error conn: {uwr.error}");
            callback("CALL", 0, "neutral");
            yield break;
        }

        if (uwr.result is UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Gemini API error protocol: {uwr.error}");
            long code = uwr.responseCode;
		    string body = uwr.downloadHandler.text;
		    Debug.LogError($"HTTP {code} – {uwr.error}\nResponse body:\n{body}");
            callback("CALL", 0, "neutral");
            yield break;
        }

        // 4) Parse response
        var apiResp = JsonUtility.FromJson<GenerateMessageResponse>(uwr.downloadHandler.text);
        if (apiResp?.candidates == null || apiResp.candidates.Length == 0)
        {
            callback("CALL", 0, "neutral");
            yield break;
        }

        // 5) Extract the JSON blob
        LLMResponse decodedResp;
        try
        {
            decodedResp = DeserializeGeminiResponse(uwr.downloadHandler.text);
        }
        catch
        {
            Debug.LogWarning("Malformed decision JSON: " + uwr.downloadHandler.text);
            callback("CALL", 0, "neutral");
            yield break;
        }

        // normalize
        string action = decodedResp.action.ToUpper();
        int raiseAmt  = Math.Max(0, decodedResp.raiseAmount);
        callback(action, raiseAmt, decodedResp.emotion);
    }

    /// <summary>
    /// Sends chat history to Gemini to generate an AI chat reply.
    /// </summary>
    /// <param name="aiIndex">Index of the AI in the player panels.</param>
    /// <param name="playerPanels">All player UI panels for context.</param>
    /// <param name="chatHistory">List of prior chat messages.</param>
    /// <param name="onComplete">Callback with raw JSON response or null on error.</param>
    public static IEnumerator RequestAIReply(
	    int aiIndex,
	    PlayerPanel[] playerPanels,
	    List<ChatMessage> chatHistory,
	    Action<string> onComplete  // callback(rawJson) or rawJson==null on error
	)
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(2, 7));
	    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={geminiApiKey}";

	    // build the prompt messages
	    var messages = new List<Part>();
	    messages.Add(new Part("You are a poker AI. The chat message is your tool to have conversations with other players. " +
	        "Please chat with the other players and produce emotions based off the current conversations as if you are: " +
	        playerPanels[aiIndex].nameText.text + ". " +  $"Your current emotion is: {playerPanels[aiIndex].expression}. " +
	        "You are going to receive the past conversation with the layout of \"Player Name\": \"Message\". " +
	        "Any messages containing \"ACTION - \" are a player's action. " +
	        "Messages containing \"WON \" indicate the player who won the previous hand. " +
	        "Please use these as context for the conversation if it is related to the ongoing poker hand. " +
	        "The current emotion you feel is: " + playerPanels[aiIndex].expression + 
	        ", please take this into account for your response. " + 
	        "When you reply, output EXACTLY a JSON object with four fields: " +
	        "1. \"action\" (always \"\" here), " +
			"2. \"raiseAmount\" (always 0 here), " +	
	        "3. \"text\" (your chat message), " +
	        "4. \"emotion\" (one of: " +
	        "\"happy\", \"sad\", \"neutral\", \"inquisitive\", \"deceitful\", \"sad_bluff\", \"happy_bluff\")."
	    ));

	    int start = Mathf.Max(0, chatHistory.Count - 15);
	    for (int i = start; i < chatHistory.Count; i++)
	    {
	        var e = chatHistory[i];
	        	messages.Add(new Part($"{e.role}: {e.content}"));
	    }

	    Content singleContent = new Content {
            parts = messages.ToArray()
        };
        GeminiRequest request = new GeminiRequest
        {
            contents = new Content[] { singleContent }
        };
	    string bodyJson = JsonUtility.ToJson(request);

	    using var uwr = new UnityWebRequest(url, "POST")
	    {
	        uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson)),
	        downloadHandler = new DownloadHandlerBuffer()
	    };
	    uwr.SetRequestHeader("Content-Type", "application/json");

	    yield return uwr.SendWebRequest();

	    // on error, callback(null)
	    if (uwr.result is UnityWebRequest.Result.ConnectionError
	                  or UnityWebRequest.Result.ProtocolError)
	    {
	        Debug.LogError($"Gemini API error: {uwr.error}");
	        long code = uwr.responseCode;
		    string body = uwr.downloadHandler.text;
		    Debug.LogError($"HTTP {code} – {uwr.error}\nResponse body:\n{body}");
	        onComplete(null);
	        yield break;
	    }

	    // otherwise pass back the raw JSON blob
	    onComplete(uwr.downloadHandler.text);
	}

    /// <summary>
    /// Builds a descriptive string of the player's hole and community cards,
    /// current pot, bets, and chip counts for LLM prompts.
    /// </summary>    
    private static string BuildStateDescription(PlayerState p, GameState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hole cards: {CardListToString(p.hole)}");
        sb.AppendLine($"Community cards: {CardListToString(state.communityCards)}");
        sb.AppendLine($"Pot size: {state.pot}");
        sb.AppendLine($"Current highest bet: {state.currentBet}");
        sb.AppendLine($"Your chips: {p.chips}");
        sb.AppendLine("Other players:");
        for (int i = 0; i < state.players.Count; i++)
        {
            var o = state.players[i];
            if (o == p || o.hasFolded) continue;
            sb.AppendLine($" - {o.name}: {o.chips} chips and {o.currentBet} current bet.");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a list of Card objects into a comma-separated sprite name string.
    /// </summary>
    private static string CardListToString(IEnumerable<Card> cards)
    {
        var parts = new List<string>();
        foreach (var c in cards)
            parts.Add(c.spriteName);  // e.g. "AS", "10H"
        return string.Join(",", parts);
    }

    /// <summary>
    /// Parses the nested JSON response from Gemini and extracts action, raiseAmount,
    /// text, and emotion into an LLMResponse object.
    /// </summary>
    /// <param name="jsonString">Raw JSON payload from the Gemini API.</param>
    /// <returns>Deserialized LLMResponse or null on parse error.</returns>
    public static LLMResponse DeserializeGeminiResponse(string jsonString)
    {
        // 1. Deserialize the main JSON response
        GeminiApiResponse apiResponse = JsonUtility.FromJson<GeminiApiResponse>(jsonString);

        // 2. Check if there are any candidates
        if (apiResponse != null && apiResponse.candidates != null && apiResponse.candidates.Length > 0)
        {
            CandidateResponse firstCandidate = apiResponse.candidates[0];

            // 3. Check if the candidate has content and parts
            if (firstCandidate.content != null && firstCandidate.content.parts != null && firstCandidate.content.parts.Length > 0)
            {
                PartResponse firstPart = firstCandidate.content.parts[0];

                // 4. Extract the string containing the inner JSON
                string innerJsonString = firstPart.text;

                // Remove the ```json\n and \n``` if present
                if (innerJsonString.StartsWith("```json\n"))
                {
                    innerJsonString = innerJsonString.Substring("```json\n".Length);
                }
                if (innerJsonString.EndsWith("\n```"))
                {
                    innerJsonString = innerJsonString.Substring(0, innerJsonString.Length - "\n```".Length);
                }

                // 5. Deserialize the inner JSON into LLMResponse
                LLMResponse llmResponse = JsonUtility.FromJson<LLMResponse>(innerJsonString);

                return llmResponse;
            }
            else
            {
                Debug.LogError("Could not find content parts in the response.");
                return null;
            }
        }
        else
        {
            Debug.LogError("Could not find candidates in the response.");
            return null;
        }
    }
}
