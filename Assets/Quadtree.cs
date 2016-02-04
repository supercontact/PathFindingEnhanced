using UnityEngine;
using System.Collections;

public class Quadtree {

    public int level;
    public Vector2 corner;
    public float size;
    public Quadtree parent;
    public Quadtree[,] children;
    public bool blocked = false;

    public static Vector2[] dirs = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

    public Quadtree(float _size, Vector2 _corner) {
        level = 0;
        size = _size;
        corner = _corner;
        TestDisplay();
    }
    public Quadtree(int _level, float _size, Vector2 _corner, Quadtree _parent) {
        level = _level;
        size = _size;
        corner = _corner;
        parent = _parent;
        TestDisplay();
    }
    public virtual void CreateChildren() {
        if (children == null) {
            children = new Quadtree[2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++) {
                    children[xi, yi] = new Quadtree(level + 1, size / 2, corner + xi * size / 2 * Vector2.right + yi * size / 2 * Vector2.up, this);
                }
        }
    }

    public Quadtree Find(Vector2 p) {
        if (!Contains(p)) return null;
        if (children != null) {
            int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
            int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
            return children[xi, yi].Find(p);
        }
        return this;
    }

    public Quadtree BackwardFind(Vector2 p) {
        if (!Contains(p)) {
            if (level == 0) return null;
            return parent.BackwardFind(p);
        }
        if (children != null) {
            int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
            int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
            return children[xi, yi].Find(p);
        }
        return this;
    }

    public bool Contains(Vector2 p) {
        return p.x >= corner.x && p.x < corner.x + size && p.y >= corner.y && p.y < corner.y + size;
    }

    public bool IntersectLine(Vector2 p1, Vector2 p2) {
        bool sign = true;
        float xp, xm, yp, ym;
        if (p1.x < p2.x) {
            xm = p1.x;
            xp = p2.x;
        } else {
            xm = p2.x;
            xp = p1.x;
            sign = !sign;
        }
        if (p1.y < p2.y) {
            ym = p1.y;
            yp = p2.y;
        } else {
            ym = p2.y;
            yp = p1.y;
            sign = !sign;
        }
        if (xm < corner.x + size && xp >= corner.x && ym < corner.y + size && yp >= corner.y) {
            Vector2 v = p2 - p1;
            if (sign) {
                return U.Det(corner + Vector2.up * size - p1, v) * U.Det(corner + Vector2.right * size - p1, v) < 0;
            } else {
                return U.Det(corner - p1, v) * U.Det(corner + Vector2.one * size - p1, v) < 0;
            }
        }
        return false;
    }

    public void DivideUntilLevel(Vector2 p, int maxLevel, bool markAsBlocked = false) {
        if (Contains(p)) {
            if (level < maxLevel) {
                CreateChildren();
                int xi = Mathf.FloorToInt((p.x - corner.x) * 2 / size);
                int yi = Mathf.FloorToInt((p.y - corner.y) * 2 / size);
                children[xi, yi].DivideUntilLevel(p, maxLevel);
            } else {
                blocked = markAsBlocked;
            }
        }
    }

    public void DivideLineUntilLevel(Vector2 p1, Vector2 p2, int maxLevel, bool markAsBlocked = false) {
        if (IntersectLine(p1, p2)) {
            if (level < maxLevel) {
                CreateChildren();
                for (int xi = 0; xi < 2; xi++)
                    for (int yi = 0; yi < 2; yi++)
                        children[xi, yi].DivideLineUntilLevel(p1, p2, maxLevel);
            } else {
                blocked = markAsBlocked;
            }
        }
    }

    GameObject disp;
    public void TestDisplay() {
        disp = GameObject.Instantiate(GameObject.Find("QuadtreeObj"));
        disp.transform.position = new Vector3(corner.x + size / 2, -1 + level * 0.1f, corner.y + size / 2);
        disp.GetComponent<MeshRenderer>().material.color = new Color(level * 0.05f, level * 0.05f, level * 0.15f);
        disp.transform.localScale = Vector3.one * size * 0.95f / 10;
    }
    public void CancelDisplay() {
        GameObject.Destroy(disp);
        disp = null;
    }

}
