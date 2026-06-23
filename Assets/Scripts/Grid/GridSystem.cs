using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public interface IBuildCommand
{
    void Execute();
    void Undo();
}

public class GridSystem : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ObjectToPlace;
    public GameObject SelectorPrefab;
    public float GridSize = 1f;
    public int GridRange;
    public Color GridColor = new Color(0, 0, 0, 0.2f);
    public float BuildRate = 0.1f;
    public int MaxObjects = 20;

    public float UndoRate = 0.05f;
    public float UndoHoldDelay = 0.4f;

    public event Action<int> OnGridStateChanged;

    private float SelectorYOffset = 0.05f;
    private Vector3 currentGridPosition;
    private GameObject selectorObject;
    private Renderer[] selectorRenderers;   
    public Dictionary<Vector3, GameObject> PlacedPipes = new Dictionary<Vector3, GameObject>();
    private bool canPlaceAtCurrentLocation = false;
    private Material gridMaterial;
    private float nextActionTime;
    private float nextUndoTime;

    private Stack<IBuildCommand> undoStack = new Stack<IBuildCommand>();
    private Stack<IBuildCommand> redoStack = new Stack<IBuildCommand>();

    private void Start()
    {
        CreateSelectorObject(); 
        gridMaterial = new Material(Shader.Find("Sprites/Default"));

        if (TryGetComponent<MeshRenderer>(out var runtimeMesh))
        {
            runtimeMesh.material.renderQueue = 2001;
        }

        NotifyUI();
    }

    void CreateSelectorObject()
    {
        selectorObject = Instantiate(SelectorPrefab);

        selectorRenderers = selectorObject.GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        UpdateSelectorPosition();

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (selectorObject != null) selectorObject.SetActive(false);
            return;
        }

        if (Time.time >= nextActionTime)
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                PlaceObject();
                nextActionTime = Time.time + BuildRate;
            }
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                DeleteObject();
                nextActionTime = Time.time + BuildRate;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.zKey.wasPressedThisFrame)
            {
                Undo();
                nextUndoTime = Time.time + UndoHoldDelay;
            }
            else if (Keyboard.current.zKey.isPressed && Time.time >= nextUndoTime)
            {
                Undo();
                nextUndoTime = Time.time + UndoRate;
            }

            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                Redo();
            }
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetGrid();
            }
        }
    }

    void UpdateSelectorPosition()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (selectorObject != null) selectorObject.SetActive(false);
            return;
        }

        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            currentGridPosition = GetSnappedPosition(hit.point);

            bool isSurfaceValid = hit.collider.CompareTag("Placeable");
            bool isSpaceFree = !PlacedPipes.ContainsKey(currentGridPosition); 
            bool isUnderLimit = PlacedPipes.Count < MaxObjects;

            canPlaceAtCurrentLocation = isSurfaceValid && isSpaceFree && isUnderLimit;

            Vector3 visualPosition = currentGridPosition + new Vector3(0, SelectorYOffset, 0);
            selectorObject.transform.position = visualPosition;

            if (canPlaceAtCurrentLocation)
            {
                SetSelectorColor(new Color(0, 1, 0, 0.5f));
                selectorObject.SetActive(true);
            }
            else if (!isSpaceFree || !isUnderLimit)
            {
                SetSelectorColor(new Color(1, 0, 0, 0.5f));
                selectorObject.SetActive(true);
            }
            else
            {
                selectorObject.SetActive(false);
            }
        }
        else
        {
            selectorObject.SetActive(false);
        }
    }

    void SetSelectorColor(Color color)
    {
        if (selectorRenderers == null) return;

        foreach (var renderer in selectorRenderers)
        {
            renderer.material.color = color;
        }
    }

    private void NotifyUI()
    {
        int objectsLeft = MaxObjects - PlacedPipes.Count;

        OnGridStateChanged?.Invoke(objectsLeft);
    }

    Vector3 GetSnappedPosition(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / GridSize) * GridSize,
            Mathf.Round(position.y / GridSize) * GridSize,
            Mathf.Round(position.z / GridSize) * GridSize
        );
    }

    void PlaceObject()
    {
        if (!canPlaceAtCurrentLocation) return;

        Vector3 placementPosition = selectorObject.transform.position;

        IBuildCommand command = new PlaceCommand(this, ObjectToPlace, currentGridPosition);
        ExecuteNewCommand(command);

        canPlaceAtCurrentLocation = false;
    }

    void DeleteObject()
    {
        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("PlacedObject") || hit.collider.transform.root.CompareTag("PlacedObject"))
            {
                GameObject targetObject = hit.collider.transform.root.gameObject;

                Vector3 gridPos = GetSnappedPosition(targetObject.transform.position);

                IBuildCommand command = new DeleteCommand(this, targetObject, gridPos);
                ExecuteNewCommand(command);
            }
        }
    }

    private void ExecuteNewCommand(IBuildCommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear();
        NotifyUI();
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;
        IBuildCommand command = undoStack.Pop();
        command.Undo();
        redoStack.Push(command);
        NotifyUI();
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;
        IBuildCommand command = redoStack.Pop();
        command.Execute();
        undoStack.Push(command);
        NotifyUI();
    }
    private void OnRenderObject()
    {
        if (gridMaterial == null) return;

        gridMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(GridColor);

        float offsetX = 0.5f;
        float offsetZ = -0.5f;

        for (float i = -GridRange; i <= GridRange; i += GridSize)
        {
            GL.Vertex3(-GridRange + offsetX, 0, i + offsetZ);
            GL.Vertex3(GridRange + offsetX, 0, i + offsetZ);

            GL.Vertex3(i + offsetX, 0, -GridRange + offsetZ);
            GL.Vertex3(i + offsetX, 0, GridRange + offsetZ);
        }
        GL.End();
    }

    public void ResetGrid()
    {
        GameObject.FindGameObjectsWithTag("PlacedObject").ToList().ForEach(Destroy);

        undoStack.Clear();
        redoStack.Clear();
        PlacedPipes.Clear();
        NotifyUI(); 
    }

    public void UpdatePipeNetwork(Vector3 targetPosition)
    {
        if (PlacedPipes.TryGetValue(targetPosition, out GameObject centralPipe))
        {
            if (centralPipe.TryGetComponent<PipeNode>(out var node))
            {
                node.CheckNeighborsAndSetShape(PlacedPipes, targetPosition, GridSize);
            }
        }

        Vector3[] neighbors = new Vector3[]
        {
        targetPosition + Vector3.forward * GridSize,
        targetPosition + Vector3.back * GridSize,
        targetPosition + Vector3.right * GridSize,
        targetPosition + Vector3.left * GridSize
        };

        foreach (Vector3 neighborPos in neighbors)
        {
            if (PlacedPipes.TryGetValue(neighborPos, out GameObject neighborPipe))
            {
                if (neighborPipe.TryGetComponent<PipeNode>(out var node))
                {
                    node.CheckNeighborsAndSetShape(PlacedPipes, neighborPos, GridSize);
                }
            }
        }
    }
}

public class PlaceCommand : IBuildCommand
{
    private GridSystem gridSystem;
    private GameObject prefab;
    private Vector3 position;
    private GameObject spawnedObject;

    public PlaceCommand(GridSystem system, GameObject prefab, Vector3 pos)
    {
        this.gridSystem = system;
        this.prefab = prefab;
        this.position = pos;
    }

    public void Execute()
    {
        if (gridSystem.PlacedPipes.ContainsKey(position)) return;

        spawnedObject = Object.Instantiate(prefab, position, Quaternion.identity);
        spawnedObject.tag = "PlacedObject";
        gridSystem.PlacedPipes.Add(position, spawnedObject);

        gridSystem.UpdatePipeNetwork(position);
    }

    public void Undo()
    {
        if (spawnedObject != null)
        {
            gridSystem.PlacedPipes.Remove(position);
            spawnedObject.SetActive(false);

            gridSystem.UpdatePipeNetwork(position);
        }
    }
}

public class DeleteCommand : IBuildCommand
{
    private GridSystem gridSystem;
    private GameObject objectToDelete;
    private Vector3 position;

    public DeleteCommand(GridSystem system, GameObject target, Vector3 pos)
    {
        this.gridSystem = system;
        this.objectToDelete = target;
        this.position = pos;
    }

    public void Execute()
    {
        if (objectToDelete == null) return;

        gridSystem.PlacedPipes.Remove(position);
        objectToDelete.SetActive(false);

        gridSystem.UpdatePipeNetwork(position);
    }

    public void Undo()
    {
        if (objectToDelete != null)
        {
            objectToDelete.SetActive(true);
            gridSystem.PlacedPipes.Add(position, objectToDelete);

            gridSystem.UpdatePipeNetwork(position);
        }
    }
}