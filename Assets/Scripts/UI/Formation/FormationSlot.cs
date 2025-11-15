using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Formation Slot - Drop zone for hero cards in formation grid
/// Handles drop events, highlighting, and click events
/// </summary>
public class FormationSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum HighlightState
    {
        None,
        Valid,
        Invalid
    }

    [Header("Slot Configuration")]
    public HeroColumn column = HeroColumn.FrontLine;
    public int row = 1;

    [Header("Visual References")]
    public Image background;
    public Image highlightValid;
    public Image highlightInvalid;
    public Transform occupantAnchor;

    [Header("Highlight Colors")]
    public Color validColor = new Color(0.2f, 0.8f, 1f, 0.5f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    private FormationWindow owner;
    private DraggableHeroCard occupantCard;
    private bool isHighlightingEnabled = false;

    public void Initialize(FormationWindow window)
    {
        owner = window;
        SetHighlightState(HighlightState.None);
        Debug.Log($"[SLOT] {gameObject.name} initialized, owner={(owner != null ? "set" : "null")}");
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableHeroCard dragged = eventData.pointerDrag?.GetComponent<DraggableHeroCard>();
        
        Debug.Log($"[SLOT] OnDrop triggered on {gameObject.name}, dragged={(dragged != null ? dragged.GetHero()?.heroName : "null")}, owner={(owner != null ? owner.name : "NULL")}");
        
        if (dragged == null || owner == null)
        {
            Debug.LogWarning($"[SLOT] Drop failed: dragged={dragged != null}, owner={owner != null}, owner object={(owner != null ? owner.name : "destroyed or null")}");
            return;
        }

        bool success = owner.TryAssign(dragged.GetHero(), this, allowSwap: true);
        
        Debug.Log($"[SLOT] TryAssign result: {success}");
        
        if (success)
        {
            dragged.MarkDropAccepted();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHighlightingEnabled)
            return;

        // Additional hover feedback can be added here
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHighlightingEnabled)
            return;

        // Reset hover feedback
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null && !IsEmpty())
        {
            owner.OnSlotHeroClicked(this);
        }
    }

    public void SetHighlightState(HighlightState state)
    {
        isHighlightingEnabled = (state != HighlightState.None);

        if (highlightValid != null)
        {
            highlightValid.enabled = (state == HighlightState.Valid);
            if (state == HighlightState.Valid)
            {
                highlightValid.color = validColor;
            }
        }

        if (highlightInvalid != null)
        {
            highlightInvalid.enabled = (state == HighlightState.Invalid);
            if (state == HighlightState.Invalid)
            {
                highlightInvalid.color = invalidColor;
            }
        }
    }

    public void SetOccupant(DraggableHeroCard card)
    {
        occupantCard = card;
        
        if (card != null && occupantAnchor != null)
        {
            card.transform.SetParent(occupantAnchor);
            RectTransform rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }
    }

    public void ClearOccupant()
    {
        if (occupantCard != null)
        {
            // Don't destroy, just unlink - FormationWindow manages card lifecycle
            occupantCard = null;
        }
    }

    public DraggableHeroCard GetOccupant()
    {
        return occupantCard;
    }

    public bool IsEmpty()
    {
        return occupantCard == null;
    }
}
