using System.Collections.Generic;
using UnityEngine;

public class ClueSetManager : MonoBehaviour
{
    public static ClueSetManager Instance { get; private set; }

    [Header("Clue Tracking")]
    public List<CluePickup> clues = new List<CluePickup>();

    [Header("Debug")] 
    public bool log = true;

    public int Total => clues.Count;
    public int CollectedCount { get; private set; } = 0;

    public bool AllCollected => Total > 0 && CollectedCount >= Total;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(CluePickup clue)
    {
        if (!clues.Contains(clue))
        {
            clues.Add(clue);
            if (log) Debug.Log($"[ClueSetManager] Registered clue {clue.name}");
        }
    }

    public void Unregister(CluePickup clue)
    {
        if (clues.Remove(clue))
        {
            if (log) Debug.Log($"[ClueSetManager] Unregistered clue {clue.name}");
            // Recompute count in case this was collected
            Recount();
        }
    }

    public void NotifyCollected(CluePickup clue)
    {
        if (log) Debug.Log($"[ClueSetManager] Collected {clue.name}");
        Recount();
        if (AllCollected && log)
        {
            Debug.Log("[ClueSetManager] All clues collected");
        }
    }

    private void Recount()
    {
        int c = 0;
        foreach (var clue in clues)
        {
            if (clue != null && clue.IsCollected)
                c++;
        }
        CollectedCount = c;
    }
}
