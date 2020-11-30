// TaskSetting.cs
// This class represents each task setup element. Holds references to the instantiated UI elements
// MB: Lukas Vozenilek

using TMPro;
using UnityEngine;

namespace TaskSim
{
    public class TaskSetting : MonoBehaviour
    {
        public TMP_InputField duration;
        public TMP_InputField period;
        public TMP_InputField release;
        public TMP_Text taskName;
        public int taskID;
    }
}