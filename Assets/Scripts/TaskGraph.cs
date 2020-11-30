// TaskGraph.cs
// This class is the highest level of each instantiated visual graph. It holds references to UI elements and the ticks
// MB: Lukas Vozenilek

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSim
{
    public class TaskGraph : MonoBehaviour
    {
        public List<TimeTick> ticks;
        public Transform ticksParent;
        public TMP_Text graphTitle;
        public Image zeroRelease;
    }
}