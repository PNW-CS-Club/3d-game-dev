//Original Code Author: Aedan Graves
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
#endif

// TODO
// Better implement the new input system.
// create compatibility layers for Unity 2017 and 2018
// better implement animation calls(?)
// more camera animations
namespace SUPERCharacter{
[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))][AddComponentMenu("SUPER Character/SUPER Character Controller")]
public class SUPERCharacterAIO : NetworkBehaviour{
    #region Variables

    public bool controllerPaused = false;

    #region Camera Settings
    
    // *** Public Variables ***
    
    //Both
    [Header("Camera Settings")]
    public Camera playerCamera;
    public GameObject cameraHolder;
    public bool  enableCameraControl = true, lockAndHideMouse = true, autoGenerateCrosshair = true, showCrosshairIn3rdPerson = false, drawPrimitiveUI = false;
    public Sprite crosshairSprite;
    public PerspectiveModes cameraPerspective = PerspectiveModes._1stPerson;
    //use mouse wheel to switch modes. (too close will set it to fps mode and attempting to zoom out from fps will switch to tps mode)
    public bool automaticallySwitchPerspective = true;
    #if ENABLE_INPUT_SYSTEM
    public Key perspectiveSwitchingKey = Key.Q;
    public Key toggleMouseLockKey = Key.Escape;
    #else
    public KeyCode perspectiveSwitchingKey_L = KeyCode.None;
    public KeyCode toggleMouseLockKey_L = KeyCode.Escape;
    #endif

    public MouseInputInversionModes mouseInputInversion;
    public float sensitivity = 8;
    public float rotationWeight = 4;
    public float verticalRotationRange = 170.0f;
    public float standingEyeHeight = 0.8f;
    public float crouchingEyeHeight = 0.25f;

    //First person
    public ViewInputModes viewInputMethods;
    public float fovKickAmount = 10; 
    public float fovSensitivityMultiplier = 0.74f;

    //Third Person
    public bool rotateCharacterToCameraForward = false;
    public float maxCameraDistance = 8;
    public LayerMask cameraObstructionIgnore = -1;
    public float cameraZoomSensitivity = 5; 
    public float bodyCatchupSpeed = 2.5f;
    public float inputResponseFiltering = 2.5f;



    // *** Internal Variables ***
    
    //Both
    Vector2 MouseXY;
    Vector2 viewRotVelRef;
    bool isInFirstPerson, isInThirdPerson, perspecTog;
    bool setInitialRot = true;
    Vector3 initialRot;
    Image crosshairImg;
    Image stamMeter, stamMeterBG;
    Image statsPanel, statsPanelBG;
    Image HealthMeter, HydrationMeter, HungerMeter;
    Vector2 normalMeterSizeDelta = new Vector2(175,12), normalStamMeterSizeDelta = new Vector2(330,5);
    float internalEyeHeight;

    //First Person
    float initialCameraFOV, FOVKickVelRef, currentFOVMod;

    //Third Person
    float mouseScrollWheel, maxCameraDistInternal, currentCameraZ, cameraZRef;
    Vector3 headPos, headRot, currentCameraPos, cameraPosVelRef;
    Quaternion quatHeadRot;
    Ray cameraObstCheck;
    RaycastHit cameraObstResult;
    [Space(20)]
    #endregion

    #region Movement
    
    
    // *** Public Variables ***
    [Header("Movement Settings")]
    public bool enableMovementControl = true;

    //Walking/Sprinting/Crouching
    [Range(1.0f,650.0f)]public float walkingSpeed = 140, sprintingSpeed = 260, crouchingSpeed = 45;
    [Range(1.0f,400.0f)] public float decelerationSpeed=240;
    #if ENABLE_INPUT_SYSTEM
    public Key sprintKey = Key.LeftShift, crouchKey = Key.LeftCtrl, slideKey = Key.V;
    #else
    public KeyCode sprintKey_L = KeyCode.LeftShift, crouchKey_L = KeyCode.LeftControl, slideKey_L = KeyCode.V;
    #endif
    public bool canSprint=true, isSprinting, toggleSprint, sprintOverride, canCrouch=true, isCrouching, toggleCrouch, crouchOverride, isIdle;
    public Stances currentStance = Stances.Standing;
    public float stanceTransitionSpeed = 5.0f, crouchingHeight = 0.80f;
    public GroundSpeedProfiles currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
    public LayerMask whatIsGround =-1;

    //Slope affectors
    public float hardSlopeLimit = 70, slopeInfluenceOnSpeed = 1, maxStairRise = 0.25f, stepUpSpeed=0.2f;

    //Jumping
    public bool canJump=true, holdJump=false, jumpEnhancements=true, jumped=false;
    #if ENABLE_INPUT_SYSTEM
        public Key jumpKey = Key.Space;
    #else
        public KeyCode jumpKey_L = KeyCode.Space;
    #endif
    [Range(1.0f,650.0f)] public float jumpPower = 40;
    [Range(0.0f,1.0f)] public float airControlFactor = 1;
    public float descentMultiplier = 2.5f;
    public float tapJumpMultiplier = 2.1f;
    float jumpBlankingPeriod;

    //Sliding
    public bool isSliding, canSlide = true;
    public float slidingDeceleration = 150.0f, slidingTransitionSpeed=4, maxFlatSlideDistance =10;
    

    
    // *** Internal Variables ***

    //Walking/Sprinting/Crouching
    public GroundInfo currentGroundInfo;
    float standingHeight;
    float currentGroundSpeed;
    Vector3 InputDir;
    float HeadRotDirForInput;
    Vector2 MovInput;
    Vector2 MovInput_Smoothed;
    Vector2 _2DVelocity;
    float _2DVelocityMag, speedToVelocityRatio;
    PhysicsMaterial _ZeroFriction, _MaxFriction;
    CapsuleCollider capsule;
    Rigidbody p_Rigidbody;
    bool crouchInput_Momentary, crouchInput_FrameOf, sprintInput_FrameOf,sprintInput_Momentary, slideInput_FrameOf, slideInput_Momentary;
    bool changingStances = false; 

    //Slope Affectors

    //Jumping
    bool jumpInput_Momentary, jumpInput_FrameOf;

    //Sliding
    Vector3 cachedDirPreSlide, cachedPosPreSlide;



    [Space(20)]
    #endregion
    
    #region Parkour
            #if SAIO_ENABLE_PARKOUR

    //
    //Public
    //

    //Vaulting
    public bool canVault = true, isVaulting, autoVaultWhenSpringing;
    #if ENABLE_INPUT_SYSTEM
    public Key VaultKey = Key.E;
    #else
    public KeyCode VaultKey_L = KeyCode.E;
    #endif
    public string vaultObjectTag = "Vault Obj";
    public float vaultSpeed = 7.5f, maxVaultDepth = 1.5f, maxVaultHeight = 0.75f;


    //
    //Internal
    //

    //Vaulting
    RaycastHit VC_Stage1, VC_Stage2, VC_Stage3, VC_Stage4;
    Vector3 vaultForwardVec;
    bool vaultInput;

    //All
    #endif
    private bool doingPosInterp, doingCamInterp;
    #endregion

    #region Stamina System
    //Public
    public bool enableStaminaSystem = true, jumpingDepletesStamina = true;
    [Range(0.0f,250.0f)]public float stamina = 50.0f;
    [Range(0.0f,250.0f)]public float currentStaminaLevel = 0, s_minimumStaminaToSprint = 5.0f, s_depletionSpeed = 2.0f,  s_regenerationSpeed = 1.2f, s_JumpStaminaDepletion = 5.0f;

    //Internal
    bool staminaIsChanging;
    bool ignoreStamina = false;
    #endregion
    
    #region Footstep System
    [Header("Footstep System")]
    public bool enableFootstepSounds = true;
    public FootstepTriggeringMode footstepTriggeringMode = FootstepTriggeringMode.CalculatedTiming;
    [Range(0.0f,1.0f)] public float stepTiming = 0.15f;
    public List<GroundMaterialProfile> footstepSoundSet = new List<GroundMaterialProfile>();
    bool shouldCalculateFootstepTriggers = true;
    float StepCycle = 0;
    AudioSource playerAudioSource;
    List<AudioClip> currentClipSet = new();
    [Space(18)]
    #endregion
    
    #region  Headbob
    //
    //Public
    //
    public bool enableHeadBob = true;
    [Range(1.0f,5.0f)] public float headBobSpeed = 0.5f;
    [Range(1.0f,5.0f)] public float headBobPower = 0.25f;
    [Range(0.0f,3.0f)] public float zTilt = 3;

    //
    //Internal
    //
    bool shouldCalculateHeadBob;
    Vector3 headBobCameraPosition;
    float headBobCyclePosition, headBobWarmUp;

    #endregion
    
    #region  Survival Stats
    //
    //Public
    //
    public bool enableSurvivalStats = true;
    public SurvivalStats defaultSurvivalStats = new SurvivalStats();
    public float statTickRate = 6.0f, hungerDepletionRate = 0.06f, hydrationDepletionRate = 0.14f;
    public SurvivalStats currentSurvivalStats = new SurvivalStats();

    //
    //Internal
    //
    float StatTickTimer;
    #endregion

    #region Interactable
    #if ENABLE_INPUT_SYSTEM

    //
    //Public
    //
    public Key interactKey = Key.E;
    #else
    public KeyCode interactKey_L = KeyCode.E;
    #endif
    public float interactRange = 4;
    public LayerMask interactableLayer = -1;
    //
    //Internal
    //
    bool interactInput;
    #endregion  

    #region Animation
    //
    //Public
    //

    //First-person
    public Animator _1stPersonCharacterAnimator;
    //Third-Person
    public Animator _3rdPersonCharacterAnimator;
    public string a_velocity, a_2DVelocity, a_Grounded, a_Idle, a_Jumped, a_Sliding, a_Sprinting, a_Crouching;
    public bool stickRendererToCapsuleBottom = true;

    #endregion
    
    [Space(18)]
    public bool enableGroundingDebugging = false, enableMovementDebugging = false, enableMouseAndCameraDebugging = false, enableVaultDebugging = false;
    #endregion

    public override void OnStartAuthority() {
        cameraHolder.SetActive(true);
    }

    void Start() {
        if (!isLocalPlayer) return;
        
        
        #region Camera
        maxCameraDistInternal = maxCameraDistance;
        initialCameraFOV = playerCamera.fieldOfView;
        headBobCameraPosition = Vector3.up*standingEyeHeight;
        internalEyeHeight = standingEyeHeight;
        if (lockAndHideMouse) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (autoGenerateCrosshair || drawPrimitiveUI) {
                Canvas canvas = playerCamera.gameObject.GetComponentInChildren<Canvas>();
                if (canvas == null) {canvas = new GameObject("AutoCrosshair").AddComponent<Canvas>();}
                canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
                canvas.transform.SetParent(playerCamera.transform);
                canvas.transform.position = Vector3.zero;
            if (autoGenerateCrosshair && crosshairSprite) {
                crosshairImg = new GameObject("Crosshair").AddComponent<Image>();
                crosshairImg.sprite = crosshairSprite;
                crosshairImg.rectTransform.sizeDelta = new Vector2(25,25);
                crosshairImg.transform.SetParent(canvas.transform);
                crosshairImg.transform.position = Vector3.zero;
                crosshairImg.raycastTarget = false;
            }
            if (drawPrimitiveUI) {
                //Stam Meter BG
                stamMeterBG = new GameObject("Stam BG").AddComponent<Image>();
                stamMeterBG.rectTransform.sizeDelta = normalStamMeterSizeDelta;
                stamMeterBG.transform.SetParent(canvas.transform);
                stamMeterBG.rectTransform.anchorMin = new Vector2(0.5f,0);
                stamMeterBG.rectTransform.anchorMax = new Vector2(0.5f,0);
                stamMeterBG.rectTransform.anchoredPosition = new Vector2(0,22);
                stamMeterBG.color = Color.gray;
                stamMeterBG.gameObject.SetActive(enableStaminaSystem);
                //Stam Meter
                stamMeter = new GameObject("Stam Meter").AddComponent<Image>();
                stamMeter.rectTransform.sizeDelta = normalStamMeterSizeDelta;
                stamMeter.transform.SetParent(canvas.transform);
                stamMeter.rectTransform.anchorMin = new Vector2(0.5f,0);
                stamMeter.rectTransform.anchorMax = new Vector2(0.5f,0);
                stamMeter.rectTransform.anchoredPosition = new Vector2(0,22);
                stamMeter.color = Color.white;
                stamMeter.gameObject.SetActive(enableStaminaSystem);
                //Stats Panel
                statsPanel = new GameObject("Stats Panel").AddComponent<Image>();
                statsPanel.rectTransform.sizeDelta = new Vector2(3,45);
                statsPanel.transform.SetParent(canvas.transform);
                statsPanel.rectTransform.anchorMin = new Vector2(0,0);
                statsPanel.rectTransform.anchorMax = new Vector2(0,0);
                statsPanel.rectTransform.anchoredPosition = new Vector2(12,33);
                statsPanel.color = Color.clear;
                statsPanel.gameObject.SetActive(enableSurvivalStats);
                //Stats Panel BG
                statsPanelBG = new GameObject("Stats Panel BG").AddComponent<Image>();
                statsPanelBG.rectTransform.sizeDelta = new Vector2(175,45);
                statsPanelBG.transform.SetParent(statsPanel.transform);
                statsPanelBG.rectTransform.anchorMin = new Vector2(0,0);
                statsPanelBG.rectTransform.anchorMax = new Vector2(1,0);
                statsPanelBG.rectTransform.anchoredPosition = new Vector2(87,22);
                statsPanelBG.color = Color.white*0.5f;
                //Health Meter
                HealthMeter = new GameObject("Health Meter").AddComponent<Image>();
                HealthMeter.rectTransform.sizeDelta = normalMeterSizeDelta;
                HealthMeter.transform.SetParent(statsPanel.transform);
                HealthMeter.rectTransform.anchorMin = new Vector2(0,0);
                HealthMeter.rectTransform.anchorMax = new Vector2(1,0);
                HealthMeter.rectTransform.anchoredPosition = new Vector2(87,6);
                HealthMeter.color =new Color32(211,0,0, 255);
                //Hydration Meter
                HydrationMeter = new GameObject("Hydration Meter").AddComponent<Image>();
                HydrationMeter.rectTransform.sizeDelta = normalMeterSizeDelta;
                HydrationMeter.transform.SetParent(statsPanel.transform);
                HydrationMeter.rectTransform.anchorMin = new Vector2(0,0);
                HydrationMeter.rectTransform.anchorMax = new Vector2(1,0);
                HydrationMeter.rectTransform.anchoredPosition = new Vector2(87,22);
                HydrationMeter.color =new Color32(0,194,255, 255);
                //Hunger Meter
                HungerMeter = new GameObject("Hunger Meter").AddComponent<Image>();
                HungerMeter.rectTransform.sizeDelta = normalMeterSizeDelta;
                HungerMeter.transform.SetParent(statsPanel.transform);
                HungerMeter.rectTransform.anchorMin = new Vector2(0,0);
                HungerMeter.rectTransform.anchorMax = new Vector2(1,0);
                HungerMeter.rectTransform.anchoredPosition = new Vector2(87,38);
                HungerMeter.color = new Color32(142,54,0, 255);
                
            }
        }
        if (cameraPerspective == PerspectiveModes._3rdPerson && !showCrosshairIn3rdPerson) {
            crosshairImg?.gameObject.SetActive(false);
        }
        initialRot = transform.localEulerAngles;
        #endregion 

        #region Movement
        p_Rigidbody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        standingHeight = capsule.height;
        currentGroundSpeed = walkingSpeed;
        _ZeroFriction = new PhysicsMaterial("Zero_Friction") {
            dynamicFriction = 0f,
            staticFriction = 0,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        _MaxFriction = new PhysicsMaterial("Max_Friction") {
            dynamicFriction = 1,
            staticFriction = 1,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Average
        };

        #endregion

        //Stamina System
        currentStaminaLevel = stamina;
        
        //Footstep
        playerAudioSource = GetComponent<AudioSource>();
        
    }
    void Update() {
        if (!isLocalPlayer) return;

        if (!controllerPaused) {
            #region Input
#if ENABLE_INPUT_SYSTEM
            if (toggleMouseLockKey != Key.None && Keyboard.current[toggleMouseLockKey].wasPressedThisFrame) {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            }
            
            MouseXY.x = Mouse.current.delta.y.ReadValue()/50;
            MouseXY.y = Mouse.current.delta.x.ReadValue()/50;
            
            mouseScrollWheel = Mouse.current.scroll.y.ReadValue()/1000;
            if (perspectiveSwitchingKey!=Key.None)perspecTog = Keyboard.current[perspectiveSwitchingKey].wasPressedThisFrame;
            if (interactKey!=Key.None)interactInput = Keyboard.current[interactKey].wasPressedThisFrame;
            //movement

            if (jumpKey!=Key.None)jumpInput_Momentary =  Keyboard.current[jumpKey].isPressed;
            if (jumpKey!=Key.None)jumpInput_FrameOf =  Keyboard.current[jumpKey].wasPressedThisFrame;

            if (crouchKey!=Key.None) {
                crouchInput_Momentary =  Keyboard.current[crouchKey].isPressed;
                crouchInput_FrameOf = Keyboard.current[crouchKey].wasPressedThisFrame;
            }
            if (sprintKey!=Key.None) {
                sprintInput_Momentary = Keyboard.current[sprintKey].isPressed;
                sprintInput_FrameOf = Keyboard.current[sprintKey].wasPressedThisFrame;
            }
            if (slideKey != Key.None) {
                slideInput_Momentary = Keyboard.current[slideKey].isPressed;
                slideInput_FrameOf = Keyboard.current[slideKey].wasPressedThisFrame;
            }
#if SAIO_ENABLE_PARKOUR
            vaultInput = Keyboard.current[VaultKey].isPressed;
#endif
            MovInput.x = Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0;
            MovInput.y = Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0;
#else
            //camera
            if (enableCameraControl)
            {
                MouseXY.x = Input.GetAxis("Mouse Y");
                MouseXY.y = Input.GetAxis("Mouse X");
                mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
                perspecTog = Input.GetKeyDown(perspectiveSwitchingKey_L);
                interactInput =Input.GetKeyDown(interactKey_L);        
            }
            
            //movement

            jumpInput_Momentary = Input.GetKey(jumpKey_L);
            jumpInput_FrameOf = Input.GetKeyDown(jumpKey_L);
            crouchInput_Momentary = Input.GetKey(crouchKey_L);
            crouchInput_FrameOf = Input.GetKeyDown(crouchKey_L);
            sprintInput_Momentary = Input.GetKey(sprintKey_L);
            sprintInput_FrameOf = Input.GetKeyDown(sprintKey_L);
            slideInput_Momentary = Input.GetKey(slideKey_L);
            slideInput_FrameOf = Input.GetKeyDown(slideKey_L);
            #if SAIO_ENABLE_PARKOUR

            vaultInput = Input.GetKeyDown(VaultKey_L);
            #endif
            MovInput = Vector2.up *Input.GetAxisRaw("Vertical") + Vector2.right * Input.GetAxisRaw("Horizontal");
#endif
            #endregion

            #region Camera
            if (enableCameraControl) {
                switch (cameraPerspective) {
                    case PerspectiveModes._1stPerson:{
                        //This is called in FixedUpdate for the 3rd person mode
                        if (!isInFirstPerson) { ChangePerspective(PerspectiveModes._1stPerson); }
                        if (perspecTog || (automaticallySwitchPerspective && mouseScrollWheel < 0)) { ChangePerspective(PerspectiveModes._3rdPerson); }
                        HeadBobCycleCalculator();
                        FOVKick();
                    }break;

                    case PerspectiveModes._3rdPerson:{
                        if (!isInThirdPerson) { ChangePerspective(PerspectiveModes._3rdPerson); }
                        if (perspecTog || (automaticallySwitchPerspective && maxCameraDistInternal == 0 && currentCameraZ == 0)) { ChangePerspective(PerspectiveModes._1stPerson); }
                        maxCameraDistInternal = Mathf.Clamp(maxCameraDistInternal - mouseScrollWheel*cameraZoomSensitivity*2, automaticallySwitchPerspective ? 0 : capsule.radius*2, maxCameraDistance);
                    }break;
                }

            
                if (setInitialRot) {
                    setInitialRot = false;
                    RotateView(initialRot,false);
                    InputDir = transform.forward;
                }
            }
            if (drawPrimitiveUI) {
                if (enableSurvivalStats) {
                    if (!statsPanel.gameObject.activeSelf) { statsPanel.gameObject.SetActive(true); }

                    HealthMeter.rectTransform.sizeDelta    = Vector2.Lerp(Vector2.up*12, normalMeterSizeDelta, currentSurvivalStats.health/defaultSurvivalStats.health);
                    HydrationMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up*12, normalMeterSizeDelta, currentSurvivalStats.hydration/defaultSurvivalStats.hydration);
                    HungerMeter.rectTransform.sizeDelta    = Vector2.Lerp(Vector2.up*12, normalMeterSizeDelta, currentSurvivalStats.hunger/defaultSurvivalStats.hunger);
                } else { 
                    if (statsPanel.gameObject.activeSelf)statsPanel.gameObject.SetActive(false);
               
                }
                if (enableStaminaSystem) {
                    if (!stamMeterBG.gameObject.activeSelf)stamMeterBG.gameObject.SetActive(true);
                    if (!stamMeter.gameObject.activeSelf)stamMeter.gameObject.SetActive(true);
                    if (staminaIsChanging) {
                        if (stamMeter.color != Color.white) {
                            stamMeterBG.color = Vector4.MoveTowards(stamMeterBG.color, new Vector4(0,0,0,0.5f),0.15f);
                            stamMeter.color = Vector4.MoveTowards(stamMeter.color, new Vector4(1,1,1,1),0.15f);
                        }
                        stamMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up*5,normalStamMeterSizeDelta, currentStaminaLevel/stamina);
                    } else { 
                        if (stamMeter.color != Color.clear) {
                            stamMeterBG.color = Vector4.MoveTowards(stamMeterBG.color, new Vector4(0,0,0,0),0.15f);
                            stamMeter.color = Vector4.MoveTowards(stamMeter.color, new Vector4(0,0,0,0),0.15f);
                        }
                    }
                } else { 
                    if (stamMeterBG.gameObject.activeSelf) stamMeterBG.gameObject.SetActive(false);
                    if (stamMeter.gameObject.activeSelf) stamMeter.gameObject.SetActive(false);
                }
            }
        
            if (currentStance == Stances.Standing && !changingStances) {
                internalEyeHeight = standingEyeHeight;
            }
            #endregion

            #region Movement
            if (cameraPerspective == PerspectiveModes._3rdPerson) {
                HeadRotDirForInput = Mathf.MoveTowardsAngle(HeadRotDirForInput,headRot.y, bodyCatchupSpeed*(1+Time.deltaTime));
                MovInput_Smoothed = Vector2.MoveTowards(MovInput_Smoothed, MovInput, inputResponseFiltering*(1+Time.deltaTime));
            }

            Vector3 inputDirUnscaled;
            if (cameraPerspective == PerspectiveModes._1stPerson) {
                inputDirUnscaled =
                    transform.forward*MovInput.y +
                    transform.right*(viewInputMethods == ViewInputModes.Traditional ? MovInput.x : 0);
            }
            else {
                inputDirUnscaled = Quaternion.AngleAxis(HeadRotDirForInput, Vector3.up) *
                                   (Vector3.forward*MovInput_Smoothed.y + Vector3.right*MovInput_Smoothed.x);
            }
            InputDir = Vector3.ClampMagnitude(inputDirUnscaled, 1);

            GroundMovementSpeedUpdate();
            if (canJump && (holdJump ? jumpInput_Momentary : jumpInput_FrameOf)) { Jump(jumpPower); }
            #endregion
        
            //Stamina system
            if (enableStaminaSystem) { CalculateStamina(); }
        
            //Footstep
            CalculateFootstepTriggers();

            //Survival Stats
            if (enableSurvivalStats && Time.time > StatTickTimer) { TickStats(); }

            //Interaction
            if (interactInput) { TryInteract(); }
        
        } else { 
            jumpInput_FrameOf = false;
            jumpInput_Momentary = false;
        }
        #region Animation
        UpdateAnimationTriggers(controllerPaused);
        #endregion
    }
    void FixedUpdate() {
        if (!isLocalPlayer) return;

        if (!controllerPaused) {

            #region Movement
            if (enableMovementControl) {
                GetGroundInfo();
                MovePlayer(InputDir, currentGroundSpeed);

                if (isSliding) { Slide(); }
            }
            #endregion

            #region Camera
            if (enableCameraControl) {
                RotateView(MouseXY, sensitivity, rotationWeight);
                if (cameraPerspective == PerspectiveModes._3rdPerson) {
                    UpdateBodyRotation_3rdPerson();
                    UpdateCameraPosition_3rdPerson();
                }
            }
            #endregion
        }
    }
    private void OnTriggerEnter(Collider other) {
        //Collectables
        other.GetComponent<ICollectable>()?.Collect();
    }
 
    #region Camera Functions
    
    float NormalizeAngle(float angle) {
        while (angle > 180)
            angle -= 360;
        while (angle < -180)
            angle += 360;
        return angle;
    }
    
    void RotateView(Vector2 yawPitchInput, float inputSensitivity, float cameraWeight) {
        
        switch (viewInputMethods) {
            
            case ViewInputModes.Traditional:{  
                yawPitchInput.x *= (mouseInputInversion is MouseInputInversionModes.X or MouseInputInversionModes.Both) ? 1 : -1;
                yawPitchInput.y *= (mouseInputInversion is MouseInputInversionModes.Y or MouseInputInversionModes.Both) ? -1 : 1;
                float maxDelta = Mathf.Min(5, 26 - cameraWeight) * 360;
                switch(cameraPerspective) {
                    case PerspectiveModes._1stPerson:{
                        Vector2 targetAngles = new(playerCamera.transform.localEulerAngles.x, p_Rigidbody.rotation.eulerAngles.y);
                        float fovMod = fovSensitivityMultiplier>0 && playerCamera.fieldOfView <= initialCameraFOV ? ((initialCameraFOV - playerCamera.fieldOfView)*(fovSensitivityMultiplier/10))+1 : 1;
                        targetAngles = Vector2.SmoothDamp(targetAngles,
                            targetAngles + yawPitchInput * (inputSensitivity * 5 / fovMod), 
                            ref viewRotVelRef,
                            Mathf.Pow(cameraWeight * fovMod, 2) * Time.fixedDeltaTime, 
                            maxDelta, 
                            Time.fixedDeltaTime);
                        targetAngles.x = NormalizeAngle(targetAngles.x);
                        targetAngles.x = Mathf.Clamp(targetAngles.x,-0.5f*verticalRotationRange,0.5f*verticalRotationRange);
                        playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward* (enableHeadBob? headBobCameraPosition.z : 0));
                        p_Rigidbody.MoveRotation(Quaternion.Euler(Vector3.up*targetAngles.y));
                    }break;

                    case PerspectiveModes._3rdPerson:{
                        headPos = transform.position + Vector3.up *standingEyeHeight;
                        quatHeadRot = Quaternion.Euler(headRot);
                        headRot = Vector3.SmoothDamp(headRot,
                            headRot + (Vector3)yawPitchInput * (inputSensitivity * 5),
                            ref cameraPosVelRef,
                            Mathf.Pow(cameraWeight, 2) * Time.fixedDeltaTime,
                            maxDelta,
                            Time.fixedDeltaTime);
                        headRot.y = NormalizeAngle(headRot.y);
                        headRot.x = NormalizeAngle(headRot.x);
                        headRot.x = Mathf.Clamp(headRot.x,-0.5f*verticalRotationRange,0.5f*verticalRotationRange);
                    }break;
                        
                }
            
            }break;
            
            case ViewInputModes.Retro:{
                yawPitchInput = Vector2.up * (Input.GetAxis("Horizontal") * (mouseInputInversion is MouseInputInversionModes.Y or MouseInputInversionModes.Both ? -1 : 1));
                Vector2 targetAngles = new(playerCamera.transform.localEulerAngles.x, transform.localEulerAngles.y);
                float fovMod = fovSensitivityMultiplier > 0 && playerCamera.fieldOfView <= initialCameraFOV ? ((initialCameraFOV - playerCamera.fieldOfView)*(fovSensitivityMultiplier/10))+1 : 1;
                targetAngles += yawPitchInput * (inputSensitivity/fovMod);   
                targetAngles.x = 0;
                playerCamera.transform.localEulerAngles = Vector3.right*targetAngles.x + Vector3.forward*(enableHeadBob ? headBobCameraPosition.z : 0);
                transform.localEulerAngles = Vector3.up * targetAngles.y;
            }break;
        }
        
    }

    public void RotateView(Vector3 absoluteEulerAngles, bool smoothRotation) {

        switch (cameraPerspective) {

            case PerspectiveModes._1stPerson: {
                absoluteEulerAngles.x = NormalizeAngle(absoluteEulerAngles.x);
                absoluteEulerAngles.x = Mathf.Clamp(absoluteEulerAngles.x,-0.5f*verticalRotationRange,0.5f*verticalRotationRange);

                if (smoothRotation) {
                    IEnumerator SmoothRot() {
                        doingCamInterp = true;
                        Vector3 refVec = Vector3.zero;
                        Vector3 targetAngles = new(playerCamera.transform.localEulerAngles.x, transform.eulerAngles.y, 0);
                        while (Vector3.Distance(targetAngles, absoluteEulerAngles) > 0.1f) {
                            targetAngles = Vector3.SmoothDamp(targetAngles, absoluteEulerAngles, ref refVec, 25*Time.deltaTime);
                            targetAngles.x = NormalizeAngle(targetAngles.x);
                            targetAngles.x = Mathf.Clamp(targetAngles.x, -verticalRotationRange/2f, verticalRotationRange/2f);
                            playerCamera.transform.localEulerAngles = Vector3.right * targetAngles.x;
                            transform.eulerAngles = Vector3.up * targetAngles.y;
                            yield return null;
                        }

                        doingCamInterp = false;
                    }

                    StopCoroutine("SmoothRot");
                    StartCoroutine(SmoothRot());
                }
                else {
                    playerCamera.transform.eulerAngles = Vector3.right * absoluteEulerAngles.x;
                    transform.eulerAngles = new(0, absoluteEulerAngles.y, absoluteEulerAngles.z);
                }
            }break;

            case (PerspectiveModes._3rdPerson):{
                if (smoothRotation) {
                    absoluteEulerAngles.y = NormalizeAngle(absoluteEulerAngles.y);
                    absoluteEulerAngles.x += NormalizeAngle(absoluteEulerAngles.x);
                    absoluteEulerAngles.x = Mathf.Clamp(absoluteEulerAngles.x,-0.5f*verticalRotationRange,0.5f*verticalRotationRange);
                    IEnumerator SmoothRot() {
                        doingCamInterp = true;
                        Vector3 refVec = Vector3.zero;
                        while (Vector3.Distance(headRot, absoluteEulerAngles)>0.1f) {
                            headPos = p_Rigidbody.position + Vector3.up *standingEyeHeight;
                            quatHeadRot = Quaternion.Euler(headRot);
                            headRot = Vector3.SmoothDamp(headRot,absoluteEulerAngles,ref refVec,25*Time.deltaTime);
                            headRot.y = NormalizeAngle(headRot.y);
                            headRot.x = NormalizeAngle(headRot.x);
                            headRot.x = Mathf.Clamp(headRot.x,-0.5f*verticalRotationRange,0.5f*verticalRotationRange);
                            yield return null;
                        }
                        doingCamInterp = false;
                    }
                    StopCoroutine("SmoothRot");
                    StartCoroutine(SmoothRot());
                }
                else{
                    headRot = absoluteEulerAngles;
                    headRot.y = NormalizeAngle(headRot.y);
                    headRot.x = NormalizeAngle(headRot.x);
                    headRot.x = Mathf.Clamp(headRot.x, -0.5f*verticalRotationRange,0.5f*verticalRotationRange);
                    quatHeadRot = Quaternion.Euler(headRot);
                }
            }break;
        }
    }
    public void ChangePerspective(PerspectiveModes newPerspective = PerspectiveModes._1stPerson) {   
        switch(newPerspective) {
            case PerspectiveModes._1stPerson:{
                StopCoroutine("SmoothRot");
                isInThirdPerson = false;
                isInFirstPerson = true;
                transform.eulerAngles = Vector3.up * headRot.y;
                playerCamera.transform.localPosition = Vector3.up*standingEyeHeight;
                playerCamera.transform.localEulerAngles = (Vector2)playerCamera.transform.localEulerAngles;
                cameraPerspective = newPerspective;
                if (_3rdPersonCharacterAnimator) {
                    _3rdPersonCharacterAnimator.gameObject.SetActive(false);
                }
                if (_1stPersonCharacterAnimator) {
                    _1stPersonCharacterAnimator.gameObject.SetActive(true);
                }
                if (crosshairImg && autoGenerateCrosshair) {
                    crosshairImg.gameObject.SetActive(true);
                }
            }break;

            case PerspectiveModes._3rdPerson:{
                StopCoroutine("SmoothRot");
                isInThirdPerson = true;
                isInFirstPerson = false;
                playerCamera.fieldOfView = initialCameraFOV;
                maxCameraDistInternal = maxCameraDistInternal == 0 ? capsule.radius * 2 : maxCameraDistInternal;
                currentCameraZ = -(maxCameraDistInternal * 0.85f);
                playerCamera.transform.localEulerAngles = (Vector2)playerCamera.transform.localEulerAngles;
                headRot.y = transform.eulerAngles.y;
                headRot.x = playerCamera.transform.eulerAngles.x;
                cameraPerspective = newPerspective;
                if (_3rdPersonCharacterAnimator) {
                    _3rdPersonCharacterAnimator.gameObject.SetActive(true);
                }

                if (_1stPersonCharacterAnimator) {
                    _1stPersonCharacterAnimator.gameObject.SetActive(false);
                }

                if (crosshairImg && autoGenerateCrosshair) {
                    crosshairImg.gameObject.SetActive(showCrosshairIn3rdPerson);
                }
            }break;
        }
    }
    void FOVKick() {
        if (cameraPerspective == PerspectiveModes._1stPerson && fovKickAmount>0) {
            currentFOVMod = (!isIdle && isSprinting)
                ? initialCameraFOV + fovKickAmount*(sprintingSpeed/walkingSpeed - 1)
                : initialCameraFOV;
            if (!Mathf.Approximately(playerCamera.fieldOfView, currentFOVMod) && playerCamera.fieldOfView >= initialCameraFOV) {
                playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, currentFOVMod,ref FOVKickVelRef, Time.deltaTime,50);
            }
        }
    }
    private void HeadBobCycleCalculator() {
        if (!enableHeadBob) return;
        
        if (!isIdle && currentGroundInfo.isGettingGroundInfo && !isSliding) {
            headBobWarmUp = Mathf.MoveTowards(headBobWarmUp, 1, Time.deltaTime * 5);
            headBobCyclePosition += _2DVelocity.magnitude * (Time.deltaTime * (headBobSpeed / 10));

            headBobCameraPosition.x = Mathf.Sin(Mathf.PI * (2 * headBobCyclePosition + 0.5f)) *
                                      (headBobPower / 50) * headBobWarmUp;
            headBobCameraPosition.y =
                Mathf.Abs(Mathf.Sin(Mathf.PI * (2 * headBobCyclePosition)) * 0.75f * (headBobPower / 50)) *
                headBobWarmUp + internalEyeHeight;
            headBobCameraPosition.z =
                Mathf.Sin(Mathf.PI * (2 * headBobCyclePosition)) * (zTilt / 3) * headBobWarmUp;
        }
        else {
            headBobCameraPosition = Vector3.MoveTowards(headBobCameraPosition, Vector3.up * internalEyeHeight,
                Time.deltaTime / (headBobPower * 0.3f));
            headBobWarmUp = 0.1f;
        }

        playerCamera.transform.localPosition = (Vector2)headBobCameraPosition;
        if (StepCycle > headBobCyclePosition * 3) StepCycle = headBobCyclePosition + 0.5f;
    }

    void UpdateCameraPosition_3rdPerson() {
        //Camera Obstacle Check
        cameraObstCheck = new Ray(headPos + quatHeadRot * (Vector3.forward * capsule.radius), quatHeadRot * -Vector3.forward);
        if (Physics.SphereCast(cameraObstCheck, 0.5f, out cameraObstResult, maxCameraDistInternal, cameraObstructionIgnore, QueryTriggerInteraction.Ignore)) {
            currentCameraZ = Vector3.Distance(headPos, cameraObstResult.point) * -0.9f;
        }
        else {
            currentCameraZ = Mathf.SmoothDamp(currentCameraZ, -(maxCameraDistInternal * 0.85f), ref cameraZRef, Time.deltaTime, 10, Time.fixedDeltaTime);
        }

        //Debugging
        if (enableMouseAndCameraDebugging) {
            Debug.Log(headRot);
            Debug.DrawRay(cameraObstCheck.origin, cameraObstCheck.direction * maxCameraDistance, Color.red);
            Debug.DrawRay(cameraObstCheck.origin, cameraObstCheck.direction * -currentCameraZ, Color.green);
        }

        currentCameraPos = headPos + (quatHeadRot * (Vector3.forward * currentCameraZ));
        playerCamera.transform.position = currentCameraPos;
        playerCamera.transform.rotation = quatHeadRot;
    }

    void UpdateBodyRotation_3rdPerson() { 
         //if the character is moving, rotate capsule to match camera forward
         //change button down to bool of isFiring or isTargeting
         if (isSliding) {
             transform.localRotation = Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(
                 p_Rigidbody.rotation.eulerAngles.y,
                 Mathf.Atan2(p_Rigidbody.linearVelocity.x, p_Rigidbody.linearVelocity.z) * Mathf.Rad2Deg,
                 10));
         }
         else if (!isIdle && currentGroundInfo.isGettingGroundInfo) {
             transform.rotation = Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(
                 p_Rigidbody.rotation.eulerAngles.y, 
                 Mathf.Atan2(InputDir.x, InputDir.z) * Mathf.Rad2Deg, 
                 10));
         }
         else if (!currentGroundInfo.isGettingGroundInfo && rotateCharacterToCameraForward) {
             transform.localRotation = Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(
                     p_Rigidbody.rotation.eulerAngles.y, 
                     headRot.y, 
                     10));
         }
    }
    #endregion

    #region Movement Functions
    void MovePlayer(Vector3 direction, float speed) {
        isIdle = (direction.normalized.magnitude <= 0);
        _2DVelocity = Vector2.right * p_Rigidbody.linearVelocity.x + Vector2.up * p_Rigidbody.linearVelocity.z;
        speedToVelocityRatio = Mathf.Lerp(0, 2, Mathf.InverseLerp(0, sprintingSpeed/50, _2DVelocity.magnitude));
        _2DVelocityMag = Mathf.Clamp(walkingSpeed/50 / _2DVelocity.magnitude, 0f,2f);
    
        //Sliding
        if (isSliding) {
            p_Rigidbody.AddForce(
                -(p_Rigidbody.linearVelocity - Physics.gravity) * (slidingDeceleration * Time.fixedDeltaTime),
                ForceMode.Force);
        }
        //Movement
        else if (currentGroundInfo.isGettingGroundInfo && !jumped && !doingPosInterp) {
            //Deceleration
            if (direction.magnitude == 0 && p_Rigidbody.linearVelocity.normalized.magnitude > 0.1f) {
                p_Rigidbody.AddForce(
                    -new Vector3(p_Rigidbody.linearVelocity.x,
                        currentGroundInfo.isInContactWithGround ? p_Rigidbody.linearVelocity.y - Physics.gravity.y : 0,
                        p_Rigidbody.linearVelocity.z) * (decelerationSpeed * Time.fixedDeltaTime), ForceMode.Force);
            }
            //normal speed
            else if (currentGroundInfo.isGettingGroundInfo && currentGroundInfo.groundAngle < hardSlopeLimit &&
                     currentGroundInfo.groundAngle_Raw < hardSlopeLimit) {
                p_Rigidbody.linearVelocity = Vector3.MoveTowards(p_Rigidbody.linearVelocity,
                    Vector3.ClampMagnitude(direction*(speed * Time.fixedDeltaTime) + Vector3.down, speed / 50), 1);
            }

            capsule.sharedMaterial = InputDir.magnitude > 0 ? _ZeroFriction : _MaxFriction;
        }
        //Air Control
        else if (!currentGroundInfo.isGettingGroundInfo) {
            p_Rigidbody.AddForce(direction * (walkingSpeed * Time.fixedDeltaTime * airControlFactor * 5 * currentGroundInfo.groundAngleMultiplier_Inverse_persistent), ForceMode.Acceleration);
            p_Rigidbody.linearVelocity = Vector3.ClampMagnitude(
                Vector3.right * p_Rigidbody.linearVelocity.x + Vector3.forward * p_Rigidbody.linearVelocity.z,
                walkingSpeed / 50) + Vector3.up * p_Rigidbody.linearVelocity.y;
            if (!currentGroundInfo.potentialStair && jumpEnhancements) {
                if (p_Rigidbody.linearVelocity.y < 0 && p_Rigidbody.linearVelocity.y > Physics.gravity.y * 1.5f) {
                    p_Rigidbody.linearVelocity +=
                        Vector3.up * (Physics.gravity.y * descentMultiplier * Time.fixedDeltaTime);
                }
                else if (p_Rigidbody.linearVelocity.y > 0 && !jumpInput_Momentary) {
                    p_Rigidbody.linearVelocity +=
                        Vector3.up * (Physics.gravity.y * (tapJumpMultiplier - 1) * Time.fixedDeltaTime);
                }
            }
        }

        
    }

    void Jump(float force) {
        if (currentGroundInfo.isInContactWithGround && 
            currentGroundInfo.groundAngle < hardSlopeLimit &&
            (!enableStaminaSystem || !jumpingDepletesStamina || currentStaminaLevel > s_JumpStaminaDepletion * 1.2f) &&
            Time.time > jumpBlankingPeriod + 0.1f && 
            currentStance == Stances.Standing && !jumped) 
        {
            jumped = true;
            p_Rigidbody.linearVelocity = Vector3.right * p_Rigidbody.linearVelocity.x +
                                         Vector3.forward * p_Rigidbody.linearVelocity.z;
            p_Rigidbody.AddForce(Vector3.up * (force / 10), ForceMode.Impulse);
            if (enableStaminaSystem && jumpingDepletesStamina) {
                InstantStaminaReduction(s_JumpStaminaDepletion);
            }

            capsule.sharedMaterial = _ZeroFriction;
            jumpBlankingPeriod = Time.time;
        }
    }

    void Slide() {
        if (!isSliding) {
            if (currentGroundInfo.isInContactWithGround) {
                //do debug print
                if (enableMovementDebugging) { print("Starting Slide."); }

                p_Rigidbody.AddForce(transform.forward*sprintingSpeed + Vector3.up*currentGroundInfo.groundInfluenceDirection.y, ForceMode.Force);
                cachedDirPreSlide = transform.forward;
                cachedPosPreSlide = transform.position;
                capsule.sharedMaterial = _ZeroFriction;
                StartCoroutine(ApplyStance(slidingTransitionSpeed,Stances.Crouching));
                isSliding = true;
            }
        } else if (slideInput_Momentary) {
            if (enableMovementDebugging) { print("Continuing Slide."); }

            if (Vector3.Distance(transform.position, cachedPosPreSlide) < maxFlatSlideDistance) {
                p_Rigidbody.AddForce(cachedDirPreSlide * (sprintingSpeed / 50), ForceMode.Force);
            }

            if (p_Rigidbody.linearVelocity.magnitude > sprintingSpeed / 50) {
                p_Rigidbody.linearVelocity = p_Rigidbody.linearVelocity.normalized * (sprintingSpeed / 50);
            }
            else if (p_Rigidbody.linearVelocity.magnitude < crouchingSpeed / 25) {
                if (enableMovementDebugging) { print("Slide too slow, ending slide into crouch."); }
                isSliding = false;
                isSprinting = false;
                StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Crouching));
                currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
            }
        } else { 
            if (OverheadCheck()) {
                if (p_Rigidbody.linearVelocity.magnitude > walkingSpeed / 50) {
                    if (enableMovementDebugging) { print("Key released, ending slide into a sprint."); }
                    isSliding = false;
                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                    currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                }
                else {
                    if (enableMovementDebugging) { print("Key released, ending slide into a walk."); }
                    isSliding = false;
                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                    currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                }
            }
            else {
                if (enableMovementDebugging) { print("Key released but there is an obstruction. Ending slide into crouch."); }
                isSliding = false;
                isSprinting = false;
                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
            }

        }
    }
    void GetGroundInfo() {
        //to Get if we're actually touching ground.
        //to act as a normal and point buffer.
        currentGroundInfo.groundFromSweep = null;

        currentGroundInfo.groundFromSweep = Physics.SphereCastAll(transform.position, capsule.radius - 0.001f,
            Vector3.down, capsule.height/2 - capsule.radius/2, whatIsGround);
        currentGroundInfo.isInContactWithGround = Physics.Raycast(transform.position, Vector3.down, out currentGroundInfo.groundFromRay, (capsule.height/2)+0.25f,whatIsGround);
        
        if (jumped &&
            (Physics.Raycast(transform.position,
                 Vector3.down,
                 capsule.height / 2 + 0.1f,
                 whatIsGround) ||
             Physics.CheckSphere(transform.position - (Vector3.up * ((capsule.height / 2) - (capsule.radius - 0.05f))),
                 capsule.radius,
                 whatIsGround)) &&
            Time.time > jumpBlankingPeriod + 0.1f) {
            jumped=false;
        }
        
        if (currentGroundInfo.groundFromSweep != null && currentGroundInfo.groundFromSweep.Length != 0) {
            currentGroundInfo.isGettingGroundInfo=true;
            currentGroundInfo.groundNormals_lowGrade.Clear();
            currentGroundInfo.groundNormals_highGrade.Clear();
            foreach(RaycastHit hit in currentGroundInfo.groundFromSweep) {
                if (hit.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Angle(hit.normal, Vector3.up) < hardSlopeLimit) {
                    currentGroundInfo.groundNormals_lowGrade.Add(hit.normal);
                } else {
                    currentGroundInfo.groundNormals_highGrade.Add(hit.normal);
                }
            }

            currentGroundInfo.groundNormal_Averaged = Average(currentGroundInfo.groundNormals_lowGrade.Any()
                ? currentGroundInfo.groundNormals_lowGrade
                : currentGroundInfo.groundNormals_highGrade);
            currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
            currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromSweep.Average(x =>
                (x.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Angle(x.normal, Vector3.up) < hardSlopeLimit)
                    ? x.point.y
                    : currentGroundInfo.groundFromRay.point.y);
                
        } else {
            currentGroundInfo.isGettingGroundInfo = false;
            currentGroundInfo.groundNormal_Averaged = currentGroundInfo.groundFromRay.normal;
            currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
            currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromRay.point.y;
        }

        if (currentGroundInfo.isGettingGroundInfo) {
            currentGroundInfo.groundAngleMultiplier_Inverse_persistent = currentGroundInfo.groundAngleMultiplier_Inverse;
        }
            
        currentGroundInfo.groundInfluenceDirection = Vector3.MoveTowards(currentGroundInfo.groundInfluenceDirection, Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.up)).normalized,2*Time.fixedDeltaTime);
        currentGroundInfo.groundInfluenceDirection.y = 0;
        currentGroundInfo.groundAngle = Vector3.Angle(currentGroundInfo.groundNormal_Averaged,Vector3.up);
        currentGroundInfo.groundAngle_Raw = Vector3.Angle(currentGroundInfo.groundNormal_Raw,Vector3.up);
        currentGroundInfo.groundAngleMultiplier_Inverse = (currentGroundInfo.groundAngle - 90f) / -90f;
        currentGroundInfo.groundAngleMultiplier = currentGroundInfo.groundAngle / 90f;
           
        currentGroundInfo.groundTag = currentGroundInfo.isInContactWithGround ? currentGroundInfo.groundFromRay.transform.tag : string.Empty;
        if (Physics.Raycast(transform.position + Vector3.down*(capsule.height/2f - 0.1f), InputDir,
                out currentGroundInfo.stairCheck_RiserCheck, capsule.radius + 0.1f, whatIsGround)) {
            if (Physics.Raycast(
                    currentGroundInfo.stairCheck_RiserCheck.point +
                    currentGroundInfo.stairCheck_RiserCheck.normal*-0.05f + Vector3.up, Vector3.down,
                    out currentGroundInfo.stairCheck_HeightCheck, 1.1f)) {
                if (!Physics.Raycast(
                        transform.position + Vector3.down*(capsule.height/2f - maxStairRise) +
                        InputDir * (capsule.radius - 0.05f), InputDir, 0.2f, whatIsGround)) {
                    if (!isIdle &&
                        currentGroundInfo.stairCheck_HeightCheck.point.y > currentGroundInfo.stairCheck_RiserCheck.point.y + 0.025f &&
                        Vector3.Angle(currentGroundInfo.groundNormal_Averaged, currentGroundInfo.stairCheck_RiserCheck.normal) > 0.5f) 
                    {
                        p_Rigidbody.position -= Vector3.up*-0.1f;
                        currentGroundInfo.potentialStair = true;
                    }
                } else { currentGroundInfo.potentialStair = false; }
            }
        } else { currentGroundInfo.potentialStair = false; }
             

        currentGroundInfo.playerGroundPosition = Mathf.MoveTowards(currentGroundInfo.playerGroundPosition, currentGroundInfo.groundRawYPosition+ (capsule.height/2) + 0.01f,0.05f);
        

        if (currentGroundInfo.isInContactWithGround && enableFootstepSounds && shouldCalculateFootstepTriggers) {
            if (currentGroundInfo.groundFromRay.collider is TerrainCollider) {
                currentGroundInfo.groundMaterial = null;
                currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                currentGroundInfo.currentTerrain = currentGroundInfo.groundFromRay.transform.GetComponent<Terrain>();
                if (currentGroundInfo.currentTerrain) {
                    Vector2 XZ = new(
                        (transform.position.x - currentGroundInfo.currentTerrain.transform.position.x) 
                          / currentGroundInfo.currentTerrain.terrainData.size.x 
                          * currentGroundInfo.currentTerrain.terrainData.alphamapWidth, 
                        (transform.position.z - currentGroundInfo.currentTerrain.transform.position.z) 
                          / currentGroundInfo.currentTerrain.terrainData.size.z 
                          * currentGroundInfo.currentTerrain.terrainData.alphamapHeight);
                    float[,,] aMap =
                        currentGroundInfo.currentTerrain.terrainData.GetAlphamaps((int)XZ.x, (int)XZ.y, 1, 1);
                    for (int i = 0; i < aMap.Length; i++) {
                        if (aMap[0, 0, i] == 1) {
                            currentGroundInfo.groundLayer =
                                currentGroundInfo.currentTerrain.terrainData.terrainLayers[i];
                            break;
                        }
                    }
                } else { currentGroundInfo.groundLayer = null; }                
            } else {
                currentGroundInfo.groundLayer = null;
                currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                currentGroundInfo.currentMesh = currentGroundInfo.groundFromRay.transform.GetComponent<MeshFilter>().sharedMesh;
                if (currentGroundInfo.currentMesh && currentGroundInfo.currentMesh.isReadable) {
                    int limit = currentGroundInfo.groundFromRay.triangleIndex * 3, submesh;
                    for (submesh = 0; submesh < currentGroundInfo.currentMesh.subMeshCount; submesh++) {
                        int indices = currentGroundInfo.currentMesh.GetTriangles(submesh).Length;
                        if (indices > limit) { break; }
                        limit -= indices;
                    }
                    currentGroundInfo.groundMaterial = currentGroundInfo.groundFromRay.transform.GetComponent<Renderer>().sharedMaterials[submesh];
                }
                else {
                    currentGroundInfo.groundMaterial = currentGroundInfo.groundFromRay.collider.GetComponent<MeshRenderer>().sharedMaterial;
                }
            }
        }
        else {
            currentGroundInfo.groundMaterial = null;
            currentGroundInfo.groundLayer = null;
            currentGroundInfo.groundPhysicMaterial = null;
        }
#if UNITY_EDITOR
        if (enableGroundingDebugging) {
            print($"Grounded: {currentGroundInfo.isInContactWithGround}, " +
                  $"Ground Hits: {currentGroundInfo.groundFromSweep?.Length}, " +
                  $"Ground Angle: {currentGroundInfo.groundAngle:0.00}, " +
                  $"Ground Multi: {currentGroundInfo.groundAngleMultiplier:0.00}, " +
                  $"Ground Multi Inverse: {currentGroundInfo.groundAngleMultiplier_Inverse:0.00}");
            print($"Ground mesh readable for dynamic foot steps: {currentGroundInfo.currentMesh?.isReadable}");
            Debug.DrawRay(transform.position, Vector3.down * (capsule.height/2 + 0.1f), Color.green);
            Debug.DrawRay(transform.position, currentGroundInfo.groundInfluenceDirection, Color.magenta);
            Debug.DrawRay(
                transform.position + Vector3.down*(capsule.height*0.5f - 0.05f) + InputDir*(capsule.radius - 0.05f),
                InputDir*(capsule.radius + 0.1f),
                Color.cyan);
            Debug.DrawRay(
                transform.position + Vector3.down*(capsule.height*0.5f - 0.5f) + InputDir*(capsule.radius - 0.05f),
                InputDir * (capsule.radius + 0.3f),
                new Color(0, .2f, 1, 1));
        }
#endif
    }
    void GroundMovementSpeedUpdate() {
        #if SAIO_ENABLE_PARKOUR
        if (!isVaulting)
        #endif
        {
            switch (currentGroundMovementSpeed) {
                case GroundSpeedProfiles.Walking:{
                    if (isCrouching || isSprinting) {
                        isSprinting = false;
                        isCrouching = false;
                        currentGroundSpeed = walkingSpeed;
                        StopCoroutine("ApplyStance");
                        StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Standing));
                    }
                    #if SAIO_ENABLE_PARKOUR
                    if (vaultInput && canVault) {VaultCheck();}
                    #endif
                    //check for state change call
                    if ((canCrouch && crouchInput_FrameOf) || crouchOverride) {
                        isCrouching = true;
                        isSprinting = false;
                        currentGroundSpeed = crouchingSpeed;
                        currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                        StopCoroutine("ApplyStance");
                        StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Crouching));
                    } else if ((canSprint && sprintInput_FrameOf &&
                                (!enableStaminaSystem || !jumpingDepletesStamina || currentStaminaLevel > s_minimumStaminaToSprint) &&
                                (!enableSurvivalStats || (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving))
                               ) || sprintOverride) 
                    {
                        isCrouching = false;
                        isSprinting = true;
                        currentGroundSpeed = sprintingSpeed;
                        currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                        StopCoroutine("ApplyStance");
                        StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Standing));
                    }
                    break;
                }
                
                case GroundSpeedProfiles.Crouching:{
                    if (!isCrouching) {
                        isCrouching = true;
                        isSprinting = false;
                        currentGroundSpeed = crouchingSpeed;
                        StopCoroutine("ApplyStance");
                        StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Crouching));
                    }


                    //check for state change call
                    if ((toggleCrouch ? crouchInput_FrameOf : !crouchInput_Momentary)&&!crouchOverride && OverheadCheck()) {
                        isCrouching = false;
                        isSprinting = false;
                        currentGroundSpeed = walkingSpeed;
                        currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                        StopCoroutine("ApplyStance");
                        StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Standing));
                    }
                    else if (((canSprint && sprintInput_FrameOf &&
                               (!enableStaminaSystem || !jumpingDepletesStamina || currentStaminaLevel > s_minimumStaminaToSprint) &&
                               (!enableSurvivalStats || (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving))) 
                              || sprintOverride) && OverheadCheck()) 
                    {
                        isCrouching = false;
                        isSprinting = true;
                        currentGroundSpeed = sprintingSpeed;
                        currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                        StopCoroutine("ApplyStance");
                        StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Standing));
                    }
                    break;
                }

                case GroundSpeedProfiles.Sprinting:{
                    //if (!isIdle)
                    {
                        if (!isSprinting) {
                            isCrouching = false;
                            isSprinting = true;
                            currentGroundSpeed = sprintingSpeed;
                            StopCoroutine("ApplyStance");
                            StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Standing));
                        } 
                        #if SAIO_ENABLE_PARKOUR
                        if ((vaultInput || autoVaultWhenSpringing) && canVault) {VaultCheck();}
                        #endif
                        //check for state change call
                        if (canSlide && !isIdle && slideInput_FrameOf && currentGroundInfo.isInContactWithGround) {
                            Slide();
                            currentGroundMovementSpeed = GroundSpeedProfiles.Sliding;
                        }
                        
                        else if ((canCrouch && crouchInput_FrameOf) || crouchOverride) {
                            isCrouching = true;
                            isSprinting = false;
                            currentGroundSpeed = crouchingSpeed;
                            currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                            StopCoroutine("ApplyStance");
                            StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Crouching));
                            //Can't leave sprint in toggle sprint.
                        } else if ((toggleSprint ? sprintInput_FrameOf : !sprintInput_Momentary) && !sprintOverride) {
                            isCrouching = false;
                            isSprinting = false;
                            currentGroundSpeed = walkingSpeed;
                            currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                            StopCoroutine("ApplyStance");
                            StartCoroutine(ApplyStance(stanceTransitionSpeed,Stances.Standing));
                        }
                        break;
                    }
                }
                case GroundSpeedProfiles.Sliding:{
                }break;
            }
        }
    }
    IEnumerator ApplyStance(float smoothSpeed, Stances newStance) {
        currentStance = newStance;
        float targetCapsuleHeight = currentStance==Stances.Standing? standingHeight : crouchingHeight;
        float targetEyeHeight = currentStance == Stances.Standing? standingEyeHeight : crouchingEyeHeight;
        while (!Mathf.Approximately(capsule.height,targetCapsuleHeight)) {
            changingStances = true;
            capsule.height = (smoothSpeed>0? Mathf.MoveTowards(capsule.height, targetCapsuleHeight, stanceTransitionSpeed*Time.fixedDeltaTime) : targetCapsuleHeight);
            internalEyeHeight = (smoothSpeed > 0 ? Mathf.MoveTowards(internalEyeHeight, targetEyeHeight, stanceTransitionSpeed * Time.fixedDeltaTime) : targetCapsuleHeight);
            
            if (currentStance == Stances.Crouching && currentGroundInfo.isGettingGroundInfo) {
                p_Rigidbody.linearVelocity += Vector3.down * 2;
                if (enableMovementDebugging) {print("Applying Stance and applying down force ");}
            }
            yield return new WaitForFixedUpdate();
        }
        changingStances = false;
        yield return null;
    }
    bool OverheadCheck() {    //Returns true when there is no obstruction.
        bool result = Physics.Raycast(transform.position,Vector3.up,standingHeight - capsule.height/2, whatIsGround);
        return !result;
    }

    Vector3 Average(List<Vector3> vectors) {
        Vector3 returnVal = default(Vector3);
        vectors.ForEach(x => { returnVal += x; });
        returnVal /= vectors.Count;
        return returnVal;
    }
    
    #endregion

    #region Stamina System
    private void CalculateStamina() {
        if (isSprinting && !ignoreStamina && !isIdle) {
            if (currentStaminaLevel != 0) {
                currentStaminaLevel = Mathf.MoveTowards(currentStaminaLevel, 0, s_depletionSpeed*Time.deltaTime);
            } else if (!isSliding) { currentGroundMovementSpeed = GroundSpeedProfiles.Walking; }
            staminaIsChanging = true;
        }
        else if (currentStaminaLevel != stamina &&
                 !ignoreStamina &&
                 (!enableSurvivalStats || (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving))) {
            currentStaminaLevel = Mathf.MoveTowards(currentStaminaLevel, stamina, s_regenerationSpeed*Time.deltaTime);
            staminaIsChanging = true;
        } else { 
            staminaIsChanging =false;
        }
    }
    public void InstantStaminaReduction(float reduction) {
        if (!ignoreStamina && enableStaminaSystem) {
            currentStaminaLevel -= reduction;
            currentStaminaLevel = Mathf.Clamp(currentStaminaLevel, 0, stamina);
        }
    }
    #endregion

    #region Footstep System

    void CalculateFootstepTriggers() {
        if (enableFootstepSounds && footstepTriggeringMode == FootstepTriggeringMode.CalculatedTiming &&
            shouldCalculateFootstepTriggers) {
            if (_2DVelocity.magnitude > currentGroundSpeed / 100 && !isIdle) {
                if (cameraPerspective == PerspectiveModes._1stPerson) {
                    if ((enableHeadBob ? headBobCyclePosition : Time.time) > StepCycle &&
                        currentGroundInfo.isGettingGroundInfo && !isSliding) {
                        CallFootstepClip();
                        StepCycle = enableHeadBob
                            ? headBobCyclePosition + 0.5f
                            : Time.time + stepTiming * _2DVelocityMag * 2;
                    }
                }
                else {
                    if (Time.time > StepCycle && currentGroundInfo.isGettingGroundInfo && !isSliding) {
                        CallFootstepClip();
                        StepCycle = Time.time + stepTiming * _2DVelocityMag * 2;
                    }
                }
            }
        }
    }

    public void CallFootstepClip() {
        if (playerAudioSource) {
            if (enableFootstepSounds && footstepSoundSet.Any()) {
                for (int i = 0; i < footstepSoundSet.Count; i++) {
                    if (footstepSoundSet[i].profileTriggerType == MatProfileType.Material) {
                        if (footstepSoundSet[i]._Materials.Contains(currentGroundInfo.groundMaterial)) {
                            currentClipSet = footstepSoundSet[i].footstepClips;
                            break;
                        }
                        else if (i == footstepSoundSet.Count - 1) {
                            currentClipSet = null;
                        }
                    }
                    else if (footstepSoundSet[i].profileTriggerType == MatProfileType.PhysicMaterial) {
                        if (footstepSoundSet[i]._physicMaterials.Contains(currentGroundInfo.groundPhysicMaterial)) {
                            currentClipSet = footstepSoundSet[i].footstepClips;
                            break;
                        }
                        else if (i == footstepSoundSet.Count - 1) {
                            currentClipSet = null;
                        }
                    }
                    else if (footstepSoundSet[i].profileTriggerType == MatProfileType.TerrainLayer) {
                        if (footstepSoundSet[i]._Layers.Contains(currentGroundInfo.groundLayer)) {
                            currentClipSet = footstepSoundSet[i].footstepClips;
                            break;
                        }
                        else if (i == footstepSoundSet.Count - 1) {
                            currentClipSet = null;
                        }
                    }
                }

                if (currentClipSet != null && currentClipSet.Any()) {
                    playerAudioSource.PlayOneShot(currentClipSet[Random.Range(0, currentClipSet.Count)]);
                }
            }
        }
    }
    #endregion
    
    #region Parkour Functions
    #if SAIO_ENABLE_PARKOUR
    void VaultCheck() {
        if (!isVaulting) {
            if (enableVaultDebugging) { Debug.DrawRay(transform.position-(Vector3.up*(capsule.height/4)), transform.forward*(capsule.radius*2), Color.blue,120);}
            if (Physics.Raycast(transform.position-(Vector3.up*(capsule.height/4)), transform.forward,out VC_Stage1,capsule.radius*2) && VC_Stage1.transform.CompareTag(vaultObjectTag)) {
                float vaultObjAngle = Mathf.Acos(Vector3.Dot(Vector3.up,(Quaternion.LookRotation(VC_Stage1.normal,Vector3.up)*Vector3.up))) * Mathf.Rad2Deg;

                Vector3 origin = -VC_Stage1.normal*0.05f + (VC_Stage1.point + Quaternion.LookRotation(VC_Stage1.normal, Vector3.up) * Vector3.up * maxVaultHeight);
                var dir = -(Quaternion.LookRotation(VC_Stage1.normal, Vector3.up) * Vector3.up);
                if (enableVaultDebugging) {
                    Debug.DrawRay(origin, dir * capsule.height, Color.cyan, 120);
                }
                if (Physics.Raycast(origin, dir, out VC_Stage2, capsule.height) &&
                    VC_Stage2.transform == VC_Stage1.transform && VC_Stage2.point.y <= currentGroundInfo.groundRawYPosition+maxVaultHeight+vaultObjAngle) 
                {
                    vaultForwardVec = -VC_Stage1.normal;

                    if (enableVaultDebugging) {Debug.DrawLine(VC_Stage2.point+(vaultForwardVec*maxVaultDepth)-(Vector3.up*0.01f), (VC_Stage2.point- (Vector3.up*.01f)), Color.red,120   );}
                    if (Physics.Linecast((VC_Stage2.point+(vaultForwardVec*maxVaultDepth))-(Vector3.up*0.01f), VC_Stage2.point - (Vector3.up*0.01f),out VC_Stage3)) {
                        Ray vc4 = new Ray(VC_Stage3.point+(vaultForwardVec*(capsule.radius+(vaultObjAngle*0.01f))),Vector3.down);
                        if (enableVaultDebugging) { Debug.DrawRay(vc4.origin, vc4.direction,Color.green,120);}
                        Physics.SphereCast(vc4,capsule.radius,out VC_Stage4,maxVaultHeight+(capsule.height/2));
                        Vector3 proposedPos = Vector3.right*vc4.origin.x + (Vector3.up*(VC_Stage4.point.y + capsule.height/2 + 0.01f))+(Vector3.forward*vc4.origin.z) + (VC_Stage3.normal*0.02f);

                        if (VC_Stage4.collider && !Physics.CheckCapsule(proposedPos-(Vector3.up*((capsule.height/2)-capsule.radius)), proposedPos+(Vector3.up*((capsule.height/2)-capsule.radius)),capsule.radius)) {
                            isVaulting = true;
                            StopCoroutine("PositionInterp");
                            StartCoroutine(PositionInterp(proposedPos, vaultSpeed));

                        }else if (enableVaultDebugging) {Debug.Log("Cannot Vault this Object. Sufficient space/ground was not found on the other side of the vault object.");}
                    }else if (enableVaultDebugging) {Debug.Log("Cannot Vault this object. Object is too deep or there is an obstruction on the other side.");}
                }if (enableVaultDebugging) {Debug.Log("Vault Object is too high or there is something ontop of the object that is not marked as vaultable.");}

            }

        }else if (!doingPosInterp) {
            isVaulting = false;
        }
    }
    
    IEnumerator PositionInterp(Vector3 pos, float speed) {
        doingPosInterp = true;
        Vector3 vel = p_Rigidbody.velocity;
        p_Rigidbody.useGravity = false;
        p_Rigidbody.velocity = Vector3.zero;
        capsule.enabled = false;
        while (Vector3.Distance(p_Rigidbody.position, pos)>0.01f) {
            p_Rigidbody.velocity = Vector3.zero;
            p_Rigidbody.position = (Vector3.MoveTowards(p_Rigidbody.position, pos,speed*Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
        capsule.enabled = true;
        p_Rigidbody.useGravity = true;
        p_Rigidbody.velocity = vel;
        doingPosInterp = false;
        if (isVaulting) {VaultCheck();}
    }
    #endif
    #endregion

    #region Survival Stat Functions
    public void TickStats() {
        if (currentSurvivalStats.hunger>0) {
            currentSurvivalStats.hunger = Mathf.Clamp(currentSurvivalStats.hunger-(hungerDepletionRate+(isSprinting&&!isIdle ? 0.1f:0)), 0, defaultSurvivalStats.hunger);
            currentSurvivalStats.isStarving = (currentSurvivalStats.hunger<(defaultSurvivalStats.hunger/10));
        }
        if (currentSurvivalStats.hydration>0) {
            currentSurvivalStats.hydration = Mathf.Clamp(currentSurvivalStats.hydration-(hydrationDepletionRate+(isSprinting&&!isIdle ? 0.1f:0)), 0, defaultSurvivalStats.hydration);
            currentSurvivalStats.isDehydrated = (currentSurvivalStats.hydration<(defaultSurvivalStats.hydration/8));
        }
        currentSurvivalStats.hasLowHealth = (currentSurvivalStats.health<(defaultSurvivalStats.health/10));

        StatTickTimer = Time.time + (60/statTickRate);
    }
    public void ImmediateStateChange(float amount, StatSelector stat=StatSelector.Health) {
        switch (stat) {
            case StatSelector.Health:{
                currentSurvivalStats.health = Mathf.Clamp(currentSurvivalStats.health+amount,0,defaultSurvivalStats.health);
                currentSurvivalStats.hasLowHealth = (currentSurvivalStats.health < defaultSurvivalStats.health/10);

            }break;

            case StatSelector.Hunger:{
                currentSurvivalStats.hunger = Mathf.Clamp(currentSurvivalStats.hunger+amount,0,defaultSurvivalStats.hunger);
                currentSurvivalStats.isStarving = (currentSurvivalStats.hunger < defaultSurvivalStats.hunger/10);
            }break;

            case StatSelector.Hydration:{
                currentSurvivalStats.hydration = Mathf.Clamp(currentSurvivalStats.hydration+amount,0,defaultSurvivalStats.hydration);
                currentSurvivalStats.isDehydrated = (currentSurvivalStats.hydration < defaultSurvivalStats.hydration/8);
            }break;
        }
    }
    public void LevelUpStat(float newMaxStatLevel, StatSelector stat = StatSelector.Health, bool refill = true) {
        switch(stat) {
            case StatSelector.Health:{
                defaultSurvivalStats.health = Mathf.Clamp(newMaxStatLevel,0,newMaxStatLevel);
                if (refill) {currentSurvivalStats.health = Mathf.Clamp(newMaxStatLevel,0,newMaxStatLevel);}
                currentSurvivalStats.hasLowHealth = (currentSurvivalStats.health<(defaultSurvivalStats.health/10));

            }break;
            case StatSelector.Hunger:{
                defaultSurvivalStats.hunger = Mathf.Clamp(newMaxStatLevel,0,newMaxStatLevel);
                if (refill) {currentSurvivalStats.hunger = Mathf.Clamp(newMaxStatLevel,0,newMaxStatLevel);}
                currentSurvivalStats.isStarving = (currentSurvivalStats.hunger<(defaultSurvivalStats.hunger/10));

            }break;
            case StatSelector.Hydration:{
                defaultSurvivalStats.hydration = Mathf.Clamp(newMaxStatLevel,0,newMaxStatLevel);
                if (refill) {currentSurvivalStats.hydration = Mathf.Clamp(newMaxStatLevel,0,newMaxStatLevel);}
                currentSurvivalStats.isDehydrated = (currentSurvivalStats.hydration<(defaultSurvivalStats.hydration/8));

            }break;
        }
    }
    
    #endregion

    #region Animator Update
    void UpdateAnimationTriggers(bool zeroOut = false) {
        switch (cameraPerspective) {
            case PerspectiveModes._1stPerson:{
                if (_1stPersonCharacterAnimator) {
                    //Setup Fistperson animation triggers here.

                }
            }break;
            
            case PerspectiveModes._3rdPerson:{
                if (_3rdPersonCharacterAnimator) {
                    if (stickRendererToCapsuleBottom) {
                        _3rdPersonCharacterAnimator.transform.position =
                            Vector3.right   * _3rdPersonCharacterAnimator.transform.position.x +
                            Vector3.up      * (transform.position.y - capsule.height/2f) +
                            Vector3.forward * _3rdPersonCharacterAnimator.transform.position.z;
                    }
                    if (!zeroOut) {
                        //Setup Third-person animation triggers here.
                        if (a_velocity != "") {
                            _3rdPersonCharacterAnimator.SetFloat(a_velocity, p_Rigidbody.linearVelocity.sqrMagnitude);    
                        }
                        if (a_2DVelocity != "") {
                            _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, _2DVelocity.magnitude); 
                        }
                        if (a_Idle != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Idle,isIdle); 
                        }
                        if (a_Sprinting != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Sprinting,isSprinting);   
                        }
                        if (a_Crouching != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Crouching,isCrouching);   
                        }
                        if (a_Sliding != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Sliding,isSliding);   
                        }
                        if (a_Jumped != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Jumped,jumped);   
                        }
                        if (a_Grounded != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Grounded, currentGroundInfo.isInContactWithGround); 
                        }
                    } else { 
                        if (a_velocity != "") {
                            _3rdPersonCharacterAnimator.SetFloat(a_velocity, 0);    
                        }
                        if (a_2DVelocity != "") {
                            _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, 0); 
                        }
                        if (a_Idle != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Idle,true); 
                        }
                        if (a_Sprinting != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Sprinting,false);   
                        }
                        if (a_Crouching != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Crouching,false);   
                        }
                        if (a_Sliding != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Sliding,false);   
                        }
                        if (a_Jumped != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Jumped,false);   
                        }
                        if (a_Grounded != "") {
                            _3rdPersonCharacterAnimator.SetBool(a_Grounded, true); 
                        }
                    }
                    
                }

            }break;
        }
    }
    #endregion

    #region Interactables

    public bool TryInteract() {
        if (cameraPerspective == PerspectiveModes._3rdPerson) {
            Collider[] cols = Physics.OverlapBox(transform.position + transform.forward*(interactRange / 2),
                Vector3.one * (interactRange / 2), transform.rotation, interactableLayer, QueryTriggerInteraction.Ignore);
            IInteractable interactable = null;
            float lastColestDist = 100;
            foreach (Collider c in cols) {
                IInteractable i = c.GetComponent<IInteractable>();
                if (i != null) {
                    float d = Vector3.Distance(transform.position, c.transform.position);
                    if (d < lastColestDist) {
                        lastColestDist = d;
                        interactable = i;
                    }
                }
            }

            return (interactable != null && interactable.Interact());
        }

        // camera is first-person
        if (Physics.SphereCast(playerCamera.transform.position, 0.25f, playerCamera.transform.forward, out var h, interactRange, interactableLayer, QueryTriggerInteraction.Ignore)) {
            IInteractable i = h.collider.GetComponent<IInteractable>();
            if (i != null) { return i.Interact(); }
        }

        return false;
    }

    #endregion

    #region Gizmos
    #if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (enableGroundingDebugging) {
            if (Application.isPlaying) {

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position-(Vector3.up*((capsule.height/2)-(capsule.radius+0.1f))),capsule.radius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position-(Vector3.up*((capsule.height/2)-(capsule.radius-0.5f))),capsule.radius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(new Vector3(transform.position.x,currentGroundInfo.playerGroundPosition,transform.position.z),0.05f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(new Vector3(transform.position.x,currentGroundInfo.groundRawYPosition,transform.position.z),0.05f);
                Gizmos.color = Color.green;
                
            }
        
        }

        #if SAIO_ENABLE_PARKOUR
        if (enableVaultDebugging &&Application.isPlaying) {
            Gizmos.DrawWireSphere(VC_Stage3.point+(vaultForwardVec*(capsule.radius)),capsule.radius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(VC_Stage4.point,capsule.radius);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(((Vector3.right*(VC_Stage3.point+(vaultForwardVec*(capsule.radius))).x)+(Vector3.up*(VC_Stage4.point.y+(capsule.height/2)+0.01f))+(Vector3.forward*(VC_Stage3.point+(vaultForwardVec*(capsule.radius))).z)),capsule.radius);
        }
        #endif
    }
    #endif
    #endregion
    
    public void PausePlayer(PauseModes pauseMode) {
        controllerPaused = true;
        switch(pauseMode) {
            case PauseModes.MakeKinematic:{
                p_Rigidbody.isKinematic = true;
            }break;
            
            case PauseModes.FreezeInPlace:{
                 p_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }break;

            case PauseModes.BlockInputOnly:{

            }break;
        }
       
        p_Rigidbody.linearVelocity = Vector3.zero;
        InputDir = Vector2.zero;
        MovInput = Vector2.zero;
        MovInput_Smoothed = Vector2.zero;
        capsule.sharedMaterial = _MaxFriction;
        
        UpdateAnimationTriggers(true);
        if (a_velocity != "") {
                _3rdPersonCharacterAnimator.SetFloat(a_velocity, 0);   
        }
    }
    public void UnpausePlayer(float delay = 0) {
        if (delay==0) {
            controllerPaused = false;
            p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            p_Rigidbody.isKinematic = false;
        }
        else{
            StartCoroutine(UnpausePlayerI(delay));
        }
    }
    IEnumerator UnpausePlayerI(float delay) {
        yield return new WaitForSecondsRealtime(delay);
        controllerPaused = false;
        p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        p_Rigidbody.isKinematic = false;
    }

}


#region Classes and Enums
[System.Serializable]
public class GroundInfo {
    public bool isInContactWithGround, isGettingGroundInfo, potentialStair;
    public float groundAngleMultiplier_Inverse = 1, groundAngleMultiplier_Inverse_persistent = 1, groundAngleMultiplier = 0, groundAngle, groundAngle_Raw, playerGroundPosition, groundRawYPosition;
    public Vector3 groundInfluenceDirection, groundNormal_Averaged, groundNormal_Raw;

    public List<Vector3> groundNormals_lowGrade;
    public List<Vector3> groundNormals_highGrade;
    public string groundTag;
    public Material groundMaterial;
    public TerrainLayer groundLayer;
    public PhysicsMaterial groundPhysicMaterial;
    internal Terrain currentTerrain;
    internal Mesh currentMesh;
    internal RaycastHit groundFromRay, stairCheck_RiserCheck, stairCheck_HeightCheck;
    internal RaycastHit[] groundFromSweep;
}

[System.Serializable]
public class GroundMaterialProfile{
    public MatProfileType profileTriggerType = MatProfileType.Material;
    public List<Material> _Materials;
    public List<PhysicsMaterial> _physicMaterials;
    public List<TerrainLayer> _Layers;
    public List<AudioClip> footstepClips = new List<AudioClip>();
}

[System.Serializable]
public class SurvivalStats {
    public float health = 250.0f;
    public float hunger = 100.0f;
    public float hydration = 100f;
    public bool hasLowHealth, isStarving, isDehydrated;
}

public enum StatSelector { Health, Hunger, Hydration }
public enum MatProfileType { Material, TerrainLayer, PhysicMaterial }
public enum FootstepTriggeringMode { CalculatedTiming, CalledFromAnimations }
public enum PerspectiveModes { _1stPerson, _3rdPerson }
public enum ViewInputModes { Traditional, Retro }
public enum MouseInputInversionModes { None, X, Y, Both }
public enum GroundSpeedProfiles { Crouching, Walking, Sprinting, Sliding }
public enum Stances { Standing, Crouching }
public enum PauseModes { MakeKinematic, FreezeInPlace, BlockInputOnly }
#endregion

#region Interfaces
public interface IInteractable{
    bool Interact();
}

public interface ICollectable{
    void Collect();
}
#endregion


#region Editor Scripting
#if UNITY_EDITOR
[CustomEditor(typeof(SUPERCharacterAIO))]
public class SuperFPEditor : Editor{
    Color32 statBackingColor = new Color32(64,64,64,255);
    
    GUIStyle labelHeaderStyle;
    GUIStyle l_scriptHeaderStyle;
    GUIStyle labelSubHeaderStyle;
    GUIStyle clipSetLabelStyle;
    GUIStyle SupportButtonStyle;
    GUIStyle ShowMoreStyle;
    GUIStyle BoxPanel;
    Texture2D BoxPanelColor;
    SUPERCharacterAIO t;
    SerializedObject tSO, SurvivalStatsTSO;
    SerializedProperty interactableLayer, obstructionMaskField, groundLayerMask, groundMatProf, defaultSurvivalStats, currentSurvivalStats;
    static bool cameraSettingsFoldout = false, movementSettingFoldout = false, survivalStatsFoldout, footStepFoldout = false;

    public void OnEnable() {
        t = (SUPERCharacterAIO)target;
        tSO = new SerializedObject(t);
        SurvivalStatsTSO = new SerializedObject(t);
        obstructionMaskField = tSO.FindProperty("cameraObstructionIgnore");
        groundLayerMask = tSO.FindProperty("whatIsGround");
        groundMatProf = tSO.FindProperty("footstepSoundSet");
        interactableLayer = tSO.FindProperty("interactableLayer"); 
        BoxPanelColor= new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        BoxPanelColor.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.2f));
        BoxPanelColor.Apply();
    }

    void HandleGuiChange(string undoMessage) {
        if (!GUI.changed) return;
        EditorUtility.SetDirty(t); 
        Undo.RecordObject(t, undoMessage); 
        tSO.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI() {
        
        #region Style Null Check
        labelHeaderStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 };
        l_scriptHeaderStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 16};
        labelSubHeaderStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 10, richText = true};
        ShowMoreStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, margin = new RectOffset(15,0,0,0), fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
        clipSetLabelStyle = labelSubHeaderStyle ?? new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
        SupportButtonStyle ??= new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 10, richText = true };
        BoxPanel ??= new GUIStyle(GUI.skin.box) { normal = { background = BoxPanelColor } };
        #endregion

        #region PlaymodeWarning

        string warningMessage =
            "It is recommended you switch to another GameObject's inspector, as updates to this inspector panel during playmode " +
            "can cause lag in the rigidbody calculations and cause unwanted adverse effects to gameplay. \n\n" +
            "Please note this is NOT an issue in application builds.";
        if (Application.isPlaying) {
            EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
        }
        #endregion

        #region Label  
        EditorGUILayout.Space();
        GUILayout.Label("<b><i><size=18><color=#FC80A5>S</color><color=#FFFF9F>U</color><color=#99FF99>P</color><color=#76D7EA>E</color><color=#BF8FCC>R</color></size></i></b> <size=12><i>Character Controller</i></size>",l_scriptHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space();
        #endregion

        #region Camera Settings
        GUILayout.Label("Camera Settings", labelHeaderStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(BoxPanel);
        t.enableCameraControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Control","Should the player have control over the camera?"), t.enableCameraControl);
        t.playerCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Player Camera", "The Camera Attached to the Player."), t.playerCamera,typeof(Camera),true);
        t.cameraHolder = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Camera Holder", "The Game Object Holding the Camera."), t.cameraHolder,typeof(GameObject),true);
        t.cameraPerspective = (PerspectiveModes)EditorGUILayout.EnumPopup(new GUIContent("Camera Perspective Mode", "The current perspective of the character."), t.cameraPerspective);
        
        if (cameraSettingsFoldout) {
            t.automaticallySwitchPerspective = EditorGUILayout.ToggleLeft(
                new GUIContent("Automatically Switch Perspective",
                    "Should the Camera perspective mode automatically change based on the distance between the camera and the character's head?"),
                t.automaticallySwitchPerspective);
            GUIContent perspectiveSwitchingGuiContent = new("Perspective Switch Key",
                "The keyboard key used to switch perspective modes. Set to none if you do not wish to allow perspective switching");
            #if ENABLE_INPUT_SYSTEM
            t.perspectiveSwitchingKey = (Key)EditorGUILayout.EnumPopup(perspectiveSwitchingGuiContent, t.perspectiveSwitchingKey);
            #else
            if (!t.automaticallySwitchPerspective) {
                t.perspectiveSwitchingKey_L = (KeyCode)EditorGUILayout.EnumPopup(perspectiveSwitchingGuiContent, t.perspectiveSwitchingKey_L);
            }
            #endif
            t.mouseInputInversion = (MouseInputInversionModes)EditorGUILayout.EnumPopup(new GUIContent("Mouse Input Inversion", "Which axes of the mouse input should be inverted if any?"), t.mouseInputInversion);
            t.sensitivity = EditorGUILayout.Slider(new GUIContent("Mouse Sensitivity", "Sensitivity of the mouse"), t.sensitivity,1,20);
            t.rotationWeight = EditorGUILayout.Slider(new GUIContent("Camera Weight", "How heavy should the camera feel?"), t.rotationWeight, 1,25);
            t.verticalRotationRange =EditorGUILayout.Slider(new GUIContent("Vertical Rotation Range", "The vertical angle range (In degrees) that the camera is allowed to move in"), t.verticalRotationRange,1,180);
  
            t.lockAndHideMouse = EditorGUILayout.ToggleLeft(new GUIContent("Lock and Hide mouse Cursor", "Should the controller lock and hide the cursor?"), t.lockAndHideMouse);
            t.autoGenerateCrosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Generate Crosshair", "Should the controller automatically generate a crosshair?"), t.autoGenerateCrosshair);
            GUI.enabled = t.autoGenerateCrosshair;
            t.crosshairSprite = (Sprite)EditorGUILayout.ObjectField(new GUIContent("Crosshair Sprite", "The Sprite the controller will use when generating a crosshair."), t.crosshairSprite, typeof(Sprite),false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            t.showCrosshairIn3rdPerson = EditorGUILayout.ToggleLeft(new GUIContent("Show Crosshair in 3rd person?", "Should the controller show the crosshair in 3rd person?"), t.showCrosshairIn3rdPerson);
            GUI.enabled = true;
            t.drawPrimitiveUI = EditorGUILayout.ToggleLeft(new GUIContent("Draw Primitive UI", "Should the controller automatically generate and draw primitive stat UI?"), t.drawPrimitiveUI);
            EditorGUILayout.Space(20);

            if (t.cameraPerspective == PerspectiveModes._1stPerson) {
                t.viewInputMethods = (ViewInputModes)EditorGUILayout.EnumPopup(new GUIContent("Camera Input Methods", "The input method used to rotate the camera."), t.viewInputMethods);
                t.standingEyeHeight = EditorGUILayout.Slider(new GUIContent("Standing Eye Height", "The Eye height of the player measured from the center of the character's capsule and upwards."), t.standingEyeHeight,0,1);
                t.crouchingEyeHeight = EditorGUILayout.Slider(new GUIContent("Crouching Eye Height", "The Eye height of the player measured from the center of the character's capsule and upwards."), t.crouchingEyeHeight,0,1);
                t.fovKickAmount = EditorGUILayout.Slider(new GUIContent("FOV Kick Amount", "How much should the camera's FOV change based on the current movement speed?"), t.fovKickAmount,0,50);
                t.fovSensitivityMultiplier = EditorGUILayout.Slider(new GUIContent("FOV Sensitivity Multiplier", "How much should the camera's FOV effect the mouse sensitivity? (Lower FOV = less sensitive)"), t.fovSensitivityMultiplier,0,1);
            } else { 
                t.rotateCharacterToCameraForward = EditorGUILayout.ToggleLeft(new GUIContent("Rotate Ungrounded Character to Camera Forward", "Should the character get rotated towards the camera's forward facing direction when mid air?"), t.rotateCharacterToCameraForward);
                t.standingEyeHeight = EditorGUILayout.Slider(new GUIContent("Head Height", "The Head height of the player measured from the center of the character's capsule and upwards."), t.standingEyeHeight,0,1);  
                t.maxCameraDistance = EditorGUILayout.Slider(new GUIContent("Max Camera Distance", "The farthest distance the camera is allowed to hover from the character's head"), t.maxCameraDistance,0,15);
                t.cameraZoomSensitivity = EditorGUILayout.Slider(new GUIContent("Camera Zoom Sensitivity", "How sensitive should the mouse scroll wheel be when zooming the camera in and out?"), t.cameraZoomSensitivity, 1,5);
                t.bodyCatchupSpeed = EditorGUILayout.Slider(new GUIContent("Body Mesh Alignment Speed","How quickly will the body align itself with the camera's relative direction"), t.bodyCatchupSpeed, 0, 5);
                t.inputResponseFiltering = EditorGUILayout.Slider(new GUIContent("Input Response Filtering","How quickly will the internal input direction align itself the player's input"), t.inputResponseFiltering, 0, 5);
                EditorGUILayout.PropertyField(obstructionMaskField,new GUIContent("Camera Obstruction Layers", "The Layers the camera will register as an obstruction and move in front of ."));
            }
        }
        EditorGUILayout.Space();
        cameraSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(cameraSettingsFoldout,cameraSettingsFoldout ? "<color=#B83C82>show less</color>" : "<color=#B83C82>show more</color>", ShowMoreStyle);
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.EndVertical();
        HandleGuiChange("Undo Camera Setting changes");
        #endregion
    
        #region Movement Settings

        EditorGUILayout.Space(); EditorGUILayout.LabelField("",GUI.skin.horizontalSlider,GUILayout.MaxHeight(6)); EditorGUILayout.Space();
        GUILayout.Label("Movement Settings",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space(20);

        EditorGUILayout.BeginVertical(BoxPanel);
        if (movementSettingFoldout) {
            #region Stances and Speed
            t.enableMovementControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Movement","Should the player have control over the character's movement?"), t.enableMovementControl);
            GUILayout.Label("<color=grey>Stances and Speed</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(BoxPanel);
            EditorGUILayout.Space(15);
            
            GUI.enabled = false;
            t.currentGroundMovementSpeed = (GroundSpeedProfiles)EditorGUILayout.EnumPopup(new GUIContent("Current Movement Speed", "Displays the player's current movement speed"), t.currentGroundMovementSpeed);
            GUI.enabled = true;

            EditorGUILayout.Space();
            t.walkingSpeed = EditorGUILayout.Slider(new GUIContent("Walking Speed", "How quickly can the player move while walking?"), t.walkingSpeed,1,400);

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(BoxPanel);
            t.canSprint = EditorGUILayout.ToggleLeft(new GUIContent("Can Sprint", "Is the player allowed to enter a sprint?"), t.canSprint);
            GUI.enabled = t.canSprint;
            t.toggleSprint = EditorGUILayout.ToggleLeft(new GUIContent("Toggle Sprint", "Should the spring key act as a toggle?"), t.toggleSprint);
            #if ENABLE_INPUT_SYSTEM
            t.sprintKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key", "The Key used to enter a sprint."), t.sprintKey);
            #else
            t.sprintKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Sprint Key", "The Key used to enter a sprint."), t.sprintKey_L);
            #endif
            t.sprintingSpeed = EditorGUILayout.Slider(new GUIContent("Sprinting Speed", "How quickly can the player move while sprinting?"), t.sprintingSpeed, t.walkingSpeed+1,650);
            t.decelerationSpeed = EditorGUILayout.Slider(new GUIContent("Deceleration Factor", "Behaves somewhat like a braking force"), t.decelerationSpeed,1,300);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(BoxPanel);
            t.canCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Can Crouch", "Is the player allowed to crouch?"), t.canCrouch);
            GUI.enabled = t.canCrouch;
            t.toggleCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Toggle Crouch", "Should pressing the crouch button act as a toggle?"), t.toggleCrouch);
            #if ENABLE_INPUT_SYSTEM
            t.crouchKey= (Key)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "The Key used to start a crouch."), t.crouchKey);
            #else
            t.crouchKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "The Key used to start a crouch."), t.crouchKey_L);
            #endif
            t.crouchingSpeed = EditorGUILayout.Slider(new GUIContent("Crouching Speed", "How quickly can the player move while crouching?"), t.crouchingSpeed, 1, t.walkingSpeed-1);
            t.crouchingHeight = EditorGUILayout.Slider(new GUIContent("Crouching Height", "How small should the character's capsule collider be when crouching?"), t.crouchingHeight,0.01f,2);
            EditorGUILayout.EndVertical();
        
            GUI.enabled = true;

            
            EditorGUILayout.Space(20);
            GUI.enabled = false;
            t.currentStance = (Stances)EditorGUILayout.EnumPopup(new GUIContent("Current Stance", "Displays the character's current stance"), t.currentStance);
            GUI.enabled = true;
            t.stanceTransitionSpeed = EditorGUILayout.Slider(new GUIContent("Stance Transition Speed", "How quickly should the character change stances?"), t.stanceTransitionSpeed,0.1f, 10);

            EditorGUILayout.PropertyField(groundLayerMask, new GUIContent("What Is Ground", "What physics layers should be considered to be ground?"));

            #region Slope affectors
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(BoxPanel);
            GUILayout.Label("<color=grey>Slope Affectors</color>",new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleLeft,fontSize = 10, richText = true},GUILayout.ExpandWidth(true));

            t.hardSlopeLimit = EditorGUILayout.Slider(new GUIContent("Hard Slope Limit", "At what slope angle should the player no longer be able to walk up?"), t.hardSlopeLimit,45, 89);
            t.maxStairRise = EditorGUILayout.Slider(new GUIContent("Maximum Stair Rise", "How tall can a single stair rise?"), t.maxStairRise,0,1.5f);
            t.stepUpSpeed = EditorGUILayout.Slider(new GUIContent("Step Up Speed", "How quickly will the player climb a step?"), t.stepUpSpeed,0.01f,0.45f);
            EditorGUILayout.EndVertical();
            #endregion
            EditorGUILayout.EndVertical();
            #endregion

            #region Jumping
            EditorGUILayout.Space();
            GUILayout.Label("<color=grey>Jumping Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(BoxPanel);
            //EditorGUILayout.Space(15);

            t.canJump = EditorGUILayout.ToggleLeft(new GUIContent("Can Jump", "Is the player allowed to jump?"), t.canJump);
            GUI.enabled = t.canJump;
            #if ENABLE_INPUT_SYSTEM
            t.jumpKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "The Key used to jump."), t.jumpKey);
            #else
            t.jumpKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "The Key used to jump."), t.jumpKey_L);
            #endif
            t.holdJump = EditorGUILayout.ToggleLeft(new GUIContent("Continuous Jumping", "Should the player be able to continue jumping without letting go of the Jump key"), t.holdJump);
            t.jumpPower = EditorGUILayout.Slider(new GUIContent("Jump Power", "How much power should a jump have?"), t.jumpPower,1,650f);
            t.airControlFactor = EditorGUILayout.Slider(new GUIContent("Air Control Factor", "EXPERIMENTAL: How much control should the player have over their direction while in the air"), t.airControlFactor,0,1);
            GUI.enabled = t.enableStaminaSystem;
                t.jumpingDepletesStamina = EditorGUILayout.ToggleLeft(new GUIContent("Jumping Depletes Stamina", "Should jumping deplete stamina?"), t.jumpingDepletesStamina);
                t.s_JumpStaminaDepletion = EditorGUILayout.Slider(new GUIContent("Jump Stamina Depletion Amount", "How much stamina should jumping use?"), t.s_JumpStaminaDepletion, 0, t.stamina);
            GUI.enabled = true;
            t.jumpEnhancements = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump Enhancements","Should extra math be used to enhance the jump curve?"), t.jumpEnhancements);
            if (t.jumpEnhancements) {
                t.descentMultiplier = EditorGUILayout.Slider(new GUIContent("On Decent Multiplier","When the player begins to descend  during a jump, what should gravity be multiplied by?"), t.descentMultiplier, 0.1f,5);
                t.tapJumpMultiplier = EditorGUILayout.Slider(new GUIContent("Tap Jump Multiplier","When the player lets go of space prematurely during a jump, what should gravity be multiplied by?"), t.tapJumpMultiplier, 0.1f,5);
            }

            EditorGUILayout.EndVertical();
            #endregion

            #region Sliding
            EditorGUILayout.Space();
            GUILayout.Label("<color=grey>Sliding Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(BoxPanel);
            //EditorGUILayout.Space(15);

            t.canSlide = EditorGUILayout.ToggleLeft(new GUIContent("Can Slide", "Is the player allowed to slide? Use the crouch key to initiate a slide!"), t.canSlide);
            GUI.enabled = t.canSlide;
            #if ENABLE_INPUT_SYSTEM
            t.slideKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Slide Key", "The Key used to Slide while the character is sprinting."), t.slideKey);
            #else
            t.slideKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Slide Key", "The Key used to Slide wile the character is sprinting."), t.slideKey_L);
            #endif
            t.slidingDeceleration = EditorGUILayout.Slider(new GUIContent("Sliding Deceleration", "How much deceleration should be applied while sliding?"), t.slidingDeceleration, 50,300);
            t.slidingTransitionSpeed = EditorGUILayout.Slider(new GUIContent("Sliding Transition Speed", "How quickly should the character transition from the current stance to sliding?"), t.slidingTransitionSpeed,0.01f,10);
            t.maxFlatSlideDistance = EditorGUILayout.Slider(new GUIContent("Flat Slide Distance", "If the player starts sliding on a flat surface with no ground angle influence, How many units should the player slide forward?"), t.maxFlatSlideDistance, 0.5f,15);
            GUI.enabled = true;
            EditorGUILayout.EndVertical();
            #endregion
            
            HandleGuiChange("Undo Movement Setting changes");
        } else { 
            t.enableMovementControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Movement","Should the player have control over the character's movement?"), t.enableMovementControl);
            t.walkingSpeed = EditorGUILayout.Slider(new GUIContent("Walking Speed", "How quickly can the player move while walking?"), t.walkingSpeed,1,400);
            t.sprintingSpeed = EditorGUILayout.Slider(new GUIContent("Sprinting Speed", "How quickly can the player move while sprinting?"), t.sprintingSpeed, t.walkingSpeed+1,650);
        }
        EditorGUILayout.Space();
        movementSettingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(movementSettingFoldout,movementSettingFoldout ?  "<color=#B83C82>show less</color>" : "<color=#B83C82>show more</color>", ShowMoreStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndFoldoutHeaderGroup();
        #endregion

        #if SAIO_ENABLE_PARKOUR
        #region Parkour Settings
        EditorGUILayout.Space(); EditorGUILayout.LabelField("",GUI.skin.horizontalSlider,GUILayout.MaxHeight(6)); EditorGUILayout.Space();
        GUILayout.Label("Parkour Settings",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space(20);
        
        #region Vault
        EditorGUILayout.Space();
        GUILayout.Label("<color=grey>Vaulting Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.BeginVertical(BoxPanel);

        t.canVault = EditorGUILayout.ToggleLeft(new GUIContent("Can Vault", "Is the player allowed to vault objects?"), t.canVault);
        GUI.enabled = t.canVault;
        t.autoVaultWhenSpringing = EditorGUILayout.ToggleLeft(new GUIContent("Auto Vault While Spriting", "Should the controller automatically vault objects while sprinting?"), t.autoVaultWhenSpringing);
        if (!t.autoVaultWhenSpringing) {
            #if ENABLE_INPUT_SYSTEM
            t.VaultKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Vault Key", "The Key used to to vault an object"), t.VaultKey);
            #else
            t.VaultKey_L = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Vault Key", "The Key used to to vault an object"), t.VaultKey_L);
            #endif
        }
        t.vaultObjectTag = EditorGUILayout.TagField(new GUIContent("Vault Object Tag", "The tag required on an object to be considered vaultable."), t.vaultObjectTag);
        t.vaultSpeed = EditorGUILayout.Slider(new GUIContent("Vault Speed", "How quickly can the player vault an object?"), t.vaultSpeed, 0.1f, 15);
        t.maxVaultDepth = EditorGUILayout.Slider(new GUIContent("Maximum Vault Depth", "How deep (in meters) can a vaultable object be before it's no longer considered vaultable?"), t.maxVaultDepth, 0.1f, 3);
        t.maxVaultHeight = EditorGUILayout.Slider(new GUIContent("Maximum Vault Height", "How Tall (in meters) can a vaultable object be before it's no longer considered vaultable?"), t.maxVaultHeight, 0.1f, 3);
        EditorGUILayout.EndVertical();
        
        HandleGuiChange("Undo Vault Setting changes");
        #endregion

        #endregion
        #endif

        #region Stamina
        EditorGUILayout.Space(); 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider,GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space();
        GUILayout.Label("Stamina",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(BoxPanel);
        t.enableStaminaSystem = EditorGUILayout.ToggleLeft(new GUIContent("Enable Stamina System", "Should the controller enable its stamina system?"), t.enableStaminaSystem);

        //preview bar
        Rect casingRectSP = EditorGUILayout.GetControlRect();
        Rect statRectSP = new Rect(casingRectSP.x + 2, casingRectSP.y + 2,
            Mathf.Clamp(casingRectSP.width / t.stamina * t.currentStaminaLevel - 4, 0, casingRectSP.width),
            casingRectSP.height - 4);
        Rect statRectMSP = new Rect(casingRectSP.x + 2, casingRectSP.y + 2,
                Mathf.Clamp(casingRectSP.width / t.stamina * t.s_minimumStaminaToSprint - 4, 0, casingRectSP.width),
                casingRectSP.height - 4);
        EditorGUI.DrawRect(casingRectSP,statBackingColor);
        EditorGUI.DrawRect(statRectMSP,new Color32(96,96,64,255));
        EditorGUI.DrawRect(statRectSP,new Color32(94,118,135,(byte)(GUI.enabled? 191:64)));
       
        
        GUI.enabled = t.enableStaminaSystem;
        t.stamina = EditorGUILayout.Slider(new GUIContent("Stamina", "The maximum stamina level"), t.stamina, 0, 250.0f);
        t.s_minimumStaminaToSprint = EditorGUILayout.Slider(new GUIContent("Minimum Stamina To Sprint", "The minimum stamina required to enter a sprint."), t.s_minimumStaminaToSprint,0, t.stamina);
        t.s_depletionSpeed = EditorGUILayout.Slider(new GUIContent("Depletion Speed", "The speed at which stamina will deplete while sprinting."), t.s_depletionSpeed, 0, 15.0f);
        t.s_regenerationSpeed = EditorGUILayout.Slider(new GUIContent("Regeneration Speed", "The speed at which stamina will regenerate"), t.s_regenerationSpeed, 0, 10.0f);
       
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        HandleGuiChange("Undo Stamina Setting changes");
        #endregion

        #region Footstep Audio
        EditorGUILayout.Space(); 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space();
        GUILayout.Label("Footstep Audio",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(BoxPanel);
        
        t.enableFootstepSounds = EditorGUILayout.ToggleLeft(new GUIContent("Enable Footstep System", "Should the controller enable its footstep audio systems?"), t.enableFootstepSounds);
        GUI.enabled = t.enableFootstepSounds;
        string popupMessage = "How should a footstep SFX call be triggered? \n\n" +
                              "- Calculated Timing: The controller will attempt to calculate the footstep cycle position based on head bob cycle position, " +
                              "movement speed, and capsule size. This can sometimes be inaccurate depending on the selected perspective and base walk speed. " +
                              "(Not recommended if character animations are being used)\n\n" +
                              "- Called From Animations: The controller will not do its own footstep cycle calculations/call for SFX. " +
                              "Instead the controller will rely on character Animations to call the 'CallFootstepClip()' function. " +
                              "This gives much more precise results. The controller will still calculate what footstep clips should be played.";
        t.footstepTriggeringMode = (FootstepTriggeringMode)EditorGUILayout.EnumPopup(
            new GUIContent("Footstep Trigger Mode", popupMessage), t.footstepTriggeringMode);
        
        if (t.footstepTriggeringMode == FootstepTriggeringMode.CalculatedTiming) {
            t.stepTiming = EditorGUILayout.Slider(new GUIContent("Step Timing", "The time (measured in seconds) between each footstep."), t.stepTiming, 0.0f, 1.0f);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.Space();
        EditorGUI.indentLevel++;
        footStepFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(footStepFoldout, $"<color=#B83C82>{(footStepFoldout ? "hide" : "show")} clip stacks</color>", ShowMoreStyle);
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUI.indentLevel--;
        if (footStepFoldout) {
            if (t.footstepSoundSet.Any()) {
                if (!Application.isPlaying) {
                    for (int i = 0; i< groundMatProf.arraySize; i++) {
                        EditorGUILayout.BeginVertical(BoxPanel);
                        EditorGUILayout.BeginVertical(BoxPanel);

                        SerializedProperty profile = groundMatProf.GetArrayElementAtIndex(i),
                            clipList = profile.FindPropertyRelative("footstepClips"),
                            mat = profile.FindPropertyRelative("_Materials"),
                            physMat = profile.FindPropertyRelative("_physicMaterials"),
                            layer = profile.FindPropertyRelative("_Layers"),
                            triggerType = profile.FindPropertyRelative("profileTriggerType");
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Clip Stack {i+1}", clipSetLabelStyle);
                        if (GUILayout.Button(new GUIContent("X", "Remove this profile"),GUILayout.MaxWidth(20))) {t.footstepSoundSet.RemoveAt(i);UpdateGroundProfiles(); break;}
                        EditorGUILayout.EndHorizontal();
                        
                        //Check again that the list of profiles isn't empty in case we removed the last one with the button above.
                        if (t.footstepSoundSet.Any()) {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(triggerType,new GUIContent("Trigger Mode", "Is this clip stack triggered by a Material or a Terrain Layer?"));
                            switch(t.footstepSoundSet[i].profileTriggerType) {
                                case MatProfileType.Material:{EditorGUILayout.PropertyField(mat, new GUIContent("Materials", "The materials used to trigger this footstep stack"));}break;
                                case MatProfileType.PhysicMaterial:{EditorGUILayout.PropertyField(physMat, new GUIContent("Physic Materials", "The Physic Materials used to trigger this footstep stack"));}break;
                                case MatProfileType.TerrainLayer:{EditorGUILayout.PropertyField(layer, new GUIContent("Terrain Layers", "The Terrain Layers used to trigger this footstep stack"));}break;
                            }
                            EditorGUILayout.Space();

                            EditorGUILayout.PropertyField(clipList, new GUIContent("Clip Stack", "The Audio clips used in this stack"), true);
                            EditorGUI.indentLevel--;
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space();
                            HandleGuiChange($"Undo changes to Clip Stack {i + 1}");
                        }
                    }
                } else { 
                    EditorGUILayout.HelpBox("Footstep sound sets hidden to save runtime resources.", MessageType.Info);
                }
            }

            if (GUILayout.Button(new GUIContent("Add Profile", "Add new profile"))) {
                t.footstepSoundSet.Add(new GroundMaterialProfile{
                    profileTriggerType = MatProfileType.Material,
                    _Materials = null,
                    _Layers = null,
                    footstepClips = new List<AudioClip>()
                });
                UpdateGroundProfiles();
            }
            if (GUILayout.Button(new GUIContent("Remove All Profiles", "Remove all profiles"))) {
                t.footstepSoundSet.Clear();
            }
            EditorGUILayout.Space();
            footStepFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(footStepFoldout, $"<color=#B83C82>{(footStepFoldout ? "hide" : "show")} clip stacks</color>", ShowMoreStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        GUI.enabled = true;
        EditorGUILayout.HelpBox("Due to limitations, in order to use the Material trigger mode, " +
                                "imported meshes must have Read/Write enabled. " +
                                "Additionally, these meshes cannot be marked as Batching Static. " +
                                "Workarounds for both of these limitations are being researched.",
            MessageType.Info);
        EditorGUILayout.EndVertical();
        HandleGuiChange("Undo Footstep Audio Setting changes");

        #endregion
    
        #region Headbob
        EditorGUILayout.Space(); 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space();
        GUILayout.Label("Head Bob", labelHeaderStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(BoxPanel);

        t.enableHeadBob = EditorGUILayout.ToggleLeft(new GUIContent("Enable Head Bobbing", "Should the controller enable its head-bobbing systems?"), t.enableHeadBob);
        GUI.enabled = t.enableHeadBob;
        t.headBobSpeed = EditorGUILayout.Slider(new GUIContent("Head Bob Speed", "How fast does the head bob sway?"), t.headBobSpeed, 1.0f, 5.0f);
        t.headBobPower = EditorGUILayout.Slider(new GUIContent("Head Bob Power", "How far does the head bob sway?"), t.headBobPower,1.0f,5.0f);
        t.zTilt = EditorGUILayout.Slider(new GUIContent("Head Bob Tilt", "How much does the head bob tilt at the sway extreme?"), t.zTilt, 0.0f, 5.0f);
        
        GUI.enabled = true;
        EditorGUILayout.EndVertical();
        HandleGuiChange("Undo Head Bob Setting changes");
        #endregion

        #region Survival Stats
        EditorGUILayout.Space(); 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space();
        GUILayout.Label("Survival Stats",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space(10);

        SurvivalStatsTSO = new SerializedObject(t);
        defaultSurvivalStats = SurvivalStatsTSO.FindProperty("defaultSurvivalStats");
        currentSurvivalStats = SurvivalStatsTSO.FindProperty("currentSurvivalStats");
        
            #region Basic settings
            EditorGUILayout.BeginVertical(BoxPanel);
            GUILayout.Label("<color=grey>Basic Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
            t.enableSurvivalStats = EditorGUILayout.ToggleLeft(new GUIContent("Enable Survival Stats", "Should the controller enable its survival systems?"), t.enableSurvivalStats);
            GUI.enabled = t.enableSurvivalStats;
            t.statTickRate = EditorGUILayout.Slider(new GUIContent("Stat Ticks Per-minute", "How many times per minute should the stats do a tick update? Each tick depletes/regenerates the stats by their respective rates below."), t.statTickRate, 0.1f, 20.0f);
            #endregion
            if (survivalStatsFoldout) {

                #region Health Settings
                GUILayout.Label("<color=grey>Health Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(BoxPanel);
                SerializedProperty statHP = defaultSurvivalStats.FindPropertyRelative("Health"), currentStatHP = currentSurvivalStats.FindPropertyRelative("Health");
                
                //preview bar
                Rect casingRectHP = EditorGUILayout.GetControlRect();
                Rect statRectHP = new Rect(casingRectHP.x + 2, casingRectHP.y + 2,
                    Mathf.Clamp(casingRectHP.width / statHP.floatValue * currentStatHP.floatValue - 4, 0, casingRectHP.width),
                    casingRectHP.height - 4);
                EditorGUI.DrawRect(casingRectHP,statBackingColor);
                EditorGUI.DrawRect(statRectHP,new Color32(211,0,0,(byte)(GUI.enabled ? 191 : 64)));
            
                EditorGUILayout.PropertyField(statHP,new GUIContent("Health Points", "How much health does the controller start with?"));
            
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent("Health is critically low?"),currentSurvivalStats.FindPropertyRelative("hasLowHealth").boolValue);
                GUI.enabled = t.enableSurvivalStats;
                EditorGUILayout.EndVertical();
                #endregion

                #region Hunger Settings
                GUILayout.Label("<color=grey>Hunger Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(BoxPanel);
                SerializedProperty statHU = defaultSurvivalStats.FindPropertyRelative("Hunger"), currentStatHU = currentSurvivalStats.FindPropertyRelative("Hunger");
                
                //preview bar
                Rect casingRectHU = EditorGUILayout.GetControlRect(); 
                Rect statRectHU = new Rect(casingRectHU.x + 2, casingRectHU.y + 2,
                    Mathf.Clamp(casingRectHU.width / statHU.floatValue * currentStatHU.floatValue - 4, 0, casingRectHU.width), 
                    casingRectHU.height - 4);
                EditorGUI.DrawRect(casingRectHU,statBackingColor);
                EditorGUI.DrawRect(statRectHU,new Color32(142,54,0,(byte)(GUI.enabled ? 191 : 64)));
            
                EditorGUILayout.PropertyField(statHU,new GUIContent("Hunger Points", "How much Hunger does the controller start with?"));
                t.hungerDepletionRate = EditorGUILayout.Slider(new GUIContent("Hunger Depletion Per Tick","How much does hunger deplete per tick?"), t.hungerDepletionRate,0,5);
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent("Player is Starving?"),currentSurvivalStats.FindPropertyRelative("isStarving").boolValue);
                GUI.enabled = t.enableSurvivalStats;
                EditorGUILayout.EndVertical();
                #endregion

                #region Hydration Settings
                GUILayout.Label("<color=grey>Hydration Settings</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginVertical(BoxPanel);
                SerializedProperty statHY = defaultSurvivalStats.FindPropertyRelative("Hydration"), currentStatHY = currentSurvivalStats.FindPropertyRelative("Hydration");
                
                //preview bar
                Rect casingRectHY = EditorGUILayout.GetControlRect();
                Rect statRectHY = new Rect(casingRectHY.x + 2, casingRectHY.y + 2,
                    Mathf.Clamp(casingRectHY.width / statHY.floatValue * currentStatHY.floatValue - 4, 0, casingRectHY.width), 
                    casingRectHY.height - 4);
                EditorGUI.DrawRect(casingRectHY,statBackingColor);
                EditorGUI.DrawRect(statRectHY,new Color32(0,194,255,(byte)(GUI.enabled ? 191 : 64)));
                
                EditorGUILayout.PropertyField(statHY,new GUIContent("Hydration Points", "How much Hydration does the controller start with?"));
                t.hydrationDepletionRate = EditorGUILayout.Slider(new GUIContent("Hydration Depletion Per Tick","How much does hydration deplete per tick?"), t.hydrationDepletionRate,0,5);
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent("Player is Dehydrated?"),currentSurvivalStats.FindPropertyRelative("isDehydrated").boolValue);
                GUI.enabled = t.enableSurvivalStats;
                EditorGUILayout.EndVertical();
                #endregion
            }
            EditorGUILayout.Space();
            survivalStatsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(survivalStatsFoldout,survivalStatsFoldout ?  "<color=#B83C82>show less</color>" : "<color=#B83C82>show more</color>", ShowMoreStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();

        GUI.enabled = true;
        HandleGuiChange("Undo Survival Stat Setting changes");
        #endregion

        #region Interactable
        EditorGUILayout.Space(); 
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space();
        GUILayout.Label("Interactables Settings",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(BoxPanel);

        #if ENABLE_INPUT_SYSTEM
        t.interactKey = (Key)EditorGUILayout.EnumPopup(new GUIContent("Interact Key", "The keyboard key used to Interact with objects that implement IInteract"), t.interactKey);
        #else
        t.interactKey_L =(KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Interact Key", "The keyboard key used to Interact with objects that implement IInteract"), t.interactKey_L);
        #endif
        t.interactRange = EditorGUILayout.Slider(new GUIContent("Range","How far out can an interactable be from the player's position?"), t.interactRange, 0.1f,10);
        EditorGUILayout.PropertyField(interactableLayer,new GUIContent("Interactable Layers", "The Layers to check for interactables  on."));

        EditorGUILayout.EndVertical();
        #endregion

        #region Animation Triggers
        EditorGUILayout.Space(); EditorGUILayout.LabelField("",GUI.skin.horizontalSlider,GUILayout.MaxHeight(6)); EditorGUILayout.Space();
        GUILayout.Label("Animator Setup",labelHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(BoxPanel);
        t._1stPersonCharacterAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("1st Person Animator", "The animator used on the 1st person character mesh (if any)"), t._1stPersonCharacterAnimator,typeof(Animator), true);
        t._3rdPersonCharacterAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("3rd Person Animator", "The animator used on the 3rd person character mesh (if any)"), t._3rdPersonCharacterAnimator,typeof(Animator), true);
        if (t._3rdPersonCharacterAnimator || t._1stPersonCharacterAnimator) {
            EditorGUILayout.BeginVertical(BoxPanel);
            GUILayout.Label("Parameters",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
            t.a_velocity = EditorGUILayout.TextField(new GUIContent("Velocity (Float)","(Float) The name of the Velocity Parameter in the animator"), t.a_velocity);
            t.a_2DVelocity = EditorGUILayout.TextField(new GUIContent("2D Velocity (Float)","(Float) The name of the 2D Velocity Parameter in the animator"), t.a_2DVelocity);
            t.a_Idle = EditorGUILayout.TextField(new GUIContent("Idle (Bool)","(Bool) The name of the Idle Parameter in the animator"), t.a_Idle);
            t.a_Sprinting = EditorGUILayout.TextField(new GUIContent("Sprinting (Bool)","(Bool) The name of the Sprinting Parameter in the animator"), t.a_Sprinting);
            t.a_Crouching = EditorGUILayout.TextField(new GUIContent("Crouching (Bool)","(Bool) The name of the Crouching Parameter in the animator"), t.a_Crouching);
            t.a_Sliding = EditorGUILayout.TextField(new GUIContent("Sliding (Bool)","(Bool) The name of the Sliding Parameter in the animator"), t.a_Sliding);
            t.a_Jumped = EditorGUILayout.TextField(new GUIContent("Jumped (Bool)","(Bool) The name of the Jumped Parameter in the animator"), t.a_Jumped);
            t.a_Grounded = EditorGUILayout.TextField(new GUIContent("Grounded (Bool)","(Bool) The name of the Grounded Parameter in the animator"), t.a_Grounded);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.HelpBox(
            "WIP - This is a work in progress feature and currently very primitive.\n\n " +
            "No triggers, bools, floats, or ints are set up in the script. " +
            "To utilize this feature, find 'UpdateAnimationTriggers()' function in this script and set up triggers with the correct string names there. " +
            "This function gets called by the script whenever a relevant parameter gets updated. (I.e. when 'isVaulting' changes)",
            MessageType.Info);
        EditorGUILayout.EndVertical();
        HandleGuiChange("Undo Animation settings changes");
        #endregion

        #region Debuggers
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("",GUI.skin.horizontalSlider,GUILayout.MaxHeight(6));EditorGUILayout.Space(); 
        GUILayout.Label("<color=grey>Debuggers</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.BeginVertical(BoxPanel);
        
        float maxWidth = EditorGUIUtility.currentViewWidth/2 - 20;
        EditorGUILayout.BeginHorizontal();
        t.enableGroundingDebugging = GUILayout.Toggle(t.enableGroundingDebugging, "Debug Grounding System","Button", GUILayout.Width(maxWidth));
        t.enableMovementDebugging = GUILayout.Toggle(t.enableMovementDebugging, "Debug Movement System","Button", GUILayout.Width(maxWidth));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        t.enableMouseAndCameraDebugging = GUILayout.Toggle(t.enableMouseAndCameraDebugging,"Debug Mouse and Camera","Button", GUILayout.Width(maxWidth));
        t.enableVaultDebugging = GUILayout.Toggle(t.enableVaultDebugging, "Debug Vault System","Button", GUILayout.Width(maxWidth));
        EditorGUILayout.EndHorizontal();
    
        if (t.enableGroundingDebugging || t.enableMovementDebugging || t.enableMouseAndCameraDebugging || t.enableVaultDebugging) {
            EditorGUILayout.HelpBox("Debuggers can cause lag! Even in Application builds, make sure to keep these switched off unless absolutely necessary!",MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
        HandleGuiChange("Undo debugger changes");
        #endregion
    
        #region Web Links
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6)); 
        EditorGUILayout.Space(); 
        //GUILayout.Label("<color=grey>Support</color>",labelSubHeaderStyle,GUILayout.ExpandWidth(true));
        EditorGUILayout.BeginVertical("box");
        GUI.color = new Color(0.16f,0.67f,0.87f,1)*3f;
        if (GUILayout.Button("<color=white>Open Support Page</color>",SupportButtonStyle)) {
            Application.OpenURL("http://www.aedangraves.info/SuperCharacterSupport_Page.html");
        }
        
        GUI.color = Color.white;
        EditorGUILayout.EndVertical();

        #endregion
    }

    void UpdateGroundProfiles() {
        tSO = new SerializedObject(t);
        groundMatProf = tSO.FindProperty("footstepSoundSet");
    }
}
#endif
#endregion
}
