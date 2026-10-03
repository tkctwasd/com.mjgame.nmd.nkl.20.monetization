using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelResume : UIPanel
{
    // AOA resume da dong (hoac khong show duoc) -> tat man cho
    public void Complete()
    {
        if (IsShown) Hide();
    }
}
