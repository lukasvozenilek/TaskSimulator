// TimeTick.cs
// This class represents each time tick. It holds references to the UI elements for each tick as well as updates colors and fills based on the scheduling results.
// MB: Lukas Vozenilek

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSim
{
    public class TimeTick : MonoBehaviour
    {
        public Image fill;
        public TMP_Text text;
        public Image deadlineImage;
        public Image tickImage;

        public void UpdateGraphics(bool scheduled, bool deadline, bool missedDeadline, bool released)
        {
            fill.enabled = scheduled;
            deadlineImage.enabled = deadline || released;
            if (missedDeadline)
            {
                tickImage.color = Color.red;
                deadlineImage.color = Color.red;
            } else if (deadline)
            {
                tickImage.color = Color.green;
                deadlineImage.color = Color.green;
            }
            else
            {
                tickImage.color = Color.white;
                deadlineImage.color = Color.white;
            }
        }
    }
}