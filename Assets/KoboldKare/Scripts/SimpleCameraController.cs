﻿using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityScriptableSettings;

public class SimpleCameraController : OrbitCameraPivotBase {
    private PlayerInput controls;
    private SettingFloat fov;
    private Quaternion offset;
    class CameraState {
        public float x;
        public float y;
        public float z;

        public void SetFromTransform(Transform t) {
            x = t.position.x;
            y = t.position.y;
            z = t.position.z;
        }

        public void Translate(Vector3 translation) {
            Vector3 rotatedTranslation = OrbitCamera.GetPlayerIntendedRotation() * translation;
            x += rotatedTranslation.x;
            y += rotatedTranslation.y;
            z += rotatedTranslation.z;
        }

        public void LerpTowards(CameraState target, float positionLerpPct) {
            x = Mathf.Lerp(x, target.x, positionLerpPct);
            y = Mathf.Lerp(y, target.y, positionLerpPct);
            z = Mathf.Lerp(z, target.z, positionLerpPct);
        }

        public void UpdateTransform(Transform t) {
            t.position = new Vector3(x, y, z);
        }
    }
    
    CameraState m_TargetCameraState = new CameraState();
    CameraState m_InterpolatingCameraState = new CameraState();

    [Header("Movement Settings")]
    [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
    public float boost = 3.5f;

    [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
    public float positionLerpTime = 0.2f;

    [Header("Rotation Settings")]
    [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
    public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

    [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
    public float rotationLerpTime = 0.01f;

    [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
    public bool invertY = false;

    public UnityScriptableSettings.SettingFloat mouseSensitivity;

    void OnEnable() {
        m_TargetCameraState.SetFromTransform(transform);
        m_InterpolatingCameraState.SetFromTransform(transform);
    }

    public void SetControls(PlayerInput controls) {
        this.controls = controls;
    }

    public void SetRotationOffset(Quaternion offset) {
        this.offset = offset;
    }

    public override Quaternion GetRotation(Quaternion camRotation) {
        return offset*base.GetRotation(camRotation);
    }

    private void Start() {
        fov = SettingsManager.GetSetting("CameraFOV") as SettingFloat;
        Debug.Log(fov);
    }

    Vector3 GetInputTranslationDirection()
    {
        Vector3 direction = new Vector3();
        Vector2 moveInput = controls.actions["Move"].ReadValue<Vector2>();
        direction += new Vector3(moveInput.x,0f,moveInput.y);
        if (controls.actions["Jump"].ReadValue<float>()>0.5f) {
            direction += Vector3.up;
        }
        if (controls.actions["Crouch"].ReadValue<float>() > 0.5f) {
            direction += Vector3.down;
        }
        return direction;
    }
    
    void LateUpdate() {
        var translation = GetInputTranslationDirection() * Time.deltaTime;
        // Speed up movement when shift key held
        if (controls.actions["Walk"].ReadValue<float>()>0.5f) {
            translation /= 10.0f;
        }
        // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
        boost += controls.actions["Grab Push and Pull"].ReadValue<float>() * 0.002f;
        //boost += Input.mouseScrollDelta.y * 0.2f;
        translation *= Mathf.Pow(2.0f, boost);

        m_TargetCameraState.Translate(translation);

        // Framerate-independent interpolation
        // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
        var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
        m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct);

        m_InterpolatingCameraState.UpdateTransform(transform);
    }

    public override float GetDistanceFromPivot(Quaternion camRotation) {
        return 0f;
    }

    public override float GetFOV(Quaternion camRotation) {
        return fov.GetValue();
    }
}
