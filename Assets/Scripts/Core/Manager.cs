using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public event System.Action<Chip> customChipCreated;

    public ChipEditor chipEditorPrefab;
    public ChipPackage chipPackagePrefab;
    public Wire wirePrefab;
    public Chip[] builtinChips;

    ChipEditor activeChipEditor;
    int currentChipCreationIndex;
    static Manager instance;

    private void Awake()
    {
        instance = this;
        activeChipEditor = FindObjectOfType<ChipEditor>();
        //FindObjectOfType<CreateMenu>().onChipCreatePressed += SaveAndPackageChip;
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
