using Assets.Scripts.MapEditor.Models.Enums;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.MapEditor.Controllers
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class MapTerrain : MonoBehaviour
    {
        [Header("Resolution settings")]
        [Tooltip("Сколько ВЕРШИН высоты приходится на 1 метр" +
                 " (1 = по-старому, 2 = каждые 0.5 м, 4 = каждые 0.25 м)")]
        [Min(1)] public int heightSubDiv = 1;

        [Tooltip("Сколько ячеек покрытия приходится на 1 метр" +
                 " (4 даст минимальный мазок 0.25 м)")]
        [Min(1)] public int surfaceSubDiv = 4;

        [SerializeField] float cellSize = 1f;                      // базовый метр

        [Header("Surface Colors (vertex)")]
        public Color grassColor = Color.green;
        public Color mudColor = new(.45f, .25f, .10f);
        public Color gravelColor = new(.50f, .50f, .50f);
        public Color waterColor = new(.10f, .30f, .80f);
        public Color iceColor = new(.80f, .90f, 1f);

        // ---------- runtime-поля ----------
        // высоты
        private int _heRes;      // узлов по стороне
        private float _heCell;     // шаг по высоте  (м)
        private float[,] _heights;

        // покрытие
        private int _suRes;      // плиток по стороне
        private float _suCell;     // шаг по покрытию (м)
        private SurfaceType[,] _surface;

        // визуализация
        Mesh _mesh;
        Color[] _vertColors;

        public SurfaceType[,] SurfaceArray => _surface;
        public int HeightResolution => _heRes;
        public int SurfaceResolution => _suRes;
        public float MapHalfWorld => _heRes * _heCell * 0.5f;

        // ------------------------------------------------------------ public API
        public void Init(int meters)
        {
            // высоты
            _heRes = meters * heightSubDiv;
            _heCell = cellSize / heightSubDiv;
            _heights = new float[_heRes + 1, _heRes + 1];

            // покрытие
            _suRes = meters * surfaceSubDiv;
            _suCell = cellSize / surfaceSubDiv;
            _surface = new SurfaceType[_suRes, _suRes];
            for (int x = 0; x < _suRes; x++)
                for (int z = 0; z < _suRes; z++)
                    _surface[x, z] = SurfaceType.Grass;

            GenerateMesh();
            ApplyVertexColors();
        }

        #region ─── helpers ──────────────────────────────────────────────────────────
        public bool InsideXZ(Vector3 p)                       // находится в границах?
        {
            float h = MapHalfWorld;
            return p.x >= -h && p.x <= h && p.z >= -h && p.z <= h;
        }
        #endregion

        #region ─── HEIGHTS ────────────────────────────────────────────────────
        public float[] ExportHeights()
        {
            var flat = new float[(_heRes + 1) * (_heRes + 1)];
            Buffer.BlockCopy(_heights, 0, flat, 0, sizeof(float) * flat.Length);
            return flat;
        }

        public void ImportHeights(int res, float[] flat)
        {
            _heRes = res;
            _heCell = cellSize / heightSubDiv;
            _heights = new float[_heRes + 1, _heRes + 1];
            Buffer.BlockCopy(flat, 0, _heights, 0, sizeof(float) * flat.Length);
            UpdateMesh();
        }

        public float[,] GetHeightsCopy()
        {
            var c = new float[_heRes + 1, _heRes + 1];
            Array.Copy(_heights, c, _heights.Length);
            return c;
        }

        public void SetHeights(float[,] h) 
        { 
            _heights = h; 
            UpdateMesh(); 
        }
        #endregion

        #region ─── SURFACE ────────────────────────────────────────────────────
        public byte[] ExportSurfaces()
        {
            int len = _suRes * _suRes;
            var arr = new byte[len];
            for (int y = 0; y < _suRes; y++)
                for (int x = 0; x < _suRes; x++)
                    arr[y * _suRes + x] = (byte)_surface[x, y];
            return arr;
        }

        public void ImportSurfaces(int res, byte[] data)
        {
            _suRes = res;
            _suCell = cellSize / surfaceSubDiv;
            _surface = new SurfaceType[_suRes, _suRes];

            for (int y = 0; y < _suRes; y++)
                for (int x = 0; x < _suRes; x++)
                    _surface[x, y] = (SurfaceType)data[y * _suRes + x];

            ApplyVertexColors();
        }

        public SurfaceType SurfaceAt(Vector3 worldPos)
        {
            float half = _heRes * _heCell * .5f;
            int sx = Mathf.FloorToInt((worldPos.x + half) / _suCell);
            int sz = Mathf.FloorToInt((worldPos.z + half) / _suCell);
            if (sx < 0 || sz < 0 || sx >= _suRes || sz >= _suRes) return SurfaceType.Grass;
            return _surface[sx, sz];
        }

        public void ModifySurfaceWorld(Vector3 wp, SurfaceType type, float radius)
        {
            float half = _heRes * _heCell * .5f;
            int cx = Mathf.RoundToInt((wp.x + half) / _suCell);
            int cz = Mathf.RoundToInt((wp.z + half) / _suCell);
            int rad = Mathf.CeilToInt(radius / _suCell);

            for (int ix = -rad; ix <= rad; ix++)
                for (int iz = -rad; iz <= rad; iz++)
                {
                    int px = cx + ix, pz = cz + iz;
                    if (px < 0 || pz < 0 || px >= _suRes || pz >= _suRes) continue;
                    if (ix * ix + iz * iz > rad * rad) continue;        // круг
                    _surface[px, pz] = type;
                }
            ApplyVertexColors();
        }

        public void SetSurfaces(SurfaceType[,] src)
        {
            _surface = src;
            ApplyVertexColors();
        }
        #endregion

        #region ─── MESH ───────────────────────────────────────────────────────
        void GenerateMesh()
        {
            _mesh = new Mesh { name = "TerrainMesh" };
            _mesh.indexFormat = IndexFormat.UInt32;


            Vector3[] verts = new Vector3[(_heRes + 1) * (_heRes + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[_heRes * _heRes * 6];

            float half = _heRes * _heCell * .5f;
            int t = 0;
            for (int z = 0; z <= _heRes; z++)
                for (int x = 0; x <= _heRes; x++)
                {
                    int i = z * (_heRes + 1) + x;
                    verts[i] = new Vector3(x * _heCell - half, _heights[x, z], z * _heCell - half);
                    uvs[i] = new Vector2((float)x / _heRes, (float)z / _heRes);

                    if (x < _heRes && z < _heRes)
                    {
                        int a = i, b = i + 1, c = i + _heRes + 1, d = c + 1;
                        tris[t++] = a; tris[t++] = c; tris[t++] = b;
                        tris[t++] = b; tris[t++] = c; tris[t++] = d;
                    }
                }

            _mesh.vertices = verts;
            _mesh.triangles = tris;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();

            _vertColors = new Color[verts.Length];
            _mesh.colors = _vertColors;

            GetComponent<MeshFilter>().mesh = _mesh;
            var mc = GetComponent<MeshCollider>();
            mc.sharedMesh = _mesh;
        }

        void UpdateMesh()
        {
            var v = _mesh.vertices;
            for (int z = 0; z <= _heRes; z++)
                for (int x = 0; x <= _heRes; x++)
                    v[z * (_heRes + 1) + x].y = _heights[x, z];

            _mesh.vertices = v;
            _mesh.RecalculateNormals();
            _mesh.UploadMeshData(false);
            GetComponent<MeshCollider>().sharedMesh = _mesh;
            ApplyVertexColors();
        }
        #endregion

        #region ─── HEIGHT brush ──────────────────────────────────────────────
        public void ModifyWorld(Vector3 wp, float delta, float radius)
        {
            float half = _heRes * _heCell * .5f;
            int cx = Mathf.RoundToInt((wp.x + half) / _heCell);
            int cz = Mathf.RoundToInt((wp.z + half) / _heCell);
            int rad = Mathf.CeilToInt(radius / _heCell);

            for (int ix = -rad; ix <= rad; ix++)
                for (int iz = -rad; iz <= rad; iz++)
                {
                    int px = cx + ix, pz = cz + iz;
                    if (px < 0 || pz < 0 || px > _heRes || pz > _heRes) continue;
                    float fall = 1f - Mathf.Sqrt(ix * ix + iz * iz) / rad;
                    if (fall < 0) continue;
                    _heights[px, pz] += delta * fall;
                }
            UpdateMesh();
        }
        #endregion

        #region ─── VERTEX colours ────────────────────────────────────────────
        void ApplyVertexColors()
        {
            if (_vertColors == null || _vertColors.Length != _mesh.vertexCount)
                _vertColors = new Color[_mesh.vertexCount];

            Vector3[] verts = _mesh.vertices;
            float half = _heRes * _heCell * .5f;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = transform.TransformPoint(verts[i]);
                int sx = Mathf.Clamp(Mathf.FloorToInt((wp.x + half) / _suCell), 0, _suRes - 1);
                int sz = Mathf.Clamp(Mathf.FloorToInt((wp.z + half) / _suCell), 0, _suRes - 1);

                _vertColors[i] = _surface[sx, sz] switch
                {
                    SurfaceType.Grass => grassColor,
                    SurfaceType.Mud => mudColor,
                    SurfaceType.Gravel => gravelColor,
                    SurfaceType.Water => waterColor,
                    SurfaceType.Ice => iceColor,
                    _ => grassColor
                };
            }
            _mesh.colors = _vertColors;
        }
        #endregion
    }
}
