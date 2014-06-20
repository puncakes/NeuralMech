using UnityEngine;
using System.Collections;
using System;

public class Thruster : MonoBehaviour {

	private ParticleSystem _particleSystem;
	private float _lastThrust = 0;
	// Use this for initialization
	void Start () {
		_particleSystem = this.gameObject.GetComponentInChildren<ParticleSystem> ();
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void addThrust(double t)
	{
		float thrust = (float)Math.Abs(t);
		this.rigidbody2D.AddForce (this.transform.up * (float)thrust);
		_particleSystem.startSpeed = thrust*0.16f;
		_lastThrust = thrust;
	}

	public double getThrust()
	{
		return (double)_lastThrust;
	}
}
