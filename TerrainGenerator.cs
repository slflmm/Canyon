using UnityEngine;
using System.Collections;

public class TerrainGenerator : MonoBehaviour {

	public float topHeight;
	public float bottomHeight;
	public float stdInit;

	public int width;
	public int resolution; 
	
	private float roughness; 
	private int nPoints;

	public Vector2[] linePoints;

	// Use this for initialization
	void Start () {

		roughness = 1/Mathf.Sqrt(2); // Brownian motion roughness

		nPoints = width * resolution * 6; // determine how many points we will have in the heightmap
		
		// here is where we run the midpoint displacement algorithm and get a list of height points
		float[] heightPoints = generatePoints (topHeight, bottomHeight, nPoints);
		
		// using the height points obtained above, get the vertices, uv mappings, and triangulation to make a mesh
		Vector3[] vertices = defineVertices (heightPoints, 0-width, width*6);
		Vector2[] uvs = defineUVs (heightPoints);
		int[] triangles = defineTriangles (heightPoints);
		
		// instantiate the actual mesh
		createMesh (vertices, uvs, triangles);
	}

	/**
	 * Returns an array of equally-spaced height points, based on the start and end heights provided.
	 * */
	float[] generatePoints (float startHeight, float endHeight, int n) {
		float[] points = new float[n];

		// set the extrema to be canyon measurements
		points [0] = startHeight;

		points [n / 6 - 1] = startHeight;
		points [2 * n / 6 - 1] = endHeight;
		points [3 * n / 6 - 1] = endHeight;
		points [4 * n / 6 - 1] = endHeight;
		points [5 * n / 6 - 1] = startHeight;
		points [points.Length - 1] = startHeight;

		// now run the midpoint displacement algorithm to modify array values
		midpointDisplacement (points, stdInit, 0, n / 6);
		midpointDisplacement (points, stdInit, n / 6 -1, 2 * n / 6);
		midpointDisplacement (points, stdInit, 2 * n / 6 -1, 3 * n / 6);
		midpointDisplacement (points, stdInit, 3 * n / 6 -1, 4 * n / 6);
		midpointDisplacement (points, stdInit, 4 * n / 6 -1, 5 * n / 6);
		midpointDisplacement (points, stdInit, 5 * n / 6 -1, n);

		return points;
	}

	/**
	 * Applies the midpoint displacement algorithm to some array of points p.
	 * Assumes the first and last height values are filled.
	 * */
	void midpointDisplacement(float[] p, float std, int start, int end) {
		int size = end - start;
		float newstd = std * roughness;

		if (size <= 2) return; // stop if you can't break it down further
		else {
			int middle = start + size/2;
			p[middle] = (p[start] + p[end-1])/2 + Gaussian (std); // get this midpoint's value
			midpointDisplacement(p, newstd, start, middle + 1); // fill in the values for the first half
			midpointDisplacement (p, newstd, middle, end); // fill in the values for the second half
		}
	}

	/**
	 * Create a list of vertices for the polygon formed by the given list of heights.
	 * Ordered to make triangulation easier.
	 * */
	Vector3[] defineVertices(float[] heights, float startX, float width) {
		Vector3[] vertices = new Vector3[nPoints*2];
		linePoints = new Vector2[nPoints];
		for (int i = 0, j = 0; j < heights.Length; i+=2, j++) {
			float xpos = startX + j*width*1.0f/nPoints;
			vertices[i] = new Vector3(xpos, heights[j], transform.position.z);
			vertices[i+1] = new Vector3(xpos, 0, transform.position.z);
			linePoints[j] = new Vector2(xpos, heights[j]); // keep track of the top-side points too
		}
		return vertices;
	}

	/**
	 * Create the UV mappings for mapping mesh coordinates to texture coordinates. 
	 * Fun times fiddling with magic numbers.
	 * */
	Vector2[] defineUVs(float[] heights) {
		Vector2[] uvs = new Vector2[nPoints*2];
		float f = 1.0f / heights.Length;
		for (int i = 0, j = 0; j < heights.Length; i+=2, j++) {
			uvs[i] = new Vector2(f*j*(width/4), heights[j]/(topHeight/2));
			uvs[i+1] = new Vector2(f*j*(width/4), 0);
		}
		return uvs;
	}

	/**
	 * Generates the triangles for the mesh.
	 * Each group of 4 vertices gets 2 new triangles (thankfully already ordered properly in defineVertices)
	 * */
	int[] defineTriangles(float[] heights) {
		int[] triangles = new int[heights.Length*6];
		for (int i = 0, j =0; i <= heights.Length*2 - 4; i += 2, j+= 6) {
			triangles[j] = i;
			triangles[j+1] = i+3;
			triangles[j+2] = i+1;
			triangles[j+3] = i+3;
			triangles[j+4] = i;
			triangles[j+5] = i+2;
		}
		return triangles;
	}

	/**
	 * Use the vertices, UVs, and triangles provided to construct a mesh.
	 * Also applies a texture.
	 * */
	void createMesh(Vector3[] vertices, Vector2[] uvs, int[] triangles) {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();

		GameObject o = new GameObject ("ground");
		o.AddComponent <MeshRenderer>();
		MeshFilter filter = (MeshFilter) o.AddComponent<MeshFilter>();
		filter.mesh = mesh;
		o.GetComponent<Renderer>().material.mainTexture = Resources.Load ("dirt2copy") as Texture;
		o.transform.parent = transform;
	}

	public bool inFlatLand(int i) {
		if (i < nPoints / 6 || i > 5*nPoints/6 || (i > 2*nPoints/6 && i < 4*nPoints/6))
			return true;
		else
			return false;
	}

	/**
	 * Uses a Box-Muller transform to get a number from a Gaussian distribution centered at 0 with the sppecified standard deviation.
	 * */
	private float Gaussian (float std) {
		float u1 = Random.value;
		float u2 = Random.value;
		float randStdNormal = Mathf.Sqrt (-2.0f * Mathf.Log (u1)) * Mathf.Sin (2.0f * Mathf.PI * u2); // gaussian(0,1)
		return std*randStdNormal; // gaussian(0, std)
	}
}
