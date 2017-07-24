using System;
/// <summary>
/// Static methods for roulette wheel selection from a set of choices with predefined probabilities.
/// </summary>
public static class RouletteWheel
{
    /// <summary>
    /// A simple single throw routine.
    /// </summary>
    /// <param name="probability">A probability between 0..1 that the throw will result in a true result.</param>
    /// <param name="rng">Random number generator.</param>
    public static bool SingleThrow(double probability)
    {
        return ThreadSafeRandom.NextDouble() < probability;
    }

    /// <summary>
    /// Performs a single throw for a given number of outcomes with equal probabilities.
    /// </summary>
    /// <param name="numberOfOutcomes">The number of possible outcomes.</param>
    /// <param name="rng">Random number generator.</param>
    /// <returns>An integer between 0..numberOfOutcomes-1. In effect this routine selects one of the possible outcomes.</returns>
    public static int SingleThrowEven(int numberOfOutcomes)
    {
        return (int)(ThreadSafeRandom.NextDouble() * numberOfOutcomes);
    }

    /// <summary>
    /// Performs a single throw onto a roulette wheel where the wheel's space is unevenly divided between outcomes.
    /// The probabilty that a segment will be selected is given by that segment's value in the 'probabilities'
    /// array within the specified RouletteWheelLayout. The probabilities within a RouletteWheelLayout have already 
    /// been normalised so that their total is always equal to 1.0.
    /// </summary>
    /// <param name="layout">The roulette wheel layout.</param>
    /// <param name="rng">Random number generator.</param>
    public static int SingleThrow(RouletteWheelLayout layout)
    {
        // Throw the ball and return an integer indicating the outcome.
        double throwValue = layout.ProbabilitiesTotal * ThreadSafeRandom.NextDouble();
        double accumulator = 0.0;
        for (int i = 0; i < layout.Probabilities.Length; i++)
        {
            accumulator += layout.Probabilities[i];
            if (throwValue < accumulator)
            {
                return layout.Labels[i];
            }
        }

        // We might get here through floating point arithmetic rounding issues. 
        // e.g. accumulator == throwValue. 

        // Find a nearby non-zero probability to select.
        // Wrap around to start of array.
        for (int i = 0; i < layout.Probabilities.Length; i++)
        {
            if (layout.Probabilities[i] != 0.0)
            {
                return layout.Labels[i];
            }
        }

        // If we get here then we have an array of zero probabilities.
        throw new Exception("Invalid operation. No non-zero probabilities to select.");
    }
}