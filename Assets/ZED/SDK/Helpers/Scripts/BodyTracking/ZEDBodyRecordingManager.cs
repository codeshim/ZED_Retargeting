using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public enum BoneCount
{
    Default = 22,
}

public enum RecordingState
{
    NONE,
    RECORDING,
    TESTPLAYING,
    PLAYING,
}

public class ZEDBodyRecordingManager : RealTimeAnimation
{
    public ZEDManager zedManager;
    public ZEDBodyTrackingManager zedTrackingManager;

    private RecordingState recordingState = RecordingState.NONE;
    public RecordingState RecordingState
    {
        get => recordingState;
        set
        {
            // if state changed
            if (recordingState != value)
            {
                // previouse state
                switch (recordingState)
                {
                    case RecordingState.NONE:
                        break;
                    case RecordingState.RECORDING:
                        Debug.Log("End of recording.");
                        break;
                    case RecordingState.TESTPLAYING:
                        Debug.Log("Test-playing recorded animation data is finished.");
                        break;
                    case RecordingState.PLAYING:
                        Debug.Log("Playing animation data is finished.");
                        break;
                }

                recordingState = value;

                // current changed state
                switch (recordingState)
                {
                    case RecordingState.NONE:
                        break;
                    case RecordingState.RECORDING:
                        Debug.Log("Start recording!");
                        break;
                    case RecordingState.TESTPLAYING:
                        Debug.Log("Start test-playing recorded animation data");
                        break;
                    case RecordingState.PLAYING:
                        Debug.Log("Start playing loaded animation data");
                        break;
                }
            }
        }
    }

    public static Actor sourceActor = null;
    public Actor targetActor = null;
    //public int curBoneCount = (int)BoneCount.Default;

    private List<List<float>> recordedList = new List<List<float>>();
    private List<List<float>> loadedList = new List<List<float>>();

    public string dataPath = "";

    private int frameIdx = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (!zedManager)
        {
            zedManager = FindObjectOfType<ZEDManager>();
        }

        //if (zedManager)
        //{
        //    zedManager.OnBodyTracking += GetDetectedBody;
        //}
        
        if (!zedTrackingManager)
        {
            zedTrackingManager = FindObjectOfType<ZEDBodyTrackingManager>();
            if (zedTrackingManager.maximumNumberOfDetections != 1)
            {
                zedTrackingManager.maximumNumberOfDetections = 1;
            }
        }

        Assert.IsNotNull(targetActor, "Assign target avatar");
    }

    private void Recording(Actor actor)
    {
        List<float> jointlist = new List<float>();

        // Add global position of pelvis
        jointlist.Add(actor.Bones[0].Transform.position.x);
        jointlist.Add(actor.Bones[0].Transform.position.y);
        jointlist.Add(actor.Bones[0].Transform.position.z);

        // Add local quarternion rotation of all joints
        for (int i = 0; i < actor.Bones.Length; i++)
        {
            jointlist.Add(actor.Bones[i].Transform.rotation.x);
            jointlist.Add(actor.Bones[i].Transform.rotation.y);
            jointlist.Add(actor.Bones[i].Transform.rotation.z);
            jointlist.Add(actor.Bones[i].Transform.rotation.w);
        }

        //Debug.Log("jointlist : " + jointlist.Count);

        if (jointlist.Count == 3 + (int)BoneCount.Default * 4)
        {
            // Add to framelist
            recordedList.Add(jointlist);
        }
        else
        {
            Debug.LogError("The number of bones is not correct. it should be " + BoneCount.Default);
            RecordingState = RecordingState.NONE;
        }
    }

    private void SavingRecordedData()
    {
        string writeFilepath = EditorUtility.SaveFilePanel("Overwrite with txt", "", "Test" + ".txt", "txt");
        Debug.Log("Filepath : " + writeFilepath);
        if (writeFilepath.Length != 0)
        {
            // Write
            string[,] output = new string[recordedList.Count, recordedList[0].Count];

            for (int i = 0; i < recordedList.Count; i++)
            {
                for (int j = 0; j < recordedList[0].Count; j++)
                {
                    output[i, j] = recordedList[i][j].ToString();
                }
            }
            int length = output.GetLength(0);
            int clength = output.GetLength(1);
            string delimiter = " ";
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < length; index++)
            {
                string line = output[index, 0];
                for (int cindex = 1; cindex < clength; cindex++)
                {
                    line = line + delimiter + output[index, cindex];
                }
                stringBuilder.AppendLine(line);
            }
            StreamWriter outStream = File.CreateText(writeFilepath);
            outStream.Write(stringBuilder);
            outStream.Close();

            recordedList.Clear();
        }
    }

    private void Loading()
    {
        loadedList.Clear();
        dataPath = EditorUtility.OpenFilePanel("Overwrite with txt", "", "txt");
        if (dataPath.Length != 0)
        {
            //List<float> jointlist = new List<float>();

            string fileContnet = File.ReadAllText(dataPath);
            //Debug.Log(fileContnet);
            string[] rows = fileContnet.Split("\n");        
            foreach (string row in rows)
            {
                List<float> jointlist = new List<float>();
                string[] componants = row.Split(" ");
                //Debug.Log(componants[0]);
                foreach (string componant in componants)
                {
                    float parse = 0f;
                    if (float.TryParse(componant, out parse))
                    {
                        jointlist.Add(parse);
                    }                   
                }
                Debug.Log(jointlist.Count);
                if (jointlist.Count == 3 + (int)BoneCount.Default * 4)
                {
                    loadedList.Add(jointlist);
                }
            }            
        }
        Debug.Log("loadedList : " + loadedList.Count + ", " + loadedList[0].Count);
    }

    private void Playing(List<List<float>> frameList)
    {
        // Update global position of pelvis of target(x, y, z)
        Vector3 tagetPos = new Vector3(frameList[frameIdx][0], frameList[frameIdx][1], frameList[frameIdx][2]);
        targetActor.Bones[0].Transform.position = tagetPos;

        // Update local quarternion rotation of total joints of target(x, y, z)
        for (int i = 3; i < frameList[frameIdx].Count; i += 4)
        {
            Quaternion targetRot = new Quaternion(frameList[frameIdx][i],
                frameList[frameIdx][i + 1], frameList[frameIdx][i + 2], frameList[frameIdx][i + 3]);
            Debug.Log((int)i / 4);
            targetActor.Bones[i / 4].Transform.rotation = targetRot;
        }

        frameIdx++;
        if (frameIdx >= frameList.Count)
        {
            frameIdx = 0;
            RecordingState = RecordingState.NONE;
        }
    }

    protected override void Setup()
    {
        // connection initialization
        _helloRequester = new HelloRequester();
        _helloRequester.Start(); //Thread ½ÇÇà
    }
    protected override void Feed()
    {
        // send(to python) -> save
        //Debug.Log("Send animation data to pytorch!(deltaTime : " + Time.deltaTime + ") ; FixedUpdate()");

        // Recording
        if (RecordingState == RecordingState.NONE)
        {
            //if (jointFrameList.Count > 0) jointFrameList.Clear();
        }
        
        if (RecordingState == RecordingState.RECORDING)
        {
            // if ZED body tracking data finished updating actor's transform
            if (sourceActor != null)
            {
                // recording
                Recording(sourceActor);
                Debug.Log(recordedList.Count + "(joint " + recordedList[0].Count + ")" + " Frame Added");

                sourceActor = null;
            }
            else
            {
                Debug.LogError("ZED body tracking data failed to update actor's transform. Force to quit recording");
                RecordingState = RecordingState.NONE;
            }
        }
    }
    protected override void Read()
    {
        // read(from python) -> saved read
        if (RecordingState == RecordingState.TESTPLAYING)
        {
            if (recordedList.Count > 0)
            {
                Playing(recordedList);
            }
            else
            {
                Debug.LogError("Recorded data does not exist. Force to quit playing");
                RecordingState = RecordingState.NONE;
            }
        }

        if (RecordingState == RecordingState.PLAYING)
        {
            //if ()
            //string fileContnet = File.ReadAllText(dataPath);
            //Debug.Log(fileContnet);
            if (loadedList.Count > 0)
            {
                Playing(loadedList);
            }
            else
            {
                Debug.LogError("Uploaded data does not exist. Force to quit playing");
                RecordingState = RecordingState.NONE;
            }
        }
    }
    protected override void OnRenderObjectDerived()
    {
        //UltiDraw.Begin();
        //UltiDraw.DrawSphere(new Vector3(0, 0, 0), Quaternion.identity, 2f, UltiDraw.Red);
        //UltiDraw.End();
    }
    protected override void Postprocess()
    {

    }
    protected override void OnGUIDerived()
    {

    }

    [CustomEditor(typeof(ZEDBodyRecordingManager), true)]
    public class ZEDBodyRecordingManager_Editor : Editor
    {
        public ZEDBodyRecordingManager Target;

        public void Awake()
        {
            Target = (ZEDBodyRecordingManager)target;
        }

        public override void OnInspectorGUI()
        {
            inspector();
        }

        void inspector()
        {
            Utility.ResetGUIColor();
            Utility.SetGUIColor(UltiDraw.LightGrey);

            // Assigning Target Avatar
            EditorGUILayout.BeginHorizontal();
            Target.targetActor = (Actor)EditorGUILayout.ObjectField("Target Avatar", Target.targetActor, typeof(Actor), true);
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying) return;

            // Recording Function
            EditorGUILayout.BeginHorizontal();
            if (Target.RecordingState == RecordingState.NONE)
            {
                if (Utility.GUIButton("Start Recording!", UltiDraw.DarkGrey, UltiDraw.White))
                {
                    Target.recordedList.Clear();
                    Target.RecordingState = RecordingState.RECORDING;
                }
            }
            if (Target.RecordingState == RecordingState.RECORDING)
            {
                if (Utility.GUIButton("Stop Recording!", UltiDraw.DarkRed, UltiDraw.Red))
                {
                    Target.RecordingState = RecordingState.NONE;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            // if recording is finished and recorded data exists
            if (Target.RecordingState == RecordingState.NONE && Target.recordedList.Count > 0)
            {
                // Test recorded data
                if (Utility.GUIButton("Test Recorded Data", UltiDraw.DarkGrey, UltiDraw.White))
                {
                    //Target.CopyJointFrameList();
                    Target.frameIdx = 0;
                    Target.RecordingState = RecordingState.TESTPLAYING;
                }
                // Save data
                if (Utility.GUIButton("Save Recorded Data", UltiDraw.DarkBlue, UltiDraw.Blue))
                {
                    Target.SavingRecordedData();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Upload animation data
            EditorGUILayout.BeginHorizontal();
            if (Application.isPlaying)
            {
                Target.dataPath = EditorGUILayout.TextField("Data Path", Target.dataPath);
                if (Utility.GUIButton("Upload Data", UltiDraw.DarkGrey, UltiDraw.White))
                {
                    Target.Loading();
                }
            }
            else
            {
                Target.dataPath = "";
            }
            EditorGUILayout.EndHorizontal();

            // Play loaded animation data
            EditorGUILayout.BeginHorizontal();
            if (Target.loadedList.Count > 0)
            {
                if (Utility.GUIButton("Play loaded Data", UltiDraw.DarkGrey, UltiDraw.White))
                {
                    Target.frameIdx = 0;
                    Target.RecordingState = RecordingState.PLAYING;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

