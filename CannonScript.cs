using UnityEngine;                                                                                                                                                                                                                        using UnityEngine;
using System.Collections;

public class CannonScript : MonoBehaviour {

	public GameObject projectile;
	public int cannonType;

	private float initialR;

	void Start () {
		initialR = transform.rotation.eulerAngles.z;
	}

	// Update is called once per frame
	void Update () {
		// if you are the right-side cannon and space is pressed...
		if (cannonType == 0 && Input.GetKeyDown (KeyCode.Space) || cannonType == 1 && Input.GetKeyDown(KeyCode.Tab)) {
			WorldScript world = GameObject.Find ("Background").GetComponent<WorldScript> ();
			// pick some angle for the cannon and rotate it
			float desiredAngle = initialR + Random.Range (world.minAngle, world.maxAngle);
			float rotateBy = Mathf.Abs( transform.rotation.eulerAngles.z - desiredAngle );
			transform.Rotate (0,0,rotateBy);

			// calculate initial position + velocity vector
			float initialForce = Random.Range (world.minVel, world.maxVel);
			Vector2 initialPos = transform.position + transform.right*4;
			Vector2 initialVel = transform.right * initialForce;
			// NOTE: when we set the y component of initial velocity to 0, and set initialForce to 100, we still reach the other side
			// (as required)

			// instantiate and shoot the projectile
			GameObject newProjectile = (GameObject) Instantiate (projectile, initialPos, Quaternion.identity);
			if (cannonType == 0) {
				newProjectile.GetComponent<BulletScript>().shoot (initialPos, initialVel);
			}
			else {
				RagdollScript s = newProjectile.GetComponent<RagdollScript>();
				s.shoot (initialPos, initialVel);
			}
		}
	}
}
