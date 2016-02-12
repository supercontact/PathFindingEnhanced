using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using C5;

public class Node {
	public int index;
	public List<Arc> arcs;
	public Vector3 center;
    public Node(Vector3 _center, int _index) {
        center = _center;
        index = _index;
        arcs = new List<Arc>();
    }
}

public class Arc
{
    public Node from, to;
    public float distance;
    public Arc(Node _from, Node _to) {
        from = _from;
        to = _to;
        distance = (from.center - to.center).magnitude;
    }
    public Arc(Node _from, Node _to, float _distance) {
        from = _from;
        to = _to;
        distance = _distance;
    }
}

public class AstarNodeInfo : Node {
	public float f, g, h;
	public int indexTemp;
	public AstarNodeInfo parent;
	public bool open = false;
	public bool closed = false;
	public IPriorityQueueHandle<AstarNodeInfo> handle;
	public AstarNodeInfo(Node node) : base(node.center, node.index) {
		arcs = node.arcs;
	}
}

public class NodeFComparer : IComparer<AstarNodeInfo> {
	public int Compare (AstarNodeInfo x, AstarNodeInfo y)
	{
		return x.f - y.f > 0 ? 1 : (x.f - y.f < 0 ? -1 : 0);
	}
}

public class Graph {
	public List<Node> nodes;

    public Graph() {
        nodes = new List<Node>();
    }
	public int addNode(Vector3 center) {
		nodes.Add(new Node(center, nodes.Count));
        return nodes.Count - 1;
    }

	public void addArc(int fromIndex, int toIndex) {
		nodes[fromIndex].arcs.Add(new Arc(nodes[fromIndex], nodes[toIndex]));
	}

	public delegate float H(Node from, Node to);
	public float estimatedCost(Node from, Node to) {
		return (from.center - to.center).magnitude;
	}

	public List<Node> backtrack(AstarNodeInfo node) {
		List<Node> temp =  new List<Node>();
        temp.Add(node);
		while(node.parent != null) {
			temp.Add(node.parent);
			node = node.parent;
		}
		int n = temp.Count;
		List<Node> result =  new List<Node>();
		for (int i = 0; i < n; i++) {
			result.Add(temp[n - 1 - i]);
		}
		return result;
	}

	public List<Node> AStar(Node source, Node destination, H h = null) {
		Dictionary<int, AstarNodeInfo> infoTable = new Dictionary<int, AstarNodeInfo>();
		
		if (h == null)
			h = estimatedCost;
		AstarNodeInfo sourceInfo = new AstarNodeInfo(source);
		infoTable[sourceInfo.index] = sourceInfo;
		
		IntervalHeap<AstarNodeInfo> open = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
		sourceInfo.open = true;
        sourceInfo.g = 0;
        sourceInfo.f = h(source, destination);
        open.Add(ref sourceInfo.handle, sourceInfo);
        AstarNodeInfo current = null;
		while(open.Count > 0) {
			current = open.DeleteMin();
            current.open = false;
            current.closed = true;
            if (current.index == destination.index) break;
			foreach (Arc a in current.arcs) {
				AstarNodeInfo successor;
				if (!infoTable.TryGetValue(a.to.index, out successor)) {
					successor = new AstarNodeInfo(a.to);
                    successor.g = float.MaxValue;
                    successor.h = h(successor, destination);
                    infoTable[a.to.index] = successor;
				}
                if (!successor.closed) {
                    float g_old = successor.g;
                    // ComputeCost
                    if (successor.g > current.g + a.distance) {
                        successor.parent = current;
                        successor.g = current.g + a.distance;
                        successor.f = successor.g + successor.h;
                    } //
                    if (successor.g < g_old) {
                        if (successor.open)
                            open.Delete(successor.handle);
                        open.Add(ref successor.handle, successor);
                        successor.open = true;
                    }
                }
			}
		}
		if (current.index != destination.index) return null;
		return backtrack(current);
	}

    public List<Node> ThetaStar(Node source, Node destination, Octree space, H h = null) {
        float t = Time.realtimeSinceStartup;
        Dictionary<int, AstarNodeInfo> infoTable = new Dictionary<int, AstarNodeInfo>();

        if (h == null)
            h = estimatedCost;
        AstarNodeInfo sourceInfo = new AstarNodeInfo(source);
        infoTable[sourceInfo.index] = sourceInfo;

        IntervalHeap<AstarNodeInfo> open = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
        sourceInfo.open = true;
        sourceInfo.g = 0;
        sourceInfo.f = h(source, destination);
        open.Add(ref sourceInfo.handle, sourceInfo);
        AstarNodeInfo current = null;
        int nodeCount = 0;
        int newNodeCount = 0;
        while (open.Count > 0) {
            nodeCount++;
            current = open.DeleteMin();
            //Debug.Log(current.f);
            current.open = false;
            current.closed = true;
            if (current.index == destination.index) break;
            foreach (Arc a in current.arcs) {
                AstarNodeInfo successor;
                if (!infoTable.TryGetValue(a.to.index, out successor)) {
                    newNodeCount++;
                    successor = new AstarNodeInfo(a.to);
                    successor.g = float.MaxValue;
                    successor.h = h(successor, destination);
                    infoTable[a.to.index] = successor;
                }
                if (!successor.closed) {
                    float g_old = successor.g;
                    // ComputeCost
                    AstarNodeInfo parent = current;
                    if (parent.parent != null && space.LineOfSight(parent.parent.center, successor.center)) {
                        parent = parent.parent;
                    }
                    float gNew = parent.g + (successor.center - parent.center).magnitude;
                    if (successor.g > gNew) {
                        successor.parent = parent;
                        successor.g = gNew;
                        successor.f = successor.g + successor.h;
                    } //
                    if (successor.g < g_old) {
                        if (successor.open)
                            open.Delete(successor.handle);
                        open.Add(ref successor.handle, successor);
                        successor.open = true;
                    }
                }
            }
        }
        Debug.Log("time: " + (Time.realtimeSinceStartup - t) + " NodeCount: " + nodeCount + " NewNodeCount: " + newNodeCount);
        if (current.index != destination.index) return null;
        while (current.parent.parent != null && space.LineOfSight(current.parent.parent.center, current.center)) {
            current.parent = current.parent.parent;
        }
        return backtrack(current);
    }

}



