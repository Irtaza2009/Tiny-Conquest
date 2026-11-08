using UnityEngine;

public class PlatformInput : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;

    private Platform selectedPlatform;
    private PathConnection[] connections;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        connections = FindObjectsByType<PathConnection>(
            FindObjectsSortMode.None
        );
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    private void HandleClick()
    {
        if (gameCamera == null)
            return;

        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Platform clickedPlatform = hit.collider.GetComponentInParent<Platform>();

        if (clickedPlatform == null)
        {
            ClearSelection();
            return;
        }

        // First click: select player platform.
        if (selectedPlatform == null)
        {
            if (clickedPlatform.Owner == PlatformOwner.Player)
            {
                SelectPlatform(clickedPlatform);
            }

            return;
        }

        // Click selected platform to deselect it.
        if (clickedPlatform == selectedPlatform)
        {
            ClearSelection();
            return;
        }

        // Click another player platform to change selection.
        if (clickedPlatform.Owner == PlatformOwner.Player)
        {
            SelectPlatform(clickedPlatform);
            return;
        }

        // Send all available troops to the destination.
        if (ArmyManager.Instance != null)
        {
            ArmyManager.Instance.SendTroops(selectedPlatform, clickedPlatform, selectedPlatform.TroopCount);
        }

        ClearSelection();
    }

    private void SelectPlatform(Platform platform)
    {
        ClearSelection();

        selectedPlatform = platform;
        selectedPlatform.isSelected = true;

        foreach (PathConnection connection in connections)
        {
            if (connection == null)
                continue;

            Platform neighbour = connection.GetOtherPlatform(selectedPlatform);

            if (neighbour == null ||
                neighbour.Owner == PlatformOwner.Player)
            {
                continue;
            }

            Transform[] route;

            if (connection.TryGetRoute(
                selectedPlatform,
                neighbour,
                out route))
            {
                neighbour.SetAttackHighlight(true);
            }
        }
    }

    private void ClearSelection()
    {
        if (selectedPlatform != null)
            selectedPlatform.isSelected = false;

        Platform previousSelection = selectedPlatform;

        foreach (PathConnection connection in connections)
        {
            if (connection == null)
                continue;

            Platform neighbour = connection.GetOtherPlatform(previousSelection);

            if (neighbour != null)
                neighbour.SetAttackHighlight(false);
        }

        selectedPlatform = null;
    }
}
