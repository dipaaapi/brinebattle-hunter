using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The hunter on the raft (Ed's ask), outside battles (the battle stage moves the hunter then):
    /// - now and then an idle move: looks around, hops, stretches or takes a few steps;
    /// - casting and reeling: walks to the raft's edge facing the catch and pulls with the whole body;
    /// - after a lost battle: lies knocked out ("Zz") while resting.
    /// Placeholder primitives, like the rest of the art until Phase 9.
    /// </summary>
    public class HunterMotion : MonoBehaviour
    {
        public HookController hook;

        static readonly Vector3 Home = new Vector3(0f, 0.8f, 0f);
        enum Idle { None, Look, Hop, Stretch, Wander }

        Transform hunter;
        Vector3 baseScale;
        float yaw, nextIdle = 3f, idleTime;
        Idle idle;
        Vector3 wanderTo;
        GUIStyle zzStyle;

        Raft Raft => hook.raft;

        void Start()
        {
            hunter = Raft != null ? Raft.transform.Find("Hunter") : null;
            if (hunter == null) return;
            baseScale = hunter.localScale;
            // A visor so you can see which way the hunter faces.
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            Destroy(visor.GetComponent<Collider>());
            visor.transform.SetParent(hunter, false);
            visor.transform.localPosition = new Vector3(0f, 0.5f, 0.42f);
            visor.transform.localScale = new Vector3(0.7f, 0.18f, 0.25f);
            Tint.Apply(visor.GetComponent<Renderer>(), new Color(0.15f, 0.18f, 0.25f));
        }

        void Update()
        {
            if (hunter == null) return;
            float dt = Time.deltaTime;
            if (BattleController.InBattle)
            {
                // The battle stage moves the hunter; here it only turns to face the monster (Ed's ask).
                var foe = BattleController.Foe;
                if (foe == null) return;
                var to = Raft.transform.InverseTransformDirection(foe.transform.position - hunter.position);
                to.y = 0f;
                if (to.sqrMagnitude < 0.01f) return;
                yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 540f * dt);
                hunter.localRotation = Quaternion.Slerp(hunter.localRotation, Quaternion.Euler(0f, yaw, 0f), 12f * dt);
                return;
            }
            Vector3 goal = Home, offset = Vector3.zero;
            float pitch = 0f, roll = 0f, stretch = 1f, speed = 2.2f, wantYaw = yaw;

            var target = hook.HookTarget;
            if (target != null)
            {
                // Walk to the edge nearest the catch and face it; reeling = lean back and tug.
                var dir = Raft.transform.InverseTransformDirection(target.transform.position - Raft.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    dir.Normalize();
                    goal = EdgeToward(dir);
                    wantYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    if (hook.Reeling)
                    {
                        float tug = Mathf.Sin(Time.time * 9f);
                        pitch = -18f + tug * 10f;
                        offset = -dir * (0.06f + 0.06f * tug);
                    }
                    else pitch = 15f;
                }
                speed = 3.5f;
                idle = Idle.None;
            }
            else if (hook.RestLeft > 0f)
            {
                // Knocked out: flat on the deck until the rest is over.
                pitch = -80f;
                offset = Vector3.down * 0.35f;
                idle = Idle.None;
            }
            else IdleMoves(ref goal, ref offset, ref wantYaw, ref stretch, dt);

            yaw = Mathf.MoveTowardsAngle(yaw, wantYaw, 360f * dt);
            var local = hunter.localPosition;
            var flatGoal = goal + offset;
            hunter.localPosition = new Vector3(
                Mathf.MoveTowards(local.x, flatGoal.x, speed * dt),
                Mathf.Lerp(local.y, flatGoal.y, 12f * dt),
                Mathf.MoveTowards(local.z, flatGoal.z, speed * dt));
            hunter.localRotation = Quaternion.Slerp(hunter.localRotation, Quaternion.Euler(pitch, yaw, roll), 10f * dt);
            hunter.localScale = Vector3.Lerp(hunter.localScale, new Vector3(baseScale.x / Mathf.Sqrt(stretch), baseScale.y * stretch, baseScale.z / Mathf.Sqrt(stretch)), 10f * dt);
        }

        /// <summary>Every few seconds one small move, then back to standing at the centre.</summary>
        void IdleMoves(ref Vector3 goal, ref Vector3 offset, ref float wantYaw, ref float stretch, float dt)
        {
            if (WorldPause.Active) return;
            idleTime += dt;
            if (idle == Idle.None && idleTime >= nextIdle)
            {
                idle = (Idle)Random.Range(1, 5);
                idleTime = 0f;
                if (idle == Idle.Wander)
                {
                    var p = Random.insideUnitCircle * 0.35f;
                    wanderTo = Home + new Vector3(p.x, 0f, p.y);
                    if (!Raft.WalkableAt(wanderTo)) wanderTo = Home;
                }
            }
            switch (idle)
            {
                case Idle.Look:
                    wantYaw = Mathf.Sin(idleTime * 2.5f) * 70f;
                    if (idleTime > 2.5f) Done();
                    break;
                case Idle.Hop:
                    offset = Vector3.up * Mathf.Abs(Mathf.Sin(idleTime * 9f)) * 0.25f;
                    if (idleTime > 1.05f) Done();
                    break;
                case Idle.Stretch:
                    stretch = 1f + Mathf.Sin(Mathf.Clamp01(idleTime / 1.2f) * Mathf.PI) * 0.25f;
                    if (idleTime > 1.2f) Done();
                    break;
                case Idle.Wander:
                    goal = idleTime < 2.5f ? wanderTo : Home;
                    var d = wanderTo - Home;
                    if (idleTime < 2.5f && d.sqrMagnitude > 0.001f) wantYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    offset = Vector3.up * Mathf.Abs(Mathf.Sin(idleTime * 10f)) * 0.04f;
                    if (idleTime > 4f) Done();
                    break;
            }
        }

        void Done()
        {
            idle = Idle.None;
            idleTime = 0f;
            nextIdle = Random.Range(3f, 7f);
        }

        /// <summary>The last spot still on the raft walking from the centre along <paramref name="dir"/>, a little in from the edge.</summary>
        Vector3 EdgeToward(Vector3 dir)
        {
            Vector3 last = Home;
            for (float s = 0.25f; s < 6f; s += 0.25f)
            {
                var p = Home + dir * s;
                if (!Raft.WalkableAt(p)) break;   // planks only, never onto a module (Ed's ask)
                last = p;
            }
            return last - dir * 0.1f;
        }

        void OnGUI()
        {
            if (hunter == null || hook.RestLeft <= 0f || BattleController.InBattle || Camera.main == null) return;
            HuntStyles.Begin();
            zzStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 16, fontStyle = FontStyle.Bold };
            var sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(hunter.position + Vector3.up * 0.6f));
            if (sp.z < 0f) return;
            float rise = Time.time * 20f % 24f;
            GUI.Label(new Rect(sp.x - 40, HuntStyles.H - sp.y - 20 - rise, 80, 24), "<color=#cfe3ff>z Z</color>", zzStyle);
        }
    }
}
