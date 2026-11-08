using UnityEngine;

public class PlatformInput : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;

    private Platform selectedPlatform;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
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
            if (selectedPlatform != null)
            {
                selectedPlatform.isSelected = false;
            }
            
            selectedPlatform = null;
            return;
        }

        // First click: select player platform.
        if (selectedPlatform == null)
        {
            if (clickedPlatform.Owner == PlatformOwner.Player)
            {
                selectedPlatform = clickedPlatform;
                selectedPlatform.isSelected = true;
            }

            return;
        }

        // Click selected platform to deselect it.
        if (clickedPlatform == selectedPlatform)
        {
            selectedPlatform.isSelected = false;
            selectedPlatform = null;
            return;
        }

        // Click another player platform to change selection.
        if (clickedPlatform.Owner == PlatformOwner.Player)
        {
            selectedPlatform.isSelected = false;
            selectedPlatform = clickedPlatform;
            selectedPlatform.isSelected = true;
            return;
        }

        // Send all available troops to the destination.
        if (ArmyManager.Instance != null)
        {
            ArmyManager.Instance.SendTroops(selectedPlatform, clickedPlatform, selectedPlatform.TroopCount);
        }

        selectedPlatform.isSelected = false;
        selectedPlatform = null;
    }
}
