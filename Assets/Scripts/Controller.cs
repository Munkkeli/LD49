using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public struct PenguinCount {
    public int left;
    public int middle;
    public int right;

    public int Total => left + middle + right;
}

public struct FailState {
    public bool isIcebergFailNear;
    public bool isFishFailNear;

    public bool Any => isIcebergFailNear || isFishFailNear;
}

public class Controller : MonoBehaviour {
    public static Controller instance;

    public float difficulty = 0f;
    public float _risingDifficulty = 0f;

    public Camera camera;
    public AudioSource icebergAudio;
    public GameObject penguinPrefab;

    public AudioClip[] iceSlushSounds;
    public AudioClip alarmSound;
    public AudioClip orcaAlarmSound;

    public AudioSource alarmSource;
    
    public Transform spawnPoint;
    public Transform leftFishing;
    public Transform rightFishing;
    
    public ParticleSystem leftFishParticles;
    public ParticleSystem rightFishParticles;
    public ParticleSystem heartParticles;

    public Transform[] rotateWithCamera;
    
    private readonly List<Penguin> _penguins = new List<Penguin>();

    public PenguinCount penguinCounts = new PenguinCount();
    public FailState failState = new FailState();

    private Transform _cameraTransform;
    public float _icebergTilt = 0;
    public float _icebergTiltModifier = 0;
    private float _icebergTiltSoundCooldown = 0;
    private float _icebergSinTime = 0;
    private float _currentTiltRotation;
    private float _icebergTiltVel;
    private float _icebergFailTimer = 0f;

    private float _orcaAlarmCooldown = 0f;

    private float _penguinSpawnCooldown = 1f;

    public float fishCount = 100;
    public int penguinCount = 10;

    public Orca leftOrca;
    public Orca rightOrca;

    private int _orcaSideBias = 0;

    private AudioSource _leftOrcaAudio;
    private AudioSource _rightOrcaAudio;

    private ParticleSystem.EmissionModule _leftFishEmission;
    private ParticleSystem.EmissionModule _rightFishEmission;
    private ParticleSystem.EmissionModule _heartEmission;
    
    private ParticleSystem.MinMaxCurve _leftFishEmissionCurve;
    private ParticleSystem.MinMaxCurve _rightFishEmissionCurve;
    private ParticleSystem.MinMaxCurve _heartEmissionCurve;

    public bool isFail = false;

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

        _heartEmission = heartParticles.emission;
        _heartEmissionCurve = new ParticleSystem.MinMaxCurve(10);
        _heartEmission.rateOverTime = _heartEmissionCurve;

        _leftOrcaAudio = leftFishing.GetComponent<AudioSource>();
        _rightOrcaAudio = rightFishing.GetComponent<AudioSource>();
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

        failState.isIcebergFailNear = Math.Abs(_icebergTilt) > 0.8f;

        penguinCounts.left = left;
        penguinCounts.middle = middle;
        penguinCounts.right = right;
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
        if (isFail) {
            Physics2D.gravity = _cameraTransform.rotation * Vector2.down *4f;
            return;
        };

        if (failState.Any && !alarmSource.isPlaying) {
            alarmSource.PlayOneShot(alarmSound);
        }

        if (leftOrca.State == OrcaState.Attacking || rightOrca.State == OrcaState.Attacking) {
            _orcaAlarmCooldown -= Time.deltaTime;
        }
        else {
            _orcaAlarmCooldown = 0f;
        }

        if (leftOrca.State == OrcaState.Attacking && _orcaAlarmCooldown <= 0) {
            _leftOrcaAudio.PlayOneShot(orcaAlarmSound);
            _orcaAlarmCooldown = 0.5f;
        }
        
        if (rightOrca.State == OrcaState.Attacking && _orcaAlarmCooldown <= 0) {
            _rightOrcaAudio.PlayOneShot(orcaAlarmSound);
            _orcaAlarmCooldown = 0.5f;
        }

        float targetTiltRotation = 24 * -_icebergTilt;
        _currentTiltRotation = Mathf.SmoothDamp(_currentTiltRotation, targetTiltRotation, ref _icebergTiltVel,
           1f);
        
        _cameraTransform.rotation = Quaternion.Euler(0, 0, _currentTiltRotation);
        foreach (Transform _transform in rotateWithCamera) {
            _transform.rotation = Quaternion.Euler(0, 0, _currentTiltRotation);
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

        failState.isFishFailNear = fishCount < 20 && (fishIncrement - fishDecrement) < 0;
        
        _heartEmission.rateOverTimeMultiplier = middle * 0.5f;

        _penguinSpawnCooldown -= Time.deltaTime * middle * 0.02f;
        if (_penguinSpawnCooldown <= 0) {
            _penguinSpawnCooldown = 1f;
            CreatePenguin();
        }

        if (fishCount < 0) {
            isFail = true;
        }

        if (Mathf.Abs(_icebergTilt) >= 1f) {
            _icebergFailTimer += Time.deltaTime;
            if (_icebergFailTimer > 3f) {
                isFail = true;
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

        float difficultyPenguinCount = Mathf.Max(0, penguinCount - 20f);
        float penguinDifficulty = Math.Min(1f, (difficultyPenguinCount * 2.2f) / 100f);
        _risingDifficulty += Time.deltaTime * 0.01f;
        difficulty = (penguinDifficulty * 5.5f) + _risingDifficulty;
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
