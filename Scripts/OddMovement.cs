// Attach to Player gameobject to handle movement
// Based on: 
// https://github.com/id-Software/Quake-III-Arena/blob/master/code/game/bg_pmove.c
// https://github.com/ValveSoftware/source-sdk-2013/blob/56accfdb9c4abd32ae1dc26b2e4cc87898cf4dc1/sp/src/game/shared/gamemovement.cpp

// TehLemon 2017/11/29

using UnityEngine;
using UnityEngine.Assertions;
using TehLemon;
using TehLemon.Utilities;

namespace TehLemon
{
    [DisallowMultipleComponent]
    public class OddMovement : MonoBehaviour
    {
        // Queue up input commands
        struct Cmd
        {
            public int Forward;
            public int Right;
            public bool Jump;
            public bool Crouch;

            public Vector3 MoveAxis
            {
                get
                {
                    return new Vector3(Right, 0, Forward);
                }
            }
        }

        #region Inspector Variables      
        [SerializeField] OddCameraSettings m_camSettings;
        [SerializeField] OddPlayerSettings m_stats;
        #endregion

        // Cached referecences
        Rigidbody m_camera;
        CharacterController m_controller;
        Cmd m_cmd;

        // Current view rotation
        float m_rotX = 0f; // up down
        float m_rotY = 0f; // left right
        // Move direction in world space
        Vector3 m_moveDir = Vector3.zero;
        Vector3 m_velocity = Vector3.zero;
        float m_currentEyeOffset = 0.0f;
        float m_currentGroundSpeed = 0.0f;

        void Start()
        {
            Debug.Log("Player Initialising");
            m_camera = Camera.main.GetComponent<Rigidbody>();
            m_controller = GetComponent<CharacterController>();

            Assert.IsNotNull(m_camera);
            Assert.IsNotNull(m_controller);

            // Temporary
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            m_camera.MovePosition(Top.Add(y: m_camSettings.EyeOffset));

            m_currentGroundSpeed = m_stats.GroundSpeed;

            Debug.Log("Player Initialised");

        }

        // Player input
        void Update()
        {
            // Aim camera
            m_rotX -= Input.GetAxisRaw(Keys.MouseY) * m_camSettings.MouseSensitivity * InvertedY; // up down
            m_rotY += Input.GetAxisRaw(Keys.MouseX) * m_camSettings.MouseSensitivity * InvertedX; // left right

            m_rotX = Mathf.Clamp(m_rotX, -90, 90);

            transform.rotation = Quaternion.Euler(0, m_rotY, 0);

            // Queue inputs
            m_cmd.Forward = (int)Input.GetAxisRaw(Keys.Vertical);
            m_cmd.Right = (int)Input.GetAxisRaw(Keys.Horizontal);
            m_cmd.Jump = Input.GetButton(Keys.Jump);
            m_cmd.Crouch = Input.GetButton(Keys.Crouch);

            m_moveDir = MathUtil.OrientateVec(m_cmd.MoveAxis, m_camera.transform.forward);
            m_moveDir = transform.forward * m_cmd.Forward + transform.right * m_cmd.Right;
            m_moveDir = Vector3.ProjectOnPlane(m_moveDir, Vector3.up).normalized;

            // Move camera
            // Follow player directly but smooth the y offset for animated crouching
            //float newCamHeight = MathUtil.Damp(m_camera.transform.position.y
            //  , Top.y + m_eyeOffset
            // , m_crouchSpeed);
            float targetEyeOffset = (Height * 0.5f) + m_camSettings.EyeOffset;
            m_currentEyeOffset = MathUtil.Damp(m_currentEyeOffset, targetEyeOffset, m_camSettings.CamCrouchSpeed, Time.deltaTime);
            m_camera.position = transform.position.Add(y: m_currentEyeOffset);
            m_camera.rotation = Quaternion.Euler(m_rotX, m_rotY, 0);
        }

        // Player movement
        void FixedUpdate()
        {
            DebugText = "";

            // Crouching
            if (m_cmd.Crouch)
            {
                m_controller.height = m_stats.DefaultHeight * m_stats.CrouchScale;
                m_currentGroundSpeed = m_stats.CrouchSpeed;
            }
            // Uncrouching
            else if (Height < m_stats.DefaultHeight && CanStand())
            {
                m_controller.height = m_stats.DefaultHeight;
                m_currentGroundSpeed = m_stats.GroundSpeed;
            }

            // Movement
            if (m_controller.isGrounded)
            {
                m_velocity = MoveGround(m_moveDir, m_velocity);
            }
            else
            {
                m_velocity = MoveAir(m_moveDir, m_velocity);
            }
            // Gravity
            m_velocity += new Vector3(0, m_stats.Gravity, 0);

            // Shorten jump if jump button is let go early
            if (!m_controller.isGrounded && m_velocity.y > m_stats.MinJumpSpeed && !m_cmd.Jump)
            {
                m_velocity = m_velocity.With(y: m_stats.MinJumpSpeed);
            }

            // Move player
            m_controller.Move(m_velocity * Time.fixedDeltaTime);

#if UNITY_EDITOR
            DebugText += $"Grounded {m_controller.isGrounded}" +
                $"\nMoveDir {m_moveDir}" +
                $"\nVelocity {m_velocity} : {m_velocity.magnitude}" +
                $"\nCrouching: {IsCrouching} " +
                $"\nCan stand: {CanStand()}" +
                $"\nFriction: {m_currentGroundSpeed}";
#endif
        }

        // Check if there's enough room to uncrouch
        bool CanStand()
        {
            Vector3 start = Bottom.Add(y: Radius + 0.1f);
            Vector3 end = StandingTop.Add(y: -Radius);
            return !Physics.CheckCapsule(start, end, Radius * m_stats.UncrouchWidthFactor, m_stats.CollisionMask);
        }

        // https://flafla2.github.io/2015/02/14/bunnyhop.html
        // accelDir: normalized direction that the player has requested to move (taking into account the movement keys and look direction)
        // prevVelocity: The current velocity of the player, before any additional calculations
        // accelerate: The server-defined player acceleration value
        // max_velocity: The server-defined maximum player velocity (this is not strictly adhered to due to strafejumping)
        Vector3 Accelerate(Vector3 accelDir, Vector3 prevVelocity, float accelerate, float max_velocity)
        {
            float projVel = Vector3.Dot(prevVelocity, accelDir); // Vector projection of Current velocity onto accelDir.
            float accelVel = accelerate;// * Time.fixedDeltaTime; // Accelerated velocity in direction of movment

            // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
            if (projVel + accelVel > max_velocity)
            {
                accelVel = max_velocity - projVel;
            }
            return prevVelocity + (accelDir * accelVel);
        }

        Vector3 MoveGround(Vector3 accelDir, Vector3 prevVelocity)
        {
            // Apply Friction            
            float speed = prevVelocity.magnitude;
            if (speed != 0) // To avoid divide by zero errors
            {
                float drop = speed * m_stats.Friction;// * Time.fixedDeltaTime;
                prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // Scale the velocity based on friction.
            }

            // ground_accelerate and max_velocity_ground are server-defined movement variables
            prevVelocity = Accelerate(accelDir, prevVelocity, m_stats.GroundAccel, m_currentGroundSpeed);

            if (m_cmd.Jump)
            {
                prevVelocity = prevVelocity.With(y: m_stats.JumpSpeed);
            }

            return prevVelocity;
        }

        Vector3 MoveAir(Vector3 accelDir, Vector3 prevVelocity)
        {
            // air_accelerate and max_velocity_air are server-defined movement variables
            return Accelerate(accelDir, prevVelocity, m_stats.AirAccel, m_stats.AirSpeed);
        }

        #region Properties
        int InvertedX
        {
            get { return m_camSettings.InvertCameraX ? -1 : 1; }
        }
        int InvertedY
        {
            get { return m_camSettings.InvertCameraY ? -1 : 1; }
        }
        float Height
        {
            get { return m_controller.height; }
        }
        float Radius
        {
            get { return m_controller.radius; }
        }
        Vector3 Bottom
        {
            get { return transform.position.Add(y: -(Height * 0.5f)); }
        }
        Vector3 Top
        {
            get { return transform.position.Add(y: (Height * 0.5f)); }
        }
        Vector3 StandingTop
        {
            get { return transform.position.With(y: Bottom.y + m_stats.DefaultHeight); }
        }
        bool IsCrouching
        {
            get { return Height < m_stats.DefaultHeight; }
        }
        #endregion

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;

                Vector3 start = Bottom.Add(y: Radius + 0.1f);
                Vector3 end = StandingTop.Add(y: -Radius);
                Vector3 size = new Vector3(Radius * m_stats.UncrouchWidthFactor * 2, Radius * 2, Radius * m_stats.UncrouchWidthFactor * 2);
                Gizmos.DrawWireCube(start, size);
                Gizmos.DrawWireCube(end, size);
            }
        }

        [HideInInspector]
        public string DebugText = "";
#endif
    }

    [CreateAssetMenu(fileName = "OddPlayerSettings", menuName = "TehLemon/OddPlayer/OddPlayerSettings")]
    public class OddPlayerSettings : ScriptableObject
    {
        [Header("Player Heights")]
        [SerializeField]
        public float DefaultHeight = 1.8f;
        [Tooltip("How much the player collision height scales when they crouch")]
        [SerializeField]
        public float CrouchScale = 0.5f;
        [Tooltip("How far inside a low ceiling to block uncrouching. Radius * width factor.")]
        [SerializeField]
        public float UncrouchWidthFactor = 0.2f;
        [SerializeField]
        public LayerMask CollisionMask;

        [Header("Running")]
        [SerializeField]
        public float GroundSpeed = 5f;
        [SerializeField]
        public float GroundAccel = 100f;
        [SerializeField]
        public float Friction = 8f;
        [SerializeField]
        public float CrouchSpeed = 2.25f;

        [Header("Jumping")]
        [SerializeField]
        public float AirSpeed = 3.5f;
        [SerializeField]
        public float AirAccel = 200f;
        [SerializeField]
        public float Gravity = -0.9f;
        [SerializeField]
        public float JumpSpeed = 9.75f;
        [SerializeField]
        public float MinJumpSpeed = 4.875f;
    }

    [CreateAssetMenu(fileName = "OddCameraSettings", menuName = "TehLemon/OddPlayer/OddCameraSettings")]
    public class OddCameraSettings : ScriptableObject
    {
        public float MouseSensitivity = 3.0f;
        public bool InvertCameraX = false;
        public bool InvertCameraY = false;
        [Tooltip("How high the camera is offset from the top of the player collision")]
        public float EyeOffset = 0f;
        [Tooltip("How fast the camera crouches and uncrouches")]
        public float CamCrouchSpeed = 12.5f;
    }
}


