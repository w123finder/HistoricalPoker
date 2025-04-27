using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/// <summary>
/// Represents the entire state of a poker game, including players, community cards,
/// pot tracking (including side-pots), blinds, and turn management.
/// </summary>
public class GameState
{
    // Blinds & dealer
    public int dealerIndex = 0;
    public int smallBlindAmount = 5;
    public int bigBlindAmount   = 10;

    // The total pot
    public int pot = 0;

    // Tracks the current highest bet this round
    public int currentBet = 0;

    // How much each player must put in to call
    public int toCall = 0;

    // The *minimum* extra chips required to make a legal raise
    public int lastRaiseAmount = 0;

    // Card lists
    public List<Card> communityCards = new List<Card>();
    public List<PlayerState> players = new List<PlayerState>();

    // Whose turn it is
    public int activePlayerIndex = 0;

    // Whether last action was raise
    public bool lastActionWasRaise = false;

    // Player Name List
    private static List<string> playerList = new List<string> { "Sun Tsu", "Queen Liz", "BlackBeard" };

    // Track how much each player has put in this hand:
    public Dictionary<PlayerState,int> contributions = new();

    // A side-pot struct:
    public class Pot {
        public int amount;
        public List<PlayerState> eligiblePlayers;
    }



    /// <summary>
    /// Creates a sample GameState with 4 players (1 human + 3 AI) and default chip counts.
    /// </summary>
    public static GameState CreateDemoState()
    {
        var gs = new GameState();
        gs.pot              = 0;
        gs.currentBet       = 0;
        gs.toCall           = 0;
        gs.lastRaiseAmount  = gs.bigBlindAmount;  // initialize to big blind for pre‐flop
        gs.dealerIndex      = 0;
        gs.activePlayerIndex = 1;  // e.g. seat to the left of dealer
        gs.lastActionWasRaise = false;

        // demo players
        for (int i = 0; i < 4; i++)
        {
            string playerName = i == 0 ? "You" : null;
            if (i > 0 && playerList.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, playerList.Count - 1);
                playerName = playerList[randomIndex];
                playerList.RemoveAt(randomIndex);
            }
            else if (i > 0)
            {
                playerName = "AI Player " + i; // Fallback if playerList is empty
            }
            gs.players.Add(new PlayerState {
                name      = playerName,
                chips     = 1000,
                hole      = new Card[] {
                    new Card("card_back"), new Card("card_back")
                },
                hasFolded    = false,
                currentBet   = 0,
                expression   = "neutral",
                hasGoneBust  = false
            });
        }

        playerList = new List<string> { "Sun Tsu", "Queen Liz", "BlackBeard" };

        // No community cards yet
        return gs;
    }

    /// <summary>
    /// Computes main and side pots based on contributions, returning a list of Pot slices.
    /// </summary>
    public List<Pot> ComputePots()
    {
        var levels = contributions.Values
                                   .Where(v => v > 0)
                                   .Distinct()
                                   .OrderBy(v => v)
                                   .ToList();

        var pots = new List<Pot>();
        int prev = 0;
        foreach (int level in levels)
        {
            // who put in at least 'level'?
            var eligible = new List<PlayerState>();
            foreach (var kv in contributions)
                if (kv.Value >= level && !kv.Key.hasFolded)
                    eligible.Add(kv.Key);

            int potAmount = (level - prev) * eligible.Count;
            pots.Add(new Pot { amount = potAmount, eligiblePlayers = eligible });
            prev = level;
        }
        return pots;
    }

    /// <summary>
    /// Resets and initializes per-player pot contributions at hand start.
    /// </summary>
    public void ResetPotTracking()
    {
        contributions.Clear();
        foreach (var p in players)
            contributions[p] = 0;
    }

    /// <summary>
    /// Advances isActive to the next player in rotation.
    /// </summary>
    public void AdvanceTurn()
    {
        players[activePlayerIndex].isActive = false;
        activePlayerIndex = (activePlayerIndex + 1) % players.Count;
        players[activePlayerIndex].isActive = true;
    }
}