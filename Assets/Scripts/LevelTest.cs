using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelTest : MonoBehaviour
{
    public ChipEditor chipEditor;
    ChipInterfaceEditor inputEditor;
    ChipInterfaceEditor outputEditor;
    public bool testStart = false;
    public List<float> inputSpawnPos = new List<float>();
    public List<float> outputSpawnPos = new List<float>();
    public GameObject successBox;
    public GameObject implementationHolder;

    int inputCount;
    int outputCount;

    public List<string> testNums = new List<string>();
    public Simulation simulation;
    public float stepTime = 5;
    float lastTime;
    public TMP_Text testBar;

    void Start()
    {
        inputEditor = chipEditor.inputsEditor;
        outputEditor = chipEditor.outputsEditor;
        inputCount = inputSpawnPos.Count;
        outputCount = outputSpawnPos.Count;
        for(int i = 0; i < inputSpawnPos.Count; i++)
        {
            inputEditor.HandleSpawning(inputSpawnPos[i]);
        }
        for(int i = 0; i < outputSpawnPos.Count; i++)
        {
            outputEditor.HandleSpawning(outputSpawnPos[i]);
        }
        inputEditor.gameObject.SetActive(false);
        outputEditor.gameObject.SetActive(false);
        for(int i = 0; i < testNums.Count; i++)
        {
            testBar.text += testNums[i]+'\n';
        }
        implementationHolder.SetActive(true);
        successBox.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (testStart)
        {
            TestCircuit();
        }
    }

    void SetInput(int input, int inputCount)
    {
        inputEditor.signals[inputCount].currentState = input;
        inputEditor.signals[inputCount].SetDisplayState(input);
    }

    void TestCircuit()
    {
        testBar.text = "";
        List<bool> results = new List<bool>();
        for(int i = 0; i < testNums.Count; i++)
        {
            string a = testNums[i].Split(':')[0];
            string b = testNums[i].Split(':')[1];
            for(int j = 0; j < a.Length; j++)
            {
                SetInput(int.Parse(a[j].ToString()), j);
            }
            simulation.StepSimulation();
            for(int j = 0; j < b.Length; j++)
            {
                if (int.Parse(b[j].ToString()) == outputEditor.signals[j].currentState){
                    results.Add(true);
                    testBar.text += testNums[i] + " O" + '\n';
                    //Debug.Log(int.Parse(b[j].ToString()) + " : " + outputEditor.signals[j].currentState + "succsess");
                }
                else
                {
                    results.Add(false);
                    testBar.text += testNums[i] + " X" + '\n';
                    //Debug.Log("Failure");
                }
            }
        }
        testStart = false;
        if (results.Contains(false))
        {
            Debug.Log("Something went wrong");
        }
        else
        {
            implementationHolder.SetActive(false);
            successBox.SetActive(true);
        }
        for(int i = 0; i < inputCount; i++)
        {
            SetInput(0, i);
        }
    }

    public void BeginTest()
    {
        this.testStart = true;
    }
}