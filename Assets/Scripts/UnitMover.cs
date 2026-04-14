using UnityEngine;

public class UnitMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    public void MoveToWorldPosition(Vector3 world)
    {
        targetPosition = world;
        targetPosition.z = 0f;
        isMoving = true;
    }

    private void Update()
    {
        if (GameSessionState.IsDaySummaryShowing)
            return;

        if (GameModeManager.Instance == null) return;
        if (!GameModeManager.Instance.IsActionMode()) return;

        HandleMouseInput();
        MoveToTarget();
    }

    private void HandleMouseInput()
    {
        SelectableUnit selectableUnit = GetComponent<SelectableUnit>();

        if (selectableUnit == null) return;
        if (!selectableUnit.IsSelected()) return;

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;

            targetPosition = mouseWorldPosition;
            isMoving = true;
        }
    }

    private void MoveToTarget()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}