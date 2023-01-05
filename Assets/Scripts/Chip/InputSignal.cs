using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputSignal : ChipSignal
{
    protected override void Start()
    {
        base.Start();
        SetCol();
    }

    public void ToggleActive()
    {
        currentState = 1 - currentState;
        SetCol();
    }

    public void SendSignal(int signal)
    {
        currentState = signal;
        outputPins[0].ReceiveSignal(currentState);
        SetCol();
    }

    public void SendSignal()
    {
        outputPins[0].ReceiveSignal(currentState);
    }

    void SetCol()
    {
        SetDisplayState(currentState);
    }

    public override void UpdateSignalName(string newName)
    {
        base.UpdateSignalName(newName);
        outputPins[0].pinName = newName;
    }

    private void OnMouseDown()
    {
        ToggleActive();
    }
}
