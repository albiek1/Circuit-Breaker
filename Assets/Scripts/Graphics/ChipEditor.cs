using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipEditor : MonoBehaviour
{
    public Transform chipImplementationHolder;
    public Transform wireHolder;
    public ChipInterfaceEditor inputsEditor;
    public ChipInterfaceEditor outputsEditor;
    public ChipInteraction chipInteraction;
    public PinAndWireInteraction pinAndWireInteraction;

    [HideInInspector]
    public string chipName;
    [HideInInspector]
    public Color chipColor;
    [HideInInspector]
    public Color chipNameColor;
    [HideInInspector]
    public int creationIndex;

    private void Awake()
    {
        InteractionHandler[] allHandlers = { inputsEditor, outputsEditor, chipInteraction, pinAndWireInteraction };
        foreach(var handler in allHandlers)
        {
            handler.InitAllHandlers(allHandlers);
        }

        pinAndWireInteraction.Init(chipInteraction, inputsEditor, outputsEditor);
        pinAndWireInteraction.onConnectionChanged += OnChipNetworkModified;
        GetComponentInChildren<Canvas>().worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        inputsEditor.OrderedUpdate();
        outputsEditor.OrderedUpdate();
        pinAndWireInteraction.OrderedUpdate();
        chipInteraction.OrderedUpdate();
    }

    void OnChipNetworkModified()
    {
        CycleDetector.MarkAllCycles(this);
    }
}
