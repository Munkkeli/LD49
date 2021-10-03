using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Background")] public Transform smallIcebergsBackground;

    [Header("Menu")]
    public GameObject menuIntroPanel;
    public UnityEngine.UI.Button readyButton;
    
    public GameObject menuCriteriaPanel;
    public Image fishFailImage;
    public Image penguinFailImage;
    public Image icebergFailImage;
    public Image penguinWinImage;
    public GameObject failText;
    public UnityEngine.UI.Button continueButton;
    
    public GameObject menuTutorialPanel;
    public UnityEngine.UI.Button startButton;
    
    public GameObject winPanel;
    public UnityEngine.UI.Button keepGoingButton;
    
    [Header("Misc")]
    public GameObject winText;

    private float _flashTimer = 1f;
    private bool _flashState = true;

    private void Awake() {
        instance = this;
        
        readyButton.onClick.AddListener(() => {
            Controller.instance.State = GameState.Criteria;
        });
        
        continueButton.onClick.AddListener(() => {
            if (Controller.instance.State == GameState.Fail) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            Controller.instance.State = GameState.Tutorial;
        });
        
        startButton.onClick.AddListener(() => {
            Controller.instance.State = GameState.Game;
        });
        
        keepGoingButton.onClick.AddListener(() => {
            Controller.instance.State = GameState.Game;
        });

        ChangeState(GameState.Intro);
    }

    public void OnGUI() {
        // Fail condition fulfilled
        if (Controller.instance.isFail) {
            failText.SetActive(true);
            return;
        }
        
        PenguinCount penguinCounts = Controller.instance.penguinCounts;
        
        // UI status counters
        fishText.text = $"{Math.Max(0, Math.Round(Controller.instance.fishCount, 2))}";
        penguinText.text = $"{penguinCounts.Total}";

        // Iceberg section counts
        leftText.text = $"{penguinCounts.left}";
        middleText.text = $"{penguinCounts.middle}";
        rightText.text = $"{penguinCounts.right}";
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
        
        // Background iceberg scroll
        smallIcebergsBackground.localPosition =
            new Vector3(Controller.instance.icebergTiltModifier * 0.1f, smallIcebergsBackground.localPosition.y, 0);
    }

    public void ChangeState(GameState state) {
        menuIntroPanel.SetActive(false);
        menuCriteriaPanel.SetActive(false);
        menuTutorialPanel.SetActive(false);
        winPanel.SetActive(false);
        
        if (state == GameState.Intro) {
            menuIntroPanel.SetActive(true);
        }
        if (state == GameState.Criteria) {
            menuCriteriaPanel.SetActive(true);
            fishFailImage.color = Color.white;
            penguinFailImage.color = Color.white;
            icebergFailImage.color = Color.white;
            penguinWinImage.color = Color.white;
            failText.SetActive(false);
            continueButton.GetComponentInChildren<Text>().text = "Continue";
        }
        if (state == GameState.Tutorial) {
            menuTutorialPanel.SetActive(true);
        }
        if (state == GameState.Fail) {
            menuCriteriaPanel.SetActive(true);
            fishFailImage.color = new Color(1, 1, 1, 0.2f);
            penguinFailImage.color = new Color(1, 1, 1, 0.2f);
            icebergFailImage.color = new Color(1, 1, 1, 0.2f);
            penguinWinImage.color = new Color(1, 1, 1, 0f);

            if (Controller.instance.failState.reason == FailReason.Fish) {
                fishFailImage.color = Color.white;
            }
            if (Controller.instance.failState.reason == FailReason.Penguin) {
                penguinFailImage.color = Color.white;
            }
            if (Controller.instance.failState.reason == FailReason.Iceberg) {
                icebergFailImage.color = Color.white;
            }

            failText.SetActive(true);
            
            continueButton.GetComponentInChildren<Text>().text = "Retry";
        }
        if (state == GameState.Win) {
            winPanel.SetActive(true);
        }
    }
}
