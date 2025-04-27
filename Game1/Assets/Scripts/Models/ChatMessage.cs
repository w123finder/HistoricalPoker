using UnityEngine;

/// <summary>
/// Represents a single entry in the chat log, storing who sent it and what they said.
/// </summary>
[System.Serializable]
public class ChatMessage
{
    /// <summary>
    /// The origin of the message, for example "You", "James", or system/ACTION tags.
    /// </summary>
    public string role;

    /// <summary>
    /// The textual content of the chat message.
    /// </summary>
    public string content;

    /// <summary>
    /// Constructs a new ChatMessage with the given sender role and message content.
    /// </summary>
    /// <param name="role">Identifier for who sent the message.</param>
    /// <param name="content">The text of the chat message.</param>
    public ChatMessage(string role, string content)
    {
        this.role    = role;
        this.content = content;
    }
}
