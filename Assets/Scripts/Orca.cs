using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OrcaState {
    Idle = 1,
    Attacking = 2,
    Retreating = 3,
}

public class Orca : MonoBehaviour {
    public PenguinState side;
    public OrcaState State {
        get => _state;
        set {
            _state = value;
            if (value == OrcaState.Attacking) transform.position = _startPosition;
        }
    }
    
    public float movementSpeed = 1f;
    
    public Transform attackPoint;

    private OrcaState _state = OrcaState.Idle;
    private SpriteRenderer _renderer;
    private Vector3 _startPosition;

    // Start is called before the first frame update
    void Start() {
        _renderer = GetComponent<SpriteRenderer>();
        _startPosition = transform.position;
    }

    // Update is called once per frame
    void Update() {
        if (State == OrcaState.Idle) {
            transform.position = _startPosition;
            return;
        }

        int retreatDirection = side == PenguinState.Left ? 10 : -10;

        Vector2 position = transform.position;
        Vector2 goal = State switch {
            OrcaState.Attacking => attackPoint.position,
            OrcaState.Retreating => attackPoint.position + new Vector3(retreatDirection, -10f),
            _ => _startPosition,
        };
        
        float distanceToGoal = position.x - goal.x;
        float distanceFromAttack = position.x - attackPoint.position.x;
        bool isAtGoal = !(Mathf.Abs(distanceToGoal) > 0.01f);

        if (!isAtGoal) {
            float translation = (distanceToGoal < 0 ? movementSpeed : -movementSpeed) * Time.deltaTime;

            // _renderer.flipX = translation > 0;

            translation = translation < 0
                ? Mathf.Min(translation, distanceToGoal)
                : Mathf.Max(translation, distanceToGoal);
            transform.Translate(new Vector2(translation, 0));

            position = transform.position;

            if (State == OrcaState.Attacking) {
                position.y = goal.y - distanceToGoal;
                if (side == PenguinState.Left) {
                    position.y = goal.y + distanceToGoal;
                }
                else {
                    position.y = goal.y - distanceToGoal;
                }
            }
            if (State == OrcaState.Retreating) {
                if (side == PenguinState.Left) {
                    position.y = attackPoint.position.y - distanceFromAttack;
                }
                else {
                    position.y = attackPoint.position.y + distanceFromAttack;
                }
            }

            transform.position = position;
        }
        else {
            if (State == OrcaState.Attacking) {
                Controller.instance.OrcaAttack(side);
            }

            State = State switch {
                OrcaState.Attacking => OrcaState.Retreating,
                OrcaState.Retreating => OrcaState.Idle,
                _ => OrcaState.Idle,
            };
        }
    }
}
