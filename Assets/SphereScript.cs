using UnityEngine;
using System.Collections;

public class SphereScript : MonoBehaviour {

    public GameObject cam;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = cam.transform.position + Vector3.up * 1.1f;
	}
}
