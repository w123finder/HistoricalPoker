using UnityEngine;

/// <summary>
/// A class composed of helper classes which assist in deserializing the Response Payload:
   ///{
    /// ""candidates"": [{
    ///     ""content"": {
    ///         ""parts"": [{
    ///             ""text"": ""```json\n{\n  \"action\": \"RAISE\",\n  \"raiseAmount\": 5,\n  \"text\": \"\",\n  \"emotion\": \"neutral\"\n}\n```""
    ///         }],
    ///         ""role"": ""model""
    ///     },
    ///     ""finishReason"": ""STOP"",
    ///     ""avgLogprobs"": -0.01362207680940628
    /// }],
    /// ""usageMetadata"": {
    ///     ""promptTokenCount"": 232,
    ///     ""candidatesTokenCount"": 40,
    ///     ""totalTokenCount"": 272,
    ///     ""promptTokensDetails"": [{
    ///         ""modality"": ""TEXT"",
    ///         ""tokenCount"": 232
    ///     }],
    ///     ""candidatesTokensDetails"": [{
    ///         ""modality"": ""TEXT"",
    ///         ""tokenCount"": 40
    ///     }]
    /// },
    /// ""modelVersion"": ""gemini-2.0-flash""
	/// }";
	/// Into:
	///	{
	/// 	public string action;
	///     public int    raiseAmount;
	///     public string text;
	///     public string emotion;
	/// }
///
/// And deserialize the payload into the LLMResponse Class
/// </summary>

[System.Serializable]
public class PartResponse
{
    public string text;
}

[System.Serializable]
public class ContentResponse
{
    public PartResponse[] parts;
    public string role;
}

[System.Serializable]
public class CandidateResponse
{
    public ContentResponse content;
    public string finishReason;
    public float avgLogprobs;
}

[System.Serializable]
public class UsageTokensDetails
{
    public string modality;
    public int tokenCount;
}

[System.Serializable]
public class UsageMetadataResponse
{
    public int promptTokenCount;
    public int candidatesTokenCount;
    public int totalTokenCount;
    public UsageTokensDetails[] promptTokensDetails;
    public UsageTokensDetails[] candidatesTokensDetails;
}

[System.Serializable]
public class GeminiApiResponse
{
    public CandidateResponse[] candidates;
    public UsageMetadataResponse usageMetadata;
    public string modelVersion;
}

[System.Serializable]
public class LLMResponse
{
	public string action;
    public int    raiseAmount;
    public string text;
    public string emotion;
}

[System.Serializable]
public class Candidate
{
    public PartResponse message;
}

[System.Serializable]
public class GenerateMessageResponse
{
	public Candidate[] candidates;
}
