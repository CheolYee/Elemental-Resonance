using Gamelib.SoundSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Systems
{
    [CreateAssetMenu(fileName = "Player Input",menuName = "SO/Core/PlayerInput",order = 5)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        [Header("Hover Layer")]
        [SerializeField] private LayerMask whatIsTarget;

        public event Action OnClick;
        
        private Controls _controls;
        
        private Vector3 _worldMousePos;
        private Vector2 screenMousePosition;

        private Camera mainCam;

        public Camera MainCam
        {
            get
            {
                if (mainCam == null)
                    mainCam = Camera.main;

                return mainCam;
            }
        }
        
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();

                _controls.Player.SetCallbacks(this);
            }

            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        #region Input Actions

        public void OnPointer(InputAction.CallbackContext context)
        {
            screenMousePosition = context.ReadValue<Vector2>();
        }

        public void OnClcik(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnClick?.Invoke();
        }

        public void SetEnable(bool isEnable)
        {
            if (isEnable)
                OnEnable();
            else
                OnDisable();
        }
        
        public Vector3 GetWorldMousePos()
        {
            if (MainCam is null) //메인 카메라의 ScreenPointToRay를 사용해야 하므로, 예외 처리를 해준다.
                return _worldMousePos;
            
            //스크린 좌표계를 월드 좌표계로 변환하기 위해 카메라에서 레이를 쏜다.
            Ray cameraRay = MainCam.ScreenPointToRay(_worldMousePos); 
            //레이가 충돌하는 지점이 마우스의 월드 좌표가 된다.
            //레이가 땅과 충돌하지 않으면 이전 프레임의 월드 마우스 위치를 유지하고 반환한다.
            if (Physics.Raycast(cameraRay, out RaycastHit hitInfo, MainCam.farClipPlane, whatIsTarget))
            { 
                //RaycastHit 구조체에는 맞은 정점인 포인트와
                //맞은 콜라이더의 다양한 정보가 담겨있다.
                _worldMousePos = hitInfo.point;
            }
            return _worldMousePos;
        }

        #endregion
    }
}