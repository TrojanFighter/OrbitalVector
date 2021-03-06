﻿using UnityEngine;
using System.Collections;

public class LookAtCamera : MonoBehaviour {
    GameObject camera;
	// Use this for initialization
	void Start () {
		if (InitMe.GetInstance().EnableVR) {
			camera = GameObject.Find ("Camera (eye)");
		} else {
			camera = GameObject.Find ("Camera");
		}
        if (camera == null)
        {
            Debug.Log("Can't find camera");
        }
	}
	
	// Update is called once per frame
	void Update () {
        transform.LookAt(camera.transform.position);
	}
}
