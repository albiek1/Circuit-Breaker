using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipInterfaceEditor : InteractionHandler
{
    const int maxGroupSize = 8;

    public event System.Action<Chip> onDeleteChip;
    public event System.Action onChipsAddedOrDeleted;

    public enum EditorType { Input, Output }
    public enum HandleState { Default, Highlighted, Selected }
    const float forwardDepth = -0.1f;

    public List<ChipSignal> signals { get; private set; }

    public EditorType editorType;

    [Header("References")]
    public Transform chipContainer;
    public ChipSignal signalPrefab;
    public RectTransform propertiesUI;
    public TMPro.TMP_InputField nameField;
    public UnityEngine.UI.Button deleteButton;
    public UnityEngine.UI.Toggle twosComplementToggle;
    public Transform signalHolder;

    [Header("Apperance")]
    public Vector2 handleSize;
    public Color handleCol;
    public Color highlightedHandleCol;
    public Color selectedHandleCol;
    public float propertiesUIX;
    public Vector2 propertiesHeightMinMax;
    public bool showPreviewSignal;
    public float groupSpacing = 1;

    ChipSignal highlightedSignal;
    public List<ChipSignal> selectedSignals { get; private set; }
    ChipSignal[] previewSignals;

    BoxCollider2D inputBounds;

    Mesh quadMesh;
    Material handleMat;
    Material highlightedHandleMat;
    Material selectedHandleMat;
    bool mouseInInputBounds;

    bool isDragging;
    float dragHandleStartY;
    float dragMouseStartY;

    int currentGroupSize = 1;
    int currentGroupID;
    Dictionary<int, ChipSignal[]> groupsByID;

    private void Awake()
    {
        signals = new List<ChipSignal>();
        selectedSignals = new List<ChipSignal>();
        groupsByID = new Dictionary<int, ChipSignal[]>();

        inputBounds = GetComponent<BoxCollider2D>();
        MeshShapeCreator.CreateQuadMesh(ref quadMesh);
        handleMat = CreateUnlitMaterial(handleCol);
        highlightedHandleMat = CreateUnlitMaterial(highlightedHandleCol);
        selectedHandleMat = CreateUnlitMaterial(selectedHandleCol);

        previewSignals = new ChipSignal[maxGroupSize];
        for(int i = 0; i < maxGroupSize; i++)
        {
            var previewSignal = Instantiate(signalPrefab);
            previewSignal.SetInteractable(false);
            previewSignal.gameObject.SetActive(false);
            previewSignal.signalName = "Preview";
            previewSignal.transform.SetParent(transform, true);
            previewSignals[i] = previewSignal;
        }

        propertiesUI.gameObject.SetActive(false);
        deleteButton.onClick.AddListener(DeleteSelected);
    }

    public override void OrderedUpdate()
    {
        if (!InputHelper.MouseOverUIObject())
        {
            UpdateColors();
            HandleInput();
        }
        DrawSignalHandles();
    }

    public void LoadSignal(ChipSignal signal)
    {
        signal.transform.parent = signalHolder;
        signals.Add(signal);
    }

    void HandleInput()
    {
        Vector2 mousePos = InputHelper.MouseWorldPos;

        mouseInInputBounds = inputBounds.OverlapPoint(mousePos);
        if (mouseInInputBounds)
        {
            RequestFocus();
        }

        if (HasFocus)
        {
            highlightedSignal = GetSignalUnderMouse();

            if (highlightedSignal)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    SelectSignal(highlightedSignal);
                }
            }

            if(selectedSignals.Count > 0)
            {
                if (isDragging)
                {
                    float handleNewY = (mousePos.y + (dragHandleStartY - dragMouseStartY));
                    bool cancel = Input.GetKeyDown(KeyCode.Escape);
                    if (cancel)
                    {
                        handleNewY = dragHandleStartY;
                    }

                    for(int i = 0; i < selectedSignals.Count; i++)
                    {
                        float y = CalcY(handleNewY, selectedSignals.Count, i);
                        SetYPos(selectedSignals[i].transform, y);
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        isDragging = false;
                    }

                    if (cancel)
                    {
                        FocusLost();
                    }
                }

                UpdateUIProperties();

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    FocusLost();
                }
            }

            HidePreviews();
            if(highlightedSignal == null && !isDragging)
            {
                if (mouseInInputBounds)
                {
                    if(InputHelper.AnyOfTheseKeysDown(KeyCode.Plus, KeyCode.KeypadPlus, KeyCode.Equals))
                    {
                        currentGroupSize = Mathf.Clamp(currentGroupSize + 1, 1, maxGroupSize);
                    }
                    else if(InputHelper.AnyOfTheseKeysDown(KeyCode.Minus, KeyCode.KeypadMinus, KeyCode.Underscore))
                    {
                        currentGroupSize = Mathf.Clamp(currentGroupSize - 1, 1, maxGroupSize);
                    }

                    HandleSpawning();
                }
            }
        }
    }

    float CalcY(float mouseY, int groupSize, int index)
    {
        float centerY = mouseY;
        float halfExtent = groupSpacing * (groupSize - 1f);
        float maxY = centerY + halfExtent + handleSize.y / 2f;
        float minY = centerY - halfExtent - handleSize.y / 2f;

        if(maxY > BoundsTop)
        {
            centerY -= (maxY - BoundsTop);
        }
        else if(minY < BoundsBottom)
        {
            centerY += (BoundsBottom - minY);
        }

        float t = (groupSize > 1) ? index / (groupSize - 1f) : 0.5f;
        t = t * 2 - 1;
        float posY = centerY - t * halfExtent;
        return posY;
    }

    public ChipSignal[][] GetGroups()
    {
        var keys = groupsByID.Keys;
        ChipSignal[][] groups = new ChipSignal[keys.Count][];
        int i = 0;
        foreach(var key in keys)
        {
            groups[i] = groupsByID[key];
            i++;
        }
        return groups;
    }

    void HandleSpawning()
    {
        float containerX = chipContainer.position.x + chipContainer.localScale.x / 2 * ((editorType == EditorType.Input) ? -1 : 1);
        float centerY = ClampY(InputHelper.MouseWorldPos.y);

        if (Input.GetMouseButtonDown(0))
        {
            bool isGroup = currentGroupSize > 1;
            ChipSignal[] spawnedSignals = new ChipSignal[currentGroupSize];

            for(int i = 0; i < currentGroupSize; i++)
            {
                float posY = CalcY(InputHelper.MouseWorldPos.y, currentGroupSize, i);
                Vector3 spawnPos = new Vector3(containerX, posY, chipContainer.position.z + forwardDepth);

                ChipSignal spawnedSignal = Instantiate(signalPrefab, spawnPos, Quaternion.identity, signalHolder);
                if (isGroup)
                {
                    spawnedSignal.GroupID = currentGroupID;
                    spawnedSignal.displayGroupDecimalValue = true;
                }
                signals.Add(spawnedSignal);
                spawnedSignals[i] = spawnedSignal;
            }

            if (isGroup)
            {
                groupsByID.Add(currentGroupID, spawnedSignals);
                currentGroupSize = 1;
                currentGroupID++;
            }
            SelectSignal(signals[signals.Count - 1]);
            onChipsAddedOrDeleted?.Invoke();
        }
        else
        {
            for(int i = 0; i < currentGroupSize; i++)
            {
                float posY = CalcY(InputHelper.MouseWorldPos.y, currentGroupSize, i);
                Vector3 spawnPos = new Vector3(containerX, posY, chipContainer.position.z + forwardDepth);
                DrawHandle(posY, HandleState.Highlighted);
                if (showPreviewSignal)
                {
                    previewSignals[i].gameObject.SetActive(true);
                    previewSignals[i].transform.position = spawnPos - Vector3.forward * forwardDepth;
                }
            }
        }
    }

    public void HandleSpawning(float centerY)
    {
        float containerX = chipContainer.position.x + chipContainer.localScale.x / 2 * ((editorType == EditorType.Input) ? -1 : 1);
        bool isGroup = currentGroupSize > 1;
        ChipSignal[] spawnedSignals = new ChipSignal[currentGroupSize];

        for(int i = 0; i < currentGroupSize; i++)
        {
            float posY = CalcY(InputHelper.MouseWorldPos.y, currentGroupSize, i);
            Vector3 spawnPos = new Vector3(containerX, centerY, chipContainer.position.z + forwardDepth);

            ChipSignal spawnedSignal = Instantiate(signalPrefab, spawnPos, Quaternion.identity, signalHolder);
            if (isGroup)
            {
                spawnedSignal.GroupID = currentGroupID;
                spawnedSignal.displayGroupDecimalValue = true;
            }
            signals.Add(spawnedSignal);
            spawnedSignals[i] = spawnedSignal;
        }

        if (isGroup)
        {
            groupsByID.Add(currentGroupID, spawnedSignals);
            currentGroupSize = 1;
            currentGroupID++;
        }
        //SelectSignal(signals[signals.Count - 1]);
        onChipsAddedOrDeleted?.Invoke();
    }

    void HidePreviews()
    {
        for(int i = 0; i < previewSignals.Length; i++)
        {
            previewSignals[i].gameObject.SetActive(false);
        }
    }

    float BoundsTop
    {
        get
        {
            return transform.position.y + transform.localScale.y / 2;
        }
    }

    float BoundsBottom
    {
        get
        {
            return transform.position.y - transform.localScale.y / 2;
        }
    }

    float ClampY(float y)
    {
        return Mathf.Clamp(y, BoundsBottom + handleSize.y / 2f, BoundsTop - handleSize.y / 2f);
    }

    protected override void FocusLost()
    {
        highlightedSignal = null;
        selectedSignals.Clear();
        propertiesUI.gameObject.SetActive(false);

        HidePreviews();
        currentGroupSize = 1;
    }

    void UpdateUIProperties()
    {
        if(selectedSignals.Count > 0)
        {
            Vector3 center = (selectedSignals[0].transform.position + selectedSignals[selectedSignals.Count - 1].transform.position) / 2;
            propertiesUI.transform.position = new Vector3(center.x + propertiesUIX, center.y, propertiesUI.transform.position.z);

            for(int i = 0; i < selectedSignals.Count; i++)
            {
                selectedSignals[i].UpdateSignalName(nameField.text);
                selectedSignals[i].useTwosComplement = twosComplementToggle.isOn;
            }
        }
    }

    void DrawSignalHandles()
    {
        for (int i = 0; i < signals.Count; i++)
        {
            HandleState handleState = HandleState.Default;
            if (signals[i] == highlightedSignal)
            {
                handleState = HandleState.Highlighted;
            }
            if (selectedSignals.Contains(signals[i]))
            {
                handleState = HandleState.Selected;
            }
            DrawHandle(signals[i].transform.position.y, handleState);
        }
    }

    ChipSignal GetSignalUnderMouse()
    {
        ChipSignal signalUnderMouse = null;
        float nearestDst = float.MaxValue;

        for(int i = 0; i < signals.Count; i++)
        {
            ChipSignal currentSignal = signals[i];
            float handleY = currentSignal.transform.position.y;

            Vector2 handleCenter = new Vector2(transform.position.x, handleY);
            Vector2 mousePos = InputHelper.MouseWorldPos;

            const float selectionBufferY = 0.1f;

            float halfSizeX = transform.localScale.x / 2f;
            float halfSizeY = (handleSize.y + selectionBufferY) / 2f;
            bool insideX = mousePos.x >= handleCenter.x - halfSizeX && mousePos.x <= handleCenter.x + halfSizeX;
            bool insideY = mousePos.y >= handleCenter.y - halfSizeY && mousePos.y <= handleCenter.y + halfSizeY;

            if(insideX && insideY)
            {
                float dst = Mathf.Abs(mousePos.y - handleY);
                if(dst < nearestDst)
                {
                    nearestDst = dst;
                    signalUnderMouse = currentSignal;
                }
            }
        }
        return signalUnderMouse;
    }

    void SelectSignal(ChipSignal signalToDrag)
    {
        selectedSignals.Clear();
        for(int i = 0; i < signals.Count; i++)
        {
            if (signals[i] == signalToDrag || ChipSignal.InSameGroup(signals[i], signalToDrag))
            {
                selectedSignals.Add(signals[i]);
            }
        }
        bool isGroup = selectedSignals.Count > 1;

        isDragging = true;

        dragMouseStartY = InputHelper.MouseWorldPos.y;
        if(selectedSignals.Count % 2 == 0)
        {
            int indexA = Mathf.Max(0, selectedSignals.Count / 2 - 1);
            int indexB = selectedSignals.Count / 2;
            dragHandleStartY = (selectedSignals[indexA].transform.position.y + selectedSignals[indexB].transform.position.y) / 2f;
        }
        else
        {
            dragHandleStartY = selectedSignals[selectedSignals.Count / 2].transform.position.y;
        }

        propertiesUI.gameObject.SetActive(true);
        propertiesUI.sizeDelta = new Vector2(propertiesUI.sizeDelta.x, (isGroup) ? propertiesHeightMinMax.y : propertiesHeightMinMax.x);
        nameField.text = selectedSignals[0].signalName;
        nameField.Select();
        nameField.caretPosition = nameField.text.Length;
        twosComplementToggle.gameObject.SetActive(isGroup);
        twosComplementToggle.isOn = selectedSignals[0].useTwosComplement;
        UpdateUIProperties();
    }

    void DeleteSelected()
    {
        for(int i = selectedSignals.Count - 1; i >= 0; i--)
        {
            ChipSignal signalToDelete = selectedSignals[i];
            if (groupsByID.ContainsKey(signalToDelete.GroupID))
            {
                groupsByID.Remove(signalToDelete.GroupID);
            }
            onDeleteChip?.Invoke(signalToDelete);
            signals.Remove(signalToDelete);
            Destroy(signalToDelete.gameObject);
        }
        onChipsAddedOrDeleted?.Invoke();
        selectedSignals.Clear();
        FocusLost();
    }

    void DrawHandle(float y, HandleState handleState = HandleState.Default)
    {
        float renderZ = forwardDepth;
        Material currentHandleMat;
        switch (handleState)
        {
            case HandleState.Highlighted:
                currentHandleMat = highlightedHandleMat;
                break;
            case HandleState.Selected:
                currentHandleMat = selectedHandleMat;
                renderZ = forwardDepth * 2;
                break;
            default:
                currentHandleMat = handleMat;
                break;
        }

        Vector3 scale = new Vector3(handleSize.x, handleSize.y, 1);
        Vector3 pos3D = new Vector3(transform.position.x, y, transform.position.z + renderZ);
        Matrix4x4 handleMatrix = Matrix4x4.TRS(pos3D, Quaternion.identity, scale);
        Graphics.DrawMesh(quadMesh, handleMatrix, currentHandleMat, 0);
    }

    Material CreateUnlitMaterial(Color col)
    {
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = col;
        return mat;
    }

    void SetYPos(Transform t, float y)
    {
        t.position = new Vector3(t.position.x, y, t.position.z);
    }

    void UpdateColors()
    {
        handleMat.color = handleCol;
        highlightedHandleMat.color = highlightedHandleCol;
        selectedHandleMat.color = selectedHandleCol;
    }
}
