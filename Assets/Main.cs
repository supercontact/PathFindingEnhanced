using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

    public Camera cam;
    public GameObject obj;
    public GameObject mark;
    public ProgressiveQuadtree tree;
    public ProgressiveOctree tree2;

    Mesh mesh;
    Geometry geo;
    Graph graph;
	// Use this for initialization
	void Start () {
        mesh = MeshFactory.ReadMeshFromFile("cow", 1.5f, new Vector3(0.15f, 0.15f, 0));
        obj.GetComponent<MeshFilter>().mesh = mesh;
        geo = new Geometry(mesh);
        graph = geo.ToGraph();
        TestQuadtree();
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

    void TestQuadtree () {
        tree = new ProgressiveQuadtree(10, new Vector2(-5, -5));
        //tree2 = new ProgressiveOctree(4, new Vector3(-2, -2, -2));
    }
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown(KeyCode.Space)) {
            Test();
        }

        if (Input.GetMouseButtonDown(0)) {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                //tree.DivideUntilLevel(new Vector2(hit.point.x, hit.point.z), 5);
                tree.DivideLineUntilLevel(new Vector2(0, 0), new Vector2(hit.point.x, hit.point.z), 5);
                //tree2.DivideUntilLevel(hit.point, 5);
            }
        }
	}
}
