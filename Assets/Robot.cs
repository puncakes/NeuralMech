using UnityEngine;
using System.Collections.Generic;
using System;

public class Robot : IComparable<Robot>
{
	GameObject _gameObject;
	GameObject _robot;

    //you have no idea how much work went into this one line
	public NEATGenome _network { get; set; }

	Transform[] _originalTransforms;
	Renderer[] _allRenderers;

	private RobotParts _robotParts;

	private double _curentFitness;
    private double[] _inputs;
    private double[] _outputs;
	private float _recordHeight;
    private String _goName;

    public String name;

    public static int RobotCount = 0;

	public Robot(GameObject gameObject)
	{
        _goName = gameObject.name;
		init (gameObject);
		
		//helper class for retrieving inputs for the neural network
		_robotParts = new RobotParts (_robot);        
	}

	public Robot(GameObject gameObject, NEATGenome network)
	{
        _goName = gameObject.name;
        init (gameObject);

		//helper class for retrieving inputs for the neural network
		_robotParts = new RobotParts (_robot);

		_network = new NEATGenome(network);
    }

	void init (GameObject gameObject)
	{
		_robot = (GameObject)UnityEngine.Object.Instantiate(Resources.Load(_goName, typeof(GameObject)));
        name = "Robot: " + RobotCount;
        RobotCount++;

		_allRenderers = _robot.GetComponentsInChildren<Renderer> ();

		//_originalTransforms = new Transform[_robot.GetComponentsInChildren<Transform> ().Length];
		//_robot.GetComponentsInChildren<Transform>().CopyTo (_originalTransforms, 0);
		int i = 0;
	}

	public void Reset()
	{
        _curentFitness = 0;
		Fitness = 0;
		_recordHeight = 0;
		//Debug.Log ("Resetting...");

		Destroy ();

		init (_robot);//(GameObject)UnityEngine.Object.Instantiate(Resources.Load("Robot 3", typeof(GameObject)));
		_robotParts.setGameObject (_robot);

		/*Transform[] currentTransforms = _robot.GetComponentsInChildren<Transform> ();
		for (int i = 0; i < _originalTransforms.Length; i++)
		{
			currentTransforms[i].position = _originalTransforms[i].position;
			currentTransforms[i].rotation = _originalTransforms[i].rotation;
		}*/

		//Vector3 position = new Vector3 (0, 0, 0);//Random.Range(-5.0F, 5.0F), 0, 0);
		//_robot = (GameObject)UnityEngine.Object.Instantiate(_gameObject, position, Quaternion.identity);
	}

	public void setVisible(bool b)
	{
		foreach(Renderer r in _allRenderers)
		{
			r.enabled = b;
		}
	}

	public void Destroy()
	{
		UnityEngine.Object.Destroy (_robot);
	}

	public RobotParts getRobotParts()
	{
		return _robotParts;
	}

    public void GetInputsForNetwork()
    {
        _inputs = _robotParts.getRobotPartsInputs();
    }

	public void Think ()
	{
		_outputs = _network.Compute (_inputs);
	}

	public void Act ()
	{
		_robotParts.setParts (_outputs);
	}

	//reward for moving left;
	public void UpdateScores ()
	{
		Vector3 pos = _robotParts.getPosition ();
		double rot = _robotParts.getRotation ();
		if (_recordHeight == null) 
		{
			_recordHeight = pos.y;
		} else if(pos.y > _recordHeight) {
			//_fitness = pos.y * 10.0;
			_recordHeight = pos.y;
		}
		//_curentFitness += 0.001f * pos.x;
		foreach (Renderer r in _allRenderers) {
			r.material.color = new Color(0.5f, Math.Max(0.5f, (float)getScore() * 0.02f), 0.5f);
		}
		//_fitness -= Math.Abs (pos.x);
		//_fitness -= Math.Abs (rot*0.01);
		//_fitness = _robotParts.getPosition ().x * 10.0;

	}

    private double getScore()
    {
        double fitness = _curentFitness;
        Vector3 pos = _robotParts.getPosition();
        fitness += _recordHeight * 10.0;
        //_fitness += pos.y * 100.0;
        //fitness += pos.x * 5.0;

        return fitness;
    }

	public void FinalizeScore ()
	{
        Fitness += getScore();
	}

    //prolly dumb but idc
    public double Fitness { get { return _network.Fitness; } set { _network.Fitness = value; } }

    #region IComparable implementation

    public int CompareTo (Robot other)
	{
		// might be null when deserializing
		//if (_ga == null)
		//{
		//	return 0;
		//}
		if (Math.Abs(Fitness - other.Fitness) < 0.0000001)
		{
			return 0;
		}
		if (Fitness > other.Fitness)
		{
			return -1;
		}
		return 1;
	}

    #endregion
}

