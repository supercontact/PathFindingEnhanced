using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

    public Camera cam;
    public GameObject obj;
    public GameObject mark;
    //public Quadtree tree;
    public Octree tree1;
    public Octree tree2;
    public SpaceUnit[] ships;
    public static float defaultShipSize = 0.1f;
    public static float defaultWaypointSize = 0.2f;

    public Material line1;
    public Material line2;

    Mesh mesh;
    //Geometry geo;
    Graph graph1;
    Graph graph2;
    Commanding command;

	// Use this for initialization
	void Start () {
        //mesh = MeshFactory.ReadMeshFromFile("bague", 0.6f, new Vector3(0.15f, 0.15f, 0));
        //obj.GetComponent<MeshFilter>().mesh = mesh;
        //tree1 = new Octree(16, new Vector3(-8, -8, -8), 8);
        tree2 = new ProgressiveOctree(16, new Vector3(-8, -8, -8), 8);
        //tree1.BuildFromGameObject(obj, defaultShipSize);
        tree2.BuildFromGameObject(obj, Mathf.Max(defaultShipSize - tree2.cellSize * Mathf.Sqrt(3) / 2, 0));
        //tree2.TestDisplay();
        graph1 = tree2.ToCenterGraph();
        graph2 = tree2.ToCornerGraph();
        command = new Commanding(tree2, graph2);
        for (int i = 0; i < ships.Length; i++) {
            ships[i].space = tree2;
            ships[i].spaceGraph = graph2;
            command.activeUnits.Add(ships[i]);
        }
    }

    void Test () {
        /*int i1 = Random.Range(0, graph.nodes.Count);
        int[] i2 = { Random.Range(0, graph.nodes.Count) , Random.Range(0, graph.nodes.Count) , Random.Range(0, graph.nodes.Count) };
        List<List<Node>> paths = graph.FindPath(graph.LazyThetaStar, graph.nodes[i1], new List<Node>() { graph.nodes[i2[0]], graph.nodes[i2[1]], graph.nodes[i2[2]] }, tree2);*/

        Vector3 v1 = new Vector3(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
        List<Vector3> v2 = new List<Vector3>();
        for (int i = 0; i < 1; i++) {
            v2.Add(new Vector3(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f)));
        }
        List<List<Node>> paths1 = graph1.FindPath(graph1.LazyThetaStar, v1, v2, tree2);
        List<List<Node>> paths2 = graph2.FindPath(graph2.LazyThetaStar, v1, v2, tree2);

        ClearDisplay();
        foreach (List<Node> path in paths1) {
            DrawPath(path, line1);
        }
        foreach (List<Node> path in paths2) {
            DrawPath(path, line2);
        }
    }

    int testTimes = 0;
    float[] totalTime = new float[7];
    float[] totalLength = new float[7];
    void TestScene1(int times = 1) {
        for (int i = 0; i < times; i++) {
            testTimes++;
            float t;
            Vector3 s = new Vector3(Random.Range(-2.0f, 0f), Random.Range(-2.0f, 0f), Random.Range(-2.0f, -0.2f));
            Vector3 d = new Vector3(Random.Range(-2.0f, 0f), Random.Range(-2.0f, 0f), Random.Range(0.2f, 2.0f));
            t = Time.realtimeSinceStartup;
            totalLength[0] += PathLength(graph1.FindPath(graph1.AStar, s, d, tree1));
            totalTime[0] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[1] += PathLength(graph1.FindPath(graph1.ThetaStar, s, d, tree1));
            totalTime[1] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[2] += PathLength(graph1.FindPath(graph1.LazyThetaStar, s, d, tree1));
            totalTime[2] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[3] += PathLength(graph2.FindPath(graph2.AStar, s, d, tree2));
            totalTime[3] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[4] += PathLength(graph2.FindPath(graph2.ThetaStar, s, d, tree2));
            totalTime[4] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[5] += PathLength(graph2.FindPath(graph2.LazyThetaStar, s, d, tree2));
            totalTime[5] += Time.realtimeSinceStartup - t;

            totalLength[6] += Mathf.Sqrt(Mathf.Min(
                U.Sq(s.x - d.x) + U.Sq(Mathf.Sqrt(s.z * s.z + s.y * s.y) + 1.0f / 16 + Mathf.Sqrt(U.Sq(d.z - 1.0f / 16) + d.y * d.y)),
                U.Sq(s.y - d.y) + U.Sq(Mathf.Sqrt(s.z * s.z + s.x * s.x) + 1.0f / 16 + Mathf.Sqrt(U.Sq(d.z - 1.0f / 16) + d.x * d.x))));
        }
        for (int i = 0; i < 7; i++) {
            Debug.Log(i + ": Average Time " + (totalTime[i] / testTimes) + "s Average Length " + (totalLength[i] / totalLength[6]) + " times: " + testTimes);
        }
    }

    void TestScene2(int times = 1) {
        for (int i = 0; i < times; i++) {
            testTimes++;
            float t;
            Vector3 s = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-2.0f, -1.1f));
            Vector3 d = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(1.1f, 2.0f));
            t = Time.realtimeSinceStartup;
            totalLength[0] += PathLength(graph1.FindPath(graph1.AStar, s, d, tree1));
            totalTime[0] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[1] += PathLength(graph1.FindPath(graph1.ThetaStar, s, d, tree1));
            totalTime[1] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[2] += PathLength(graph1.FindPath(graph1.LazyThetaStar, s, d, tree1));
            totalTime[2] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[3] += PathLength(graph2.FindPath(graph2.AStar, s, d, tree2));
            totalTime[3] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[4] += PathLength(graph2.FindPath(graph2.ThetaStar, s, d, tree2));
            totalTime[4] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[5] += PathLength(graph2.FindPath(graph2.LazyThetaStar, s, d, tree2));
            totalTime[5] += Time.realtimeSinceStartup - t;

            float angle = Vector3.Angle(s, d) * Mathf.Deg2Rad - Mathf.Acos(1/s.magnitude) - Mathf.Acos(1/d.magnitude);
            totalLength[6] += angle > 0 ? Mathf.Sqrt(s.sqrMagnitude - 1) + Mathf.Sqrt(d.sqrMagnitude - 1) + angle : (d - s).magnitude;
        }
        for (int i = 0; i < 7; i++) {
            Debug.Log(i + ": Average Time " + (totalTime[i] / testTimes) + "s Average Length " + (totalLength[i] / totalLength[6]) + " times: " + testTimes);
        }
    }

    void TestScene3(int times = 1) {
        for (int i = 0; i < times; i++) {
            testTimes++;
            float t;
            Vector3 s = new Vector3(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
            Vector3 d = new Vector3(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
            t = Time.realtimeSinceStartup;
            totalLength[0] += PathLength(graph1.FindPath(graph1.AStar, s, d, tree1));
            totalTime[0] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[1] += PathLength(graph1.FindPath(graph1.ThetaStar, s, d, tree1));
            totalTime[1] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[2] += PathLength(graph1.FindPath(graph1.LazyThetaStar, s, d, tree1));
            totalTime[2] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[3] += PathLength(graph2.FindPath(graph2.AStar, s, d, tree2));
            totalTime[3] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[4] += PathLength(graph2.FindPath(graph2.ThetaStar, s, d, tree2));
            totalTime[4] += Time.realtimeSinceStartup - t;
            t = Time.realtimeSinceStartup;
            totalLength[5] += PathLength(graph2.FindPath(graph2.LazyThetaStar, s, d, tree2));
            totalTime[5] += Time.realtimeSinceStartup - t;
        }
        for (int i = 0; i < 6; i++) {
            Debug.Log(i + ": Average Time " + (totalTime[i] / testTimes) + "s Average Length " + (totalLength[i] / testTimes) + " times: " + testTimes);
        }
    }

    void ResetData() {

    }

    float PathLength(List<Node> path) {
        if (path == null) return 0;
        float result = 0;
        Node prev = path[0];
        for (int i = 1; i < path.Count; i++) {
            result += (path[i].center - prev.center).magnitude;
            prev = path[i];
        }
        return result;
    }

    List<GameObject> display = new List<GameObject>();
    void DrawPath(List<Node> path, Material mat) {
        if (path == null) return;
        Vector3[] pathV = new Vector3[path.Count];
        int i = 0;
        foreach (Node node in path) {
            GameObject m = Instantiate(mark);
            display.Add(m);
            m.transform.position = node.center;
            if (i == 0) {
                m.transform.localScale = Vector3.one * 0.2f;
            }
            pathV[i] = node.center;
            i++;
        }
        GameObject lineObject = new GameObject();
        display.Add(lineObject);
        LineRenderer lr = lineObject.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.SetVertexCount(path.Count);
        lr.SetPositions(pathV);
        lr.SetWidth(0.01f, 0.01f);
    }
    void ClearDisplay() {
        foreach (GameObject g in display) {
            GameObject.Destroy(g);
        }
        display.Clear();
    }


    bool choosing = false;
    GameObject mouseClickMark;
    Vector3 mouseClickOrigin;
    Vector3 mouseClickNormal;
    float mouseClickHeight;
    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Test();
            //TestScene3(100);
        }

        if (Input.GetMouseButtonDown(0)) {
            if (!choosing) {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit)) {
                    mouseClickOrigin = hit.point;
                    mouseClickNormal = hit.normal;
                    mouseClickHeight = 0;
                    mouseClickMark = GameObject.Instantiate(mark);
                    mouseClickMark.AddComponent<LineRenderer>().SetWidth(0.02f, 0.02f);
                    choosing = true;
                }
            } else {
                GameObject.Destroy(mouseClickMark);
                command.MoveOrder(mouseClickOrigin + mouseClickNormal * mouseClickHeight);
                choosing = false;
            }
        }
        if (choosing) {
            Vector3 dir = cam.WorldToScreenPoint(mouseClickOrigin + mouseClickNormal) - cam.WorldToScreenPoint(mouseClickOrigin);
            dir.z = 0;
            dir.Normalize();
            Vector3 mouseVec = Input.mousePosition - cam.WorldToScreenPoint(mouseClickOrigin);
            mouseVec.z = 0;
            mouseClickHeight = Vector3.Dot(mouseVec, dir) / 100;
            mouseClickHeight = Mathf.Max(defaultShipSize + tree2.cellSize, mouseClickHeight);
            mouseClickMark.transform.position = mouseClickOrigin + mouseClickNormal * mouseClickHeight;
            mouseClickMark.GetComponent<LineRenderer>().SetVertexCount(2);
            mouseClickMark.GetComponent<LineRenderer>().SetPosition(0, mouseClickOrigin);
            mouseClickMark.GetComponent<LineRenderer>().SetPosition(1, mouseClickMark.transform.position);
            //Debug.Log(mouseClickMark.transform.position);
        }
    }
}
