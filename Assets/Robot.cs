using UnityEngine;
using System.Collections.Generic;
using System;
using SharpNeat.Phenomes;

public class Robot : IComparable<Robot>
{
	GameObject _gameObject;
	GameObject _robot;
	NeuralNetwork _network;

	Transform[] _originalTransforms;
	Renderer[] _allRenderers;

	private RobotParts _robotParts;

	private double _fitness;
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

	public Robot(GameObject gameObject, NeuralNetwork network)
	{
        _goName = gameObject.name;
        init (gameObject);

		//helper class for retrieving inputs for the neural network
		_robotParts = new RobotParts (_robot);

		_network = (NeuralNetwork)network.Clone ();
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
		_fitness = 0;
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

	public NeuralNetwork getBrain ()
	{
		return _network;
	}


	public void Think ()
	{
		double[] inputs = _robotParts.getRobotPartsInputs ();

		_outputs = _network.Compute (inputs);
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
		//_fitness += 0.1f * pos.y;
		foreach (Renderer r in _allRenderers) {
			r.material.color = new Color(0.5f, Math.Max(0.5f, _recordHeight * 0.2f), 0.5f);
		}
		//_fitness -= Math.Abs (pos.x);
		//_fitness -= Math.Abs (rot*0.01);
		//_fitness = _robotParts.getPosition ().x * 10.0;

	}

	public void FinalizeScore ()
	{
		Vector3 pos = _robotParts.getPosition ();
		_fitness += _recordHeight * 200.0;
		//_fitness += pos.y * 100.0;
		//_fitness += pos.x * 10.0;
	}


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

