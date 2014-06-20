/// <summary>
/// A simple mutation based on random numbers.
/// </summary>
///
public class DefaultMutation : IMutate
{
	/// <summary>
	/// The amount to perturb by.
	/// </summary>
	///
	private readonly double _perturbAmount;
	
	/// <summary>
	/// Construct a perturb mutation.
	/// </summary>
	///
	/// <param name="thePerturbAmount">The amount to mutate by(percent).</param>
	public DefaultMutation(double thePerturbAmount)
	{
		_perturbAmount = thePerturbAmount;
	}
	
	#region IMutate Members
	
	/// <summary>
	/// Perform a perturb mutation on the specified chromosome.
	/// </summary>
	///
	/// <param name="chromosome">The chromosome to mutate.</param>
	public void PerformMutation(NeuralNetwork network)
	{
		for (int i = 0; i < network.CalculateSize(); i++)
		{
			network.Weights[i] += (_perturbAmount - (ThreadSafeRandom.NextDouble() * _perturbAmount * 2.0));
		}
	}
	
	#endregion
}