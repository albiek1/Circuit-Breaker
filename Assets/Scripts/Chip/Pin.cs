using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pin : MonoBehaviour
{
    public enum PinType { ChipInput, ChipOutput }
    public PinType pinType;

    public Chip chip;
    public string pinName;

    [HideInInspector] public bool cyclic = false;

    [HideInInspector] public int index;

    [HideInInspector] public Pin parentPin;

    [HideInInspector] public List<Pin> childPins = new List<Pin>();

    int currentState;

    Color defaultCol = Color.black;
    Color interactCol = new Color(0.7f, 0.7f, 0.7f);
    Material material;

    public static float radius
    {
        get
        {
            float diameter = 0.215f;
            return diameter / 2;
        }
    }

    public static float interactionRadius
    {
        get
        {
            return radius * 1.1f;
        }
    }

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().material;
        material.color = defaultCol;
    }

    private void Start()
    {
        SetScale();
    }

    public void SetScale()
    {
        transform.localScale = Vector3.one * radius * 2;
    }

    public int State
    {
        get
        {
            return currentState;
        }
    }

    public bool HasParent
    {
        get
        {
            return parentPin != null || pinType == PinType.ChipOutput;
        }
    }

    public void ReceiveSignal(int signal)
    {
        currentState = signal;

        if (pinType == PinType.ChipInput && !cyclic)
        {
            chip.ReceiveInputSignal(this);
        }
        else if(pinType == PinType.ChipOutput)
        {
            for(int i = 0; i < childPins.Count; i++)
            {
                childPins[i].ReceiveSignal(signal);
            }
        }
    }

    public static void MakeConnection(Pin pinA, Pin pinB)
    {
        if(IsValidConnection(pinA, pinB))
        {
            Pin parentPin = (pinA.pinType == PinType.ChipOutput) ? pinA : pinB;
            Pin childPin = (pinA.pinType == PinType.ChipInput) ? pinA : pinB;

            parentPin.childPins.Add(childPin);
            childPin.parentPin = parentPin;
        }
    }

    public static void RemoveConnection(Pin pinA, Pin pinB)
    {
        Pin parentPin = (pinA.pinType == PinType.ChipOutput) ? pinA : pinB;
        Pin childPin = (pinA.pinType == PinType.ChipInput) ? pinA : pinB;

        parentPin.childPins.Remove(childPin);
        childPin.parentPin = null;
    }

    public static bool IsValidConnection(Pin pinA, Pin pinB)
    {
        return pinA.pinType != pinB.pinType;
    }

    public static bool TryConnect(Pin pinA, Pin pinB)
    {
        if(pinA.pinType != pinB.pinType)
        {
            Debug.Log("trying " + pinA.pinName + " and " + pinB.pinName);
            Pin parentPin = (pinA.pinType == PinType.ChipOutput) ? pinA : pinB;
            Pin childPin = (parentPin == pinB) ? pinA : pinB;
            parentPin.childPins.Add(childPin);
            childPin.parentPin = parentPin;
            return true;
        }
        return false;
    }

    public void MouseEnter()
    {
        transform.localScale = Vector3.one * interactionRadius * 2;
        material.color = interactCol;
    }

    public void MouseExit()
    {
        transform.localScale = Vector3.one * radius * 2;
        material.color = defaultCol;
    }
}
