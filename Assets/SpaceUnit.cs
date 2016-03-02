using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceUnit : MonoBehaviour {

    public Octree space;
    public Queue<Node> wayPoints;
    public float wayPointRange;
    public float maxVelocity = 1f;
    public float acceleration = 5f;


    public Vector3 velocity;
    public Vector3 position;
    public bool active = true;

	// Use this for initialization
	void Start () {
        position = transform.position;
	}

    // Update is called once per frame
    void Update() {
        position = transform.position;
        Node next = null;
        if (wayPoints != null && wayPoints.Count > 0) {
            next = wayPoints.Peek(); 
            while (next != null && (next.center - position).sqrMagnitude < wayPointRange * wayPointRange) {
                wayPoints.Dequeue();
                next = wayPoints.Count > 0 ? wayPoints.Peek() : null;
            }
        }
        Vector3 targetVelocity = next != null ? (next.center - position).normalized * maxVelocity : Vector3.zero;
        if ((targetVelocity - velocity).sqrMagnitude < U.Sq(acceleration * Time.deltaTime)) {
            velocity = targetVelocity;
        } else {
            velocity += (targetVelocity - velocity).normalized * acceleration * Time.deltaTime;
        }
        position += velocity * Time.deltaTime;

        transform.position = position;
        if (velocity.sqrMagnitude > 0.0001f) {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * 5);
        }
        Rigidbody body = GetComponent<Rigidbody>();
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    public void SetWayPoints(List<Node> wp, float range) {
        if (wp == null) return;
        wayPoints = new Queue<Node>(wp);
        wayPointRange = range;
    }
}
