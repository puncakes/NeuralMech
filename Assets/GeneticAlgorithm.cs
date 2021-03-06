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
	private NeuralNetwork _network;
	
	
	//the population
	public List<Robot> Robots { get; set; }

	public GeneticAlgorithm(GameObject gameObject, int popSize, double mutationChance, double percentToMate, double matingPopPercent, bool elitism)
	{
		_gameObject = gameObject;

		setupNetworkToClone ();

		Crossover = new Splice (_network.CalculateSize () / 3);
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
			Robot r = new Robot(gameObject, _network);
			r.getBrain().Randomize();
			Robots.Add(r);
		}
	}

	void setupNetworkToClone ()
	{
		_network = new NeuralNetwork();

		//constructer for initializing the robot parts
		//without instantiating the unity object
		Robot r = new Robot (_gameObject);
		int numInputs = r.getRobotParts().getRobotPartsInputs().Length;
		int numOutputs = r.getRobotParts ().getRobotPartsOutputs ().Length;
		r.Destroy ();
		
		//setup inputs and hidden layers
		_network.addLayer(new ActivationTANH(), numInputs, 0);
		//_network.addLayer(new ActivationTANH(), (int)(numInputs * 1.0), 1);
		//_network.addLayer(new ActivationTANH(), (int)(numInputs * 1.0), 1);
		//_network.addLayer(new ActivationTANH(), (int)(numInputs * 0.7), 1);
		_network.addLayer(new ActivationTANH(), numOutputs, 0);
		_network.finalize();
	}

	public void nextGeneration()
	{            
		Loom Loom = Loom.Current;
		//sort after receving final scores from fitness function
		//fitness function is called by an outsides class
		Robots.Sort();

		Debug.Log ("Best Robot: " + Robots [0].Fitness);
		
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
			
			Crossover.Mate(mother, father, child1, child2);
			
			if (ThreadSafeRandom.NextDouble() < _mutationChance)
			{
				Mutate.PerformMutation(child1.getBrain());
			}
			
			if (ThreadSafeRandom.NextDouble() < _mutationChance)
			{
				Mutate.PerformMutation(child2.getBrain());
			}

            children.Add(child1);
            children.Add(child2);

			offspringIndex += 2;
		}

        //need to cull robots that are not children to make more room in the gene pool
        int numCulled = 0;
		foreach(Robot r in Robots) 
		{
            bool found = false;
            if(r == Robots[0] && _elitism)
            {
                //Debug.Log("Skipping cull of elite brain: " + r.Fitness);
                found = true;
            } else {
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
            if(!found)
            {
                //make new brain
                r.getBrain().Randomize();
                numCulled++;
            }			
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

		//failed first attempt at multi-threading
		/*if (procCount > 0) 
		{
			for(int i = 0; i < procCount; i++)
			{
				Loom.RunAsync(()=>{
					int lowerLimit = i * countToMate / procCount;
					int upperLimit = lowerLimit + countToMate / procCount;
					for(int x = lowerLimit; x < upperLimit; x++)
					{
						Robot mother = Robots[x];
						int fatherindex = (int)(ThreadSafeRandom.NextDouble() * matingPopulationSize);
						Robot father = Robots[fatherindex];
						Robot child1 = Robots[offspringIndex];
						Robot child2 = Robots[offspringIndex + 1];

						Crossover.Mate(mother, father, child1, child2);

						if (ThreadSafeRandom.NextDouble() < _mutationChance)
						{
							Mutate.PerformMutation(
								ref child1.getBrain().Weights);
						}
						
						if (ThreadSafeRandom.NextDouble() < _mutationChance)
						{
							Mutate.PerformMutation(
								ref child2.getBrain().Weights);
						}

						offspringIndex += 2;
					}
				});

				Loom.QueueOnMainThread(()=>{
					int lowerLimit = (i * (((_populationSize - numOfOffspring)-countToMate) / procCount)) + countToMate;
					int upperLimit = lowerLimit + (((_populationSize - numOfOffspring)-countToMate) / procCount);

					Robot ng = new Robot(_gameObject, Robots[i].getBrain());
					Robots[i] = ng;
				});
			}
		}*/
		
		//Robots.Sort();
	}
}

