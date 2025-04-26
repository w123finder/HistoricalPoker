# Poker AI Unity Project

A Texas Hold'em–style poker game built in Unity, featuring a human player and 3 AI opponents you may recognize! These characters are powered by Google’s Gemini 2.0 Flash LLM, and are prompted to bet, converse, and act (via sprites) how their real life counterparts would. Players bet, fold, and raise; side pots are supported; and in-game chat lets AIs converse with each other and the human via a scrolling chat window.

---

## Architecture Overview

- **GameLogic**: Core loop and state transitions. Manages dealing, blinds, betting rounds, showdowns, and start/end screens.  
- **GameState**: Snapshot of the table—players, community cards, blinds, pot contributions (for side-pots), and turn order. Exposes `ComputePots()` to slice main and side pots.  
- **PlayerState**: Holds a single player’s data (name, chips, hole cards, fold status, emotion, bust flag).  
- **Deck**: 52-card deck with `Reset()`, `Shuffle()`, and `Draw()` methods. Cards map to sprite names via the `Card` class.  
- **Evaluator**: Brute-force hand evaluator. From 7 cards, generates all 5-card combos, ranks them, and returns the best `HandValue`.  
- **UIManager**: Binds Unity UI—pot display, action buttons (Fold/Call/Raise), community cards, raise panel, and updates `PlayerPanel`s each turn.  
- **ChatManager**: Handles chat bubbles, scroll behavior, and triggers AI chat replies. Maintains a rolling history of messages for context.  
- **PlayerPanel**: Maps `PlayerState` to on-screen elements—avatar, hole cards, chip count, turn indicator, and expression.  
- **AIPlayerLogic**: Sends betting decisions and chat requests to Gemini 2.0 Flash via UnityWebRequest. Coroutines `DecideCoroutine` and `RequestAIReply` handle network flow, error handling, and JSON parsing.  
- **Gemini Version**: Uses **Gemini 2.0 Flash** (`models/gemini-2.0-flash:generateContent`) for decision-making and chat generation.

---

### Setup Instructions

1. **Clone the Repository**  
   ```bash
   git clone https://github.com/yourusername/poker-ai-unity.git
   cd poker-ai-unity

### **Open in Unity**

 a.   Launch Unity Hub (2021.3 LTS or later recommended).

 b.   Click **Add**, navigate to the cloned folder, and open the project.

### **Configure API Key**

In ```Assets/Scripts/PlayerLogic/AIPlayerLogic/AIPlayerLogic.cs```, set your Gemini API key at the top:
```
    private static string geminiApiKey = "YOUR_API_KEY_HERE";
```
### Import Card & Avatar Assets

* Place card sprites under `Assets/Resources/Cards/` named `card_<Suit>_<Rank>.png`.
* Place avatar folders under `Assets/Resources/Avatars/<PlayerName>/`, each containing `<expression>_0.png` sprites.

### Scene & UI Setup

* Open `Assets/Scenes/Main.unity`.
* Ensure your Canvas contains:
    * `UIManager` and `ChatManager` components with Inspector references set.
    * `PlayerPanel` entries linked to their `Image`/`Text` components.
    * `Chat Scroll View` with `VerticalLayoutGroup` + `ContentSizeFitter`.
    * `StartPanel` and `EndPanel` for game start/end UIs.

### Play Mode

* Press **Play** in the Unity Editor.
* Click **Start Betting** to begin.
* Use **Fold**/**Call**/**Raise** buttons or chat to interact.

## Notes & Tips

* **Side Pots:** When a player goes all-in, side pots are computed automatically and awarded correctly.
* **Chat History:** Limited to the last 20 messages; AI uses the most recent 15 for context.
* **Debugging:** Watch the Unity Console for API errors or JSON-parsing warnings.
* **Future Work:** Add sound effects, tune AI temperature, or theme the UI for polish.
