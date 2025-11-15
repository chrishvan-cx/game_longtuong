using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Draggable Hero Card - Represents a hero in the formation UI
/// Can be dragged from hero list to formation slots or between slots
/// Double-click on slot cards to remove them from formation
/// </summary>
public class DraggableHeroCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Visual References")]
    public Image portrait;
    public TMP_Text heroName;
    public GameObject deployedBadge; // Optional "Deployed" badge
    public TMP_Text statusDeploy; // "Deployed" text for sidebar cards

    private HeroData hero;
    private FormationWindow owner;
    private FormationSlot currentSlot; // null if in list
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPosition;
    private bool dropAccepted = false;
    private RectTransform rectTransform;
    private Canvas canvas;
    private GameObject dragClone; // Visual clone during drag
    private bool isDeployed = false; // Track if this hero is deployed

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Find the canvas for proper drag positioning
        canvas = GetComponentInParent<Canvas>();
    }

    public void Bind(HeroData heroData, FormationWindow window, FormationSlot slot)
    {
        hero = heroData;
        owner = window;
        currentSlot = slot;

        // Update visuals
        if (portrait != null && hero != null && hero.sprite != null)
        {
            portrait.sprite = hero.sprite;
        }

        if (heroName != null && hero != null)
        {
            heroName.text = hero.heroName;
        }

        // Check if hero is deployed (either in a slot, or check with owner)
        if (slot != null)
        {
            // This card is IN a formation slot
            isDeployed = true;
        }
        else if (owner != null && hero != null)
        {
            // This is a sidebar card - check if hero is deployed elsewhere
            isDeployed = owner.IsHeroDeployed(hero);
        }
        else
        {
            isDeployed = false;
        }

        // Update deployed badge (for slot cards)
        if (deployedBadge != null)
        {
            deployedBadge.SetActive(slot != null);
        }

        // Update status deploy text (for sidebar cards)
        if (statusDeploy != null)
        {
            // Only show "Deployed" text on sidebar cards that are deployed
            bool showDeployedText = (slot == null && isDeployed);
            statusDeploy.gameObject.SetActive(showDeployedText);
            if (showDeployedText)
            {
                statusDeploy.text = "Deployed";
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null || hero == null)
            return;

        // Store original state
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Read current slot from owner
        currentSlot = owner.GetSlotOf(hero);

        // Prevent dragging if this is a sidebar card for a deployed hero
        if (currentSlot == null && isDeployed)
        {
            Debug.Log($"[DRAG] Cannot drag deployed hero {hero.heroName} from sidebar");
            eventData.pointerDrag = null; // Cancel the drag
            return;
        }
        
        Debug.Log($"[DRAG] Begin drag: {hero.heroName}, currentSlot={currentSlot?.name ?? "null (sidebar)"}");

        // Enable slot highlights
        owner.EnableSlotHighlights(true, hero);

        bool isFromSidebar = (currentSlot == null);

        if (isFromSidebar)
        {
            // Create visual clone for dragging (sidebar card stays in place)
            dragClone = Instantiate(gameObject, owner.GetDragLayer());
            RectTransform cloneRect = dragClone.GetComponent<RectTransform>();
            cloneRect.position = rectTransform.position;
            cloneRect.sizeDelta = rectTransform.sizeDelta;
            cloneRect.localScale = transform.localScale;

            // Make clone semi-transparent
            CanvasGroup cloneGroup = dragClone.GetComponent<CanvasGroup>();
            if (cloneGroup != null)
            {
                cloneGroup.alpha = 0.7f;
                cloneGroup.blocksRaycasts = false;
            }

            // Disable dragging on the clone itself
            DraggableHeroCard cloneDrag = dragClone.GetComponent<DraggableHeroCard>();
            if (cloneDrag != null)
            {
                cloneDrag.enabled = false;
            }

            Debug.Log($"[DRAG] Created visual clone for sidebar card");
        }
        else
        {
            // Card from slot - move the actual card for dragging
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;

            if (owner.GetDragLayer() != null)
            {
                transform.SetParent(owner.GetDragLayer());
                Debug.Log($"[DRAG] Moved slot card to DragLayer");
            }
        }

        dropAccepted = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null)
            return;

        // Determine which object to move (clone for sidebar, self for slot)
        RectTransform targetRect = (dragClone != null) ? dragClone.GetComponent<RectTransform>() : rectTransform;
        CanvasGroup targetGroup = (dragClone != null) ? dragClone.GetComponent<CanvasGroup>() : canvasGroup;

        // Ensure we're not blocking raycasts
        if (targetGroup != null)
        {
            targetGroup.blocksRaycasts = false;
        }

        // Follow cursor
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            owner.GetDragLayer() as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );

        targetRect.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (owner == null)
            return;

        Debug.Log($"[DRAG] End drag: {hero.heroName}, dropAccepted={dropAccepted}, currentSlot={currentSlot?.name ?? "null"}");

        // Disable highlights
        owner.EnableSlotHighlights(false);

        // Restore opacity and raycast blocking
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Check if this card is from sidebar (currentSlot == null means from list)
        bool isFromSidebar = (currentSlot == null);

        if (isFromSidebar)
        {
            Debug.Log($"[DRAG] Card from sidebar - returning to original position");
            // Sidebar cards ALWAYS return to original position
            // FormationWindow creates a NEW card in the slot
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;

            // Destroy the drag clone
            if (dragClone != null)
            {
                Destroy(dragClone);
                dragClone = null;
                Debug.Log($"[DRAG] Destroyed drag clone");
            }
        }
        else
        {
            // Card from slot - if drop not accepted, unassign it
            if (!dropAccepted)
            {
                Debug.Log($"[DRAG] Card from slot - drop not accepted, unassigning");
                owner.Unassign(currentSlot, returnToList: false);
                // This card will be destroyed by Unassign
            }
            else
            {
                Debug.Log($"[DRAG] Card from slot - drop accepted, FormationWindow handles it");
            }
            // If dropAccepted, FormationWindow handles swap/move
        }

        // Reset flag
        dropAccepted = false;
    }

    public void MarkDropAccepted()
    {
        dropAccepted = true;
        Debug.Log($"[DRAG] Drop accepted for {hero.heroName}");
    }

    public HeroData GetHero()
    {
        return hero;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle double-click on cards that are in formation slots
        if (eventData.clickCount == 2 && currentSlot != null && owner != null)
        {
            Debug.Log($"[DOUBLE-CLICK] Removing {hero.heroName} from formation slot");
            owner.Unassign(currentSlot, returnToList: false);
        }
    }
}
