using UnityEngine;

public class PipeIndicator : MonoBehaviour
{
    public Light indicatorLight;
    public Renderer indicatorRenderer;
    public Material redMaterial;
    public Material greenMaterial;
    public Color redColor = Color.red;
    public Color greenColor = Color.green;
    public bool useRendererEmission = true;
    public bool startRed = true;

    void Awake()
    {
        SetRepaired(!startRed);
    }

    public void SetRepaired(bool repaired)
    {
        var color = repaired ? greenColor : redColor;
        ApplyColor(color);
        ApplyMaterial(repaired);
    }

    private void ApplyColor(Color c)
    {
        if (indicatorLight != null)
        {
            indicatorLight.color = c;
        }
        if (indicatorRenderer != null)
        {
            var mat = indicatorRenderer.material;
            if (useRendererEmission && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.color = c;
            }
        }
    }

    private void ApplyMaterial(bool repaired)
    {
        if (indicatorRenderer == null) return;
        if (repaired && greenMaterial != null)
        {
            indicatorRenderer.material = greenMaterial;
            return;
        }
        if (!repaired && redMaterial != null)
        {
            indicatorRenderer.material = redMaterial;
            return;
        }
        // Fallback to color-only when materials not assigned
    }
}
