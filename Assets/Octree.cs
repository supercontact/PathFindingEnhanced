using UnityEngine;
using System.Collections;

public class Octree
{

    public int level;
    public Vector3 corner;
    public float size;
    public Octree parent;
    public Octree[,,] children;
    public bool blocked = false;
    public bool containsBlocked = false;

    public static Vector3[] dirs = { Vector3.right, Vector3.up, Vector3.left, Vector3.down, Vector3.forward, Vector3.back };

    public Octree(float _size, Vector3 _corner) {
        level = 0;
        size = _size;
        corner = _corner;
        TestDisplay();
    }
    public Octree(int _level, float _size, Vector3 _corner, Octree _parent) {
        level = _level;
        size = _size;
        corner = _corner;
        parent = _parent;
        TestDisplay();
    }
    public virtual void CreateChildren() {
        if (children == null) {
            children = new Octree[2, 2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++) {
                        children[xi, yi, zi] = new Octree(level + 1, size / 2, corner + xi * size / 2 * Vector3.right + yi * size / 2 * Vector3.up + zi * size / 2 * Vector3.forward, this);
                    }

            CancelDisplay();
        }
    }

    public Octree Find(Vector3 p) {
        if (!Contains(p)) return null;
        if (children != null) {
            int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
            int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
            int zi = Mathf.FloorToInt((p.z - corner.z) * 2 / size);
            return children[xi, yi, zi].Find(p);
        }
        return this;
    }

    public Octree BackwardFind(Vector3 p) {
        if (!Contains(p)) {
            if (level == 0) return null;
            return parent.BackwardFind(p);
        }
        if (children != null) {
            int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
            int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
            int zi = Mathf.FloorToInt((p.z - corner.z) * 2 / size);
            return children[xi, yi, zi].Find(p);
        }
        return this;
    }

    public bool Contains(Vector3 p) {
        return p.x >= corner.x && p.x < corner.x + size && p.y >= corner.y && p.y < corner.y + size && p.z >= corner.z && p.z < corner.z + size;
    }

    public bool IntersectLine(Vector3 p1, Vector3 p2) {
        Vector3 center = corner + Vector3.one * size / 2;
        float r = size / 2;
        p1 -= center;
        p2 -= center;
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

    public bool IntersectTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
        Vector3 center = corner + Vector3.one * size / 2;
        float r = size / 2;
        p1 -= center;
        p2 -= center;
        p3 -= center;
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
                int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
                int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
                int zi = Mathf.FloorToInt((p.z - corner.z) * 2 / size);
                children[xi, yi, zi].DivideUntilLevel(p, maxLevel, markAsBlocked);
            } else {
                blocked = markAsBlocked;
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
                        for (int zi = 0; zi < 2; zi++) {
                            children[xi, yi, zi].DivideTriangleUntilLevel(p1, p2, p3, maxLevel, markAsBlocked);
                        }
            } else {
                blocked = markAsBlocked;
            }
        }
    }

    public bool LineOfSight(Vector2 p1, Vector2 p2) {
        if (!containsBlocked) return true;
        if (IntersectLine(p1, p2)) {
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

    GameObject disp;
    public void TestDisplay() {
        disp = GameObject.Instantiate(GameObject.Find("OctreeObj"));
        disp.transform.position = new Vector3(corner.x + size / 2, corner.y + size / 2, corner.z + size / 2);
        disp.GetComponent<MeshRenderer>().material.color = new Color(level * 0.05f, level * 0.05f, level * 0.15f);
        disp.transform.localScale = Vector3.one * size * 0.9f;
    }
    public void CancelDisplay() {
        GameObject.Destroy(disp);
        disp = null;
    }

}
