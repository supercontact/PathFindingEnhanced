using UnityEngine;
using System.Collections;

/// <summary>
/// Smoothly show / hide the camera center object.
/// </summary>
public class CameraCenterScript : MonoBehaviour {

	public Transform X;
	public Transform Y;
	public Transform Z;
	public Transform O;
	public float smoothT = 0.1f;

	private bool visible = false;
	private float progress = 0;

	// Use this for initialization
	void Start () {
		X.localScale = new Vector3(1, 0, 0);
		Y.localScale = new Vector3(0, 1, 0);
		Z.localScale = new Vector3(0, 0, 1);
		O.localScale = new Vector3(0, 0, 0);
	}
	
	// Update is called once per frame
	void Update () {
		if (visible) {
			progress = 1 + (progress - 1) * Mathf.Exp( - Time.unscaledDeltaTime / smoothT);
		} else {
			progress *= Mathf.Exp( - Time.unscaledDeltaTime / smoothT);
		}
		if (progress > 0.01f || visible) {
			X.localScale = new Vector3(1, progress, progress);
			Y.localScale = new Vector3(progress, 1, progress);
			Z.localScale = new Vector3(progress, progress, 1);
			O.localScale = new Vector3(progress, progress, progress);
		} else {
			X.gameObject.SetActive(false);
			Y.gameObject.SetActive(false);
			Z.gameObject.SetActive(false);
			O.gameObject.SetActive(false);
		}
		transform.localRotation = Quaternion.Euler(0, Time.unscaledDeltaTime * 120, 0) * transform.localRotation;
	}

	public void Show() {
		visible = true;
		X.gameObject.SetActive(true);
		Y.gameObject.SetActive(true);
		Z.gameObject.SetActive(true);
		O.gameObject.SetActive(true);
	}
	public void Hide() {
		visible = false;
	}
}
