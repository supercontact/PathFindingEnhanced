using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class Vertex {
	/// <summary>
	/// The position of the vertex.
	/// </summary>
	public Vector3 p;
	/// <summary>
	/// One of the halfedge pointing to the vertex.
	/// </summary>
	public Halfedge edge;
	/// <summary>
	/// A temporary list to store all edges.
	/// </summary>
	public List<Halfedge> edges;
	/// <summary>
	/// The index of the vertex in the vertices array.
	/// </summary>
	public int index = 0;
	/// <summary>
	/// Whether the vertex is on a border.
	/// </summary>
	public bool onBorder;

	public Vertex(Vector3 pos) {
		p = pos;
		onBorder = true;
	}

	/// <summary>
	/// Calculates the weight of the vertex (A third of the areas of its neighboring triangles).
	/// </summary>
	public double CalculateVertexAreaTri() {
		double result = 0;
		FillEdgeArray();
		foreach (Halfedge edge in edges) {
			if (edge.face.index != -1) {
				result += edge.face.CalculateAreaTri();
			}
		}
		result /= 3;
		ClearEdgeArray();
		return result;
	}

	/// <summary>
	/// Calculates the average normal of its neighboring triangles.
	/// </summary>
	public Vector3 CalculateNormalTri() {
		Vector3 result = new Vector3();
		FillEdgeArray();
		foreach (Halfedge edge in edges) {
			if (edge.face.index != -1) {
				result += edge.face.CalculateNormalTri();
			}
		}
		result.Normalize();
		ClearEdgeArray();
		return result;
	}

	/// <summary>
	/// Fills the edges array which stores all incident halfedges.
	/// </summary>
	public void FillEdgeArray() {
		edges = new List<Halfedge>();
		Halfedge first = edge, temp = edge;
		do {
			edges.Add(temp);
			temp = temp.next.opposite;
		} while (temp != null && temp != first);
		if (temp == null) {
			temp = first;
			while (temp.opposite != null) {
				temp = temp.opposite.prev;
				edges.Add(temp);
			}
		}
	}

	/// <summary>
	/// Clears the edges array.
	/// </summary>
	public void ClearEdgeArray() {
		edges = null;
	}
}


public class Face {
	/// <summary>
	/// One of the halfedge which belongs to the face
	/// </summary>
	public Halfedge edge;
	/// <summary>
	/// A temporary list to store all edges.
	/// </summary>
	public List<Halfedge> edges;
	/// <summary>
	/// The index of the face in the faces array.
	/// </summary>
	public int index;

	/// <summary>
	/// Calculates the area of the triangle face.
	/// </summary>
	public double CalculateAreaTri() {
		Vector3 a = edge.vertex.p;
		Vector3 b = edge.next.vertex.p;
		Vector3 c = edge.prev.vertex.p;
		return Vector3.Cross((b-a), (c-a)).magnitude / 2;
	}
	/// <summary>
	/// Calculates the normal of the triangle face.
	/// </summary>
	public Vector3 CalculateNormalTri() {
		Vector3 a = edge.vertex.p;
		Vector3 b = edge.next.vertex.p;
		Vector3 c = edge.prev.vertex.p;
		return Vector3.Cross((b-a), (c-a)).normalized;
	}
	/// <summary>
	/// Calculates the center of the face.
	/// </summary>
	public Vector3 CalculateCenter() {
		FillEdgeArray();
		Vector3 c = new Vector3();
		foreach(Halfedge edge in edges) {
			c += edge.vertex.p;
		}
		c /= edges.Count;
		ClearEdgeArray();
		return c;
	}
	/// <summary>
	/// Fills the edges array which stores all halfedges on the face.
	/// </summary>
	public void FillEdgeArray() {
		edges = new List<Halfedge>();
		Halfedge first = edge, temp = edge;
		do {
			edges.Add(temp);
			temp = temp.next;
		} while (temp != first);
	}
	/// <summary>
	/// Clears the edges array.
	/// </summary>
	public void ClearEdgeArray() {
		edges = null;
	}
}


public class Halfedge {
	public Halfedge next;
	public Halfedge prev;
	public Halfedge opposite;
	/// <summary>
	/// The vertex it points to;
	/// </summary>
	public Vertex vertex;
	public Face face;
    public bool isBorder = false;

	public double Length() {
		return (vertex.p - prev.vertex.p).magnitude;
	}
}


/// <summary>
/// The halfedge representation of a mesh.
/// </summary>
public class Geometry {

	/// <summary>
	/// The mesh linked to this geometry.
	/// </summary>
	public Mesh linkedMesh;
	
	public List<Vertex> vertices;
	public List<Halfedge> halfedges;
	public List<Face> faces;
	public List<Face> boundaries;

	/// <summary>
	/// The max edge length.
	/// </summary>
	public double h;
	public bool hasBorder {
		get {return boundaries.Count > 0;}
	}

	public Geometry() {
		vertices = new List<Vertex>();
		halfedges = new List<Halfedge>();
		faces = new List<Face>();
		boundaries = new List<Face>();
	}
	public Geometry(Mesh mesh) {
		vertices = new List<Vertex>();
		halfedges = new List<Halfedge>();
		faces = new List<Face>();
		boundaries = new List<Face>();
		FromMesh(mesh);
	}

	~Geometry() {
		Clear();
	}

	/// <summary>
	/// Unreferences all objects, letting them be collected by GC.
	/// </summary>
	public void Clear() {
		foreach (Vertex v in vertices) {
			v.edge = null;
			v.edges = null;
			v.ClearEdgeArray();
		}
		foreach (Halfedge e in halfedges) {
			e.next = null;
			e.prev = null;
			e.opposite = null;
			e.face = null;
			e.vertex = null;
		}
		foreach (Face f in faces) {
			f.edge = null;
			f.ClearEdgeArray();
		}
		vertices.Clear();
		halfedges.Clear();
		faces.Clear();
	}

	/// <summary>
	/// Construct the geometry from a mesh.
	/// </summary>
	public void FromMesh(Mesh mesh) {
		linkedMesh = mesh;
		Clear();

		// Build vertices
		Vector3[] meshVerts = mesh.vertices;
		for (int i = 0; i < meshVerts.Length; i++) {
			vertices.Add(new Vertex(meshVerts[i]));
			vertices[i].index = i;
			vertices[i].edges = new List<Halfedge>();
		}

		// Build faces and halfedges, linked to vertices
		int[] meshFaces = mesh.triangles;
		for (int i = 0; i < meshFaces.Length / 3; i++) {
			Face trig = new Face();
			Halfedge e1 = new Halfedge(), e2 = new Halfedge(), e3 = new Halfedge();
			e1.face = trig;
			e1.next = e2;
			e1.prev = e3;
			e1.vertex = vertices[meshFaces[3*i]];
			e2.face = trig;
			e2.next = e3;
			e2.prev = e1;
			e2.vertex = vertices[meshFaces[3*i+1]];
			e3.face = trig;
			e3.next = e1;
			e3.prev = e2;
			e3.vertex = vertices[meshFaces[3*i+2]];
			trig.edge = e1;
			trig.index = i;
			
			faces.Add(trig);
			halfedges.Add(e1);
			halfedges.Add(e2);
			halfedges.Add(e3);
			e1.vertex.edge = e1;
			e1.vertex.edges.Add(e1);
			e2.vertex.edge = e2;
			e2.vertex.edges.Add(e2);
			e3.vertex.edge = e3;
			e3.vertex.edges.Add(e3);
		}

		// Set the corresponding opposite to each halfedge
		for (int i = 0; i < vertices.Count; i++) {
			vertices[i].onBorder = false;
			for (int j = 0; j < vertices[i].edges.Count; j++) {
				Halfedge et = vertices[i].edges[j];
				if (et.opposite == null) {
					Vertex vt = et.prev.vertex;
					bool foundOpposite = false;
					for (int k = 0; k < vt.edges.Count; k++) {
						if (vt.edges[k].prev.vertex == vertices[i]) {
							if (foundOpposite) {
								//GameObject.Instantiate(GameObject.Find("Pin")).transform.position = vertices[i].p;
								throw new Exception("Edge shared by 3 faces, not manifold!");
							}
							et.opposite = vt.edges[k];
							//vt.edges[k].opposite = et;
							//break;
							foundOpposite = true;
						}
					}

					// If no opposite, we are on a boundary
					if (et.opposite == null) {
						vertices[i].onBorder = true;
						et.opposite = new Halfedge();
						et.opposite.opposite = et;
						et.opposite.vertex = et.prev.vertex;
                        et.opposite.isBorder = true;
						halfedges.Add(et.opposite);
					}
				}
			}
		}
		
		// Reconnect all newly created halfedges on a boundary
		for (int i = 0; i < halfedges.Count; i++) {
			if (halfedges[i].next == null) {
				// This halfedge is on a boundary, adding a new boundary face.
				Face boundary = new Face();
				boundaries.Add(boundary);
				boundary.edge = halfedges[i];
				boundary.index = -1;

				// Connect all halfedges of this boundary
				Halfedge first = halfedges[i], temp = halfedges[i];
				do {
					Halfedge next = temp.opposite;
					while (next.prev != null) {
						next = next.prev.opposite;
					}
					temp.next = next;
					next.prev = temp;
					temp.face = boundary;
					temp.vertex.edges.Add(temp);
					temp = next;
				} while (temp != first);
			}
		}

		for (int i = 0; i < vertices.Count; i++) {
			vertices[i].ClearEdgeArray();
		}

		// Calculate the max edge length
		h = 0;
		foreach (Halfedge e in halfedges) {
			if (e.Length() > h) {
				h = e.Length();
			}
			//h += e.Length();
		}
		//h /= halfedges.Count;

		Debug.Log("Geometry created, Eular number = " + (vertices.Count - halfedges.Count / 2 + faces.Count));
	}
	
	public void ToMesh(Mesh mesh) {
		Vector3[] verts = new Vector3[vertices.Count];
		for (int i = 0; i < vertices.Count; i++) {
			verts[i] = vertices[i].p;
			vertices[i].index = i;
		}
		int[] trigs = new int[faces.Count * 3];
		for (int i = 0; i < faces.Count; i++) {
			trigs[3*i] = faces[i].edge.vertex.index;
			trigs[3*i+1] = faces[i].edge.next.vertex.index;
			trigs[3*i+2] = faces[i].edge.prev.vertex.index;
		}
		mesh.vertices = verts;
		mesh.triangles = trigs;
		mesh.RecalculateNormals();
	}

    public Graph ToGraph() {
        Graph result = new Graph();
        for (int i = 0; i < faces.Count; i++) {
            result.AddNode(faces[i].CalculateCenter());
        }
        for (int i = 0; i < halfedges.Count; i++) {
            if (!halfedges[i].isBorder && !halfedges[i].opposite.isBorder) {
                result.AddArc(halfedges[i].face.index, halfedges[i].opposite.face.index);
            }
        }
        return result;
    }

	/// <summary>
	/// Calculates the Lc sparse matrix (unweighted laplacien matrix) multilied by the factor.
	/// If considerBorder is set to true, the rows and columns of the border vertices will be set to 0 (Dirichlet condition).
	/// </summary>
	public alglib.sparsematrix CalculateLcMatrixSparse(double factor = 1, bool considerBorder = false, IEnumerable<Vertex> multiSources = null) {
		int n = vertices.Count;
		alglib.sparsematrix result;
		alglib.sparsecreate(n, n, out result);

		HashSet<Vertex> srcs = null;
		double[] modif = null;
		if (multiSources != null) {
			srcs = new HashSet<Vertex>(multiSources);
			modif = new double[n];
		}

		for (int i = 0; i < n; i++) {
			if ((!considerBorder || !vertices[i].onBorder) && (srcs == null || !srcs.Contains(vertices[i]))) {
				vertices[i].FillEdgeArray();
				Vector3 vi = vertices[i].p;
				double aii = 0;
				foreach (Halfedge e in vertices[i].edges) {
					int j = e.prev.vertex.index;
					Vector3 vj = e.prev.vertex.p;
					Vector3 va = e.next.vertex.p;
					Vector3 vb = e.opposite.next.vertex.p;
					double cota = 0;
					double cotb = 0;

					if (e.face.index != -1) {
						double cosa = Vector3.Dot((vi - va), (vj - va)) / (vi - va).magnitude / (vj - va).magnitude;
						cota = cosa / Math.Sqrt(1 - cosa * cosa);
						if (double.IsNaN(cota) || System.Math.Abs(cota) > Settings.cotLimit) {
							cota = cosa > 0 ? Settings.cotLimit : -Settings.cotLimit;
						}
					}

					if (e.opposite.face.index != -1) {
						double cosb = Vector3.Dot((vi - vb), (vj - vb)) / (vi - vb).magnitude / (vj - vb).magnitude;
						cotb = cosb / Math.Sqrt(1 - cosb * cosb);
						if (double.IsNaN(cotb) || System.Math.Abs(cotb) > Settings.cotLimit) {
							cotb = cosb > 0 ? Settings.cotLimit : -Settings.cotLimit;
						}
					}

					aii -= factor * (cota + cotb) / 2;
					if (!considerBorder || !vertices[j].onBorder) {
						if (srcs == null || !srcs.Contains(vertices[j])) {
							alglib.sparseset(result, i, j, factor * (cota + cotb) / 2);
						} else {
							modif[i] -= factor * (cota + cotb) / 2;
						}
					}
				}
				alglib.sparseset(result, i, i, aii);
			}
		}
		modification = modif;
		return result;
	}
	// Temporary vector
	public double[] modification;

	/// <summary>
	/// Calculates 3 values of cotangent * opposite edge vector of every triangle.
	/// Stored in an array of 3*nFace elements.
	/// It can be used to calculate divergence.
	/// </summary>
	public Vector3[] CalculateDivData() {
		Vector3[] result = new Vector3[faces.Count * 3];
		int counter = 0;
		for (int i = 0; i < faces.Count; i++) {
			faces[i].FillEdgeArray();
			foreach (Halfedge edge in faces[i].edges) {
				Vector3 v = edge.vertex.p;
				Vector3 v1 = edge.prev.vertex.p;
				Vector3 v2 = edge.next.vertex.p;
				double cos = Vector3.Dot(v1-v, v2-v) / (v1-v).magnitude / (v2-v).magnitude;
				double cot = cos / System.Math.Sqrt(1 - cos * cos);
				if (double.IsNaN(cot) || System.Math.Abs(cot) > Settings.cotLimit) {
					cot = cos > 0 ? Settings.cotLimit : -Settings.cotLimit;
				}
				result[counter++] = (float) cot * (v2 - v1) / 2;
			}
			faces[i].ClearEdgeArray();
		}
		return result;
	}


	/// <summary>
	/// Sets the position of a vertex to the average position of its neighboring vertices.
	/// </summary>
	public void FixVertex(int index) {
		vertices[index].FillEdgeArray();
		Vector3 pos = new Vector3();
		foreach (Halfedge edge in vertices[index].edges) {
			pos += edge.prev.vertex.p;
		}
		pos /= vertices[index].edges.Count;
		vertices[index].ClearEdgeArray();
		vertices[index].p = pos;
		Vector3[] verts = linkedMesh.vertices;
		verts[index] = pos;
		linkedMesh.vertices = verts;
	}


}

