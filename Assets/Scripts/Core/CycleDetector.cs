using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CycleDetector
{
    static bool currentChipHasCycle;

    public static List<Chip> MarkAllCycles(ChipEditor chipEditor)
    {
        var chipsWithCycles = new List<Chip>();

        HashSet<Chip> examinedChips = new HashSet<Chip>();
        Chip[] chips = chipEditor.chipInteraction.allChips.ToArray();

        for(int i = 0; i < chips.Length; i++)
        {
            for(int j = 0; j < chips[i].inputPins.Length; j++)
            {
                chips[i].inputPins[j].cyclic = false;
            }
        }
        
        for(int i = 0; i < chips.Length; i++)
        {
            examinedChips.Clear();
            currentChipHasCycle = false;
            MarkCycles(chips[i], chips[i], examinedChips);
            if (currentChipHasCycle)
            {
                chipsWithCycles.Add(chips[i]);
            }
        }
        return chipsWithCycles;
    }

    static void MarkCycles (Chip originalChip, Chip currentChip, HashSet<Chip> examinedChips)
    {
        if(!examinedChips.Contains(currentChip)){
            examinedChips.Add(currentChip);
        }
        else
        {
            return;
        }

        foreach(var outputPin in currentChip.outputPins)
        {
            foreach(var childPin in outputPin.childPins)
            {
                var childChip = childPin.chip;
                if(childChip != null)
                {
                    if(childChip == originalChip)
                    {
                        currentChipHasCycle = true;
                        childPin.cyclic = true;
                    }
                    else if(!childPin.cyclic)
                    {
                        MarkCycles(originalChip, childChip, examinedChips);
                    }
                }
            }
        }
    }
}
