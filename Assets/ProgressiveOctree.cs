using UnityEngine;
using System.Collections;

public class ProgressiveOctree : Octree
{

    public ProgressiveOctree(float _size, Vector3 _corner) : base(_size, _corner) {

    }
    public ProgressiveOctree(int _level, float _size, Vector3 _corner, ProgressiveOctree _parent) : base(_level, _size, _corner, _parent) {

    }
    public override void CreateChildren() {
        if (children == null) {
            children = new ProgressiveOctree[2, 2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++) {
                        children[xi, yi, zi] = new ProgressiveOctree(level + 1, size / 2, corner + xi * size / 2 * Vector3.right + yi * size / 2 * Vector3.up + zi * size / 2 * Vector3.forward, this);
                    }
            
            if (level != 0) {
                Vector3 center = corner + Vector3.one * size / 2;
                for (int i = 0; i < 6; i++) {
                    Octree found = parent.BackwardFind(center + dirs[i] * size);
                    if (found != null && found.level < level) {
                        found.CreateChildren();
                    }
                }
            }

            CancelDisplay();
        }
    }
}
