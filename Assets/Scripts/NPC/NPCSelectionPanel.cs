using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;
    public TMP_Text areaTitle;
    public Transform npcContainer;  // Should have Horizontal Layout Group
    public Button closeButton;

    private List<GameObject> spawnedNPCButtons = new List<GameObject>();

    void Awake()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        // Hide panel initially
        Hide();
    }

    public void ShowNPCs(string areaName, MapNPC[] npcs)
    {
        // Clear previous NPCs
        ClearNPCs();

        // Set area title
        if (areaTitle != null)
            areaTitle.text = areaName;

        // Spawn NPC buttons
        if (npcContainer != null)
        {
            foreach (var npc in npcs)
            {
                if (npc == null) continue;

                // Instantiate NPC directly (MapNPC already has click handler)
                GameObject npcButton = Instantiate(npc.gameObject, npcContainer);
                spawnedNPCButtons.Add(npcButton);
            }
        }

        // Show panel
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        ClearNPCs();
    }

    private void ClearNPCs()
    {
        foreach (var btn in spawnedNPCButtons)
        {
            if (btn != null)
                Destroy(btn);
        }
        spawnedNPCButtons.Clear();
    }
}
