using UnityEngine;
using System.Collections;

public class ColliderHelper : MonoBehaviour {

	public Vector2 RelativeVelocity { get; set;}
	public Vector2 ContactPoint { get; set;}
	public Vector2 ContactNormal { get; set;}

	// Use this for initialization
	void Start () {
		ContactPoint = Vector2.zero;
		ContactNormal = Vector2.zero;
		RelativeVelocity = Vector2.zero;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnCollisionStay2D(Collision2D other)
	{
		ContactPoint = other.contacts [0].point;
		ContactNormal = other.contacts [0].normal;
		RelativeVelocity = other.relativeVelocity;
	}

	void OnCollisionExit2D(Collision2D other)
	{
		ContactPoint = Vector2.zero;
		ContactNormal = Vector2.zero;
		RelativeVelocity = Vector2.zero;
	}
}
