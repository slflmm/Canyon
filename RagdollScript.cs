using UnityEngine;
using System.Collections;

public class RagdollScript : MonoBehaviour {

	// we will keep a list of points, as well as a list of links between them
	private Point[] points;
	private Link[] links;

	private LineRenderer lr;

	private int numPoints;
	private int numLinks;
	private int numVisibleLinks;

	private Point initialPos;
	private Point initialVel;

	private bool isMoving = false;
	private bool timed;
	private float last_fixed_time;

	/**
	 * The Point class is like Vector2, but is not a struct.
	 * Equations for basic Stormer-Verlet, http://en.wikipedia.org/wiki/Verlet_integration 
	 * */
	public class Point {

		public float x;
		public float y;

		public float prev_x;
		public float prev_y;

		public float init_dist;

		public Point(float px, float py) {
			prev_x = px;
			x = px;
			prev_y = py;
			y = py;
		}

		public void setVelocity(Point vel) {
			float gravity = GameObject.Find ("Background").GetComponent<WorldScript> ().gravity;
			prev_x = x;
			prev_y = y;
			x = prev_x + vel.x*Time.deltaTime;
			y = prev_y - 0.5f*gravity*Time.deltaTime*Time.deltaTime + vel.y*Time.deltaTime;
		}

		public void physics_update() {
			WorldScript world = GameObject.Find ("Background").GetComponent<WorldScript> ();
			float gravity = world.gravity;
			float wind = world.wind.x;
			float vel_x = x - prev_x;
			float vel_y = y - prev_y;

			float wind_res_x = -world.windRes * vel_x;
			float wind_res_y = -world.windRes * vel_y;

			float next_x = x + vel_x + wind*Time.deltaTime + wind_res_x;
			float next_y = y + vel_y - gravity*Time.deltaTime*Time.deltaTime + wind_res_y;

			prev_x = x;
			prev_y = y;

			x = next_x;
			y = next_y;
		}

		public Vector3 vectorize() {
			return new Vector3 (x, y, -4);
		}

		public Vector2 vectorize2() {
			return new Vector2 (x, y);
		}

		public static Point operator +(Point p1, Point p2) {
			return new Point (p1.x + p2.x, p1.y + p2.y);
		}
		public static Point operator -(Point p1, Point p2) {
			return new Point (p1.x - p2.x, p1.y - p2.y);
		}
		public static Point operator *(float f, Point p) {
			return new Point (p.x * f, p.y * f);
		}
		public static float prevDistance(Point p1, Point p2) {
			return Mathf.Sqrt (Mathf.Pow (p1.prev_x - p2.prev_x, 2) + Mathf.Pow (p1.prev_y - p2.prev_y, 2));
		}
		public static float distance(Point p1, Point p2) {
			return Mathf.Sqrt (Mathf.Pow (p1.x - p2.x, 2) + Mathf.Pow (p1.y - p2.y, 2));
		}
	}

	/**
	 * The Link class keeps two Points linked together at a specific distance.
	 * */
	public class Link {
		float restingDistance;

		Point p1;
		Point p2;

		public LineRenderer l;
		public GameObject g;

		public Link(Point a, Point b, float dist) {
			p1 = a;
			p2 = b;
			restingDistance = dist;
			l = new LineRenderer();
		}

		// solves a link constraint according to equations from http://gamedevelopment.tutsplus.com/tutorials/simulate-tearable-cloth-and-ragdolls-with-simple-verlet-integration--gamedev-519
		public void solve() {
			float distance = Point.distance(p1, p2);
			float difference = (restingDistance - distance) / distance;
			float translateX = (p1.x - p2.x) * 0.5f * difference;
			float translateY = (p1.y - p2.y) * 0.5f * difference;
			p1.x = p1.x + translateX;
			p1.y = p1.y + translateY;
			p2.x = p2.x - translateX;
			p2.y = p2.y - translateY;
		}

		public void handleCollisions() {

			// get a hold of the height points (w/ correct coordinates)
			TerrainGenerator t = GameObject.Find ("Canyon").GetComponent<TerrainGenerator> ();
			Vector2[] terrain_segments = t.linePoints;

			// look for collisions with the terrain
			for (int i = 0; i < terrain_segments.Length-1; i++) {
				Vector2 terrain1 = terrain_segments[i];
				Vector2 terrain2 = terrain_segments[i+1];

				Point intersection = getIntersection (terrain1, terrain2);
				if (intersection != null) {

					if (t.inFlatLand( i))
						Object.Destroy(g);
					else {
						// for both points, get the vector from point to intersection
						Vector2 v1 = new Vector2(intersection.x, intersection.y) - new Vector2(p1.x, p1.y);
						Vector2 v2 = new Vector2(intersection.x, intersection.y) - new Vector2(p2.x, p2.y);
						// check the dot product of that vector with line normal (terrain1 -- terrain2)
						Vector2 normal = new Vector2();
						normal.x = (terrain2.y-terrain1.y);
						normal.y = -(terrain2.x - terrain1.x);
						// use the fact that a negative dot product means > 180 degree diff in orientation
						// if the dot product is negative, translate the point to the intersection
						if (Vector2.Dot (v1, normal) < 0) {
							p1.prev_x = p1.x;
							p1.prev_y = p1.y;
							Vector2 nproj = (Vector2.Dot (v1, normal)/Vector2.Dot (normal,normal)) * normal;
							p1.x = p1.x + nproj.x;
							p1.y = p1.y + nproj.y;
						}
						else  { // we are interested in the other point
							p2.prev_x = p2.x;
							p2.prev_y = p2.y;
							Vector2 nproj = (Vector2.Dot (v2, normal)/Vector2.Dot (normal,normal)) * normal;
							p2.x = p2.x + nproj.x;
							p2.y = p2.y + nproj.y;
						}
					}
				}
			}

			// now look for collisions with balls
			GameObject[] balls = GameObject.FindGameObjectsWithTag ("Ball");
			for (int i = 0; i < balls.Length; i++) {
				BulletScript ball = balls[i].GetComponent<BulletScript>();
//				Debug.Log (ball.position);
//				Debug.Log (ball.r);
				Vector2 closest = closestOnLine(ball.r, ball.position);
				Vector2 dist_v = ball.position - closest;
				// if we do end up having a collision...
				if (dist_v.magnitude < ball.r) {
					Vector2 offset = dist_v.normalized * (ball.r - dist_v.magnitude);
					// update the points' positions by offset
					p1.x = p1.x - offset.x;
					p2.x = p2.x - offset.x;
					p1.y = p1.y - offset.y;
					p2.y = p2.y - offset.y;
				}
			}

		}

		/**
		 * Find the point of intersection between two lines.
		 * Algorithm from http://community.topcoder.com/tc?module=Static&d1=tutorials&d2=geometry2
		 * */
		private Point getIntersection(Vector2 other1, Vector2 other2) {
			// the Link's line in Ax + By + C form
			float A1 = p2.y - p1.y;
			float B1 = p1.x - p2.x;
			float C1 = A1 * p1.x + B1 * p1.y;
			// the other line in the same format
			float A2 = other2.y - other1.y;
			float B2 = other1.x - other2.x;
			float C2 = A2 * other1.x + B2 * other1.y;

			float det = A1 * B2 - A2 * B1;
			if (det == 0) {
				return null;
			}
			else {
				float ret_x = (B2*C1 - B1*C2)/det;
				float ret_y = (A1*C2 - A2*C1)/det;
				if ((Mathf.Min(p1.x, p2.x) <= ret_x && ret_x <= Mathf.Max (p1.x, p2.x)) &&
				    (Mathf.Min (p1.y, p2.y) <= ret_y && ret_y <= Mathf.Max (p1.y, p2.y)) &&
					(Mathf.Min (other1.x, other2.x) <= ret_x && ret_x <= Mathf.Max (other1.x, other2.x)) &&
				    (Mathf.Min (other1.y, other2.y) <= ret_y && ret_y <= Mathf.Max (other1.y, other2.y))) {
					// if you fall within both line segments' limits, the segments intersect
					return new Point(ret_x, ret_y);
				}
				return null;
			}
		}

		/**
		 * Finds the closest point on line to circle with radius r and position p
		 * */
		private Vector2 closestOnLine(float r, Vector2 p) {
			
			Vector2 seg_v = p2.vectorize2()-p1.vectorize2();
			Vector2 pt_v = p - p1.vectorize2();
			seg_v.Normalize ();
			
			float proj_v_length = Vector2.Dot (pt_v, seg_v.normalized);
			Vector2 closest = new Vector2 ();
			if (proj_v_length <= 0)
				closest = p1.vectorize2();
			else if (proj_v_length >= seg_v.magnitude)
				closest = p2.vectorize2 ();
			else {
				Vector2 proj_v = proj_v_length * seg_v.normalized;
				closest = p1.vectorize2() + proj_v;
			}
			return closest;
			
		}

		public void draw() {
			l.SetPosition (0, p1.vectorize());
			l.SetPosition (1, p2.vectorize());
		}
	}
	
	// Update is called once per frame
	void Update () {

		updateMoving ();

		if (!isVisible()){
				Object.Destroy (this.gameObject);
		}

		// destroy a projectile that has not moved in 2 seconds
		if (!isMoving && !timed) {
			last_fixed_time = Time.time;
			timed = true;
		}
		if (timed) {
			if (Time.time - last_fixed_time > 4) {
				if (!isMoving) {
					Object.Destroy(this.gameObject);
				}
				timed = false;
			}
		}

		
		for (int i = 0; i < numPoints; i++) {
			points[i].physics_update();
		}
		// for each constraint solve
		for (int solve = 0; solve < 3; solve++) {
			
			for (int i = 0; i < numLinks; i++) {
				links[i].handleCollisions();
			}

			// solve each link constraint
			for (int i = 0; i < numLinks; i++) {
				links[i].solve();
			}
		}
		// draw lines
		for (int i = 0; i < numVisibleLinks; i++) {
			links[i].draw();
		}

	}
	

	public void shoot(Vector2 initialPosition, Vector2 initialVelocity) {

		initialPos = new Point (initialPosition.x, initialPosition.y) - new Point(1.5f, -1);
		initialVel = new Point (initialVelocity.x, initialVelocity.y);

		// initialize a list of points, then build links between them
		numPoints = 19;
		points = new Point[numPoints];
		points [0] = new Point (0, 0) + initialPos;
		points [1] = new Point (0, 1 ) + initialPos;
		points [2] = new Point (1, 0 ) + initialPos;
		points [3] = new Point (1, 1 ) + initialPos;
		
		points [4] = new Point (1.5f, -1 ) + initialPos;
		points [5] = new Point (1.5f, -3 ) + initialPos;
		points [6] = new Point (4, -1 ) + initialPos;
		points [7] = new Point (4, -3 ) + initialPos;
		
		points [8] = new Point (2, -3 ) + initialPos;
		points [9] = new Point (2, -4 ) + initialPos;
		points [10] = new Point (2, -5 ) + initialPos;
		points [11] = new Point (1.5f, -5 ) + initialPos;
		
		points [12] = new Point (3.5f, -3 ) + initialPos;
		points [13] = new Point (3.5f, -4 ) + initialPos;
		points [14] = new Point (3.5f, -5 ) + initialPos;
		points [15] = new Point (3, -5 ) + initialPos;

		points [16] = new Point (5, 0) + initialPos;

		points [17] = new Point (0.25f, 0.66f) + initialPos;
		points [18] = new Point (0.5f, 0.66f) + initialPos;

		for (int i = 0; i < numPoints; i++) {
			points[i].setVelocity(initialVel);
		}
		
		numVisibleLinks = 19;
		numLinks = 33;
		links = new Link[numLinks];
		// here are the visible links
		links [0] = new Link (points [0], points [1], Point.prevDistance(points[0], points[1]));
		links [1] = new Link (points [0], points [2], Point.prevDistance(points[0], points[2]));
		links [2] = new Link (points [1], points [3], Point.prevDistance(points[1], points[3]));
		links [3] = new Link (points [2], points [3], Point.prevDistance(points[2], points[3]));
		
		links [4] = new Link (points[2], points[4], Point.prevDistance(points[2], points[4]));
		
		links [5] = new Link (points [4], points [5], Point.prevDistance (points [4], points [5]));
		links [6] = new Link (points [4], points [6], Point.prevDistance (points [4], points [6]));
		links [7] = new Link (points [5], points [8], Point.prevDistance (points [5], points [8]));
		links [8] = new Link (points [8], points [12], Point.prevDistance (points [8], points [12]));
		links [9] = new Link (points [12], points [7], Point.prevDistance (points [12], points [7]));
		links [10] = new Link (points [6], points [7], Point.prevDistance (points [6], points [7]));
		
		links [11] = new Link (points [8], points [9], Point.prevDistance (points [8], points [9]));
		links [12] = new Link (points [9], points [10], Point.prevDistance (points [9], points [10]));
		links [13] = new Link (points [10], points [11], Point.prevDistance (points [10], points [11]));

		links [14] = new Link (points [12], points [13], Point.prevDistance (points [12], points [13]));
		links [15] = new Link (points [13], points [14], Point.prevDistance (points [13], points [14]));
		links [16] = new Link (points [14], points [15], Point.prevDistance (points [14], points [15]));

		links [17] = new Link (points [6], points [16], Point.prevDistance (points [6], points [16]));

		links [18] = new Link (points [17], points [18], Point.prevDistance (points [17], points [18]));

		// and here are the invisible ones
		// these make the body stay rectangular
		links [19] = new Link (points [4], points [7], Point.prevDistance (points [4], points [7]));
		links [20] = new Link (points [5], points [6], Point.prevDistance (points [5], points [6]));
		links [21] = new Link (points [8], points [6], Point.prevDistance (points [8], points [6]));
		links [22] = new Link (points [12], points [4], Point.prevDistance (points [12], points [4]));
		links [23] = new Link (points [8], points [4], Point.prevDistance (points [8], points [4]));
		links [24] = new Link (points [12], points [6], Point.prevDistance (points [12], points [6]));
		links [25] = new Link (points [5], points [7], Point.prevDistance (points [5], points [7]));
		// these 2 make the head stay rectangular
		links [26] = new Link (points [0], points [3], Point.prevDistance (points [0], points [3]));
		links [27] = new Link (points [1], points [2], Point.prevDistance (points [1], points [2]));
		// and these hold the eye in place
		links [28] = new Link (points [1], points [17], Point.prevDistance (points [1], points [17]));
		links [29] = new Link (points [3], points [18], Point.prevDistance (points [3], points [18]));
		links [30] = new Link (points [2], points [18], Point.prevDistance (points [2], points [18]));
		// finally we hold the feet at 90 deg angles
		links [31] = new Link (points[9], points[11], Point.prevDistance(points[9], points[11]));
		links [32] = new Link (points [13], points [15], Point.prevDistance (points [13], points [15]));

		for (int i = 0; i < numLinks; i++) {
			// for each link make a child with a line renderer
			GameObject g = new GameObject();
			g.transform.position = transform.position;
			g.transform.SetParent (transform);
			lr = (LineRenderer)g.AddComponent <LineRenderer>();
			//lr.material = new Material (Shader.Find("Diffuse"));
			//lr.SetColors (new Color (0, 0, 0), new Color (0, 0, 0));
			lr.SetWidth(0.2f, 0.2f);
			lr.SetVertexCount (2);
			links[i].l = lr;
			links[i].g = this.gameObject;
		}
	}

	private void updateMoving() {
		bool b = false;
		for (int i = 0; i < numPoints; i++) {
			Point p = points[i];
			b = Mathf.Sqrt( Mathf.Pow(p.x - p.prev_x, 2) + Mathf.Pow(p.y - p.prev_y, 2)) < 0.00001f;
		}
		isMoving = b;
	}

	private bool isVisible() {
		for (int i = 0; i < numPoints; i++) {
			Vector3 pos = Camera.main.WorldToViewportPoint (points[i].vectorize());
			if (pos.x < 0.0 || pos.x > 1.0 || pos.y < 0.0 || pos.y > 1.0) return false;
		}
		return true;
	}

}
