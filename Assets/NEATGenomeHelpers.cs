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
    //
    public HashSet<int> InputConnections;
    public HashSet<int> OutputConnections;
    public int innovationIndex;
    public NodeType ntype;
    public int depth;
    public double activationSum;

    public NodeGene()
    {
        InputConnections = new HashSet<int>();
        OutputConnections = new HashSet<int>();
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="copyFrom">NodeGene to copy from.</param>
    /// <param name="copyConnectivityData">Indicates whether or not top copy connectivity data for the Node.</param>
    public NodeGene(NodeGene copyFrom, bool copyConnectivityData)
    {
        innovationIndex = copyFrom.innovationIndex;
        ntype = copyFrom.ntype;
        activationSum = copyFrom.activationSum;

        if (copyConnectivityData)
        {
            InputConnections = new HashSet<int>(copyFrom.InputConnections);
            OutputConnections = new HashSet<int>(copyFrom.OutputConnections);
        }
        else
        {
            InputConnections = new HashSet<int>();
            OutputConnections = new HashSet<int>();
        }
    }

    public NodeGene CreateCopy(bool copyData)
    {
        return new NodeGene(this, copyData);
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
    /// <param name="copyFrom">NodeGene to copy from.</param>
    /// <param name="copyConnectivityData">Indicates whether or not top copy connectivity data for the Node.</param>
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
        // return (int)(_srcNodeId * 71u + _tgtNodeId);
    }

    #endregion
}

public struct NodeGeneRecord
{
    /// <summary>
    /// Represents an added Node. When a Node is added to a neural network in NEAT an existing
    /// connection between two Nodes is discarded and replaced with the new Node and two new connections,
    /// one connection between the source Node and the new Node and another from the new Node to the target Node.
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
    /// Gets the added Node's ID.
    /// </summary>
    public int AddedNodeId
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

    public NetworkLayer()
    {
        NodesInLayer = new NodeGeneList();
        layerDepth = -1;
    }
}

// ENHANCEMENT: Consider switching to a SortedList[K,V] - which guarantees item sort order at all times. 

/// <summary>
/// Represents a sorted list of NodeGene objects. The sorting of the items is done on request
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
            Add(srcGene.CreateCopy(true));
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts a NodeGene into its correct (sorted) location within the gene list.
    /// Normally Node genes can safely be assumed to have a new Innovation ID higher
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
    /// Remove the Node gene with the specified innovation ID.
    /// Returns the removed gene.
    /// </summary>
    public NodeGene Remove(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {
            throw new ApplicationException("Attempt to remove Node with an unknown NodeId");
        }
        NodeGene nodeGene = this[idx];
        RemoveAt(idx);
        return nodeGene;
    }

    /// <summary>
    /// Gets the Node gene with the specified innovation ID using a fast binary search. 
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
    /// Sort Node gene's into ascending order by their innovation IDs.
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
    /// Inserts a NodeGene into its correct (sorted) location within the gene list.
    /// Normally Node genes can safely be assumed to have a new Innovation ID higher
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
    /// Remove the Node gene with the specified innovation ID.
    /// Returns the removed gene.
    /// </summary>
    public ConnectionGene Remove(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {
            throw new ApplicationException("Attempt to remove Node with an unknown NodeId");
        }
        ConnectionGene nodeGene = this[idx];
        RemoveAt(idx);
        return nodeGene;
    }

    /// <summary>
    /// Gets the Node gene with the specified innovation ID using a fast binary search. 
    /// Returns null if no such gene is in the list.
    /// </summary>
    public ConnectionGene GetConnectionById(int nodeId)
    {
        int idx = BinarySearch(nodeId);
        if (idx < 0)
        {   // Not found.
            return null;
        }
        return this[idx];
    }

    /// <summary>
    /// Sort Node gene's into ascending order by their innovation IDs.
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

/// <summary>
/// Used for building a list of connection genes. 
/// 
/// Connection genes are added one by one to a list and a dictionary of added connection genes is maintained
/// keyed on ConnectionEndpointsStruct to allow a caller to check if a connection with the same end points
/// (and potentially a different innovation ID) already exists in the list.
/// </summary>
public class ConnectionGeneListBuilder
{
    readonly ConnectionGeneList _connectionGeneList;
    readonly Dictionary<ConnectionGeneRecord, ConnectionGene> _connectionGeneDictionary;
    readonly SortedDictionary<int, NodeGene> _nodeDictionary;
    // Note. connection gene innovation IDs always start above zero as they share the ID space with nodes, 
    // which always come first (e.g. bias node is always ID 0).
    int _highestConnectionGeneId = 0;

    #region Constructor

    /// <summary>
    /// Constructs the builder with the provided capacity. The capacity should be chosen 
    /// to limit the number of memory re-allocations that occur within the contained
    /// connection list dictionary.
    /// </summary>
    public ConnectionGeneListBuilder(int connectionCapacity)
    {
        _connectionGeneList = new ConnectionGeneList(connectionCapacity);
        _connectionGeneDictionary = new Dictionary<ConnectionGeneRecord, ConnectionGene>(connectionCapacity);
        // TODO: Determine better initial capacity.
        _nodeDictionary = new SortedDictionary<int, NodeGene>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the contained list of connection genes.
    /// </summary>
    public ConnectionGeneList ConnectionGeneList
    {
        get { return _connectionGeneList; }
    }

    /// <summary>
    /// Gets the builder's dictionary of connection genes keyed on ConnectionEndpointsStruct.
    /// </summary>
    public Dictionary<ConnectionGeneRecord, ConnectionGene> ConnectionGeneDictionary
    {
        get { return _connectionGeneDictionary; }
    }

    /// <summary>
    /// Gets the builder's dictionary of node IDs obtained from contained connection gene endpoints.
    /// </summary>
    public SortedDictionary<int, NodeGene> NodeDictionary
    {
        get { return _nodeDictionary; }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Add a ConnectionGene to the builder, but only if the connection is not already present (as determined by it's node ID endpoints).
    /// </summary>
    /// <param name="connectionGene">The connection to add.</param>
    /// <param name="parentGenome">The conenction's parent genome. This is used to obtain NodeGene(s) for the connection endpoints.</param>
    /// <param name="overwriteExisting">A flag that indicates if this connection should take precedence oevr an existing connection with
    /// the same endpoints.</param>
    public void TryAddGene(ConnectionGene connectionGene, NEATGenome parentGenome, bool overwriteExisting)
    {
        // Check if a matching gene has already been added.
        ConnectionGeneRecord connectionKey = new ConnectionGeneRecord(connectionGene.srcInnovationIndex, connectionGene.destInnovationIndex);

        ConnectionGene existingConnectionGene;
        if (!_connectionGeneDictionary.TryGetValue(connectionKey, out existingConnectionGene))
        {   // Add new connection gene.
            ConnectionGene connectionGeneCopy = new ConnectionGene(connectionGene);
            _connectionGeneDictionary.Add(connectionKey, connectionGeneCopy);

            // Insert connection gene into a list. Use more efficient approach (append to end) if we know the gene belongs at the end.
            if (connectionGeneCopy.innovationIndex > _highestConnectionGeneId)
            {
                _connectionGeneList.Add(connectionGeneCopy);
                _highestConnectionGeneId = connectionGeneCopy.innovationIndex;
            }
            else
            {
                _connectionGeneList.InsertIntoPosition(connectionGeneCopy);
            }

            // Add node genes (if not already added).
            // Source node.
            NodeGene srcNodeGene;
            if (!_nodeDictionary.TryGetValue(connectionGene.srcInnovationIndex, out srcNodeGene))
            {
                srcNodeGene = parentGenome._nodeList.GetNodeById(connectionGene.srcInnovationIndex);
                srcNodeGene = new NodeGene(srcNodeGene, false); // Make a copy.
                _nodeDictionary.Add(srcNodeGene.innovationIndex, srcNodeGene);
            }

            // Target node.
            NodeGene tgtNodeGene;
            if (!_nodeDictionary.TryGetValue(connectionGene.destInnovationIndex, out tgtNodeGene))
            {
                tgtNodeGene = parentGenome._nodeList.GetNodeById(connectionGene.destInnovationIndex);
                tgtNodeGene = new NodeGene(tgtNodeGene, false); // Make a copy.
                _nodeDictionary.Add(tgtNodeGene.innovationIndex, tgtNodeGene);
            }

            // Register connectivity with each node.
            srcNodeGene.OutputConnections.Add(connectionGene.innovationIndex);
            tgtNodeGene.InputConnections.Add(connectionGene.innovationIndex);
        }
        else if (overwriteExisting)
        {   // The genome we are building already has a connection with the same node endpoints as the one we are
            // trying to add. It didn't match up during correlation because it has a different innovation number, this
            // is possible because the innovation history buffers throw away old innovations in a FIFO manner in order
            // to prevent them from bloating.

            // Here the 'overwriteExisting' flag is set so the gene we are currently trying to add is probably from the
            // fitter parent, and therefore we want to use its connection weight in place of the existing gene's weight.
            existingConnectionGene.weight = connectionGene.weight;
        }
    }

    /// <summary>
    /// Tests if adding the specified connection would cause a cyclic pathway in the network connectivity.
    /// Returns true if the connection would form a cycle.
    /// Note. This same logic is implemented on NeatGenome.IsConnectionCyclic() but against slightly 
    /// different data structures, hence the method is re-implemented here.
    /// </summary>
    public bool IsConnectionCyclic(int srcNodeId, int tgtNodeId)
    {
        // Quick test. Is connection connecting a node to itself.
        if (srcNodeId == tgtNodeId)
        {
            return true;
        }

        // Quick test. If one of the node's is not yet registered with the builder then there can be no cyclic connection
        // (the connection is coming-from or going-to a dead end).
        NodeGene srcNode;
        if (!_nodeDictionary.TryGetValue(srcNodeId, out srcNode) || !_nodeDictionary.ContainsKey(tgtNodeId))
        {
            return false;
        }

        // Trace backwards through sourceNode's source Nodes. If targetNode is encountered then it feeds
        // signals into sourceNode already and therefore a new connection between sourceNode and targetNode
        // would create a cycle.

        // Maintain a set of Nodes that have been visited. This allows us to avoid unnecessary re-traversal 
        // of the network and detection of cyclic connections.
        HashSet<int> visitedNodes = new HashSet<int>();
        visitedNodes.Add(srcNodeId);

        // Search uses an explicitly created stack instead of function recursion, the logic here is that this 
        // may be more efficient through avoidance of multiple function calls (but not sure).
        Stack<int> workStack = new Stack<int>();

        // Push source Node's sources onto the work stack. We could just push the source Node but we choose
        // to cover that test above to avoid the one extra NodeID lookup that would require.
        foreach (int ConnId in srcNode.InputConnections)
        {
            workStack.Push(_connectionGeneList.GetConnectionById(ConnId).srcInnovationIndex);
        }

        // While there are Nodes to check/traverse.
        while (0 != workStack.Count)
        {
            // Pop a Node to check from the top of the stack, and then check it.
            int currNodeId = workStack.Pop();
            if (visitedNodes.Contains(currNodeId))
            {
                // Already visited (via a different route).
                continue;
            }

            if (currNodeId == tgtNodeId)
            {
                // Target Node already feeds into the source Node.
                return true;
            }

            // Register visit of this node.
            visitedNodes.Add(currNodeId);

            // Push the current Node's source Nodes onto the work stack.
            NodeGene currNode = _nodeDictionary[currNodeId];
            foreach (int ConnId in currNode.InputConnections)
            {
                workStack.Push(_connectionGeneList.GetConnectionById(ConnId).srcInnovationIndex);
            }
        }

        // Connection not cyclic.
        return false;
    }

    #endregion
}