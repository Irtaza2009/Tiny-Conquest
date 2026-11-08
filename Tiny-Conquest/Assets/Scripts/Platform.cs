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
    [SerializeField] private PlatformOwner owner = PlatformOwner.Neutral;

    [Header("Troop Production")]
    [SerializeField] private SwordsmanController swordsmanPrefab;
    [SerializeField] private ArcherController archerPrefab;
    [SerializeField] private int maxTroops = 5;
    [SerializeField] private int startingTroops = 2;
    [SerializeField] private float productionInterval = 3f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnSpacing = 0.7f;
    [SerializeField] private float spawnHeightOffset = 0.5f;

    [Header("Visuals")]
    [SerializeField] private GameObject neutralPlatformPrefab;
    [SerializeField] private GameObject playerPlatformPrefab;
    [SerializeField] private GameObject aiPlatformPrefab;
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private GameObject attackHighlight;
    [SerializeField] private TMP_Text troopCountLabel;
    public bool isSelected;

    [Header("Layers")]
    [SerializeField] private string neutralLayer = "NeutralUnits";
    [SerializeField] private string playerLayer = "PlayerUnits";
    [SerializeField] private string aiLayer = "EnemyUnits";

    private readonly List<UnitController> stationedTroops =
        new List<UnitController>();

    private float productionTimer;

    private GameObject currentPlatformVisual;

    public PlatformOwner Owner => owner;
    public int TroopCount => stationedTroops.Count;
    public int MaxTroops => maxTroops;

    public void SetAttackHighlight(bool enabled)
    {
        if (attackHighlight != null)
            attackHighlight.SetActive(enabled);
    }

    private void Start()
    {
        startingTroops = Mathf.Clamp(startingTroops, 0, maxTroops);
        UpdatePlatformVisual();
        UpdateSelectionHighlight();
        SetAttackHighlight(false);

        for (int i = 0; i < startingTroops; i++)
            SpawnTroop();

        UpdateTroopCounter();
    }

    private void Update()
    {
        UpdateSelectionHighlight();
        RemoveDeadTroops();
        CheckCapture();

        if (owner == PlatformOwner.Neutral || IsContested())
            return;

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
        if (stationedTroops.Count >= maxTroops)
            return;

        UnitController prefab = ChooseUnitPrefab();

        if (prefab == null)
        {
            Debug.LogError(
                name + ": Assign a swordsman or archer prefab to Platform.",
                this
            );
            return;
        }

        Vector3 basePosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position + Vector3.up * spawnHeightOffset;

        int index = stationedTroops.Count;
        int columns = Mathf.Min(3, maxTroops);
        int totalRows = Mathf.CeilToInt((float)maxTroops / columns);
        int row = index / columns;
        int column = index % columns;
        int troopsInThisRow = Mathf.Min(
            columns,
            maxTroops - row * columns
        );

        float xOffset =
            (column - (troopsInThisRow - 1) * 0.5f) * spawnSpacing;
        float zOffset =
            ((totalRows - 1) * 0.5f - row) * spawnSpacing;

        UnitController troop = Instantiate(
            prefab,
            basePosition + new Vector3(xOffset, 0f, zOffset),
            Quaternion.identity
        );

        troop.SetTeam(GetTeamID());
        SetLayerRecursively(troop.gameObject, GetUnitLayer());
        stationedTroops.Add(troop);
        UpdateTroopCounter();
    }

    private UnitController ChooseUnitPrefab()
    {
        bool spawnSwordsman = Random.Range(0, 5) < 3;

        if (spawnSwordsman)
            return swordsmanPrefab;

        if (archerPrefab != null)
            return archerPrefab;

        Debug.LogWarning(
            name + ": Archer was selected but no archer prefab is assigned. " +
            "Falling back to swordsman.",
            this
        );
        return swordsmanPrefab;
    }

    private void CheckCapture()
    {
        bool hasPlayer = false;
        bool hasAI = false;
        bool hasNeutral = false;

        foreach (UnitController troop in stationedTroops)
        {
            if (troop == null || troop.IsDead)
                continue;

            if (troop.TeamID == 0) hasNeutral = true;
            else if (troop.TeamID == 1) hasPlayer = true;
            else if (troop.TeamID == 2) hasAI = true;
        }

        if (hasPlayer && !hasAI && !hasNeutral)
            SetOwner(PlatformOwner.Player);
        else if (!hasPlayer && hasAI && !hasNeutral)
            SetOwner(PlatformOwner.AI);
        else if (!hasPlayer && !hasAI && hasNeutral)
            SetOwner(PlatformOwner.Neutral);
    }

    private bool IsContested()
    {
        bool hasPlayer = false;
        bool hasAI = false;
        bool hasNeutral = false;

        foreach (UnitController troop in stationedTroops)
        {
            if (troop == null || troop.IsDead)
                continue;

            if (troop.TeamID == 0) hasNeutral = true;
            else if (troop.TeamID == 1) hasPlayer = true;
            else if (troop.TeamID == 2) hasAI = true;
        }

        int teamCount = 0;
        if (hasPlayer) teamCount++;
        if (hasAI) teamCount++;
        if (hasNeutral) teamCount++;

        return teamCount > 1;
    }

    private int GetTeamID()
    {
        if (owner == PlatformOwner.Player)
            return 1;
        if (owner == PlatformOwner.AI)
            return 2;
        return 0;
    }

    private string GetUnitLayer()
    {
        if (owner == PlatformOwner.Player)
            return playerLayer;
        if (owner == PlatformOwner.AI)
            return aiLayer;
        return neutralLayer;
    }

    private void SetLayerRecursively(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogWarning("Layer '" + layerName + "' does not exist.", this);
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

    public List<UnitController> TakeTroops(int amount)
    {
        RemoveDeadTroops();
        amount = Mathf.Clamp(amount, 0, stationedTroops.Count);

        List<UnitController> dispatchedTroops =
            new List<UnitController>();

        for (int i = 0; i < amount; i++)
        {
            int lastIndex = stationedTroops.Count - 1;
            UnitController troop = stationedTroops[lastIndex];
            stationedTroops.RemoveAt(lastIndex);

            if (troop != null)
                dispatchedTroops.Add(troop);
        }

        UpdateTroopCounter();
        return dispatchedTroops;
    }

    public void SetOwner(PlatformOwner newOwner)
    {
        if (owner == newOwner)
            return;

        owner = newOwner;
        UpdatePlatformVisual();
    }

    public void ReceiveTroop(UnitController troop)
    {
        if (troop == null || stationedTroops.Contains(troop))
            return;

        stationedTroops.Add(troop);
        UpdateTroopCounter();
    }

    private void UpdatePlatformVisual()
    {
        if (currentPlatformVisual != null)
            Destroy(currentPlatformVisual);

        GameObject platformPrefab = GetPlatformPrefab();

        if (platformPrefab == null)
        {
            Debug.LogWarning(
                name + ": No platform visual prefab is assigned for " +
                owner + ".",
                this
            );
            return;
        }

        currentPlatformVisual = Instantiate(
            platformPrefab,
            transform.position,
            transform.rotation,
            transform
        );
        currentPlatformVisual.transform.localPosition = Vector3.zero;
        currentPlatformVisual.transform.localRotation = Quaternion.identity;
        currentPlatformVisual.transform.localScale = Vector3.one;
    }

    private GameObject GetPlatformPrefab()
    {
        switch (owner)
        {
            case PlatformOwner.Player:
                return playerPlatformPrefab;

            case PlatformOwner.AI:
                return aiPlatformPrefab;

            default:
                return neutralPlatformPrefab;
        }
    }

    private void UpdateSelectionHighlight()
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(isSelected);
    }

    private void UpdateTroopCounter()
    {
        if (troopCountLabel != null)
            troopCountLabel.text = stationedTroops.Count + "/" + maxTroops;
    }
}
