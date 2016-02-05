using UnityEngine;
using System.Collections;

public class ProgressiveOctree : Octree
{
    public ProgressiveOctree(float _size, Vector3 _corner, int _maxLevel) : base(_size, _corner, _maxLevel) {
        root = new ProgressiveOctreeNode(0, new int[] { 0, 0, 0 }, null, this);
    }
}
public class ProgressiveOctreeNode : OctreeNode
{
    public ProgressiveOctreeNode(int _level, int[] _index, ProgressiveOctreeNode _parent, ProgressiveOctree _tree) : base(_level, _index, _parent, _tree) { }

    public override void CreateChildren() {
        if (children == null) {
            children = new ProgressiveOctreeNode[2, 2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++) {
                        int[] newIndex = { index[0] * 2 + xi, index[1] * 2 + yi, index[2] * 2 + zi };
                        children[xi, yi, zi] = new ProgressiveOctreeNode(level + 1, newIndex, this, (ProgressiveOctree)tree);
                    }
            if (level != 0) {
                for (int i = 0; i < 6; i++) {
                    OctreeNode found = tree.Find(new int[] { index[0] + Octree.dir[i, 0], index[1] + Octree.dir[i, 1], index[2] + Octree.dir[i, 2] }, level);
                    if (found != null && found.level < level) {
                        found.CreateChildren();
                    }
                }
            }

        }
    }
}
