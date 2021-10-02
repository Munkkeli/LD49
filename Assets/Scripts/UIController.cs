using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIController : MonoBehaviour {
    public static UIController instance;
    
    [Header("Status Counters")]
    public Text fishText;
    public Text penguinText;

    [Header("Iceberg Section Counts")]
    public Text leftText;
    public Text middleText;
    public Text rightText;

    [Header("Alarm Icons")]
    public UnityEngine.UI.Button leftAlarm;
    public UnityEngine.UI.Button middleAlarm;
    public UnityEngine.UI.Button rightAlarm;
    public GameObject fishAlarm;

    public GameObject winText;
    public GameObject failText;

    private float _flashTimer = 1f;
    private bool _flashState = true;

    private void Awake() {
        instance = this;
    }

    public void OnGUI() {
        // Fail condition fulfilled
        if (Controller.instance.isFail) {
            failText.SetActive(true);
            return;
        }
        
        PenguinCount penguinCouns = Controller.instance.penguinCounts;
        
        // UI status counters
        fishText.text = $"{Math.Max(0, Math.Round(Controller.instance.fishCount, 2))}";
        penguinText.text = $"{penguinCouns.Total}";

        // Iceberg section counts
        leftText.text = $"{penguinCouns.left}";
        middleText.text = $"{penguinCouns.middle}";
        rightText.text = $"{penguinCouns.right}";
    }
    
    public void Update() {
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0) {
            _flashTimer = 0.1f;
            _flashState = !_flashState;
        }

        FailState failState = Controller.instance.failState;

        // Fail near indicators
        middleAlarm.gameObject.SetActive(failState.isIcebergFailNear && _flashState);
        fishAlarm.SetActive(failState.isFishFailNear && _flashState);
        
        // Orca indicators
        leftAlarm.gameObject.SetActive(Controller.instance.leftOrca.State == OrcaState.Attacking && _flashState);
        rightAlarm.gameObject.SetActive(Controller.instance.rightOrca.State == OrcaState.Attacking && _flashState);
    }
}
