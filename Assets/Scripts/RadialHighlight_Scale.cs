using UnityEngine;

// 默认的高亮方案：轻微放大
public class RadialHighlight_Scale : MonoBehaviour, IRadialHighlight
{
    public float minScale = 1f;
    public float maxScale = 1.15f;
    Vector3 _base;

    void Awake() { _base = transform.localScale; }

    public void Apply(float w)
    {
        float s = Mathf.Lerp(minScale, maxScale, Mathf.Clamp01(w));
        transform.localScale = _base * s;
    }
}