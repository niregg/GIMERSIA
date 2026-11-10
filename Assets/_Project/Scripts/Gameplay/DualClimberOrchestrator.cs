using UnityEngine;
using UnityEngine.InputSystem;
using Gimersia.Input;
using Gimersia.Core;
using Gimersia.Juice;

namespace Gimersia.Gameplay
{
    public sealed class DualClimberOrchestrator : MonoBehaviour
    {
        [Header("Configurations & Inputs")]
        [SerializeField] private InputReaderSO _inputReader;
        [SerializeField] private CharacterMovementConfigSO _movementConfig;

        [Header("Climbers & Core Ball")]
        [SerializeField] private BeanClimberController _climberA;
        [SerializeField] private BeanClimberController _climberB;
        [SerializeField] private TerraCoreBall _terraCoreBall;

        [Header("Cameras for Split-Screen (It Takes Two Style)")]
        [SerializeField] private Camera _cameraA;
        [SerializeField] private Camera _cameraB;
        [SerializeField] private Transform _cameraRigA;
        [SerializeField] private Transform _cameraRigB;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 6.2f, -10.2f);
        [SerializeField] private float _cameraFollowSmoothTime = 0.12f;
        [SerializeField] private float _abyssFallThresholdY = -8.5f;
        [SerializeField] private float _summitTargetHeight = 125f;

        [Header("Co-op State")]
        [SerializeField] private bool _isSplitScreenMode = false;

        private BeanClimberController _activeClimber;
        private BeanClimberController _inactiveClimber;
        private CompanionFollowerAI _aiFollowerA;
        private CompanionFollowerAI _aiFollowerB;

        private Vector3 _cameraVelA;
        private Vector3 _cameraVelB;
        private float _cameraYawA;
        private float _cameraYawB;

        private Vector3 _lastCheckpointPos = new Vector3(0f, 1.5f, -24f);
        private int _currentZoneIndex = 1;
        private string _currentZoneName = "Zona 1: Pantai & Lembah Awal (0m)";

        private float _elapsedRunTime;
        private bool _isVictoryAchieved;
        private bool _isGamePaused;

        private AudioSource _oceanSurfSource;
        private AudioSource _pineWindSource;
        private AudioSource _summitGaleSource;

        public BeanClimberController ClimberA => _climberA;
        public BeanClimberController ClimberB => _climberB;
        public BeanClimberController ActiveClimber => _activeClimber != null ? _activeClimber : _climberA;
        public BeanClimberController InactiveClimber => _inactiveClimber != null ? _inactiveClimber : _climberB;
        public TerraCoreBall Ball => _terraCoreBall;
        public Camera CameraA => _cameraA;
        public Camera CameraB => _cameraB;
        public bool IsSplitScreenMode => _isSplitScreenMode;
        public int CurrentZoneIndex => _currentZoneIndex;
        public string CurrentZoneName => _currentZoneName;
        public float ElapsedRunTime => _elapsedRunTime;
        public bool IsVictoryAchieved => _isVictoryAchieved;
        public bool IsGamePaused => _isGamePaused;
        public float CurrentAltitude
        {
            get
            {
                float yA = _climberA != null ? _climberA.transform.position.y : 0f;
                float yB = _climberB != null ? _climberB.transform.position.y : 0f;
                return Mathf.Max(0f, Mathf.Max(yA, yB));
            }
        }
        public float SummitTargetHeight => _summitTargetHeight;

        private void Awake()
        {
            AutoResolveSceneReferencesIfMissing();
        }

        private void Start()
        {
            if (_movementConfig == null)
            {
                _movementConfig = ScriptableObject.CreateInstance<CharacterMovementConfigSO>();
            }

            if (_inputReader == null)
            {
                _inputReader = ScriptableObject.CreateInstance<InputReaderSO>();
            }

            _inputReader.EnableGameplayInput();
            SubscribeInput();

            if (_climberA != null && _climberB != null && _terraCoreBall != null)
            {
                _climberA.InitializeClimber(_movementConfig, _terraCoreBall, _climberB);
                _climberB.InitializeClimber(_movementConfig, _terraCoreBall, _climberA);

                _aiFollowerA = _climberA.GetComponent<CompanionFollowerAI>();
                if (_aiFollowerA == null) _aiFollowerA = _climberA.gameObject.AddComponent<CompanionFollowerAI>();
                _aiFollowerA.SetTargetLeader(_climberB.transform);

                _aiFollowerB = _climberB.GetComponent<CompanionFollowerAI>();
                if (_aiFollowerB == null) _aiFollowerB = _climberB.gameObject.AddComponent<CompanionFollowerAI>();
                _aiFollowerB.SetTargetLeader(_climberA.transform);

                _activeClimber = _climberA;
                _inactiveClimber = _climberB;
                _lastCheckpointPos = _climberA.transform.position;

                _terraCoreBall.AttachToHolder(_climberA);
            }

            ApplySplitScreenViewports();
            UpdateCompanionAiStates();
            InitializeAltitudeSoundscape();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        private void Update()
        {
            if (!_isVictoryAchieved && !_isGamePaused)
            {
                _elapsedRunTime += Time.deltaTime;
            }

            PollDualPlayerInputs();
            CheckAbyssFallAndSafetyRespawn();
            UpdateAltitudeSoundscape(Time.deltaTime);

            // In Solo Mode, automatically switch control to the receiver when passing/throwing the ball
            if (!_isSplitScreenMode && _terraCoreBall != null && _activeClimber != null && _inactiveClimber != null)
            {
                if ((_terraCoreBall.State == BallState.InFlightThrown || _terraCoreBall.State == BallState.InFlightQuickPass) &&
                    _terraCoreBall.IntendedReceiver == _inactiveClimber)
                {
                    SwapActiveClimber();
                }
                else if (_terraCoreBall.State == BallState.HeldByPlayer && _terraCoreBall.CurrentHolder == _inactiveClimber)
                {
                    SwapActiveClimber();
                }
            }
        }

        private void LateUpdate()
        {
            UpdateCamerasTracking(Time.unscaledDeltaTime);
        }

        public void SetSplitScreenMode(bool enableSplitScreen)
        {
            _isSplitScreenMode = enableSplitScreen;
            ApplySplitScreenViewports();
            UpdateCompanionAiStates();
        }

        public void ToggleSplitScreenMode()
        {
            SetSplitScreenMode(!_isSplitScreenMode);
        }

        private void ApplySplitScreenViewports()
        {
            AutoResolveSceneReferencesIfMissing();

            if (_cameraA == null)
            {
                return;
            }

            if (_isSplitScreenMode && _cameraB != null)
            {
                _cameraA.gameObject.SetActive(true);
                _cameraA.enabled = true;
                _cameraA.depth = 0f;
                _cameraA.rect = new Rect(0f, 0f, 0.5f, 1f);

                _cameraB.gameObject.SetActive(true);
                _cameraB.enabled = true;
                _cameraB.depth = 1f;
                _cameraB.rect = new Rect(0.5f, 0f, 0.5f, 1f);

                AudioListener listenerB = _cameraB.GetComponent<AudioListener>();
                if (listenerB != null)
                {
                    listenerB.enabled = false;
                }
            }
            else
            {
                _cameraA.gameObject.SetActive(true);
                _cameraA.enabled = true;
                _cameraA.depth = 0f;
                _cameraA.rect = new Rect(0f, 0f, 1f, 1f);

                if (_cameraB != null)
                {
                    _cameraB.enabled = false;
                    _cameraB.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateCompanionAiStates()
        {
            if (_isSplitScreenMode)
            {
                if (_aiFollowerA != null) _aiFollowerA.SetAiActive(false);
                if (_aiFollowerB != null) _aiFollowerB.SetAiActive(false);
            }
            else
            {
                if (_activeClimber == null) _activeClimber = _climberA;
                if (_inactiveClimber == null) _inactiveClimber = _climberB;

                if (_activeClimber == _climberA)
                {
                    if (_aiFollowerA != null) _aiFollowerA.SetAiActive(false);
                    if (_aiFollowerB != null && _climberA != null)
                    {
                        _aiFollowerB.SetTargetLeader(_climberA.transform);
                        _aiFollowerB.SetAiActive(true);
                    }
                }
                else
                {
                    if (_aiFollowerB != null) _aiFollowerB.SetAiActive(false);
                    if (_aiFollowerA != null && _climberB != null)
                    {
                        _aiFollowerA.SetTargetLeader(_climberB.transform);
                        _aiFollowerA.SetAiActive(true);
                    }
                }
            }
        }

        public void RegisterCheckpoint(Vector3 safePos, int zoneIndex, string zoneName)
        {
            if (zoneIndex >= _currentZoneIndex)
            {
                _lastCheckpointPos = safePos;
                _currentZoneIndex = zoneIndex;
                _currentZoneName = zoneName;
            }
        }

        public void RespawnAtLastCheckpoint()
        {
            if (_climberA == null || _climberB == null || _terraCoreBall == null) return;

            Rigidbody rbA = _climberA.GetComponent<Rigidbody>();
            Rigidbody rbB = _climberB.GetComponent<Rigidbody>();
            if (rbA != null) rbA.velocity = Vector3.zero;
            if (rbB != null) rbB.velocity = Vector3.zero;

            _climberA.transform.position = _lastCheckpointPos + new Vector3(-1.4f, 0.8f, 0f);
            _climberB.transform.position = _lastCheckpointPos + new Vector3(1.4f, 0.8f, 0f);

            _terraCoreBall.AttachToHolder(_activeClimber != null ? _activeClimber : _climberA);
        }

        public void SummonPartnerToActiveCheckpoint()
        {
            BeanClimberController leader = _isSplitScreenMode ? _climberA : _activeClimber;
            BeanClimberController partner = _isSplitScreenMode ? _climberB : _inactiveClimber;
            if (leader == null || partner == null) return;

            Rigidbody rbPartner = partner.GetComponent<Rigidbody>();
            if (rbPartner != null) rbPartner.velocity = Vector3.zero;

            partner.transform.position = leader.transform.position + new Vector3(1.6f, 0.8f, -1.2f);
        }

        public void TriggerSummitVictory()
        {
            _isVictoryAchieved = true;
        }

        public void TogglePause()
        {
            _isGamePaused = !_isGamePaused;
            if (ServiceLocator.TryGet<ITimeService>(out ITimeService timeService))
            {
                timeService.SetPaused(_isGamePaused);
            }
            else
            {
                Time.timeScale = _isGamePaused ? 0f : 1f;
            }
        }

        private void CheckAbyssFallAndSafetyRespawn()
        {
            if (_climberA != null && _climberA.transform.position.y < _abyssFallThresholdY)
            {
                RespawnSingleClimber(_climberA, new Vector3(-1.3f, 0.8f, 0f));
            }

            if (_climberB != null && _climberB.transform.position.y < _abyssFallThresholdY)
            {
                RespawnSingleClimber(_climberB, new Vector3(1.3f, 0.8f, 0f));
            }

            if (_terraCoreBall != null && _terraCoreBall.transform.position.y < _abyssFallThresholdY)
            {
                _terraCoreBall.AttachToHolder(_activeClimber != null ? _activeClimber : _climberA);
            }
        }

        private void RespawnSingleClimber(BeanClimberController climber, Vector3 offset)
        {
            bool wasHoldingBall = climber.HasBall;
            Rigidbody rb = climber.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;

            climber.transform.position = _lastCheckpointPos + offset;
            if (wasHoldingBall && _terraCoreBall != null)
            {
                _terraCoreBall.AttachToHolder(climber);
            }
        }

        private void PollDualPlayerInputs()
        {
            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
                return;
            }

            if ((Keyboard.current != null && (Keyboard.current.f1Key.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame)) ||
                UnityEngine.Input.GetKeyDown(KeyCode.F1) || UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                ToggleSplitScreenMode();
            }

            if (_isGamePaused) return;

            // Who does Keyboard P1 (WASD + Space + F/E/Q) control?
            // In Split-Screen: Always Climber A.
            // In Solo Mode: Whichever climber is currently ActiveClimber!
            BeanClimberController primaryTarget = _isSplitScreenMode ? _climberA : (_activeClimber != null ? _activeClimber : _climberA);
            if (primaryTarget == null) return;

            Vector2 p1RawMove = Vector2.zero;
            bool p1SpaceHeld = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed) p1RawMove.x -= 1f;
                if (Keyboard.current.dKey.isPressed) p1RawMove.x += 1f;
                if (Keyboard.current.sKey.isPressed) p1RawMove.y -= 1f;
                if (Keyboard.current.wKey.isPressed) p1RawMove.y += 1f;

                if (Keyboard.current.zKey.isPressed) _cameraYawA -= 85f * Time.deltaTime;
                if (Keyboard.current.cKey.isPressed) _cameraYawA += 85f * Time.deltaTime;

                p1SpaceHeld = Keyboard.current.spaceKey.isPressed;
                if (Keyboard.current.spaceKey.wasPressedThisFrame) primaryTarget.RequestJump();

                if (Keyboard.current.fKey.wasPressedThisFrame) primaryTarget.BeginThrowCharge();
                if (Keyboard.current.fKey.wasReleasedThisFrame) primaryTarget.ReleaseThrow();
                if (Keyboard.current.eKey.wasPressedThisFrame) primaryTarget.TryExecuteCatchInput();
                if (Keyboard.current.qKey.wasPressedThisFrame) primaryTarget.TryExecuteQuickPass();

                if (!_isSplitScreenMode && Keyboard.current.tabKey.wasPressedThisFrame) SwapActiveClimber();
                if (Keyboard.current.rKey.wasPressedThisFrame && _terraCoreBall != null) _terraCoreBall.AttachToHolder(primaryTarget);
                if (Keyboard.current.gKey.wasPressedThisFrame) SummonPartnerToActiveCheckpoint();
            }
            else
            {
                if (UnityEngine.Input.GetKey(KeyCode.A)) p1RawMove.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D)) p1RawMove.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) p1RawMove.y -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.W)) p1RawMove.y += 1f;

                if (UnityEngine.Input.GetKey(KeyCode.Z)) _cameraYawA -= 85f * Time.deltaTime;
                if (UnityEngine.Input.GetKey(KeyCode.C)) _cameraYawA += 85f * Time.deltaTime;

                p1SpaceHeld = UnityEngine.Input.GetKey(KeyCode.Space);
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) primaryTarget.RequestJump();

                if (UnityEngine.Input.GetKeyDown(KeyCode.F)) primaryTarget.BeginThrowCharge();
                if (UnityEngine.Input.GetKeyUp(KeyCode.F)) primaryTarget.ReleaseThrow();
                if (UnityEngine.Input.GetKeyDown(KeyCode.E)) primaryTarget.TryExecuteCatchInput();
                if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) primaryTarget.TryExecuteQuickPass();

                if (!_isSplitScreenMode && UnityEngine.Input.GetKeyDown(KeyCode.Tab)) SwapActiveClimber();
                if (UnityEngine.Input.GetKeyDown(KeyCode.R) && _terraCoreBall != null) _terraCoreBall.AttachToHolder(primaryTarget);
                if (UnityEngine.Input.GetKeyDown(KeyCode.G)) SummonPartnerToActiveCheckpoint();
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) primaryTarget.BeginThrowCharge();
                if (Mouse.current.leftButton.wasReleasedThisFrame) primaryTarget.ReleaseThrow();
                if (Mouse.current.rightButton.wasPressedThisFrame) primaryTarget.TryExecuteCatchInput();
            }

            Quaternion camRotA = Quaternion.Euler(0f, _cameraYawA, 0f);
            Vector3 worldMoveA = camRotA * new Vector3(p1RawMove.x, 0f, p1RawMove.y);
            primaryTarget.SetJumpHeld(p1SpaceHeld);
            primaryTarget.SetMoveInput(new Vector2(worldMoveA.x, worldMoveA.z));

            // =========================================================================
            // PLAYER 2 (Sahabat B in Split-Screen Mode): Arrow Keys / Numpad / Gamepad
            // =========================================================================
            if (_isSplitScreenMode && _climberB != null)
            {
                Vector2 p2RawMove = Vector2.zero;
                bool p2JumpHeld = false;

                if (Keyboard.current != null)
                {
                    if (Keyboard.current.leftArrowKey.isPressed) p2RawMove.x -= 1f;
                    if (Keyboard.current.rightArrowKey.isPressed) p2RawMove.x += 1f;
                    if (Keyboard.current.downArrowKey.isPressed) p2RawMove.y -= 1f;
                    if (Keyboard.current.upArrowKey.isPressed) p2RawMove.y += 1f;

                    if (Keyboard.current.numpad4Key.isPressed) _cameraYawB -= 85f * Time.deltaTime;
                    if (Keyboard.current.numpad6Key.isPressed) _cameraYawB += 85f * Time.deltaTime;

                    p2JumpHeld = Keyboard.current.rightCtrlKey.isPressed || Keyboard.current.numpad0Key.isPressed;
                    if (Keyboard.current.rightCtrlKey.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame) _climberB.RequestJump();

                    if (Keyboard.current.numpad1Key.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) _climberB.BeginThrowCharge();
                    if (Keyboard.current.numpad1Key.wasReleasedThisFrame || Keyboard.current.enterKey.wasReleasedThisFrame) _climberB.ReleaseThrow();
                    if (Keyboard.current.numpad2Key.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame) _climberB.TryExecuteCatchInput();
                    if (Keyboard.current.numpad3Key.wasPressedThisFrame || Keyboard.current.backslashKey.wasPressedThisFrame) _climberB.TryExecuteQuickPass();
                }
                else
                {
                    if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) p2RawMove.x -= 1f;
                    if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) p2RawMove.x += 1f;
                    if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) p2RawMove.y -= 1f;
                    if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) p2RawMove.y += 1f;

                    if (UnityEngine.Input.GetKey(KeyCode.Keypad4)) _cameraYawB -= 85f * Time.deltaTime;
                    if (UnityEngine.Input.GetKey(KeyCode.Keypad6)) _cameraYawB += 85f * Time.deltaTime;

                    p2JumpHeld = UnityEngine.Input.GetKey(KeyCode.RightControl) || UnityEngine.Input.GetKey(KeyCode.Keypad0);
                    if (UnityEngine.Input.GetKeyDown(KeyCode.RightControl) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad0)) _climberB.RequestJump();

                    if (UnityEngine.Input.GetKeyDown(KeyCode.Keypad1) || UnityEngine.Input.GetKeyDown(KeyCode.Return)) _climberB.BeginThrowCharge();
                    if (UnityEngine.Input.GetKeyUp(KeyCode.Keypad1) || UnityEngine.Input.GetKeyUp(KeyCode.Return)) _climberB.ReleaseThrow();
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Keypad2) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift)) _climberB.TryExecuteCatchInput();
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Keypad3) || UnityEngine.Input.GetKeyDown(KeyCode.Backslash)) _climberB.TryExecuteQuickPass();
                }

                if (Gamepad.current != null)
                {
                    Vector2 stick = Gamepad.current.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.05f) p2RawMove = stick;

                    if (Gamepad.current.buttonSouth.wasPressedThisFrame) _climberB.RequestJump();
                    if (Gamepad.current.buttonSouth.isPressed) p2JumpHeld = true;

                    if (Gamepad.current.rightTrigger.wasPressedThisFrame) _climberB.BeginThrowCharge();
                    if (Gamepad.current.rightTrigger.wasReleasedThisFrame) _climberB.ReleaseThrow();
                    if (Gamepad.current.rightShoulder.wasPressedThisFrame) _climberB.TryExecuteCatchInput();
                    if (Gamepad.current.buttonWest.wasPressedThisFrame) _climberB.TryExecuteQuickPass();
                }

                Quaternion camRotB = Quaternion.Euler(0f, _cameraYawB, 0f);
                Vector3 worldMoveB = camRotB * new Vector3(p2RawMove.x, 0f, p2RawMove.y);
                _climberB.SetJumpHeld(p2JumpHeld);
                _climberB.SetMoveInput(new Vector2(worldMoveB.x, worldMoveB.z));
            }
        }

        public void SwapActiveClimber()
        {
            if (_climberA == null || _climberB == null) return;

            if (_activeClimber != null)
            {
                _activeClimber.SetMoveInput(Vector2.zero);
                _activeClimber.SetJumpHeld(false);
            }

            BeanClimberController temp = _activeClimber;
            _activeClimber = _inactiveClimber;
            _inactiveClimber = temp;

            UpdateCompanionAiStates();
        }

        private void AutoResolveSceneReferencesIfMissing()
        {
            if (_climberA == null || _climberB == null)
            {
                BeanClimberController[] all = FindObjectsByType<BeanClimberController>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].gameObject.name.Contains("Sahabat_A")) _climberA = all[i];
                    else if (all[i].gameObject.name.Contains("Sahabat_B")) _climberB = all[i];
                }
                if (_climberA == null && all.Length > 0) _climberA = all[0];
                if (_climberB == null && all.Length > 1) _climberB = all[1];
            }

            if (_terraCoreBall == null) _terraCoreBall = FindFirstObjectByType<TerraCoreBall>();

            if (_cameraA == null || _cameraB == null)
            {
                Camera[] cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < cams.Length; i++)
                {
                    if (cams[i].gameObject.name.Contains("Camera_P1"))
                    {
                        _cameraA = cams[i];
                        _cameraRigA = cams[i].transform.parent != null ? cams[i].transform.parent : cams[i].transform;
                    }
                    else if (cams[i].gameObject.name.Contains("Camera_P2"))
                    {
                        _cameraB = cams[i];
                        _cameraRigB = cams[i].transform.parent != null ? cams[i].transform.parent : cams[i].transform;
                    }
                }
            }
        }

        private void SubscribeInput()
        {
            if (_inputReader == null) return;
            _inputReader.SwapCharacterPerformedEvent += SwapActiveClimber;
        }

        private void UnsubscribeInput()
        {
            if (_inputReader == null) return;
            _inputReader.SwapCharacterPerformedEvent -= SwapActiveClimber;
        }

        private void UpdateCamerasTracking(float dt)
        {
            if (_isVictoryAchieved && _cameraRigA != null)
            {
                if (_cameraA != null)
                {
                    _cameraA.rect = new Rect(0f, 0f, 1f, 1f);
                }
                if (_cameraB != null && _cameraB.gameObject.activeSelf)
                {
                    _cameraB.gameObject.SetActive(false);
                }

                Vector3 center = (_climberA != null && _climberB != null)
                    ? (_climberA.transform.position + _climberB.transform.position) * 0.5f
                    : (_climberA != null ? _climberA.transform.position : new Vector3(0f, 125f, 290f));

                float orbitAngle = Time.unscaledTime * 22f;
                Quaternion orbitRot = Quaternion.Euler(18f, orbitAngle, 0f);
                Vector3 orbitPos = center + (orbitRot * new Vector3(0f, 3.8f, -12.5f));
                _cameraRigA.position = Vector3.SmoothDamp(_cameraRigA.position, orbitPos, ref _cameraVelA, 0.25f, Mathf.Infinity, dt);

                Vector3 lookDir = (center + Vector3.up * 1.8f) - _cameraRigA.position;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    _cameraRigA.rotation = Quaternion.Slerp(_cameraRigA.rotation, Quaternion.LookRotation(lookDir.normalized, Vector3.up), 10f * dt);
                }
                return;
            }

            if (_isSplitScreenMode)
            {
                if (_cameraRigA != null && _climberA != null)
                {
                    UpdateSingleCamera(_cameraRigA, _climberA.transform.position, _cameraYawA, ref _cameraVelA, dt);
                }

                if (_cameraRigB != null && _climberB != null)
                {
                    UpdateSingleCamera(_cameraRigB, _climberB.transform.position, _cameraYawB, ref _cameraVelB, dt);
                }
            }
            else
            {
                BeanClimberController focusClimber = _activeClimber != null ? _activeClimber : _climberA;
                if (_cameraRigA != null && focusClimber != null)
                {
                    Vector3 targetFocus = focusClimber.transform.position;
                    if (_terraCoreBall != null && (_terraCoreBall.State == BallState.InFlightThrown || _terraCoreBall.State == BallState.InFlightQuickPass))
                    {
                        targetFocus = Vector3.Lerp(focusClimber.transform.position, _terraCoreBall.transform.position, 0.45f);
                    }
                    UpdateSingleCamera(_cameraRigA, targetFocus, _cameraYawA, ref _cameraVelA, dt);
                }
            }
        }

        private void UpdateSingleCamera(Transform rig, Vector3 focusPos, float yawAngle, ref Vector3 vel, float dt)
        {
            Quaternion yawRot = Quaternion.Euler(0f, yawAngle, 0f);
            Vector3 rotatedOffset = yawRot * _cameraOffset;
            Vector3 targetPos = focusPos + rotatedOffset;

            rig.position = Vector3.SmoothDamp(rig.position, targetPos, ref vel, _cameraFollowSmoothTime, Mathf.Infinity, dt);

            Vector3 lookTarget = focusPos + Vector3.up * 1.35f;
            Vector3 dir = lookTarget - rig.position;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                rig.rotation = Quaternion.Slerp(rig.rotation, targetRot, 14f * dt);
            }
        }

        private void InitializeAltitudeSoundscape()
        {
            const int sampleRate = 22050;
            const int durationSec = 4;
            int totalSamples = sampleRate * durationSec;

            // 1. Coastal Ocean Waves (0m - 30m)
            float[] oceanSamples = new float[totalSamples];
            // 2. Sub-Alpine Pine Breeze & Bamboo Flute Overtone (30m - 80m)
            float[] pineSamples = new float[totalSamples];
            // 3. Summit Gale & Nusantara Gamelan Harmonic Drone (80m - 125m)
            float[] summitSamples = new float[totalSamples];

            float prevOcean = 0f;
            float prevPine = 0f;
            float prevGale = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)sampleRate;
                float cyclePhase = (t / durationSec) * Mathf.PI * 2f;

                float rawNoise = (Mathf.PerlinNoise(i * 0.037f, 1.23f) * 2f) - 1f;

                // Ocean surf wave swell
                prevOcean = (prevOcean * 0.96f) + (rawNoise * 0.04f);
                float surfSwell = 0.55f + (0.45f * Mathf.Sin(cyclePhase));
                oceanSamples[i] = prevOcean * surfSwell * 0.35f;

                // Pine wind + soft bamboo harmonic (D5 = 587.33 Hz, A5 = 880 Hz)
                prevPine = (prevPine * 0.92f) + (rawNoise * 0.08f);
                float bambooHarmonic = (Mathf.Sin(2f * Mathf.PI * 293.66f * t) * 0.04f) +
                                       (Mathf.Sin(2f * Mathf.PI * 440.0f * t) * 0.03f);
                pineSamples[i] = ((prevPine * 0.28f) + bambooHarmonic) * (0.7f + 0.3f * Mathf.Sin(cyclePhase * 2f));

                // Summit gale + resonant Nusantara bronze gong/gamelan chord (146.83 Hz, 220 Hz, 329.63 Hz)
                prevGale = (prevGale * 0.86f) + (rawNoise * 0.14f);
                float gamelanChord = (Mathf.Sin(2f * Mathf.PI * 146.83f * t) * 0.06f) +
                                     (Mathf.Sin(2f * Mathf.PI * 220.0f * t) * 0.05f) +
                                     (Mathf.Sin(2f * Mathf.PI * 329.63f * t) * 0.04f);
                summitSamples[i] = (prevGale * 0.34f) + gamelanChord;
            }

            _oceanSurfSource = CreateLoopingAmbienceSource("Ambience_CoastalOcean", oceanSamples, sampleRate);
            _pineWindSource = CreateLoopingAmbienceSource("Ambience_PineBreeze", pineSamples, sampleRate);
            _summitGaleSource = CreateLoopingAmbienceSource("Ambience_SummitGale", summitSamples, sampleRate);
        }

        private AudioSource CreateLoopingAmbienceSource(string sourceName, float[] samples, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(sourceName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);

            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.Play();
            return src;
        }

        private void UpdateAltitudeSoundscape(float dt)
        {
            float alt = CurrentAltitude;

            float targetOceanVol = Mathf.Clamp01(1f - Mathf.InverseLerp(8f, 38f, alt)) * 0.22f;
            float targetPineVol = Mathf.Clamp01(1f - (Mathf.Abs(alt - 55f) / 42f)) * 0.24f;
            float targetSummitVol = Mathf.Clamp01(Mathf.InverseLerp(65f, 115f, alt)) * 0.28f;

            if (_oceanSurfSource != null)
            {
                _oceanSurfSource.volume = Mathf.MoveTowards(_oceanSurfSource.volume, targetOceanVol, 0.25f * dt);
            }
            if (_pineWindSource != null)
            {
                _pineWindSource.volume = Mathf.MoveTowards(_pineWindSource.volume, targetPineVol, 0.25f * dt);
            }
            if (_summitGaleSource != null)
            {
                _summitGaleSource.volume = Mathf.MoveTowards(_summitGaleSource.volume, targetSummitVol, 0.25f * dt);
            }
        }
    }
}
