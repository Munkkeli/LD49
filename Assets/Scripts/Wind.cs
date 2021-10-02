using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wind : MonoBehaviour {
    private AudioSource _audioSource;
    
    // Start is called before the first frame update
    void Start() {
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update() {
        float one = (1f + Mathf.Sin(Time.time)) / 2f;
        float two = (1f + Mathf.Sin(Time.time * 0.5f)) / 2f;

        float final = Mathf.Clamp(0.5f + ((one + two) / 2f), 0.5f, 1.5f);
        
        _audioSource.pitch = final;
    }
}
