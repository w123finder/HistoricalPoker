using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using System.Linq;
using UnityEngine.UI;


/// <summary>
/// Core poker game loop: handles start/end panels, dealing cards, posting blinds,
/// managing betting rounds, side-pots, showdowns, and transitions between hands.
/// Coordinates state via GameState, interactions via UIManager, and chat logging via ChatManager.
/// </summary>
public class GameLogic : MonoBehaviour
{
    // Singleton instance so UIManager can call us
    public static GameLogic Instance { get; private set; }

    // Hook to your UIManager 
    public UIManager uiManager;

    // For documenting actions
    public ChatManager chatManager;

    // For Start Game UI
    public GameObject startPanel;      
    public Button     startButton;    

    // For End Game UI
    public GameObject endPanel;        
    public Button     continueButton;  
    public Button     resetButton;     
    public TextMeshProUGUI resultText;

    // For covering other players cards
    public List<GameObject> coveringCards = new List<GameObject>();

    // The current game state 
    private GameState currentState;
    private bool   allowContinue = false;
    private bool   gameRunning   = false;

    // Tracking deck
    private Deck deck;

    // Live players
    private int numLivePlayers;

     // ── for human input ─────────────────────────────────────────────────────────
    private string pendingAction;
    private bool actionReceived;


    void Awake()
    {
        // Ensure only one instance
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {

         // 1) Show only the Start screen
        startPanel.SetActive(true);
        endPanel.SetActive(false);

        // 2) Hook up button clicks
        startButton.onClick.AddListener(() => {
            startPanel.SetActive(false);
            BeginGameLoop();
        });
        continueButton.onClick.AddListener(() => {
            endPanel.SetActive(false);
            allowContinue = false;    // one-time “watch” only
            ResumeGameLoop();
        });
        resetButton.onClick.AddListener(() => {
            endPanel.SetActive(false);
            BeginGameLoop();
        });
    }

    /// <summary>
    /// Begin a new game session: initialize state, update UI, and start the hand loop.
    /// </summary>
    void BeginGameLoop()
    {
        gameRunning    = true;
        currentState = GameState.CreateDemoState();
        numLivePlayers = currentState.players.Count;
        uiManager.UpdateGameUI(currentState);
        StartCoroutine(StartHand());
    }

    /// <summary>
    /// Resume game loop after a bust “watch” without resetting chips.
    /// </summary>
    void ResumeGameLoop()
    {
        gameRunning = true;          
        StartCoroutine(StartHand());
    }


    /// <summary>
    /// Expose the current GameState for UI callbacks.
    /// </summary>
    /// <returns>The live GameState instance.</returns>
    public GameState getCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Called by UIManager when the human player issues an action (fold, call, raise).
    /// Buffers the action for the coroutine to process.
    /// </summary>
    /// <param name="action">String representation of the player’s action.</param>
    public void HandlePlayerAction(string action)
    {
        pendingAction   = action;
        actionReceived  = true;
    }

// ---------------------------------------------------------------------------------------
// IEnumerator Core Gameplay Actions
// ---------------------------------------------------------------------------------------

    /// <summary>
    /// Main loop that deals successive hands until someone wins or the human busts.
    /// </summary>
    public IEnumerator StartHand()
    {
        while (gameRunning && numLivePlayers > 1)
        {
            deck = new Deck();
            currentState.ResetPotTracking();
            currentState.pot = 0;
            yield return StartCoroutine(HandCoroutine());
            // after each hand, check if *you* went bust:
            var you = currentState.players[0];
            if (you.chips <= 0 && !allowContinue)
            {
                // show end screen and pause
                ShowEndScreen(playerWon:false);
                yield break;
            }
        }

        if (gameRunning)
            ShowEndScreen(playerWon:true); 
    }


    /// <summary>
    /// Run the sequence for one full poker hand: blinds, deal, betting rounds, showdown.
    /// </summary>
    private IEnumerator HandCoroutine()
    {
        // 1) Rotate dealer button
        currentState.dealerIndex = (currentState.dealerIndex + 1) % currentState.players.Count;

        // 2) Deal hole cards
        DealHoleCards();

        // 3+4) Pre-flop betting and post blinds
        yield return RunBettingRound(startOffset: (getUTGIndex() - currentState.dealerIndex + currentState.players.Count) % currentState.players.Count);
        if (CountActivePlayers() <= 1)
        {
            DealCommunityCards(5);
            ToggleCards(true);
            ResolveShowdown(true);
            uiManager.UpdateGameUI(currentState);
            yield return new WaitForSeconds(6);
            ToggleCards(false);
            yield break;
        }

        // 5) Deal Flop
        DealCommunityCards(3);
        yield return RunBettingRound(startOffset: 1);
        if (CountActivePlayers() <= 1)
        {
            DealCommunityCards(2);
            ToggleCards(true);
            ResolveShowdown(true);
            uiManager.UpdateGameUI(currentState);
            yield return new WaitForSeconds(6);
            ToggleCards(false);
            yield break;
        }

        // 6) Deal Turn
        DealCommunityCards(1);
        yield return RunBettingRound(startOffset: 1);
        if (CountActivePlayers() <= 1)
        {
            DealCommunityCards(1);
            ToggleCards(true);
            ResolveShowdown(true);
            uiManager.UpdateGameUI(currentState);
            yield return new WaitForSeconds(6);
            ToggleCards(false);
            yield break;
        }

        // 7) Deal River
        DealCommunityCards(1);
        yield return RunBettingRound(startOffset: 1);
        if (CountActivePlayers() <= 1)
        {
            ToggleCards(true);
            ResolveShowdown(true);
            uiManager.UpdateGameUI(currentState);
            yield return new WaitForSeconds(6);
            ToggleCards(false);
            yield break;
        }

        ToggleCards(true);

        // 8) Showdown & award pot
        ResolveShowdown(false);

        // 9) Update UI
        uiManager.UpdateGameUI(currentState);

        yield return new WaitForSeconds(6);

        ToggleCards(false);

        // Hand complete
        yield break;
    }


    /// <summary>
    /// Conduct a betting round starting at a given seat offset from the dealer.
    /// Handles FOLD, CALL, RAISE, all-in, and updates GameState contributions and pot.
    /// </summary>
    /// <param name="startOffset">Positions ahead of dealerIndex to begin action.</param>
    private IEnumerator RunBettingRound(int startOffset)
    {
        var players = currentState.players;
        int n = players.Count;
        int startPassedThrough = 0;

        // At each new round, reset each player's currentBet and the minimum raise
        foreach (var p in players)
            p.currentBet = 0;
        currentState.currentBet     = 0;
        currentState.toCall         = 0;
        currentState.lastRaiseAmount = currentState.bigBlindAmount;

        int othervalue = ((getUTGIndex() - currentState.dealerIndex + currentState.players.Count) % currentState.players.Count);

        if (startOffset == ((getUTGIndex() - currentState.dealerIndex + currentState.players.Count) % currentState.players.Count)) {
            PostBlinds();
        }

        // Count of players who still need to act before round ends
        int remainingToAct = CountActivePlayers();

        // Start at dealer + offset
        int startIdx = (currentState.dealerIndex + startOffset) % n;
        int idx = (currentState.dealerIndex + startOffset) % n;


        while (remainingToAct > 0)
        {
            var p = players[idx];
            if (startIdx == idx) startPassedThrough++;

            // Only non-folded players with chips get to act
            if (!p.hasFolded && p.chips > 0)
            {
                p.isActive = true;
                currentState.activePlayerIndex = idx;
                uiManager.UpdateGameUI(currentState);

                if (idx == 0)
                    yield return WaitForHumanAction(p);
                else
                    yield return WaitForAIAction(p, idx);

                // If they raised, everyone else must now respond
                if (currentState.lastActionWasRaise)
                    remainingToAct = CountActivePlayers() - 1;
                else
                    remainingToAct--;
                p.isActive = false;
            }

            idx = (idx + 1) % n;
        }
    }

        /// <summary>
    /// Wait for the human to click Fold/Call/Raise, then process their action.
    /// </summary>
    IEnumerator WaitForHumanAction(PlayerState p)
    {
        actionReceived = false;

        while (!actionReceived)
            yield return null;

        currentState.lastActionWasRaise = ProcessAction(p, pendingAction);
        yield return null;
    }

    /// <summary>
    /// Delay and invoke AI to decide an action via Gemini, then process it.
    /// </summary>
    IEnumerator WaitForAIAction(PlayerState p, int idx)
    {
        yield return new WaitForSeconds(0.5f);

        string decidedAction  = "CALL";
        int    decidedAmt     = 0;
        string decidedEmotion = "neutral";

        // Kick off the Gemini-based decision
        yield return StartCoroutine(
            AIPlayerLogic.DecideCoroutine(
                p,
                currentState,
                aiIndex: idx,
                callback: (action, amt, emotion) => {
                    decidedAction = action;
                    decidedAmt    = amt;
                    decidedEmotion = emotion;
                },
                uiManager.playerPanels[idx]
            )
        );

        // Setting emotion
        p.expression = decidedEmotion;

        // Build the actionText for your ProcessAction
        string actionText = decidedAction == "RAISE"
            ? $"RAISE {decidedAmt}"
            : decidedAction;

        currentState.lastActionWasRaise = ProcessAction(p, actionText);

        yield return null;
    }


// ---------------------------------------------------------------------------------------
// Non IEnumerator Core Gameplay Actions
// ---------------------------------------------------------------------------------------


    /// <summary>
    /// Execute a player's action string, updating chips, bets, pot, and chat log.
    /// </summary>
    /// <param name="p">PlayerState of the actor.</param>
    /// <param name="action">Action text (FOLD, CALL, RAISE X).</param>
    /// <returns>True if it was a raise; false otherwise.</returns>
    bool ProcessAction(PlayerState p, string action)
    {
        action = action.ToUpper();
        int toCall = Mathf.Max(0, currentState.currentBet - p.currentBet);

        // FOLD
        if (action.StartsWith("FOLD"))
        {
            chatManager.AppendMessage(p.name, "ACTION - FOLD");
            p.hasFolded = true;
            return false;
        }

        // CALL or CHECK
        if (action.StartsWith("CALL") || action.StartsWith("CHECK"))
        {
            int callAmt = Mathf.Min(toCall, p.chips);
            p.chips      -= callAmt;
            p.currentBet += callAmt;
            currentState.toCall = currentState.currentBet;  // stays the same
            currentState.contributions[p] += callAmt;
            currentState.pot += callAmt;
            if (callAmt == 0) {
                chatManager.AppendMessage(p.name, "ACTION - CHECK");
            } else {
                chatManager.AppendMessage(p.name, $"ACTION - CALL for {callAmt}");
            }
            return false;
        }

        // RAISE X
        if (action.StartsWith("RAISE"))
        {
            var parts = action.Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[1], out int requestedRaise))
            {
                // 1) Determine how much extra your opponents can actually call
                int minOppChips = int.MaxValue;
                foreach (var o in currentState.players)
                {
                    if (o == p || o.hasFolded || o.hasGoneBust) continue;
                    minOppChips = Mathf.Min(minOppChips, o.chips);
                }
                // If nobody else can call, fall back to your full stack
                if (minOppChips == int.MaxValue)
                    minOppChips = p.chips - toCall;

                // 2) Calculate how much you could raise beyond just calling
                int maxExtra = p.chips - toCall;

                // 3) Cap your extra raise so that opponents can call
                int capExtra = Mathf.Min(minOppChips, maxExtra);

                // 4) Enforce the minimum‐raise (lastRaiseAmount) up to that cap
                int actualRaise = Mathf.Clamp(
                    requestedRaise,
                    currentState.lastRaiseAmount,
                    capExtra
                );

                // 5) If you can’t meet the minimum raise, treat it as a call or all‐in
                if (actualRaise < currentState.lastRaiseAmount)
                {
                    int callAmt = Mathf.Min(toCall, p.chips);
                    p.chips      -= callAmt;
                    p.currentBet += callAmt;
                    currentState.contributions[p] += callAmt;
                    currentState.pot += callAmt;

                    // If that call empties your stack, it’s an all‐in
                    string msg = p.chips == 0
                        ? $"ACTION - ALL IN for {callAmt}"
                        : $"ACTION - CALL for {callAmt}";
                    chatManager.AppendMessage(p.name, msg);
                    return false;
                }

                // 6) Otherwise, commit your call + raise
                int totalPut = toCall + actualRaise;
                p.chips      -= totalPut;
                p.currentBet += totalPut;
                currentState.contributions[p] += totalPut;
                currentState.pot += totalPut;

                // 7) Update the betting state
                currentState.lastRaiseAmount = actualRaise;
                currentState.currentBet     = p.currentBet;
                currentState.toCall         = currentState.currentBet;

                // 8) If you’re now broke, it’s an all‐in; otherwise a normal bet
                string actionMsg = p.chips == 0
                    ? $"ACTION - ALL IN for {totalPut}"
                    : $"ACTION - BET for {totalPut}";
                chatManager.AppendMessage(p.name, actionMsg);

                return true;  // it's a raise, so others must respond
            }
        }

        // fallback to call
        int fallbackAmt = Mathf.Min(toCall, p.chips);
        p.chips      -= fallbackAmt;
        p.currentBet += fallbackAmt;
        currentState.contributions[p] += fallbackAmt;
        currentState.pot += fallbackAmt;
        chatManager.AppendMessage(p.name, $"ACTION - CALL for {fallbackAmt}");
        return false;
    }

    /// <summary>
    /// Show or hide the back-of-card covers over opponents’ hole cards.
    /// </summary>
    /// <param name="show">True to reveal cards; false to cover.</param>
    void ToggleCards(bool showdown)
    {
        foreach (var cardBack in coveringCards)
        {
            cardBack.SetActive(!showdown);
        }
    }


    /// <summary>
    /// Deal two hole cards to each active (non-bust) player.
    /// </summary>
    void PostBlinds()
    {
        var ps = currentState.players;
        int sbIndex = NextActive(currentState.dealerIndex);
        int bbIndex = NextActive(sbIndex);

        // Small blind
        var sb = ps[sbIndex];
        int sbAmt = Mathf.Min(sb.chips, currentState.smallBlindAmount);
        sb.chips      -= sbAmt;
        sb.currentBet += sbAmt;
        currentState.contributions[sb] += sbAmt;

        // Big blind
        var bb = ps[bbIndex];
        int bbAmt = Mathf.Min(bb.chips, currentState.bigBlindAmount);
        bb.chips      -= bbAmt;
        bb.currentBet += bbAmt;
        currentState.contributions[bb] += bbAmt;

        // Initialize pot and toCall
        currentState.pot      = sbAmt + bbAmt;
        currentState.currentBet = bbAmt;
        currentState.toCall     = bbAmt;

        // Set first active player to seat after BB
        currentState.activePlayerIndex = (bbIndex + 1) % ps.Count;
    }

    /// <summary>
    /// Deal two hole cards to each active (non-bust) player.
    /// </summary>
    void DealHoleCards()
    {
        foreach (var player in currentState.players)
        {
            // reset fold/bet state for *active* players only
            if (player.hasGoneBust) {
                player.hole[0]    = new Card("card_empty");
                player.hole[1]    = new Card("card_empty");
                continue;
            }

            player.hasFolded  = false;
            player.currentBet = 0; 
            player.hole[0]    = deck.Draw();
            player.hole[1]    = deck.Draw();
        }
        currentState.communityCards.Clear();
        uiManager.UpdateGameUI(currentState);
    }

    /// <summary>
    /// Determine and pay out main and side pots, then mark busts.
    /// </summary>
    /// <param name="endTurnEarly">True if only one player remains.</param>
    void ResolveShowdown(bool endTurnEarly)
    {
        bool turnEnded = false;

        // 1) Find side-pots
        var pots = currentState.ComputePots();

        // End turn early case, just get only player left in eligiblePlayers
        if (endTurnEarly) {
            var eligible = pots[0].eligiblePlayers;
            if (eligible.Count == 1)
            {
                var winner = eligible[0];
                chatManager.AppendMessage(winner.name, $"WON {currentState.pot} CHIPS");
                winner.chips += currentState.pot;
                turnEnded = true;
            }

        } 

        // 2) For each pot, award to best hand among eligible
        if (!turnEnded) {
            var evaluator = new Evaluator();
            foreach (var pot in pots)
            {
                PlayerState best = null;
                HandValue bestVal = default;
                foreach (var p in pot.eligiblePlayers)
                {
                    var hv = evaluator.Evaluate(p.hole, currentState.communityCards);
                    if (best == null || hv.CompareTo(bestVal) > 0)
                    {
                        best    = p;
                        bestVal = hv;
                    }
                }
                // announce and pay out
                chatManager.AppendMessage(best.name, $"WON {pot.amount} CHIPS");
                best.chips += pot.amount;
            }

        }

        // 3) Mark busts as before
        foreach (var p in currentState.players)
        {
            if (!p.hasGoneBust && p.chips <= 0)
            {
                p.hasGoneBust = true;
                numLivePlayers--;
                chatManager.AppendMessage(p.name, "ACTION - BUST!");
            }
        }
    }

// ---------------------------------------------------------------------------------------
// Gameplay Helpers
// ---------------------------------------------------------------------------------------

    /// <summary>
    /// Deal new community cards into the board.
    /// </summary>
    /// <param name="count">Number of cards to deal (3=flop, 1=turn/river).</param>
    void DealCommunityCards(int count)
    {
        for (int i = 0; i < count; i++)
            currentState.communityCards.Add(deck.Draw());
        uiManager.UpdateGameUI(currentState);
    }

    /// <summary>
    /// Display the end-of-game UI panel with win/loss message and options.
    /// </summary>
    /// <param name="playerWon">True if the human won; false otherwise.</param>
    void ShowEndScreen(bool playerWon)
    {
        gameRunning = false;         // stop any further hands

        // Update the header text on EndPanel:
        var header = endPanel.transform.Find("ResultText")?
                       .GetComponent<TextMeshProUGUI>();
        if (header != null)
            header.text = playerWon 
               ? "Congratulations, You Win!" 
               : "You Lost All Your Chips";

        // If the player *didn’t* win and they haven’t continued yet, allow continue.
        continueButton.gameObject.SetActive(!playerWon && !allowContinue);

        // After a “watch” resume, we’ll prevent further continue:
        // that flag is already set to false initially, and set to
        // false again in the continueButton callback—so next time
        // playerWon==false, they’ll only see Reset.

        endPanel.SetActive(true);
    }

    /// <summary>
    /// Find the next non-bust player seat after a given index.
    /// </summary>
    /// <param name="start">Starting seat index.</param>
    /// <returns>Index of next active player.</returns>
    int NextActive(int start)
    {
        var ps = currentState.players;
        int n = ps.Count, idx = start;
        do {
            idx = (idx + 1) % n;
        } while (ps[idx].hasGoneBust);
        return idx;
    }

    /// <summary>
    /// Count how many players remain in the hand (not folded and not bust).
    /// </summary>
    /// <returns>Number of active players.</returns>
    int CountActivePlayers()
    {
        int cnt = 0;
        foreach (var p in currentState.players)
            if (!p.hasFolded && p.chips > 0)
                cnt++;
        return cnt;
    }

    /// <summary>
    /// Compute index of Under-The-Gun (first to act pre-flop).
    /// </summary>
    /// <returns>Seat index one after the big blind.</returns>
    int getUTGIndex() {
        int sbIndex = NextActive(currentState.dealerIndex);
        int bbIndex = NextActive(sbIndex);
        return NextActive(bbIndex);
    }
}