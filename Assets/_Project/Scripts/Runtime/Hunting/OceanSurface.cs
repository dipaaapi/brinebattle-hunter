using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Placeholder ocean: a generated wave texture that scrolls with the current, plus white
    /// streaks riding the current so you can see which way drifts will float. The surface itself
    /// rises and falls with <see cref="BrineBattle.Hunting.Waves"/> (owner's ask), on a finer mesh made at start.
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
        Mesh mesh;
        Vector3[] baseVerts, verts;
        public int meshCells = 150;

        Vector3 Dir => spawner != null ? spawner.currentDirection.normalized : Vector3.right;
        float Speed => spawner != null ? spawner.driftSpeed : 1.5f;

        void Start()
        {
            var r = GetComponent<Renderer>();
            mat = r.material;
            BuildMesh();
            var tex = WaveTexture(256);
            const float tiling = 10f;
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

        /// <summary>Swaps the 10×10 Unity plane for a finer grid over the same area so the waves look smooth.</summary>
        void BuildMesh()
        {
            var mf = GetComponent<MeshFilter>();
            var old = mf.sharedMesh;
            var b = old.bounds;
            int n = Mathf.Max(2, meshCells);
            baseVerts = new Vector3[(n + 1) * (n + 1)];
            var uv = new Vector2[baseVerts.Length];
            for (int z = 0; z <= n; z++)
                for (int x = 0; x <= n; x++)
                {
                    float u = x / (float)n, v = z / (float)n;
                    baseVerts[z * (n + 1) + x] = new Vector3(Mathf.Lerp(b.min.x, b.max.x, u), 0f, Mathf.Lerp(b.min.z, b.max.z, v));
                    uv[z * (n + 1) + x] = new Vector2(u, v);
                }
            var tris = new int[n * n * 6];
            for (int z = 0, t = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int i = z * (n + 1) + x;
                    tris[t++] = i; tris[t++] = i + n + 1; tris[t++] = i + 1;
                    tris[t++] = i + 1; tris[t++] = i + n + 1; tris[t++] = i + n + 2;
                }
            mesh = new Mesh { name = "Ocean waves", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            mesh.vertices = baseVerts;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(b.center, b.size + Vector3.up * 2f);
            mf.sharedMesh = mesh;
            verts = (Vector3[])baseVerts.Clone();
        }

        void Swell()
        {
            if (mesh == null) return;
            var tf = transform;
            float sy = Mathf.Max(0.0001f, tf.lossyScale.y);
            for (int i = 0; i < baseVerts.Length; i++)
            {
                var w = tf.TransformPoint(baseVerts[i]);
                verts[i] = new Vector3(baseVerts[i].x, (BrineBattle.Hunting.Waves.Height(w.x, w.z) - tf.position.y) / sy, baseVerts[i].z);
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
        }

        void Update()
        {
            Swell();   // the sea keeps moving even while menus pause the world
            if (WorldPause.Active) return;
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
                var p = s.position;
                p.y = BrineBattle.Hunting.Waves.Height(p.x, p.z) + 0.03f;
                s.position = p;
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
            // A smooth swell (sines in scattered directions) plus thin bright ridges for wave crests.
            // Every wave has a whole number of periods across the tile, so it still repeats seamlessly.
            var rng = new System.Random(7);
            var swell = Waves(rng, 10, -5, 5);
            var ridges = Waves(rng, 6, 1, 7);
            var h = new float[n * n]; var r = new float[n * n];
            float hMax = 0f, rMax = 0f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n, sum = 0f, ridge = 0f;
                    foreach (var w in swell) sum += w.w * Mathf.Sin((w.x * u + w.y * v) * Mathf.PI * 2f + w.z);
                    foreach (var w in ridges) ridge += Mathf.Pow(1f - Mathf.Abs(Mathf.Sin((w.x * u + w.y * v) * Mathf.PI + w.z)), 6f);
                    int i = y * n + x;
                    h[i] = sum; r[i] = ridge;
                    hMax = Mathf.Max(hMax, Mathf.Abs(sum)); rMax = Mathf.Max(rMax, ridge);
                }
            var deep = new Color(0.04f, 0.22f, 0.40f);
            var mid = new Color(0.10f, 0.38f, 0.58f);
            var crest = new Color(0.55f, 0.78f, 0.90f);
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++)
            {
                var c = Color.Lerp(deep, mid, Mathf.SmoothStep(0f, 1f, 0.5f + 0.5f * h[i] / hMax));
                px[i] = Color.Lerp(c, crest, Mathf.SmoothStep(0.35f, 0.9f, r[i] / rMax) * 0.55f);
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Random whole-number wave directions: x, y = frequency, z = phase, w = amplitude.</summary>
        static Vector4[] Waves(System.Random rng, int count, int xMin, int max)
        {
            var list = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                int fx, fy;
                do { fx = rng.Next(xMin, max + 1); fy = rng.Next(-max, max + 1); } while (fx == 0 && fy == 0);
                list[i] = new Vector4(fx, fy, (float)rng.NextDouble() * Mathf.PI * 2f, 1f / Mathf.Sqrt(fx * fx + fy * fy));
            }
            return list;
        }
    }
}
