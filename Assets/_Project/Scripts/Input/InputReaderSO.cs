using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gimersia.Input
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "Gimersia/Input/Input Reader")]
    public sealed class InputReaderSO : ScriptableObject
    {
        public event Action<Vector2> MoveEvent;
        public event Action JumpPerformedEvent;
        public event Action ThrowStartedEvent;
        public event Action ThrowReleasedEvent;
        public event Action CatchPerformedEvent;
        public event Action QuickPassPerformedEvent;
        public event Action SwapCharacterPerformedEvent;

        private InputActionMap _gameplayMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _throwAction;
        private InputAction _catchAction;
        private InputAction _quickPassAction;
        private InputAction _swapCharacterAction;

        private void OnEnable()
        {
            InitializeActionsIfNeeded();
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            DisableAllInput();
        }

        public void InitializeActionsIfNeeded()
        {
            if (_gameplayMap != null)
            {
                return;
            }

            _gameplayMap = new InputActionMap("Gameplay");

            _moveAction = _gameplayMap.AddAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _jumpAction = _gameplayMap.AddAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _throwAction = _gameplayMap.AddAction("Throw", InputActionType.Button);
            _throwAction.AddBinding("<Mouse>/leftButton");
            _throwAction.AddBinding("<Keyboard>/f");
            _throwAction.AddBinding("<Gamepad>/rightTrigger");

            _catchAction = _gameplayMap.AddAction("Catch", InputActionType.Button);
            _catchAction.AddBinding("<Mouse>/rightButton");
            _catchAction.AddBinding("<Keyboard>/e");
            _catchAction.AddBinding("<Gamepad>/leftTrigger");

            _quickPassAction = _gameplayMap.AddAction("QuickPass", InputActionType.Button);
            _quickPassAction.AddBinding("<Keyboard>/q");
            _quickPassAction.AddBinding("<Gamepad>/rightShoulder");

            _swapCharacterAction = _gameplayMap.AddAction("SwapCharacter", InputActionType.Button);
            _swapCharacterAction.AddBinding("<Keyboard>/tab");
            _swapCharacterAction.AddBinding("<Gamepad>/buttonNorth");

            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _jumpAction.performed += OnJumpPerformed;
            _throwAction.started += OnThrowStarted;
            _throwAction.canceled += OnThrowCanceled;
            _catchAction.performed += OnCatchPerformed;
            _quickPassAction.performed += OnQuickPassPerformed;
            _swapCharacterAction.performed += OnSwapCharacterPerformed;
        }

        public void EnableGameplayInput()
        {
            InitializeActionsIfNeeded();
            _gameplayMap.Enable();
        }

        public void DisableAllInput()
        {
            _gameplayMap?.Disable();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            MoveEvent?.Invoke(context.ReadValue<Vector2>());
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpPerformedEvent?.Invoke();
        }

        private void OnThrowStarted(InputAction.CallbackContext context)
        {
            ThrowStartedEvent?.Invoke();
        }

        private void OnThrowCanceled(InputAction.CallbackContext context)
        {
            ThrowReleasedEvent?.Invoke();
        }

        private void OnCatchPerformed(InputAction.CallbackContext context)
        {
            CatchPerformedEvent?.Invoke();
        }

        private void OnQuickPassPerformed(InputAction.CallbackContext context)
        {
            QuickPassPerformedEvent?.Invoke();
        }

        private void OnSwapCharacterPerformed(InputAction.CallbackContext context)
        {
            SwapCharacterPerformedEvent?.Invoke();
        }
    }
}
