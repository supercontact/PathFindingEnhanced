using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

    public GameObject obj;
    public GameObject mark;

    Mesh mesh;
    Geometry geo;
    Graph graph;
	// Use this for initialization
	void Start () {
        mesh = MeshFactory.ReadMeshFromFile("cow", 1.5f, new Vector3(0.15f, 0.15f, 0));
        obj.GetComponent<MeshFilter>().mesh = mesh;
        geo = new Geometry(mesh);
        graph = geo.ToGraph();
        
	}

    void Test () {
        int i1 = Random.Range(0, graph.nodes.Count);
        int i2 = Random.Range(0, graph.nodes.Count);
        List<Node> path = graph.Astar(graph.nodes[i1], graph.nodes[i2]);
        foreach (Node node in path) {
            GameObject m = Instantiate(mark);
            m.transform.position = node.center;
        }
    }
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown(KeyCode.Space)) {
            Test();
        }
	}
}
