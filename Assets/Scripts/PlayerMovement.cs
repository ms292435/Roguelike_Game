using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Handles 2D player character locomotion via the Unity Input System and Rigidbody2D.
    /// Reads directional movement inputs and applies linear velocity scaled by player speed.
    /// </summary>
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