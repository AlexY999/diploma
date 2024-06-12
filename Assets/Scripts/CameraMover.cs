using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField, Range(0.1f, 10.0f)]
    private float moveSpeed = 2.0f;
    [SerializeField, Range(30.0f, 150.0f)]
    private float mouseSensitivity = 90.0f;
    private bool _isCameraMoving = true;
    private Transform _cameraTransform;
    private Vector3 _initialMousePosition;
    private Vector3 _currentCameraRotation;
    private Vector3 _currentCameraPosition;
    private Quaternion _initialCameraRotation;
    private bool _isUIMessageActive;

    void Start()
    {
        _cameraTransform = this.gameObject.transform;
        _initialCameraRotation = this.gameObject.transform.rotation;
    }

    void Update()
    {
        CamControlIsActive();

        if (_isCameraMoving)
        {
            ResetCameraRotation();
            CameraRotationMouseControl();
            CameraSlideMouseControl();
            CameraPositionKeyControl();
        }
    }
    
    public void CamControlIsActive()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isCameraMoving = !_isCameraMoving;

            if (_isUIMessageActive == false)
            {
                StartCoroutine(DisplayUiMessage());
            }
            Debug.Log("CamControl : " + _isCameraMoving);
        }
    }
    
    private void ResetCameraRotation()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            this.gameObject.transform.rotation = _initialCameraRotation;
            Debug.Log("Cam Rotate : " + _initialCameraRotation.ToString());
        }
    }
    
    private void CameraRotationMouseControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _initialMousePosition = Input.mousePosition;
            _currentCameraRotation.x = _cameraTransform.transform.eulerAngles.x;
            _currentCameraRotation.y = _cameraTransform.transform.eulerAngles.y;
        }

        if (Input.GetMouseButton(0))
        {
            // Normalization = (start position - current position) / resolution
            float x = (_initialMousePosition.x - Input.mousePosition.x) / Screen.width;
            float y = (_initialMousePosition.y - Input.mousePosition.y) / Screen.height;

            // current rotate angle ＋ movement amount * mouse sensitivity
            float eulerX = _currentCameraRotation.x + y * mouseSensitivity;
            float eulerY = _currentCameraRotation.y + x * mouseSensitivity;

            _cameraTransform.rotation = Quaternion.Euler(eulerX, eulerY, 0);
        }
    }
    
    private void CameraSlideMouseControl()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _initialMousePosition = Input.mousePosition;
            _currentCameraPosition = _cameraTransform.position;
        }

        if (Input.GetMouseButton(1))
        {
            // Normalization: (start position - current position) / resolution
            float x = (_initialMousePosition.x - Input.mousePosition.x) / Screen.width;
            float y = (_initialMousePosition.y - Input.mousePosition.y) / Screen.height;

            x = x * moveSpeed;
            y = y * moveSpeed;

            Vector3 velocity = _cameraTransform.rotation * new Vector3(x, y, 0);
            velocity = velocity + _currentCameraPosition;
            _cameraTransform.position = velocity;
        }
    }
    
    private void CameraPositionKeyControl()
    {
        Vector3 campos = _cameraTransform.position;

        if (Input.GetKey(KeyCode.D)) { campos += _cameraTransform.right * Time.deltaTime * moveSpeed; }
        if (Input.GetKey(KeyCode.A)) { campos -= _cameraTransform.right * Time.deltaTime * moveSpeed; }
        if (Input.GetKey(KeyCode.E)) { campos += _cameraTransform.up * Time.deltaTime * moveSpeed; }
        if (Input.GetKey(KeyCode.Q)) { campos -= _cameraTransform.up * Time.deltaTime * moveSpeed; }
        if (Input.GetKey(KeyCode.W)) { campos += _cameraTransform.forward * Time.deltaTime * moveSpeed; }
        if (Input.GetKey(KeyCode.S)) { campos -= _cameraTransform.forward * Time.deltaTime * moveSpeed; }

        _cameraTransform.position = campos;
    }
    
    private IEnumerator DisplayUiMessage()
    {
        _isUIMessageActive = true;
        float time = 0;
        while (time < 2)
        {
            time = time + Time.deltaTime;
            yield return null;
        }
        _isUIMessageActive = false;
    }

    void OnGUI()
    {
        if (_isUIMessageActive == false) { return; }
        GUI.color = Color.black;
        if (_isCameraMoving == true)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "Active Camera Operation");
        }

        if (_isCameraMoving == false)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "Deactive Camera Operation");
        }
    }

}