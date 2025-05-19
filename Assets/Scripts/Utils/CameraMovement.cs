using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    [SerializeField]
    private Camera cam;

    private Vector3 dragOrigin;

    [SerializeField]
    private float zoomStep, zoomMin; 
        
        
    private float zoomMax;



    [SerializeField] 
    private SpriteRenderer mapSprite;

    private float mapMinX, mapMaxX, mapMinY, mapMaxY;

    private float initialPinchDistance;
    private bool isPinching = false;

    private void Awake()
    {
        mapMinX = mapSprite.transform.position.x - mapSprite.bounds.size.x / 2f;
        mapMaxX = mapSprite.transform.position.x + mapSprite.bounds.size.x / 2f;
        mapMinY = mapSprite.transform.position.y - mapSprite.bounds.size.y / 2f;
        mapMaxY = mapSprite.transform.position.y + mapSprite.bounds.size.y / 2f;

        float maxHeightZoom = mapSprite.bounds.size.y / 2f;
        float maxWidthZoom = mapSprite.bounds.size.x / (2f * cam.aspect);

        // Use the larger value to ensure the entire map fits
        zoomMax = Mathf.Min(maxHeightZoom, maxWidthZoom);

    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (Input.touchCount)
        {
            case 1:
                PanCamera();
                isPinching = false;
                break;
            case 2:
                Zoom();
                break;
        }



       
    }

    private void PanCamera ()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                dragOrigin = cam.ScreenToWorldPoint(touch.position);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(touch.position);
                cam.transform.position = ClampCamera(cam.transform.position += difference);
            }
        }

    }

    private void Zoom()
    {

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            isPinching = true;
            initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
        }
        else if (isPinching && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
        {
            // Get the center point between the two fingers
            Vector2 touchCenter = (touch0.position + touch1.position) * 0.5f;

            // Convert touch position from screen space to world space
            Vector3 worldPointBeforeZoom = cam.ScreenToWorldPoint(new Vector3(touchCenter.x, touchCenter.y, 0));

            // Calculate and apply new zoom
            float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
            float pinchDelta = currentPinchDistance - initialPinchDistance;
            float newSize = Mathf.Clamp(cam.orthographicSize - (pinchDelta * zoomStep), zoomMin, zoomMax);
            cam.orthographicSize = newSize;

            // Get the world position of the touch center after zooming
            Vector3 worldPointAfterZoom = cam.ScreenToWorldPoint(new Vector3(touchCenter.x, touchCenter.y, 0));

            // Move the camera to keep the touched point in the same position
            Vector3 worldDelta = worldPointAfterZoom - worldPointBeforeZoom;
            cam.transform.position = ClampCamera(cam.transform.position -= worldDelta);

            initialPinchDistance = currentPinchDistance;
        }

    }


    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        float minX = mapMinX + camWidth / 2f;
        float maxX = mapMaxX - camWidth / 2f;
        float minY = mapMinY + camHeight / 2f;
        float maxY = mapMaxY - camHeight / 2f;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPosition.z);
    }
}
