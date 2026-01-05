using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapSection
{
    public string id;       // e.g., "section_0"
    public Light[] lights; // All lights in this section
}



public class MapControlManager : MonoBehaviour
{
    public static MapControlManager Instance;

    [Header("Configuration")]
    public List<MapSection> sections; // Need 21 of these in Inspector
    public List<DoorController> doors; // Need 17 of these in Inspector (DoorController scripts)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetSectionState(string id, bool isLightsOn)
    {
        var section = sections.Find(s => s.id == id);
        if (section != null)
        {
            foreach (var light in section.lights)
            {
                if (light != null) 
                {
                    Debug.Log($"[MapControlManager] Setting light {light.name} in section {section.id} to {(isLightsOn ? "ON" : "OFF")}");
                    light.enabled = isLightsOn;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[MapControlManager] Section '{id}' not found! Check Inspector.");
        }
    }

    public void OnDoorClosed(string id)
    {
        var door = doors.Find(d => d.doorID == id);
        if (door != null)
        {
            door.LockDoor();
        }
    }

    public void OnDoorUnlockable(string id)
    {
        var door = doors.Find(d => d.doorID == id);
        if (door != null)
        {
            door.MakeUnlockable();
        }
    }
}
