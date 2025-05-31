using UnityEngine;

namespace Assets.Scripts.Tools
{
    [ExecuteAlways]
    public class WheelColliderGizmoDrawer : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            WheelCollider wheel = GetComponent<WheelCollider>();
            if (wheel == null) return;

            Gizmos.color = Color.green;

            Vector3 pos;
            Quaternion quat;
            wheel.GetWorldPose(out pos, out quat);
            Gizmos.DrawWireSphere(pos, wheel.radius);
        }
    }
}
