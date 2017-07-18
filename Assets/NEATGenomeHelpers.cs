using System;
using System.Collections.Generic;

public enum NodeType
{
    Input,
    Output,
    Hidden,
    Bias
}

public enum CorrelationItemType
{
	Match,
	Disjoint,
	Excess
}

public enum Mutation
{
    ModifyWeights,
    AddNode,
    AddConnection,
    RemoveConnection
}

public class NodeGene
{
    public HashSet<int> InputNodes;
    public HashSet<int> OutputNodes;
    public List<ConnectionGene> srcConnections;
    public int innovationIndex;
    public NodeType ntype;
    public int depth;
    public double activationSum;

    public NodeGene()
    {

    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="copyFrom">NeuronGene to copy from.</param>
    /// <param name="copyConnectivityData">Indicates whether or not top copy connectivity data for the neuron.</param>
    public NodeGene(NodeGene copyFrom, bool copyConnectivityData)
    {
        innovationIndex = copyFrom.innovationIndex;
        ntype = copyFrom.ntype;
        activationSum = copyFrom.activationSum;

        if (copyConnectivityData)
        {
            InputNodes = new HashSet<int>(copyFrom.InputNodes);
            OutputNodes = new HashSet<int>(copyFrom.OutputNodes);
        }
        else
        {
            InputNodes = new HashSet<int>();
            OutputNodes = new HashSet<int>();
        }
    }

    public NodeGene createCopy()
    {
        return new NodeGene(this, true);
    }
}

public class ConnectionGene
{
    public int srcInnovationIndex;
    public int destInnovationIndex;
    public double weight;
    public bool enabled;
    public int innovationIndex;

    public ConnectionGene()
    {

    }

    public ConnectionGene(int srcInnovationIndex, int destInnovationIndex, double weight, bool enabled, int innovationIndex)
    {
        this.srcInnovationIndex = srcInnovationIndex;
        this.destInnovationIndex = destInnovationIndex;
        this.weight = weight;
        this.enabled = enabled;
        this.innovationIndex = innovationIndex;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="copyFrom">NeuronGene to copy from.</param>
    /// <param name="copyConnectivityData">Indicates whether or not top copy connectivity data for the neuron.</param>
    public ConnectionGene(ConnectionGene copyFrom)
    {
        innovationIndex = copyFrom.innovationIndex;
        srcInnovationIndex = copyFrom.srcInnovationIndex;
        destInnovationIndex = copyFrom.destInnovationIndex;
        weight = copyFrom.weight;
        enabled = copyFrom.enabled;
    }

    public ConnectionGene createCopy()
    {
        return new ConnectionGene(this);
    }
}

public struct ConnectionGeneRecord : IEqualityComparer<ConnectionGeneRecord>
{
    readonly int _srcNodeId;
    readonly int _tgtNodeId;

    #region Constructor

    /// <summary>
    /// Construct with the provided source and target node IDs.
    /// </summary>
    public ConnectionGeneRecord(int sourceNodeId, int targetNodeId)
    {
        _srcNodeId = sourceNodeId;
        _tgtNodeId = targetNodeId;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the source node ID.
    /// </summary>
    public int SourceNodeId
    {
        get { return _srcNodeId; }
    }

    /// <summary>
    /// Gets the target node ID.
    /// </summary>
    public int TargetNodeId
    {
        get { return _tgtNodeId; }
    }

    #endregion

    #region IEqualityComparer<ConnectionEndpointsStruct> Members

    /// <summary>
    /// Implementation for IEqualityComparer.
    /// </summary>
    public bool Equals(ConnectionGeneRecord x, ConnectionGeneRecord y)
    {
        return (x._srcNodeId == y._srcNodeId) && (x._tgtNodeId == y._tgtNodeId);
    }

    /// <summary>
    /// Implementation for IEqualityComparer.
    /// </summary>
    public int GetHashCode(ConnectionGeneRecord obj)
    {
        // Drawing.Point uses x^y for a hash, but this is actually an extremely poor hash function
        // for a pair of coordinates. Here we swap the low and high 16 bits of one of the 
        // Id's to generate a much better hash for our (and most other likely) circumstances.
        return (int)(_srcNodeId ^ ((_tgtNodeId >> 16) + (_tgtNodeId << 16)));

        // ENHANCEMENT: Consider better hashes such as FNV or SuperFastHash
        // Also try this from Java's com.sun.hotspot.igv.data.Pair class.
        // return (int)(_srcNeuronId * 71u + _tgtNeuronId);
    }

    #endregion
}

public struct NodeGeneRecord
{
    /// <summary>
    /// Represents an added neuron. When a neuron is added to a neural network in NEAT an existing
    /// connection between two neurons is discarded and replaced with the new neuron and two new connections,
    /// one connection between the source neuron and the new neuron and another from the new neuron to the target neuron.
    /// This struct represents those three IDs.
    /// 
    /// This struct exists to represent newly added structure in a history buffer of added structures. This allows us to 
    /// re-use IDs where a mutation recreates a structure that has previously occured through previous mutations on other 
    /// genomes.
    /// </summary>
    readonly int _addedNodeId;
    readonly int _addedInputConnectionId;
    readonly int _addedOutputConnectionId;

    #region Constructor

    /// <summary>
    /// Construct by assigning new IDs gemnerated by the provided UInt32IdGenerator.
    /// </summary>
    public NodeGeneRecord(int nodeId, int inputConnectionId, int outputConnectionId)
    {
        _addedNodeId = nodeId;
        _addedInputConnectionId = inputConnectionId;
        _addedOutputConnectionId = outputConnectionId;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the added neuron's ID.
    /// </summary>
    public int AddedNeuronId
    {
        get { return _addedNodeId; }
    }

    /// <summary>
    /// Gets the added input connection's ID.
    /// </summary>
    public int AddedInputConnectionId
    {
        get { return _addedInputConnectionId; }
    }

    /// <summary>
    /// Gets the added output connection's ID.
    /// </summary>
    public int AddedOutputConnectionId
    {
        get { return _addedOutputConnectionId; }
    }

    #endregion
}

public class NetworkLayer
{
    public NodeGeneList NodesInLayer;
    public int layerDepth;
}

// ENHANCEMENT: Consider switching to a SortedList[K,V] - which guarantees item sort order at all times. 

/// <summary>
/// Represents a sorted list of NeuronGene objects. The sorting of the items is done on request
/// rather than being strictly enforced at all times (e.g. as part of adding and removing items). This
/// approach is currently more convenient for use in some of the routines that work with NEAT genomes.
/// 
/// Because we are not using a strictly sorted list such as the generic class SortedList[K,V] a customised 
/// BinarySearch() method is provided for fast lookup of items if the list is known to be sorted. If the list is
/// not sorted then the BinarySearch method's behaviour is undefined. This is potentially a source of bugs 
/// and thus this class should probably migrate to SortedList[K,V] or be modified to ensure items are sorted 
/// prior to a binary search.
/// 
/// Sort order is with respect to connection gene innovation ID.
/// </summary>
public class NodeGeneList : List<NodeGene>
{
    #region Constructors

    /// <summary>
    /// Construct an empty list.
    /// </summary>
    public NodeGeneList()
    {
    }

    /// <summary>
    /// Construct an empty list with the specified capacity.
    /// </summary>
    public NodeGeneList(int capacity) : base(capacity)
    {
    }

    /// <summary>
    /// Copy constructor. The newly allocated list has a capacity 1 larger than copyFrom
    /// allowing for a single add node mutation to occur without reallocation of memory.
    /// </summary>
    public NodeGeneList(ICollection<NodeGene> copyFrom)
        : base(copyFrom.Count + 1)
    {
        foreach (NodeGene srcGene in copyFrom)
        {
            Add(srcGene.createCopy());
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts a NeuronGene into its correct (sorted) location within the gene list.
    /// Normally neuron genes can safely be assumed to have a new Innovation ID higher
    /// than all existing IDs, and so we can just call Add().
    /// This routine handles genes with older IDs that need placing correctly.
    /// </summary>
    public void InsertIntoPosition(NodeGene nodeGene)
    {
        // Determine the insert idx with a linear search, starting from the end 
        // since mostly we expect to be adding genes that belong only 1 or 2 genes
        // from the end at most.
        int idx = Count - 1;
        for (; idx > -1; idx--)
        {
            if (this[idx].innovationIndex < nodeGene.innovationIndex)
            {   // Insert idx found.
                break;
            }
        }
        Insert(idx + 1, nodeGene);
    }

    /// <summary>
    /// Remove the neuron gene with the specified innovation ID.
    /// Returns the removed gene.
    /// </summary>
    public NodeGene Remove(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {
            throw new ApplicationException("Attempt to remove neuron with an unknown neuronId");
        }
        NodeGene nodeGene = this[idx];
        RemoveAt(idx);
        return nodeGene;
    }

    /// <summary>
    /// Gets the neuron gene with the specified innovation ID using a fast binary search. 
    /// Returns null if no such gene is in the list.
    /// </summary>
    public NodeGene GetNodeById(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {   // Not found.
            return null;
        }
        return this[idx];
    }

    /// <summary>
    /// Sort neuron gene's into ascending order by their innovation IDs.
    /// </summary>
    public void SortByInnovationId()
    {
        Sort(delegate (NodeGene x, NodeGene y)
        {
            // Test the most likely cases first.
            if (x.innovationIndex < y.innovationIndex)
            {
                return -1;
            }
            if (x.innovationIndex > y.innovationIndex)
            {
                return 1;
            }
            return 0;
        });
    }

    /// <summary>
    /// Obtain the index of the gene with the specified ID by performing a binary search.
    /// Binary search is fast and can be performed so long as the genes are sorted by ID.
    /// If the genes are not sorted then the behaviour of this method is undefined.
    /// </summary>
    public int BinarySearch(int id)
    {
        int lo = 0;
        int hi = Count - 1;

        while (lo <= hi)
        {
            int i = (lo + hi) >> 1;

            if (this[i].innovationIndex < id)
            {
                lo = i + 1;
            }
            else if (this[i].innovationIndex > id)
            {
                hi = i - 1;
            }
            else
            {
                return i;
            }
        }

        return ~lo;
    }

    /// <summary>
    /// For debug purposes only. Don't call this method in normal circumstances as it is an
    /// expensive O(n) operation.
    /// </summary>
    public bool IsSorted()
    {
        int count = this.Count;
        if (0 == count)
        {
            return true;
        }

        int prev = this[0].innovationIndex;
        for (int i = 1; i < count; i++)
        {
            if (this[i].innovationIndex <= prev)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}

public class ConnectionGeneList : List<ConnectionGene>
{
    #region Constructors

    /// <summary>
    /// Construct an empty list.
    /// </summary>
    public ConnectionGeneList()
    {
    }

    /// <summary>
    /// Construct an empty list with the specified capacity.
    /// </summary>
    public ConnectionGeneList(int capacity) : base(capacity)
    {
    }

    /// <summary>
    /// Copy constructor. The newly allocated list has a capacity 1 larger than copyFrom
    /// allowing for a single add node mutation to occur without reallocation of memory.
    /// </summary>
    public ConnectionGeneList(ICollection<ConnectionGene> copyFrom)
        : base(copyFrom.Count + 1)
    {
        foreach (ConnectionGene srcGene in copyFrom)
        {
            Add(srcGene.createCopy());
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts a NeuronGene into its correct (sorted) location within the gene list.
    /// Normally neuron genes can safely be assumed to have a new Innovation ID higher
    /// than all existing IDs, and so we can just call Add().
    /// This routine handles genes with older IDs that need placing correctly.
    /// </summary>
    public void InsertIntoPosition(ConnectionGene nodeGene)
    {
        // Determine the insert idx with a linear search, starting from the end 
        // since mostly we expect to be adding genes that belong only 1 or 2 genes
        // from the end at most.
        int idx = Count - 1;
        for (; idx > -1; idx--)
        {
            if (this[idx].innovationIndex < nodeGene.innovationIndex)
            {   // Insert idx found.
                break;
            }
        }
        Insert(idx + 1, nodeGene);
    }

    /// <summary>
    /// Remove the neuron gene with the specified innovation ID.
    /// Returns the removed gene.
    /// </summary>
    public ConnectionGene Remove(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {
            throw new ApplicationException("Attempt to remove neuron with an unknown neuronId");
        }
        ConnectionGene nodeGene = this[idx];
        RemoveAt(idx);
        return nodeGene;
    }

    /// <summary>
    /// Gets the neuron gene with the specified innovation ID using a fast binary search. 
    /// Returns null if no such gene is in the list.
    /// </summary>
    public ConnectionGene GetNodeById(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {   // Not found.
            return null;
        }
        return this[idx];
    }

    /// <summary>
    /// Sort neuron gene's into ascending order by their innovation IDs.
    /// </summary>
    public void SortByInnovationId()
    {
        Sort(delegate (ConnectionGene x, ConnectionGene y)
        {
            // Test the most likely cases first.
            if (x.innovationIndex < y.innovationIndex)
            {
                return -1;
            }
            if (x.innovationIndex > y.innovationIndex)
            {
                return 1;
            }
            return 0;
        });
    }

    /// <summary>
    /// Obtain the index of the gene with the specified ID by performing a binary search.
    /// Binary search is fast and can be performed so long as the genes are sorted by ID.
    /// If the genes are not sorted then the behaviour of this method is undefined.
    /// </summary>
    public int BinarySearch(int id)
    {
        int lo = 0;
        int hi = Count - 1;

        while (lo <= hi)
        {
            int i = (lo + hi) >> 1;

            if (this[i].innovationIndex < id)
            {
                lo = i + 1;
            }
            else if (this[i].innovationIndex > id)
            {
                hi = i - 1;
            }
            else
            {
                return i;
            }
        }

        return ~lo;
    }

    /// <summary>
    /// For debug purposes only. Don't call this method in normal circumstances as it is an
    /// expensive O(n) operation.
    /// </summary>
    public bool IsSorted()
    {
        int count = this.Count;
        if (0 == count)
        {
            return true;
        }

        int prev = this[0].innovationIndex;
        for (int i = 1; i < count; i++)
        {
            if (this[i].innovationIndex <= prev)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}

/// <summary>
/// Statistics resulting from the comparison of two NEAT genomes.
/// </summary>
public class CorrelationStatistics
{
	int _matchingGeneCount;
	int _disjointConnectionGeneCount;
	int _excessConnectionGeneCount;
	double _connectionWeightDelta;

	#region Properties

	/// <summary>
	/// Gets or sets the number of matching connection genes between the two comparison genomes.
	/// </summary>
	public int MatchingGeneCount
	{
		get { return _matchingGeneCount; }
		set { _matchingGeneCount = value; }
	}

	/// <summary>
	/// Gets or sets the number of disjoint connection genes between the two comparison genomes.
	/// </summary>
	public int DisjointConnectionGeneCount
	{
		get { return _disjointConnectionGeneCount; }
		set { _disjointConnectionGeneCount = value; }
	}

	/// <summary>
	/// Gets or sets the number of excess connection genes between the two comparison genomes.
	/// </summary>
	public int ExcessConnectionGeneCount
	{
		get { return _excessConnectionGeneCount; }
		set { _excessConnectionGeneCount = value; }
	}

	/// <summary>
	/// Gets or sets the cumulative total of absolute weight differences between all of the connection genes that matched up.
	/// </summary>
	public double ConnectionWeightDelta
	{
		get { return _connectionWeightDelta; }
		set { _connectionWeightDelta = value; }
	}

	#endregion
}

/// <summary>
/// A single comparison item resulting from the comparison of two genomes. If the CorrelationItemType
/// is Match then both connection gene properties will be non-null, otherwise one of them will be null 
/// and the other will hold a reference to a disjoint or excess connection gene.
/// 
/// Note. We generally only compare connection genes when comparing genomes. Connection genes along with
/// their innovation IDs actually represent the complete network topology (and of course the connection weights).
/// </summary>
public class CorrelationItem
{
	readonly CorrelationItemType _correlationItemType;
	readonly ConnectionGene _connectionGene1;
	readonly ConnectionGene _connectionGene2;

	#region Constructor

	/// <summary>
	/// Constructs a new CorrelationItem.
	/// </summary>
	public CorrelationItem(CorrelationItemType correlationItemType, ConnectionGene connectionGene1, ConnectionGene connectionGene2)
	{
		_correlationItemType = correlationItemType;
		_connectionGene1 = connectionGene1;
		_connectionGene2 = connectionGene2;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the CorrelationItemType.
	/// </summary>
	public CorrelationItemType CorrelationItemType
	{
		get { return _correlationItemType; }
	}

	/// <summary>
	/// Gets the corresponding connection gene from comparison genome 1.
	/// </summary>
	public ConnectionGene ConnectionGene1
	{
		get { return _connectionGene1; }
	}

	/// <summary>
	/// Gets the corresponding connection gene from comparison genome 2.
	/// </summary>
	public ConnectionGene ConnectionGene2
	{
		get { return _connectionGene2; }
	}

	#endregion
}

/// <summary>
/// The results from comparing two NEAT genomes and correlating their connection genes.
/// </summary>
public class CorrelationResults
{
	readonly CorrelationStatistics _correlationStatistics = new CorrelationStatistics();
	readonly List<CorrelationItem> _correlationItemList;

	#region Constructor

	/// <summary>
	/// Constructs with a specified initial correlation item list capacity.
	/// </summary>
	public CorrelationResults(int itemListCapacity)
	{
		_correlationItemList = new List<CorrelationItem>(itemListCapacity);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the statistics for the genome comparison.
	/// </summary>
	public CorrelationStatistics CorrelationStatistics
	{
		get { return _correlationStatistics; }
	}

	/// <summary>
	/// Gets the list of correlation items from the genome comparison.
	/// </summary>
	public List<CorrelationItem> CorrelationItemList
	{
		get { return _correlationItemList; }
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Performs an integrity check on the correlation items.
	/// Returns true if the test is passed.
	/// </summary>
	public bool PerformIntegrityCheck()
	{
		long prevInnovationId = -1;

		foreach(CorrelationItem item in _correlationItemList)
		{
			if(item.CorrelationItemType==CorrelationItemType.Match)
			{
				if(item.ConnectionGene1==null || item.ConnectionGene2==null) {
					return false;
				}

				if((item.ConnectionGene1.innovationIndex != item.ConnectionGene2.innovationIndex)
					|| (item.ConnectionGene1.srcInnovationIndex != item.ConnectionGene2.srcInnovationIndex)
					|| (item.ConnectionGene1.destInnovationIndex != item.ConnectionGene2.destInnovationIndex)) {
					return false;
				}

				// Innovation ID's should be in order and not duplicated.
				if(item.ConnectionGene1.innovationIndex <= prevInnovationId) {
					return false;
				}
				prevInnovationId = item.ConnectionGene1.innovationIndex;
			}
			else // Disjoint or excess gene.
			{
				if((item.ConnectionGene1==null && item.ConnectionGene2==null)
					|| (item.ConnectionGene1!=null && item.ConnectionGene2!=null))
				{   // Precisely one gene should be present.
					return false;
				}
				if(item.ConnectionGene1 != null)
				{
					if(item.ConnectionGene1.innovationIndex <= prevInnovationId) {
						return false;
					}
					prevInnovationId = item.ConnectionGene1.innovationIndex;
				}
				else // ConnectionGene2 is present.
				{
					if(item.ConnectionGene2.innovationIndex <= prevInnovationId) {
						return false;
					}
					prevInnovationId = item.ConnectionGene2.innovationIndex;
				}
			}
		}
		return true;
	}

	#endregion
}