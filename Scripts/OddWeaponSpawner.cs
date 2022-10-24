using UnityEngine;
using UnityEngine.Assertions;

namespace TehLemon
{
    public class OddWeaponSpawner : MonoBehaviour
    {
        [Tooltip("The weapon prefab")]
        [SerializeField]
        GameObject m_weapon;

        void Awake()
        {
            Assert.IsTrue(gameObject.CompareTag(Tags.WeaponSpawner), $"{gameObject} not tagged as WeaponSpawner");
            Assert.IsTrue(gameObject.layer == Layers.PlayerOnly, $"{gameObject} not layered as PlayerOnly");
        }

        public GameObject Weapon
        {
            get { return m_weapon; }
        }
    }
}