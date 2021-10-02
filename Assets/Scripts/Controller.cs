using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Controller : MonoBehaviour {
    public static Controller instance;

    public float difficulty = 0f;
    public float _risingDifficulty = 0f;

    public Camera camera;
    public AudioSource icebergAudio;
    public GameObject penguinPrefab;

    public AudioClip[] iceSlushSounds;
    
    public Transform spawnPoint;
    public Transform leftFishing;
    public Transform rightFishing;

    public Text leftText;
    public Text middleText;
    public Text rightText;
    
    public UnityEngine.UI.Button leftAlarm;
    public UnityEngine.UI.Button middleAlarm;
    public UnityEngine.UI.Button rightAlarm;

    public Text fishText;
    public Text penguinText;

    public GameObject winText;
    public GameObject failText;

    public ParticleSystem leftFishParticles;
    public ParticleSystem rightFishParticles;

    public GameObject fishAlarm;

    public Transform[] rotateWithCamera;
    
    private readonly List<Penguin> _penguins = new List<Penguin>();

    private Transform _cameraTransform;
    public float _icebergTilt = 0;
    public float _icebergTiltModifier = 0;
    private float _icebergTiltSoundCooldown = 0;
    private float _icebergSinTime = 0;

    private float _penguinSpawnCooldown = 1f;

    private float _flashTimer = 1f;
    private bool _flashState = true;

    public float fishCount = 100;
    public int penguinCount = 10;

    public Orca leftOrca;
    public Orca rightOrca;

    private int _orcaSideBias = 0;

    private bool _isIcebergFailNear = false;
    private float _icebergFailTimer = 0f;
    
    private bool _isFishFailNear = false;
    private bool _isPenguinFailNear = false;

    private ParticleSystem.EmissionModule _leftFishEmission;
    private ParticleSystem.EmissionModule _rightFishEmission;

    private ParticleSystem.MinMaxCurve _leftFishEmissionCurve;
    private ParticleSystem.MinMaxCurve _rightFishEmissionCurve;

    private bool _isFail = false;

    private float _orcaAttackCooldown = 30f;
    
    private Penguin? GetPenguin(PenguinState state, PenguinState direction) {
        Penguin? match = _penguins.FirstOrDefault(penguin => penguin.State == state);
        if (match) return match;

        return direction switch {
            PenguinState.Left => _penguins.FirstOrDefault(penguin => penguin.State == PenguinState.Right),
            PenguinState.Right => _penguins.FirstOrDefault(penguin => penguin.State == PenguinState.Left),
            _ => null
        };
    }

    private void Awake() {
        instance = this;
        _cameraTransform = camera.transform;

        _leftFishEmission = leftFishParticles.emission;
        _leftFishEmissionCurve = new ParticleSystem.MinMaxCurve(10);
        _leftFishEmission.rateOverTime = _leftFishEmissionCurve;

        _rightFishEmission = rightFishParticles.emission;
        _rightFishEmissionCurve = new ParticleSystem.MinMaxCurve(10);
        _rightFishEmission.rateOverTime = _rightFishEmissionCurve;

        // leftPlus.onClick.AddListener(() => {
        //     Penguin? penguin = GetPenguin(PenguinState.Idle, PenguinState.Left);
        //     if (penguin != null) penguin.State = PenguinState.Left;
        // });
        //
        // leftMinus.onClick.AddListener(() => {
        //     Penguin? penguin = GetPenguin(PenguinState.Left, PenguinState.Idle);
        //     if (penguin != null) penguin.State = PenguinState.Idle;
        // });
        //
        // // leftAlarm.onClick.AddListener(() => {
        // //     foreach (Penguin penguin in _penguins.Where(penguin => penguin.State == PenguinState.Left)) {
        // //         penguin.State = PenguinState.Idle;
        // //     }
        // // });
        //
        // rightPlus.onClick.AddListener(() => {
        //     Penguin? penguin = GetPenguin(PenguinState.Idle, PenguinState.Right);
        //     if (penguin != null) penguin.State = PenguinState.Right;
        // });
        //
        // rightMinus.onClick.AddListener(() => {
        //     Penguin? penguin = GetPenguin(PenguinState.Right, PenguinState.Idle);
        //     if (penguin != null) penguin.State = PenguinState.Idle;
        // });
        //
        // // rightAlarm.onClick.AddListener(() => {
        // //     foreach (Penguin penguin in _penguins.Where(penguin => penguin.State == PenguinState.Right)) {
        // //         penguin.State = PenguinState.Idle;
        // //     }
        // // });
    }

    private void UpdateIcebergTilt() {
        float icebergDifficulty = Mathf.Min(0.25f, difficulty * 0.008f);

        _icebergSinTime += Time.deltaTime * (0.1f + icebergDifficulty);
        _icebergTiltModifier = Mathf.Sin(_icebergSinTime) * 1f;

        float center = spawnPoint.transform.position.x;

        int left = 0;
        int middle = 0;
        int right = 0;
        
        float totalWeight = _penguins.Sum(penguin => {
            if (penguin.State == PenguinState.Left) left++;
            if (penguin.State == PenguinState.Idle) middle++;
            if (penguin.State == PenguinState.Right) right++;
            
            return center - penguin.transform.position.x;
        });

        float penguinTilt = (totalWeight / _penguins.Count) / 8f;
        float totalTilt = penguinTilt + _icebergTiltModifier;

        _icebergTilt = Mathf.Clamp(totalTilt, -1f, 1f);

        _isIcebergFailNear = Math.Abs(_icebergTilt) > 0.8f;

        leftText.text = $"{left}";
        middleText.text = $"{middle}";
        rightText.text = $"{right}";
    }

    private void CreatePenguin() {
        GameObject obj = Instantiate(penguinPrefab);
        obj.transform.position = spawnPoint.position;
            
        _penguins.Add(obj.GetComponent<Penguin>());
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < penguinCount; i++) {
            CreatePenguin();
        }
    }

    // Update is called once per frame
    void Update() {
        if (_isFail) {
            failText.SetActive(true);
           // return;
        }

        float tiltRotation = 24 * -_icebergTilt;
        
        _cameraTransform.rotation = Quaternion.Euler(0, 0, tiltRotation);

        foreach (Transform _transform in rotateWithCamera) {
            _transform.rotation = Quaternion.Euler(0, 0, tiltRotation);
        }

        _icebergTiltSoundCooldown -= Time.deltaTime;
        if (Math.Abs(_icebergTilt) > 0.1 && _icebergTiltSoundCooldown <= 0) {
            _icebergTiltSoundCooldown = Random.Range(2f, 6f);
            icebergAudio.transform.position = (Vector2)spawnPoint.position + (Random.insideUnitCircle * 3f);
            icebergAudio.PlayOneShot(iceSlushSounds[Random.Range(0, iceSlushSounds.Length)], Math.Abs(_icebergTilt) * 10f);
        }

        int left = _penguins.Count(penguin => penguin.State == PenguinState.Left);
        int right = _penguins.Count(penguin => penguin.State == PenguinState.Right);
        int middle = _penguins.Count - (left + right);

        float fishDecrement = Time.deltaTime * middle * 0.1f;
        float fishIncrement = Time.deltaTime * (left + right) * 0.05f;

        _leftFishEmission.rateOverTimeMultiplier = left * 0.5f;
        _rightFishEmission.rateOverTimeMultiplier = right * 0.5f;

        penguinCount = _penguins.Count;
        fishCount += fishIncrement - fishDecrement;

        _isFishFailNear = fishCount < 20;

        fishText.text = $"{Math.Max(0, Math.Round(fishCount, 2))}";
        penguinText.text = $"{penguinCount}";

        _penguinSpawnCooldown -= Time.deltaTime * middle * 0.03f;
        if (_penguinSpawnCooldown <= 0) {
            _penguinSpawnCooldown = 1f;
            CreatePenguin();
        }

        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0) {
            _flashTimer = 0.05f;
            _flashState = !_flashState;
            
            // failText.SetActive(_flashState);
        }

        middleAlarm.gameObject.SetActive(_isIcebergFailNear && _flashState);
        fishAlarm.SetActive(_isFishFailNear && _flashState);
        
        leftAlarm.gameObject.SetActive(leftOrca.State == OrcaState.Attacking && _flashState);
        rightAlarm.gameObject.SetActive(rightOrca.State == OrcaState.Attacking && _flashState);

        if (fishCount < 0) {
            _isFail = true;
        }

        if (Mathf.Abs(_icebergTilt) >= 1f) {
            _icebergFailTimer += Time.deltaTime;
            if (_icebergFailTimer > 3f) {
                _isFail = true;
            }
        }
        else {
            _icebergFailTimer = 0f;
        }

        _orcaAttackCooldown -= Time.deltaTime;
        if (_orcaAttackCooldown <= 0) {
            _orcaAttackCooldown = Random.Range(20f, 30f) - Math.Min(14f, difficulty);

            float orcaSide = Random.Range(-2f, 2f) + _orcaSideBias;
            if (orcaSide < 0) {
                leftOrca.State = OrcaState.Attacking;
                _orcaSideBias += 1;
            }
            else {
                rightOrca.State = OrcaState.Attacking;
                _orcaSideBias -= 1;
            }
        }

        _risingDifficulty += Time.deltaTime * 0.01f;
        difficulty = ((penguinCount / 100f) * 4f) + _risingDifficulty;
    }

    private void FixedUpdate() {
        UpdateIcebergTilt();
    }

    private void KillPenguin(Penguin penguin) {
        _penguins.Remove(penguin);
        Destroy(penguin.gameObject);
    }

    public void OrcaAttack(PenguinState side) {
        Penguin[] potentialVictims = _penguins.Where(penguin => penguin.State == side && penguin.isAtGoal).ToArray();
        if (!potentialVictims.Any()) return;

        int numberOfVictims = 1 + (int)Random.Range(potentialVictims.Length * 0.25f, potentialVictims.Length * 0.75f);
        for (int i = 0; i < numberOfVictims; i++) {
            KillPenguin(potentialVictims[i]);
        }
        
        Debug.Log($"{numberOfVictims} penguin(s) died :(");
    }

    public void OrderPenguin(PenguinState from, PenguinState to) {
        Penguin? penguin = GetPenguin(from, to);
        if (penguin != null) penguin.State = to;
    }
}
