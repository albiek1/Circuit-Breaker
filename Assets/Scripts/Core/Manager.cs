using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public ChipEditor chipEditorPrefab;
    public Wire wirePrefab;
    public Chip[] builtinChips;

    ChipEditor activeChipEditor;
    int currentChipCreationIndex;
    static Manager instance;

    private void Awake()
    {
        instance = this;
        activeChipEditor = FindObjectOfType<ChipEditor>();
    }

    private void Start()
    {
        
    }

    public static ChipEditor ActiveChipEditor
    {
        get
        {
            return instance.activeChipEditor;
        }
    }

    public void SpawnChip(Chip chip)
    {
        activeChipEditor.chipInteraction.SpawnChip(chip);
    }
}
