using System;
using UnityEngine;
using UnityEngine.InputSystem;
using OceanFactory.Buildings;
using OceanFactory.Core;
using OceanFactory.UI;

namespace OceanFactory.Input
{
    public class InputReader : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;

        public Vector2 MoveAxis { get; private set; }
        public float ZoomDelta { get; private set; }
        public Vector2 PointerScreen { get; private set; }
        public Vector3 PointerWorld { get; private set; }
        public Vector2 PointerDelta { get; private set; }
        public bool PointerPrimaryHeld { get; private set; }
        public bool PointerSecondaryHeld { get; private set; }

        public event Action OnPointerPrimaryDown;
        public event Action OnPointerPrimaryUp;
        public event Action OnPointerSecondaryDown;
        public event Action OnPointerSecondaryUp;
        public event Action OnTogglePause;
        public event Action OnCancel;
        public event Action OnRotatePressed;

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<InputReader>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (mainCamera == null) mainCamera = Camera.main;

            Vector2 move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            }
            MoveAxis = move;

            if (mouse != null)
            {
                PointerScreen = mouse.position.ReadValue();
                PointerDelta = mouse.delta.ReadValue();
                if (mainCamera != null)
                {
                    var w = mainCamera.ScreenToWorldPoint(new Vector3(PointerScreen.x, PointerScreen.y, -mainCamera.transform.position.z));
                    PointerWorld = w;
                }
                ZoomDelta = mouse.scroll.ReadValue().y;
                PointerPrimaryHeld = mouse.leftButton.isPressed;
                PointerSecondaryHeld = mouse.rightButton.isPressed;
                if (mouse.leftButton.wasPressedThisFrame) OnPointerPrimaryDown?.Invoke();
                if (mouse.leftButton.wasReleasedThisFrame) OnPointerPrimaryUp?.Invoke();
                if (mouse.rightButton.wasPressedThisFrame) OnPointerSecondaryDown?.Invoke();
                if (mouse.rightButton.wasReleasedThisFrame) OnPointerSecondaryUp?.Invoke();
            }
            else
            {
                PointerDelta = Vector2.zero;
            }

            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    // Esc is a single, prioritized cancel:
                    //   1) Recipe list open      -> close it
                    //   2) Recipe picker open    -> close it
                    //   3) Build mode active     -> drop back to None
                    //   4) otherwise             -> toggle the pause panel
                    // Exactly one of these fires per press, so Esc never cascades into pause
                    // while also cancelling a mode.
                    if (RecipeListController.Instance != null && RecipeListController.Instance.IsOpen)
                    {
                        RecipeListController.Instance.Close();
                    }
                    else if (RecipePickerController.Instance != null && RecipePickerController.Instance.IsOpen)
                    {
                        RecipePickerController.Instance.Close();
                    }
                    else if (Services.TryGet<BuildModeService>(out var escBms) && escBms.Current != BuildMode.None)
                    {
                        escBms.SetMode(BuildMode.None);
                        OnCancel?.Invoke();
                    }
                    else
                    {
                        OnTogglePause?.Invoke();
                    }
                }
                if (kb.rKey.wasPressedThisFrame)
                {
                    OnRotatePressed?.Invoke();
                }
                if (kb.mKey.wasPressedThisFrame)
                {
                    // M toggles the global recipe browser. Close the picker first so
                    // we don't end up with both panels stacked on top of each other.
                    if (RecipePickerController.Instance != null && RecipePickerController.Instance.IsOpen)
                    {
                        RecipePickerController.Instance.Close();
                    }
                    RecipeListController.Toggle();
                }

                // Hotkeys for build modes (Hub is auto-placed at world start, not buildable by hand).
                // 1=Extractor  2=Conveyor  3=Assembler  4=Remove  0/Esc=None
                if (Services.TryGet<BuildModeService>(out var bms))
                {
                    if (kb.digit1Key.wasPressedThisFrame) bms.SetMode(BuildMode.Extractor);
                    if (kb.digit2Key.wasPressedThisFrame) bms.SetMode(BuildMode.Conveyor);
                    if (kb.digit3Key.wasPressedThisFrame) bms.SetMode(BuildMode.Assembler);
                    if (kb.digit4Key.wasPressedThisFrame) bms.SetMode(BuildMode.Remove);
                    if (kb.digit0Key.wasPressedThisFrame) bms.SetMode(BuildMode.None);
                }
            }
        }
    }
}
