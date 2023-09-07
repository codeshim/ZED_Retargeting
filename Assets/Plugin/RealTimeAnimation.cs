using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class RealTimeAnimation : MonoBehaviour {

	public enum FPS {Thirty, Sixty}

	public Actor _actor;
    public HelloRequester _helloRequester;
	public int Frame;
	

	public float AnimationTime {get; private set;}
	public float PostprocessingTime {get; private set;}
	public FPS Framerate = FPS.Thirty;

	protected abstract void Setup();
	protected abstract void Feed();
	protected abstract void Read();
	protected abstract void Postprocess();
    protected abstract void OnGUIDerived();
    protected abstract void OnRenderObjectDerived();

    private void Awake()
    {
		
	}
    void Start() {
		Setup();
	}

	public void update_pose(int index, Matrix4x4[][] _motion, Actor _actor)
	{
		//Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
		for (int j = 0; j < _actor.Bones.Length; j++)
			_actor.Bones[j].Transform.SetPositionAndRotation(_motion[index][j].GetPosition(), _motion[index][j].GetRotation());

	}
	public void update_pose(int index, Matrix4x4[][] _motion, Matrix4x4[] _root, Actor _actor)
	{
		//Debug.Log("Frame " + index + "/" + _motion.GetLength(0));
		for (int j = 0; j < _actor.Bones.Length; j++)
		{
			Matrix4x4 jointmat = _motion[index][j].GetRelativeTransformationFrom(_root[index]);
			_actor.Bones[j].Transform.SetPositionAndRotation(jointmat.GetPosition(), jointmat.GetRotation());
		}

	}
	void FixedUpdate() {
		//Utility.SetFPS(Mathf.RoundToInt(GetFramerate()));
		Time.fixedDeltaTime = (float)1 / 30;

		System.DateTime t1 = Utility.GetTimestamp();

        // send target information 
        Feed();
		// get modified pose
		Read();
		// post processing
		AnimationTime = (float)Utility.GetElapsedTime(t1);
		System.DateTime t2 = Utility.GetTimestamp();
		Postprocess();
		PostprocessingTime = (float)Utility.GetElapsedTime(t2);

    }

    void OnGUI()
    {
		OnGUIDerived();
		//if (_kinectManager != null && _kinectManager.IsInitialized())
		//{
		//    _kinectManager.onGUIKinectSetting();

		//}
	}
	
    void OnRenderObject()
    {
        if (Application.isPlaying)
        {
            OnRenderObjectDerived();
        }
    }

	private void OnApplicationQuit()
    {
		OnDestroy();
		
    }
    private void OnDestroy()
    {
		_helloRequester.Stop();
		//_kinectManager.destoryKinectSetting();
	}

    public float GetFramerate() {
		switch(Framerate) {
			case FPS.Thirty:
			return 30f;
			case FPS.Sixty:
			return 60f;
		}
		return 1f;
	}

	public StringBuilder WriteFloat(StringBuilder sb_, float x, bool first)
	{
		if (first)
		{
			sb_.Append(x);
		}
		else
		{
			sb_.Append(" ");
			sb_.Append(x);
		}
		return sb_;
	}
	public StringBuilder WriteString(StringBuilder sb_, string x, bool first)
	{
		if (first)
		{
			sb_.Append(x);
		}
		else
		{
			sb_.Append(" ");
			sb_.Append(x);
		}
		return sb_;
	}
	public StringBuilder WritePosition(StringBuilder sb_, Vector3 position, bool first)
	{
		sb_ = WriteFloat(sb_, position.x, first);
		sb_ = WriteFloat(sb_, position.y, false);
		sb_ = WriteFloat(sb_, position.z, false);

		return sb_;
	}
	public StringBuilder WriteQuat(StringBuilder sb_, Quaternion quat, bool first)
	{
		sb_ = WriteFloat(sb_, quat.x, first);
		sb_ = WriteFloat(sb_, quat.y, false);
		sb_ = WriteFloat(sb_, quat.z, false);
		sb_ = WriteFloat(sb_, quat.w, false);

		return sb_;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(RealTimeAnimation), true)]
	public class RealTimeAnimation_Editor : Editor {

		public RealTimeAnimation Target;

		void Awake() {
			Target = (RealTimeAnimation)target;
		}

		public override void OnInspectorGUI() {
			Undo.RecordObject(Target, Target.name);

			DrawDefaultInspector();

			EditorGUILayout.HelpBox("Animation: " + 1000f*Target.AnimationTime + "ms", MessageType.None);
			EditorGUILayout.HelpBox("Postprocessing: " + 1000f*Target.PostprocessingTime + "ms", MessageType.None);

			if(GUI.changed) {
				EditorUtility.SetDirty(Target);
			}
		}

	}
	#endif


}
