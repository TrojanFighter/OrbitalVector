﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XboxCtrlrInput;		// Be sure to include this if you want an object to have Xbox input
//public class Controller

public class ShipPhysics : MonoBehaviour 
{
	public XboxController controller;
    public void QueryControllers()
    {
        int queriedNumberOfCtrlrs = XCI.GetNumPluggedCtrlrs();

        if (queriedNumberOfCtrlrs == 1)
        {
            Debug.Log("Only " + queriedNumberOfCtrlrs + " Xbox controller plugged in.");
        }
        else if (queriedNumberOfCtrlrs == 0)
        {
            Debug.Log("No Xbox controllers plugged in!");
        }
        else
        {
            Debug.Log(queriedNumberOfCtrlrs + " Xbox controllers plugged in.");
        }

        XCI.DEBUG_LogControllerNames();

        // This code only works on Windows
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            Debug.Log("Windows Only:: Any Controller Plugged in: " + XCI.IsPluggedIn(XboxController.Any).ToString());

            Debug.Log("Windows Only:: Controller 1 Plugged in: " + XCI.IsPluggedIn(XboxController.First).ToString());
            Debug.Log("Windows Only:: Controller 2 Plugged in: " + XCI.IsPluggedIn(XboxController.Second).ToString());
            Debug.Log("Windows Only:: Controller 3 Plugged in: " + XCI.IsPluggedIn(XboxController.Third).ToString());
            Debug.Log("Windows Only:: Controller 4 Plugged in: " + XCI.IsPluggedIn(XboxController.Fourth).ToString());
        }
    }
    bool lastPrint = false;
    public void GetController()
    {
        //RightStick
        if (XCI.GetButtonDown(XboxButton.RightStick, controller))
        {
            //TODO call rightstick delegate, which can be remapped to func
            nullSpin = true;
            NullSpin();
        }
        if (XCI.GetButtonDown(XboxButton.A, controller))
        {
            //fire!
            Fire();
        }
        if (XCI.GetButtonDown(XboxButton.B, controller))
        {
        }
        if (XCI.GetButtonDown(XboxButton.X, controller))
        {
            FireMissile();
        }
        if (XCI.GetButtonDown(XboxButton.Y, controller))
        {
        }

        // Left stick movement
		float axisY = XCI.GetAxis(XboxAxis.LeftStickY, controller);
        //if (axisY != 0) Debug.Log("axisY: " + axisY);
        float axisX = -XCI.GetAxis(XboxAxis.LeftStickX, controller);
        float rotScalar = 0.2f;
        float pitchAmt = Mathf.Sign(axisY) * Mathf.Sqrt(Mathf.Abs(axisY)) * rotScalar;
        float rollAmt = Mathf.Sign(axisX) * Mathf.Sqrt(Mathf.Abs(axisX)) * rotScalar;
        //actual camera+ship rotation
        PitchUp(-pitchAmt, true);
        RollLeft(-rollAmt, true);
        float neg = 1;
        //just ship rotation, for show
        var rot = Quaternion.Euler(pitchAmt*20, 0, 0) * Quaternion.Euler(0, 0, rollAmt*20);
        _ship.transform.localRotation = rot;
    }
//}
//public class ShipPhysics : MonoBehaviour 
//{
    Rigidbody rb;
    public GameObject _camera;
    public bool nullSpin = false;
    public bool FireMissileFlag = false;
    public float Torque = 10;
    public float engineScalar = 6;
    public KeyCode kLeft = KeyCode.A;
    public KeyCode kRight = KeyCode.D;
    public KeyCode kUp = KeyCode.W;
    public KeyCode kDown = KeyCode.S;

    public float SlowDown = 1;
    public GameObject _ship;
    public GameObject lEngine, rEngine;
    Transform leftEngine, rightEngine;
    public GameObject _beam, _gun;
    public GameObject _missile;
    public GameObject _target;
    public GameObject _debugCameraLineObj;
    LineRenderer _debugCameraLine;
    public GameObject _debugMarker;
    // Use this for initialization
    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.zero;
        leftEngine = transform.Find("engine_left");
        rightEngine = transform.Find("engine_right");
        if (CheckNull())
        {
            leftEngine = lEngine.transform;
            rightEngine = rEngine.transform;
            CheckNull();
        }
        _debugCameraLine = _debugCameraLineObj.GetComponent<LineRenderer>();
        QueryControllers();
    }
    GameObject CameraFollowMissile;
    float b;
    void CameraFollow(GameObject missile)
    {
        Debug.Log("Following!");
        _camera.transform.parent = null;//
        //_debugMarker.transform.parent = missile.transform;
        CameraFollowMissile = missile;
        b = (missile.transform.position - _camera.transform.position).magnitude;
        CameraFollow();
    }
    float QuadraticSolver(float A, float B, float C)
    {
        float a = float.NaN;
        var determinant = B * B - 4 * A * C;
        var sqrtD = Mathf.Sqrt(determinant);
        if (determinant < 0)
        {
            Debug.Log("complex conjugates, give up camera follow for now");
        } else if (determinant == 0)
        {
            //only 1 root
            a = (-1 * B + sqrtD) / 2;
        } else
        {
            //2 roots, find larger root
            var sol1 = (-1 * B + sqrtD) / (2 * A);
            var sol2 = (-1 * B - sqrtD) / (2 * A);
            a = Mathf.Max(sol1, sol2);
        }
        return a;
    }
    void CheckValidTriangle(float a, float b, float c)
    {
        Debug.Log("triangle sides a: " + a + " b: " + b + " c: " + c);
        var largest = Mathf.Max(a, Mathf.Max(b, c));
        if (largest > largest - a + b + c)
        {
            Debug.Log("invalid triangle");
            return;
        }
    }
    void CameraFollow()
    {
        if (CameraFollowMissile == null || !CameraFollowMissile.activeSelf
            || _target == null || !_target.activeSelf)
        {
            return;
        }
        var missile = CameraFollowMissile;
        float fov = 20; //fov in degrees
        var camera2missile = (missile.transform.position - _camera.transform.position);
        var missile2target = (_target.transform.position - missile.transform.position);
        float a = 0; //camera2target need to find this first
        //float b = camera2missile.magnitude;
        float c = missile2target.magnitude;

        //for debug visualization
        _debugCameraLine.SetPosition(0, _target.transform.position);
        _debugCameraLine.SetPosition(1, missile.transform.position);
        _debugCameraLine.SetPosition(2, _camera.transform.position);

        //find a, camera2target
        //using c^2 = a^2 + b^2 - 2abcos(fov); find a
        var B = -2 * b * Mathf.Cos(Mathf.Deg2Rad * fov);
        var C = b * b - c * c;
        a = QuadraticSolver(1, B, C);
        CheckValidTriangle(a, b, c);

        //now we know all sides of the triangle, time to find angle between target and camera relative to missile
        var phi = Mathf.Acos((b * b + c * c - a * a) / (2 * b * c)) * Mathf.Rad2Deg;
        Debug.Log("phi: " + phi );

        //get axis orthogonal to a,b,c
        var orthAxis = Vector3.Cross(camera2missile, missile2target).normalized;
        _debugCameraLine.SetPosition(3, orthAxis*5 + _camera.transform.position);
        _debugCameraLine.SetPosition(4, _camera.transform.position);

        //move camera to the right place by starting vector missile to target
        var newCameraPos = (missile2target.normalized) * b;//camera2missile dist

        //then rotate by phi around axis
        newCameraPos = Quaternion.AngleAxis(phi, orthAxis) * newCameraPos + missile.transform.position;
        _debugMarker.transform.position = missile2target + missile.transform.position;
        _camera.transform.position = newCameraPos;
        newCameraPos = missile.transform.position - _camera.transform.position;
        var newCamera2Target = _target.transform.position - _camera.transform.position;
        var newCamera2Missile = missile.transform.position - _camera.transform.position;
        Debug.Log("fov from phi: " + Vector3.Angle(newCamera2Missile, newCamera2Target));

        //turn towards middle between missile and target
        //start w/ dir from camera to misisle, then turn half fov (might be other way)
        var lookAtMissile = Quaternion.LookRotation( missile.transform.position - _camera.transform.position);
        //turn back fov/2
        var newCameraLook = Quaternion.AngleAxis(fov / 2, orthAxis) * lookAtMissile;
        _camera.transform.rotation = newCameraLook;
        _debugCameraLine.SetPosition(5, _camera.transform.forward*30 + _camera.transform.position);
    }
    Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }
    [ContextMenu("Fire Missile!")]
    public GameObject FireMissile()
    {
        Debug.Log("Fire missile");
        var newMissile = Instantiate(_missile);
        //set rot and pos to gun
        newMissile.SetActive(true);
        newMissile.transform.position = _gun.transform.position;
        newMissile.transform.rotation = _gun.transform.rotation;
        //give speed
        var speed = _gun.transform.forward * 5;
        newMissile.GetComponent<Rigidbody>().velocity = speed;
        //sound!
        //set die time
        var missileLogic = newMissile.GetComponent<MissileLogic>();
        missileLogic.BornTime = Time.time;
        missileLogic.DieAfterTime = 30;
        missileLogic.enabled = true;
        missileLogic.target = _target;
        CameraFollow(newMissile);
        return newMissile;
    }
    void Fire()
    {
        //take a copy of the beam, 
        var newBeam = Instantiate(_beam);
        //set rot and pos to gun
        newBeam.SetActive(true);
        newBeam.transform.position = _gun.transform.position;
        newBeam.transform.rotation = _gun.transform.rotation;
        //give speed
        var speed = _gun.transform.forward * 100;
        newBeam.GetComponent<Rigidbody>().velocity = speed;
        Debug.Log("beam velocity: " + speed.magnitude);
        //sound!
        //set die time
        var beamLogic = newBeam.GetComponent<BeamLogic>();
        beamLogic.BornTime = Time.time;
        beamLogic.DieAfterTime = 5;
        beamLogic.enabled = true;
    }
    bool CheckNull()
    { 
        if (leftEngine == null
            || rightEngine == null)
        {
            Debug.Log("Engine(s) not found!");
            return false;
        }
        return true;
	}
    void PitchUp(float torque, bool up = true)
    {
        float neg = (up) ? -1 : 1;
        var amt = neg * torque;
        rb.AddTorque(transform.right * amt);

        //var rot = Quaternion.Euler(-transform.right * neg * torque*200);
        //Debug.Log("pitching " + amt);
        var rot = Quaternion.Euler(amt*20, 0, 0);
        //ship.transform.Rotate(transform.forward, -amt);
        _ship.transform.localRotation = rot;
    }
    //spins engine to rotate by 1 engine rotation
    void PitchUpWithEngineSpin(float amt)
    {
        //TODO rewrite to rotate engine by 1 rotation only
        leftEngine.Rotate(transform.right, engineScalar * amt);
        rightEngine.Rotate(transform.right, engineScalar * amt);
        transform.Rotate(transform.right, -amt);
        rb.angularVelocity = Vector3.zero;
    }
    void RollLeft( float torque, bool left = true)
    {
        float neg = (left) ? -1 : 1;
        var amt = neg * torque;
        rb.AddTorque(transform.forward * amt);
        //var rot = Quaternion.Euler(-transform.right * neg * torque*200);
        //Debug.Log("rolling " + amt);
        var rot = Quaternion.Euler(0, 0, amt*20);
        //ship.transform.Rotate(transform.forward, -amt);
        _ship.transform.localRotation = rot;
        /*
        leftEngine.Rotate(transform.right, neg * engineScalar * amt);
        rightEngine.Rotate(transform.right, -neg * engineScalar * amt);
        transform.Rotate(transform.forward, -amt);
        rb.angularVelocity = Vector3.zero;
        */
    }
    //use rotational inertia of engines to rotate
    void GetKeyboard2()
    {
        if (Input.GetKey(kLeft)) RollLeft(Torque);
        else if (Input.GetKey(kRight)) RollLeft(Torque, false);
        else if (Input.GetKey(kUp)) PitchUp(Torque, false);
        else if (Input.GetKey(kDown)) PitchUp(Torque, true);
    }
    //use thrusters to rotate
    void GetKeyboard()
    {
        if (Input.GetKey(kLeft)) rb.AddTorque(transform.forward * Torque);
        else if (Input.GetKey(kRight)) rb.AddTorque(-transform.forward * Torque);
        else if (Input.GetKey(kUp)) PitchUp(Torque, true);
        else if (Input.GetKey(kDown)) rb.AddTorque(-transform.right * Torque);
    }
    void NullSpin()
    {
        if (nullSpin)
        {
            if (rb.angularVelocity.magnitude > SlowDown * Time.deltaTime)
            {
                rb.AddTorque(-rb.angularVelocity.normalized * SlowDown, ForceMode.Acceleration);
            } else
            {
                nullSpin = false;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
	// Update is called once per frame
	void FixedUpdate () {
        GetKeyboard();
        GetController();
        NullSpin();
        CameraFollow();
        /*
        Spin(ref SpinLeft, transform.up);
        Spin(ref SpinRight, -transform.up);
        Spin(ref SpinUp, transform.right);
        Spin(ref SpinDown, -transform.right);
        */
    }
}
