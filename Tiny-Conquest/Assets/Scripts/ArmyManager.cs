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
        connections = FindObjectsByType<PathConnection>(FindObjectsSortMode.None);
    }

    public bool SendTroops(
        Platform source,
        Platform destination,
        int amount)
    {
        if (source == null || destination == null)
            return false;

        if (source == destination || amount <= 0)
            return false;

        if (source.Owner == PlatformOwner.Neutral)
            return false;

        PathConnection connection = null;
        Transform[] route = null;

        foreach (PathConnection candidate in connections)
        {
            if (candidate.TryGetRoute(
                source,
                destination,
                out route))
            {
                connection = candidate;
                break;
            }
        }

        if (connection == null || route == null)
        {
            Debug.Log("These platforms are not connected.");
            return false;
        }

        List<SwordsmanController> troops =
            source.TakeTroops(amount);

        if (troops.Count == 0)
            return false;

        foreach (SwordsmanController troop in troops)
        {
            if (troop != null)
                troop.SetRoute(route, destination);
        }

        return true;
    }
}
