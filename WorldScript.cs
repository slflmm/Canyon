using UnityEngine;
using System.Collections;

public class WorldScript : MonoBehaviour {

	/*
	 * PARAMETERS WHICH FULFILL ASSIGNMENT REQUIREMENTS:
	 * 
	 * Max initial force (maxVel): 100
	 * Min initial force (minVel): 25
	 * [Default initial force: 65]
	 * 
	 * Wind range: [-0.4, 0.4]
	 * 
	 * Wind resistance is set to 0.005 of velocity.
	 * Cannon angle varies from [15, 60] degrees.
	 * */

	public float gravity;

	public float maxWind;

	public float maxVel;
	public float minVel;

	public float windRes;

	public float minAngle;
	public float maxAngle;

	
	public Vector2 wind;

	private Transform arrow;
	private Vector3 initArrowScale;

	// Use this for initialization
	void Start () {
		arrow = transform.Find ("WindArrow");
		initArrowScale = arrow.transform.localScale;
		InvokeRepeating ("changeWind", 0, 0.5f);
	}

	void changeWind () {
		wind.x = Random.Range (-maxWind, maxWind);
	}
	
	// Update is called once per frame
	void Update () {
		arrow.transform.localScale = new Vector3(initArrowScale.x*wind.x*8, initArrowScale.y, initArrowScale.z);
	}
}
