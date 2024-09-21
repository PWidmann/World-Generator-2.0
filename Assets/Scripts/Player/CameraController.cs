using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RTS camera controller
/// =====================
/// Main scene camera must be child of the GameObject this script is attached to. Set camera local transform to:
/// Position: 0, 5, 5
/// Rotation: 45, 0, 0
/// </summary>
public class CameraController : MonoBehaviour
{
    #region Members
    [Header("Movement")]
    [SerializeField] private float normalMoveSpeed = 2f;
    [SerializeField] private float fastMoveSpeed = 4f;
    [SerializeField] private float moveSmoothTime = 0.2f;
    [SerializeField] private float camTargetHeight = 0f;
    [SerializeField] private float zoomSmoothTime = 0.2f;

    [Header("Zoom")]
    [SerializeField] private float startCameraDistance = 5f;
    [SerializeField] private float minCameraDistance = 2f;
    [SerializeField] private float maxCameraDistance = 10f;
    [SerializeField] private float zoomStep = 10f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 1;
    [SerializeField] private float rotationSmoothSpeed = 1;
    [SerializeField] private bool invertRotation = false;

    //Movement
    private float movementSpeed;
    private Vector2 movement;
    private Vector2 movementInput;
    private Vector3 targetPosition;
    private Vector3 moveVelocity;

    //Rotation
    private Ray ray;
    private float planeEntry;
    private float rotationAmount;
    private Plane mouseDragPlane;
    private Vector3 rotateStartPosition;
    private Vector3 rotateCurrentPosition;
    private Vector3 rotationDelta;
    private Quaternion targetRotation;

    // Zoom
    private float scrollValue; 
    private float targetCameraDistance;
    private Vector3 targetZoomPos;
    private Vector3 cameraZoomVelocity;

    // Drag
    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;
    private bool isDragging = false;

    // Ref
    private Camera cam;

    private bool IsMouseOverGameWindow
    {
        get
        {
            return !(0 > Input.mousePosition.x ||
                   0 > Input.mousePosition.y ||
                   Screen.width < Input.mousePosition.x ||
                   Screen.height < Input.mousePosition.y);
        }
    }
    #endregion

    void Start()
    {
        cam = Camera.main;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        movementSpeed = normalMoveSpeed;
        targetCameraDistance = startCameraDistance;
        mouseDragPlane = new Plane(Vector3.up, new Vector3(0, camTargetHeight, 0));
    }

    private void Update()
    {
        GetInput();
        CameraMovement();
        CameraRotation();
        CameraZoom();
        MouseDrag();
    }

    public void SetCamTargetHeight(float height)
    {
        camTargetHeight = height;
        mouseDragPlane = new Plane(Vector3.up, new Vector3(0, camTargetHeight, 0));
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    private void GetInput()
    {
        movementInput = GetMovementInput();
        movementSpeed = (Input.GetKey(KeyCode.LeftShift)) ? fastMoveSpeed : normalMoveSpeed;
        scrollValue = Input.GetAxis("Mouse ScrollWheel");
    }

    private void CameraMovement()
    {
        if (movementInput != Vector2.zero)
        {
            movement = movementInput * movementSpeed * Time.deltaTime;
            if (movementInput.y > 0) targetPosition += transform.forward * movement.y;
            if (movementInput.y < 0) targetPosition += transform.forward * movement.y;
            if (movementInput.x > 0) targetPosition += transform.right * movement.x;
            if (movementInput.x < 0) targetPosition += transform.right * movement.x;
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref moveVelocity, moveSmoothTime);
    }

    private void CameraRotation()
    {
        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;
            rotationDelta = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;
            rotationAmount = (invertRotation) ? (rotationDelta.x / 5f) : (-rotationDelta.x / 5f);
            targetRotation *= Quaternion.Euler(Vector3.up * rotationSpeed * rotationAmount);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime * 10);
    }

    private void CameraZoom()
    {
        if (IsMouseOverGameWindow)
        {
            if (scrollValue != 0)
            {
                targetCameraDistance = (scrollValue > 0) ? targetCameraDistance - zoomStep : targetCameraDistance + zoomStep;
                targetCameraDistance = Mathf.Clamp(targetCameraDistance, minCameraDistance, maxCameraDistance);
            }
        }

        targetZoomPos = new Vector3(cam.transform.localPosition.x, targetCameraDistance, -targetCameraDistance);
        cam.transform.localPosition = Vector3.SmoothDamp(cam.transform.localPosition, targetZoomPos, ref cameraZoomVelocity, zoomSmoothTime);
    }

    private void MouseDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ray = cam.ScreenPointToRay(Input.mousePosition);
            if (mouseDragPlane.Raycast(ray, out planeEntry)) dragStartPosition = ray.GetPoint(planeEntry);
            isDragging = true;
        }
        if (Input.GetMouseButton(1))
        {
            ray = cam.ScreenPointToRay(Input.mousePosition);
            if (mouseDragPlane.Raycast(ray, out planeEntry)) dragCurrentPosition = ray.GetPoint(planeEntry);
            targetPosition = transform.position + dragStartPosition - dragCurrentPosition;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }
    }
    private Vector2 GetMovementInput()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        return movementInput.normalized;
    }
}
