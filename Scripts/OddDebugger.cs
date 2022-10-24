using UnityEngine;

namespace TehLemon
{
	#if UNITY_EDITOR
    public class OddDebugger : MonoBehaviour
    {
        [SerializeField]
        bool m_showDebugText = false;

        OddMovement movement;
        OddActions actions;

        void Start()
        {
            movement = GetComponent<OddMovement>();
            actions = GetComponent<OddActions>();
        }

        string debugText = "";
        void OnGUI()
        {
            if (m_showDebugText)
            {
                debugText = $"_Movement_\n{movement.DebugText}\n\n_Actions_\n{actions.DebugText}";

                GUI.Label(new Rect(10, 10, Screen.width, Screen.height), debugText);
            }
        }
    }
	#endif
}