using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TehLemon
{
    [DisallowMultipleComponent]
    public class OddActions : MonoBehaviour
    {
        [SerializeField] OddActionsSettings m_settings;

        // The spotlight component on the main camera
        Light m_flashlight;
        // The transform of the main FPS camera
        Transform m_cam;
        // The collider the player is currently looking at
        RaycastHit m_lookingAt;
        // Our currently owned weapons
        List<OddWeapon> m_weapons = new List<OddWeapon>();
        // The index of the currently equipped weapon in the list
        int m_currentWepI = -1;

        void Start()
        {
            m_flashlight = Camera.main.GetComponent<Light>();
            m_cam = Camera.main.transform;

            Assert.IsNotNull(m_flashlight);
            Assert.IsNotNull(m_cam);
            Assert.IsNotNull(m_settings);

            m_flashlight.enabled = false;
        }

        void Update()
        {
            // Find out what we're looking at		
            Ray ray = new Ray(m_cam.position, m_cam.forward);
            Physics.Raycast(ray, out m_lookingAt, m_settings.LookRange);

            // Player input
            if (Input.GetButtonDown(Keys.Use))
            {
                Use();
            }
            if (Input.GetButtonDown(Keys.Flashlight))
            {
                Flashlight();
            }
            if (Input.GetButton(Keys.Fire1))
            {
                Fire(false);
            }
            if (Input.GetButton(Keys.Fire2))
            {
                Fire2(false);
            }
            if (Input.GetButtonDown(Keys.Fire1))
            {
                Fire(true);
            }
            if (Input.GetButtonDown(Keys.Fire2))
            {
                Fire2(true);
            }
        }

        void Use()
        {
            if (m_lookingAt.distance > m_settings.UseRange)
            {
                return;
            }
        }

        void Flashlight()
        {
            m_flashlight.enabled = !m_flashlight.enabled;
        }

        void Fire(bool btnDown)
        {
            if (CurrentWep == null) { return; }
            if (btnDown)
            {
                CurrentWep.FireOnce(m_lookingAt);
            }
            else
            {
                CurrentWep.Fire(m_lookingAt);
            }
        }

        void Fire2(bool btnDown)
        {
            if (CurrentWep == null) { return; }
            if (btnDown)
            {
                CurrentWep.Fire2Once(m_lookingAt);
            }
            else
            {
                CurrentWep.Fire2(m_lookingAt);
            }
        }

        void SwitchWeapons(int newWep)
        {
            // If we're already equipping a weapon, disable it
            if (m_currentWepI > -1)
            {
                CurrentWep.gameObject.SetActive(false);
            }
            // Enable the weapon we're switching to
            m_currentWepI = newWep;
            CurrentWep.gameObject.SetActive(true);
        }

        void OnTriggerEnter(Collider other)
        {
            // Pick up new weapons
            if (other.CompareTag(Tags.WeaponSpawner))
            {

                // Add weapon to player
                GameObject newWeapon = other.GetComponent<OddWeaponSpawner>().Weapon;
                Assert.IsNotNull(newWeapon, "Weapon spawner missing OddWeaponSpawner component");
                // Disable the weapon spawner
                other.gameObject.SetActive(false);

                // Spawn and attach the new weapon to the camera
                OddWeapon weaponScript = Instantiate(newWeapon, m_cam).GetComponent<OddWeapon>();
                Assert.IsNotNull(weaponScript, "New weapon does not have an OddWeapon script");
                m_weapons.Add(weaponScript);
                SwitchWeapons(m_weapons.Count - 1);
            }
        }

        void OnGUI()
        {
            // Show pop-up if looking at a usable object
            // Temporary for debug purposes: Replace with an actual GUI at some point.
            if (m_lookingAt.collider
                && m_lookingAt.collider.CompareTag(Tags.Usable)
                && m_lookingAt.distance <= m_settings.UseRange)
            {
                GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2, 100, 100),
                            "Press E to use");
            }
        }

        OddWeapon CurrentWep
        {
            get
            {
                return m_weapons.Count > 0 ? m_weapons[m_currentWepI] : null;
            }
        }

#if UNITY_EDITOR
        [HideInInspector]
        public string DebugText = "";
#endif
    }

    [CreateAssetMenu(fileName = "OddActionsSettings", menuName = "TehLemon/OddPlayer/OddActionsSettings")]
    public class OddActionsSettings : ScriptableObject
    {
        [Tooltip("Maximum range of all raycasts")]
        public float LookRange = 50f;
        [Tooltip("How far away can the player can Use things")]
        public float UseRange = 1.25f;
    }
}

