using Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00._Work._Resources._02._Scripts.Systems
{
    [CreateAssetMenu(fileName = "UI Input", menuName = "SO/Core/UIInput", order = 0)]
    public class UIInputSo : ScriptableObject, Controls.IUIActions
    {
        [SerializeField] private PlayerInputSO playerInput;
        
        private Controls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.UI.SetCallbacks(this);
            }
            _controls.UI.Enable();
        }

        private void OnDisable()
        {
            _controls.UI.Disable();
        }
        
        public void SetPlayerInputEnable(bool isEnable) => playerInput.SetEnable(isEnable);
        public void SetEnable(bool isEnable)
        {
            if (isEnable)
                OnEnable();
            else
                OnDisable();
        }

        #region 사용하지 않는 콜백함수

        public void OnNavigate(InputAction.CallbackContext context)
        {
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
        }

        public void OnPoint(InputAction.CallbackContext context)
        {
        }

        public void OnClick(InputAction.CallbackContext context)
        {
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
        }

        public void OnMiddleClick(InputAction.CallbackContext context)
        {
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
        }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
        }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            
        }

        #endregion
    }
}