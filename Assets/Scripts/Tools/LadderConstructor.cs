using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class LadderConstructor : MonoBehaviour
{
    [Header("Ячейка лестницы")]
    [Tooltip("Префаб ячейки — обычно это тот же префаб, на который навешен этот скрипт.")]
    public GameObject cellPrefab;

    [Header("Интерактивные зоны")]
    [Tooltip("Префаб зоны в начале лестницы (у нижней ячейки).")]
    public GameObject startZonePrefab;

    [Tooltip("Смещение зоны в начале лестницы (локальные координаты).")]
    public Vector3 startZoneOffset = Vector3.zero;

    [Tooltip("Размер (масштаб) зоны в начале лестницы.")]
    public Vector3 startZoneScale = Vector3.one;

    [Tooltip("Префаб зоны в конце лестницы (у верхней ячейки).")]
    public GameObject endZonePrefab;

    [Tooltip("Смещение зоны в конце лестницы (локальные координаты).")]
    public Vector3 endZoneOffset = Vector3.zero;

    [Tooltip("Размер (масштаб) зоны в конце лестницы.")]
    public Vector3 endZoneScale = Vector3.one;

    [Header("Параметры")]
    [Tooltip("Высота одной ячейки.")]
    public float cellHeight = 1f;

    [Tooltip("Смещение пивота модели ячейки от её нижнего края (в метрах). " +
             "Если пивот в центре модели — это cellHeight / 2. Влияет только на гизмо.")]
    public float cellPivotOffset = 0.5f;

    [Min(0)]
    [Tooltip("Количество ячеек лестницы. Сам объект с этим компонентом ячейкой не является — это точка старта построения.")]
    public int cellCount = 0;

    [SerializeField, HideInInspector]
    private List<Transform> spawnedCells = new List<Transform>();

    [SerializeField, HideInInspector]
    private Transform spawnedStartZone;

    [SerializeField, HideInInspector]
    private Transform spawnedEndZone;

    [SerializeField, HideInInspector]
    private GameObject lastStartZonePrefab;

    [SerializeField, HideInInspector]
    private GameObject lastEndZonePrefab;

    public float TotalHeight => cellCount * cellHeight;
    
    private void Awake()
    {
        // На старте сцены зоны уже могут быть созданы в редакторе,
        // поэтому просто прокидываем ссылки между LadderInteractor
        LinkZones();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;

        var startChanged = startZonePrefab != lastStartZonePrefab;
        var endChanged = endZonePrefab != lastEndZonePrefab;

        if (startChanged || endChanged)
        {
            lastStartZonePrefab = startZonePrefab;
            lastEndZonePrefab = endZonePrefab;

            EditorApplication.delayCall += () =>
            {
                if (this) Construct();
            };
        }
        else if (spawnedStartZone || spawnedEndZone)
        {
            EditorApplication.delayCall += () =>
            {
                if (this) UpdateZones();
            };
        }
#endif
    }

    public void Construct()
    {
        if (!cellPrefab)
        {
            Debug.LogWarning("LadderConstructor: не назначен cellPrefab.", this);
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && IsEditingSourcePrefabInPlace())
        {
            return;
        }
#endif
        spawnedCells.RemoveAll(c => !c);

        if (cellCount > spawnedCells.Count)
        {
            for (var i = spawnedCells.Count; i < cellCount; i++)
                AddCell(i);
        }
        else if (cellCount < spawnedCells.Count)
        {
            for (var i = spawnedCells.Count - 1; i >= cellCount; i--)
                RemoveCellAt(i);
        }

        UpdateZones();
    }

#if UNITY_EDITOR
   
    private static GameObject ResolvePrefabAsset(GameObject obj)
    {
        if (!obj) return null;

        var assetType = PrefabUtility.GetPrefabAssetType(obj);

        if (assetType != PrefabAssetType.NotAPrefab && !PrefabUtility.IsPartOfPrefabInstance(obj))
            return obj;

        if (PrefabUtility.IsPartOfPrefabInstance(obj))
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (source) return source;
        }

        return null;
    }

    private bool IsEditingSourcePrefabInPlace()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (!stage) return false;
        if (!stage.IsPartOfPrefabContents(gameObject)) return false;

        var stagePath = stage.assetPath;
        var sourceAsset = ResolvePrefabAsset(cellPrefab);
        var cellPrefabPath = sourceAsset ? AssetDatabase.GetAssetPath(sourceAsset) : null;
        return !string.IsNullOrEmpty(stagePath) && stagePath == cellPrefabPath;
    }
#endif

    private void AddCell(int index)
    {
        GameObject cell;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (cellPrefab == gameObject)
            {
                Debug.LogError(
                    "LadderConstructor: cellPrefab указывает на сам этот объект в сцене! " +
                    "Перетащи в это поле файл префаба из окна Project, а не объект из Hierarchy.",
                    this);
                return;
            }

            var sourceAsset = ResolvePrefabAsset(cellPrefab);

            if (sourceAsset)
            {
                cell = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset, transform);
            }
            else
            {
                cell = Instantiate(cellPrefab, transform);
            }

            if (!cell)
            {
                Debug.LogError("LadderConstructor: не удалось создать ячейку.", this);
                return;
            }

            Undo.RegisterCreatedObjectUndo(cell, "Add Ladder Cell");
        }
        else
#endif
        {
            cell = Instantiate(cellPrefab, transform);
        }

        cell.name = $"{cellPrefab.name}_{(index + 1):00}";
        cell.transform.localPosition = new Vector3(0f, index * cellHeight, 0f);
        cell.transform.localRotation = Quaternion.identity;
        cell.transform.localScale = Vector3.one;

        var childConstructor = cell.GetComponent<LadderConstructor>();
        if (childConstructor)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(childConstructor);
            else
#endif
                Destroy(childConstructor);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var meshSourceAsset = ResolvePrefabAsset(cellPrefab);
            if (meshSourceAsset)
            {
                SyncMeshes(meshSourceAsset, cell);
            }

            RebuildProBuilderMeshIfPresent(cell);
        }
#endif

        spawnedCells.Add(cell.transform);
    }

#if UNITY_EDITOR
    
    private static void SyncMeshes(GameObject sourceAsset, GameObject instance)
    {
        var sourceFilters = sourceAsset.GetComponentsInChildren<MeshFilter>(true);
        var instanceFilters = instance.GetComponentsInChildren<MeshFilter>(true);
        var filterCount = Mathf.Min(sourceFilters.Length, instanceFilters.Length);
        for (var i = 0; i < filterCount; i++)
        {
            if (instanceFilters[i].sharedMesh != sourceFilters[i].sharedMesh)
                instanceFilters[i].sharedMesh = sourceFilters[i].sharedMesh;
        }

        var sourceColliders = sourceAsset.GetComponentsInChildren<MeshCollider>(true);
        var instanceColliders = instance.GetComponentsInChildren<MeshCollider>(true);
        var colliderCount = Mathf.Min(sourceColliders.Length, instanceColliders.Length);
        for (var i = 0; i < colliderCount; i++)
        {
            if (instanceColliders[i].sharedMesh != sourceColliders[i].sharedMesh)
                instanceColliders[i].sharedMesh = sourceColliders[i].sharedMesh;
        }
    }

    private static void RebuildProBuilderMeshIfPresent(GameObject cell)
    {
        var pbType = System.Type.GetType("UnityEngine.ProBuilder.ProBuilderMesh, Unity.ProBuilder");

        if (pbType == null)
        {
            foreach (var comp in cell.GetComponentsInChildren<Component>(true))
            {
                if (comp && comp.GetType().Name == "ProBuilderMesh")
                {
                    Debug.LogWarning(
                        $"LadderConstructor: найден компонент ProBuilderMesh, но по неверному имени сборки. " +
                        $"Реальный тип: {comp.GetType().AssemblyQualifiedName}. " +
                        "Пришли эту строку — поправлю reflection под твою версию ProBuilder.",
                        cell);
                    break;
                }
            }
            return;
        }

        var pbComponents = cell.GetComponentsInChildren(pbType, true);
        if (pbComponents.Length == 0) return;

        var toMesh =
            pbType.GetMethod("ToMesh", new System.Type[] { typeof(MeshTopology) });

        var refreshMaskType =
            pbType.Assembly.GetType("UnityEngine.ProBuilder.RefreshMask");
        var refresh = refreshMaskType != null
            ? pbType.GetMethod("Refresh", new System.Type[] { refreshMaskType })
            : pbType.GetMethod("Refresh", System.Type.EmptyTypes);
        var refreshAllValue = refreshMaskType != null
            ? System.Enum.Parse(refreshMaskType, "All")
            : null;

        var optimize =
            pbType.GetMethod("Optimize", new System.Type[] { typeof(bool) });

        foreach (var pb in pbComponents)
        {
            toMesh?.Invoke(pb, new object[] { MeshTopology.Triangles });

            if (refresh != null)
                refresh.Invoke(pb, refreshMaskType != null ? new object[] { refreshAllValue } : null);

            optimize?.Invoke(pb, new object[] { false });
        }
    }
#endif

    private void RemoveCellAt(int index)
    {
        var cell = spawnedCells[index];
        spawnedCells.RemoveAt(index);
        if (!cell) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.DestroyObjectImmediate(cell.gameObject);
        else
#endif
            Destroy(cell.gameObject);
    }

    private void UpdateZones()
    {
        if (cellCount <= 0)
        {
            DestroyZone(ref spawnedStartZone);
            DestroyZone(ref spawnedEndZone);
            return;
        }

        spawnedStartZone = EnsureZone(spawnedStartZone, startZonePrefab, "StartZone",
            new Vector3(0f, 0f, 0f) + startZoneOffset, startZoneScale);
        spawnedEndZone = EnsureZone(spawnedEndZone, endZonePrefab, "EndZone",
            new Vector3(0f, cellCount * cellHeight, 0f) + endZoneOffset, endZoneScale);

        LinkZones();
    }

    private void LinkZones()
    {
        LadderInteractor startInteractor = null;
        LadderInteractor endInteractor = null;

        if (spawnedStartZone)
        {
            startInteractor = spawnedStartZone.GetComponent<LadderInteractor>();
            if (!startInteractor)
            {
                Debug.LogWarning(
                    "LadderConstructor: экземпляр startZonePrefab не содержит компонент LadderInteractor.",
                    this);
            }
        }

        if (spawnedEndZone)
        {
            endInteractor = spawnedEndZone.GetComponent<LadderInteractor>();
            if (!endInteractor)
            {
                Debug.LogWarning(
                    "LadderConstructor: экземпляр endZonePrefab не содержит компонент LadderInteractor.",
                    this);
            }
        }

        if (startInteractor && endInteractor)
        {
            startInteractor.SetOtherEnd(endInteractor);
            endInteractor.SetOtherEnd(startInteractor);
        }
    }

    private Transform EnsureZone(Transform existing, GameObject prefab, string name, Vector3 localPos, Vector3 localScale)
    {
        if (!prefab)
        {
            DestroyZone(ref existing);
            return null;
        }

        if (existing && !IsZoneFromPrefab(existing, prefab))
        {
            DestroyZone(ref existing);
        }

        if (!existing)
        {
            existing = CreateZone(prefab, name);
        }

        if (!existing) return null;

        existing.localPosition = localPos;
        existing.localRotation = Quaternion.identity;
        existing.localScale = localScale;
        return existing;
    }

    private bool IsZoneFromPrefab(Transform zone, GameObject prefab)
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return true;

        var zoneSource = ResolvePrefabAsset(zone.gameObject);
        var prefabSource = ResolvePrefabAsset(prefab);
        return zoneSource && prefabSource && zoneSource == prefabSource;
#else
        return true;
#endif
    }

    private Transform CreateZone(GameObject prefab, string name)
    {
        GameObject zone = null;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var sourceAsset = ResolvePrefabAsset(prefab);

            if (sourceAsset)
            {
                zone = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset, transform);
            }
            else
            {
                zone = Instantiate(prefab, transform);
            }

            if (!zone)
            {
                Debug.LogError($"LadderConstructor: не удалось создать зону {name}.", this);
                return null;
            }

            Undo.RegisterCreatedObjectUndo(zone, $"Add {name}");
        }
        else
#endif
        {
            zone = Instantiate(prefab, transform);
        }

        zone.name = name;

        var childConstructor = zone.GetComponent<LadderConstructor>();
        if (childConstructor)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(childConstructor);
            else
#endif
                Destroy(childConstructor);
        }

        return zone.transform;
    }

    private void DestroyZone(ref Transform zone)
    {
        if (!zone) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Undo.DestroyObjectImmediate(zone.gameObject);
        else
#endif
            Destroy(zone.gameObject);

        zone = null;
    }

    public void ClearAll()
    {
        cellCount = 0;
        Construct();
    }

    private void OnDrawGizmos()
    {
        DrawGizmo(new Color(0.2f, 0.85f, 1f, 0.35f), new Color(0.2f, 0.85f, 1f, 0.9f));
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmo(new Color(1f, 0.6f, 0.1f, 0.45f), new Color(1f, 0.6f, 0.1f, 1f));
    }

    private void DrawGizmo(Color fillColor, Color lineColor)
    {
        var prevMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        const float plateThickness = 0.04f;
        var plateSize = new Vector3(1f, plateThickness, 1f);
        var bottomEdgeY = -cellPivotOffset;
        var plateCenter = new Vector3(0f, bottomEdgeY + plateThickness * 0.5f, 0f);

        Gizmos.color = fillColor;
        Gizmos.DrawCube(plateCenter, plateSize * 0.98f);
        Gizmos.color = lineColor;
        Gizmos.DrawWireCube(plateCenter, plateSize);

        var topY = bottomEdgeY + Mathf.Max(cellCount, 1) * cellHeight;
        Gizmos.DrawLine(new Vector3(0f, bottomEdgeY, 0f), new Vector3(0f, topY, 0f));

        var arrowSize = 0.15f;
        var top = new Vector3(0f, topY, 0f);
        Gizmos.DrawLine(top, top + new Vector3(arrowSize, -arrowSize, 0f));
        Gizmos.DrawLine(top, top + new Vector3(-arrowSize, -arrowSize, 0f));
        Gizmos.DrawLine(top, top + new Vector3(0f, -arrowSize, arrowSize));
        Gizmos.DrawLine(top, top + new Vector3(0f, -arrowSize, -arrowSize));

        Gizmos.matrix = prevMatrix;

#if UNITY_EDITOR
        Handles.color = lineColor;
        Handles.Label(
            transform.TransformPoint(new Vector3(0f, topY + 0.15f, 0f)),
            $"Ladder: {cellCount} ячеек ({cellCount * cellHeight:0.##} м)");
#endif
    }
}