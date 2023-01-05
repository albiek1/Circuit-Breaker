using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinAndWireInteraction : InteractionHandler
{
    public event System.Action onConnectionChanged;
    public event System.Action<Pin> onMouseOverPin;
    public event System.Action<Pin> onMouseExitPin;

    enum State { None, PlacingWire}
    public LayerMask pinMask;
    public LayerMask wireMask;
    public Transform wireHolder;
    public Wire wirePrefab;

    State currentState;
    Pin pinUnderMouse;
    Pin wireStartPin;
    Wire wireToPlace;
    Wire highlightedWire;
    Dictionary<Pin, Wire> wiresByChipInputPin;
    public List<Wire> allWires { get; private set; }

    private void Awake()
    {
        allWires = new List<Wire>();
        wiresByChipInputPin = new Dictionary<Pin, Wire>();
    }

    public void Init(ChipInteraction chipInteraction, ChipInterfaceEditor inputEditor, ChipInterfaceEditor outputEditor)
    {
        chipInteraction.onDeleteChip += DeleteChipWires;
        inputEditor.onDeleteChip += DeleteChipWires;
        outputEditor.onDeleteChip += DeleteChipWires;
    }

    public override void OrderedUpdate()
    {
        bool mouseOverUI = InputHelper.MouseOverUIObject();

        if (!mouseOverUI)
        {
            HandlePinHighlighting();

            switch (currentState)
            {
                case State.None:
                    HandleWireHighlighting();
                    HandleWireDeletion();
                    HandleWireCreation();
                    break;
                case State.PlacingWire:
                    HandleWirePlacement();
                    break;
            }
        }
    }

    void HandleWireHighlighting()
    {
        var wireUnderMouse = InputHelper.GetObjectUnderMouse2D(wireMask);
        if(wireUnderMouse && pinUnderMouse == null)
        {
            if (highlightedWire)
            {
                highlightedWire.SetSelectionState(false);
            }
            highlightedWire = wireUnderMouse.GetComponent<Wire>();
            highlightedWire.SetSelectionState(true);
        }
        else if (highlightedWire)
        {
            highlightedWire.SetSelectionState(false);
            highlightedWire = null;
        }
    }

    void HandleWireDeletion()
    {
        if (highlightedWire)
        {
            if(InputHelper.AnyOfTheseKeysDown(KeyCode.Backspace, KeyCode.Delete))
            {
                RequestFocus();
                if (HasFocus)
                {
                    DestroyWire(highlightedWire);
                    onConnectionChanged?.Invoke();
                }
            }
        }
    }

    void HandleWirePlacement()
    {
        if(InputHelper.AnyOfTheseKeysDown(KeyCode.Escape, KeyCode.Backspace, KeyCode.Delete) || Input.GetMouseButtonDown(1))
        {
            StopPlacingWire();
        }
        else
        {
            Vector2 mousePos = InputHelper.MouseWorldPos;

            wireToPlace.UpdateWireEndPoint(mousePos);

            if (Input.GetMouseButtonDown(0))
            {
                if (pinUnderMouse)
                {
                    TryPlaceWire(wireStartPin, pinUnderMouse);
                }
                else
                {
                    wireToPlace.AddAnchorPoint(mousePos);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if(pinUnderMouse && pinUnderMouse != wireStartPin)
                {
                    TryPlaceWire(wireStartPin, pinUnderMouse);
                }
            }
        }
    }

    public Wire GetWire(Pin childPin)
    {
        if (wiresByChipInputPin.ContainsKey(childPin))
        {
            return wiresByChipInputPin[childPin];
        }
        return null;
    }

    void TryPlaceWire(Pin startPin, Pin endPin)
    {
        if(Pin.IsValidConnection(startPin, endPin))
        {
            Pin chipInputPin = (startPin.pinType == Pin.PinType.ChipInput) ? startPin : endPin;
            RemoveConflictingWire(chipInputPin);

            wireToPlace.Place(endPin);
            Pin.MakeConnection(startPin, endPin);
            allWires.Add(wireToPlace);
            wiresByChipInputPin.Add(chipInputPin, wireToPlace);
            wireToPlace = null;
            currentState = State.None;

            onConnectionChanged?.Invoke();
        }
        else
        {
            StopPlacingWire();
        }
    }

    void RemoveConflictingWire(Pin chipInputPin)
    {
        if (wiresByChipInputPin.ContainsKey(chipInputPin))
        {
            DestroyWire(wiresByChipInputPin[chipInputPin]);
        }
    }

    void DestroyWire(Wire wire)
    {
        wiresByChipInputPin.Remove(wire.ChipInputPin);
        allWires.Remove(wire);
        Pin.RemoveConnection(wire.startPin, wire.endPin);
        Destroy(wire.gameObject);
    }

    void HandleWireCreation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(pinUnderMouse || highlightedWire)
            {
                RequestFocus();
                if (HasFocus)
                {
                    currentState = State.PlacingWire;
                    wireToPlace = Instantiate(wirePrefab, parent: wireHolder);

                    if (pinUnderMouse)
                    {
                        wireStartPin = pinUnderMouse;
                        wireToPlace.ConnectToFirstPin(wireStartPin);
                    }
                    else if (highlightedWire)
                    {
                        wireStartPin = highlightedWire.ChipOutputPin;
                        wireToPlace.ConnectToFirstPinViaWire(wireStartPin, highlightedWire, InputHelper.MouseWorldPos);
                    }
                }
            }
        }
    }

    void HandlePinHighlighting()
    {
        Vector2 mousePos = InputHelper.MouseWorldPos;
        Collider2D pinCollider = Physics2D.OverlapCircle(mousePos, Pin.interactionRadius - Pin.radius, pinMask);
        if (pinCollider)
        {
            Pin newPinUnderMouse = pinCollider.GetComponent<Pin>();
            if(pinUnderMouse != newPinUnderMouse)
            {
                if(pinUnderMouse != null)
                {
                    pinUnderMouse.MouseExit();
                    onMouseExitPin?.Invoke(pinUnderMouse);
                }
            }
            newPinUnderMouse.MouseEnter();
            pinUnderMouse = newPinUnderMouse;
            onMouseOverPin?.Invoke(pinUnderMouse);
        }
        else
        {
            if (pinUnderMouse)
            {
                pinUnderMouse.MouseExit();
                onMouseExitPin?.Invoke(pinUnderMouse);
                pinUnderMouse = null;
            }
        }
    }

    void DeleteChipWires(Chip chip)
    {
        List<Wire> wiresToDestroy = new List<Wire>();

        foreach(var outputPin in chip.outputPins)
        {
            foreach(var childPin in outputPin.childPins)
            {
                wiresToDestroy.Add(wiresByChipInputPin[childPin]);
            }
        }

        foreach(var inputPin in chip.inputPins)
        {
            if (inputPin.parentPin)
            {
                wiresToDestroy.Add(wiresByChipInputPin[inputPin]);
            }
        }

        for(int i = 0; i < wiresToDestroy.Count; i++)
        {
            DestroyWire(wiresToDestroy[i]);
        }
        onConnectionChanged?.Invoke();
    }

    void StopPlacingWire()
    {
        if (wireToPlace)
        {
            Destroy(wireToPlace.gameObject);
            wireToPlace = null;
            wireStartPin = null;
        }
        currentState = State.None;
    }

    protected override void FocusLost()
    {
        if (pinUnderMouse)
        {
            pinUnderMouse.MouseExit();
            pinUnderMouse = null;
        }

        if (highlightedWire)
        {
            highlightedWire.SetSelectionState(false);
            highlightedWire = null;
        }
        currentState = State.None;
    }

    protected override bool CanReleaseFocus()
    {
        if(currentState == State.PlacingWire || pinUnderMouse)
        {
            return false;
        }
        return true;
    }
}
