using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {

    // Use this for initialization
    virtual protected void Start () {
	
	}

    // Update is called once per frame
    virtual protected void Update () {
	    
	}

    virtual public void Attack(SpaceUnit target) {
        // Nothing
    }
    virtual public void Stop() {
        // Nothing
    }

    virtual public void Fire(SpaceUnit target) {
        // Nothing
    }

}
