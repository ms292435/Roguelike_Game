using UnityEngine;

namespace Roguelike
{
    public class PlayerMovement : MonoBehaviour
    {
        private PlayerControls mControls;
        private Vector2 mMovementInput;

        private Rigidbody2D mRigidbody;
        private void Awake()
        {
            mControls = new PlayerControls();
            mRigidbody = GetComponent<Rigidbody2D>();
        }
        private void OnEnable() => mControls.Enable();

        private void OnDisable() => mControls.Disable();

        private void Update()
        {
            mMovementInput = mControls.Player.Move.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            mRigidbody.linearVelocity = mMovementInput * Player.Instance.Speed;
        }
    }
}