using UnityEngine;
using System.Collections;

public class Draw : MonoBehaviour {

    Vector3 center = new Vector3(5, 0, 0);
    float d = 1;
	// Use this for initialization
	void Start () {
        int t = 0;
        Vector3[] pts = new Vector3[27];
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                for (int k = -1; k <= 1; k++) {
                    pts[t] = center + d * new Vector3(i, j, k);
                    t++;
                }
            }
        }
        for (int i = 0; i < 26; i++) {
            for (int j = i + 1; j < 27; j++) {
                GameObject g = new GameObject();
                LineRenderer l = g.AddComponent<LineRenderer>();
                l.SetWidth(0.01f, 0.01f);
                l.SetVertexCount(2);
                l.SetPosition(0, pts[i]);
                l.SetPosition(1, pts[j]);
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
