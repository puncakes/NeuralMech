using UnityEngine;
using System.Collections;

public class Trigger : MonoBehaviour {

	public bool isIntersecting { get; set;}

	// Use this for initialization
	void Start () {
		isIntersecting = false;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		Debug.Log("Something has entered this zone.");
		isIntersecting = true;
	}

	void OnTriggerExit2D(Collider2D other)
	{
		Debug.Log("Something has exited this zone."); 
		isIntersecting = false;
	}
}
