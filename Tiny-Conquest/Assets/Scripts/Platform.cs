using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum PlatformOwner
{
    Neutral,
    Player,
    AI
}

public class Platform : MonoBehaviour
{
    [Header("Ownership")]
    [SerializeField] private PlatformOwner owner =
        PlatformOwner.Neutral;

    [Header("Troop Production")]
    [SerializeField] private SwordsmanController swordsmanPrefab;
    [SerializeField] private int maxTroops = 5;
    [SerializeField] private int startingTroops = 2;
    [SerializeField] private float productionInterval = 3f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnSpacing = 0.7f;
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Header("Visuals")]
    [SerializeField] private Renderer[] ownershipRenderers;
    [SerializeField] private TMP_Text troopCountLabel;

    [Header("Layers")]
    [SerializeField] private string neutralLayer = "NeutralUnits";
    [SerializeField] private string playerLayer = "PlayerUnits";
    [SerializeField] private string aiLayer = "EnemyUnits";

    private readonly List<SwordsmanController> stationedTroops = new List<SwordsmanController>();

    private float productionTimer;

    private readonly Color neutralColor = Color.grey;

    private readonly Color playerColor = new Color(0.44f, 0.76f, 0.8f);

    private readonly Color aiColor = new Color(0.8f, 0.51f, 0.51f);

    public PlatformOwner Owner => owner;
    public int TroopCount => stationedTroops.Count;
    public int MaxTroops => maxTroops;

    private void Start()
    {
        startingTroops = Mathf.Clamp(
            startingTroops, 0, maxTroops
        );

        UpdateVisuals();

        for (int i = 0; i < startingTroops; i++)
            SpawnTroop();

        UpdateTroopCounter();
    }

    private void Update()
    {
        RemoveDeadTroops();

        CheckCapture();

        if (owner == PlatformOwner.Neutral)
            return;

        if (isContested())
        {
            return;
        }

        if (stationedTroops.Count >= maxTroops)
        {
            productionTimer = 0f;
            return;
        }

        productionTimer += Time.deltaTime;

        if (productionTimer >= productionInterval)
        {
            productionTimer = 0f;
            SpawnTroop();
        }
    }

    private void SpawnTroop()
    {
        if (stationedTroops.Count >= maxTroops ||
            swordsmanPrefab == null)
        {
            return;
        }

        Vector3 basePosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * spawnHeightOffset;

        // arranging soldiers in centered rows, radius not working... whahawhahwhah
        int index = stationedTroops.Count;
        int columns = Mathf.Min(3, maxTroops);
        int totalRows = Mathf.CeilToInt(
            (float)maxTroops / columns
        );

        int row = index / columns;
        int column = index % columns;

        int troopsInThisRow = Mathf.Min(
            columns,
            maxTroops - row * columns
        );

        float xOffset =
            (column - (troopsInThisRow - 1) * 0.5f)
            * spawnSpacing;

        float zOffset =
            ((totalRows - 1) * 0.5f - row)
            * spawnSpacing;

        Vector3 spawnPosition = basePosition +
            new Vector3(xOffset, 0f, zOffset);

        SwordsmanController troop = Instantiate(
            swordsmanPrefab,
            spawnPosition,
            Quaternion.identity
        );

        troop.SetTeam(GetTeamID());

        SetLayerRecursively(
            troop.gameObject,
            GetUnitLayer()
        );

        stationedTroops.Add(troop);

        UpdateTroopCounter();
    }

    private void CheckCapture()
    {
        bool hasPlayer = false;
        bool hasAI = false;
        bool hasNeutral = false;

        foreach (SwordsmanController troop in stationedTroops)
        {
            if (troop == null || troop.IsDead)
                continue;
            
            switch (troop.TeamID)
            {
                case 0:
                    hasNeutral = true;
                    break;
                
                case 1:
                    hasPlayer = true;
                    break;

                case 2:
                    hasAI = true;
                    break;
            }
        }

        if (hasPlayer && !hasAI && !hasNeutral)
        {
            SetOwner(PlatformOwner.Player);
        }
        else if (!hasPlayer && hasAI && !hasNeutral)
        {
            SetOwner(PlatformOwner.AI);
        }
        else if (!hasPlayer && !hasAI && hasNeutral)
        {
            SetOwner(PlatformOwner.Neutral);
        }
    }

    private bool isContested()
    {
        bool hasPlayer = false;
        bool hasAI = false;
        bool hasNeutral = false;

        foreach (SwordsmanController troop in stationedTroops)
        {
            if (troop == null || troop.IsDead)
                continue;
            
            switch (troop.TeamID)
            {
                case 0:
                    hasNeutral = true;
                    break;
                
                case 1:
                    hasPlayer = true;
                    break;

                case 2:
                    hasAI = true;
                    break;
            }
        }

        int teamCount = 0;

        if (hasPlayer) teamCount++;
        if (hasAI) teamCount++;
        if (hasNeutral) teamCount++;

        return teamCount > 1;
    }

    private int GetTeamID()
    {
        switch (owner)
        {
            case PlatformOwner.Player:
                return 1;

            case PlatformOwner.AI:
                return 2;

            default:
                return 0;
        }
    }

    private string GetUnitLayer()
    {
        switch (owner)
        {
            case PlatformOwner.Player:
                return playerLayer;

            case PlatformOwner.AI:
                return aiLayer;

            default:
                return neutralLayer;
        }
    }

    private void SetLayerRecursively(
        GameObject obj,
        string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogWarning(
                "Layer '" + layerName + "' does not exist."
            );

            return;
        }

        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layerName);
    }

    private void RemoveDeadTroops()
    {
        stationedTroops.RemoveAll(troop => troop == null);
        UpdateTroopCounter();
    }

    public List<SwordsmanController> TakeTroops(int amount)
    {
        RemoveDeadTroops();

        amount = Mathf.Clamp(
            amount, 0, stationedTroops.Count
        );

        List<SwordsmanController> dispatchedTroops =
            new List<SwordsmanController>();

        for (int i = 0; i < amount; i++)
        {
            int lastIndex = stationedTroops.Count - 1;

            SwordsmanController troop =
                stationedTroops[lastIndex];

            stationedTroops.RemoveAt(lastIndex);

            if (troop != null)
                dispatchedTroops.Add(troop);
        }

        UpdateTroopCounter();

        return dispatchedTroops;
    }

    public void SetOwner(PlatformOwner newOwner)
    {
        owner = newOwner;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        Color ownerColor = neutralColor;

        switch (owner)
        {
            case PlatformOwner.Player:
                ownerColor = playerColor;
                break;

            case PlatformOwner.AI:
                ownerColor = aiColor;
                break;
        }

        foreach (Renderer platformRenderer in ownershipRenderers)
        {
            if (platformRenderer != null)
                platformRenderer.material.color = ownerColor;
        }
    }

    private void UpdateTroopCounter()
    {
        if (troopCountLabel != null)
        {
            troopCountLabel.text =
                stationedTroops.Count + "/" + maxTroops;
        }
    }

    public void ReceiveTroop(SwordsmanController troop)
    {
        if (troop == null || stationedTroops.Contains(troop))
        {
            return;
        }

        stationedTroops.Add(troop);
        UpdateTroopCounter();
    }
}
