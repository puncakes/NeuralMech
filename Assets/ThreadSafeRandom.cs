/// <summary>
/// A thread safe random number generator.
/// </summary>
using System;


public class ThreadSafeRandom
{
	/// <summary>
	/// A non-thread-safe random number generator that uses a time-based seed.
	/// </summary>
	private static readonly Random Random = new Random();
	
	/// <summary>
	/// Generate a random number between 0 and 1.
	/// </summary>
	/// <returns></returns>
	public static double NextDouble()
	{
		lock (Random)
		{
			return Random.NextDouble();
		}
	}
	
	/// <summary>
	/// Generate a random number
	/// </summary>
	/// <returns></returns>
	public static int Next(int max)
	{
		lock (Random)
		{
			return Random.Next(max);
		}
	}
}