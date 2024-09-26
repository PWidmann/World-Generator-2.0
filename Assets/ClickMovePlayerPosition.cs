using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickMovePlayerPosition : MonoBehaviour
{
    [SerializeField] private Transform startPosition;

    private LayerMask waterLayerMask;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        waterLayerMask = LayerMask.NameToLayer("Water");
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        { 
            Vector3 mousePosition = Input.mousePosition;

            // Create a ray from the camera through the mouse position
            Ray ray = cam.ScreenPointToRay(mousePosition);

            // Variable to store raycast hit information
            RaycastHit hitInfo;

            // Perform the raycast
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, waterLayerMask))
            {
                startPosition.position = hitInfo.point;
            }
        }

    }
}
