﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using System;
using UnityEngine.Events;
using KoboldKare;
using Photon.Pun;
using Photon.Realtime;
using Photon;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using PenetrationTech;
using TMPro;

public class Kobold : MonoBehaviourPun, IGameEventGenericListener<float>, IGrabbable, IAdvancedInteractable, IPunInstantiateMagicCallback, IPunObservable {
    public StatusEffect koboldStatus;
    [System.Serializable]
    public class PenetrableSet {
        public Penetrable penetratable;
        public Rigidbody ragdollAttachBody;
        public bool isFemaleExclusiveAnatomy = false;
    }

    [System.Serializable]
    public class UnityEventFloat : UnityEvent<float> {}

    [System.Serializable]
    public class StatChangeEvent {
        public Stat changedStat;
        public UnityEventFloat onChange;
    }

    public List<StatChangeEvent> statChangedEvents = new List<StatChangeEvent>();

    public delegate void RagdollEventHandler(bool ragdolled);
    public event RagdollEventHandler RagdollEvent;
    public float nextEggTime;


    public Task ragdollTask;

    public StatBlock statblock = new StatBlock();

    public List<PenetrableSet> penetratables = new List<PenetrableSet>();

    public List<Transform> attachPoints = new List<Transform>();

    public AudioClip[] yowls;
    public GenericLODConsumer lodLevel;
    public Transform root;
    public EggSpawner eggSpawner;
    public Animator animator;
    public Rigidbody body;
    [HideInInspector]
    public float uprightTimer = 0;
    private float originalUprightTimer = 0;
    public GameEventFloat MetabolizeEvent;
    public List<GenericInflatable> boobs = new List<GenericInflatable>();
    public List<GenericInflatable> bellies = new List<GenericInflatable>();
    public List<GenericInflatable> subcutaneousStorage = new List<GenericInflatable>();
    public GenericInflatable sizeInflatable;
    public GenericReagentContainer balls;
    public GenericReagentContainer dickContainer;
    public BodyProportion bodyProportion;
    public UnityEvent OnRagdoll;
    public UnityEvent OnStandup;
    public TMPro.TMP_Text chatText;
    public UnityEvent OnEggFormed;
    public UnityEvent OnOrgasm;
    [HideInInspector]
    public List<DickInfo.DickSet> activeDicks = new List<DickInfo.DickSet>();

    public Grabber grabber;
    public AudioSource gurgleSource;
    public List<Renderer> koboldBodyRenderers;
    [HideInInspector]
    public float sex;
    public Transform hip;
    public LayerMask playerHitMask;
    [HideInInspector]
    public float topBottom;
    [HideInInspector]
    public float thickness;
    //[HideInInspector]
    //public float inout;
    private CollisionDetectionMode oldCollisionMode = CollisionDetectionMode.Discrete;
    //public PhotonView photonView;
    public KoboldCharacterController controller;
    public float stimulation = 0f;
    public float stimulationMax = 30f;
    public float stimulationMin = -30f;
    public UnityEvent SpawnEggEvent;
    //public KoboldUseEvent onGrabEvent;
    public float uprightForce = 10f;
    public Animator koboldAnimator;
    public List<Rigidbody> ragdollBodies = new List<Rigidbody>();
    private Rigidbody[] allRigidbodies;
    private float lastPumpTime = 0f;
    private bool grabbed = false;
    private List<Vector3> savedJointAnchors = new List<Vector3>();
    private Vector3 networkedRagdollHipPosition;
    public bool isLoaded = false;
    public float arousal = 0f;
    public Coroutine displayMessageRoutine;
    public bool ragdolled {
        get {
            if (ragdollBodies[0] == null) {
                return false;
            }
            return uprightTimer > 0f;
        }
    }
    public bool notRagdolled {
        get {
            return !ragdolled;
        }
    }
    public void AddStimulation(float s) {
        stimulation += s;
        if (stimulation >= stimulationMax) {
            OnOrgasm.Invoke();
            foreach(var dickSet in activeDicks) {
                dickSet.dick.Cum();
            }
            PumpUpDick(1f);
            stimulation = stimulationMin;
        }
    }
    private void RecursiveSetLayer(Transform t, int fromLayer, int toLayer) {
        for (int i = 0; i < t.childCount; i++) {
            RecursiveSetLayer(t.GetChild(i), fromLayer, toLayer);
        }
        if (t.gameObject.layer == fromLayer && t.GetComponent<Collider>() != null) {
            t.gameObject.layer = toLayer;
        }
    }
    public Vector4 HueBrightnessContrastSaturation {
        set {
            foreach (Renderer r in koboldBodyRenderers) {
                if (r == null) {
                    continue;
                }
                foreach (Material m in r.materials) {
                    m.SetVector("_HueBrightnessContrastSaturation", value);
                }
            }
        }
        get {
            foreach (Renderer r in koboldBodyRenderers) {
                foreach (Material m in r.materials) {
                    return m.GetVector("_HueBrightnessContrastSaturation");
                }
            }
            return new Vector4(0, 0.5f, 0.5f, 0.5f);
        }
    }
    //private bool incremented = false;
    //public AnimatorUpdateMode modeSave;

    public void Awake() {
        statblock.StatusEffectsChangedEvent += OnStatusEffectsChanged;
        allRigidbodies = new Rigidbody[2];
        allRigidbodies[0] = body;
        allRigidbodies[1] = ragdollBodies[0];
        /*foreach(var dickGroup in dickGroups) {
            foreach (var dickSet in dickGroup.dicks) {
                dickSet.dickAttachPosition = dickSet.parent.InverseTransformPoint(dickSet.dick.dickTransform.position);
                dickSet.initialDickForwardHipSpace = dickSet.parent.InverseTransformDirection(dickSet.dick.dickTransform.TransformDirection(dickSet.dick.dickForwardAxis));
                dickSet.initialDickUpHipSpace = dickSet.parent.InverseTransformDirection(dickSet.dick.dickTransform.TransformDirection(dickSet.dick.dickUpAxis));
                dickSet.initialBodyLocalRotation = dickSet.dick.body.transform.localRotation;
                dickSet.initialTransformLocalRotation = dickSet.dick.dickTransform.localRotation;
                //dickSet.joint.axis = Vector3.up;
                //dickSet.joint.secondaryAxis = Vector3.forward;
                //dickSet.dick.body.transform.parent = root;
                dickSet.joint.autoConfigureConnectedAnchor = false;
                if (dickSet.joint is ConfigurableJoint) {
                    Debug.LogWarning("Configurable joints will cause problems! They won't get removed properly due to a unity bug, and using a while loop to remove them will sometimes delete freezes. So just don't use them!");
                    dickSet.savedJoint = new ConfigurableJointData((ConfigurableJoint)dickSet.joint);
                } else if (dickSet.joint is CharacterJoint) {
                    dickSet.savedJoint = new CharacterJointData((CharacterJoint)dickSet.joint);
                }
                koboldBodyRenderers.AddRange(dickSet.dick.deformationTargets);
            }
        }*/
        savedJointAnchors.Clear();
        foreach (Rigidbody ragdollBody in ragdollBodies) {
            if (ragdollBody.GetComponent<CharacterJoint>() == null) {
                continue;
            }
            savedJointAnchors.Add(ragdollBody.GetComponent<CharacterJoint>().connectedAnchor);
            ragdollBody.GetComponent<CharacterJoint>().autoConfigureConnectedAnchor = false;
        }
        //for(int i=1;i<ragdollBodies.Count+1;i++) {
            //allRigidbodies[i] = ragdollBodies[i-1];
        //}
    }
    public void OnCompleteBodyProportion() {
        if (originalUprightTimer > 0f) {
            KnockOver(originalUprightTimer);
        }
    }
    public void Load(ExitGames.Client.Photon.Hashtable s) {
        isLoaded = true;
        if (s.ContainsKey("Sex")) {
            sex = (float)s["Sex"];
            foreach (Renderer r in koboldBodyRenderers) {
                if (r is SkinnedMeshRenderer) {
                    SkinnedMeshRenderer bodyMesh = (SkinnedMeshRenderer)r;
                    for (int o = 0; o < bodyMesh.sharedMesh.blendShapeCount; o++) {
                        if (bodyMesh.sharedMesh.GetBlendShapeName(o) == "MaleEncode") {
                            bodyMesh.SetBlendShapeWeight(o, Mathf.Clamp01(1f - sex * 2f) * 100f);
                        }
                    }
                }
            }
        }
        bool changedBodyShape = false;
        if (s.ContainsKey("TopBottom")) {
            topBottom = (float)s["TopBottom"];
            changedBodyShape = true;
        }
        if (s.ContainsKey("Thickness")) {
            thickness = (float)s["Thickness"];
            changedBodyShape = true;
        }
        if (changedBodyShape) {
            float oldUpright = uprightTimer;
            StandUp();
            bodyProportion.Initialize();
            if (oldUpright > 0f) {
                KnockOver(oldUpright);
            }
        }
        Vector4 hbcs = HueBrightnessContrastSaturation;
        if (s.ContainsKey("Brightness")) {
            hbcs.y = (float)s["Brightness"];
        }
        if (s.ContainsKey("Contrast")) {
            hbcs.z = (float)s["Contrast"];
        }
        if (s.ContainsKey("Saturation")) {
            hbcs.w = (float)s["Saturation"];
        }
        if (s.ContainsKey("Hue")) {
            hbcs.x = (float)s["Hue"];
        }
        HueBrightnessContrastSaturation = hbcs;

        if (s.ContainsKey("KoboldSize")) {
            sizeInflatable.GetContainer().AddMix(ReagentDatabase.GetReagent("GrowthSerum"),(float)s["KoboldSize"] * sizeInflatable.reagentVolumeDivisor, GenericReagentContainer.InjectType.Inject);
        }

        if (s.ContainsKey("BoobSize")) {
            foreach (var boob in boobs) {
                boob.GetContainer().AddMix(ReagentDatabase.GetReagent("Fat"), (float)s["BoobSize"] * boob.reagentVolumeDivisor * 0.7f, GenericReagentContainer.InjectType.Inject);
                boob.GetContainer().AddMix(ReagentDatabase.GetReagent("Milk"), (float)s["BoobSize"] * boob.reagentVolumeDivisor * 0.3f, GenericReagentContainer.InjectType.Inject);
            }
        }
        if (s.ContainsKey("ActiveStatusEffects")) {
            int[] activeEffects = (int[])s["ActiveStatusEffects"];
            bool isSame = activeEffects.Length == statblock.activeEffects.Count;
            for (int i=0;isSame&&i<activeEffects.Length&&i<statblock.activeEffects.Count;i++) {
                if (statblock.activeEffects[i].effect.GetID() != activeEffects[i]) {
                    isSame = false;
                }
            }
            if (!isSame) {
                statblock.Clear();
                foreach (var id in activeEffects) {
                    statblock.AddStatusEffect(StatusEffect.GetFromID(id), StatBlock.StatChangeSource.Network);
                }
            }
        }
    }
    public void OnStatusEffectsChanged(StatBlock block, StatBlock.StatChangeSource source) {
        foreach (var statEvent in statChangedEvents) {
            statEvent.onChange.Invoke(block.GetStat(statEvent.changedStat));
        }
    }
    private void OnSteamAudioChanged(UnityScriptableSettings.ScriptableSetting setting) {
        foreach(AudioSource asource in GetComponentsInChildren<AudioSource>(true)) {
            asource.spatialize = setting.value > 0f;
        }
        //foreach(SteamAudio.SteamAudioSource source in GetComponentsInChildren<SteamAudio.SteamAudioSource>(true)) {
            //source.enabled = setting.value > 0f;
        //}
    }

    void Start() {
        statblock.AddStatusEffect(koboldStatus, StatBlock.StatChangeSource.Misc);
        lastPumpTime = Time.timeSinceLevelLoad;
        MetabolizeEvent.RegisterListener(this);
        foreach (var b in bellies) {
            b.GetContainer().OnChange.AddListener(OnReagentContainerChanged);
        }
        bodyProportion.OnComplete += OnCompleteBodyProportion;
        var steamAudioSetting = UnityScriptableSettings.ScriptableSettingsManager.instance.GetSetting("SteamAudio");
        steamAudioSetting.onValueChange -= OnSteamAudioChanged;
        steamAudioSetting.onValueChange += OnSteamAudioChanged;
        OnSteamAudioChanged(steamAudioSetting);
    }
    private void OnDestroy() {
        bodyProportion.OnComplete -= OnCompleteBodyProportion;
        statblock.StatusEffectsChangedEvent -= OnStatusEffectsChanged;
        MetabolizeEvent.UnregisterListener(this);
        foreach (var b in bellies) {
            b.GetContainer().OnChange.RemoveListener(OnReagentContainerChanged);
        }
        if (photonView.IsMine) {
            PhotonNetwork.CleanRpcBufferIfMine(photonView);
            PhotonNetwork.OpCleanRpcBuffer(photonView);
        }
        var steamAudioSetting = UnityScriptableSettings.ScriptableSettingsManager.instance.GetSetting("SteamAudio");
        steamAudioSetting.onValueChange -= OnSteamAudioChanged;
    }
    public bool OnGrab(Kobold kobold) {
        //onGrabEvent.Invoke(kobold, transform.position);
        grabbed = true;
        //KnockOver(999999f);
        //modeSave = animator.updateMode;
        //animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
        animator.SetBool("Carried", true);
        //pickedUp = 1;
        //transSpeed = 5.0f;
        photonView.RPC("OnEndStation", RpcTarget.AllBuffered);
        controller.frictionMultiplier = 0.1f;
        controller.enabled = false;
        return true;
    }
    [PunRPC]
    public void RPCKnockOver() {
        KnockOver(9999f);
    }
    [PunRPC]
    public void RPCStandUp() {
        StandUp();
    }
    public IEnumerator KnockOverRoutine() {
        // If we need jigglebones disabled, it takes TWO frames for it to take effect! So... here we wait!
        // Otherwise jigglebones will move rigidbodies and fuck stuff up...
        OnRagdoll.Invoke();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        //RecursiveSetLayer(transform, LayerMask.NameToLayer("Hitbox"), LayerMask.NameToLayer("PlayerHitbox"));
        if (koboldAnimator == null) {
            // Oh dear, guess we got removed already. Just quit out.
            yield return null;
        }
        koboldAnimator.enabled = false;
        controller.enabled = false;
        //foreach(var penSet in penetratables) {
            //penSet.penetratable.SwitchBody(penSet.ragdollAttachBody);
        //}
        foreach (Rigidbody b in ragdollBodies) {
            b.velocity = body.velocity;
            b.isKinematic = false;
            //b.interpolation = RigidbodyInterpolation.Interpolate;
            if (lodLevel.isClose) {
                b.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
        oldCollisionMode = body.collisionDetectionMode;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        body.GetComponent<Collider>().enabled = false;
        foreach(JigglePhysics.JiggleBone j in GetComponentsInChildren<JigglePhysics.JiggleBone>()) {
            j.updateMode = JigglePhysics.JiggleBone.UpdateType.FixedUpdate;
        }
        foreach(JigglePhysics.JiggleSoftbody s in GetComponentsInChildren<JigglePhysics.JiggleSoftbody>()) {
            s.updateMode = JigglePhysics.JiggleSoftbody.UpdateType.FixedUpdate;
        }

        // We need to know the final result of our ragdoll before we update the anchors.
        bodyProportion.ScaleSkeleton();
        int i = 0;
        foreach (Rigidbody ragdollBody in ragdollBodies) {
            CharacterJoint j = ragdollBody.GetComponent<CharacterJoint>();
            if (j == null) {
                continue;
            }
            //j.anchor = Vector3.zero;
            j.connectedAnchor = savedJointAnchors[i++];
        }
        // FIXME: For somereason, after kobolds get grabbed and tossed off of a live physics animation-- the body doesn't actually stay kinematic. I'm assuming due to one of the ragdoll events.
        // Adding this extra set fixes it for somereason, though this is not a proper fix.
        body.isKinematic = true;
        RagdollEvent?.Invoke(true);
    }
    public void KnockOver(float duration = 3f) {
        //uprightTimer = Mathf.Max(Mathf.Max(0 + duration, uprightTimer + duration), 1f);
        if (bodyProportion.running) {
            originalUprightTimer = Mathf.Max(duration, originalUprightTimer);
            return;
        }
        uprightTimer = duration;
        if (body.isKinematic) {
            return;
        }
        if (ragdollTask != null && ragdollTask.Running) {
            ragdollTask.Stop();
        }
        ragdollTask = new Task(KnockOverRoutine());
    }
    // This was a huuuUUGE pain, but for somereason joints forget their initial orientation if you switch bodies.
    // I tried a billion different things to try to reset the initial orientation, this was the only thing that worked for me!
    public void StandUp() {
        originalUprightTimer = 0f;
        uprightTimer = 0f;
        if ((!body.isKinematic && ragdollBodies[0].isKinematic) || koboldAnimator.enabled) {
            return;
        }
        //foreach(var penSet in penetratables) {
            //penSet.penetratable.SwitchBody(body);
        //}
        Vector3 diff = hip.position - body.transform.position;
        body.transform.position += diff;
        hip.position -= diff;
        body.transform.position += Vector3.up*0.5f;
        body.isKinematic = false;
        body.GetComponent<Collider>().enabled = true;
        body.collisionDetectionMode = oldCollisionMode;
        Vector3 averageVel = Vector3.zero;
        foreach (Rigidbody b in ragdollBodies) {
            averageVel += b.velocity;
        }
        averageVel /= ragdollBodies.Count;
        body.velocity = averageVel;
        controller.enabled = true;
        //RecursiveSetLayer(transform, LayerMask.NameToLayer("PlayerHitbox"), LayerMask.NameToLayer("Hitbox"));
        foreach (Rigidbody b in ragdollBodies) {
            //b.interpolation = RigidbodyInterpolation.None;
            b.collisionDetectionMode = CollisionDetectionMode.Discrete;
            b.isKinematic = true;
        }
        foreach(JigglePhysics.JiggleBone j in GetComponentsInChildren<JigglePhysics.JiggleBone>()) {
            j.updateMode = JigglePhysics.JiggleBone.UpdateType.LateUpdate;
        }
        foreach(JigglePhysics.JiggleSoftbody s in GetComponentsInChildren<JigglePhysics.JiggleSoftbody>()) {
            s.updateMode = JigglePhysics.JiggleSoftbody.UpdateType.LateUpdate;
        }
        //foreach(var penSet in penetratables) {
            //penSet.penetratable.SwitchBody(body);
        //}
        koboldAnimator.enabled = true;
        controller.enabled = true;
        OnStandup.Invoke();
        RagdollEvent?.Invoke(false);
    }
    public void PumpUpDick(float amount) {
        if (amount > 0 ) {
            lastPumpTime = Time.timeSinceLevelLoad;
        }
        arousal += amount;
        arousal = Mathf.Clamp01(arousal);
    }
    public void OnRelease(Kobold kobold) {
        //animator.updateMode = modeSave;
        animator.SetBool("Carried", false);
        if (body.velocity.magnitude > 3f) {
            KnockOver(3f);
        } else {
            foreach(Collider c in Physics.OverlapSphere(transform.position, 1f, playerHitMask, QueryTriggerInteraction.Collide)) {
                Kobold k = c.GetComponentInParent<Kobold>();
                if (k!=this && k!=kobold) {
                    k.GetComponentInChildren<GenericUsable>().Use(this);
                }
            }
            controller.enabled = true;
        }
        controller.frictionMultiplier = 1f;
        grabbed = false;
        //pickedUp = 0;
        //transSpeed = 1f;
    }
    private void Update() {
        dickContainer.OverrideReagent(ReagentDatabase.GetReagent("Blood"), arousal*0.92f + (0.08f * Mathf.Clamp01(Mathf.Sin(Time.time*2f)))*arousal);
    }
    private void FixedUpdate() {
        if (!grabbed) {
            uprightForce = Mathf.MoveTowards(uprightForce, 10f, Time.deltaTime * 10f);
        } else {
            uprightForce = Mathf.MoveTowards(uprightForce, 0f, Time.deltaTime * 2f);
            PumpUpDick(Time.deltaTime*0.1f);
        }
        if (uprightTimer > 0f) {
            uprightTimer -= Time.fixedDeltaTime;
            if (uprightTimer < 0f) {
                StandUp();
            }
        }
        if (uprightTimer <= 0) {
            body.angularVelocity -= body.angularVelocity*0.2f;
            float penetrationAmount = 0f;
            //foreach(var penSet in penetratables) {
                //if (penSet.penetratable.isActiveAndEnabled) {
                    //penetrationAmount += penSet.penetratable.realGirth;
                //}
            //}
            float deflectionForgivenessDegrees = 5f;
            Vector3 cross = Vector3.Cross(body.transform.up, Vector3.up);
            float angleDiff = Mathf.Max(Vector3.Angle(body.transform.up, Vector3.up) - deflectionForgivenessDegrees, 0f);
            body.AddTorque(cross*angleDiff, ForceMode.Acceleration);
            //body.angularVelocity += new Vector3(rot.x, rot.y, rot.z).MagnitudeClamped(0f, 1f) * Mathf.Max((1f - penetrationAmount * 2f) * uprightForce, 0f);
        }
        //dick.dickTransform.GetComponent<CharacterJoint>().connectedAnchor = body.transform.InverseTransformPoint(hip.TransformPoint(dickAttachPosition));
        //ConfigurableJoint dickJoint = dick.body.GetComponent<ConfigurableJoint>();
        /*if (activeDicks != null) {
            foreach (var dickSet in activeDicks) {
                if (dickSet.joint == null) {
                    continue;
                }
                Vector3 dickForward = dickSet.dick.dickTransform.TransformDirection(dickSet.dick.dickForwardAxis);
                Vector3 dickUp = dickSet.dick.dickTransform.TransformDirection(dickSet.dick.dickUpAxis);
                Vector3 dickRight = Vector3.Cross(dickUp, dickForward);
                Vector3 hipUp = dickSet.parent.TransformDirection(dickSet.initialDickUpHipSpace);
                Vector3 hipForward = dickSet.parent.TransformDirection(dickSet.initialDickForwardHipSpace);
                Vector3 hipRight = Vector3.Cross(hipUp, hipForward);

                //FIXME
                // Force the dick to be oriented correctly.
                //dickSet.dick.dickTransform.rotation = Quaternion.FromToRotation(dickUp, Vector3.ProjectOnPlane(hipUp, dickForward).normalized) * dickSet.dick.dickTransform.rotation;
                //dickSet.joint.autoConfigureConnectedAnchor = false; //dickJoint.axis = body.transform.right;
                //if (dickSet.joint.connectedBody == body || dickSet.joint.connectedBody.isKinematic) {
                if (uprightTimer <= 0) { // If we're not ragdolled
                    dickSet.dick.body.interpolation = body.interpolation;
                    dickSet.joint.connectedAnchor = dickSet.joint.connectedBody.transform.InverseTransformPoint(dickSet.parent.TransformPoint(dickSet.dickAttachPosition));
                } else {
                    dickSet.dick.body.interpolation = dickSet.joint.connectedBody.interpolation;
                }
                //dick.dickTransform.position = hip.TransformPoint(dickAttachPosition);
                if (dickSet.container.contents.ContainsKey(ReagentData.ID.Blood)) {
                    Quaternion ro = Quaternion.FromToRotation(dickForward, hipForward);
                    dickSet.dick.body.angularVelocity *= 0.8f;
                    dickSet.dick.body.AddTorque(new Vector3(ro.x, ro.y, ro.z) * 30f * dickSet.container.contents[ReagentData.ID.Blood].volume);
                }
                //dickSet.joint.axis = dickSet.dick.dickTransform.TransformDirection(dickSet.dick.dickUpAxis);
                //dickSet.dick.body.position = dickSet.parent.TransformPoint(dickSet.dickAttachPosition);
                //dickSet.dick.body.MovePosition(dickSet.parent.TransformPoint(dickSet.dickAttachPosition));
            }
        }*/
        if (Time.timeSinceLevelLoad-lastPumpTime > 10f) {
            PumpUpDick(-Time.deltaTime * 0.01f);
        }
        if (!photonView.IsMine) {
            Vector3 dir = networkedRagdollHipPosition - hip.position;
            hip.GetComponent<Rigidbody>().AddForce(dir, ForceMode.VelocityChange);
        }
    }

    [PunRPC]
    public void RPCGrab(int photonID) {
        PhotonView view = PhotonView.Find(photonID);
        if (view == null) {
            return;
        }
        IGrabbable g = view.GetComponentInChildren<IGrabbable>();
        if (g != null) {
            GetComponentInChildren<Grabber>().TryGrab(g);
        }
    }
    [PunRPC]
    public void RPCDrop() {
        GetComponentInChildren<Grabber>().TryDrop();
    }

    [PunRPC]
    public void RPCPrecisionGrab(int grabberViewID, int colliderID, Vector3 lHitPoint) {
        PhotonView view = PhotonView.Find(grabberViewID);
        if (view != null) {
            Collider[] colliders = view.GetComponentsInChildren<Collider>();
            if (colliderID >= 0 && colliderID < colliders.Length) {
                GetComponentInChildren<PrecisionGrabber>().Grab(colliders[colliderID], lHitPoint, Vector3.up);
            }
        }
    }

    [PunRPC]
    public void RPCFreeze(int grabberViewID, int colliderID, Vector3 localPosition, Vector3 worldPosition, Quaternion rotation, bool affRotation) {
        PhotonView view = PhotonView.Find(grabberViewID);
        if (view != null) {
            Collider[] colliders = view.GetComponentsInChildren<Collider>();
            if (colliderID >= 0 && colliderID < colliders.Length) {
                GetComponentInChildren<PrecisionGrabber>().Freeze(colliders[colliderID], localPosition, worldPosition, rotation, affRotation);
            }
        }
    }

    [PunRPC]
    public void RPCUnfreezeAll() {
        GetComponentInChildren<PrecisionGrabber>().Unfreeze(false);
    }
    public void SendChat(string message) {
        photonView.RPC("RPCSendChat", RpcTarget.All, new object[]{message});
    }
    [PunRPC]
    public void RPCSendChat(string message) {
        GameManager.instance.SpawnAudioClipInWorld(yowls[UnityEngine.Random.Range(0,yowls.Length)], transform.position);
        if (displayMessageRoutine != null) {
            StopCoroutine(displayMessageRoutine);
        }
        displayMessageRoutine = StartCoroutine(DisplayMessage(message,5f));
    }
    IEnumerator DisplayMessage(string message, float duration) {
        chatText.text = message;
        chatText.alpha = 1f;
        yield return new WaitForSeconds(duration);
        float endTime = Time.time + 1f;
        while(Time.time < endTime) {
            chatText.alpha = endTime-Time.time;
            yield return null;
        }
        chatText.alpha = 0f;
    }
    public void InteractTo(Vector3 worldPosition, Quaternion worldRotation) {
        PumpUpDick(Time.deltaTime * 0.02f);
        uprightForce = Mathf.MoveTowards(uprightForce, 1f, Time.deltaTime*10f);
    }
    public void OnInteract(Kobold k) {
        grabbed = true;
        if (k != this) {
            controller.frictionMultiplier = 0.1f;
        }
    }
    public bool IsPenetrating(Kobold k) {
        foreach(var penetratable in k.penetratables) {
            foreach(var dickset in activeDicks) {
                if (penetratable.penetratable.ContainsPenetrator(dickset.dick)) {
                    return true;
                }
            }
        }
        return false;
    }
    public void OnEndInteract(Kobold k) {
        grabbed = false;
        controller.frictionMultiplier = 1f;
        //uprightForce = 40f;
    }
    public bool ShowHand() { return true; }
    public bool PhysicsGrabbable() { return true; }

    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        if (info.photonView.InstantiationData != null && info.photonView.InstantiationData[0] is Hashtable) {
            //Debug.Log(info.photonView.InstantiationData[0]);
            Load((Hashtable)(info.photonView.InstantiationData[0]));
        }
    }

    public Rigidbody[] GetRigidBodies()
    {
        return allRigidbodies;
    }

    public Renderer[] GetRenderers() {
        return new Renderer[]{};
    }

    public Transform GrabTransform(Rigidbody b) {
        if (!body.isKinematic) {
            return hip;
        } else {
            return b.transform;
        }
    }

    public GrabbableType GetGrabbableType() {
        return GrabbableType.Kobold;
    }

    public void OnEventRaised(GameEventGeneric<float> e, float f) {
        stimulation = Mathf.MoveTowards(stimulation, 0f, f*0.1f);
        foreach (var belly in bellies) {
            ReagentContents vol = belly.GetContainer().Metabolize(f);
            belly.GetContainer().AddMix(ReagentDatabase.GetReagent("Egg"), vol.GetVolumeOf(ReagentDatabase.GetReagent("Cum"))*3f, GenericReagentContainer.InjectType.Metabolize);
            float melonJuiceVolume = vol.GetVolumeOf(ReagentDatabase.GetReagent("MelonJuice"));
            foreach (var boob in boobs) {
                boob.GetContainer().AddMix(ReagentDatabase.GetReagent("Fat"), melonJuiceVolume*4f / boobs.Count, GenericReagentContainer.InjectType.Metabolize);
                boob.GetContainer().AddMix(ReagentDatabase.GetReagent("Milk"), melonJuiceVolume*4f*0.33f / boobs.Count, GenericReagentContainer.InjectType.Metabolize);
            }
            float eggplantJuiceVolume = vol.GetVolumeOf(ReagentDatabase.GetReagent("EggplantJuice"));
            dickContainer.AddMix(ReagentDatabase.GetReagent("Fat"), eggplantJuiceVolume*2f, GenericReagentContainer.InjectType.Metabolize);
            float growthSerumVolume = vol.GetVolumeOf(ReagentDatabase.GetReagent("GrowthSerum"));
            foreach (var ss in subcutaneousStorage) {
                ss.GetContainer().AddMix(ReagentDatabase.GetReagent("GrowthSerum"), growthSerumVolume/subcutaneousStorage.Count, GenericReagentContainer.InjectType.Metabolize);
            }
            float milkShakeVolume = vol.GetVolumeOf(ReagentDatabase.GetReagent("MilkShake"));
            foreach (var ss in subcutaneousStorage) {
                ss.GetContainer().AddMix(ReagentDatabase.GetReagent("Fat"), milkShakeVolume*2f/subcutaneousStorage.Count, GenericReagentContainer.InjectType.Metabolize);
            }
            float pineappleJuiceVolume = vol.GetVolumeOf(ReagentDatabase.GetReagent("PineappleJuice"));
            balls.AddMix(ReagentDatabase.GetReagent("Fat"), pineappleJuiceVolume*3f, GenericReagentContainer.InjectType.Metabolize);
            balls.AddMix(ReagentDatabase.GetReagent("Cum"), pineappleJuiceVolume*1f, GenericReagentContainer.InjectType.Metabolize);

            if (Time.timeSinceLevelLoad > nextEggTime) {
                float currentEggVolume = belly.GetContainer().GetVolumeOf(ReagentDatabase.GetReagent("Egg"));
                if (currentEggVolume > 8f) {
                    OnEggFormed.Invoke();
                    nextEggTime = Time.timeSinceLevelLoad + 30f;
                    bool spawnedEgg = false;
                    foreach(var penetratableSet in penetratables) {
                        if (penetratableSet.isFemaleExclusiveAnatomy && penetratableSet.penetratable.isActiveAndEnabled) {
                            eggSpawner.targetPenetrable = penetratableSet.penetratable;
                            eggSpawner.spawnAlongLength = 1f;
                            eggSpawner.SpawnEgg();
                            spawnedEgg = true;
                            break;
                        }
                    }
                    if (!spawnedEgg) {
                        foreach(var penetratableSet in penetratables) {
                            if (penetratableSet.penetratable.isActiveAndEnabled) {
                                eggSpawner.targetPenetrable = penetratableSet.penetratable;
                                eggSpawner.spawnAlongLength = 0.5f;
                                eggSpawner.SpawnEgg();
                                spawnedEgg = true;
                                break;
                            } 
                        }
                    }
                    if (spawnedEgg) {
                        belly.GetContainer().OverrideReagent(ReagentDatabase.GetReagent("Egg"), currentEggVolume-8f);
                    }
                }
            }
        }
    }

    IEnumerator WaitAndThenStopGargling(float time) {
        yield return new WaitForSeconds(time);
        gurgleSource.Pause();
    }
    public void OnReagentContainerChanged(GenericReagentContainer.InjectType injectType) {
        if (injectType != GenericReagentContainer.InjectType.Spray) {
            return;
        }
        koboldAnimator.SetTrigger("Quaff");
        if (!gurgleSource.isPlaying) {
            gurgleSource.Play();
            gurgleSource.pitch = 0.9f + sex*0.4f;
            StartCoroutine(WaitAndThenStopGargling(0.25f));
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(ragdolled);
            stream.SendNext(hip.position);
        } else {
            bool ragged = (bool)stream.ReceiveNext();
            if (!ragdolled && ragged && !bodyProportion.running) {
                KnockOver(99999f);
            }
            if (ragdolled && !ragged) {
                StandUp();
            }
            networkedRagdollHipPosition = (Vector3)stream.ReceiveNext();
        }
    }

    public void OnThrow(Kobold kobold) {
    }
}
