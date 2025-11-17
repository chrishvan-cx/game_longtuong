using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Formation Window - Manages hero deployment and positioning
/// Left: Hero list (vertical scroll)
/// Center: Formation grid (3x3 slots: FrontLine/MidLine/BackLine × Row 1-3)
/// Right: Selected hero skills (vertical scroll)
/// </summary>
public class FormationWindow : MonoBehaviour
{
    public const int MaxDeployed = 5;

    [Header("Panel References")]
    public GameObject panelRoot;
    public TMP_Text titleText;
    public Button closeButton;
    public TMP_Text deployedCounterText;
    public Transform dragLayer; // Top-level canvas layer for dragging

    [Header("Left Sidebar - Hero List")]
    public ScrollRect heroListScroll;
    public Transform heroListContent;

    [Header("Center - Formation Grid")]
    public Transform formationGridRoot;
    public GameObject heroUnitPrefab; // For both sidebar and formation slots
    public FormationSlot[] allSlots;

    [Header("Right Sidebar - Skills")]
    public ScrollRect skillScroll;
    public Transform skillContent;
    public Image selectedHeroIcon;
    public TMP_Text selectedHeroName;
    public GameObject skillItemPrefab; // Prefab with Image (icon) + TMP_Text (name)

    [Header("Animation")]
    public float fadeInDuration = 0.4f;
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("HeroUnit Display")]
    public float heroUnitUIScale = 90f;
    public string heroUnitSortingLayer = "UI_Top";
    public int heroUnitSortOrder = 100;

    // State tracking
    private Dictionary<FormationSlot, HeroData> slotToHero = new Dictionary<FormationSlot, HeroData>();
    private Dictionary<HeroData, FormationSlot> heroToSlot = new Dictionary<HeroData, FormationSlot>();
    private Dictionary<HeroData, DraggableHeroCard> heroToListCard = new Dictionary<HeroData, DraggableHeroCard>();
    private int deployedCount = 0;
    private CanvasGroup canvasGroup;
    private HeroData currentSelectedHero;

    void Awake()
    {
        // Get or add CanvasGroup for fade animation
        if (panelRoot != null)
        {
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        // Find all slots if not manually assigned
        if (formationGridRoot != null && (allSlots == null || allSlots.Length == 0))
        {
            allSlots = formationGridRoot.GetComponentsInChildren<FormationSlot>();
            Debug.Log($"[FORMATION] Found {allSlots.Length} slots in FormationGrid");
        }
        else if (formationGridRoot == null)
        {
            Debug.LogWarning("[FORMATION] formationGridRoot is NULL!");
        }

        // Initialize slots with reference to this window
        if (allSlots != null && allSlots.Length > 0)
        {
            Debug.Log($"[FORMATION] Initializing {allSlots.Length} slots");
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    slot.Initialize(this);
                }
            }
        }
        else
        {
            Debug.LogWarning("[FORMATION] No slots to initialize!");
        }

        // Hide initially
        Hide();
    }

    public void Show()
    {
        Debug.Log("FormationWindow.Show() called!");

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Debug.Log("PanelRoot activated");
        }
        else
        {
            Debug.LogWarning("PanelRoot is null!");
        }

        // Set title
        if (titleText != null)
        {
            titleText.text = "Formation";
        }

        // Build UI from PlayerData
        BuildFromPlayerData();

        // Fade in
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ClearUI();
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeInCurve.Evaluate(elapsed / fadeInDuration);
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void BuildFromPlayerData()
    {
        ClearUI();

        if (PlayerData.Instance == null || PlayerData.Instance.playerTeam == null)
        {
            Debug.LogWarning("PlayerData or playerTeam is null!");
            return;
        }

        PopulateHeroList();
        PopulateGridFromSaved();
        UpdateCounter();
        ClearSelectedHero();
    }

    private void PopulateHeroList()
    {
        if (heroListContent == null || heroUnitPrefab == null)
        {
            Debug.LogWarning("heroListContent or heroUnitPrefab is null!");
            return;
        }

        Debug.Log($"Populating hero list with {PlayerData.Instance.playerTeam.Count} heroes");

        foreach (var hero in PlayerData.Instance.playerTeam)
        {
            if (hero == null)
            {
                Debug.LogWarning("Null hero in playerTeam!");
                continue;
            }

            Debug.Log($"Creating card for hero: {hero.heroName}");
            GameObject cardObj = Instantiate(heroUnitPrefab, heroListContent);
            cardObj.transform.localScale = Vector3.one * heroUnitUIScale;

            HeroUnit heroUnit = cardObj.GetComponent<HeroUnit>();
            if (heroUnit != null)
            {
                heroUnit.Setup(hero, 0);
                heroUnit.SetViewMode(HeroViewMode.FaceIcon);
                SetHeroUnitSortOrder(heroUnit);
            }

            DraggableHeroCard card = cardObj.GetComponent<DraggableHeroCard>();
            if (card == null)
            {
                card = cardObj.AddComponent<DraggableHeroCard>();
            }

            if (card != null)
            {
                card.Bind(hero, this, null);
                heroToListCard[hero] = card;
            }
            else
            {
                Debug.LogWarning($"Failed to add DraggableHeroCard component!");
            }
        }

        Debug.Log($"Hero list populated. Total cards created: {heroToListCard.Count}");
    }

    private void PopulateGridFromSaved()
    {
        if (allSlots == null)
            return;

        // Clear all slots first
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.ClearOccupant();
            }
        }

        slotToHero.Clear();
        heroToSlot.Clear();
        deployedCount = 0;

        // ✅ Get deployed heroes (row >= 1)
        List<HeroData> deployedHeroes = PlayerData.Instance.GetDeployedHeroes();

        foreach (var hero in deployedHeroes)
        {
            // Find matching slot using heroRole (not position!)
            FormationSlot targetSlot = FindSlot(hero.heroRole, hero.row);
            if (targetSlot != null)
            {
                AssignHeroToSlot(hero, targetSlot, false);
            }
        }

        RefreshSidebarCards();
    }

    private FormationSlot FindSlot(HeroColumn column, int row)
    {
        if (allSlots == null)
            return null;

        foreach (var slot in allSlots)
        {
            if (slot != null && slot.column == column && slot.row == row)
            {
                return slot;
            }
        }
        return null;
    }

    public bool IsHeroDeployed(HeroData hero)
    {
        return heroToSlot.ContainsKey(hero);
    }

    private void RefreshSidebarCards()
    {
        // Update all sidebar cards to show deployed status
        foreach (var kvp in heroToListCard)
        {
            HeroData hero = kvp.Key;
            DraggableHeroCard card = kvp.Value;

            if (card != null)
            {
                // Re-bind to update deployed status
                card.Bind(hero, this, null);
            }
        }
    }

    public bool TryAssign(HeroData hero, FormationSlot targetSlot, bool allowSwap = true)
    {
        if (hero == null || targetSlot == null)
            return false;

        // Column restriction: hero can only go to slots of their role type
        HeroColumn heroColumn;
        if (heroToSlot.ContainsKey(hero))
        {
            // Hero is already deployed - use their current slot's column
            heroColumn = heroToSlot[hero].column;
        }
        else
        {
            // Hero from sidebar - use their permanent role
            heroColumn = hero.heroRole;
        }

        // ✅ Column restriction: hero can only go to slots matching their heroRole
        if (targetSlot.column != hero.heroRole)
        {
            Debug.Log($"Cannot place {hero.heroName} (role: {hero.heroRole}) in {targetSlot.column} slot!");
            return false;
        }

        // Check if hero is already in this slot
        if (heroToSlot.ContainsKey(hero) && heroToSlot[hero] == targetSlot)
        {
            return true; // Already there, no-op
        }

        // Check if hero is in another slot (moving)
        bool isMoving = heroToSlot.ContainsKey(hero);
        FormationSlot currentSlot = isMoving ? heroToSlot[hero] : null;

        // Check if target slot is occupied
        bool targetOccupied = slotToHero.ContainsKey(targetSlot);
        HeroData targetHero = targetOccupied ? slotToHero[targetSlot] : null;

        // Logic: placing new hero (not moving)
        if (!isMoving)
        {
            if (deployedCount >= MaxDeployed && !targetOccupied)
            {
                // Cannot place new hero, max reached
                Debug.Log("Cannot deploy more heroes! Max reached.");
                return false;
            }

            if (targetOccupied && allowSwap)
            {
                // Swap: new hero takes slot, target hero goes back to list
                UnassignHeroFromSlot(targetSlot, true);
                AssignHeroToSlot(hero, targetSlot, true);
                return true;
            }
            else if (!targetOccupied)
            {
                // Place in empty slot
                AssignHeroToSlot(hero, targetSlot, true);
                return true;
            }
            else
            {
                return false; // Occupied and no swap
            }
        }
        else // Moving from another slot
        {
            if (targetOccupied && allowSwap)
            {
                // Swap the two heroes
                SwapHeroes(currentSlot, targetSlot);
                return true;
            }
            else if (!targetOccupied)
            {
                // Move to empty slot
                UnassignHeroFromSlot(currentSlot, false);
                AssignHeroToSlot(hero, targetSlot, true);
                return true;
            }
            else
            {
                return false; // Occupied and no swap
            }
        }
    }

    private void AssignHeroToSlot(HeroData hero, FormationSlot slot, bool updateListCard)
    {
        // Update mappings
        slotToHero[slot] = hero;
        heroToSlot[hero] = slot;

        // ✅ Update hero data (heroRole stays the same, only row changes)
        hero.row = slot.row;

        // Create HeroUnit in the slot for battle preview
        GameObject unitObj = Instantiate(heroUnitPrefab, slot.occupantAnchor);
        unitObj.transform.localScale = Vector3.one * heroUnitUIScale;

        // Initialize HeroUnit with data
        HeroUnit heroUnit = unitObj.GetComponent<HeroUnit>();
        if (heroUnit != null)
        {
            heroUnit.data = hero;
            heroUnit.Setup(hero, 0);
            heroUnit.SetViewMode(HeroViewMode.Interface);
            SetHeroUnitSortOrder(heroUnit);
        }

        // Add DraggableHeroCard component for drag functionality
        DraggableHeroCard dragCard = unitObj.GetComponent<DraggableHeroCard>();
        if (dragCard == null)
        {
            dragCard = unitObj.AddComponent<DraggableHeroCard>();
        }

        // Set up drag card
        if (dragCard != null)
        {
            dragCard.Bind(hero, this, slot);
        }

        slot.SetOccupant(dragCard);

        deployedCount++;
        UpdateCounter();

        // Refresh sidebar cards to update deployed status
        if (updateListCard)
        {
            RefreshSidebarCards();
        }
    }

    private void UnassignHeroFromSlot(FormationSlot slot, bool returnToList)
    {
        if (!slotToHero.ContainsKey(slot))
            return;

        HeroData hero = slotToHero[slot];

        // Update mappings
        slotToHero.Remove(slot);
        heroToSlot.Remove(hero);

        // ✅ Update hero data (heroRole stays the same, row = 0 = undeployed)
        hero.row = 0;

        // Destroy the card in the slot
        DraggableHeroCard slotCard = slot.GetOccupant();
        if (slotCard != null)
        {
            Destroy(slotCard.gameObject);
        }
        slot.ClearOccupant();

        deployedCount--;
        UpdateCounter();

        // Refresh sidebar cards to update deployed status
        RefreshSidebarCards();

        // Clear selection if this hero was selected
        if (currentSelectedHero == hero)
        {
            ClearSelectedHero();
        }
    }

    public void Unassign(FormationSlot slot, bool returnToList = true)
    {
        UnassignHeroFromSlot(slot, returnToList);
    }

    private void SwapHeroes(FormationSlot slotA, FormationSlot slotB)
    {
        if (!slotToHero.ContainsKey(slotA) || !slotToHero.ContainsKey(slotB))
            return;

        HeroData heroA = slotToHero[slotA];
        HeroData heroB = slotToHero[slotB];

        // Destroy old cards
        DraggableHeroCard cardA = slotA.GetOccupant();
        DraggableHeroCard cardB = slotB.GetOccupant();

        if (cardA != null) Destroy(cardA.gameObject);
        if (cardB != null) Destroy(cardB.gameObject);

        slotA.ClearOccupant();
        slotB.ClearOccupant();

        // Temporarily remove both from mappings
        slotToHero.Remove(slotA);
        slotToHero.Remove(slotB);
        heroToSlot.Remove(heroA);
        heroToSlot.Remove(heroB);

        // ✅ Update hero data - ONLY change row, heroRole stays the same
        heroA.row = slotB.row;
        heroB.row = slotA.row;

        // Update mappings
        slotToHero[slotA] = heroB;
        slotToHero[slotB] = heroA;
        heroToSlot[heroA] = slotB;
        heroToSlot[heroB] = slotA;

        // Create new HeroUnits in swapped positions
        // Hero A goes to slot B
        GameObject unitA = Instantiate(heroUnitPrefab, slotB.occupantAnchor);
        unitA.transform.localScale = Vector3.one * heroUnitUIScale;
        HeroUnit heroUnitA = unitA.GetComponent<HeroUnit>();
        if (heroUnitA != null)
        {
            heroUnitA.data = heroA;
            heroUnitA.Setup(heroA, 0);
            heroUnitA.SetViewMode(HeroViewMode.Interface);
            SetHeroUnitSortOrder(heroUnitA);
        }
        DraggableHeroCard dragA = unitA.GetComponent<DraggableHeroCard>();
        if (dragA == null)
        {
            dragA = unitA.AddComponent<DraggableHeroCard>();
        }
        if (dragA != null)
        {
            dragA.Bind(heroA, this, slotB);
            slotB.SetOccupant(dragA);
        }

        // Hero B goes to slot A
        GameObject unitB = Instantiate(heroUnitPrefab, slotA.occupantAnchor);
        unitB.transform.localScale = Vector3.one * heroUnitUIScale;
        HeroUnit heroUnitB = unitB.GetComponent<HeroUnit>();
        if (heroUnitB != null)
        {
            heroUnitB.data = heroB;
            heroUnitB.Setup(heroB, 0);
            heroUnitB.SetViewMode(HeroViewMode.Interface);
            SetHeroUnitSortOrder(heroUnitB);
        }
        DraggableHeroCard dragB = unitB.GetComponent<DraggableHeroCard>();
        if (dragB == null)
        {
            dragB = unitB.AddComponent<DraggableHeroCard>();
        }
        if (dragB != null)
        {
            dragB.Bind(heroB, this, slotA);
            slotA.SetOccupant(dragB);
        }

        // Refresh sidebar cards (in case of sidebar-to-slot swap)
        RefreshSidebarCards();
    }

    private void RefreshListCardStatus(HeroData hero)
    {
        // Update the list card's visual state (deployed badge, disable drag, etc.)
        // This will be handled by DraggableHeroCard.Bind() method
    }

    public void UpdateCounter()
    {
        if (deployedCounterText != null)
        {
            deployedCounterText.text = $"Deployed: {deployedCount}/{MaxDeployed}";
        }
    }

    public void OnSlotHeroClicked(FormationSlot slot)
    {
        if (!slotToHero.ContainsKey(slot))
        {
            ClearSelectedHero();
            return;
        }

        HeroData hero = slotToHero[slot];
        currentSelectedHero = hero;

        // Update right sidebar
        if (selectedHeroIcon != null && hero.sprite != null)
        {
            selectedHeroIcon.sprite = hero.sprite;
            selectedHeroIcon.enabled = true;
        }

        if (selectedHeroName != null)
        {
            selectedHeroName.text = hero.heroName;
        }

        // Populate skill list
        PopulateSkillList(hero);
    }

    private void PopulateSkillList(HeroData hero)
    {
        // Clear existing skill items
        if (skillContent != null)
        {
            foreach (Transform child in skillContent)
            {
                Destroy(child.gameObject);
            }
        }

        if (hero == null || skillItemPrefab == null || skillContent == null)
            return;

        // Add special skill if exists
        if (hero.specialSkill != null)
        {
            CreateSkillItem(hero.specialSkill);
        }

        // TODO: Add more skills here when hero has multiple skills
        // For now, just showing the special skill
    }

    private void CreateSkillItem(Skill skill)
    {
        GameObject item = Instantiate(skillItemPrefab, skillContent);

        // Find components in the skill item prefab
        Image skillIcon = item.GetComponentInChildren<Image>();
        TMP_Text skillName = item.GetComponentInChildren<TMP_Text>();

        // Set skill icon (if skill has an icon sprite, otherwise use effect prefab preview)
        if (skillIcon != null && skill.effectPrefab != null)
        {
            // You can add a Sprite field to Skill.cs for skill icons later
            // For now, we'll just enable the image
            skillIcon.enabled = true;
        }

        // Set skill name
        if (skillName != null)
        {
            skillName.text = skill.skillName;
        }
    }

    private void ClearSelectedHero()
    {
        currentSelectedHero = null;

        if (selectedHeroIcon != null)
        {
            selectedHeroIcon.enabled = false;
        }

        if (selectedHeroName != null)
        {
            selectedHeroName.text = "";
        }

        // Clear skill list
        if (skillContent != null)
        {
            foreach (Transform child in skillContent)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void EnableSlotHighlights(bool enable, HeroData draggingHero = null)
    {
        if (allSlots == null)
            return;

        foreach (var slot in allSlots)
        {
            if (slot == null)
                continue;

            if (enable)
            {
                bool isValid = CanPlaceHero(slot, draggingHero);
                slot.SetHighlightState(isValid ? FormationSlot.HighlightState.Valid : FormationSlot.HighlightState.Invalid);
            }
            else
            {
                slot.SetHighlightState(FormationSlot.HighlightState.None);
            }
        }
    }

    private bool CanPlaceHero(FormationSlot slot, HeroData hero)
    {
        if (hero == null)
            return false;

        // ✅ Check if slot column matches hero role
        if (slot.column != hero.heroRole)
        {
            return false;
        }

        // If hero is already placed (moving), allow movement within same column
        if (heroToSlot.ContainsKey(hero))
            return true;

        // If slot is empty and under max deployed
        if (!slotToHero.ContainsKey(slot) && deployedCount < MaxDeployed)
            return true;

        // If slot is occupied (swap allowed within same column)
        if (slotToHero.ContainsKey(slot))
            return true;

        return false;
    }

    public FormationSlot GetSlotOf(HeroData hero)
    {
        if (heroToSlot.ContainsKey(hero))
            return heroToSlot[hero];
        return null;
    }

    public Transform GetDragLayer()
    {
        return dragLayer;
    }

    private void OnCloseClicked()
    {
        SaveBackToPlayerData();
        Hide();
    }

    private void SaveBackToPlayerData()
    {
        // Hero data already updated during assign/unassign
        // Just trigger any save mechanisms if they exist
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.OnPlayerDataChanged?.Invoke();
        }
    }

    private void ClearUI()
    {
        // Clear hero list
        if (heroListContent != null)
        {
            foreach (Transform child in heroListContent)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear slots
        if (allSlots != null)
        {
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    slot.ClearOccupant();
                }
            }
        }

        heroToListCard.Clear();
        slotToHero.Clear();
        heroToSlot.Clear();
        deployedCount = 0;
    }

    private void SetHeroUnitSortOrder(HeroUnit heroUnit)
    {
        if (heroUnit == null) return;

        // Set sprites to render ABOVE Canvas UI
        SpriteRenderer[] spriteRenderers = heroUnit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in spriteRenderers)
        {
            sr.sortingLayerName = "UI_HeroUnits"; // Above UI layer!
            sr.sortingOrder = 105;
        }

        // Set child canvases (health/energy bars)
        Canvas[] canvases = heroUnit.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI_HeroUnits";
            canvas.sortingOrder = 115;
        }
    }
}
