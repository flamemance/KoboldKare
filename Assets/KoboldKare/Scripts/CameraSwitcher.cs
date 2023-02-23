﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour {
    public GameObject FPSCanvas;
    private OrbitCameraBasicConfiguration firstpersonConfiguration;
    private OrbitCameraCharacterConfiguration thirdpersonConfiguration;
    private OrbitCameraBasicConfiguration thirdpersonRagdollConfiguration;
    private OrbitCameraBasicConfiguration freecamConfiguration;
    private SimpleCameraController freeCamController;
    private Ragdoller ragdoller;
    
    public Transform uiSlider;
    private OrbitCameraConfiguration lastConfig;

    private bool initialized = false;
    [SerializeField]
    private PlayerPossession possession;
    private KoboldCharacterController controller;
    public enum CameraMode {
        FirstPerson = 0,
        ThirdPerson,
        FreeCam,
        FreeCamLocked,
    }
    private CameraMode? mode = null;

    void OnEnable() {
        controller = GetComponentInParent<KoboldCharacterController>();
        ragdoller = GetComponentInParent<Ragdoller>();
        ragdoller.RagdollEvent += OnRagdollEvent;
        if (firstpersonConfiguration == null) {
            var animator = GetComponentInParent<CharacterDescriptor>().GetDisplayAnimator();
            var fpsPivotObj = new GameObject("FPSPivot", typeof(OrbitCameraFPSHeadPivot));
            var fpsPivot = fpsPivotObj.GetComponent<OrbitCameraFPSHeadPivot>();
            fpsPivot.Initialize(animator, HumanBodyBones.Head, 5f);
            firstpersonConfiguration = new OrbitCameraBasicConfiguration();
            firstpersonConfiguration.SetPivot(fpsPivot.GetComponent<OrbitCameraLerpTrackPivot>());
            firstpersonConfiguration.SetCullingMask(~LayerMask.GetMask("MirrorReflection"));
            var freeCamObj = new GameObject("FreeCamPivot", typeof(SimpleCameraController));
            freeCamObj.transform.SetParent(GetComponentInParent<CharacterDescriptor>().transform);
            freeCamObj.transform.localPosition = Vector3.zero;
            freeCamController = freeCamObj.GetComponent<SimpleCameraController>();
            freeCamController.SetControls(GetComponent<PlayerInput>());
            freecamConfiguration = new OrbitCameraBasicConfiguration();
            freecamConfiguration.SetPivot(freeCamController);
            freecamConfiguration.SetCullingMask(~LayerMask.GetMask("LocalPlayer"));
            OrbitCameraLerpTrackBasicPivot shoulderPivot = new GameObject("ShoulderCamPivot", typeof(OrbitCameraLerpTrackBasicPivot)).GetComponent<OrbitCameraLerpTrackBasicPivot>();
            shoulderPivot.SetInfo(new Vector2(0.33f, 0.33f), 1f);
            shoulderPivot.Initialize(animator, HumanBodyBones.Head, 1f);
            OrbitCameraLerpTrackBasicPivot buttPivot = new GameObject("ButtCamPivot", typeof(OrbitCameraLerpTrackBasicPivot)).GetComponent<OrbitCameraLerpTrackBasicPivot>();
            buttPivot.SetInfo(new Vector2(0.33f, 0.1f), 1f);
            buttPivot.Initialize(animator, HumanBodyBones.Hips, 1f);
            thirdpersonConfiguration = new OrbitCameraCharacterConfiguration();
            thirdpersonConfiguration.SetPivots(shoulderPivot, buttPivot);

            var basicRagdollPivot = animator.GetBoneTransform(HumanBodyBones.Spine).gameObject.AddComponent<OrbitCameraPivotBasic>();
            basicRagdollPivot.SetInfo(new Vector2(0.5f,0.33f), 1f);
            thirdpersonRagdollConfiguration = new OrbitCameraBasicConfiguration();
            thirdpersonRagdollConfiguration.SetPivot(basicRagdollPivot);
            thirdpersonRagdollConfiguration.SetCullingMask(~LayerMask.GetMask("LocalPlayer"));
        }
        initialized = false;
        OrbitCamera.AddConfiguration(firstpersonConfiguration);
        lastConfig = firstpersonConfiguration;
        if (!FPSCanvas.activeInHierarchy) {
            FPSCanvas.SetActive(true);
        }
        mode = CameraMode.FirstPerson;
    }

    void OnRagdollEvent(bool ragdolled) {
        if (mode == CameraMode.ThirdPerson) {
            if (ragdolled) {
                OrbitCamera.ReplaceConfiguration(lastConfig, thirdpersonRagdollConfiguration);
            } else {
                OrbitCamera.ReplaceConfiguration(lastConfig, thirdpersonRagdollConfiguration);
            }
        }
    }

    void OnDisable() {
        OrbitCamera.RemoveConfiguration(lastConfig);
        ragdoller.RagdollEvent -= OnRagdollEvent;
    }

    void Update() {
        uiSlider.transform.localPosition = Vector3.Lerp(uiSlider.transform.localPosition, -Vector3.right * (30f * ((int)mode+0.5f)), Time.deltaTime*2f);
    }

    public void OnSwitchCamera() {
        if (mode == null) {
            return;
        }

        int index = ((int)mode.Value + 1) % 4;
        SwitchCamera((CameraMode)index);
    }

    public void OnFirstPerson() {
        SwitchCamera(CameraMode.FirstPerson);
    }
    public void OnThirdPerson() {
        SwitchCamera(CameraMode.ThirdPerson);
    }
    public void OnFreeCamera() {
        SwitchCamera(CameraMode.FreeCam);
    }
    public void OnLockedCamera() {
        SwitchCamera(CameraMode.FreeCamLocked);
    }

    public void SwitchCamera(CameraMode cameraMode) {
        if (Cursor.lockState != CursorLockMode.Locked && initialized) {
            return;
        }

        if (mode == cameraMode) {
            return;
        }

        initialized = true;
        mode = cameraMode;
        possession.enabled = true;
        freeCamController.enabled = false;
        switch (mode) {
            case CameraMode.FirstPerson:
                OrbitCamera.ReplaceConfiguration(lastConfig, firstpersonConfiguration);
                lastConfig = firstpersonConfiguration;
                
                if (!FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(true);
                }
                break;
            case CameraMode.ThirdPerson:
                if (ragdoller.ragdolled) {
                    OrbitCamera.ReplaceConfiguration(lastConfig, thirdpersonRagdollConfiguration);
                    lastConfig = thirdpersonRagdollConfiguration;
                } else {
                    OrbitCamera.ReplaceConfiguration(lastConfig, thirdpersonConfiguration);
                    lastConfig = thirdpersonConfiguration;
                }

                if (!FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(true);
                }
                break;
            case CameraMode.FreeCam:
                OrbitCamera.ReplaceConfiguration(lastConfig, freecamConfiguration);
                freeCamController.SetRotationOffset(Quaternion.identity);
                lastConfig = freecamConfiguration;
                freeCamController.enabled = true;
                possession.enabled = false;
                controller.inputDir = Vector3.zero;
                controller.inputJump = false;
                if (FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(false);
                }
                break;
            case CameraMode.FreeCamLocked:
                OrbitCamera.ReplaceConfiguration(lastConfig, freecamConfiguration);
                freeCamController.SetRotationOffset(Quaternion.Inverse(freeCamController.transform.rotation));
                lastConfig = freecamConfiguration;
                freeCamController.enabled = false;
                possession.enabled = true;
                if (!FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(true);
                }
                break;
        }

    }
}
