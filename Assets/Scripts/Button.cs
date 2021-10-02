using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Button : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    public PenguinState from;
    public PenguinState to;

    private float repeatSpeed = 0.05f;

    private bool _isDown = false;
    private float _repeatCooldown;
    
    // Start is called before the first frame update
    void Start() {
        _repeatCooldown = repeatSpeed;
    }

    // Update is called once per frame
    void Update() {
        if (_isDown) {
            _repeatCooldown -= Time.deltaTime;
            if (_repeatCooldown <= 0) {
                _repeatCooldown = repeatSpeed;
                Controller.instance.OrderPenguin(from, to);
            }
        }
    }

    public void OnPointerDown(PointerEventData data) {
        _isDown = true;
        _repeatCooldown = 0.3f;
        
        Controller.instance.OrderPenguin(from, to);
    }

    public void OnPointerUp(PointerEventData data) {
        _isDown = false;
    }
}
