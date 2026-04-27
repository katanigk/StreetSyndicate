using System.Text;
using UnityEngine;

/// <summary>Optional: add to a GameObject. F9 toggles overlay (only draws in Editor or DEVELOPMENT_BUILD).</summary>
[DefaultExecutionOrder(5000)]
public class FederalBureauRuntimeDebugDrawer : MonoBehaviour
{
    public static bool Show { get; private set; }
    [SerializeField] int maxLines = 10;
    [SerializeField] int fontSize = 12;
    GUIStyle _boxStyle;
    GUIStyle _textStyle;

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F9))
            Show = !Show;
#endif
    }

    void OnGUI()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return;
#endif
        if (!Show) return;
        if (!BureauWorldState.IsBootstrapped) return;
        if (_textStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = new Color(0.9f, 0.85f, 0.6f) }
            };
        }
        var sb = new StringBuilder();
        sb.AppendLine("Federal Runtime (F9) day=" + BureauWorldState.lastRuntimeDay
                      + " interest=" + BureauWorldState.federalRuntimeInterest01
                      + " status=" + BureauWorldState.bureauStatus);
        int n = BureauWorldState.federalRuntimeLog != null ? BureauWorldState.federalRuntimeLog.Count : 0;
        for (int i = Mathf.Max(0, n - maxLines); i < n; i++)
            sb.AppendLine(BureauWorldState.federalRuntimeLog[i]);
        float h = 28f + maxLines * (fontSize + 4f);
        var bg = new Rect(4f, 4f, 560f, h);
        GUI.Box(bg, GUIContent.none, _boxStyle);
        GUI.Label(new Rect(10f, 8f, 548f, h - 4f), sb.ToString(), _textStyle);
    }
}
