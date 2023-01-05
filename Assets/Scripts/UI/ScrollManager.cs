using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollManager : MonoBehaviour
{
    public Scrollbar scrollbar;
    public List<Button> buttons;
    List<float> buttonYPos = new List<float>();
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < buttons.Count; i++)
        {
            buttonYPos.Add(buttons[i].transform.position.y);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < buttons.Count; i++)
        {
            Button btn = buttons[i];
            btn.transform.position = new Vector3(btn.transform.position.x, buttonYPos[i] + (scrollbar.value * 10), btn.transform.position.z);
        }
    }
}
