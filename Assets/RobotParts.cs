using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

public class RobotParts {

	private GameObject _gameObject;
	private Transform _importantTransformObject;

	private List<Transform> _allTransforms;
	private List<HingeJoint2D> _allHingeJoints;
	private List<Trigger> _allTriggers;
	private List<ColliderHelper> _allColliderHelpers;
	private List<Rigidbody2D> _allRigidBodies;
	private List<Thruster> _allThrusters;

	public RobotParts(GameObject gameObject)
	{
		_gameObject = gameObject;

		init ();
	}

	public void setGameObject(GameObject gameObject)
	{
		_gameObject = gameObject;

		init ();
	}

	private void init()
	{
		_allTransforms = _gameObject.GetComponentsInChildren<Transform> ().ToList();

		//only list where multiple can be attached to the same game object
		//sorting by anchor point then
		_allHingeJoints = _gameObject.GetComponentsInChildren<HingeJoint2D> ().ToList();

		_allTriggers = _gameObject.GetComponentsInChildren<Trigger> ().ToList();
		_allColliderHelpers = _gameObject.GetComponentsInChildren<ColliderHelper> ().ToList ();
		_allRigidBodies = _gameObject.GetComponentsInChildren<Rigidbody2D> ().ToList ();
		_allThrusters = _gameObject.GetComponentsInChildren<Thruster> ().ToList ();

		_allTransforms.Sort (SortTransforms);
		_allHingeJoints.Sort (SortHinges);
		_allTriggers.Sort (SortTriggers);
		_allColliderHelpers.Sort (SortColliderHelpers);
		_allRigidBodies.Sort (SortRigidBodies);
		_allThrusters.Sort (SortThrusters);
	}

	private static int SortTransforms(Transform t1, Transform t2)
	{
		return t1.gameObject.name.CompareTo (t2.gameObject.name);
	}

	private static int SortThrusters(Thruster t1, Thruster t2)
	{
		return t1.gameObject.name.CompareTo (t2.gameObject.name);
	}

	private static int SortHinges(HingeJoint2D t1, HingeJoint2D t2)
	{
		Vector2 temp = t1.anchor - t2.anchor;

		//same hinge most likely,
		//could be different but i'm not goona allow that in the editor
		if(temp == Vector2.zero)
		{
			return 0;
		}

		if(temp.x > 0)
		{
			return -1;
		} 
		else if (temp.x == 0 && temp.y > 0) 
		{
			return -1;
		}
		else
		{
			return 1;
		}
	}

	private static int SortTriggers(Trigger t1, Trigger t2)
	{
		return t1.gameObject.name.CompareTo (t2.gameObject.name);
	}

	private static int SortColliderHelpers(ColliderHelper t1, ColliderHelper t2)
	{
		return t1.gameObject.name.CompareTo (t2.gameObject.name);
	}

	private static int SortRigidBodies(Rigidbody2D t1, Rigidbody2D t2)
	{
		return t1.gameObject.name.CompareTo (t2.gameObject.name);
	}


	//don't need to worry about getting outputs matched up
	//because that's what the genetic algorithm is trying
	//to figure out for us!
	public void setParts (double[] _outputs)
	{
		int i = 0;
		i += setSpeed (i, _outputs);
		i += setThrust (i, _outputs);
	}

	public int setSpeed(int x, double[] speeds)
	{
		double scalar = 250.0;
		int index = 0;
		for(int i = 0; i < _allHingeJoints.Count; i++)
		{
			JointMotor2D motor = _allHingeJoints[i].motor;
			motor.motorSpeed = (float)speeds[x+i] * (float)scalar;
			_allHingeJoints[i].motor = motor;
			index++;
		}
		return index;
	}

	public int setThrust(int x, double[] forces)
	{
		double force = 15.0;
		int index = 0;
		for(int i = 0; i < _allThrusters.Count; i++)
		{
			//basic thrust toggle
			if(forces[i+x]>0)
			{
				_allThrusters[i].addThrust(forces[i + x] * force);
			}
			else
			{
				_allThrusters[i].addThrust(1);
			}
			index++;
		}
		return index;
	}

	public float getAveragePosition()
	{
		if (_importantTransformObject != null) 
		{
			return _importantTransformObject.position.x;
		}

		foreach (Transform child in _allTransforms)
		{
			if(child.gameObject.tag == "RobotChest")
			{
				_importantTransformObject = child;
				return child.position.x;
			}
		}

		return 0;
	}

	public double[] getRobotPartsInputs()
	{
		double[] array1 = getTriggers();
		double[] array2 = getColliders ();
		double[] array3 = getJointAngles();
		double[] array4 = getMotorSpeed();
		double[] array5 = getPositions ();
		double[] array6 = getRotations ();
		double[] array7 = getVelocities();
		double[] array8 = getAngularVelocities();
		double[] array9 = getThrusterForce ();
		double[] array10 = getCenterOfMass ();

		//ugly, don't care
		return array1.Concat (array2).Concat (array3).Concat (array4).Concat (array5)
			.Concat (array6).Concat (array7).Concat (array8).Concat (array9).Concat (array10).ToArray ();
		                                                                                                                                             
	}

	public double[] getRobotPartsOutputs ()
	{
		double[] array1 = getMotorSpeed();
		double[] array2 = getThrusterForce ();

		return array1.Concat(array2).ToArray();
	}

	public double[] getCenterOfMass()
	{
		double[] com = new double[2 * _allRigidBodies.Count];
		for(int i = 0; i < _allRigidBodies.Count; i++) 
		{
			com[i*2] = _allRigidBodies[i].centerOfMass.x;
			com[i*2+1] = _allRigidBodies[i].centerOfMass.y;
		}
		
		return com;
	}

	public double[] getThrusterForce ()
	{
		double[] forces = new double[_allThrusters.Count];
		for(int i = 0; i < _allThrusters.Count; i++) 
		{
			forces[i] = _allThrusters[i].getThrust();
		}
		
		return forces;
	}

	public Vector3 getPosition()
	{
		if (_importantTransformObject != null) 
		{
			return _importantTransformObject.position;
		}

		foreach (Transform child in _allTransforms)
		{
			if(child.gameObject.tag == "RobotChest")
			{
				_importantTransformObject = child;
				return child.position;
			}
		}
		
		return Vector3.zero;
	}

	public double getRotation()
	{
		if (_importantTransformObject != null) 
		{
			return _importantTransformObject.eulerAngles.z;
		}
		
		foreach (Transform child in _allTransforms)
		{
			if(child.gameObject.tag == "RobotChest")
			{
				_importantTransformObject = child;
				return child.eulerAngles.z;
			}
		}
		
		return 0;
	}

	public double[] getRotations()
	{
		double[] rotations = new double[_allTransforms.Count];
		for(int i = 0; i < _allTransforms.Count; i++) 
		{
			rotations[i] = _allTransforms[i].eulerAngles.z;
		}
		
		return rotations;
	}

	public double[] getPositions ()
	{
		double[] positions = new double[2 * _allTransforms.Count];
		for(int i = 0; i < _allTransforms.Count; i++) 
		{
			positions[i*2] = _allTransforms[i].position.x;
			positions[i*2+1] = _allTransforms[i].position.y;
		}

		return positions;
	}

	public double[] getVelocities()
	{
		double[] velocities = new double[2 * _allRigidBodies.Count];
		for(int i = 0; i < _allRigidBodies.Count; i++) 
		{
			velocities[i*2] = _allRigidBodies[i].velocity.x;
			velocities[i*2+1] = _allRigidBodies[i].velocity.y;
		}
		
		return velocities;
	}

	public double[] getAngularVelocities()
	{
		double[] velocities = new double[_allRigidBodies.Count];
		for(int i = 0; i < _allRigidBodies.Count; i++) 
		{
			velocities[i] = _allRigidBodies[i].angularVelocity;
		}
		
		return velocities;
	}

	public double[] getTriggers()
	{
		double[] intersections = new double[_allTriggers.Count];
		
		for(int i = 0; i < _allTriggers.Count; i++)
		{
			intersections[i] = Convert.ToDouble(_allTriggers[i].isIntersecting);
		}
		
		return intersections;
	}

	double[] getColliders ()
	{
		double[] contacts = new double[6 * _allColliderHelpers.Count];
		
		for(int i = 0; i < _allColliderHelpers.Count; i++)
		{
			contacts[i*6] = _allColliderHelpers[i].ContactPoint.x;
			contacts[i*6+1] = _allColliderHelpers[i].ContactPoint.y;
			contacts[i*6+2] = _allColliderHelpers[i].ContactNormal.x;
			contacts[i*6+3] = _allColliderHelpers[i].ContactNormal.y;
			contacts[i*6+4] = _allColliderHelpers[i].RelativeVelocity.x;
			contacts[i*6+5] = _allColliderHelpers[i].RelativeVelocity.y;
		}
		
		return contacts;
	}

	public double[] getJointAngles()
	{
		double[] angles = new double[_allHingeJoints.Count];
		
		for(int i = 0; i < _allHingeJoints.Count; i++)
		{
			angles[i] = _allHingeJoints[i].jointAngle;
		}
		
		return angles;
	}

	public double[] getMotorSpeed()
	{
		double[] speeds = new double[_allHingeJoints.Count];
		
		for(int i = 0; i < _allHingeJoints.Count; i++)
		{
			speeds[i] = _allHingeJoints[i].motor.motorSpeed;
		}
		
		return speeds;
	}
}
