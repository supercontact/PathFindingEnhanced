using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Quadtree
{
    public int maxLevel;
    public Vector2 corner;
    public float size;
    public float cellSize {
        get { return size / (1 << maxLevel); }
    }
    public QuadtreeNode root;

    public static int[,] dir = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };
    public static int[,] cornerDir = { { 0, 0 }, { 1, 0 }, { 1, 1 }, { 0, 1 } };

    public Quadtree(float _size, Vector2 _corner, int _maxLevel) {
        size = _size;
        corner = _corner;
        maxLevel = _maxLevel;
        root = new QuadtreeNode(0, new int[]{0,0}, null, this);
    }

    public int[] PositionToIndex(Vector2 p) {
        p -= corner;
        float d = cellSize;
        return new int[] { Mathf.FloorToInt(p.x / d), Mathf.FloorToInt(p.y / d) };
    }
    public Vector2 IndexToPosition(int[] gridIndex) {
        float d = cellSize;
        return new Vector2(gridIndex[0] * d, gridIndex[1] * d) + corner;
    }

    public QuadtreeNode Find(int[] gridIndex) {
        return Find(gridIndex, maxLevel);
    }
    public QuadtreeNode Find(int[] gridIndex, int level) {
        int xi = gridIndex[0];
        int yi = gridIndex[1];
        int t = 1 << level;
        if (xi >= t || xi < 0 || yi >= t || yi < 0) return null;
        QuadtreeNode current = root;
        for (int l = 0; l < level; l++) {
            t >>= 1;
            if (current.children == null) return current;
            current = current.children[xi / t, yi / t];
            xi %= t;
            yi %= t;
        }
        return current;
    }
    public QuadtreeNode Find(Vector2 p) {
        return Find(PositionToIndex(p));
    }

    public void Divide(Vector2 p, bool markAsBlocked = false) {
        int[] gridIndex = PositionToIndex(p);
        int xi = gridIndex[0];
        int yi = gridIndex[1];
        int t = 1 << maxLevel;
        if (xi >= t || xi < 0 || yi >= t || yi < 0) return;
        QuadtreeNode current = root;
        for (int l = 0; l < maxLevel; l++) {
            t >>= 1;
            current.containsBlocked =  current.containsBlocked || markAsBlocked;
            if (current.children == null) current.CreateChildren();
            current = current.children[xi / t, yi / t];
            xi %= t;
            yi %= t;
        }
        current.blocked = current.blocked || markAsBlocked;
        current.containsBlocked = current.blocked;
    }

    public void DivideLine(Vector2 p1, Vector2 p2, bool markAsBlocked = false) {
        root.DivideLineUntilLevel(p1, p2, maxLevel, markAsBlocked);
    }

    public Graph ToCenterGraph() {
        List<QuadtreeNode> leaves = root.Leaves();
        Dictionary<QuadtreeNode, Node> dict = new Dictionary<QuadtreeNode, Node>();
        int count = 0;
        List<Node> nodes = new List<Node>();
        foreach (QuadtreeNode q in leaves) {
            if (!q.blocked) {
                Node n = new Node(q.center, count);
                dict.Add(q, n);
                nodes.Add(n);
                count++;
            }
        }
        foreach (QuadtreeNode q in leaves) {
            if (!q.blocked) {
                if (q.level == 0) continue;
                Node n = dict[q];
                for (int i = 0; i < 4; i++) {
                    QuadtreeNode found = Find(new int[] { q.index[0] + dir[i, 0], q.index[1] + dir[i, 1] }, q.level);
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
        List<QuadtreeNode> leaves = root.Leaves();
        Dictionary<int, Node> dict = new Dictionary<int, Node>();
        List<Node> nodes = new List<Node>();

		foreach (QuadtreeNode q in leaves) {
	        for (int i = 0; i < 4; i++) {
	            QuadtreeNode found = Find(new int[] { q.index[0] + dir[i, 0], q.index[1] + dir[i, 1] }, q.level);
				if ((!q.blocked && (found == null || found.blocked)) || (found != null && found.level < q.level)) {
                    Node n1 = GetNodeFromDict(q.cornerGlobalIndex((i + 1) % 4), dict, nodes);
                    Node n2 = GetNodeFromDict(q.cornerGlobalIndex((i + 2) % 4), dict, nodes);
                    n1.arcs.Add(new Arc(n1, n2));
                    n2.arcs.Add(new Arc(n2, n1));
	            } else if (!q.blocked && found.children == null) {
                    Node n1 = GetNodeFromDict(q.cornerGlobalIndex((i + 1) % 4), dict, nodes);
                    Node n2 = GetNodeFromDict(q.cornerGlobalIndex((i + 2) % 4), dict, nodes);
                    n1.arcs.Add(new Arc(n1, n2));
	            }
	        }

        }
        Graph g = new Graph();
        g.nodes = nodes;
        return g;
    }

    private Node GetNodeFromDict(int[] index, Dictionary<int, Node> dict, List<Node> nodes) {
        int rowCount = 1 << maxLevel + 1;
        int key = index[0] * rowCount + index[1];
        Node result;
        if (!dict.TryGetValue(key, out result)) {
            result = new Node(IndexToPosition(index), nodes.Count);
            dict.Add(key, result);
            nodes.Add(result);
        }
        return result;
    }

    public void TestDisplay() {
        root.TestDisplay();
    }
}

public class QuadtreeNode {

    public Quadtree tree;
    public int level;
    public int[] index;
    public QuadtreeNode parent;
    public QuadtreeNode[,] children;
    public bool blocked = false;
    public bool containsBlocked = false;

    public float size {
        get { return tree.size / (1 << level); }
    }
    public Vector2 center {
        get { return corners(0) + (size / 2) * Vector2.one; }
    }
    public Vector2 corners(int n) {
        return tree.corner + size * (new Vector2(index[0] + Quadtree.cornerDir[n, 0], index[1] + Quadtree.cornerDir[n, 1]));
    }
    public int[] cornerGlobalIndex(int n) {
        int s = 1 << (tree.maxLevel - level);
        return new int[] { (index[0] + Quadtree.cornerDir[n, 0]) * s, (index[1] + Quadtree.cornerDir[n, 1]) * s };
    }

    public QuadtreeNode(int _level, int[] _index, QuadtreeNode _parent, Quadtree _tree) {
        level = _level;
        index = _index;
        parent = _parent;
        tree = _tree;
    }

    public virtual void CreateChildren() {
        if (children == null) {
            children = new QuadtreeNode[2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++) {
                    int[] newIndex = { index[0] * 2 + xi, index[1] * 2 + yi };
                    children[xi, yi] = new QuadtreeNode(level + 1, newIndex, this, tree);
                }
        }
    }

    public bool Contains(Vector2 p) {
        Vector2 pp = p - center;
        float r = tree.size / (1 << (level + 1));
        return Mathf.Abs(pp.x) < r && Mathf.Abs(pp.y) < r;
    }

    public bool IntersectLine(Vector2 p1, Vector2 p2, float tolerance = 0) {
        Vector2 c = center;
        float r = size / 2 - tolerance;
        p1 -= c;
        p2 -= c;
        float xm, xp, ym, yp;
        xm = Mathf.Min(p1.x, p2.x);
        xp = Mathf.Max(p1.x, p2.x);
        ym = Mathf.Min(p1.y, p2.y);
        yp = Mathf.Max(p1.y, p2.y);
        if (xm >= r || xp < -r || ym >= r || yp < -r) return false;

        Vector2 v = p2 - p1;
        Vector2 n = new Vector2(v.y, -v.x);
        float d = Mathf.Abs(Vector3.Dot(p1, n));
        if (d > r * (Mathf.Abs(n.x) + Mathf.Abs(n.y))) return false;

        return true;
    }

    public void DivideUntilLevel(Vector2 p, int maxLevel, bool markAsBlocked = false) {
        if (Contains(p)) {
            containsBlocked = containsBlocked || markAsBlocked;
            if (level < maxLevel) {
                CreateChildren();
                Vector2 corner = corners(0);
                int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
                int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
                children[xi, yi].DivideUntilLevel(p, maxLevel, markAsBlocked);
            } else {
                blocked = blocked || markAsBlocked;
            }
        }
    }

    public void DivideLineUntilLevel(Vector2 p1, Vector2 p2, int maxLevel, bool markAsBlocked = false) {
        if (IntersectLine(p1, p2)) {
            containsBlocked = containsBlocked || markAsBlocked;
            if (level < maxLevel) {
                CreateChildren();
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        children[xi, yi].DivideLineUntilLevel(p1, p2, maxLevel, markAsBlocked);
            } else {
                blocked = blocked || markAsBlocked;
            }
        }
    }

    public bool LineOfSight(Vector2 p1, Vector2 p2) {
        if (!containsBlocked) return true;
        if (IntersectLine(p1, p2, tree.cellSize * 0.02f)) {
            if (children != null) {
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        if (!children[xi, yi].LineOfSight(p1, p2)) return false;
                return true;
            } else {
                return blocked;
            }
        }
        return true;
    }

    private void Leaves(List<QuadtreeNode> result) {
        if (children != null) {
            for (int xi = 0; xi < 2; xi++) {
                for (int yi = 0; yi < 2; yi++) {
                    children[xi, yi].Leaves(result);
                }
            }
        } else {
            result.Add(this);
        }
    }
    public List<QuadtreeNode> Leaves() {
        List<QuadtreeNode> result = new List<QuadtreeNode>();
        Leaves(result);
        return result;
    }


    GameObject disp;
    public void TestDisplay() {
        if (children != null) {
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    children[xi, yi].TestDisplay();
        }
        if (disp == null) {
            disp = GameObject.Instantiate(GameObject.Find("QuadtreeObj"));
            Vector2 c = center;
            disp.transform.position = new Vector3(c.x, c.y, 1 - level * 0.1f);
            disp.transform.localScale = Vector3.one * size * 0.95f / 10;
        }
        disp.GetComponent<MeshRenderer>().material.color = blocked ? Color.red : new Color(level * 0.05f, level * 0.05f, level * 0.15f);
    }
    public void CancelDisplay() {
        GameObject.Destroy(disp);
        disp = null;
    }

}
