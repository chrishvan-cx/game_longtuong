# Development Auto-Reset Feature

## What It Does

During development (when running in Unity Editor), the game **automatically resets all progress** when you start the game.

This includes:
- ✅ Quest progress (mapLevel, questLevel)
- ✅ Completed quests
- ✅ Player stats (gold, XP, level)
- ✅ Unlocked map areas

## Why?

This makes testing much easier:
- No need to manually clear PlayerPrefs
- Always start with a clean slate
- Test first-time player experience every time
- No stale data from previous runs

## How It Works

The code uses Unity's preprocessor directives:

```csharp
#if UNITY_EDITOR
    // Only runs in Unity Editor (development)
    ResetAllProgressInternal();
#else
    // Only runs in builds (production)
    LoadProgress();
#endif
```

### In Unity Editor (Development):
- **Auto-resets** progress on every play
- You'll see logs:
  ```
  [DEVELOPMENT MODE] Resetting all quest progress...
  [DEVELOPMENT MODE] Resetting player progress...
  ```

### In Builds (Production):
- **Loads saved** progress normally
- Players keep their progress between sessions

## When You're Ready for Production

When you want to test with persistent data (or before building):

### Option 1: Temporarily Disable (Quick Test)

Comment out the reset in `QuestManager.cs`:

```csharp
#if UNITY_EDITOR
    // Debug.Log("[DEVELOPMENT MODE] Resetting all quest progress...");
    // ResetAllProgressInternal();
    LoadProgress(); // Add this to load saved data
#else
    LoadProgress();
#endif
```

And in `PlayerProgress.cs`:

```csharp
#if UNITY_EDITOR
    // Debug.Log("[DEVELOPMENT MODE] Resetting player progress...");
    // ResetProgressInternal();
    LoadProgress(); // Add this to load saved data
#else
    LoadProgress();
#endif
```

### Option 2: Remove Development Reset (Production Ready)

Remove the `#if UNITY_EDITOR` blocks entirely:

```csharp
void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    LoadProgress(); // Always load
}
```

## Manual Reset During Testing

You can still manually reset if needed:

### In Code:
```csharp
// Reset everything
QuestManager.Instance.ResetAllProgress();
PlayerProgress.Instance.ResetProgress();
```

### In Inspector:
- Select QuestManager GameObject
- In the Inspector, you can call `ResetAllProgress()` from the context menu

## Benefits

✅ **Faster iteration** - Test from start instantly
✅ **No manual cleanup** - No need to find and delete PlayerPrefs
✅ **Consistent testing** - Always test the same initial state
✅ **Production safe** - Won't affect player saves in builds

## Files Modified

- `QuestManager.cs` - Auto-resets quest/map/player levels
- `PlayerProgress.cs` - Auto-resets gold/XP/level

Both only reset when running in Unity Editor, never in builds!
