/// <summary>
/// Represents the layout of a roulette wheel where each sector has different degrees of arc and thus
/// probability of being selected.
/// For use by the RouletteWheel class.
/// </summary>
public class RouletteWheelLayout
{
    readonly double _probabilitiesTotal;
    readonly double[] _probabilities;
    readonly int[] _labels;

    #region Constructor

    /// <summary>
    /// Construct the layout with provided probabilities. The provided probabilites do not have to add 
    /// up to 1.0 as implicitly normalise them when using the layout.
    /// </summary>
    public RouletteWheelLayout(double[] probabilities)
    {
        // Total up probabilities. Rather than ensuring all probabilites sum to 1.0 we sum them and interpret
        // the probability values as proportions of the total (rather than actual probabilities).
        _probabilitiesTotal = 0.0;
        _labels = new int[probabilities.Length];
        for (int i = 0; i < probabilities.Length; i++)
        {
            _probabilitiesTotal += probabilities[i];
            _labels[i] = i;
        }

        // Handle special case where all provided probabilities are zero. In that case we evenly 
        // assign probabilities across all choices.
        if (0.0 == _probabilitiesTotal)
        {
            _probabilitiesTotal = probabilities.Length;
            for (int i = 0; i < probabilities.Length; i++)
            {
                probabilities[i] = 1.0;
            }
        }
        _probabilities = probabilities;
    }

    /// <summary>
    /// Construct the layout with provided probabilities and associated label values. The provided probabilites
    /// do not have to add up to 1.0 as implicitly normalise them when using the layout.
    /// </summary>
    public RouletteWheelLayout(double[] probabilities, int[] labels)
    {
        _labels = labels;

        // Total up probabilities. Rather than ensuring all probabilites sum to 1.0 we sum them and interpret
        // the probability values as proportions of the total (rather than actual probabilities).
        _probabilitiesTotal = 0.0;
        for (int i = 0; i < probabilities.Length; i++)
        {
            _probabilitiesTotal += probabilities[i];
        }

        // Handle special case where all provided probabilities are zero. In that case we evenly 
        // assign probabilities across all choices.
        if (0.0 == _probabilitiesTotal)
        {
            _probabilitiesTotal = probabilities.Length;
            for (int i = 0; i < probabilities.Length; i++)
            {
                probabilities[i] = 1.0;
            }
        }
        _probabilities = probabilities;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public RouletteWheelLayout(RouletteWheelLayout copyFrom)
    {
        _probabilitiesTotal = copyFrom._probabilitiesTotal;
        _probabilities = (double[])copyFrom._probabilities.Clone();
        _labels = (int[])copyFrom._labels.Clone();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the total of all values within <see cref="Probabilities"/>.
    /// </summary>
    public double ProbabilitiesTotal
    {
        get { return _probabilitiesTotal; }
    }

    /// <summary>
    /// Gets the array of probabilities.
    /// </summary>
    public double[] Probabilities
    {
        get { return _probabilities; }
    }

    /// <summary>
    /// Gets the array of outcome labels.
    /// </summary>
    public int[] Labels
    {
        get { return _labels; }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Remove the specified outcome from the set of probabilities and return as a new RouletteWheelLayout object.
    /// </summary>
    public RouletteWheelLayout RemoveOutcome(int labelId)
    {
        // Find the item with specified label.
        int idx = -1;
        for (int i = 0; i < _labels.Length; i++)
        {
            if (labelId == _labels[i])
            {
                idx = i;
                break;
            }
        }

        Debug.Assert(-1 != idx, "label not found");

        double[] probabilities = new double[_probabilities.Length - 1];
        int[] labels = new int[_probabilities.Length - 1];
        for (int i = 0; i < idx; i++)
        {
            probabilities[i] = _probabilities[i];
            labels[i] = _labels[i];
        }
        for (int i = idx + 1, j = idx; i < _probabilities.Length; i++, j++)
        {
            probabilities[j] = _probabilities[i];
            labels[j] = _labels[i];
        }
        return new RouletteWheelLayout(probabilities, labels);
    }

    #endregion

    #region Private Methods

    private void Init(double[] probabilities, int[] labels)
    {

    }

    #endregion
}