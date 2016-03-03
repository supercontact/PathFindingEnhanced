using UnityEngine;
using System.Collections;

public class LaserBeam : MonoBehaviour {

    public Vector3 origin;
    public Vector3 direction;
    public float speed;
    public float range;
    public int team;

    public float length = 0.1f;

    private float startTime;
    private LineRenderer lineDrawer = null;

	// Use this for initialization
	void Start () {
        startTime = Time.time;
    }
	
	// Update is called once per frame
	void Update () {
        float d = speed * (Time.time - startTime);
        if (d > range) {
            Destroy(gameObject);
            return;
        }
        if (lineDrawer == null) {
            lineDrawer = gameObject.AddComponent<LineRenderer>();
            lineDrawer.SetWidth(0.01f, 0.01f);
        }
        lineDrawer.SetPosition(0, origin + direction * d);
        lineDrawer.SetPosition(1, origin + direction * Mathf.Max(0, d - length));
    }
}
