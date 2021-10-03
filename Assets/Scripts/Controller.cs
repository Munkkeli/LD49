using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GameState {
    Intro = 1,
    Criteria = 2,
    Tutorial = 3,
    Game = 4,
    Fail = 5,
    Win = 6,
}

public enum FailReason {
    Fish = 1,
    Penguin = 2,
    Iceberg = 3,
}

public struct PenguinCount {
    public int left;
    public int middle;
    public int right;

    public int Total => left + middle + right;
}

public struct FailState {
    public bool isIcebergFailNear;
    public bool isFishFailNear;

    public FailReason reason;
    
    public bool Any => isIcebergFailNear || isFishFailNear;
}

public class Controller : MonoBehaviour {
    public static Controller instance;

    public GameState State {
        get => _state;
        set {
            _state = value;
            UIController.instance.ChangeState(value);
        }
    }
    private GameState _state = GameState.Intro;

    public float difficulty = 0f;
    public float risingDifficulty = 0f;

    public Camera camera;
    public AudioSource icebergAudio;
    public GameObject penguinPrefab;

    public AudioClip[] iceSlushSounds;
    public AudioClip[] fishBlubSounds;
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
    public float icebergTilt = 0;
    public float icebergTiltModifier = 0;
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
    
    private AudioSource _leftFishAudio;
    private AudioSource _rightFishAudio;

    private ParticleSystem.EmissionModule _leftFishEmission;
    private ParticleSystem.EmissionModule _rightFishEmission;
    private ParticleSystem.EmissionModule _heartEmission;
    
    private ParticleSystem.MinMaxCurve _leftFishEmissionCurve;
    private ParticleSystem.MinMaxCurve _rightFishEmissionCurve;
    private ParticleSystem.MinMaxCurve _heartEmissionCurve;

    public bool isFail = false;
    public bool isWin = false;

    private float _orcaAttackCooldown = 30f;

    private float _penguinSqueakCooldown = 1f;
    public float _fishBlubCooldown = 1f;
    
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
        _leftFishEmission.rateOverTimeMultiplier = 0;

        _rightFishEmission = rightFishParticles.emission;
        _rightFishEmissionCurve = new ParticleSystem.MinMaxCurve(10);
        _rightFishEmission.rateOverTime = _rightFishEmissionCurve;
        _rightFishEmission.rateOverTimeMultiplier = 0;

        _heartEmission = heartParticles.emission;
        _heartEmissionCurve = new ParticleSystem.MinMaxCurve(10);
        _heartEmission.rateOverTime = _heartEmissionCurve;
        _heartEmission.rateOverTimeMultiplier = 0;

        _leftOrcaAudio = leftFishing.GetComponent<AudioSource>();
        _rightOrcaAudio = rightFishing.GetComponent<AudioSource>();

        _leftFishAudio = leftFishParticles.GetComponent<AudioSource>();
        _rightFishAudio = rightFishParticles.GetComponent<AudioSource>();
    }

    private void UpdateIcebergTilt() {
        float icebergDifficulty = Mathf.Min(0.25f, difficulty * 0.01f);

        _icebergSinTime += Time.deltaTime * (0.1f + icebergDifficulty);
        icebergTiltModifier = Mathf.Sin(_icebergSinTime) * 1f;

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
        float totalTilt = penguinTilt + icebergTiltModifier;

        icebergTilt = Mathf.Clamp(totalTilt, -1f, 1f);

        failState.isIcebergFailNear = Math.Abs(icebergTilt) > 0.8f;

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
        if (_state != GameState.Game && _state != GameState.Fail) return;
        
        penguinCount = _penguins.Count;
        if (penguinCount >= 100 && !isWin) {
            State = GameState.Win;
            isWin = true;
        }
        
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

        float targetTiltRotation = 24 * -icebergTilt;
        _currentTiltRotation = Mathf.SmoothDamp(_currentTiltRotation, targetTiltRotation, ref _icebergTiltVel,
           1f);
        
        _cameraTransform.rotation = Quaternion.Euler(0, 0, _currentTiltRotation);
        foreach (Transform transform in rotateWithCamera) {
            transform.rotation = Quaternion.Euler(0, 0, _currentTiltRotation);
        }

        _icebergTiltSoundCooldown -= Time.deltaTime;
        if (Math.Abs(icebergTilt) > 0.1 && _icebergTiltSoundCooldown <= 0) {
            _icebergTiltSoundCooldown = Random.Range(2f, 6f);
            icebergAudio.transform.position = (Vector2)spawnPoint.position + (Random.insideUnitCircle * 3f);
            icebergAudio.PlayOneShot(iceSlushSounds[Random.Range(0, iceSlushSounds.Length)], Math.Abs(icebergTilt) * 0.9f);
        }

        int left = _penguins.Count(penguin => penguin.State == PenguinState.Left);
        int right = _penguins.Count(penguin => penguin.State == PenguinState.Right);
        int middle = _penguins.Count - (left + right);

        float fishDecrement = Time.deltaTime * middle * 0.1f;
        float fishIncrement = Time.deltaTime * (left + right) * 0.05f;

        _leftFishEmission.rateOverTimeMultiplier = left * 0.5f;
        _rightFishEmission.rateOverTimeMultiplier = right * 0.5f;
        
        fishCount += fishIncrement - fishDecrement;

        failState.isFishFailNear = fishCount < 20 && (fishIncrement - fishDecrement) < 0;
        
        _heartEmission.rateOverTimeMultiplier = middle * 0.5f;

        _penguinSpawnCooldown -= Time.deltaTime * middle * 0.03f;
        if (_penguinSpawnCooldown <= 0) {
            _penguinSpawnCooldown = 1f;
            CreatePenguin();
        }

        _fishBlubCooldown -= Time.deltaTime * Math.Min(10f, Math.Max(0, fishIncrement * 1400f));
        if (_fishBlubCooldown <= 0) {
            _fishBlubCooldown = 1f;
            FishBlub();
        }

        if (fishCount < 0) {
            FailGame(FailReason.Fish);
            return;
        }
        
        if (penguinCount <= 0) {
            FailGame(FailReason.Penguin);
            return;
        }

        if (Mathf.Abs(icebergTilt) >= 1f) {
            _icebergFailTimer += Time.deltaTime;
            if (_icebergFailTimer > 3f) {
                FailGame(FailReason.Iceberg);
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

        _penguinSqueakCooldown -= Time.deltaTime;
        if (_penguinSqueakCooldown <= 0) {
            float penguinDecrease = Mathf.Min(1f, penguinCount / 90f);
            float tiltDecrease = Mathf.Min(5f, Mathf.Abs(icebergTilt) * 5f);
            float orcaDecrease = 0;
            if (leftOrca.State == OrcaState.Attacking) {
                orcaDecrease = Mathf.Max(0, Mathf.Min(5f, 5f - Math.Abs(leftFishing.position.x - leftOrca.transform.position.x)));
            }
            if (rightOrca.State == OrcaState.Attacking) {
                orcaDecrease = Mathf.Max(0, Mathf.Min(5f, 5f - Math.Abs(rightFishing.position.x - rightOrca.transform.position.x)));
            }

            _penguins[Random.Range(0, _penguins.Count)].Squeak();
            
            _penguinSqueakCooldown = Mathf.Max(0.1f, 5.2f - (penguinDecrease + tiltDecrease + orcaDecrease)) + Random.Range(-0.3f, 0.2f);
        }

        float difficultyPenguinCount = Mathf.Max(0, penguinCount - 20f);
        float penguinDifficulty = Math.Min(1f, (difficultyPenguinCount * 2.8f) / 90f);
        risingDifficulty += Time.deltaTime * 0.01f;
        difficulty = (penguinDifficulty * 5.5f) + risingDifficulty;
    }

    private void FixedUpdate() {
        if (_state != GameState.Game) return;
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

    private void FailGame(FailReason reason) {
        _leftFishEmission.enabled = false;
        _rightFishEmission.enabled = false;
        _heartEmission.enabled = false;

        failState.reason = reason;
        State = GameState.Fail;
        
        isFail = true;
    }
    
    public void FishBlub() {
        AudioSource audioSource = Random.value > 0.5f ? _leftFishAudio : _rightFishAudio;
        if (audioSource.isPlaying) return;
        
        audioSource.pitch = Random.Range(1f, 1.2f);
        audioSource.volume = Random.Range(0.8f, 1f);
        audioSource.PlayOneShot(fishBlubSounds[Random.Range(0, fishBlubSounds.Length)]);
    }
}
