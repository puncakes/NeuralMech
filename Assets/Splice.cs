using UnityEngine;
using System.Collections.Generic;

public class Splice : ICrossover
{
	/// <summary>
	/// The cut length.
	/// </summary>
	///
	private readonly int _cutLength;
	
	/// <summary>
	/// Create a slice crossover with the specified cut length.
	/// </summary>
	///
	/// <param name="theCutLength">The cut length.</param>
	public Splice(int theCutLength)
	{
		_cutLength = theCutLength;
	}
	
	#region ICrossover Members
	
	/// <summary>
	/// Assuming this chromosome is the "mother" mate with the passed in
	/// "father".
	/// </summary>
	///
	/// <param name="mother">The mother.</param>
	/// <param name="father">The father.</param>
	/// <param name="offspring1">Returns the first offspring</param>
	/// <param name="offspring2">Returns the second offspring.</param>
	public void Mate(Robot mother, Robot father,
	                 Robot offspring1, Robot offspring2)
	{
		int geneLength = mother.getBrain().CalculateSize();
		
		// the chromosome must be cut at two positions, determine them
		var cutpoint1 = (int)(ThreadSafeRandom.NextDouble() * (geneLength - _cutLength));
		int cutpoint2 = cutpoint1 + _cutLength;
		
		// handle cut section
		for (int i = 0; i < geneLength; i++)
		{
			if (!((i < cutpoint1) || (i > cutpoint2)))
			{
				offspring1.getBrain().Weights[i] = father.getBrain().Weights[i];
				offspring2.getBrain().Weights[i] = mother.getBrain().Weights[i];
			}
		}
		
		// handle outer sections
		for (int i = 0; i < geneLength; i++)
		{
			if ((i < cutpoint1) || (i > cutpoint2))
			{
				offspring1.getBrain().Weights[i] = mother.getBrain().Weights[i];
				offspring2.getBrain().Weights[i] = father.getBrain().Weights[i];
			}
		}
	}
	
	#endregion
}


