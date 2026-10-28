using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Placeholder ocean: a generated wave texture that scrolls with the current, plus white
    /// streaks riding the current so you can see which way drifts will float.
    /// </summary>
    public class OceanSurface : MonoBehaviour
    {
        public DriftSpawner spawner;
        public int streakCount = 40;
        public float streakArea = 26f;

        Material mat;
        Vector2 uvPerWorld;     // texture offset change per metre along world x / z
        Vector2 offset;
        readonly List<Transform> streaks = new List<Transform>();
        readonly List<float> streakSpeed = new List<float>();
        Material streakMat;

        Vector3 Dir => spawner != null ? spawner.currentDirection.normalized : Vector3.right;
        float Speed => spawner != null ? spawner.driftSpeed : 1.5f;

        void Start()
        {
            var r = GetComponent<Renderer>();
            mat = r.material;
            var tex = WaveTexture(128);
            const float tiling = 30f;
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetColor("_BaseColor", Color.white);
            mat.mainTextureScale = new Vector2(tiling, tiling);
            mat.SetFloat("_Smoothness", 0.6f);
            uvPerWorld = UvGradient(GetComponent<MeshFilter>().sharedMesh) * tiling;

            streakMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            streakMat.SetColor("_BaseColor", new Color(0.85f, 0.95f, 1f));
            streakMat.color = new Color(0.85f, 0.95f, 1f);
            for (int i = 0; i < streakCount; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Quad);
                s.name = "Current streak";
                Destroy(s.GetComponent<Collider>());
                s.GetComponent<Renderer>().sharedMaterial = streakMat;
                s.transform.SetParent(transform.parent, false);
                streaks.Add(s.transform);
                streakSpeed.Add(Random.Range(0.8f, 1.4f));
                Place(s.transform, Random.Range(-streakArea, streakArea));
            }
        }

        void Place(Transform s, float along)
        {
            var dir = Dir;
            var side = Vector3.Cross(Vector3.up, dir);
            s.position = new Vector3(0f, 0.03f, 0f) + dir * along + side * Random.Range(-streakArea, streakArea);
            s.rotation = Quaternion.LookRotation(Vector3.down, dir);
            s.localScale = new Vector3(0.06f, Random.Range(1.2f, 2.6f), 1f);
        }

        void Update()
        {
            if (BattleController.InBattle) return;
            var dir = Dir;
            offset -= new Vector2(dir.x * uvPerWorld.x, dir.z * uvPerWorld.y) * Speed * 0.6f * Time.deltaTime;
            offset.x %= 1f; offset.y %= 1f;
            mat.mainTextureOffset = offset;
            mat.SetTextureOffset("_BaseMap", offset);

            for (int i = 0; i < streaks.Count; i++)
            {
                var s = streaks[i];
                s.position += dir * Speed * streakSpeed[i] * Time.deltaTime;
                if (Vector3.Dot(s.position, dir) > streakArea) Place(s, -streakArea);
            }
        }

        /// <summary>How much u and v change per world metre along x and z, read from the mesh itself.</summary>
        Vector2 UvGradient(Mesh mesh)
        {
            var v = mesh.vertices; var uv = mesh.uv; var t = mesh.triangles;
            Vector3 e1 = v[t[1]] - v[t[0]], e2 = v[t[2]] - v[t[0]];
            Vector2 d1 = uv[t[1]] - uv[t[0]], d2 = uv[t[2]] - uv[t[0]];
            float det = e1.x * e2.z - e2.x * e1.z;
            if (Mathf.Abs(det) < 1e-6f) return new Vector2(0.1f, 0.1f);
            float dudx = (d1.x * e2.z - d2.x * e1.z) / det;
            float dvdz = (e1.x * d2.y - e2.x * d1.y) / det;
            var scale = transform.lossyScale;
            return new Vector2(dudx / scale.x, dvdz / scale.z);
        }

        static Texture2D WaveTexture(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var deep = new Color(0.06f, 0.28f, 0.48f);
            var light = new Color(0.22f, 0.55f, 0.75f);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    // Tileable waves: sums of whole-period sines.
                    float w = Mathf.Sin((u * 3 + v * 1) * Mathf.PI * 2) * 0.35f
                            + Mathf.Sin((u * 1 - v * 4) * Mathf.PI * 2 + 1.3f) * 0.25f
                            + Mathf.Sin((u * 7 + v * 5) * Mathf.PI * 2 + 0.4f) * 0.15f;
                    float k = Mathf.Clamp01(0.5f + w * 0.6f);
                    k = k > 0.86f ? 1f : k * 0.8f;   // thin bright crests
                    px[y * n + x] = Color.Lerp(deep, light, k);
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }
    }
}
