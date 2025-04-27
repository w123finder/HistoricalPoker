using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Evaluates the best 5-card poker hand from a player's hole cards and community cards.
/// </summary>
public class Evaluator
{
    /// <summary>
    /// Determines the highest-ranking 5-card hand from 2 hole cards and up to 5 community cards.
    /// </summary>
    /// <param name="holeCards">Array of exactly 2 player hole cards.</param>
    /// <param name="communityCards">List of 0–5 shared community cards.</param>
    /// <returns>The best HandValue found among all 5-card combinations.</returns>
    public HandValue Evaluate(Card[] holeCards, List<Card> communityCards)
    {
        var all = holeCards.Concat(communityCards).ToArray();
        HandValue best = new HandValue(HandCategory.HighCard, 0,0,0,0,0);

        // generate every 5-card combination out of the 7 cards
        var combo = Combinations(all, 5);
        foreach (var five in combo)
        {
            var hv = Evaluate5(five);
            if (hv.CompareTo(best) > 0)
                best = hv;
        }
        return best;
    }

    /// <summary>
    /// Scores exactly 5 cards and returns its HandValue (e.g. flush, straight, etc.).
    /// </summary>
    // Evaluate exactly 5 cards
    private HandValue Evaluate5(Card[] cards)
    {
        // sort descending by rank
        var byRank = cards.OrderByDescending(c => c.Rank).ToArray();
        var ranks  = byRank.Select(c => c.Rank).ToArray();
        var suits  = cards.Select(c => c.Suit).ToArray();

        bool isFlush    = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(ranks, out int topStraight);

        // count frequencies
        var freq = ranks.GroupBy(r=>r)
                        .ToDictionary(g=>g.Key, g=>g.Count());
        var quads = freq.Where(kv=>kv.Value==4).Select(kv=>kv.Key).FirstOrDefault();
        var trips = freq.Where(kv=>kv.Value==3).Select(kv=>kv.Key).FirstOrDefault();
        var pairs = freq.Where(kv=>kv.Value==2).Select(kv=>kv.Key).OrderByDescending(x=>x).ToArray();

        // Straight flush
        if (isFlush && isStraight)
            return new HandValue(HandCategory.StraightFlush, topStraight);

        // Quads
        if (quads > 0)
        {
            int kicker = ranks.Where(r=>r!=quads).Max();
            return new HandValue(HandCategory.Quads, quads, kicker);
        }

        // Full house
        if (trips > 0 && pairs.Length >= 1)
            return new HandValue(HandCategory.FullHouse, trips, pairs[0]);

        // Flush
        if (isFlush)
            return new HandValue(HandCategory.Flush, ranks.Take(5).ToArray());

        // Straight
        if (isStraight)
            return new HandValue(HandCategory.Straight, topStraight);

        // Trips
        if (trips > 0)
        {
            var kickers = ranks.Where(r=>r!=trips).Take(2).ToArray();
            return new HandValue(HandCategory.Trips, trips, kickers[0], kickers[1]);
        }

        // Two pair
        if (pairs.Length >= 2)
        {
            int highPair = pairs[0], lowPair = pairs[1];
            int kicker   = ranks.Where(r=>r!=highPair && r!=lowPair).First();
            return new HandValue(HandCategory.TwoPair, highPair, lowPair, kicker);
        }

        // One pair
        if (pairs.Length == 1)
        {
            int pairRank = pairs[0];
            var kickers  = ranks.Where(r=>r!=pairRank).Take(3).ToArray();
            return new HandValue(HandCategory.OnePair, pairRank, kickers[0], kickers[1], kickers[2]);
        }

        // High card
        return new HandValue(HandCategory.HighCard, ranks.Take(5).ToArray());
    }

    /// <summary>
    /// Checks for a straight in a sorted rank array (allowing wheel A-2-3-4-5).
    /// </summary>    
    private bool IsStraight(int[] ranks, out int topStraight)
    {
        topStraight = 0;
        var distinct = ranks.Distinct().ToList();
        // special wheel: A-2-3-4-5
        if (distinct.Contains(14)) distinct.Add(1);
        distinct.Sort((a,b)=>b.CompareTo(a)); // desc

        int consec = 1;
        for(int i=1;i<distinct.Count;i++)
        {
            if (distinct[i] == distinct[i-1] - 1)
                consec++;
            else
                consec = 1;

            if (consec >= 5)
            {
                topStraight = distinct[i-1] + 4;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Generates all k-sized combinations from the given array.
    /// </summary>
    private IEnumerable<Card[]> Combinations(Card[] arr, int k)
    {
        int n = arr.Length;
        int[] idx = new int[k];
        for(int i=0; i<k; i++) idx[i] = i;
        while (true)
        {
            var combo = new Card[k];
            for(int i=0; i<k; i++) combo[i] = arr[idx[i]];
            yield return combo;

            // advance indices
            int pos = k - 1;
            while (pos >= 0 && idx[pos] == n - k + pos) pos--;
            if (pos < 0) yield break;
            idx[pos]++;
            for(int j = pos+1; j<k; j++)
                idx[j] = idx[j-1] + 1;
        }
    }
}
