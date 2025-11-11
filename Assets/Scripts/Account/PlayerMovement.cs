using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Click-to-move player movement for home screen
/// Player walks to clicked position on the ground
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the player moves")]
    public float moveSpeed = 3f;

    [Tooltip("How close to the target before stopping")]
    public float stoppingDistance = 0.1f;

    [Header("Ground Layer")]
    [Tooltip("Layer for ground that can be clicked (set this in Inspector)")]
    public LayerMask groundLayer;

    [Header("NPC Layer")]
    [Tooltip("Layer for NPCs that can be clicked")]
    public LayerMask npcLayer;

    [Header("Optional - Visual Feedback")]
    [Tooltip("Optional sprite that shows where player will move")]
    public GameObject moveTargetIndicator;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        targetPosition = transform.position;

        // Hide indicator initially
        if (moveTargetIndicator != null)
        {
            moveTargetIndicator.SetActive(false);
        }
    }

    void Update()
    {
        HandleMouseInput();
        MoveToTarget();
    }

    private void HandleMouseInput()
    {
        // Check for mouse click (left button)
        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Priority 1: Check if clicking on NPC (before UI check!)
            RaycastHit2D npcHit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, npcLayer);
            if (npcHit.collider != null)
            {
                QuestGiverNPC npc = npcHit.collider.GetComponentInParent<QuestGiverNPC>();
                if (npc != null)
                {
                    npc.OnNPCClicked();
                    return; // NPC handles interaction, don't treat as ground click
                }
            }

            // Priority 2: Check if clicking on UI - if so, don't move
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return; // Clicked on UI, ignore movement
            }

            // Priority 3: Check if clicking on ground
            RaycastHit2D groundHit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, groundLayer);

            if (groundHit.collider != null)
            {
                // Set new target position
                targetPosition = groundHit.point;
                isMoving = true;

                // Show indicator at target position
                if (moveTargetIndicator != null)
                {
                    moveTargetIndicator.transform.position = targetPosition;
                    moveTargetIndicator.SetActive(true);
                }
            }
            else
            {
            }
        }
    }

    private void MoveToTarget()
    {
        if (!isMoving) return;

        // Calculate distance to target
        float distance = Vector2.Distance(transform.position, targetPosition);

        // Check if we've reached the target
        if (distance <= stoppingDistance)
        {
            isMoving = false;

            // Hide indicator
            if (moveTargetIndicator != null)
            {
                moveTargetIndicator.SetActive(false);
            }

            return;
        }

        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Optional: Flip sprite based on movement direction
        FlipSprite(direction.x);
    }

    private void FlipSprite(float directionX)
    {
        // Flip the sprite to face movement direction
        if (directionX != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = directionX > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    // Optional: Public method to stop movement
    public void StopMovement()
    {
        isMoving = false;
        if (moveTargetIndicator != null)
        {
            moveTargetIndicator.SetActive(false);
        }
    }

    // Optional: Public method to check if player is moving
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// Move to a specific position (called by NPCs or other scripts)
    /// </summary>
    public void MoveToPosition(Vector3 position)
    {
        targetPosition = position;
        isMoving = true;

        // Show indicator at target position
        if (moveTargetIndicator != null)
        {
            moveTargetIndicator.transform.position = targetPosition;
            moveTargetIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// Check if pointer is over an interactive UI element (buttons, panels, etc.)
    /// </summary>
    private bool IsPointerOverUIElement()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        // Debug: Check for active canvases and graphic raycasters
        var allRaycasters = FindObjectsOfType<UnityEngine.UI.GraphicRaycaster>();
        foreach (var raycaster in allRaycasters)
        {
            Canvas canvas = raycaster.GetComponent<Canvas>();
            string renderMode = canvas != null ? canvas.renderMode.ToString() : "Unknown";
        }

        // Create pointer event data
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        // Raycast to find UI elements using EventSystem
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Fallback: Manually check World Space canvases using Physics2D raycast
        if (results.Count == 0)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Get ALL hits without layer filter to see everything
            RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, Mathf.Infinity);

            // Sort by distance to see what's in front
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {

                // Check if this is a UI element on a World Space canvas
                Canvas canvas = hit.collider.GetComponentInParent<Canvas>();

                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {

                    // Check for interactive components
                    Selectable selectable = hit.collider.GetComponent<Selectable>();
                    Selectable parentSelectable = hit.collider.GetComponentInParent<Selectable>();
                    // Check for any UI graphic with raycast target
                    var graphic = hit.collider.GetComponent<UnityEngine.UI.Graphic>();

                }
            }
        }

        // Check if any interactive UI elements were hit
        foreach (var result in results)
        {
            GameObject obj = result.gameObject;

            // Check if this is an interactive UI element:
            // 1. Has a Button, Toggle, Dropdown, InputField, Scrollbar, Slider, etc. (Selectable)
            Selectable selectable = obj.GetComponent<Selectable>();

            // Check parent for Selectable (sometimes raycast hits child Text/Image of button)
            selectable = obj.GetComponentInParent<Selectable>();

            // 2. Has an Image/RawImage with Raycast Target enabled (any clickable UI)
            Image img = obj.GetComponent<Image>();

            RawImage rawImg = obj.GetComponent<RawImage>();
            // 3. Has Text with Raycast Target enabled
            var text = obj.GetComponent<UnityEngine.UI.Text>();
        }

        return false;
    }

    // Debug: Draw movement target in Scene view
    private void OnDrawGizmos()
    {
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
