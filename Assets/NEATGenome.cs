
using System;
using System.Collections.Generic;
using UnityEngine;

public class NEATGenome
{
    //*********SHARED BY ALL GENOMES:********//
    public static int CurrentInnovation = 0;
    
    //history of created connections through all genomes in the population
    //used in mutations to ensure non-unique mutations use past innovation numbers
    //this ensures non-unique innovations do not create extraneous/incorrectly labeled species
    public static Dictionary<ConnectionGeneRecord, int> ConnectionHistory = new Dictionary<ConnectionGeneRecord, int>();

    //history of created nodes through all genomes in the population
    //used in mutations to ensure non-unique mutations use past innovation numbers
    //this ensures non-unique innovations do not create extraneous/incorrectly labeled species
    public static Dictionary<int, NodeGeneRecord> NodeHistory = new Dictionary<int, NodeGeneRecord>();

    //all genomes have one bias node alongside inpt nodes
    public static int BiasCount = 1;



    //*********LOCAL TO INDIVIDUALS:********//
    public NodeGeneList _nodeList;
    NodeGeneList _inputList;
    NodeGeneList _outputList;

    public ConnectionGeneList _connectionList;    

    List<NetworkLayer> _networkLayers;

    public int InputCount { get; set; }
    public int HiddenCount { get; set; }
    public int OutputCount { get; set; }

    public double Fitness { get; set; }

    private int _populationSize;

    //used in speciation
    int _specieIdx;
    private CoordinateVector _position;


    public NEATGenome(int numInputs, int numOutputs)
    {
        InputCount = numInputs;
        OutputCount = numOutputs;
        _nodeList = new NodeGeneList();
        _inputList = new NodeGeneList();
        _outputList = new NodeGeneList();

        _connectionList = new ConnectionGeneList();

        _networkLayers = new List<NetworkLayer>();

        Initialize();
    }

    public NEATGenome(  NodeGeneList nodeGeneList,
                        ConnectionGeneList connectionGeneList,
                        int inputNeuronCount,
                        int outputNeuronCount)
    {
        _nodeList = new NodeGeneList(nodeGeneList);
        _connectionList = new ConnectionGeneList(connectionGeneList);
        InputCount = inputNeuronCount;
        OutputCount = outputNeuronCount;        

        //make sure the conenction list is sorted by innovation id
        _connectionList.SortByInnovationId();
        _nodeList.SortByInnovationId();

        _inputList = new NodeGeneList();
        for (int i = 0; i < InputCount; i++)
        {
            _inputList.Add(_nodeList[i]);
        }

        _outputList = new NodeGeneList();
        for (int i = InputCount+BiasCount; i < InputCount + BiasCount + OutputCount; i++)
        {
            _outputList.Add(_nodeList[i]);
        }

        _networkLayers = new List<NetworkLayer>();

        CreateNetworkLayers();
    }

    public NEATGenome (NEATGenome sourceNetwork)
    {
        _nodeList = new NodeGeneList(sourceNetwork._nodeList);
        _connectionList = new ConnectionGeneList(sourceNetwork._connectionList);
        InputCount = sourceNetwork.InputCount;
        OutputCount = sourceNetwork.OutputCount;

        //make sure the conenction list is sorted by innovation id
        _connectionList.SortByInnovationId();
        _nodeList.SortByInnovationId();

        _inputList = new NodeGeneList();
        for (int i = 0; i < InputCount; i++)
        {
            _inputList.Add(_nodeList[i]);
        }

        _outputList = new NodeGeneList();
        for (int i = InputCount + BiasCount; i < InputCount + BiasCount + OutputCount; i++)
        {
            _outputList.Add(_nodeList[i]);
        }

        _networkLayers = new List<NetworkLayer>();

        CreateNetworkLayers();
    }



    //   Note. Nodes within _nodeList must always be arranged according to the following layout plan.    
    //   Input nodes.
    //   Bias - single node
    //   Output nodes.
    //   Hidden nodes.
    private void Initialize()
    {
        //create all input nodes
        for(int i = 0; i < InputCount; i++)
        {
            NodeGene n = CreateNodeGene(CurrentInnovation++, NodeType.Input);
            _inputList.Add(n);
            _nodeList.Add(n);
        }

        //create one bias node
        NodeGene biasNode = CreateNodeGene(CurrentInnovation++, NodeType.Bias);
        _nodeList.Add(biasNode);

        //create all output nodes
        for (int i = 0; i < OutputCount; i++)
        {
            NodeGene n = CreateNodeGene(CurrentInnovation++, NodeType.Output);
            _outputList.Add(n);
            _nodeList.Add(n);
        }

        //connect all inputs to all outputs to create the base network
        for (int i = 0; i < InputCount; i++)
        {
            for (int j = 0; j < OutputCount; j++)
            {
                ConnectionGene cg = new ConnectionGene();

                //increment the innovation number to keep track of when this gene was created
                cg.innovationIndex = CurrentInnovation++;
                cg.enabled = true;
                cg.weight = (ThreadSafeRandom.NextDouble() * 2.0) - 1.0;

                //innovation indices are based off the location in _nodeList (the main node list)
                //add src and dest references to the connection gene
                cg.srcInnovationIndex = _inputList[i].innovationIndex;
                cg.destInnovationIndex = _outputList[j].innovationIndex;

                //add input/output references to the respective src/dest nodes
                _inputList[i].OutputConnections.Add(cg.innovationIndex);
                _outputList[j].InputConnections.Add(cg.innovationIndex);

                //connections are stored in the genome and in a global shared list for mutation history purposes
                _connectionList.Add(cg);
                ConnectionHistory.Add(new ConnectionGeneRecord(cg.srcInnovationIndex, cg.destInnovationIndex), cg.innovationIndex);
            }
        }

        //make sure the conenction list is sorted by innovation id
        _connectionList.SortByInnovationId();
        _nodeList.SortByInnovationId();

        CreateNetworkLayers();
    }

    //go through all conenctions and randomize the weight values
    //mainly used at the start of the genetic algorithm to seed the population with
    //random genomes without changing innovation number 
    public void Randomize()
    {
        foreach(ConnectionGene cg in _connectionList)
        {
            cg.weight = (ThreadSafeRandom.NextDouble() * 2.0) - 1.0;
        }
    }

    //Should only be called when the network has changed
    private void CreateNetworkLayers()
    {
        //clear all depth values
        for(int i = 0; i < _nodeList.Count; i++)
        {
            _nodeList[i].depth = 0;
        }

        //depth first search starting from input/bias nodes to find the depth of each node in the graph
        //used to divide the graphs into layers to propogate input downwards towards the output nodes
        int depthSize = InputCount + BiasCount;
        for (int i = 0; i < depthSize; i++)
        {
            FindDepth(_nodeList[i], 0);
        }

        //network layers hold all nodes of equivelent depths
        //essentially holding all nodes whose values need to be resolved before the next layer
        _networkLayers.Clear();

        foreach(NodeGene n in _nodeList)
        {
            //the layer this node's depth requires does not exist yet
            if(n.depth > _networkLayers.Count - 1)
            {
                //create layers above the required depth level if needed
                //to allow _networkLayers to be indexed based on node depth
                int layerCount = _networkLayers.Count;
                int layersToMake = n.depth - (layerCount - 1);
                for(int i = 0; i < layersToMake; i++)
                {
                    NetworkLayer nl = new NetworkLayer();
                    nl.layerDepth = layerCount + i;
                    _networkLayers.Add(nl);
                }
            }

            _networkLayers[n.depth].NodesInLayer.Add(n);
        }
    }

    private void FindDepth(NodeGene node, int depth)
    {
        //node was already visited by a path with a greater depth
        //priority goes to greater depth
        if(node.depth >= depth && depth != 0)
        {
            return;
        }

        node.depth = depth;

        //node.OutputNodes stores innovationIndexes
        foreach(int connIndex in node.OutputConnections)
        {
            int newDepth = depth+1;
            try
            {
                int nodeId = _connectionList.GetConnectionById(connIndex).destInnovationIndex;
                FindDepth(_nodeList.GetNodeById(nodeId), newDepth);
            } catch (Exception e)
            {
                Debug.Log("whaa");
            }
        }
    }

    public double[] Compute(double[] input)
    {
        //reset all nodes' activation sums to calculate new decisions
        foreach(NodeGene n in _nodeList)
        {
            n.activationSum = 0;
        }

        for(int i = 0; i < _networkLayers.Count; i++)
        {
            NetworkLayer currentLayer = _networkLayers[i];

            //ensure inputs always lineup with previous nodes
            currentLayer.NodesInLayer.SortByInnovationId();

            for(int j = 0; j < currentLayer.NodesInLayer.Count; j++)
            {
                NodeGene n = currentLayer.NodesInLayer[j];

                //special case on the first layer
                if(i == 0)
                {
                    //only input nodes get seeded with, as you guessed, the inputs
                    //input nodes are guaranteed to be at the start of the first layer
                    //so no need to worry about setting bias/loner nodes in the else
                    if (n.ntype == NodeType.Input)
                    {
                        n.activationSum = input[j];
                    }
                    else
                    {
                        n.activationSum = 1;
                    }
                }
                else
                {
                    //pull from the previously calculated layers' nodes to activate the current node
                    foreach(int conn in n.InputConnections)
                    {
                        ConnectionGene cg = _connectionList.GetConnectionById(conn);
                        n.activationSum += _nodeList.GetNodeById(cg.srcInnovationIndex).activationSum * cg.weight;
                    }
                }

                //call activation function here
                //defaulting to TANH
                n.activationSum = 2.0 / (1.0 + BoundMath.Exp(-2.0 * n.activationSum)) - 1.0;
            }
        }

        double[] output = new double[OutputCount];
        for(int i = 0; i < OutputCount; i++)
        {
            output[i] = _outputList[i].activationSum;
        }

        return output;
    }
    
    public NEATGenome CreateOffspring(NEATGenome parent)
    {
		//overview of what's about to go down
		//create a new genome that is a combination of the two parents. basically an OR operation on both genotypes (node list & connection list)
		//looping through both parent's connections and marking matching/disjoint/excess genes into a master correlation list
		//then that list is looped through to decide what operation is needed for that correlation: 
		//	-inherit a weight from a parent for matching genes
		//	-add a connection to the child genome on disjoint/excess genes (add missing src/dest nodes as well if they are not present in the child genome)

        //genome 1 = this
        //genome 2 = parent

		CorrelationResults correlationResults = CorrelateConnectionGeneLists (_connectionList, parent._connectionList);
		//Debug.Log ("CorrelationResults check: " + (correlationResults.PerformIntegrityCheck () ? "passed" : "failed"));

        ConnectionGeneListBuilder connectionListBuilder = new ConnectionGeneListBuilder(_connectionList.Count + parent._connectionList.Count);

        // Pre-register all of the fixed nodes (bias, inputs and outputs) with the ConnectionGeneListBuilder's
        // node ID dictionary. We do this so that we can use the dictionary later on as a complete list of
        // all node IDs required by the offspring genome - if we didn't do this we might miss some of the fixed nodes
        // that happen to not be connected to or from.
        SortedDictionary<int, NodeGene> nodeDictionary = connectionListBuilder.NodeDictionary;
        for (int i = 0; i < (InputCount+BiasCount+OutputCount); i++)
        {
            nodeDictionary.Add(_nodeList[i].innovationIndex, _nodeList[i].CreateCopy(false));
        }

        //maybe change this to handle potentially different genomes with the same fitness
        bool isFittest = Fitness >= parent.Fitness;

        List<CorrelationItem> disjointExcessGeneList = new List<CorrelationItem>(correlationResults.CorrelationStatistics.DisjointConnectionGeneCount + correlationResults.CorrelationStatistics.ExcessConnectionGeneCount);

        foreach ( CorrelationItem item in correlationResults.CorrelationItemList)
        {
            // Determine which genome to copy from (if any)
            bool selectMe;
            if (CorrelationItemType.Match == item.CorrelationItemType)
            {   // For matches pick a parent genome at random (they both have the same connection gene, 
                // but with a different connection weight)   
                selectMe = ThreadSafeRandom.NextDouble() > 0.5 ? true : false;
            }
            else if (isFittest && item.ConnectionGene1 != null)
            {   // Disjoint/excess gene on the fittest genome (genome #1).
                selectMe = true;
            }
            else if (!isFittest && item.ConnectionGene2 != null)
            {   // Disjoint/excess gene on the fittest genome (genome #2).
                selectMe = false;
            }
            else
            {   
                //disjoint/excess gene in least fit genome
                //store it to add later
                disjointExcessGeneList.Add(item);
                continue;
            }

            // Get ref to the selected connection gene and its source target neuron genes.
            ConnectionGene connectionGene;
            NEATGenome parentGenome;
            if (selectMe)
            {
                connectionGene = item.ConnectionGene1;
                parentGenome = this;
            }
            else
            {
                connectionGene = item.ConnectionGene2;
                parentGenome = parent;
            }

            // Add connection gene to the offspring's genome. For genes from a match we set a flag to force
            // an override of any existing gene with the same innovation ID (which may have come from a previous disjoint/excess gene).
            // We prefer matched genes as they will tend to give better fitness to the offspring - this logic if based purely on the 
            // fact that the gene has clearly been replicated at least once before and survived within at least two genomes.
            connectionListBuilder.TryAddGene(connectionGene, parentGenome, (CorrelationItemType.Match == item.CorrelationItemType));
        }

        // Loop 2: Add disjoint/excess genes from the least fit parent (if any). These may create connectivity cycles, hence we need to test
        // for this when evoloving feedforward-only networks.
        if (disjointExcessGeneList != null && disjointExcessGeneList.Count > 0)
        {
            foreach (CorrelationItem correlItem in disjointExcessGeneList)
            {
                // Get ref to the selected connection gene and its source target neuron genes.
                ConnectionGene connectionGene;
                NEATGenome parentGenome;
                if (correlItem.ConnectionGene1 != null)
                {
                    connectionGene = correlItem.ConnectionGene1;
                    parentGenome = this;
                }
                else
                {
                    connectionGene = correlItem.ConnectionGene2;
                    parentGenome = parent;
                }

                // We are effectively adding connections from one genome to another, as such it is possible to create cyclic conenctions here.
                // Thus only add the connection if we allow cyclic connections *or* the connection does not form a cycle.
                if (!connectionListBuilder.IsConnectionCyclic(connectionGene.srcInnovationIndex, connectionGene.destInnovationIndex))
                {   // Add connection gene to the offspring's genome.
                    connectionListBuilder.TryAddGene(connectionGene, parentGenome, false);
                }
            }
        }

        // Extract the connection builders definitive list of neurons into a list.
        NodeGeneList nodeGeneList = new NodeGeneList(connectionListBuilder.NodeDictionary.Count);
        foreach (NodeGene nodeGene in nodeDictionary.Values)
        {
            nodeGeneList.Add(nodeGene);
        }

        // Note that connectionListBuilder.ConnectionGeneList is already sorted by connection gene innovation ID 
        // because it was generated by passing over the correlation items generated by CorrelateConnectionGeneLists()
        // - which returns correlation items in order.
        return new NEATGenome(nodeGeneList, connectionListBuilder.ConnectionGeneList, InputCount, OutputCount);
    }

	/// <summary>
	/// Correlates the ConnectionGenes from two distinct genomes based upon gene innovation numbers.
	/// </summary>
	private static CorrelationResults CorrelateConnectionGeneLists(ConnectionGeneList list1, ConnectionGeneList list2)
	{
		// If none of the connections match up then the number of correlation items will be the sum of the two
		// connections list counts..
		CorrelationResults correlationResults = new CorrelationResults(list1.Count + list2.Count);

		//----- Test for special cases.
		int list1Count = list1.Count;
		int list2Count = list2.Count;
		if(0 == list1Count && 0 == list2Count)
		{   // Both lists are empty!
			return correlationResults;
		}

		if(0 == list1Count)
		{   // All list2 genes are excess.
			correlationResults.CorrelationStatistics.ExcessConnectionGeneCount = list2Count;
			foreach(ConnectionGene connectionGene in list2) {
				correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Excess, null, connectionGene));
			}
			return correlationResults;
		}

		if(0 == list2Count)
		{   // All list1 genes are excess.
			correlationResults.CorrelationStatistics.ExcessConnectionGeneCount = list1Count;
			foreach(ConnectionGene connectionGene in list1) {
				correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Excess, connectionGene, null));
			}
			return correlationResults;
		}

		//----- Both connection genes lists contain genes - compare their contents.
		int list1Idx=0;
		int list2Idx=0;
		ConnectionGene connectionGene1 = list1[list1Idx];
		ConnectionGene connectionGene2 = list2[list2Idx];

		for(;;)
		{
			if(connectionGene2.innovationIndex < connectionGene1.innovationIndex)
			{   
				// connectionGene2 is disjoint.
				correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Disjoint, null, connectionGene2));
				correlationResults.CorrelationStatistics.DisjointConnectionGeneCount++;

				// Move to the next gene in list2.
				list2Idx++;
			}
			else if(connectionGene1.innovationIndex == connectionGene2.innovationIndex)
			{
				correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Match, connectionGene1, connectionGene2));
				correlationResults.CorrelationStatistics.ConnectionWeightDelta += Math.Abs(connectionGene1.weight - connectionGene2.weight);
				correlationResults.CorrelationStatistics.MatchingGeneCount++;

				// Move to the next gene in both lists.
				list1Idx++;
				list2Idx++;
			}
			else // (connectionGene2.InnovationId > connectionGene1.InnovationId)
			{   
				// connectionGene1 is disjoint.
				correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Disjoint, connectionGene1, null));
				correlationResults.CorrelationStatistics.DisjointConnectionGeneCount++;

				// Move to the next gene in list1.
				list1Idx++;
			}

			// Check if we have reached the end of one (or both) of the lists. If we have reached the end of both then 
			// although we enter the first 'if' block it doesn't matter because the contained loop is not entered if both 
			// lists have been exhausted.
			if(list1Count == list1Idx)
			{   
				// All remaining list2 genes are excess.
				for(; list2Idx < list2Count; list2Idx++)
				{
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Excess, null, list2[list2Idx]));
					correlationResults.CorrelationStatistics.ExcessConnectionGeneCount++;
				}
				return correlationResults;
			}

			if(list2Count == list2Idx)
			{
				// All remaining list1 genes are excess.
				for(; list1Idx < list1Count; list1Idx++)
				{
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.Excess, list1[list1Idx], null));
					correlationResults.CorrelationStatistics.ExcessConnectionGeneCount++;
				}
				return correlationResults;
			}

			connectionGene1 = list1[list1Idx];
			connectionGene2 = list2[list2Idx];
		}
	}

    public void Mutate()
    {
        List<Mutation> potentialMutations = new List<Mutation>();
        foreach(Mutation m in Enum.GetValues(typeof(Mutation)))
        {
            potentialMutations.Add(m);
        }

        bool success = false;

        //most changes will need an update to the network layer structure
        bool structureChange = true;

        //keep trying until one of the mutations succeeds
        //remove a previously failed mutation to prevent multiple failed attempts
        while (!success)
        {
            //choose random mutation to perform on the genome
            Mutation mutation = potentialMutations[(int)(ThreadSafeRandom.NextDouble() * potentialMutations.Count)];

            switch (mutation)
            {
                case Mutation.ModifyWeights:
                    mutateWeights();
                    success = true;
                    structureChange = false;
                    break;
                case Mutation.AddNode:
                    success = mutateAddNode();
                    break;
                case Mutation.AddConnection:
                    success = mutateAddConnection();
                    break;
                case Mutation.RemoveConnection:
                    success = mutateRemoveConnection();
                    break;
            }

            if(!success)
            {
                potentialMutations.Remove(mutation);
            }
        }

        //recalculate what layer each node is in
        if(structureChange)
        {
            CreateNetworkLayers();
        }
    }

    private void mutateWeights()
    {
        Boolean mutated = false;
        for(int i = 0; i < _connectionList.Count; i++)
        {
            if(ThreadSafeRandom.NextDouble() < NEATParams.PerturbChance)
            {
                _connectionList[i].weight += ((ThreadSafeRandom.NextDouble() * 2.0) - 1.0) * NEATParams.PerturbAmount;
                mutated = true;
            }            
        }

        //mutate at least one connection
        if(_connectionList.Count > 0 && !mutated)
        {
            int index = (int)(ThreadSafeRandom.NextDouble() * _connectionList.Count);
            _connectionList[index].weight += ((ThreadSafeRandom.NextDouble() * 2.0) - 1.0) * NEATParams.PerturbAmount;
        }
    }

    //replace a connection with two connections with a new node in the middle
    private bool mutateAddNode()
    {
        if(_connectionList.Count == 0)
        {
            return false;
        }

        //choose conenction at random to replace
        int index = (int)(ThreadSafeRandom.NextDouble() * _connectionList.Count);
        ConnectionGene connectionToReplace = _connectionList[index];
        _connectionList.RemoveAt(index);

        //need to check to see if there has already been a mutation splitting this connection in other genomes

        int nodeInnovationId = -1;
        int srcInnovationId = -1;
        int destInnovationId = -1;

        bool reuseIds = false;
        bool recordHistory = false;
        NodeGeneRecord ngr;
        if(NodeHistory.TryGetValue(connectionToReplace.innovationIndex, out ngr))
        {
            // Found existing matching structure.
            // However we can only re-use the IDs from that structrue if they aren't already present in the current genome;
            // this is possible because genes can be acquired from other genomes via sexual reproduction.
            // Therefore we only re-use IDs if we can re-use all three together, otherwise we aren't assigning the IDs to matching
            // structures throughout the population, which is the reason for ID re-use.
            if (_nodeList.BinarySearch(ngr.AddedNodeId) == -1
                && _connectionList.BinarySearch(ngr.AddedInputConnectionId) == -1
                && _connectionList.BinarySearch(ngr.AddedOutputConnectionId) == -1)
            {
                reuseIds = true;                
            }
        } 
        else
        {
            recordHistory = true;
        }

        if(reuseIds)
        {
            nodeInnovationId = ngr.AddedNodeId;
            srcInnovationId = ngr.AddedInputConnectionId;
            destInnovationId = ngr.AddedOutputConnectionId;
        }
        else
        {
            nodeInnovationId = CurrentInnovation++;
            srcInnovationId = CurrentInnovation++;
            destInnovationId = CurrentInnovation++;
        }

        NodeGene n = CreateNodeGene(nodeInnovationId, NodeType.Hidden);
        ConnectionGene c1 = new ConnectionGene(connectionToReplace.srcInnovationIndex, nodeInnovationId, 1.0, true, srcInnovationId);
        ConnectionGene c2 = new ConnectionGene(nodeInnovationId, connectionToReplace.destInnovationIndex, connectionToReplace.weight, true, destInnovationId);

        if(reuseIds)
        {
            _nodeList.InsertIntoPosition(n);
            _connectionList.InsertIntoPosition(c1);
            _connectionList.InsertIntoPosition(c2);
        }
        else
        {
            _nodeList.Add(n);
            _connectionList.Add(c1);
            _connectionList.Add(c2);
        }

        // Update each affected node to reflect the changefs made to the network
        // Original source node.
        NodeGene srcNodeGene = _nodeList.GetNodeById(connectionToReplace.srcInnovationIndex);
        srcNodeGene.OutputConnections.Remove(connectionToReplace.innovationIndex);
        srcNodeGene.OutputConnections.Add(c1.innovationIndex);

        // Original dest node.
        NodeGene destNodeGene = _nodeList.GetNodeById(connectionToReplace.destInnovationIndex);
        destNodeGene.InputConnections.Remove(connectionToReplace.innovationIndex);
        destNodeGene.InputConnections.Add(c2.innovationIndex);

        // New node.
        n.InputConnections.Add(c1.innovationIndex);
        n.OutputConnections.Add(c2.innovationIndex);

        //if this is a totally new mutation, add it to the history of the population
        //the key is the innovation number of the connection that was split for easy indexing
        //when other genomes encounter the same mutation
        if (recordHistory)
        {
            ngr = new NodeGeneRecord(nodeInnovationId, connectionToReplace.srcInnovationIndex, connectionToReplace.destInnovationIndex);
            NodeHistory.Add(connectionToReplace.innovationIndex, ngr);
        }

        return true;
    }

    private bool mutateAddConnection()
    {
        if(_nodeList.Count < 3)
        {
            return false;
        }

        // TODO: Try to improve chance of finding a candidate connection to make.
        // valid source nodes are anything not an output node
        // valid dest nodes are anything not an input/bias node
        int nodeCount = _nodeList.Count;
        int hiddenOutputNodeCount = nodeCount - (InputCount + BiasCount);
        int inputBiasHiddenNodeCount = nodeCount - OutputCount;

        for (int attempts = 0; attempts < 5; attempts++)
        {
            // Select candidate source and target neurons. 
            // Valid source nodes are bias, input and hidden nodes. Output nodes are not source node candidates
            // for acyclic nets (because that can prevent futrue conenctions from targeting the output if it would
            // create a cycle).
            int srcNodeIdx = (int)(ThreadSafeRandom.NextDouble() * inputBiasHiddenNodeCount);

            // Valid target nodes are all hidden and output nodes.
            int destNodeIdx = InputCount + BiasCount + (int)(ThreadSafeRandom.NextDouble() * (hiddenOutputNodeCount - 1));
            if (srcNodeIdx == destNodeIdx)
            {
                if (++destNodeIdx == nodeCount)
                {
                    // Wrap around to first possible target neuron (first output).
                    // ENHANCEMENT: Devise more efficient strategy. This can still select the same node as source and target (the cyclic conenction is tested for below). 
                    destNodeIdx = InputCount + BiasCount;
                }
            }

            // Test if this connection already exists or is recurrent
            NodeGene srcNode = _nodeList[srcNodeIdx];
            NodeGene destNode = _nodeList[destNodeIdx];

            //first check global record for presence of this conenction
            ConnectionGeneRecord cgr = new ConnectionGeneRecord(srcNode.innovationIndex, destNode.innovationIndex);
            
            int connIndex;
            if(ConnectionHistory.TryGetValue(cgr, out connIndex))
            {
                //then check if it is found within this genome
                if (srcNode.OutputConnections.Contains(connIndex) || IsConnectionCyclic(srcNode, destNode.innovationIndex))
                {
                    continue;
                }
            }
            

            //create the connection
            return mutateCreateConnection(srcNode, destNode);
        }

        return false;
    }

    private bool mutateCreateConnection(NodeGene srcNode, NodeGene destNode)
    {
        int innovationId;
        ConnectionGeneRecord cgr = new ConnectionGeneRecord(srcNode.innovationIndex, destNode.innovationIndex);
        ConnectionGene cg;
        if (ConnectionHistory.TryGetValue(cgr, out innovationId))
        {
            //a previous connection gene was found in the history of the population
            //use that innovation number
            cg = new ConnectionGene(srcNode.innovationIndex, destNode.innovationIndex, ThreadSafeRandom.NextDouble() * 2.0 - 1.0, true, innovationId);
            _connectionList.InsertIntoPosition(cg);
        }
        else
        {
            int newInnovationId = CurrentInnovation++;
            cg = new ConnectionGene(srcNode.innovationIndex, destNode.innovationIndex, ThreadSafeRandom.NextDouble() * 2.0 - 1.0, true, newInnovationId);
            _connectionList.Add(cg);
            ConnectionHistory.Add(cgr, newInnovationId);
        }

        // Track connections associated with each neuron.
        srcNode.OutputConnections.Add(cg.innovationIndex);
        destNode.InputConnections.Add(cg.innovationIndex);

        return true;
    }

    private bool IsConnectionCyclic(NodeGene srcNode, int innovationIndex)
    {
        // Quick test. Is connection connecting a neuron to itself.
        if (srcNode.innovationIndex == innovationIndex)
        {
            return true;
        }

        // Trace backwards through sourceNeuron's source neurons. If targetNeuron is encountered then it feeds
        // signals into sourceNeuron already and therefore a new connection between sourceNeuron and targetNeuron
        // would create a cycle.

        // Maintain a set of neurons that have been visited. This allows us to avoid unnecessary re-traversal 
        // of the network and detection of cyclic connections.
        HashSet<int> visitedNeurons = new HashSet<int>();
        visitedNeurons.Add(srcNode.innovationIndex);

        // This search uses an explicitly created stack instead of function recursion, the logic here is that this 
        // may be more efficient through avoidance of multiple function calls (but not sure).
        Stack<int> workStack = new Stack<int>();

        // Push source neuron's sources onto the work stack. We could just push the source neuron but we choose
        // to cover that test above to avoid the one extra neuronID lookup that would require.
        foreach (int ConnId in srcNode.InputConnections)
        {
            workStack.Push(_connectionList.GetConnectionById(ConnId).srcInnovationIndex);
        }

        // While there are neurons to check/traverse.
        while (0 != workStack.Count)
        {
            // Pop a neuron to check from the top of the stack, and then check it.
            int currNodeId = workStack.Pop();
            if (visitedNeurons.Contains(currNodeId))
            {
                // Already visited (via a different route).
                continue;
            }

            if (currNodeId == innovationIndex)
            {
                // Target neuron already feeds into the source neuron.
                return true;
            }

            // Register visit of this node.
            visitedNeurons.Add(currNodeId);

            // Push the current neuron's source neurons onto the work stack.
            NodeGene currNode = _nodeList.GetNodeById(currNodeId);
            foreach (int ConnId in currNode.InputConnections)
            {
                workStack.Push(_connectionList.GetConnectionById(ConnId).srcInnovationIndex);
            }
        }
        
        return false;
    }

    private bool mutateRemoveConnection()
    {
        if (_connectionList.Count < 2)
        {   // Either no connections to delete or only one. Indicate failure.
            return false;
        }

        // Select a connection at random.
        int connectionToDeleteIdx = (int)(ThreadSafeRandom.NextDouble() * _connectionList.Count);
        ConnectionGene connectionToDelete = _connectionList[connectionToDeleteIdx];

        // Delete the connection.
        _connectionList.RemoveAt(connectionToDeleteIdx);

        // Track connections associated with each neuron and remove neurons that are no longer connected to anything.
        // Source neuron.
        int srcNodeIdx = _nodeList.BinarySearch(connectionToDelete.srcInnovationIndex);
        NodeGene srcNodeGene = _nodeList[srcNodeIdx];
        srcNodeGene.OutputConnections.Remove(connectionToDelete.innovationIndex);

        if (IsNeuronRedundant(srcNodeGene))
        {
            // Remove neuron.
            _nodeList.RemoveAt(srcNodeIdx);
        }

        // Target neuron.
        int destNodeIdx = _nodeList.BinarySearch(connectionToDelete.destInnovationIndex);
        NodeGene destNodeGene = _nodeList[destNodeIdx];
        destNodeGene.InputConnections.Remove(connectionToDelete.innovationIndex);

        // Note. Check that source and target neurons are not the same neuron.
        if (srcNodeGene != destNodeGene
            && IsNeuronRedundant(destNodeGene))
        {
            // Remove neuron.
            _nodeList.RemoveAt(destNodeIdx);
        }
        
        return true;
    }

    private NodeGene CreateNodeGene(int innovationIndex, NodeType nodeType)
    {
        NodeGene n = new NodeGene();
        n.innovationIndex = innovationIndex;
        n.ntype = nodeType;
        n.activationSum = 0;

        return n;
    }
    
    private bool IsNeuronRedundant(NodeGene nodeGene)
    {
		//prevent input/output/bias neurons from being deleted
		//they can still end up not having connections though!
        if (nodeGene.ntype != NodeType.Hidden)
        {
            return false;
        }
        return (0 == (nodeGene.InputConnections.Count + nodeGene.OutputConnections.Count));
    }

    /// <summary>
    /// Gets or sets a specie index. This is the index of the species that the genome is in.
    /// Implementing this is required only when using evolution algorithms that speciate genomes.
    /// </summary>
    public int SpecieIdx
    {
        get { return _specieIdx; }
        set { _specieIdx = value; }
    }

    /// <summary>
    /// Gets a coordinate that represents the genome's position in the search space (also known
    /// as the genetic encoding space). This allows speciation/clustering algorithms to operate on
    /// an abstract cordinate data type rather than being coded against specific IGenome types.
    /// </summary>
    public CoordinateVector Position
    {
        get
        {
            if (null == _position)
            {
                // Consider each connection gene as a dimension where the innovation ID is the
                // dimension's ID and the weight is the position within that dimension.
                // The coordinate elements in the resulting array must be sorted by innovation/dimension ID,
                // this requirement is met by the connection gene list also requiring to be sorted at all times.
                ConnectionGeneList list = _connectionList;

                int count = list.Count;
                KeyValuePair<int, double>[] coordElemArray = new KeyValuePair<int, double>[count];

                for (int i = 0; i < count; i++)
                {
                    coordElemArray[i] = new KeyValuePair<int, double>(list[i].innovationIndex, list[i].weight);
                }
                _position = new CoordinateVector(coordElemArray);
            }
            return _position;
        }
    }
}