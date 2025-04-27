using UnityEngine;

/// <summary>
/// A class composed of helper classes which assist in constructing the Request Payload:
/// {
///     "contents": [
///       {
///         "parts": [
///           {
///             "text": "How does AI work?"
///           }
///         ]
///       }
///     ]
///   }
/// </summary>

[System.Serializable]
public class Part    
{
	public string text; public Part(string t)=>text=t; 
}

[System.Serializable]
public class Content 
{ 
	public Part[] parts; 
}

[System.Serializable]
public class GeminiRequest 
{ 
	public Content[] contents; 
}