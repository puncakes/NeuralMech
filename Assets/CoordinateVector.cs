using System.Collections.Generic;
/// <summary>
/// General purpose representation of a point in a multidimensional space. A vector of coordinates, 
/// each coordinate defining the position within a dimension/axis defined by an ID.
/// </summary>
public class CoordinateVector
{
    readonly KeyValuePair<int, double>[] _coordElemArray;

    #region Constructor

    /// <summary>
    /// Constructs a CoordinateVector using the provided array of ID/coordinate pairs.
    /// CoordinateVector elements must be sorted by ID.
    /// </summary>
    public CoordinateVector(KeyValuePair<int, double>[] coordElemArray)
    {
        //Debug.Assert(IsSorted(coordElemArray), "CoordinateVector elements must be sorted by ID.");
        _coordElemArray = coordElemArray;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets an array containing the ID/coordinate pairs.
    /// CoordinateVector elements are sorted by ID
    /// </summary>
    public KeyValuePair<int, double>[] CoordArray
    {
        get { return _coordElemArray; }
    }

    #endregion

    #region Static Methods [Debugging]

    private static bool IsSorted(KeyValuePair<int, double>[] coordElemArray)
    {
        if (0 == coordElemArray.Length)
        {
            return true;
        }

        int prevId = coordElemArray[0].Key;
        for (int i = 1; i < coordElemArray.Length; i++)
        {   // <= also checks for duplicates as well as sort order.
            if (coordElemArray[i].Key <= prevId)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}