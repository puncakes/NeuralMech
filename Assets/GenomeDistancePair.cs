using System;

internal struct GenomeDistancePair : IComparable<GenomeDistancePair>
{
    internal double _distance;
    internal NEATGenome _genome;

    internal GenomeDistancePair(double distance, NEATGenome genome)
    {
        _distance = distance;
        _genome = genome;
    }

    public int CompareTo(GenomeDistancePair other)
    {
        // Sorts in descending order.
        // Just remember, -1 means we don't change the order of x and y.
        if (_distance > other._distance)
        {
            return -1;
        }
        if (_distance < other._distance)
        {
            return 1;
        }
        return 0;
    }
}