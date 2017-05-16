using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class sample : MonoBehaviour {


	public ProgressController controller;
	public Image image;
	public Text text;

	void Start () {
		mStateAction = State_Setup;
		controller = GetComponentInChildren<ProgressController>();
		controller.Initialize();
		image = GetComponentInChildren<Image>();
		text = GetComponentInChildren<Text>();
	}
	

	public int processCount = 10;
	public int processMax = 2;
	public float predicteEndTime = 1.0f;
	public float processEndTime = 2.0f;

	public enum UpdateType {
		Liner,
		Step
	}
	public UpdateType updateType = UpdateType.Step;

	Action mStateAction;

	void OnGUI()
	{
		if (mStateAction != null) {
			mStateAction();
		}
	}

	void State_Setup()
	{
		GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

		GUILayout.BeginHorizontal();
		GUILayout.Label("総プロセス数:" + processCount.ToString("D2"));
		processCount = (int)GUILayout.HorizontalSlider(processCount, 1, 15);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("同時処理プロセス数:" + processMax.ToString("D2"));
		processMax = (int)GUILayout.HorizontalSlider(processMax, 1, 3);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("１プロセスの予測終了時間:" + predicteEndTime.ToString("F1"));
		predicteEndTime = GUILayout.HorizontalSlider(predicteEndTime, 0.1f, 10);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("１プロセスの実終了時間:" + processEndTime.ToString("F1"));
		processEndTime = GUILayout.HorizontalSlider(processEndTime, 0.1f, 10);
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Start")) {
			mStateAction = State_Processing;

			//初期化
			controller.Initialize();
			//プロセスの登録
			for (int i = 0; i < processCount; i++) {
				if (updateType == UpdateType.Step) {
					//進捗状況を随時報告
					controller.EntryProcess(predicteEndTime, SampleStep);
				}
				else {
					//終了時に完了のみ報告
					controller.EntryProcess(predicteEndTime, SampleLiner);
				}
			}
			//進捗状況の受取
			controller.onValueChanged.AddListener(rate =>
			{
				image.fillAmount = rate;
				text.text = (rate * 100).ToString("F1") + "%";
			});
			//完了通知の受取
			controller.onComplete.AddListener(() =>
			{
				mStateAction = State_Completed;
			});
			//同時処理プロセス数の設定
			controller.processMax = processMax;
			//プロセスを登録順に実行開始
			controller.StartProcess();
		}

		GUILayout.EndArea();

	}

	//進捗状況を随時報告
	IEnumerator SampleLiner(ProgressController.Handle handle)
	{
		var startTime = ProgressController.Handle.GetTime();
		do {
			yield return null;
			var time = ProgressController.Handle.GetTime();
			var rate = (time - startTime) / processEndTime;
			if (1.0f < rate) {
				rate = 1.0f;
			}
			handle.rate = rate;
		} while (handle.rate < 1.0f);
	}

	//終了時に完了のみ報告
	IEnumerator SampleStep(ProgressController.Handle handle)
	{
		yield return new WaitForSeconds(processEndTime);
		handle.rate = 1.0f;
	}


	void State_Processing()
	{
		EditorGUILayout.LabelField("time:" + controller.time.ToString("F1") + " sec");
		EditorGUILayout.LabelField(string.Format("{0}/{1}", controller.completeCount, controller.processCount));
	}
	void State_Completed()
	{
		if (GUILayout.Button("Restart")) {
			controller.Initialize();
			mStateAction = State_Setup;
		}
		EditorGUILayout.LabelField("Result");
		EditorGUILayout.LabelField("total:" + controller.time.ToString("F1") + " sec");
		foreach (var handle in controller.handleList) {
			EditorGUILayout.LabelField(handle.name + ":" + handle.processTime.ToString("F1") + " sec");

		}
	}
}
