using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Octree
{
    public int maxLevel;
    public Vector3 corner;
    public float size;
    public float cellSize {
        get { return size / (1 << maxLevel); }
    }
    public OctreeNode root;

    public static int[,] dir = { { 1, 0, 0 }, { -1, 0, 0 }, { 0, 1, 0 }, { 0, -1, 0 }, { 0, 0, 1 }, { 0, 0, -1 } };
    public static int[,] cornerDir = { { 0, 0, 0 }, { 1, 0, 0 }, { 1, 1, 0 }, { 0, 1, 0 }, { 0, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 0, 1, 1 } };

    public Octree(float _size, Vector3 _corner, int _maxLevel) {
        size = _size;
        corner = _corner;
        maxLevel = _maxLevel;
        root = new OctreeNode(0, new int[] { 0, 0, 0 }, null, this);
    }

    public int[] PositionToIndex(Vector3 p) {
        p -= corner;
        float d = cellSize;
        return new int[] { Mathf.FloorToInt(p.x / d), Mathf.FloorToInt(p.y / d), Mathf.FloorToInt(p.z / d) };
    }
    public Vector3 IndexToPosition(int[] gridIndex) {
        float d = cellSize;
        return new Vector3(gridIndex[0] * d, gridIndex[1] * d, gridIndex[2] * d) + corner;
    }

    public OctreeNode Find(int[] gridIndex) {
        return Find(gridIndex, maxLevel);
    }
    public OctreeNode Find(int[] gridIndex, int level) {
        int xi = gridIndex[0];
        int yi = gridIndex[1];
        int zi = gridIndex[2];
        int t = 1 << level;
        if (xi >= t || xi < 0 || yi >= t || yi < 0 || zi >= t || zi < 0) return null;
        OctreeNode current = root;
        for (int l = 0; l < level; l++) {
            t >>= 1;
            if (current.children == null) return current;
            current = current.children[xi / t, yi / t, zi / t];
            xi %= t;
            yi %= t;
            zi %= t;
        }
        return current;
    }
    public OctreeNode Find(Vector3 p) {
        return Find(PositionToIndex(p));
    }

    public void Divide(Vector3 p, bool markAsBlocked = false) {
        int[] gridIndex = PositionToIndex(p);
        int xi = gridIndex[0];
        int yi = gridIndex[1];
        int zi = gridIndex[2];
        int t = 1 << maxLevel;
        if (xi >= t || xi < 0 || yi >= t || yi < 0 || zi >= t || zi < 0) return;
        OctreeNode current = root;
        for (int l = 0; l < maxLevel; l++) {
            t >>= 1;
            current.containsBlocked = current.containsBlocked || markAsBlocked;
            if (current.children == null) current.CreateChildren();
            current = current.children[xi / t, yi / t, zi / t];
            xi %= t;
            yi %= t;
            zi %= t;
        }
        current.blocked = current.blocked || markAsBlocked;
        current.containsBlocked = current.blocked;
    }

    public void DivideTriangle(Vector3 p1, Vector3 p2, Vector3 p3, bool markAsBlocked = false) {
        root.DivideTriangleUntilLevel(p1, p2, p3, maxLevel, markAsBlocked);
    }

    public Graph ToCenterGraph() {
        List<OctreeNode> leaves = root.Leaves();
        Dictionary<OctreeNode, Node> dict = new Dictionary<OctreeNode, Node>();
        int count = 0;
        List<Node> nodes = new List<Node>();
        foreach (OctreeNode q in leaves) {
            if (!q.blocked) {
                Node n = new Node(q.center, count);
                dict.Add(q, n);
                nodes.Add(n);
                count++;
            }
        }
        foreach (OctreeNode q in leaves) {
            if (!q.blocked) {
                if (q.level == 0) continue;
                Node n = dict[q];
                for (int i = 0; i < 6; i++) {
                    OctreeNode found = Find(new int[] { q.index[0] + dir[i, 0], q.index[1] + dir[i, 1] }, q.level);
                    if (found == null || found.blocked) continue;
                    if (found.level < q.level) {
                        Node nFound = dict[found];
                        n.arcs.Add(new Arc(n, nFound));
                        nFound.arcs.Add(new Arc(nFound, n));
                    } else if (found.children == null) {
                        Node nFound = dict[found];
                        n.arcs.Add(new Arc(n, nFound));
                    }
                }
            }
        }
        Graph g = new Graph();
        g.nodes = nodes;
        return g;
    }

    public Graph ToCornerGraph() {
        List<OctreeNode> leaves = root.Leaves();
        Dictionary<long, Node> dict = new Dictionary<long, Node>();
		Dictionary<long, bool> arcAdded = new Dictionary<long, bool>();
        List<Node> nodes = new List<Node>();
        int count = 0;
        int rowCount = 1 << maxLevel + 1;
        foreach (OctreeNode o in leaves) {
			if (!o.blocked) {
	            Node[] octCornerNodes = new Node[8];
	            for (int i = 0; i < 8; i++) {
	                Vector3 octCorner = o.corners(i);
	                int[] cornerIndex = o.cornerIndex(i);
					long hash = (cornerIndex[0] * rowCount + cornerIndex[1]) * rowCount + cornerIndex[2];
	                Node n;
	                if (!dict.TryGetValue(hash, out n)) {
	                    n = new Node(octCorner, count);
	                    dict.Add(hash, n);
	                    nodes.Add(n);
	                    count++;
	                }
	                octCornerNodes[i] = n;
	            }

	            for (int i = 0; i < 6; i++) {
	                OctreeNode found = Find(new int[] { o.index[0] + dir[i, 0], o.index[1] + dir[i, 1] }, o.level);

					if (found == null || found.blocked || found.level < o.level || found.children == null) {
						int k = i / 2;
						int[,] c = new int[4,3];
						int counter = 0;
						for (int t1 = -1; t1 <= 1; t1++) {
							for (int t2 = -1; t2 <= 1; t2++) {
								c[counter,k] = dir[i, k];
								c[counter,(k+1) % 3] = t1;
								c[counter,(k+2) % 3] = t2 * t1;
								for (int j = 0; j < 3; j++) {
									c[counter, j] += o.index[j] * 2 + 1;
									c[counter, j] <<= (maxLevel - o.level);
								}
								counter++;
							}
						}
						for (int t = 0; t < 4; t++) {
							int[] arcCenter = new int[3];
							for (int j = 0; j < 3; j++) {
								arcCenter[j] = (c[t,j] + c[(t+1) % 4, j]) / 2;
							}
							long arcHash = (arcCenter[0] * rowCount * 2 + arcCenter[1]) * rowCount * 2 + arcCenter[2];
							bool temp;
							if (!arcAdded.TryGetValue(arcHash, out temp)) {
								arcAdded[arcHash] = true;
								long hash1 = (c[t,0] / 2 * rowCount + c[t,1] / 2) * rowCount + c[t,2] / 2;
								long hash2 = (c[(t+1) % 4,0] / 2 * rowCount + c[(t+1) % 4,1] / 2) * rowCount + c[(t+1) % 4,2] / 2;
								Node c1 = dict[hash1];
								Node c2 = dict[hash2];
								c1.arcs.Add(new Arc(c1, c2));
								c2.arcs.Add(new Arc(c2, c1));
							}
						}
	                }
	            }
			}
        }
        Graph g = new Graph();
        g.nodes = nodes;
        return g;
    }

    public void TestDisplay() {
        root.TestDisplay();
    }
}

public class OctreeNode
{

    public Octree tree;
    public int level;
    public int[] index;
    public OctreeNode parent;
    public OctreeNode[,,] children;
    public bool blocked = false;
    public bool containsBlocked = false;

    public float size {
        get { return tree.size / (1 << level); }
    }
    public Vector3 center {
        get { return corners(0) + (size / 2) * Vector3.one; }
    }
    public Vector3 corners(int n) {
        return tree.corner + size * (new Vector3(index[0] + Octree.cornerDir[n, 0], index[1] + Octree.cornerDir[n, 1], index[2] + Octree.cornerDir[n, 2]));
    }
    public int[] cornerIndex(int n) {
        int s = 1 << (tree.maxLevel - level);
        return new int[] { (index[0] + Octree.cornerDir[n, 0]) * s, (index[1] + Octree.cornerDir[n, 1]) * s, (index[2] + Octree.cornerDir[n, 2]) * s };
    }

    public OctreeNode(int _level, int[] _index, OctreeNode _parent, Octree _tree) {
        level = _level;
        index = _index;
        parent = _parent;
        tree = _tree;
    }

    public virtual void CreateChildren() {
        if (children == null) {
            children = new OctreeNode[2, 2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++) {
                        int[] newIndex = { index[0] * 2 + xi, index[1] * 2 + yi, index[2] * 2 + zi };
                        children[xi, yi, zi] = new OctreeNode(level + 1, newIndex, this, tree);
                    }
        }
    }

    public bool Contains(Vector3 p) {
        Vector3 pp = p - center;
        float r = tree.size / (1 << (level + 1));
        return Mathf.Abs(pp.x) < r && Mathf.Abs(pp.y) < r && Mathf.Abs(pp.z) < r;
    }

    public bool IntersectLine(Vector3 p1, Vector3 p2, float tolerance = 0) {
        Vector3 c = center;
        float r = size / 2 - tolerance;
        p1 -= c;
        p2 -= c;
        float xm, xp, ym, yp, zm, zp;
        xm = Mathf.Min(p1.x, p2.x);
        xp = Mathf.Max(p1.x, p2.x);
        ym = Mathf.Min(p1.y, p2.y);
        yp = Mathf.Max(p1.y, p2.y);
        zm = Mathf.Min(p1.z, p2.z);
        zp = Mathf.Max(p1.z, p2.z);
        if (xm >= r || xp < -r || ym >= r || yp < -r || zm >= r || zp < -r) return false;

        for (int i = 0; i < 3; i++) {
            Vector3 a = Vector3.zero;
            a[i] = 1;
            a = Vector3.Cross(a, p2 - p1);
            float d = Mathf.Abs(Vector3.Dot(p1, a));
            float rr = r * (Mathf.Abs(a[(i + 1) % 3]) + Mathf.Abs(a[(i + 2) % 3]));
            if (d > rr) return false;
        }

        return true;
    }

    public bool IntersectTriangle(Vector3 p1, Vector3 p2, Vector3 p3, float tolerance = 0) {
        Vector3 c = center;
        float r = size / 2 - tolerance;
        p1 -= c;
        p2 -= c;
        p3 -= c;
        float xm, xp, ym, yp, zm, zp;
        xm = Mathf.Min(p1.x, p2.x, p3.x);
        xp = Mathf.Max(p1.x, p2.x, p3.x);
        ym = Mathf.Min(p1.y, p2.y, p3.y);
        yp = Mathf.Max(p1.y, p2.y, p3.y);
        zm = Mathf.Min(p1.z, p2.z, p3.z);
        zp = Mathf.Max(p1.z, p2.z, p3.z);
        if (xm >= r || xp < -r || ym >= r || yp < -r || zm >= r || zp < -r) return false;

        Vector3 n = Vector3.Cross(p2 - p1, p3 - p1);
        float d = Mathf.Abs(Vector3.Dot(p1, n));
        if (d > r * (Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z))) return false;

        Vector3[] p = { p1, p2, p3 };
        Vector3[] f = { p3 - p2, p1 - p3, p2 - p1 };
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                Vector3 a = Vector3.zero;
                a[i] = 1;
                a = Vector3.Cross(a, f[j]);
                float d1 = Vector3.Dot(p[j], a);
                float d2 = Vector3.Dot(p[(j + 1) % 3], a);
                float rr = r * (Mathf.Abs(a[(i + 1) % 3]) + Mathf.Abs(a[(i + 2) % 3]));
                if (Mathf.Min(d1, d2) > rr || Mathf.Max(d1, d2) < -rr) return false;
            }
        }
        return true;
    }

    public void DivideUntilLevel(Vector3 p, int maxLevel, bool markAsBlocked = false) {
        if (Contains(p)) {
            containsBlocked = containsBlocked || markAsBlocked;
            if (level < maxLevel) {
                CreateChildren();
                Vector3 corner = corners(0);
                int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
                int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
                int zi = Mathf.FloorToInt((p.z - corner.z) * 2 / size);
                children[xi, yi, zi].DivideUntilLevel(p, maxLevel, markAsBlocked);
            } else {
                blocked = blocked || markAsBlocked;
            }
        }
    }

    public void DivideTriangleUntilLevel(Vector3 p1, Vector3 p2, Vector3 p3, int maxLevel, bool markAsBlocked = false) {
        if (IntersectTriangle(p1, p2, p3)) {
            containsBlocked = containsBlocked || markAsBlocked;
            if (level < maxLevel) {
                CreateChildren();
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        for (int zi = 0; zi < 2; zi++)
                            children[xi, yi, zi].DivideTriangleUntilLevel(p1, p2, p3, maxLevel, markAsBlocked);
            } else {
                blocked = blocked || markAsBlocked;
            }
        }
    }

    public bool LineOfSight(Vector3 p1, Vector3 p2) {
        if (!containsBlocked) return true;
        if (IntersectLine(p1, p2, tree.cellSize * 0.02f)) {
            if (children != null) {
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        for (int zi = 0; zi < 2; zi++)
                            if (!children[xi, yi, zi].LineOfSight(p1, p2)) return false;
                return true;
            } else {
                return blocked;
            }
        }
        return true;
    }

    private void Leaves(List<OctreeNode> result) {
        if (children != null) {
            for (int xi = 0; xi < 2; xi++) {
                for (int yi = 0; yi < 2; yi++) {
                    for (int zi = 0; zi < 2; zi++) {
                        children[xi, yi, zi].Leaves(result);
                    }
                }
            }
        } else {
            result.Add(this);
        }
    }
    public List<OctreeNode> Leaves() {
        List<OctreeNode> result = new List<OctreeNode>();
        Leaves(result);
        return result;
    }



    GameObject disp;
    public void TestDisplay() {
        if (children != null) {
            if (disp != null) {
                GameObject.Destroy(disp);
                disp = null;
            }
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++)
                        children[xi, yi, zi].TestDisplay();
        } else {
            if (disp == null) {
                disp = GameObject.Instantiate(GameObject.Find("OctreeObj"));
                disp.transform.position = center;
                disp.transform.localScale = Vector3.one * size * 0.9f;
            }
            disp.GetComponent<MeshRenderer>().material.color = blocked ? Color.red : new Color(level * 0.05f, level * 0.05f, level * 0.15f);
        }
    }

}
