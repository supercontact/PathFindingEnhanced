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

	public List<Node> Astar(Node source, Node destination, H h = null) {
        AstarNodeInfo[] infoTable = new AstarNodeInfo[nodes.Count];

        if (h == null)
			h = estimatedCost;
		AstarNodeInfo sourceInfo = new AstarNodeInfo(source);
        infoTable[sourceInfo.index] = sourceInfo;

        IntervalHeap<AstarNodeInfo> open = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
		open.Add(ref sourceInfo.handle, sourceInfo);
		sourceInfo.open = true;
		sourceInfo.f = h(source, destination);
		sourceInfo.g = 0;
		AstarNodeInfo nodeInfo_current = null;
		while(open.Count > 0) {
			nodeInfo_current = open.DeleteMin();
			if (nodeInfo_current.index == destination.index) break;
			foreach (Arc a in nodeInfo_current.arcs) {
                AstarNodeInfo nodeInfo_successor;
                if (infoTable[a.to.index] == null) {
                    nodeInfo_successor = new AstarNodeInfo(a.to);
                    infoTable[a.to.index] = nodeInfo_successor;
                } else {
                    nodeInfo_successor = infoTable[a.to.index];
                }
				float successor_current_cost = nodeInfo_current.g + a.distance;
				if (nodeInfo_successor.open) {
					if (nodeInfo_successor.g <= successor_current_cost) 
						continue;
				} else if (nodeInfo_successor.closed) {
					if (nodeInfo_successor.g <= successor_current_cost) 
						continue;
                    open.Add(ref nodeInfo_successor.handle, nodeInfo_successor);
                    nodeInfo_successor.closed = false;
                    nodeInfo_successor.open = true;
                } else {
					open.Add(ref nodeInfo_successor.handle, nodeInfo_successor);
					nodeInfo_successor.open = true;
					nodeInfo_successor.h = h(nodeInfo_successor, destination);
				}
				nodeInfo_successor.g = successor_current_cost;
				nodeInfo_successor.f = nodeInfo_successor.g + nodeInfo_successor.h;
				open.Replace(nodeInfo_successor.handle, nodeInfo_successor);
				nodeInfo_successor.parent = nodeInfo_current;
			}
			nodeInfo_current.closed = true;
		}
		if (nodeInfo_current.index != destination.index) throw new Exception();
		return backtrack(nodeInfo_current);
	}
}



