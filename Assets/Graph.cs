using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using C5;

public class Node {
	public int index;
    public int connectIndex = 0;
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
    public List<Node> temporaryNodes;

    public Graph() {
        nodes = new List<Node>();
        temporaryNodes = new List<Node>();
    }
    public int AddNode(Vector3 center) {
        nodes.Add(new Node(center, nodes.Count));
        return nodes.Count - 1;
    }

    public void AddArc(int fromIndex, int toIndex) {
        nodes[fromIndex].arcs.Add(new Arc(nodes[fromIndex], nodes[toIndex]));
    }

    public void CalculateConnectivity() {
        foreach (Node node in nodes) {
            node.connectIndex = 0;
        }
        int current = 1;
        foreach (Node node in nodes) {
            if (node.connectIndex == 0) {
                node.connectIndex = current;
                Queue<Node> toSet = new Queue<Node>();
                toSet.Enqueue(node);
                while (toSet.Count > 0) {
                    Node next = toSet.Dequeue();
                    foreach (Arc arc in next.arcs) {
                        if (arc.to.connectIndex != current) {
                            arc.to.connectIndex = current;
                            toSet.Enqueue(arc.to);
                        }
                    }
                }
                current++;
            }
        }
    }

    public Node AddTemporaryNode(Vector3 position, List<Node> neighbors) {
        Node newNode = new Node(position, -1 - temporaryNodes.Count);
        temporaryNodes.Add(newNode);
        foreach (Node neighbor in neighbors) {
            //GameObject test = GameObject.Instantiate(GameObject.Find("Sphere"));
            //test.transform.position = neighbor.center;
            if (neighbor != null) {
                newNode.arcs.Add(new Arc(newNode, neighbor));
                neighbor.arcs.Add(new Arc(neighbor, newNode));
                newNode.connectIndex = neighbor.connectIndex;
            }
        }
        return newNode;
    }

    public void RemoveTemporaryNodes() {
        foreach (Node node in temporaryNodes) {
            foreach (Arc arc in node.arcs) {
                List<Arc> originalArcs = new List<Arc>();
                foreach (Arc neighborArc in arc.to.arcs) {
                    if (neighborArc.to != node) {
                        originalArcs.Add(neighborArc);
                    }
                }
                arc.to.arcs = originalArcs;
            }
            node.arcs.Clear();
        }
        temporaryNodes.Clear();
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

    public delegate List<List<Node>> PathFindingMethod(Node source, List<Node> destinations, Octree space, H h = null);

    public List<Node> FindPath(PathFindingMethod method, Node source, Node destination, Octree space, H h = null) {
        return FindPath(method, source, new List<Node>() { destination }, space, h)[0];
    }
    public List<List<Node>> FindPath(PathFindingMethod method, Node source, List<Node> destinations, Octree space, H h = null) {
        return method(source, destinations, space, h);
    }
    public List<Node> FindPath(PathFindingMethod method, Vector3 source, Vector3 destination, Octree space, H h = null) {
        return FindPath(method, source, new List<Vector3>() { destination }, space, h)[0];
    }
    public List<List<Node>> FindPath(PathFindingMethod method, Vector3 source, List<Vector3> destinations, Octree space, H h = null) {
        List<Node> sourceCorners = space.FindBoundingCornerGraphNodes(source);
        Node tempSourceNode = AddTemporaryNode(source, sourceCorners);
        List<Node> tempDestinationNodes = new List<Node>();
        foreach (Vector3 destination in destinations) {
            List<Node> destinationCorners = space.FindBoundingCornerGraphNodes(destination);
            tempDestinationNodes.Add(AddTemporaryNode(destination, destinationCorners));
        }
        List<List<Node>> result = FindPath(method, tempSourceNode, tempDestinationNodes, space, h);
        RemoveTemporaryNodes();
        return result;
    }


    public List<List<Node>> AStar(Node source, List<Node> destinations, Octree space, H h = null) {
        if (h == null)
            h = estimatedCost;
        List<List<Node>> result = new List<List<Node>>();

        Dictionary<int, AstarNodeInfo> infoTable = new Dictionary<int, AstarNodeInfo>();
        AstarNodeInfo sourceInfo = new AstarNodeInfo(source);
        infoTable[sourceInfo.index] = sourceInfo;

        IntervalHeap<AstarNodeInfo> open = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
        sourceInfo.open = true;
        sourceInfo.g = 0;

        for (int i = 0; i < destinations.Count; i++) {
            Node destination = destinations[i];
            if (i == 0) {
                sourceInfo.f = h(source, destination);
                open.Add(ref sourceInfo.handle, sourceInfo);
            } else {
                AstarNodeInfo destInfo;
                if (infoTable.TryGetValue(destination.index, out destInfo) && destInfo.closed) {
                    result.Add(backtrack(destInfo));
                    continue;
                }
            }
            if (source.connectIndex != destination.connectIndex) {
                result.Add(null);
                continue;
            }

            if (i > 0) {
                IntervalHeap<AstarNodeInfo> newOpen = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
                foreach (AstarNodeInfo n in open) {
                    n.f = n.g + h(n, destination);
                    n.handle = null;
                    newOpen.Add(ref n.handle, n);
                }
                open = newOpen;
            }
            AstarNodeInfo current = null;
            while (open.Count > 0) {
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
            if (current == null || current.index != destination.index) {
                result.Add(null);
                continue;
            }
            result.Add(backtrack(current));
            open.Add(ref current.handle, current);
        }
        return result;
    }


    public List<List<Node>> ThetaStar(Node source, List<Node> destinations, Octree space, H h = null) {
        float t = Time.realtimeSinceStartup;
        int nodeCount = 0;
        int newNodeCount = 0;

        if (h == null)
            h = estimatedCost;
        List<List<Node>> result = new List<List<Node>>();

        Dictionary<int, AstarNodeInfo> infoTable = new Dictionary<int, AstarNodeInfo>();
        AstarNodeInfo sourceInfo = new AstarNodeInfo(source);
        infoTable[sourceInfo.index] = sourceInfo;

        IntervalHeap<AstarNodeInfo> open = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
        sourceInfo.open = true;
        sourceInfo.g = 0;

        for (int i = 0; i < destinations.Count; i++) {
            Node destination = destinations[i];
            if (i == 0) {
                sourceInfo.f = h(source, destination);
                open.Add(ref sourceInfo.handle, sourceInfo);
            } else {
                AstarNodeInfo destInfo;
                if (infoTable.TryGetValue(destination.index, out destInfo) && destInfo.closed) {
                    result.Add(backtrack(destInfo));
                    continue;
                }
            }
            if (source.connectIndex != destination.connectIndex) {
                result.Add(null);
                continue;
            }

            if (i > 0) {
                IntervalHeap<AstarNodeInfo> newOpen = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
                foreach (AstarNodeInfo n in open) {
                    n.f = n.g + h(n, destination);
                    n.handle = null;
                    newOpen.Add(ref n.handle, n);
                }
                open = newOpen;
            }

            AstarNodeInfo current = null;
            while (open.Count > 0) {
                nodeCount++;
                current = open.DeleteMin();
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

            if (current == null || current.index != destination.index) {
                result.Add(null);
                continue;
            }
            AstarNodeInfo check = current;
            while (check.parent != null) {
                while (check.parent.parent != null && space.LineOfSight(check.parent.parent.center, check.center)) {
                    check.parent = check.parent.parent;
                }
                check = check.parent;
            }
            result.Add(backtrack(current));
            open.Add(ref current.handle, current);
        }
        //Debug.Log("time: " + (Time.realtimeSinceStartup - t) + " NodeCount: " + nodeCount + " NewNodeCount: " + newNodeCount);
        return result;
    }


    public List<List<Node>> LazyThetaStar(Node source, List<Node> destinations, Octree space, H h = null) {
        float t = Time.realtimeSinceStartup;
        int nodeCount = 0;
        int newNodeCount = 0;

        if (h == null)
            h = estimatedCost;
        List<List<Node>> result = new List<List<Node>>();

        Dictionary<int, AstarNodeInfo> infoTable = new Dictionary<int, AstarNodeInfo>();
        AstarNodeInfo sourceInfo = new AstarNodeInfo(source);
        infoTable[sourceInfo.index] = sourceInfo;

        IntervalHeap<AstarNodeInfo> open = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
        sourceInfo.open = true;
        sourceInfo.g = 0;

        for (int i = 0; i < destinations.Count; i++) {
            Node destination = destinations[i];
            if (i == 0) {
                sourceInfo.f = h(source, destination);
                open.Add(ref sourceInfo.handle, sourceInfo);
            } else {
                AstarNodeInfo destInfo;
                if (infoTable.TryGetValue(destination.index, out destInfo) && destInfo.closed) {
                    result.Add(backtrack(destInfo));
                    continue;
                }
            }
            if (source.connectIndex != destination.connectIndex) {
                result.Add(null);
                continue;
            }

            if (i > 0) {
                IntervalHeap<AstarNodeInfo> newOpen = new IntervalHeap<AstarNodeInfo>(new NodeFComparer());
                foreach (AstarNodeInfo n in open) {
                    n.f = n.g + h(n, destination);
                    n.handle = null;
                    newOpen.Add(ref n.handle, n);
                }
                open = newOpen;
            }

            AstarNodeInfo current = null;
            while (open.Count > 0) {
                nodeCount++;
                current = open.DeleteMin();
                current.open = false;
                current.closed = true;
                // SetVertex
                if (current.parent != null && !space.LineOfSight(current.parent.center, current.center)) {
                    AstarNodeInfo realParent = null;
                    float realg = float.MaxValue;
                    foreach (Arc a in current.arcs) {
                        AstarNodeInfo tempParent;
                        float tempg;
                        if (infoTable.TryGetValue(a.to.index, out tempParent) && tempParent.closed) {
                            tempg = tempParent.g + (current.center - tempParent.center).magnitude;
                            if (tempg < realg) {
                                realParent = tempParent;
                                realg = tempg;
                            }
                        }
                    }
                    current.parent = realParent;
                    current.g = realg;
                } //
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
                        AstarNodeInfo parent = current.parent == null ? current : current.parent;
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
            if (current == null || current.index != destination.index) {
                result.Add(null);
                continue;
            }
            AstarNodeInfo check = current;
            while (check.parent != null) {
                while (check.parent.parent != null && space.LineOfSight(check.parent.parent.center, check.center)) {
                    check.parent = check.parent.parent;
                }
                check = check.parent;
            }
            result.Add(backtrack(current));
            open.Add(ref current.handle, current);
        }
        //Debug.Log("time: " + (Time.realtimeSinceStartup - t) + " NodeCount: " + nodeCount + " NewNodeCount: " + newNodeCount);
        return result;
    }
}



