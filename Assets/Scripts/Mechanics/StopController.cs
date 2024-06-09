using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Controls the player stopping mid flight with the press of the thumb stick
    /// </summary>
    public class StopController : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference _stopControl;

        [SerializeField]
        private float _stoppingSpeed;

        private Rigidbody _body;

        private void Start()
        {
            _body = GetComponent<Rigidbody>();
            _stopControl.action.Enable();
        }

        private void FixedUpdate()
        {
            if (_stopControl.action.IsPressed())
            {
                _body.AddForce(Vector3.ClampMagnitude(-_body.velocity, _stoppingSpeed), ForceMode.VelocityChange);
            }
        }
    }
}