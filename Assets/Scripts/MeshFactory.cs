using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

/// <summary>
/// A color mixer represents a gradient color set.
/// Given a number t between 0 and 1, the output color c(t) is linearly interpolated between the 2 neighboring color nodes
/// </summary>
public class ColorMixer {
	
	private struct ColorNode {
		public Color color;
		public float coeff;
		public ColorNode(Color color, float coeff) {
			this.color = color;
			this.coeff = coeff;
		}
	}
	private LinkedList<ColorNode> colors;

	public ColorMixer() {
		colors = new LinkedList<ColorNode>();
	}

	/// <summary>
	/// Inserts a color node at certain position.
	/// </summary>
	public void InsertColorNode(Color color, float coeff) {
		LinkedListNode<ColorNode> current = colors.First;
		while (current != null && current.Value.coeff < coeff) {
			current = current.Next;
		}
		if (current == null)
			colors.AddLast(new ColorNode(color, coeff));
		else
			colors.AddBefore(current, new ColorNode(color, coeff));
	}

	/// <summary>
	/// Returns the color at certain position.
	/// Value should be between 0 and 1.
	/// </summary>
	public Color GetColor(float value) {
		LinkedListNode<ColorNode> current = colors.First;
		while (current != null && current.Value.coeff < value) {
			current = current.Next;
		}
		if (current == null) 
			return colors.Last.Value.color;
		if (current.Previous == null)
			return colors.First.Value.color;
		ColorNode left = current.Previous.Value;
		ColorNode right = current.Value;
		return Color.Lerp(left.color, right.color, (value - left.coeff) / (right.coeff - left.coeff));
	}
}

/// <summary>
/// The MeshFactory class is a collection of methods for manipulating mesh and textures.
/// </summary>
public class MeshFactory {

	/// <summary>
	/// Reads the mesh from a OFF format text file.
	/// The .txt file should be put in the Resources folder
	/// Can also apply a transformation when reading the mesh.
	/// </summary>
	public static Mesh ReadMeshFromFile(string file, float scaleFactor = 1, Vector3 offset = default(Vector3), Quaternion rotation = default(Quaternion)) {
		int state = 0;
		int v = 0, f = 0, counter = 0;
		Vector3[] vertices = null;
		bool[] verticesIsUsed = null;
		int[] triangles = null;
		if (rotation == default(Quaternion)) {
			rotation = Quaternion.identity;
		}

		try {
			string line;
			TextAsset asset = Resources.Load(file) as TextAsset;
			StreamReader theReader = new StreamReader(new MemoryStream(asset.bytes), Encoding.Default);
			using (theReader) {
				line = theReader.ReadLine();
				if (!line.Equals("OFF")) {
					Debug.Log("Not OFF!");
					return null;
				}
				do {
					line = theReader.ReadLine();
					if (line != null) {
						string[] entriesRaw = line.Split(' ');
						List<string> entries = new List<string>();

						// Remove empty entries
						for (int i = 0; i < entriesRaw.Length; i++) {
							if (!entriesRaw[i].Equals("")) {
								entries.Add(entriesRaw[i]);
							}
						}

						if (entries.Count > 0) {
							// Comments in the OFF file
							if (entries[0].Equals("#")) continue;
	
							if (state == 0) {
								// Reading mesh information (first line of data)
								v = int.Parse(entries[0]);
								f = int.Parse(entries[1]);
								vertices = new Vector3[v];
								verticesIsUsed = new bool[v];
								triangles = new int[3*f];
								state = 1;
								counter = 0;
							} else if (state == 1) {
								// Reading vertex positions
								vertices[counter++] = rotation * new Vector3(float.Parse(entries[0]), float.Parse(entries[1]), float.Parse(entries[2])) * scaleFactor + offset;
								if (counter == v) {
									state = 2;
									counter = 0;
								}
							} else if (state == 2) {
								// Reading triangles
								if (!entries[0].Equals("3")) {
									Debug.Log(entries[0]+"Not a triangle mesh!");
									return null;
								}
								triangles[counter] = int.Parse(entries[1]);
								verticesIsUsed[triangles[counter++]] = true;
								triangles[counter] = int.Parse(entries[2]);
								verticesIsUsed[triangles[counter++]] = true;
								triangles[counter] = int.Parse(entries[3]);
								verticesIsUsed[triangles[counter++]] = true;
							}
						}
					}
				} while (line != null);
				theReader.Close();
			}
		} catch (Exception e) {
			Debug.Log(e.StackTrace);
			return null;
		}

		// Delete unreferenced vertices
		int[] verticesDeletedBefore = new int[v]; // How many vertices are deleted before the i-th vertex in the original vertices array
		verticesDeletedBefore[0] = verticesIsUsed[0] ? 0 : 1;
		for (int i = 1; i < v; i++) {
			verticesDeletedBefore[i] = verticesIsUsed[i] ? verticesDeletedBefore[i-1] : verticesDeletedBefore[i-1] + 1;
		}
		// Save to a new vertices array
		Vector3[] usedVertices = new Vector3[v - verticesDeletedBefore[v - 1]];
		for (int i = 0; i < v; i++) {
			if (verticesIsUsed[i]) {
				usedVertices[i - verticesDeletedBefore[i]] = vertices[i];
			}
		}
		// Shift triangle indices
		for (int i = 0; i < 3 * f; i++) {
			triangles[i] -= verticesDeletedBefore[triangles[i]];
		}

		// Create mesh object
		Mesh m = new Mesh();
		Debug.Log("New mesh loaded: "+file+" \nVertex count = "+usedVertices.Length+" triangle count = "+triangles.Length);
		m.vertices = usedVertices;
		m.triangles = triangles;
		m.RecalculateNormals();
		return m;
	}


	/// <summary>
	/// Merges all overlapping points (points with a distance smaller than the threshold between them).
	/// Useful for removing UV seams.
	/// </summary>
	/// <param name="mesh">Mesh.</param>
	public static void MergeOverlappingPoints(Mesh mesh, float threshold = 0.00001f) {
		Vector3[] verts = mesh.vertices;
		int[] triangles = mesh.triangles;

		int v = verts.Length;
		int f = triangles.Length / 3;

		KDTree kd = KDTree.MakeFromPoints(verts);
		int[] replacement = new int[verts.Length];
		for (int i = 0; i < v; i++) {
			replacement[i] = -1;
		}


		for (int i = 0; i < v; i++) {
			if (replacement[i] == -1) {
				// Replace all vertices within the threshold radius by this vertex
				int[] pts = kd.RangeSearch(verts[i], threshold);
				for (int j = 0; j < pts.Length; j++) {
					if (pts[j] != i) {
						replacement[pts[j]] = i;
					}
				}
			}
		}

		// Delete merged vertices
		int[] verticesDeletedBefore = new int[v]; // How many vertices are deleted before the i-th vertex in the original vertices array
		verticesDeletedBefore[0] = (replacement[0] == -1) ? 0 : 1;
		for (int i = 1; i < v; i++) {
			verticesDeletedBefore[i] = (replacement[i] == -1) ? verticesDeletedBefore[i-1] : verticesDeletedBefore[i-1] + 1;
		}
		// Save to a new vertices array
		Vector3[] usedVertices = new Vector3[v - verticesDeletedBefore[v - 1]];
		for (int i = 0; i < v; i++) {
			if (replacement[i] == -1) {
				usedVertices[i - verticesDeletedBefore[i]] = verts[i];
			}
		}
		// Shift triangle indices
		for (int i = 0; i < 3 * f; i++) {
			if (replacement[triangles[i]] != -1) {
				triangles[i] = replacement[triangles[i]];
			}
			triangles[i] -= verticesDeletedBefore[triangles[i]];
		}

		// Update mesh object
		mesh.triangles = triangles;
		mesh.vertices = usedVertices;
		mesh.RecalculateNormals();
	}


	/// <summary>
	/// Reorders the vertex indices of the mesh in order to decrease fill-in of a Cholesky decomposition.
	/// axis: 0 - x, 1 - y, 2 - z
	/// </summary>
	public static void ReorderVertexIndices(Mesh mesh, int axis = 0) {
		Vector3[] verts = mesh.vertices;
		int[] triangles = mesh.triangles;
		int n = verts.Length;
		int f = triangles.Length / 3;
		int[] oldIndices = new int[n];
		int[] newIndices = new int[n];
		for (int i = 0; i < n; i++) {
			oldIndices[i] = i;
		}

		Array.Sort<Vector3, int>(verts, oldIndices, new VecComparer(axis));
		for (int i = 0; i < n; i++) {
			newIndices[oldIndices[i]] = i;
		}
		for (int i = 0; i < f * 3; i++) {
			triangles[i] = newIndices[triangles[i]];
		}

		// Update mesh object
		mesh.triangles = triangles;
		mesh.vertices = verts;
		mesh.RecalculateNormals();
	}

	class VecComparer : IComparer<Vector3> {
		int axis;
		public VecComparer(int axis) {
			this.axis = axis;
		}
		public int Compare(Vector3 v1, Vector3 v2)
		{
			return (v1[axis] - v2[axis] > 0) ? 1 : ((v1[axis] - v2[axis] < 0) ? -1 : 0);
		}
	}


	/// <summary>
	/// Applys a transform to a mesh.
	/// </summary>
	public static void TransformMesh(Mesh mesh, Vector3 offset, Quaternion rot = default(Quaternion), Vector3 scale = default(Vector3)) {
		if (rot == default(Quaternion)) {
			rot = Quaternion.identity;
		}
		if (scale == default(Vector3)) {
			scale = new Vector3(1,1,1);
		}

		Vector3[] verts = mesh.vertices;
		for (int i = 0; i < verts.Length; i++) {
			verts[i] = new Vector3(verts[i].x * scale.x, verts[i].y * scale.y, verts[i].z * scale.z);
			verts[i] = rot * verts[i] + offset;
		}
		mesh.vertices = verts;
		mesh.RecalculateNormals();
	}


	/// <summary>
	/// Creates a sphere mesh.
	/// c is the segment count.
	/// </summary>
	public static Mesh CreateSphere(float radius, int c) {
		Vector3[] vertices = new Vector3[2+(c-1)*2*c];
		vertices[0] = new Vector3(0, 0, radius);
		vertices[vertices.Length - 1] = new Vector3(0, 0, -radius);
		for (int i = 1; i < c; i++) {
			for (int j = 0; j < 2*c; j++) {
				vertices[1+(i-1)*2*c+j] = new Vector3(
					radius * Mathf.Sin(Mathf.PI*i/(float)c) * Mathf.Sin(0.5f * Mathf.PI*(2*j-i)/(float)c),
					radius * Mathf.Sin(Mathf.PI*i/(float)c) * Mathf.Cos(0.5f * Mathf.PI*(2*j-i)/(float)c),
					radius * Mathf.Cos(Mathf.PI*i/(float)c));
			}
		}
		int[] triangles = new int[12*c*(c-1)];
		int count = 0;
		for (int j = 0; j < 2*c; j++) {
			triangles[count++] = 0;
			triangles[count++] = 1 + (j+1)%(2*c);
			triangles[count++] = 1 + j;
		}
		for (int i = 1; i < c-1; i++) {
			for (int j = 0; j < 2*c; j++) {
				triangles[count++] = 1+(i-1)*2*c+j;
				triangles[count++] = 1+i*2*c+(j+1)%(2*c);
				triangles[count++] = 1+i*2*c+j;
				triangles[count++] = 1+(i-1)*2*c+j;
				triangles[count++] = 1+(i-1)*2*c+(j+1)%(2*c);
				triangles[count++] = 1+i*2*c+(j+1)%(2*c);
			}
		}
		for (int j = 0; j < 2*c; j++) {
			triangles[count++] = vertices.Length -1;
			triangles[count++] = 1+(c-2)*2*c+j;
			triangles[count++] = 1+(c-2)*2*c+(j+1)%(2*c);
		}
		Mesh m = new Mesh();
		m.vertices = vertices;
		m.triangles = triangles;
		m.RecalculateNormals();
		return m;
	}


	/// <summary>
	/// Creates a striped texture.
	/// </summary>
	public static Texture2D CreateStripedTexture(int size, int stripePeriod, int stripeWidth, int offset, ColorMixer background, ColorMixer stripe, bool colorEveryPeriod = false) {
		Texture2D result = new Texture2D(size, 1, TextureFormat.ARGB32, false);
		for (int i = 0; i < size; i++) {
			int t = (i - offset) % stripePeriod;
			if (t < 0) {
				t += stripePeriod;
			}
			if (!colorEveryPeriod) {
				result.SetPixel(i, 0, t < stripeWidth ? stripe.GetColor(i/(float)size) : background.GetColor(i/(float)size));
			} else {
				result.SetPixel(i, 0, t < stripeWidth ? stripe.GetColor(t/(float)stripeWidth) : background.GetColor((t-stripeWidth)/(float)(stripePeriod-stripeWidth)));
			}
		}
		result.Apply();
		return result;
	}
}
