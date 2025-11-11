using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways] // works in edit & play mode
public class HomeGround : MonoBehaviour
{
    [Range(0f, 1f)]
    public float heightRatio = 0.66f; // 2/3 of screen height

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float fullHeight = 2f * cam.orthographicSize;
        float width = fullHeight * cam.aspect;
        float groundHeight = fullHeight * heightRatio;

        // Resize ground to fill width and partial height
        transform.localScale = new Vector3(width, groundHeight, 1);

        // Position ground so its bottom aligns with bottom of camera view
        float camBottom = cam.transform.position.y - fullHeight / 2f;
        float groundCenterY = camBottom + groundHeight / 2f;

        transform.position = new Vector3(cam.transform.position.x, groundCenterY, transform.position.z);
    }
}
