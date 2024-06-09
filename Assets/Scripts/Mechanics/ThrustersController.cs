using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Controls the hand thrusters.
    /// </summary>
    public class ThrustersController : MonoBehaviour
    {
        [SerializeField]
        private float _trusterDesiredVelocity;

        [SerializeField]
        private float _maxTrusterForce;

        [SerializeField]
        private float _trusterHeatingRate;

        [SerializeField]
        private float _trusterCoolingAcceleration;

        [Header("Referecnes")]
        [SerializeField]
        private Transform _head;

        [SerializeField]
        private Transform _forward;

        [SerializeField]
        private InputActionReference _action;

        [SerializeField]
        private UnityEvent _onStartThrust;

        [SerializeField]
        private UnityEvent _onEndThrust;

        private Rigidbody _body;

        private float _coolingSpeed;

        private bool _overheated;

        private bool _wasThrusting;

        public float Heat { get; private set; }

        private void Start()
        {
            _action.action.Enable();
            _body = transform.parent.GetComponentInParent<Rigidbody>();
            _body.maxAngularVelocity = 0;
        }

        private void FixedUpdate()
        {
            if (_overheated)
            {
                Cool();
                if (Heat <= 0.5f)
                {
                    _overheated = false;
                }
            }
            else
            {
                if (_action.action.IsPressed())
                {
                    if (!_wasThrusting)
                    {
                        _onStartThrust.Invoke();
                        _wasThrusting = true;
                    }

                    var dir = GetHandThrustersDirection();
                    var diff = dir * Mathf.Max(_trusterDesiredVelocity, _body.velocity.magnitude) - _body.velocity;
                    var force = Vector3.ClampMagnitude(diff, _maxTrusterForce) * Time.deltaTime;

                    _body.AddForce(force, ForceMode.VelocityChange);
                    Heat = Mathf.Min(1, Heat + _trusterHeatingRate * Time.deltaTime);
                    if (Heat >= 1)
                    {
                        _overheated = true;
                        _wasThrusting = false;
                        _onEndThrust.Invoke();
                    }

                    _coolingSpeed = 0;
                }
                else
                {
                    if (_wasThrusting)
                    {
                        _wasThrusting = false;
                        _onEndThrust.Invoke();
                    }

                    Cool();
                }
            }
        }

        private void Cool()
        {
            Heat = Mathf.Max(0, Heat - _coolingSpeed * Time.deltaTime);
            _coolingSpeed += _trusterCoolingAcceleration * Time.deltaTime;
        }

        private Vector3 GetHandThrustersDirection()
        {
            return _forward.forward;
        }
    }
}