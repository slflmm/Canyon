using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour {

	public Vector2 position;
	private Vector2 velocity;
	private Vector2 acceleration;

	private Vector2 lastPosition;

	private bool isMoving;
	private WorldScript world;

	public float r;

	private float last_fixed_time;
	public bool timed = false;


	// Use this for initialization
	void Start () {
		timed = false;
		world = GameObject.Find ("Background").GetComponent<WorldScript>();
		acceleration = new Vector2 (0, -world.gravity);

		Bounds b = GetComponent<SpriteRenderer>().sprite.bounds;
		r = Mathf.Max (b.extents.x, b.extents.y) * transform.lossyScale.x;
	}
	
	// Update is called once per frame
	void Update () {
		// update whether you're moving
		updateMoving ();

		// if offscreen, cease to exist!
		if (isOffScreen ()) {
			Object.Destroy (this.gameObject);
		}

		// destroy a projectile that has not moved in 2 seconds
		if (!isMoving && !timed) {
			last_fixed_time = Time.time;
			timed = true;
		}
		if (timed) {
			if (Time.time - last_fixed_time > 2) {
				if (!isMoving) {
					Object.Destroy(this.gameObject);
				}
				timed = false;
			}
		}

		// otherwise, update position, test for collision, and resolve collision
		lastPosition = position;
		// calculate new position
		position = position + velocity * Time.deltaTime + world.wind;
		// calculate new velocity
		Vector2 wind_resistance = -world.windRes * velocity;
		velocity = velocity + acceleration*Time.deltaTime + wind_resistance;

		// apply changes
		transform.position = new Vector3(position.x, position.y, transform.position.z);

		// test for and handle collisions
		handleCollisions();
	}

	/**
	 * Detects collisions and updates position + velocity accordingly.
	 * */
	public void handleCollisions() {
		// get a hold of the height points (w/ correct coordinates)
		TerrainGenerator t = GameObject.Find ("Canyon").GetComponent<TerrainGenerator> ();
		Vector2[] points = t.linePoints;
		// iterate over every line segment (make faster with binary search?)
		Vector2 normal = new Vector2 ();
		bool collided = false;
		for (int i = 0; i < points.Length-1; i++) {
			Vector2 p1 = points[i];
			Vector2 p2 = points[i+1];

			// check if enclosing circle intersects line segment
			Vector2 closest = closestOnLine(p1, p2);
			Vector2 dist_v = position - closest;
			if (dist_v.magnitude <= r){

				// if you collided somewhere flat (bottom or top of canyon) delete yourself
				if (t.inFlatLand( i))
					Object.Destroy(this.gameObject);

				collided = true;
				// move projectile so it doesn't collide with the line segment
				Vector3 offset = dist_v.normalized * (r - dist_v.magnitude);

				transform.position += offset;
				position += new Vector2(offset.x, offset.y);
				// update the collision normal
				normal.x = -(p2.y-p1.y);
				normal.y = p2.x - p1.x;
			} 
		}
		// if you had a collision, reflect velocity around the collision normal
		// apply coefficient of restitution
		if (collided) 
			velocity = 0.5f*Vector3.Reflect (velocity, normal.normalized);
	}

	/**
	 * A method used by the cannon to shoot a new projectile.
	 * */
	public void shoot (Vector2 initialPosition, Vector2 initialVelocity) {
		isMoving = true;
		velocity = initialVelocity;
		position = initialPosition;
	}

	/**
	 * Calculate the closest point on the line (p1, p2) to the circle.
	 * Uses method from http://doswa.com/2009/07/13/circle-segment-intersectioncollision.html
	 * */
	private Vector2 closestOnLine(Vector2 p1, Vector2 p2) {

		Vector2 seg_v = p2-p1;
		Vector2 pt_v = position - p1;
		seg_v.Normalize ();

		float proj_v_length = Vector2.Dot (pt_v, seg_v.normalized);
		Vector2 closest = new Vector2 ();
		if (proj_v_length <= 0)
			closest = p1;
		else if (proj_v_length >= seg_v.magnitude)
			closest = p2;
		else {
			Vector2 proj_v = proj_v_length * seg_v.normalized;
			closest = p1 + proj_v;
		}
		return closest;

	}

	private void updateMoving() {
		isMoving = Mathf.Pow(position.x - lastPosition.x,2) + Mathf.Pow(position.y - lastPosition.y, 2) > 0.0001f;
	}

	private bool isOffScreen() {
		return !GetComponent<Renderer>().isVisible;
	}

}
