using UnityEngine;
using UnityEngine.Assertions;
using TehLemon.Utilities;

namespace TehLemon
{
    public abstract class OddWeapon : MonoBehaviour
    {
		[Tooltip("Default position of the weapon relative to the camera")]
        [SerializeField]
        protected Vector3 m_idlePosition;
		[Tooltip("Default rotation of the weapon relative to the camera")]
        [SerializeField]
        protected Vector3 m_idleRotation;

        void Awake()
        {
            SetLocalPosRot(m_idlePosition, m_idleRotation);
        }

        // Use these for holding down the fire buttons
        public abstract void Fire(RaycastHit ray);
        public abstract void Fire2(RaycastHit ray);

        // Use these if you only want it to execute once on button down
        public abstract void FireOnce(RaycastHit ray);
        public abstract void Fire2Once(RaycastHit ray);

		// Smoothly moves the weapon towards the given target
		// Speed can range from 0 to infinity. Lower is slower.
        protected void DampTowards(Vector3 pos, Vector3 rot, float speed)
        {
            transform.localPosition = MathUtil.Damp(transform.localPosition, pos, speed);
            transform.localRotation = MathUtil.DampS(transform.localRotation, Quaternion.Euler(rot), speed);
        }
		protected void DampTowards(Vector3 pos, Quaternion rot, float speed)
        {
            transform.localPosition = MathUtil.Damp(transform.localPosition, pos, speed);
            transform.localRotation = MathUtil.DampS(transform.localRotation, rot, speed);
        }

		// Just a wrapper to set both the local transform position and rotation
		// You'll be using the local transforms a lot since the weapon is parented to the camera
        protected void SetLocalPosRot(Vector3 position, Vector3 rotation)
        {
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(rotation);
        }
		protected void SetLocalPosRot(Vector3 position, Quaternion rotation)
        {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }
    }
}