using UnityEngine;
using System.Collections;

public class LaserGun : Weapon {

    public SpaceUnit unit;
    public float range = 5;
    public float projectileSpeed = 10;
    public float fireRate = 1.6f;

    private float fireTimer = 0;
    public bool firing = false;
    public SpaceUnit target;

    // Use this for initialization
    override protected void Start() {
        base.Start();
    }

    // Update is called once per frame
    override protected void Update() {
        base.Update();
        if (fireTimer > 0) {
            fireTimer -= Time.deltaTime;
        } else if (firing && (target.position - transform.position).magnitude <= range) {
            Fire(target);
        }
    }

    override public void Attack(SpaceUnit target) {
        firing = true;
        this.target = target;
    }
    override public void Stop() {
        firing = false;
    }


    override public void Fire(SpaceUnit target) {
        if (fireTimer <= 0) {
            Vector3 dir = (target.position - transform.position).normalized;
            GameObject beamObj = new GameObject();
            LaserBeam beam = beamObj.AddComponent<LaserBeam>();
            beam.team = unit.team;
            beam.origin = transform.position;
            beam.direction = dir;
            beam.range = range;
            beam.speed = projectileSpeed;
            fireTimer += 1 / fireRate;
        }
    }

}
