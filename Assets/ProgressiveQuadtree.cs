using UnityEngine;
using System.Collections;

public class ProgressiveQuadtree : Quadtree
{
    public ProgressiveQuadtree(float _size, Vector2 _corner, int _maxLevel) : base(_size, _corner, _maxLevel) {
        root = new ProgressiveQuadtreeNode(0, new int[] { 0, 0 }, null, this);
    }
}
public class ProgressiveQuadtreeNode : QuadtreeNode
{
    public ProgressiveQuadtreeNode(int _level, int[] _index, ProgressiveQuadtreeNode _parent, ProgressiveQuadtree _tree) : base(_level, _index, _parent, _tree) {}

    public override void CreateChildren() {
        if (children == null) {
            children = new ProgressiveQuadtreeNode[2, 2];
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++) {
                    int[] newIndex = { index[0] * 2 + xi, index[1] * 2 + yi };
                    children[xi, yi] = new ProgressiveQuadtreeNode(level + 1, newIndex, this, (ProgressiveQuadtree) tree);
                }
            if (level != 0) {
                for (int i = 0; i < 4; i++) {
                    QuadtreeNode found = tree.Find(new int[] {index[0] + Quadtree.dir[i, 0], index[1] + Quadtree.dir[i, 1] }, level);
                    if (found != null && found.level < level) {
                        found.CreateChildren();
                    }
                }
            }

        }
    }
}
