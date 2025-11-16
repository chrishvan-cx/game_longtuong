using UnityEngine;

[ExecuteInEditMode]
public class CanvasSticky : MonoBehaviour
{
    public enum AnchorPreset
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    [Header("Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private AnchorPreset anchor = AnchorPreset.TopLeft;
    [SerializeField] private Vector2 offset = new Vector2(10, -10);  // pixel offset
    [SerializeField] private bool scaleWithDistance = true;
    [SerializeField] private float scaleMultiplier = 0.1f; // adjust scale
    [SerializeField] private float baseScale = 1f;

    [Header("Screen Scale Settings")]
    [SerializeField] private bool scaleWithScreen = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080); // Reference screen size

    private Canvas canvas;
    private RectTransform rectTransform;
    private float currentScale = 1f; // Track the current scale

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        UpdateCanvasPosition();
    }

    private void UpdateCanvasPosition()
    {
        if (targetCamera == null)
            return;

        // Calculate scale first
        float distanceScale = 1f;
        if (scaleWithDistance)
        {
            // Use a temporary position to calculate distance
            Vector3 tempScreenPoint = new Vector3(Screen.width / 2f, Screen.height / 2f, targetCamera.nearClipPlane + 0.1f);
            Vector3 tempWorldPoint = targetCamera.ScreenToWorldPoint(tempScreenPoint);
            float distance = Vector3.Distance(targetCamera.transform.position, tempWorldPoint);
            distanceScale = distance * scaleMultiplier;
        }

        // Calculate screen scale
        float screenScale = 1f;
        if (scaleWithScreen)
        {
            // Scale based on screen width (you can also use height or average of both)
            screenScale = Screen.width / referenceResolution.x;

            // Alternative: scale based on the smaller ratio to fit both dimensions
            // float widthRatio = Screen.width / referenceResolution.x;
            // float heightRatio = Screen.height / referenceResolution.y;
            // screenScale = Mathf.Min(widthRatio, heightRatio);
        }

        // Combine both scales
        currentScale = distanceScale * screenScale * baseScale;

        // Now get anchor point with the correct scale
        Vector3 screenPoint = GetAnchorScreenPoint() + new Vector3(offset.x, offset.y, targetCamera.nearClipPlane + 0.1f);

        // Convert to world-space
        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(screenPoint);

        transform.position = worldPoint;
        transform.localScale = new Vector3(currentScale, currentScale, transform.localScale.z);
    }

    private Vector3 GetAnchorScreenPoint()
    {
        float x = 0f;
        float y = 0f;

        // Use the SCALED canvas dimensions
        float w = rectTransform.rect.width * currentScale;
        float h = rectTransform.rect.height * currentScale;
        Vector2 pivot = rectTransform.pivot;

        switch (anchor)
        {
            case AnchorPreset.TopLeft:
                x = 0 + w * pivot.x;
                y = Screen.height - h * (1 - pivot.y);
                break;

            case AnchorPreset.TopCenter:
                x = Screen.width / 2f;
                y = Screen.height - h * (1 - pivot.y);
                break;

            case AnchorPreset.TopRight:
                x = Screen.width - w * (1 - pivot.x);
                y = Screen.height - h * (1 - pivot.y);
                break;

            case AnchorPreset.CenterLeft:
                x = 0 + w * pivot.x;
                y = Screen.height / 2f;
                break;

            case AnchorPreset.Center:
                x = Screen.width / 2f;
                y = Screen.height / 2f;
                break;

            case AnchorPreset.CenterRight:
                x = Screen.width - w * (1 - pivot.x);
                y = Screen.height / 2f;
                break;

            case AnchorPreset.BottomLeft:
                x = 0 + w * pivot.x;
                y = 0 + h * pivot.y;
                break;

            case AnchorPreset.BottomCenter:
                x = Screen.width / 2f;
                y = 0 + h * pivot.y;
                break;

            case AnchorPreset.BottomRight:
                x = Screen.width - w * (1 - pivot.x);
                y = 0 + h * pivot.y;
                break;
        }

        return new Vector3(x, y, 0);
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }
#endif
}