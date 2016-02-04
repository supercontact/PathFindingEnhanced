using UnityEngine;
using System.Collections;

public class ProgressiveQuadtree : Quadtree
{

    public ProgressiveQuadtree(float _size, Vector2 _corner) : base(_size, _corner)  {

    }
    public ProgressiveQuadtree(int _level, float _size, Vector2 _corner, ProgressiveQuadtree _parent) : base(_level, _size, _corner, _parent) {

    }
    public override void CreateChildren() {
        if (children == null) {
            children = new ProgressiveQuadtree[2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++) {
                    children[xi, yi] = new ProgressiveQuadtree(level + 1, size / 2, corner + xi * size / 2 * Vector2.right + yi * size / 2 * Vector2.up, this);
                }
            if (level != 0) {
                Vector2 center = corner + Vector2.one * size / 2;
                for (int i = 0; i < 4; i++) {
                    Quadtree found = parent.BackwardFind(center + dirs[i] * size);
                    if (found != null && found.level < level) {
                        found.CreateChildren();
                    }
                }
            }

        }
    }
}
