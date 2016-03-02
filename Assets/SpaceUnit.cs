using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceUnit : MonoBehaviour {

    public Octree space;
    public Graph spaceGraph;
    public Queue<Node> wayPoints;
    public float wayPointRange;
    public float maxVelocity = 1f;
    float acceleration = 3f;
    public float radius = 0.1f;
    float repulsiveRadius = 0.1f;
    float repulsiveCoeff = 5f;
    float repulsivePow = 1.5f;


    public Vector3 velocity;
    public Vector3 position;
    public bool active = true;

    public Node last;

    private float recheckTimer = 1f;
    public float recheckInterval = 1f;

	// Use this for initialization
	void Start () {
        position = transform.position;
	}

    // Update is called once per frame
    void Update() {
        position = transform.position;

        // Find next waypoint
        Node next = null;
        Vector3 nextSpot = Vector3.zero;
        if (wayPoints != null && wayPoints.Count > 0) {
            next = wayPoints.Peek();
            nextSpot = next.center + ((last == null || wayPoints.Count == 1) ? Vector3.zero : (next.center - last.center).normalized * wayPointRange);

            while (next != null && (nextSpot - position).sqrMagnitude < wayPointRange * wayPointRange) {
                last = wayPoints.Dequeue();
                next = wayPoints.Count > 0 ? wayPoints.Peek() : null;
                if (next != null) {
                    nextSpot = next.center + ((last == null || wayPoints.Count == 1) ? Vector3.zero : (next.center - last.center).normalized * wayPointRange);
                }
            }
        }
        if (wayPoints == null || wayPoints.Count == 0) last = null;

        // Check line of sight to the next way point
        recheckTimer -= Time.deltaTime;
        if (recheckTimer <= 0) {
            if (next != null && !space.LineOfSight(position, next.center, false, true)) {
                List<Node> tempPath = spaceGraph.FindPath(spaceGraph.LazyThetaStar, position, next.center, space);
                if (tempPath != null) {
                    Queue<Node> newPath = new Queue<Node>(tempPath);
                    wayPoints.Dequeue();
                    while (wayPoints.Count > 0) newPath.Enqueue(wayPoints.Dequeue());
                    wayPoints = newPath;
                }
            }
            recheckTimer += recheckInterval;
        }

        Vector3 targetVelocity = Vector3.zero;
        if (next != null) {
            targetVelocity = (nextSpot - position).normalized * maxVelocity;
        }
        if ((targetVelocity - velocity).sqrMagnitude < U.Sq(acceleration * Time.deltaTime)) {
            velocity = targetVelocity;
        } else {
            velocity += (targetVelocity - velocity).normalized * acceleration * Time.deltaTime;
        }

        Collider[] touch = Physics.OverlapSphere(position, radius + repulsiveRadius);
        foreach (Collider col in touch) {
            SpaceUnit ship = col.GetComponent<SpaceUnit>();
            if (ship != null) {
                float d = (ship.position - position).magnitude - radius - ship.radius;
                Vector3 acc = (position - ship.position).normalized * repulsiveCoeff * Mathf.Pow(1 - Mathf.Clamp01(d / repulsiveRadius), repulsivePow);
                //Vector3 acc = (position - ship.position).normalized * repulsiveCoeff / (d + radius) / (d + radius);
                velocity += acc * Time.deltaTime;
            }
        }
        for (int i = 0; i < 16; i++) {
            Ray ray = new Ray(position, Random.onUnitSphere);
            RaycastHit res;
            if (Physics.Raycast(ray, out res, radius + repulsiveRadius) && res.collider.GetComponent<SpaceUnit>() == null) {
                float d = (res.point - position).magnitude - radius;
                Vector3 acc = (position - res.point).normalized * repulsiveCoeff * Mathf.Pow(1 - Mathf.Clamp01(d / repulsiveRadius), repulsivePow);
                velocity += acc * Time.deltaTime / 16 * 8;
            }
        }
        position += velocity * Time.deltaTime;

        transform.position = position;
        if (targetVelocity.sqrMagnitude > 0.0001f) {
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetVelocity), Time.deltaTime * 5);
            transform.rotation = Quaternion.Lerp(Quaternion.identity, Quaternion.FromToRotation(transform.forward, targetVelocity), Time.deltaTime * 5) * transform.rotation;
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
