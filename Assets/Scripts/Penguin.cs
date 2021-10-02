using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public enum PenguinState {
    Idle = 1,
    Left = 2,
    Right = 3,
}

public class Penguin : MonoBehaviour {
    public float groundOffset = 1f;
    public float movementSpeed = 0.5f;

    public PenguinState State {
        get => _state;
        set {
            _state = value;
            ResetGoalOffset();
        }
    }

    private PenguinState _state = PenguinState.Idle;
    private float _goalOffset = 0;
    private float _groundOffset = 0;

    private float _fidgetTimer = 0;

    private SpriteRenderer _renderer;
    private Collider2D _collider;
    private Rigidbody2D _rigidbody;

    public bool isAtGoal = true;
    
    private void Awake() {
        _renderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _rigidbody = GetComponent<Rigidbody2D>();
        
        _groundOffset = groundOffset + Random.Range(-0.1f, 0f);
        
        ResetGoalOffset();
        ResetFidgetTimer();
    }

    private void ResetGoalOffset() {
        _goalOffset = State switch {
            PenguinState.Left => Random.Range(0f, 0.5f),
            PenguinState.Right => Random.Range(-0.5f, 0f),
            _ =>  Random.Range(-1f, 1f)
        };
    }
    
    private void ResetFidgetTimer() {
        _fidgetTimer = Random.Range(1f, 5f);
    }

    // Update is called once per frame
    void Update() {
        if (Controller.instance.isFail) {
            _collider.enabled = true;
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            return;
        }

        Vector3 position = transform.position;
        
        Vector2 goal = State switch {
            PenguinState.Idle => Controller.instance.spawnPoint.position,
            PenguinState.Left => Controller.instance.leftFishing.position,
            PenguinState.Right => Controller.instance.rightFishing.position,
            _ => position
        };
        
        float distanceToGoal = position.x - (goal.x + _goalOffset);
        isAtGoal = !(Mathf.Abs(distanceToGoal) > 0.01f);

        if (!isAtGoal) {
            float translation = (distanceToGoal < 0 ? movementSpeed : -movementSpeed) * Time.deltaTime;

            _renderer.flipX = translation > 0;

            translation = translation < 0
                ? Mathf.Min(translation, distanceToGoal)
                : Mathf.Max(translation, distanceToGoal);
            transform.Translate(new Vector2(translation, 0));
        }

        _fidgetTimer -= Time.deltaTime;
        if (_fidgetTimer <= 0) {
            if (isAtGoal) {
                if (Random.value > 0.25f) {
                    ResetGoalOffset();
                }
                else {
                    _renderer.flipX = !_renderer.flipX;
                }
            }
            
            ResetFidgetTimer();
        }
        
        transform.rotation = Quaternion.Euler(0, 0, Controller.instance.camera.transform.rotation.eulerAngles.z);
    }

    private void FixedUpdate() {
        if (Controller.instance.isFail) return;
        
        Vector3 position = transform.position;
        
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 10f);
        
        if (hit) {
            transform.position = new Vector3(position.x, hit.point.y + _groundOffset, position.z);
        }
    }
}
