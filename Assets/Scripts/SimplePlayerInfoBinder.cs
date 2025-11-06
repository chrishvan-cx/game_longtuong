using UnityEngine;
using TMPro;

// Simple dev-only binder: reads PlayerData (by tag) public fields and writes to PlayerInfoPanel (this object or by tag)
public class SimplePlayerInfoBinder : MonoBehaviour
{
    [Header("Optional explicit refs (auto-found if null)")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;

    private PlayerData _playerData;
    private Transform _panelRoot;

    void Awake()
    {
        // Ensure we have a panel root: this object if tagged PlayerInfoPanel, else find by tag
        var selfTagged = CompareTag("PlayerInfoPanel");
        _panelRoot = selfTagged ? transform : (GameObject.FindWithTag("PlayerInfoPanel")?.transform ?? transform);

        // Auto-bind texts if not assigned
        if (levelText == null) levelText = FindText(_panelRoot, "level");
        if (goldText == null) goldText = FindText(_panelRoot, "gold");
    }

    void LateUpdate()
    {
        // Resolve PlayerData by tag if needed
        if (_playerData == null)
        {
            var pdObj = GameObject.FindWithTag("PlayerData");
            if (pdObj != null) _playerData = pdObj.GetComponent<PlayerData>();
        }

        if (_playerData == null) return;

        // Write values directly from public fields
        if (levelText != null)
            levelText.text = $"Level {_playerData.playerLevel}";

        if (goldText != null)
            goldText.text = $"{_playerData.playerGold:N0}";
    }

    private TextMeshProUGUI FindText(Transform root, string contains)
    {
        contains = contains.ToLowerInvariant();
        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            if (t.name.ToLowerInvariant().Contains(contains))
                return t;
        }
        return null;
    }
}
