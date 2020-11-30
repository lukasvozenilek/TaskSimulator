// Main.cs
// Main MonoBehavior that controls all user input, canvases, simulation, and instantiation
// MB: Lukas Vozenilek

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TaskSim
{
    public class Main : MonoBehaviour
    {
        [Header("User Inputs")]
        public Slider tasksSlider;
        public TMP_Text tasksLabel;
        public Button runSim;
        public TMP_InputField simTime;
        public TMP_Dropdown algorithm;
        public TMP_Dropdown simSpeed;
        public Button randomizeTasks;
        public CanvasGroup inputGroup;
        
        [Header("Start overlay")]
        public Button startButton;
        public GameObject overlayCanvas;
        
        [Header("Task Settings")]
        public GameObject taskSettingObject;
        public Transform taskSettingsParent;
        public List<TaskSetting> allTaskSettings = new List<TaskSetting>();

        [Header("Task Graphs")] 
        public GameObject taskPrefab;
        public GameObject timeTickPrefab;
        public RectTransform taskGraphParent;
        public List<TaskGraph> taskGraphs = new List<TaskGraph>();

        [Header("Results")] 
        public GameObject resultsParent;
        public Button closeResults;
        public TMP_Text resultsText;
        
        [Header("Statistics")]
        public int usedTicks;
        public int deadlineMet;
        public int deadlineMissed;
        
        [Header("Arrays")]
        public List<TaskControlBlock> tasks = new List<TaskControlBlock>();
        public List<SimResult> simResults = new List<SimResult>();
        
        private void Start()
        {
            overlayCanvas.SetActive(true);
            resultsParent.SetActive(false);
            
            //Wait for start button to be pressed on start overlay
            startButton.onClick.AddListener(() =>
            {
                //Hide starting canvas
                overlayCanvas.SetActive(false);
                
                //Subscribe all UI event listeners
                tasksSlider.onValueChanged.AddListener(TaskAmountsChanged);
                TaskAmountsChanged(tasksSlider.value);
                runSim.onClick.AddListener(RunSimulation);
                randomizeTasks.onClick.AddListener(RandomizeTasks);
                closeResults.onClick.AddListener(OnCloseResults);
            });
        }

        /// <summary>
        /// Callback for close results button
        /// </summary>
        private void OnCloseResults()
        {
            resultsParent.SetActive(false);
        }
        
        /// <summary>
        /// Resets all statistics
        /// </summary>
        private void ResetStats()
        {
            usedTicks = 0;
            deadlineMet = 0;
            deadlineMissed = 0;
        }

        /// <summary>
        /// Callback when the task amount slider is changed
        /// </summary>
        /// <param name="val"></param>
        private void TaskAmountsChanged(float val)
        {
            tasksLabel.text = ((int) val).ToString();
            ResetAllTaskSettings((int)val);
            ResetAllTaskGraphs((int)val);
        }
    
        /// <summary>
        /// Clears all task setting list objects and creates new ones. Finally randomizes all values.
        /// </summary>
        /// <param name="amount">Number of task settings to be instantiated</param>
        private void ResetAllTaskSettings(int amount)
        {
            //Iterate through all task settings and destroy all
            if (allTaskSettings.Count != 0)
            {
                foreach (TaskSetting thisTS in allTaskSettings)
                {
                    Destroy(thisTS.gameObject);
                }

                allTaskSettings.Clear();
            }
            //Instantiate new ones
            for (int i = 0; i < amount; i++)
            {
                TaskSetting TS = Instantiate(taskSettingObject, taskSettingsParent).GetComponent<TaskSetting>();
                TS.taskName.text = (i+1).ToString();
                TS.taskID = i;
                allTaskSettings.Add(TS);
            }
            //Randomize all the current tasks
            RandomizeTasks();
        }
        
        /// <summary>
        /// Destroys all existing task graphs and instantiates the new required amount
        /// </summary>
        /// <param name="amount">Number of graphs to be instantiated</param>
        private void ResetAllTaskGraphs(int amount)
        {
            if (taskGraphs.Count != 0)
            {
                foreach (TaskGraph TG in taskGraphs)
                {
                    Destroy(TG.gameObject);
                }

                taskGraphs.Clear();
            }

            for (int i = 0; i < amount; i++)
            {
                TaskGraph TG = Instantiate(taskPrefab, taskGraphParent).GetComponent<TaskGraph>();
                TG.graphTitle.text = "Task " + (i+1).ToString();
                taskGraphs.Add(TG);
            }
        }
        
        
        /// <summary>
        /// Converts a list of settings (inputted from user) into task control blocks
        /// </summary>
        /// <param name="settings">List of task settings to be converted</param>
        private void GenerateTasks(List<TaskSetting> settings)
        {
            tasks.Clear();
            foreach (TaskSetting setting in settings)
            {
                int dur = int.Parse(setting.duration.text);
                int per = int.Parse(setting.period.text);
                int rel = int.Parse(setting.release.text);
                tasks.Add(new TaskControlBlock
                {
                    id = setting.taskID,
                    duration = dur,
                    period =  per,
                    release = rel,
                    currentRemaining = dur,
                });
            }
        }

        /// <summary>
        /// Generates random values for task duration, periods, and release. Splits into 3 random sections giving a better distribution of task types
        /// Is also a callback from the randomize tasks button
        /// </summary>
        private void RandomizeTasks()
        {
            foreach (TaskSetting TS in allTaskSettings)
            {
                float ran = UnityEngine.Random.Range(0f, 1.0f);
                if (ran < 0.3333f)
                {
                    TS.duration.text = UnityEngine.Random.Range(2, 4).ToString();
                    TS.period.text = UnityEngine.Random.Range(15, 25).ToString();
                }
                else if (ran < 0.666f)
                {
                    TS.duration.text = UnityEngine.Random.Range(1, 3).ToString();
                    TS.period.text = UnityEngine.Random.Range(9, 15).ToString();
                }
                else
                {
                    TS.duration.text = UnityEngine.Random.Range(1, 1).ToString();
                    TS.period.text = UnityEngine.Random.Range(5, 10).ToString();
                }
                
                //Randomize release time seperately as it should be independent of scheduling effort
                ran = UnityEngine.Random.Range(0f, 1.0f);
                TS.release.text = ran < 0.5f ? UnityEngine.Random.Range(0, 0).ToString() : UnityEngine.Random.Range(0, 10).ToString();
            }
        }
        
        /// <summary>
        /// Callback from Run button. Runs a single simulation or find best.
        /// </summary>
        private void RunSimulation()
        {
            simResults.Clear();
            OnCloseResults();
            if (algorithm.value == algorithm.options.Count - 1)
            {
                StartCoroutine(FindBestAlgorithm());
            }
            else
            {
                StartCoroutine(SimulationLoop(algorithm.value));
            }
        }

        /// <summary>
        /// Calculates the next deadline of a periodic task given period, releasetime, and currenttick.
        /// </summary>
        /// <param name="currentTick">Current simulation tick</param>
        /// <param name="period">Period of the task</param>
        /// <param name="releasetime">Release time of the task</param>
        /// <returns></returns>
        private int GetNextDeadline(int currentTick, int period, int releasetime)
        {
            int deadline = period + releasetime;
            while (currentTick > deadline)
            {
                deadline += period;
            }
            return deadline;
        }
        
        
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerator FindBestAlgorithm()
        {
            //Iterate through all algorithm options except last
            for (int algoID = 0; algoID < algorithm.options.Count-1; algoID++)
            {
                //Sets dropdown for visual indication of current running algorithm
                algorithm.SetValueWithoutNotify(algoID);
                yield return StartCoroutine(SimulationLoop(algoID));
                simResults.Add(new SimResult
                {
                    algoID = algoID, 
                    deadlinePercent = (float)deadlineMet/(deadlineMet+deadlineMissed),
                    utilization = (float)usedTicks / int.Parse(simTime.text)
                });
            }
            //Finally set back last option
            algorithm.SetValueWithoutNotify(algorithm.options.Count-1);
            
            //Sort results
            simResults.Sort((a1, a2) => a2.deadlinePercent.CompareTo(a1.deadlinePercent));
            ShowResults();
        }
        
        
        /// <summary>
        /// Converts algo ID into it's name
        /// </summary>
        /// <param name="id">Algo ID</param>
        /// <returns>Algo name</returns>
        private static string GetAlgoName(int id)
        {
            switch (id)
            {
                case 0: //FCFS
                    return "FCFS";
                case 1: //RR
                    return "RR";
                case 2: //RM
                    return "RM";
                case 3: //EDF
                    return "EDF";
                case 4: //LLF
                    return"LLF";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// This IEnumerator is the simulation loop for all scheduling algorithms.
        /// </summary>
        /// <param name="algorithmID">Algorithm to simulate</param>
        /// <returns></returns>
        private IEnumerator SimulationLoop(int algorithmID)
        {
            inputGroup.interactable = false;
            
            ResetStats();
            ResetAllTaskGraphs((int)tasksSlider.value);
            GenerateTasks(allTaskSettings);
            
            //Get each tick size by dividing total width by the simulation time
            int duration = int.Parse(simTime.text);
            float tickSizes = taskGraphParent.rect.width / duration;
            
            //Check if last algorithm selected (Find best algorithm)
            for (int tick = 1; tick < duration + 1; tick++)
            {
                //Debug.Log("Tick: " + tick);
                List<TaskControlBlock> sortedList = new List<TaskControlBlock>(tasks);
                switch (algorithmID)
                {
                    case 0: //FCFS
                        //Sort tasks based on most recently scheduled
                        sortedList.Sort((t1, t2) => t2.lastScheduled.CompareTo(t1.lastScheduled));
                        break;
                    case 1: //RR
                        //Sort tasks based on last scheduled time
                        sortedList.Sort((t1, t2) => t1.lastScheduled.CompareTo(t2.lastScheduled));
                        break;
                    case 2: //RM
                        //Sort tasks by inverse period
                        sortedList.Sort((t1, t2) => t1.period.CompareTo(t2.period));
                        break;
                    case 3: //EDF
                        //Sort tasks by their next closest deadline
                        sortedList.Sort((t1, t2) =>
                        {
                            int edf1 = GetNextDeadline(tick, t1.period, t1.release);
                            int edf2 = GetNextDeadline(tick, t2.period, t2.release);
                            if (edf1 < edf2)
                            {
                                return -1;
                            }
                            if (edf1 > edf2)
                            {
                                return 1;
                            }
                            //If same, use most recently scheduled task.
                            return t2.lastScheduled.CompareTo(t1.lastScheduled);
                        });
                        break;
                    case 4: //LLF
                        //Sort tasks by their next deadline, minus remaining time
                        sortedList.Sort((t1, t2) =>
                        {
                            int llf1 = GetNextDeadline(tick, t1.period, t1.release) - t1.currentRemaining - tick;
                            int llf2 = GetNextDeadline(tick, t2.period, t2.release) - t2.currentRemaining - tick;
                            if (llf1 < llf2)
                            {
                                return -1;
                            }
                            if (llf1 > llf2)
                            {
                                return 1;
                            }
                            //If equal, use most recently scheduled task
                            return t2.lastScheduled.CompareTo(t1.lastScheduled);
                        });
                        break;
                }

                //Filter priority list by tasks with remaining time and have been released then iterate over to select the task to be scheduled.
                foreach (TaskControlBlock TCB in sortedList.Where(TCB => tasks[TCB.id].currentRemaining > 0).Where(TCB=>tick > tasks[TCB.id].release))
                {
                    tasks[TCB.id].scheduled = true;
                    usedTicks++;
                    break;
                }

                for (int graphID=0;graphID < taskGraphs.Count;graphID++)
                {
                    TaskGraph TG = taskGraphs[graphID];
                    TaskControlBlock TCB = tasks[graphID];
                    TimeTick TT = Instantiate(timeTickPrefab, TG.ticksParent).GetComponent<TimeTick>();
                    RectTransform TT_Rect = TT.GetComponent<RectTransform>();
                    
                    TT_Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tickSizes);
                    TG.ticks.Add(TT);
                    TT.text.text = tick.ToString();
                    
                    //Current tick is a deadline if period is a factor of tick and the task has already been released
                    bool deadline = (tick - TCB.release) % TCB.period == 0 && tick > TCB.release;
                    bool missed = false;
                    bool release = tick == TCB.release;
                    
                    //Check here for zero release during tick 1
                    if (tick == 1 && TCB.release == 0)
                    {
                        TG.zeroRelease.enabled = true;
                    }

                    if (TCB.scheduled)
                    {
                        TCB.currentRemaining--;
                        TCB.lastScheduled = tick;
                        //Debug.Log("Scheduling task " + TCB.id);
                        //Debug.Log("Remaining time now " + TCB.currentRemaining);
                    }

                    //If currently deadline tick check for missed deadline and refresh remaining
                    if (deadline)
                    {
                        if (TCB.currentRemaining > 0)
                        {
                            //Debug.LogError("MISSED DEADLINE AT TICK " + tick + " FOR TASK " + TCB.id);
                            missed = true;
                            deadlineMissed++;
                        }
                        else
                        {
                            deadlineMet++;
                        }
                        TCB.currentRemaining = TCB.duration;
                    }

                    //Sets visual ques for scheduled and deadline
                    TT.UpdateGraphics(TCB.scheduled, deadline, missed, release);
                    TCB.scheduled = false;
                }
                yield return new WaitForSecondsRealtime(simSpeed.value == 0 ? 0 : 0.25f);
            }
            inputGroup.interactable = true;
            ShowResults();
        }

        /// <summary>
        /// Displays simulation results in the top right popup windoww. For analysis mode it displays the reslults of all algorithms.
        /// </summary>
        private void ShowResults()
        {
            resultsParent.SetActive(true);
            string results = "";
            
            //Switches between showing general results and algorith comparison results depending of all the algorithms have been run
            if (simResults.Count < algorithm.options.Count-1)
            {
                results += "Deadlines Met: " + System.Math.Round(100 * (float) deadlineMet / (deadlineMet+deadlineMissed), 1) + "%\n";
                results += "CPU Usage: " + System.Math.Round(100 * (float) usedTicks / int.Parse(simTime.text), 1) + "%\n";
            }
            else
            {
                results += "Algorithm Comparison:\n";
                foreach (SimResult result in simResults)
                {
                    results += GetAlgoName(result.algoID) + ": " + System.Math.Round(100 * result.deadlinePercent, 1) + "% met, " + System.Math.Round(100 * result.utilization, 1) + "% usage\n";
                }
            }
            resultsText.text = results;
            //Force layout rebuild so that the layout groups update immediately.
            LayoutRebuilder.ForceRebuildLayoutImmediate(resultsParent.GetComponent<RectTransform>());
        }
        
        
    }

}