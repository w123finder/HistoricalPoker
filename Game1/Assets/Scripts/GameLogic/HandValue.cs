using System;
using System.Collections.Generic;

public struct HandValue : IComparable<HandValue>
{
    public HandCategory Category;
    public int[]         Kickers;   // descending

    public HandValue(HandCategory cat, params int[] kickers)
    {
        Category = cat;
        Kickers  = kickers;
    }

    public int CompareTo(HandValue other)
    {
        // higher Category wins
        int c = Category.CompareTo(other.Category);
        if (c != 0) return c;
        // tie-break by kickers lexicographically
        for(int i=0; i<Math.Min(Kickers.Length, other.Kickers.Length); i++)
        {
            if (Kickers[i] != other.Kickers[i])
                return Kickers[i].CompareTo(other.Kickers[i]);
        }
        return 0;
    }
}