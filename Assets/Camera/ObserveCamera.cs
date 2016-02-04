using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// A user-controllable camera.
/// </summary>
public class ObserveCamera : MonoBehaviour {

	public GameObject target;
	public CameraCenterScript center;
	public Vector3 targetOffset = Vector3.zero;
	public float mouseRotationControlDistance = 500;
	public float mouseRotationFactor = 1;
	public float mousePanningFactor = 1;
	public float mouseScrollZoomingFactor = 0.1f;
	public float mouseScrollMovingFactor = 0.1f;
	public float smoothT = 0.1f;

	private Camera cam;

	private Vector3 offset = Vector3.zero;
	private float targetDistance;
	private float distance;
	private Quaternion targetRotation;

	private Vector3 prevMousePos;
	private int mouseMode = -1;

	private bool clicking;

    public GameObject ball;

	// Start is called at the beginning
	void Start () {
		targetDistance = (target.transform.position - transform.position).magnitude;
		targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
		transform.rotation = targetRotation;
		cam = GetComponent<Camera>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.unscaledDeltaTime != 0f) {
            //if (clicking) {
            if (Input.GetMouseButtonDown(0)) {
                clicking = false;
				prevMousePos = Input.mousePosition;
				mouseMode = 0;
				center.Hide();
			} else if (Input.GetMouseButtonDown(1)) {
				prevMousePos = Input.mousePosition;
				mouseMode = 1;
				center.Show();
			} else if (Input.GetMouseButton(0) && mouseMode == 0) {
				// Rotate the camera by dragging with left mouse key
				Vector3 v1 = new Vector3(prevMousePos.x - Screen.width / 2, prevMousePos.y - Screen.height / 2, -mouseRotationControlDistance);
				Vector3 v2 = new Vector3(Input.mousePosition.x - Screen.width / 2, Input.mousePosition.y - Screen.height / 2, -mouseRotationControlDistance);
				Quaternion rot = Quaternion.Inverse(Quaternion.FromToRotation(v1, v2));
				float angle;
				Vector3 axis;
				rot.ToAngleAxis(out angle, out axis);
				rot = Quaternion.AngleAxis(angle * mouseRotationFactor, axis);
				targetRotation = targetRotation * rot;
				prevMousePos = Input.mousePosition;
			} else if (Input.GetMouseButton(1) && mouseMode == 1) {
				// Pan the camera by dragging with right mouse key, or using mouse scroll wheel when the right mouse key is pressed
				float distancePixelRatio = (cam.ScreenToWorldPoint(new Vector3(1, 0, targetDistance)) - cam.ScreenToWorldPoint(new Vector3(0, 0, targetDistance))).magnitude * mousePanningFactor;
				Vector3 move = new Vector3((prevMousePos.x - Input.mousePosition.x) * distancePixelRatio,
				                           (prevMousePos.y - Input.mousePosition.y) * distancePixelRatio,
				                           Input.mouseScrollDelta.y * targetDistance * mouseScrollMovingFactor);
				targetOffset += targetRotation * move;
				prevMousePos = Input.mousePosition;
			} else {
				mouseMode = -1;
			}

			if (Input.GetMouseButtonUp(1)) {
				center.Hide();
			}

			if (!Input.GetMouseButton(1) || mouseMode != 1) {
				// Zoom the camera with mouse scroll wheel
				targetDistance *= Mathf.Exp(- Input.mouseScrollDelta.y * mouseScrollZoomingFactor);
			}
			if (Input.GetMouseButtonDown(2)) {
				// Reset the camera center position by pressing the mouse middle key
				targetOffset = Vector3.zero;
			}

			// Smooth transition
			transform.rotation = Quaternion.Slerp(targetRotation, transform.rotation, Mathf.Exp(- Time.unscaledDeltaTime / smoothT));
			distance = Mathf.Lerp(targetDistance, distance, Mathf.Exp(- Time.unscaledDeltaTime / smoothT));
			offset = Vector3.Lerp(targetOffset, offset, Mathf.Exp(- Time.unscaledDeltaTime / smoothT));
			transform.position = target.transform.position + distance * (transform.rotation * Vector3.back) + offset;
			center.transform.localPosition = new Vector3(0, 0, distance);
		}

        ball.transform.position = transform.position + Vector3.up * 110;
	}

	public void Clicking(BaseEventData data) {
		PointerEventData pdata = (PointerEventData) data;
		clicking = pdata.button == PointerEventData.InputButton.Left;
	}


}
