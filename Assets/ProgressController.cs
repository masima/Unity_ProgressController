using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ProgressController : MonoBehaviour {

	[Serializable]
	public class UpdateEvent: UnityEvent<float> { }
	public UpdateEvent onValueChanged = new UpdateEvent();
	public UnityEvent onComplete = new UnityEvent();

	bool busy = false;
	public float rate
	{
		get; protected set;
	}

	List<Handle> mHandleList = new List<Handle>();
	public List<Handle> handleList { get { return mHandleList; } }
	int mNextProcess = 0;
	int nextProcess { get { return mNextProcess; } }
	public int processCount { get { return mHandleList.Count; } }
	public int processMax = 1;

	int mBusyCount = 0;
	int mCompleteCount = 0;
	public int completeCount { get { return mCompleteCount; } }
	public bool completed
	{
		get
		{
			return mCompleteCount == mHandleList.Count;
		}
	}


	float startTime;
	float endTime;
	public float time;


	void Start()
	{
		Initialize();
	}

	public void Initialize()
	{
		busy = false;
		rate = 0.0f;
		mHandleList.Clear();
		mNextProcess = 0;
		mBusyCount = 0;
		mCompleteCount = 0;
	}

	public delegate IEnumerator ProcessDelegate(Handle handle);

	public Handle EntryProcess(float predictedTime, ProcessDelegate processDelegate)
	{
		var handle = new Handle();
		handle.name = mHandleList.Count().ToString();
		handle.controller = this;
		handle.predictedTime = predictedTime;
		handle.process = processDelegate;
		mHandleList.Add(handle);
		return handle;
	}

	public void StartProcess()
	{
		rate = 0.0f;
		time = 0;
		startTime = GetTime();
		busy = true;
		while (mBusyCount < processMax) {
			StartNextProcess();
			if (completed) {
				Complete();
				break;
			}
		}
	}
	void StartNextProcess()
	{
		mBusyCount++;
		var startIndex = mNextProcess++;
		mHandleList[startIndex].Start();

	}
	internal void CompleteProcess(Handle handle)
	{
		mBusyCount--;
		mCompleteCount++;
		if (completed) {
			Complete();
			return;
		}
		if (mNextProcess < handleList.Count) {
			StartNextProcess();
		}
	}
	void Complete()
	{
		endTime = GetTime();
		onComplete.Invoke();
	}

	void Update()
	{
		if (busy) {
			time = GetTime() - startTime;
			UpdateRate();
			if (completed) {
				busy = false;
			}
		}
	}

	void UpdateRate()
	{
		var newRate = CalculateTotalRate();
		if (rate < newRate) {
			rate = newRate;
			if (onValueChanged != null) {
				onValueChanged.Invoke(rate);
			}
		}
	}
	float CalculateTotalRate()
	{
		float sumRate = 0;
		float sumAll = 0;
		foreach (var handle in mHandleList) {
			var rate = customizeRateDelegate(handle);
			if (1.0f < rate) {
				rate = 1.0f;
			}
			sumRate += rate * handle.predictedTime;
			sumAll += handle.predictedTime;
		}
		return sumRate / sumAll;
	}
	public delegate float CustomizeRateDelegate(Handle handle);
	public CustomizeRateDelegate customizeRateDelegate = CustomizeRate;
	static float CustomizeRate(Handle handle)
	{
		var predictedRate = handle.GetPredictedRate();

		var limitter = 0.98f;
		if (limitter < predictedRate) {
			predictedRate = limitter;
		}
		if (predictedRate < handle.rate) {
			predictedRate = handle.rate;
		}
		return predictedRate;
	}


	public static float GetTime()
	{
		return Time.realtimeSinceStartup;
	}


	public class Handle {
		public ProgressController controller { get; internal set; }

		public enum State {
			Idle,
			Started,
			Compeleted
		}
		State mState = State.Idle;
		public State state
		{
			get { return mState; }
		}
		float GetTime()
		{
			return controller.time;
		}

		public string name;
		public bool started { get { return State.Started <= state; } }
		public bool completed { get { return state == State.Compeleted; } }
		public float startTime { get; protected set; }
		public float endTime { get; protected set; }
		public float processTime
		{
			get
			{
				if (state == State.Started) {
					return GetTime() - startTime;
				}
				if (state == State.Compeleted) {
					return endTime - startTime;
				}
				return 0.0f;
			}
		}
		public float predictedTime = 1.0f;
		public float rate;
		internal ProcessDelegate process;

		internal void Start()
		{
			mState = State.Started;
			startTime = GetTime();
			controller.StartCoroutine(ProcessCoroutine());
		}
		IEnumerator  ProcessCoroutine()
		{
			yield return process(this);

			endTime = GetTime();
			rate = 1.0f;
			mState = State.Compeleted;
			controller.CompleteProcess(this);
		}

		public float GetPredictedRate()
		{
			return GetPredictedRate(predictedTime);
		}
		public float GetPredictedRate(float predictedTime)
		{
			if (!started) {
				return 0;
			}
			if (completed) {
				return 1.0f;
			}
			var time = GetTime();
			float predictedRate = (time - startTime) / predictedTime;
			if (1.0f < predictedRate) {
				return 1.0f;
			}
			return predictedRate;

		}
	}
}
