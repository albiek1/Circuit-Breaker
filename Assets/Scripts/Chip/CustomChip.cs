using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomChip : Chip
{
    public InputSignal[] inputSignals;
    public OutputSignal[] outputSignals;

    public override void ReceiveInputSignal(Pin pin)
    {
        base.ReceiveInputSignal(pin);
    }

    protected override void ProcessOutput()
    {
        for(int i = 0; i < inputPins.Length; i++)
        {
            inputSignals[i].SendSignal(inputPins[i].State);
        }

        for(int i = 0; i < outputPins.Length; i++)
        {
            int outputState = outputSignals[i].inputPins[0].State;
            outputPins[i].ReceiveSignal(outputState);
        }
    }
}
