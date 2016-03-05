using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

    public Camera cam;
    public GameObject[] scenes;
    public GameObject mark;
    public GameObject ship;
    public static float defaultShipSize = 0.1f;
    public static float defaultWaypointSize = 0.2f;

    public float[,] randomRangeSource = { { -2, 2, -2, 2, -2, 2 }, { -2, 2, -2, 2, -2, 2 }, { -2, 0, -2, 0, -2, -0.2f }, { -2, 0, -2, 0, -2, -0.2f }, { -1, 1, -1, 1, -2, -1.1f }, { -2, 2, -2, 2, -2, 2 } };
    public float[,] randomRangeDestination = { { -2, 2, -2, 2, -2, 2 }, { -2, 2, -2, 2, -2, 2 }, { -2, 0, -2, 0, 0.2f, 2 }, { -2, 0, -2, 0, 0.2f, 2 }, { -1, 1, -1, 1, 1.1f, 2 }, { -2, 2, -2, 2, -2, 2 } };

    public Slider slider;
    public Text sliderValue;

    public Material[] lines;
    public Text[] pathInfo;
    public Text realPathInfo;

    public Material sky, blankSky;

    int sceneIndex = -1;
    int maxLevel = 8;
    World[] worlds;
    List<SpaceUnit> ships = new List<SpaceUnit>();

    Commanding command;

    // Use this for initialization
    void Start() {

        //mesh = MeshFactory.ReadMeshFromFile("bague", 0.6f, new Vector3(0.15f, 0.15f, 0));
        //obj.GetComponent<MeshFilter>().mesh = mesh;

        LoadScene(0);
    }

    public void LoadScene(int index) {
        if (sceneIndex != index) {
            ClearVoxels();
            ClearDisplayEntirely();
            while (ships.Count > 0) RemoveShip();

            if (sceneIndex >= 0) scenes[sceneIndex].SetActive(false);
            scenes[index].SetActive(true);
            worlds = new World[5];
            worlds[0] = new World(scenes[index], 16, Vector3.zero, maxLevel, 0, false, false);
            worlds[1] = new World(worlds[0].space, true);
            worlds[2] = new World(scenes[index], 16, Vector3.zero, maxLevel, 0, true, false);
            worlds[3] = new World(worlds[2].space, true);
            float ext = Mathf.Max(defaultShipSize - 16f / (1 << 8) * Mathf.Sqrt(3) / 2, 0);
            worlds[4] = new World(scenes[index], 16, Vector3.zero, 8, ext, true, true);

            command = new Commanding(worlds[4]);
            sceneIndex = index;
        }
    }

    public void ChangeMaxLevel(int level) {
        if (maxLevel != level) {
            maxLevel = level;
            bool refreshVoxelDisplay = false;
            if (displaying) {
                ClearVoxels();
                refreshVoxelDisplay = true;
            }
            worlds = new World[5];
            worlds[0] = new World(scenes[sceneIndex], 16, Vector3.zero, maxLevel, 0, false, false);
            worlds[1] = new World(worlds[0].space, true);
            worlds[2] = new World(scenes[sceneIndex], 16, Vector3.zero, maxLevel, 0, true, false);
            worlds[3] = new World(worlds[2].space, true);

            if (refreshVoxelDisplay) {
                DisplayVoxels(currentDisplayLevel);
            }
            if (currentV1 != null) {
                TestPathFinding();
            }
        }
    }


    bool displayExtended = false;
    bool displaying = false;
    int currentDisplayLevel = -1;
    public void DisplayVoxels(int maxLevel) {
        Octree tree = displayExtended ? worlds[4].space : worlds[3].space;
        if (displaying) tree.ClearDisplay();
        tree.DisplayVoxels(maxLevel);
        displaying = true;
        currentDisplayLevel = maxLevel;
    }
    public void ClearVoxels() {
        if (displaying) {
            Octree tree = displayExtended ? worlds[4].space : worlds[3].space;
            tree.ClearDisplay();
            displaying = false;
        }
    }
    public void DisplaySwitchTree(bool on) {
        if (on != displayExtended) {
            if (displaying) {
                ClearVoxels();
                displayExtended = !displayExtended;
                DisplayVoxels(currentDisplayLevel);
            } else {
                displayExtended = !displayExtended;
            }
        }
    }

    int RandomTestNumber = 1;
    bool[,] RandomTestWorldMethod = { { false, false, false }, { false, false, false }, { false, false, true }, { false, false, true } };
    List<Vector3> currentV1 = null;
    List<List<Vector3>> currentV2 = null;
    public void RandomTest (int n) {
        /*int i1 = Random.Range(0, graph.nodes.Count);
        int[] i2 = { Random.Range(0, graph.nodes.Count) , Random.Range(0, graph.nodes.Count) , Random.Range(0, graph.nodes.Count) };
        List<List<Node>> paths = graph.FindPath(graph.LazyThetaStar, graph.nodes[i1], new List<Node>() { graph.nodes[i2[0]], graph.nodes[i2[1]], graph.nodes[i2[2]] }, tree2);*/

        currentV1 = new List<Vector3>();
        currentV2 = new List<List<Vector3>>();

        for (int t = 0; t < n; t++) {
            Vector3 v1;
            do {
                v1 = new Vector3(
                    Random.Range(randomRangeSource[sceneIndex, 0], randomRangeSource[sceneIndex, 1]),
                    Random.Range(randomRangeSource[sceneIndex, 2], randomRangeSource[sceneIndex, 3]),
                    Random.Range(randomRangeSource[sceneIndex, 4], randomRangeSource[sceneIndex, 5]));
            } while (worlds[0].space.IsBlocked(worlds[0].space.PositionToIndex(v1)));
            List<Vector3> v2 = new List<Vector3>();

            for (int i = 0; i < RandomTestNumber; i++) {
                Vector3 v2t;
                do {
                    v2t = new Vector3(
                        Random.Range(randomRangeDestination[sceneIndex, 0], randomRangeDestination[sceneIndex, 1]),
                        Random.Range(randomRangeDestination[sceneIndex, 2], randomRangeDestination[sceneIndex, 3]),
                        Random.Range(randomRangeDestination[sceneIndex, 4], randomRangeDestination[sceneIndex, 5]));
                } while (worlds[0].space.IsBlocked(worlds[0].space.PositionToIndex(v2t)));

                v2.Add(v2t);
            }

            currentV1.Add(v1);
            currentV2.Add(v2);
        }

        TestPathFinding();
    }

    void TestPathFinding() {
        ClearDisplay();
        for (int w = 0; w < 4; w++) {
            for (int m = 0; m < 3; m++) {
                if (RandomTestWorldMethod[w, m]) {
                    Graph.PathFindingMethod method;
                    if (m == 0) method = worlds[w].spaceGraph.AStar;
                    else if (m == 1) method = worlds[w].spaceGraph.ThetaStar;
                    else method = worlds[w].spaceGraph.LazyThetaStar;
                    float totalLength = 0;
                    float startTime = Time.realtimeSinceStartup;
                    for (int i = 0; i < currentV1.Count; i++) { 
                        List<List<Node>> paths = worlds[w].spaceGraph.FindPath(method, currentV1[i], currentV2[i], worlds[w].space);
                        foreach (List<Node> path in paths) {
                            if (currentV1.Count == 1) DrawPath(path, lines[w * 3 + m]);
                            totalLength += PathLength(path);
                        }
                    }
                    pathInfo[w * 3 + m].gameObject.SetActive(true);
                    if (currentV1.Count == 1) {
                        pathInfo[w * 3 + m].text = "Distance = " + (Mathf.Round(totalLength * 10000) / 10000) + 
                            "  Time = " + (Mathf.Round((Time.realtimeSinceStartup - startTime) * 100000) / 100) + "ms";
                    } else {
                        pathInfo[w * 3 + m].text = "Average distance = " + (Mathf.Round(totalLength / currentV1.Count * 10000) / 10000) + 
                            "  Average time = " + (Mathf.Round((Time.realtimeSinceStartup - startTime) / currentV1.Count * 100000) / 100) + "ms";
                    }
                } else {
                    pathInfo[w * 3 + m].gameObject.SetActive(false);
                }
            }
        }
        if (sceneIndex == 2 || sceneIndex == 3 || sceneIndex == 4) {
            float totalLength = 0;
            for (int i = 0; i < currentV1.Count; i++) {
                for (int j = 0; j < currentV2[i].Count; j++) {
                    Vector3 s = currentV1[i];
                    Vector3 d = currentV2[i][j];
                    if (sceneIndex == 2) {
                        totalLength += Mathf.Sqrt(Mathf.Min(
                            U.Sq(s.x - d.x) + U.Sq(Mathf.Sqrt(U.Sq(s.z + 1.0f / 16) + s.y * s.y) + 1.0f / 16 + Mathf.Sqrt(d.z * d.z + d.y * d.y)),
                            U.Sq(s.y - d.y) + U.Sq(Mathf.Sqrt(U.Sq(s.z + 1.0f / 16) + s.x * s.x) + 1.0f / 16 + Mathf.Sqrt(d.z * d.z + d.x * d.x))));
                    } else if (sceneIndex == 3) {
                        totalLength += Mathf.Sqrt(Mathf.Min(
                            U.Sq(s.x - d.x) + U.Sq(Mathf.Sqrt(s.z * s.z + s.y * s.y) + 1.0f / 16 + Mathf.Sqrt(U.Sq(d.z - 1.0f / 16) + d.y * d.y)),
                            U.Sq(s.y - d.y) + U.Sq(Mathf.Sqrt(s.z * s.z + s.x * s.x) + 1.0f / 16 + Mathf.Sqrt(U.Sq(d.z - 1.0f / 16) + d.x * d.x))));
                    } else if (sceneIndex == 4) {
                        float angle = Vector3.Angle(s, d) * Mathf.Deg2Rad - Mathf.Acos(1 / s.magnitude) - Mathf.Acos(1 / d.magnitude);
                        totalLength += angle > 0 ? Mathf.Sqrt(s.sqrMagnitude - 1) + Mathf.Sqrt(d.sqrMagnitude - 1) + angle : (d - s).magnitude;
                    }
                }
            }
            realPathInfo.gameObject.SetActive(true);
            if (currentV1.Count == 1) {
                realPathInfo.text = "Real distance = " + (Mathf.Round(totalLength * 10000) / 10000);
            } else {
                realPathInfo.text = "Real average distance = " + (Mathf.Round(totalLength / currentV1.Count * 10000) / 10000);
            }
        }
    }

    public void ToggleTest(int index) {
        int worldIndex = index / 3;
        int methodIndex = index % 3;
        RandomTestWorldMethod[worldIndex, methodIndex] = !RandomTestWorldMethod[worldIndex, methodIndex];
        if (currentV1 != null && currentV1.Count == 1) {
            TestPathFinding();
        }
    }

    public void AddShip() {
        SpaceUnit newShip = Instantiate(ship).GetComponent<SpaceUnit>();
        int connectIndex;
        if (sceneIndex != 5) {
            connectIndex = worlds[4].space.FindBoundingCornerGraphNodes(worlds[4].space.root.corners(0) + Vector3.one * 0.01f)[0].connectIndex;
        } else {
            connectIndex = worlds[4].space.FindBoundingCornerGraphNodes(worlds[4].space.root.center)[0].connectIndex;
        }
        Vector3 pos;
        do {
            pos = new Vector3(Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f), Random.Range(-2.0f, 2.0f));
        } while (worlds[4].space.IsBlocked(worlds[4].space.PositionToIndex(pos)) || worlds[4].space.FindBoundingCornerGraphNodes(pos)[0].connectIndex != connectIndex);

        newShip.transform.position = pos;
        newShip.space = worlds[4].space;
        newShip.spaceGraph = worlds[4].spaceGraph;
        command.activeUnits.Add(newShip);
        ships.Add(newShip);
    }

    public void RemoveShip() {
        if (ships.Count > 0) {
            Destroy(ships[ships.Count - 1].gameObject);
            ships.RemoveAt(ships.Count - 1);
            command.activeUnits.RemoveAt(command.activeUnits.Count - 1);
        }
    }

    public void SliderValueChange(float value) {
        sliderValue.text = value + "";
    }
    public void SliderConfirm() {
        ChangeMaxLevel((int) slider.value);
    }


    public float PathLength(List<Node> path) {
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
    public void DrawPath(List<Node> path, Material mat) {
        if (path == null) return;
        Vector3[] pathV = new Vector3[path.Count];
        int i = 0;
        foreach (Node node in path) {
            GameObject m = Instantiate(mark);
            display.Add(m);
            m.transform.position = node.center;
            if (i == 0) {
                m.transform.localScale = Vector3.one * 0.1f;
            } else {
                m.transform.localScale = Vector3.one * 0.03f;
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
    public void ClearDisplay() {
        foreach (GameObject g in display) {
            GameObject.Destroy(g);
        }
        display.Clear();
    }
    public void ClearDisplayEntirely() {
        ClearDisplay();
        for (int i = 0; i < pathInfo.Length; i++) {
            pathInfo[i].gameObject.SetActive(false);
        }
        realPathInfo.gameObject.SetActive(false);
        currentV1 = null;
        currentV2 = null;
    }


    bool choosing = false;
    GameObject mouseClickMark;
    Vector3 mouseClickOrigin;
    Vector3 mouseClickNormal;
    float mouseClickHeight;
    bool useBlankSky = false;
    // Update is called once per frame
    void Update() {

        for (int i = 0; i < 9; i++) {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) {
                DisplayVoxels(i + 1);
            }
        }
        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            ClearVoxels();
        }

        if (Input.GetKeyDown(KeyCode.I)) {
            useBlankSky = !useBlankSky;
            RenderSettings.skybox = useBlankSky ? blankSky : sky;
        }

        if (Input.GetMouseButtonDown(0) && ships.Count > 0) {
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
            mouseClickHeight = Mathf.Max(defaultShipSize + worlds[3].space.cellSize, mouseClickHeight);
            mouseClickMark.transform.position = mouseClickOrigin + mouseClickNormal * mouseClickHeight;
            mouseClickMark.GetComponent<LineRenderer>().SetVertexCount(2);
            mouseClickMark.GetComponent<LineRenderer>().SetPosition(0, mouseClickOrigin);
            mouseClickMark.GetComponent<LineRenderer>().SetPosition(1, mouseClickMark.transform.position);
            //Debug.Log(mouseClickMark.transform.position);
        }
    }
}
