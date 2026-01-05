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

    // When true, all lights are forced ON and attempts to turn them OFF are ignored
    private bool lightsLockedOn = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Default state: turn all lights OFF at startup
        InitializeAllLightsOff();
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
                    if (lightsLockedOn)
                    {
                        // During purge, force lights ON regardless of incoming state
                        if (!light.enabled)
                        {
                            Debug.Log($"[MapControlManager] Purge lock: forcing light {light.name} ON (section {section.id})");
                        }
                        light.enabled = true;
                    }
                    else
                    {
                        Debug.Log($"[MapControlManager] Setting light {light.name} in section {section.id} to {(isLightsOn ? "ON" : "OFF")}");
                        light.enabled = isLightsOn;
                    }
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

    public void SetLightsLockedOn(bool locked)
    {
        lightsLockedOn = locked;
        if (lightsLockedOn)
        {
            // Immediately turn all lights ON
            TurnAllLights(true);
        }
    }

    private void InitializeAllLightsOff()
    {
        TurnAllLights(false);
    }

    private void TurnAllLights(bool on)
    {
        foreach (var section in sections)
        {
            if (section == null || section.lights == null) continue;
            foreach (var light in section.lights)
            {
                if (light == null) continue;
                light.enabled = on;
            }
        }
    }
}
