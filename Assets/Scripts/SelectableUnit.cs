using UnityEngine;

public class SelectableUnit : MonoBehaviour
{
    public static SelectableUnit CurrentlySelected;

    private SpriteRenderer spriteRenderer;
    private Color defaultColor;

    [SerializeField] private Color selectedColor = Color.yellow;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultColor = spriteRenderer.color;
        Debug.Log("SelectableUnit Awake on: " + gameObject.name);
    }

    private void OnMouseDown()
    {
        if (GameSessionState.IsDaySummaryShowing)
            return;

        Debug.Log("OnMouseDown fired on: " + gameObject.name);

        GameModeManager.EnsureExists();
        if (GameModeManager.Instance == null)
        {
            Debug.LogError("GameModeManager.Instance is NULL");
            return;
        }

        if (!GameModeManager.Instance.IsActionMode())
        {
            Debug.LogWarning("Not in Action Mode");
            return;
        }

        SelectUnit();
    }

    public void SelectUnit()
    {
        if (CurrentlySelected != null && CurrentlySelected != this)
        {
            CurrentlySelected.DeselectUnit();
        }

        CurrentlySelected = this;
        spriteRenderer.color = selectedColor;
        Debug.Log("Unit selected: " + gameObject.name);
    }

    public void DeselectUnit()
    {
        spriteRenderer.color = defaultColor;

        if (CurrentlySelected == this)
        {
            CurrentlySelected = null;
        }
    }

    public bool IsSelected()
    {
        return CurrentlySelected == this;
    }
}