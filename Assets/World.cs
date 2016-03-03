using UnityEngine;
using System.Collections;

public class World {

    public Octree space;
    public Graph spaceGraph;

    public World(GameObject scene, float size, Vector3 center, int maxLevel, float normalExtension, bool progressive = true, bool cornerGraph = true) {
        space = progressive ? new ProgressiveOctree(size, center - Vector3.one * size / 2, maxLevel) : new Octree(size, center - Vector3.one * size / 2, maxLevel);
        space.BuildFromGameObject(scene, normalExtension);
        spaceGraph = cornerGraph ? space.ToCornerGraph() : space.ToCenterGraph();
    }

    public World(Octree space, bool cornerGraph = true) {
        this.space = space;
        spaceGraph = cornerGraph ? space.ToCornerGraph() : space.ToCenterGraph();
    }

    public void DisplayVoxels() {
        space.DisplayVoxels();
    }
}
