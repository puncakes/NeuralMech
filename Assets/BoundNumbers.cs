/// <summary>
/// A simple class that prevents numbers from getting either too
/// big or too small.
/// </summary>
public static class BoundNumbers
{
	/// <summary>
	/// Too small of a number.
	/// </summary>
	public const double TooSmall = -1.0E20;
	
	/// <summary>
	/// Too big of a number.
	/// </summary>
	public const double TooBig = 1.0E20;
	
	/// <summary>
	/// Bound the number so that it does not become too big or too small.
	/// </summary>
	/// <param name="d">The number to check.</param>
	/// <returns>The new number. Only changed if it was too big or too small.</returns>
	public static double Bound(double d)
	{
		if (d < TooSmall)
		{
			return TooSmall;
		}
		return d > TooBig ? TooBig : d;
	}
}