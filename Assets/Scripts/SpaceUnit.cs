using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceUnit : MonoBehaviour {

    public Octree space;
    public Graph spaceGraph;

    public int team = 1;

    public float radius = 0.1f;

    public bool movable = true;
    public float maxVelocity = 1f;
    public float acceleration = 3f;
    public float defaultWayPointRange = 0.2f;
    public float pathFindingRecheckInterval = 1f;
    public float repulsiveRadius = 0.1f;
    public float repulsiveCoeff = 5f;
    public float repulsivePow = 1.5f;

    public bool attackable = true;
    public float returnRange = 6f;
    public float enemyCheckRange = 4.5f;
    public float enemyCheckInterval = 0.5f;
    public float startChaseRange = 2.5f;
    public float chaseRange = 1.5f;
    public float standReturnRange = 0.5f;

    public List<Weapon> weapons;



    public enum UnitState
    {
        IDLE,
        MOVING,
        ATTACKING
    }
    public UnitState state = UnitState.IDLE;

    public Vector3 velocity;
    public Vector3 position;
    public Vector3 standPoint;

    public Queue<Vector3> wayPoints;
    public float wayPointRange;
    public Vector3 lastWayPoint;

    public SpaceUnit target;

    private float enemyCheckTimer = 0.5f;
    private float pathFindingRecheckTimer = 1f;

    // Use this for initialization
    void Start() {
        position = transform.position;
        standPoint = position;
	}

    // Update is called once per frame
    void Update() {
        if (attackable) PassiveAttackUpdate();
        if (movable) MovementUpdate();
    }

    private SpaceUnit EnemyCheck() {
        SpaceUnit result = null;
        Collider[] cols = Physics.OverlapSphere(position, enemyCheckRange);
        foreach (Collider col in cols) {
            SpaceUnit unit = col.GetComponent<SpaceUnit>();
            if (unit != null && unit.team != team && space.LineOfSight(position, unit.position, false, true) &&
                (result == null || (result.position - position).magnitude > (unit.position - position).magnitude)) {
                result = unit;
            }
        }
        return result;
    } 

    private void MovementUpdate() {
        position = transform.position;

        // Find next waypoint
        Vector3 next = Vector3.zero;
        Vector3 nextSpot = Vector3.zero;
        if (wayPoints != null && wayPoints.Count > 0) {
            next = wayPoints.Peek();
            nextSpot = next + ((lastWayPoint == Vector3.zero || wayPoints.Count == 1) ? Vector3.zero : (next - lastWayPoint).normalized * wayPointRange);

            while (wayPoints.Count > 0 && (nextSpot - position).sqrMagnitude < wayPointRange * wayPointRange) {
                lastWayPoint = wayPoints.Dequeue();
                if (wayPoints.Count > 0) {
                    next = wayPoints.Peek();
                    nextSpot = next + ((lastWayPoint == Vector3.zero || wayPoints.Count == 1) ? Vector3.zero : (next - lastWayPoint).normalized * wayPointRange);
                } else if (state == UnitState.MOVING) {
                    state = UnitState.IDLE;
                    standPoint = lastWayPoint;
                }
            }
        }
        if (wayPoints == null || wayPoints.Count == 0) lastWayPoint = Vector3.zero;

        // Check line of sight to the next way point
        pathFindingRecheckTimer -= U.limitedDeltaTime;
        if (pathFindingRecheckTimer <= 0) {
            bool jumped = false;
            if (wayPoints != null && wayPoints.Count > 1) {
                Queue<Vector3>.Enumerator e = wayPoints.GetEnumerator();
                e.MoveNext(); e.MoveNext();
                Vector3 nextnext = e.Current;
                if (space.LineOfSight(position, nextnext, false, true)) {
                    lastWayPoint = wayPoints.Dequeue();
                    next = wayPoints.Peek();
                    nextSpot = next + (wayPoints.Count == 1 ? Vector3.zero : (next - lastWayPoint).normalized * wayPointRange);
                    jumped = true;
                }
            }
            if (!jumped && wayPoints != null && wayPoints.Count > 0 && !space.LineOfSight(position, next, false, true)) {
                List<Node> tempPath = spaceGraph.FindPath(spaceGraph.LazyThetaStar, position, next, space);
                if (tempPath != null) {
                    Queue<Vector3> newPath = new Queue<Vector3>();
                    foreach (Node node in tempPath) {
                        newPath.Enqueue(node.center);
                    }
                    wayPoints.Dequeue();
                    while (wayPoints.Count > 0) newPath.Enqueue(wayPoints.Dequeue());
                    wayPoints = newPath;
                }
            }
            pathFindingRecheckTimer += pathFindingRecheckInterval;
        }

        // Accelerate to target velocity
        Vector3 targetVelocity = Vector3.zero;
        if (wayPoints != null && wayPoints.Count > 0) {
            targetVelocity = (nextSpot - position).normalized * maxVelocity;
        }
        if ((targetVelocity - velocity).sqrMagnitude < U.Sq(acceleration * U.limitedDeltaTime)) {
            velocity = targetVelocity;
        } else {
            velocity += (targetVelocity - velocity).normalized * acceleration * U.limitedDeltaTime;
        }

        // Repulsive force from SpaceUnits
        Collider[] touch = Physics.OverlapSphere(position, radius + repulsiveRadius);
        foreach (Collider col in touch) {
            SpaceUnit ship = col.GetComponent<SpaceUnit>();
            if (ship != null) {
                float d = (ship.position - position).magnitude - radius - ship.radius;
                Vector3 acc = (position - ship.position).normalized * repulsiveCoeff * Mathf.Pow(1 - Mathf.Clamp01(d / repulsiveRadius), repulsivePow);
                //Vector3 acc = (position - ship.position).normalized * repulsiveCoeff / (d + radius) / (d + radius);
                velocity += acc * U.limitedDeltaTime;
            }
        }

        // Repulsive force from walls
        for (int i = 0; i < 16; i++) {
            Ray ray = new Ray(position, Random.onUnitSphere);
            RaycastHit res;
            if (Physics.Raycast(ray, out res, radius + repulsiveRadius) && res.collider.GetComponent<SpaceUnit>() == null) {
                float d = (res.point - position).magnitude - radius;
                Vector3 acc = (position - res.point).normalized * repulsiveCoeff * Mathf.Pow(1 - Mathf.Clamp01(d / repulsiveRadius), repulsivePow);
                velocity += acc * U.limitedDeltaTime / 16 * 8;
            }
        }
        position += velocity * U.limitedDeltaTime;

        // Update position
        transform.position = position;
        if (targetVelocity.sqrMagnitude > 0.0001f) {
            //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetVelocity), Time.deltaTime * 5);
            transform.rotation = Quaternion.Lerp(Quaternion.identity, Quaternion.FromToRotation(transform.forward, targetVelocity), U.limitedDeltaTime * 5) * transform.rotation;
        }
        Rigidbody body = GetComponent<Rigidbody>();
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        if (Settings.showShipTrajectory) {
            DrawTrajectory();
        } else if (line != null) {
            ClearTrajectory();
        }
    }

    private void PassiveAttackUpdate() {
        if (target != null && ((target.position - position).magnitude > enemyCheckRange || false)) {
            target = null;
        }

        if ((position - standPoint).magnitude > standReturnRange) {
            MoveOrder(standPoint, defaultWayPointRange);
        }

        if (enemyCheckTimer > 0) {
            enemyCheckTimer -= U.limitedDeltaTime;
        } else if (state == UnitState.IDLE || state == UnitState.MOVING) {
            if (target == null) {
                SpaceUnit enemy = EnemyCheck();
                if (enemy != null) {
                    target = enemy;
                    foreach (Weapon weapon in weapons) {
                        weapon.Attack(target);
                    }
                }
            }

            if (target != null && state == UnitState.IDLE && (target.position - position).magnitude > startChaseRange) {
                wayPoints = new Queue<Vector3>();
                Vector3 chasePos = target.position + (position - target.position).normalized * chaseRange;
                wayPoints.Enqueue(chasePos);
                wayPointRange = defaultWayPointRange;
            }            
            enemyCheckTimer += enemyCheckInterval;
        }

        if (target != null && state == UnitState.IDLE && ((target.position - position).magnitude > enemyCheckRange || (position - standPoint).magnitude > returnRange || false)) {
            MoveOrder(standPoint, defaultWayPointRange);
        }
    }

    private LineRenderer line = null;
    public void DrawTrajectory() {
        if (line == null) {
            line = new GameObject().AddComponent<LineRenderer>();
            line.SetWidth(0.01f, 0.01f);
            line.material = GameObject.Find("LineMaterial").GetComponent<MeshRenderer>().sharedMaterial;
        }
        if (wayPoints != null && wayPoints.Count > 0) {
            line.SetVertexCount(wayPoints.Count + 1);
            line.SetPosition(0, position);
            int t = 1;
            foreach (Vector3 pos in wayPoints) {
                line.SetPosition(t++, pos);
            }
        } else {
            line.SetVertexCount(0);
        }
    }
    public void ClearTrajectory() {
        if (line != null) {
            Destroy(line.gameObject);
        }
    }

    public void MoveOrder(Vector3 targetPoint, float range) {
        MoveOrder(spaceGraph.FindPath(spaceGraph.LazyThetaStar, position, targetPoint, space), range);
    }

    public void MoveOrder(List<Node> wp, float range) {
        if (wp == null || !movable) return;
        wayPoints = new Queue<Vector3>();
        foreach (Node node in wp) {
            wayPoints.Enqueue(node.center);
        }
        wayPointRange = range;
        state = UnitState.MOVING;
    }
}
