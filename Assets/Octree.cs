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

    public void BuildFromMeshes(Mesh[] meshes, float normalExpansion = 0) {
        for (int i = 0; i < meshes.Length; i++) {
            int[] triangles = meshes[i].triangles;
            Vector3[] verts = meshes[i].vertices;
            Vector3[] vertsNormal = meshes[i].normals;
            for (int j = 0; j < triangles.Length / 3; j++) {
                DivideTriangle(
                    verts[triangles[3 * j]] + vertsNormal[triangles[3 * j]] * normalExpansion,
                    verts[triangles[3 * j + 1]] + vertsNormal[triangles[3 * j + 1]] * normalExpansion,
                    verts[triangles[3 * j + 2]] + vertsNormal[triangles[3 * j + 2]] * normalExpansion, true);
            }
        }
    }
    public void BuildFromGameObject(GameObject gameObject, float normalExpansion = 0, bool recursive = true) {
        if (gameObject.GetComponent<MeshFilter>() != null) {
            Mesh mesh = Object.Instantiate(gameObject.GetComponent<MeshFilter>().mesh);
            if (mesh != null) {
                MeshFactory.MergeOverlappingPoints(mesh);
                mesh.RecalculateNormals();

                int[] triangles = mesh.triangles;
                Vector3[] vertsOld = mesh.vertices;
                Vector3[] vertsNormal = mesh.normals;
                Vector3[] verts = new Vector3[vertsOld.Length];
                for (int j = 0; j < verts.Length; j++) {
                    verts[j] = gameObject.transform.TransformPoint(vertsOld[j]) + gameObject.transform.TransformDirection(vertsNormal[j]) * normalExpansion;
                }
                for (int j = 0; j < triangles.Length / 3; j++) {
                    DivideTriangle(verts[triangles[3 * j]], verts[triangles[3 * j + 1]], verts[triangles[3 * j + 2]], true);
                }
            }
        }
        if (recursive) {
            for (int i = 0; i < gameObject.transform.childCount; i++) {
                if (gameObject.transform.GetChild(i).gameObject.activeInHierarchy) {
                    BuildFromGameObject(gameObject.transform.GetChild(i).gameObject, normalExpansion);
                }
            }
        }
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
    public bool IsBlocked(int[] gridIndex, bool outsideIsBlocked = false, bool doublePrecision = false) {
        int xi = gridIndex[0];
        int yi = gridIndex[1];
        int zi = gridIndex[2];
        if (doublePrecision) {
            xi /= 2;
            yi /= 2;
            zi /= 2;
        }
        int t = 1 << maxLevel;
        if (xi >= t || xi < 0 || yi >= t || yi < 0 || zi >= t || zi < 0) return outsideIsBlocked;
        OctreeNode current = root;
        for (int l = 0; l < maxLevel; l++) {
            t >>= 1;
            if (!current.containsBlocked) return false;
            if (current.children == null) return current.blocked;
            current = current.children[xi / t, yi / t, zi / t];
            xi %= t;
            yi %= t;
            zi %= t;
        }
        return current.blocked;
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

    public bool LineOfSight(Vector3 p1, Vector3 p2, bool outsideIsBlocked = false, bool doublePrecision = false) {
        Vector3 p1g = (p1 - corner) / cellSize;
        Vector3 p2g = (p2 - corner) / cellSize;
        if (doublePrecision) {
            p1g *= 2;
            p2g *= 2;
        }
        int[,] p = new int[2,3];
        int[] d = new int[3];
        int[] sign = new int[3];
        int[] f = new int[2];

        for (int i = 0; i < 3; i++) {
            //FloorToIntSnap(p1g[i], out p[0, i]);
            //FloorToIntSnap(p2g[i], out p[1, i]);
            p[0, i] = Mathf.RoundToInt(p1g[i]);
            p[1, i] = Mathf.RoundToInt(p2g[i]);
            d[i] = p[1, i] - p[0, i];
            if (d[i] < 0) {
                d[i] = -d[i];
                sign[i] = -1;
            } else {
                sign[i] = 1;
            }
        }
        int[] pBlock = { p[0, 0] + (sign[0] - 1) / 2, p[0, 1] + (sign[1] - 1) / 2, p[0, 2] + (sign[2] - 1) / 2 };

        int longAxis;
        if (d[0] >= d[1] && d[0] >= d[2]) longAxis = 0;
        else if (d[1] >= d[2]) longAxis = 1;
        else longAxis = 2;
        if (d[longAxis] == 0) return true;
        int axis0 = (longAxis + 1) % 3;
        int axis1 = (longAxis + 2) % 3;

        while (p[0, longAxis] != p[1, longAxis]) {
            f[0] += d[axis0];
            f[1] += d[axis1];
            if (f[0] >= d[longAxis] && f[1] < d[longAxis]) {
                f[0] -= d[longAxis];
                if (d[axis1] != 0) {
                    if (IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) return false;
                } else {
                    bool sight = false;
                    pBlock[axis1] -= 1;
                    if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                    pBlock[axis1] += 1;
                    if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                    if (!sight) return false;
                }
                p[0, axis0] += sign[axis0];
                pBlock[axis0] += sign[axis0];
            } else if (f[1] >= d[longAxis] && f[0] < d[longAxis]) {
                f[1] -= d[longAxis];
                if (d[axis0] != 0) {
                    if (IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) return false;
                } else {
                    bool sight = false;
                    pBlock[axis0] -= 1;
                    if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                    pBlock[axis0] += 1;
                    if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                    if (!sight) return false;
                }
                p[0, axis1] += sign[axis1];
                pBlock[axis1] += sign[axis1];
            } else if (f[0] >= d[longAxis] && f[1] >= d[longAxis]) {
                f[0] -= d[longAxis];
                f[1] -= d[longAxis];
                if (IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) return false;
                int det = f[0] * d[axis1] - f[1] * d[axis0];
                if (det > 0) {
                    pBlock[axis0] += sign[axis0];
                    if (IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) return false;
                    pBlock[axis1] += sign[axis1];
                } else if (det < 0) {
                    pBlock[axis1] += sign[axis1];
                    if (IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) return false;
                    pBlock[axis0] += sign[axis0];
                } else {
                    pBlock[axis0] += sign[axis0];
                    pBlock[axis1] += sign[axis1];
                }
                p[0, axis0] += sign[axis0];
                p[0, axis1] += sign[axis1];
            }

            if (f[0] != 0 && f[1] != 0 && IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) return false;
            if (d[axis0] == 0 && d[axis1] != 0) {
                bool sight = false;
                pBlock[axis0] -= 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                pBlock[axis0] += 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                if (!sight) return false;
            } else if (d[axis0] != 0 && d[axis1] == 0) {
                bool sight = false;
                pBlock[axis1] -= 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                pBlock[axis1] += 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                if (!sight) return false;
            } else if (d[axis0] == 0 && d[axis1] == 0) {
                bool sight = false;
                pBlock[axis0] -= 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                pBlock[axis1] -= 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                pBlock[axis0] += 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                pBlock[axis1] += 1;
                if (!IsBlocked(pBlock, outsideIsBlocked, doublePrecision)) sight = true;
                if (!sight) return false;
            }
            p[0, longAxis] += sign[longAxis];
            pBlock[longAxis] += sign[longAxis];
        }
        return true;
    }

    private bool FloorToIntSnap(float n, out int i, float epsilon = 0.001f) {
        i = Mathf.FloorToInt(n + epsilon);
        return Mathf.Abs(n - i) <= epsilon;
    }

    public Graph centerGraph;
    public Dictionary<OctreeNode, Node> centerGraphDictionary;
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
                    OctreeNode found = Find(new int[] { q.index[0] + dir[i, 0], q.index[1] + dir[i, 1], q.index[2] + dir[i, 2] }, q.level);
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
        g.CalculateConnectivity();
        g.type = Graph.GraphType.CENTER;
        centerGraph = g;
        centerGraphDictionary = dict;
        return g;
    }

    public Graph cornerGraph;
    public Dictionary<long, Node> cornerGraphDictionary;
    public Graph ToCornerGraph() {
        List<OctreeNode> leaves = root.Leaves();
        Dictionary<long, Node> dict = new Dictionary<long, Node>();
		Dictionary<long, bool> arcAdded = new Dictionary<long, bool>();
        List<Node> nodes = new List<Node>();

        foreach (OctreeNode o in leaves) {
            for (int i = 0; i < 6; i++) {
                OctreeNode found = Find(new int[] { o.index[0] + dir[i, 0], o.index[1] + dir[i, 1], o.index[2] + dir[i, 2] }, o.level);
                if ((!o.blocked && (found == null || found.blocked || found.children == null)) || (found != null && found.level < o.level)) {
                    int k = i / 2;
                    int[][] c = new int[4][];
                    int counter = 0;
                    for (int t1 = -1; t1 <= 1; t1 += 2) {
                        for (int t2 = -1; t2 <= 1; t2 += 2) {
                            c[counter] = new int[3];
                            c[counter][k] = dir[i, k];
                            c[counter][(k + 1) % 3] = t1;
                            c[counter][(k + 2) % 3] = t2 * t1;
                            for (int j = 0; j < 3; j++) {
                                c[counter][j] += o.index[j] * 2 + 1;
                                c[counter][j] /= 2;
                                c[counter][j] <<= (maxLevel - o.level);
                            }
                            counter++;
                        }
                    }
                    for (int t = 0; t < 4; t++) {
                        long arcKey = GetArcKey(c[t], c[(t + 1) % 4]);
                        bool temp;
                        if (!arcAdded.TryGetValue(arcKey, out temp)) {
                            arcAdded[arcKey] = true;
                            Node n1 = GetNodeFromDict(c[t], dict, nodes);
                            Node n2 = GetNodeFromDict(c[(t + 1) % 4], dict, nodes);
                            n1.arcs.Add(new Arc(n1, n2));
                            n2.arcs.Add(new Arc(n2, n1));
                        }
                    }
                }
            }
        }
        Graph g = new Graph();
        g.nodes = nodes;
        g.CalculateConnectivity();
        g.type = Graph.GraphType.CORNER;
        cornerGraph = g;
        cornerGraphDictionary = dict;
        return g;
    }

    private Node GetNodeFromDict(int[] index, Dictionary<long, Node> dict, List<Node> nodes = null) {
        long rowCount = 1 << maxLevel + 1;
        long key = (index[0] * rowCount + index[1]) * rowCount + index[2];
        Node result = null;
        if (!dict.TryGetValue(key, out result) && nodes != null) {
            result = new Node(IndexToPosition(index), nodes.Count);
            dict.Add(key, result);
            nodes.Add(result);
        }
        return result;
    }
    private long GetArcKey(int[] index1, int[] index2) {
        long rowCount = 1 << (maxLevel + 1) + 1;
        return ((index1[0] + index2[0]) * rowCount + index1[1] + index2[1]) * rowCount + index1[2] + index2[2];
    }

    public List<Node> FindCorrespondingCenterGraphNode(Vector3 position) {
        List<Node> result = new List<Node>();
        OctreeNode node = Find(position);
        if (node != null && centerGraphDictionary.ContainsKey(node)) {
            result.Add(centerGraphDictionary[node]);
        }
        return result;
    }

    public List<Node> FindBoundingCornerGraphNodes(Vector3 position) {
        List<Node> result = new List<Node>();
        OctreeNode node = Find(position);
        if (node != null) {
            for (int i = 0; i < 8; i++) {
                int[] cornerIndex = new int[3];
                for (int j = 0; j < 3; j++) {
                    cornerIndex[j] = (node.index[j] + cornerDir[i, j]) << (maxLevel - node.level);
                }
                result.Add(GetNodeFromDict(cornerIndex, cornerGraphDictionary));
                if (GetNodeFromDict(cornerIndex, cornerGraphDictionary) == null) {
                    Debug.Log(position + " " + cornerIndex[0] + " " + cornerIndex[1] + " " + cornerIndex[2]);
                }
            }
        }
        return result;
    }

    public void DisplayVoxels(int maxLevel = -1, bool blockedOnly = true) {
        root.DisplayVoxels(maxLevel, blockedOnly);
    }
    public void ClearDisplay() {
        root.ClearDisplay();
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
        /*if (level == 1) {
            Debug.Log(p1 + " " + p2 + " " + r);
            throw new System.Exception();
        }*/
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

        //if (level >= 1) throw new System.Exception();
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

    public bool IntersectSphere(Vector3 sphereCenter, float radius)
    {
        Vector3 c1 = corners(0);
        Vector3 c2 = corners(7);
        float r2 = radius * radius;
        for (int i = 0; i < 3; i++)
        {
            if (sphereCenter[i] < c1[i]) r2 -= (sphereCenter[i] - c1[i]) * (sphereCenter[i] - c1[i]);
            else if (sphereCenter[i] > c2[i]) r2 -= (sphereCenter[i] - c2[i]) * (sphereCenter[i] - c2[i]);
        }
        return r2 > 0;
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

    public void DivideSphereUntilLevel(Vector3 sphereCenter, float radius, int maxLevel, bool markAsBlocked = false)
    {
        if (IntersectSphere(sphereCenter, radius))
        {
            containsBlocked = containsBlocked || markAsBlocked;
            if (level < maxLevel)
            {
                CreateChildren();
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        for (int zi = 0; zi < 2; zi++)
                            children[xi, yi, zi].DivideSphereUntilLevel(sphereCenter, radius, maxLevel, markAsBlocked);
            }
            else {
                blocked = blocked || markAsBlocked;
            }
        }
    }

    /*public bool LineOfSight(Vector3 p1, Vector3 p2) {
        if (!containsBlocked) return true;
        if (IntersectLine(p1, p2)) {
            if (children != null) {
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        for (int zi = 0; zi < 2; zi++)
                            if (!children[xi, yi, zi].LineOfSight(p1, p2)) return false;
                return true;
            } else {
                return !blocked;
            }
        }
        return true;
    }*/

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
    public void DisplayVoxels(int maxLevel = -1, bool blockedOnly = true) {
        if (children != null && (maxLevel == -1 || level < maxLevel)) {
            if (disp != null) {
                GameObject.Destroy(disp);
                disp = null;
            }
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++)
                        children[xi, yi, zi].DisplayVoxels(maxLevel, blockedOnly);
        } else if (containsBlocked || !blockedOnly) {
            if (disp == null) {
                disp = GameObject.Instantiate(GameObject.Find("OctreeObj"));
                disp.transform.position = center;
                disp.transform.localScale = Vector3.one * size * 0.9f;
            }
            disp.GetComponent<MeshRenderer>().material.color = containsBlocked ? Color.red : new Color(level * 0.05f, level * 0.05f, level * 0.15f);
        }
    }
    public void ClearDisplay() {
        if (disp != null) {
            GameObject.Destroy(disp);
            disp = null;
        }
        if (children != null) {
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++)
                        children[xi, yi, zi].ClearDisplay();
        }
    }
}
