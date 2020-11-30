// TaskControlBlock.cs
// This class represents the task control block representing each task. Holds static and dynamic meta information
// MB: Lukas Vozenilek

using System;

namespace TaskSim
{
    [Serializable]
    public class TaskControlBlock
    {
        //Static
        public int id;
        public int duration;
        public int period;
        public int release;
        
        //Runtime
        public int currentRemaining;
        public bool scheduled;
        public int lastScheduled;
    }
}