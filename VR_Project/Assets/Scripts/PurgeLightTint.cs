using UnityEngine;
using System.Collections.Generic;

// Minimal: tint selected lights red on purge start, restore on purge end.
public class PurgeLightTint : MonoBehaviour
{
    [Tooltip("Lights to tint when purge starts (Realtime or Mixed)")]
    public List<Light> targetLights = new List<Light>();
    public Color purgeColor = Color.red;

    private Dictionary<Light, Color> _originalColors = new Dictionary<Light, Color>();

    public void OnPurgeStart()
    {
        _originalColors.Clear();
        foreach (var l in targetLights)
        {
            if (l == null) continue;
            if (!_originalColors.ContainsKey(l))
            {
                _originalColors[l] = l.color;
            }
            l.color = purgeColor;
        }
    }

    public void OnPurgeEnd()
    {
        foreach (var kv in _originalColors)
        {
            var l = kv.Key;
            if (l == null) continue;
            l.color = kv.Value;
        }
        _originalColors.Clear();
    }
}
