using System.Collections.Generic;
using UnityEngine;

public class ArmyManager : MonoBehaviour
{
    public static ArmyManager Instance { get; private set; }

    private PathConnection[] connections;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        connections = FindObjectsByType<PathConnection>(
            FindObjectsSortMode.None
        );
    }

    public bool SendTroops(
        Platform source,
        Platform destination,
        int amount)
    {
        if (source == null ||
            destination == null ||
            source == destination ||
            amount <= 0 ||
            source.Owner == PlatformOwner.Neutral)
        {
            return false;
        }

        Transform[] route = null;

        foreach (PathConnection candidate in connections)
        {
            if (candidate.TryGetRoute(source, destination, out route))
                break;
        }

        if (route == null || route.Length == 0)
        {
            Debug.Log("These platforms are not connected.");
            return false;
        }

        List<UnitController> troops = source.TakeTroops(amount);

        if (troops.Count == 0)
            return false;

        foreach (UnitController troop in troops)
        {
            if (troop != null)
                troop.SetRoute(route, destination);
        }

        return true;
    }
}
