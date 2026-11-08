using UnityEditor.Rendering;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [SerializeField] private float decisionInterval = 3f;
    [SerializeField] private int minimumTroopsToAttack = 4;
    [SerializeField] private int troopsToKeepBehind = 1;

    private float decisionTimer;

    private Platform[] platforms;
    private PathConnection[] connections;

    private void Start()
    {
        platforms = FindObjectsByType<Platform>(FindObjectsSortMode.None);

        connections = FindObjectsByType<PathConnection>(FindObjectsSortMode.None); 
    }

    private void Update()
    {
        decisionTimer += Time.deltaTime;

        if (decisionTimer >= decisionInterval)
        {
            decisionTimer = 0f;
            MakeDecisions();
        }
    }

    private void MakeDecisions()
    {
        if (ArmyManager.Instance == null)
            return;

        foreach (Platform source in platforms)
        {
            if (source == null || source.Owner != PlatformOwner.AI)
            {
                continue;
            }

            int availableTroops = source.TroopCount;

            if (availableTroops <= minimumTroopsToAttack)
                continue;

            Platform target = FindBestTarget(source);

            if (target == null)
                continue;

            int troopsToSend = Mathf.Max(0, availableTroops - troopsToKeepBehind);
            
            if (troopsToSend > 0)
            {
                ArmyManager.Instance.SendTroops(source, target, troopsToSend);
            }
        }
    }

    private Platform FindBestTarget(Platform source)
    {
        Platform bestNeutral = null;
        Platform bestPlayer = null;

        int lowestNeutralDefenders = int.MaxValue;
        int lowestPlayerDefenders = int.MaxValue;

        foreach (PathConnection connection in connections)
        {
            if (connection == null)
                continue;

            Platform neighbour = connection.GetOtherPlatform(source);

            if (neighbour == null)
                continue;

            if (neighbour.Owner == PlatformOwner.Neutral)
            {
                if (neighbour.TroopCount < lowestNeutralDefenders)
                {
                    lowestNeutralDefenders = neighbour.TroopCount;
                    bestNeutral = neighbour;
                }
            }
            else if (neighbour.Owner == PlatformOwner.Player)
            {
                if (neighbour.TroopCount < lowestPlayerDefenders)
                {
                    lowestPlayerDefenders = neighbour.TroopCount;
                    bestPlayer = neighbour;
                }
            }
        }

        if (bestNeutral != null)
        {
            return bestNeutral;
        }

        return bestPlayer;
    }
}
