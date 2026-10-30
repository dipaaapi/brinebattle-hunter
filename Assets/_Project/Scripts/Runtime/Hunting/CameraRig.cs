using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Zoom and rotate the view around the raft (Ed's ask). Mouse wheel or +/- zooms, right-drag or
    /// Q/E turns, F puts the view back. Battles move the camera by themselves, so the rig waits then.
    /// The numbers live in <see cref="CameraRules"/> for EditMode tests.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Raft raft;

        Camera cam;
        float yaw, pitch, distance;
        float wantYaw, wantPitch, wantDistance;
        float homeYaw, homePitch, homeDistance;

        void Start()
        {
            cam = Camera.main;
            if (cam == null || raft == null) { enabled = false; return; }
            // Start from wherever the scene put the camera, so nothing jumps.
            var offset = cam.transform.position - Focus;
            distance = wantDistance = homeDistance = CameraRules.ClampDistance(offset.magnitude);
            pitch = wantPitch = homePitch = CameraRules.ClampPitch(Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg);
            yaw = wantYaw = homeYaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
        }

        /// <summary>The raft's centre at sea level, so the camera doesn't bob with the waves.</summary>
        Vector3 Focus
        {
            get
            {
                var p = raft.transform.position;
                return new Vector3(p.x, 0f, p.z);
            }
        }

        void LateUpdate()
        {
            if (cam == null || BattleController.InBattle || BattleController.CameraBusy) return;
            if (WorldPause.Open == Menu.None && !WorldPause.Asleep) ReadInput();

            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, wantYaw, k);
            pitch = Mathf.Lerp(pitch, wantPitch, k);
            distance = Mathf.Lerp(distance, wantDistance, k);
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.SetPositionAndRotation(Focus - rot * Vector3.forward * distance, rot);
        }

        void ReadInput()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            float dt = Time.unscaledDeltaTime;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) wantDistance = CameraRules.Zoom(wantDistance, -Mathf.Sign(wheel));
                if (mouse.rightButton.isPressed)
                {
                    var d = mouse.delta.ReadValue();
                    wantYaw += d.x * CameraRules.DragDegrees;
                    wantPitch = CameraRules.ClampPitch(wantPitch - d.y * CameraRules.DragDegrees);
                }
            }
            if (kb == null) return;
            if (kb.qKey.isPressed) wantYaw += CameraRules.KeyTurnSpeed * dt;
            if (kb.eKey.isPressed) wantYaw -= CameraRules.KeyTurnSpeed * dt;
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) wantDistance = CameraRules.Zoom(wantDistance, -1f);
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) wantDistance = CameraRules.Zoom(wantDistance, 1f);
            if (kb.fKey.wasPressedThisFrame) { wantYaw = homeYaw; wantPitch = homePitch; wantDistance = homeDistance; }
        }
    }
}
