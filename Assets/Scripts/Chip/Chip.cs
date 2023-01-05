using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chip : MonoBehaviour
{
    public string chipName = "Untitled";
    public Pin[] inputPins;
    public Pin[] outputPins;

    int numInputSignalsReceived;
    int lastSimulatedFrame;
    int lastSimulationInitFrame;

    [HideInInspector]
    public BoxCollider2D bounds;

    protected virtual void Awake()
    {
        bounds = GetComponent<BoxCollider2D>();
    }

    protected virtual void Start()
    {
        SetPinIndices();
    }

    public void InitSimulationFrame()
    {
        if(lastSimulationInitFrame != Simulation.simulationFrame)
        {
            lastSimulationInitFrame = Simulation.simulationFrame;
            ProcessCycleAndUnconnectedInputs();
        }
    }

    public virtual void ReceiveInputSignal(Pin pin)
    {
        if(lastSimulatedFrame != Simulation.simulationFrame)
        {
            lastSimulatedFrame = Simulation.simulationFrame;
            numInputSignalsReceived = 0;
            InitSimulationFrame();
        }

        numInputSignalsReceived++;

        if(numInputSignalsReceived == inputPins.Length)
        {
            ProcessOutput();
        }
    }

    void ProcessCycleAndUnconnectedInputs()
    {
        for(int i = 0; i < inputPins.Length; i++)
        {
            if (inputPins[i].cyclic)
            {
                ReceiveInputSignal(inputPins[i]);
            }
            else if (!inputPins[i].parentPin)
            {
                inputPins[i].ReceiveSignal(0);
            }
        }
    }

    protected virtual void ProcessOutput()
    {

    }

    void SetPinIndices()
    {
        for(int i = 0; i < inputPins.Length; i++)
        {
            inputPins[i].index = i;
        }
        for(int i = 0; i < outputPins.Length; i++)
        {
            outputPins[i].index = i;
        }
    }

    public Vector2 BoundsSize
    {
        get
        {
            return bounds.size;
        }
    }
}
