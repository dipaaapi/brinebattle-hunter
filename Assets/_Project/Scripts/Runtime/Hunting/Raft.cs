using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>The player's raft. Phase 2 only needs its size for the gate; parts come in Phase 5.</summary>
    public class Raft : MonoBehaviour
    {
        [Range(1, 10)] public int size = 1;
        [Tooltip("How far the hook reaches from the raft centre.")]
        public float hookRange = 14f;
        [Tooltip("Battle HP per raft part. Total raft HP = parts × this (GDD §5: raft parts take damage).")]
        public int partHp = 10;

        public int PartCount
        {
            get
            {
                int n = 0;
                foreach (Transform child in transform)
                    if (child.name.StartsWith("Part")) n++;
                return Mathf.Max(1, n);
            }
        }

        public int MaxHp => PartCount * partHp;
    }
}
