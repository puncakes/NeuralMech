using UnityEngine;
using System.Collections.Generic;

public class main : MonoBehaviour {

	//DESCRIPTION: Script for evolving a population of robots to do a certain task

	//Robot prefab to be used in the population
	public GameObject _robotPrefab = null;
	public int _popSize = 1;
	public double _mutationChance = 0.05;
	public double _percentBreeding = .2;
	public double _breedingPoolPercentage = .4;
    public bool elitism = false;

	float generationTimeTrained = 0;
	public float maxGenerationTrainTime = 20.0f;

	GeneticAlgorithm _ga;

	// Use this for initialization
	void Start () {
		setupBrain ();
	}

	void setupBrain() {
		_ga = new GeneticAlgorithm (_robotPrefab, _popSize, _mutationChance, _percentBreeding, _breedingPoolPercentage, elitism);
	}

	int generation = 0;
	// Update is called once per frame

	void Update () {
		List<Robot> robots = _ga.Robots;
		if (generationTimeTrained < maxGenerationTrainTime) 
		{
            for (int i = 0; i < robots.Count; i++)
            {
                robots[i].GetInputsForNetwork();
            }

            Parallel.For(robots.Count, new System.Action<int>(ComputeNetworks));

            for (int i = 0; i < robots.Count; i++)
            {
                robots[i].Act();
                robots[i].UpdateScores();
            }

            generationTimeTrained += Time.deltaTime;
		} else {
			generationTimeTrained = 0;
			for(int i = 0; i < robots.Count; i++)
			{
				robots[i].FinalizeScore();
			}
			_ga.nextGeneration();
		}
	}

    private void ComputeNetworks(int i)
    {
        List<Robot> robots = _ga.Robots;
        robots[i].Think();        
    }
}
