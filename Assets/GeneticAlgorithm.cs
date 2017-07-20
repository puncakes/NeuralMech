using UnityEngine;
using System.Collections.Generic;
using System;

class GeneticAlgorithm
{
	public int _populationSize;
	public double _mutationChance;
	
	//the top _percentToMate of the population will mate with
	//those from the top _matingPopulationPercent
	public double _percentToMate;
	public double _matingPopulationPercent;

    public bool _elitism;

	public ICrossover Crossover { get; set; }
	public IMutate Mutate { get; set; }

	private GameObject _gameObject;
	
	
	//the population
	public List<Robot> Robots { get; set; }

	public GeneticAlgorithm(GameObject gameObject, int popSize, double mutationChance, double percentToMate, double matingPopPercent, bool elitism)
	{
		_gameObject = gameObject;

        //constructer for initializing the robot parts
        //without instantiating the unity object
        Robot r = new Robot(_gameObject);
        int numInputs = r.getRobotParts().getRobotPartsInputs().Length;
        int numOutputs = r.getRobotParts().getRobotPartsOutputs().Length;

        NEATGenome seedNetwork = new NEATGenome(numInputs, numOutputs);

        Mutate = new DefaultMutation (4.0);

		_populationSize = popSize;
		_mutationChance = mutationChance;
		_percentToMate = percentToMate;
		_matingPopulationPercent = matingPopPercent;
        _elitism = elitism;

		Robots = new List<Robot> ();
		for (int i = 0; i < _populationSize; i++) {
			//create robot, objects passed to the constructor will be cloned
			//and randomized for initial use
			r = new Robot(gameObject, seedNetwork);
			r._network.Randomize();
			Robots.Add(r);
		}
	}

	public void nextGeneration()
	{            
		//sort after receving final scores from fitness function
		//fitness function is called by an outsides class
		Robots.Sort();

        Debug.Log("Best Robot: " + Robots[0].Fitness +
            "\n\tHidden Nodes: " + (Robots[0]._network._nodeList.Count - (Robots[0]._network.InputCount + Robots[0]._network.OutputCount + 1)) +
            "\n\tConnections: " + Robots[0]._network._connectionList.Count);
		
		int countToMate = (int)(_populationSize * _percentToMate);
        int matingPopulationSize = (int)(_populationSize * _matingPopulationPercent);
        int numOfOffspring = countToMate * 2;
		int offspringIndex = matingPopulationSize;
		


		//split the mating into x / logical processor count threads
		//or none if there is only 1

		int procCount = Environment.ProcessorCount;

        List<Robot> children = new List<Robot>();

		for(int x = 0; x < countToMate; x++)
		{
			Robot mother = Robots[x];
			//always keep best brain
			int fatherindex = (int)((ThreadSafeRandom.NextDouble() * matingPopulationSize));
			Robot father = Robots[fatherindex];
			Robot child1 = Robots[offspringIndex];
			Robot child2 = Robots[offspringIndex + 1];

            child1._network = mother._network.CreateOffspring(father._network);
            child2._network = mother._network.CreateOffspring(father._network);

            if (ThreadSafeRandom.NextDouble() < _mutationChance)
			{
                child1._network.Mutate();
			}
			
			if (ThreadSafeRandom.NextDouble() < _mutationChance)
			{
                child2._network.Mutate();
            }

            children.Add(child1);
            children.Add(child2);

			offspringIndex += 2;
		}

        //Debug.Log("Num Culled: " + numCulled);

        //reset all robots to original start positions
        Robot.RobotCount = 0; //just for naming purposes
		for (int i = 0; i < Robots.Count; i++) 
		{
			Robots[i].Reset();
			/*if(i < countToMate || i > _populationSize - numOfOffspring)
			{
				Robots[i].setVisible(true);
			}
			else
			{
				Robots[i].setVisible(false);
			}*/
		}

        //need to cull robots that are not children to make more room in the gene pool
        int numCulled = 0;
        foreach (Robot r in Robots)
        {
            bool found = false;
            if (r == Robots[0] && _elitism)
            {
                //Debug.Log("Skipping cull of elite brain: " + r.Fitness);
                found = true;
            }
            else
            {
                foreach (Robot child in children)
                {
                    if (r.name == child.name)
                    {
                        found = true;
                        break;
                    }
                }
            }


            //if it is not an offspring, then time to cull
            if (!found)
            {
                //make new brain
                r._network.Randomize();
                r.setVisible(false);
                numCulled++;
            }
            else
            {
                r.setVisible(true);
            }
        }        
    }
}

