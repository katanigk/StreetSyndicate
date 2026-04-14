using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single visual recipe for runtime-built planning UI buttons (bars, rows, AoW, tabs, dock).
/// </summary>
public static class PlanningUiButtonStyle
{
    public static readonly Color RectFill = new Color(0.18f, 0.17f, 0.20f, 0.96f);
    public static readonly Color RectHighlight = new Color(0.28f, 0.29f, 0.34f, 1f);
    public static readonly Color RectPressed = new Color(0.11f, 0.12f, 0.14f, 1f);
    public static readonly Color RectDisabled = new Color(0.35f, 0.35f, 0.38f, 0.5f);
    public static readonly Color OutlineColor = new Color(0.38f, 0.39f, 0.44f, 0.9f);
    public static readonly Vector2 OutlineDistance = new Vector2(1f, -1f);
    public static readonly Color LabelPrimary = new Color(0.95f, 0.95f, 0.92f, 1f);
    /// <summary>Top tab “selected” — same family, brighter cool accent.</summary>
    public static readonly Color TabSelectedFill = new Color(0.22f, 0.34f, 0.50f, 1f);

    public static void ApplyOutline(Graphic g)
    {
        if (g == null)
            return;
        Outline o = g.gameObject.GetComponent<Outline>();
        if (o == null)
            o = g.gameObject.AddComponent<Outline>();
        o.effectColor = OutlineColor;
        o.effectDistance = OutlineDistance;
    }

    public static void ApplyColorTint(Button btn, Color normal)
    {
        if (btn == null)
            return;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = RectHighlight;
        cb.pressedColor = RectPressed;
        cb.disabledColor = RectDisabled;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.09f;
        btn.colors = cb;
    }

    /// <summary>Rect / full-width row: fill + outline + tint (normal matches image).</summary>
    public static void ApplyStandardRectButton(Button btn, Image img)
    {
        if (btn == null || img == null)
            return;
        img.color = RectFill;
        ApplyOutline(img);
        ApplyColorTint(btn, RectFill);
    }

    /// <summary>Circle buttons: same outline + tint ladder anchored at <paramref name="normal"/>.</summary>
    public static void ApplyStandardCircleButton(Button btn, Image circle, Color normal)
    {
        if (btn == null || circle == null)
            return;
        circle.color = normal;
        ApplyOutline(circle);
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = normal;
        cb.highlightedColor = Color.Lerp(normal, RectHighlight, 0.55f);
        cb.pressedColor = Color.Lerp(normal, RectPressed, 0.65f);
        cb.disabledColor = RectDisabled;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.09f;
        btn.colors = cb;
    }

    private static readonly Color AoWRadialOutline = new Color(0.62f, 0.52f, 0.22f, 0.9f);

    /// <summary>AoW portrait radial chips: wood art stays visible; warm highlight/press (no cool grey RectFill).</summary>
    public static void ApplyAoWRadialWoodButton(Button btn, Image circle)
    {
        if (btn == null || circle == null)
            return;
        circle.color = Color.white;
        Outline o = circle.gameObject.GetComponent<Outline>();
        if (o == null)
            o = circle.gameObject.AddComponent<Outline>();
        o.effectColor = AoWRadialOutline;
        o.effectDistance = OutlineDistance;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.04f, 0.98f, 1f);
        cb.pressedColor = new Color(0.86f, 0.82f, 0.74f, 1f);
        cb.disabledColor = RectDisabled;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.09f;
        btn.colors = cb;
    }
}
