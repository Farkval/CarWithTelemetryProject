using System;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class MapTerrain : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;
        [Header("Surface Colors (Vertex)")]
        public Color grassColor = Color.green;
        public Color mudColor = new Color(0.45f, 0.25f, 0.1f);
        public Color gravelColor = new Color(0.5f, 0.5f, 0.5f);
        public Color waterColor = new Color(0.1f, 0.3f, 0.8f);
        public Color iceColor = new Color(0.8f, 0.9f, 1f);

        private float[,] _heights;
        private SurfaceType[,] _surface;
        private Mesh _mesh;
        private int _resolution;
        private Color[] _vertColors;

        public SurfaceType[,] SurfaceArray => _surface;

        public void Init(int meters)
        {
            _resolution = meters;
            _heights = new float[_resolution + 1, _resolution + 1];
            _surface = new SurfaceType[_resolution, _resolution];
            // default Grass
            for (int x = 0; x < _resolution; x++)
                for (int z = 0; z < _resolution; z++)
                    _surface[x, z] = SurfaceType.Grass;

            GenerateMesh();
            ApplyVertexColors();
        }

        #region Heights

        public float[,] ExportHeights() => _heights;
        public void ImportHeights(float[,] h) { _heights = h; UpdateMesh(); }

        public float[,] GetHeightsCopy()
        {
            var c = new float[_resolution + 1, _resolution + 1];
            Array.Copy(_heights, c, _heights.Length);
            return c;
        }
        public void SetHeights(float[,] h) { _heights = h; UpdateMesh(); }

        #endregion

        #region Surface

        public byte[] ExportSurfaces()
        {
            int len = _resolution * _resolution;
            var arr = new byte[len];

            for (int y = 0; y < _resolution; y++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    int index = y * _resolution + x;
                    arr[index] = (byte)_surface[x, y];
                }
            }

            return arr;
        }

        public void ImportSurfaces(int res, byte[] data)
        {
            _resolution = res;
            _surface = new SurfaceType[res, res];

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int index = y * res + x;
                    _surface[x, y] = (SurfaceType)data[index];
                }
            }

            ApplyVertexColors();
        }

        public SurfaceType SurfaceAt(Vector3 worldPos)
        {
            float half = _resolution * cellSize * .5f;
            int x = Mathf.FloorToInt((worldPos.x + half) / cellSize);
            int z = Mathf.FloorToInt((worldPos.z + half) / cellSize);
            if (x < 0 || z < 0 || x >= _resolution || z >= _resolution) return SurfaceType.Grass;
            return _surface[x, z];
        }

        public void ModifySurfaceWorld(Vector3 worldPos, SurfaceType type, float radius)
        {
            float half = _resolution * cellSize * .5f;
            int cx = Mathf.RoundToInt((worldPos.x + half) / cellSize);
            int cz = Mathf.RoundToInt((worldPos.z + half) / cellSize);
            int rad = Mathf.CeilToInt(radius / cellSize);
            for (int ix = -rad; ix <= rad; ix++)
                for (int iz = -rad; iz <= rad; iz++)
                {
                    int px = cx + ix, pz = cz + iz;
                    if (px < 0 || pz < 0 || px >= _resolution || pz >= _resolution) continue;
                    if (ix * ix + iz * iz > rad * rad) continue;
                    _surface[px, pz] = type;
                }
            ApplyVertexColors();
        }

        #endregion

        #region Mesh

        private void GenerateMesh()
        {
            _mesh = new Mesh { name = "TerrainMesh" };
            var verts = new Vector3[(_resolution + 1) * (_resolution + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[_resolution * _resolution * 6];
            int t = 0; float half = _resolution * cellSize * .5f;

            for (int z = 0; z <= _resolution; z++)
                for (int x = 0; x <= _resolution; x++)
                {
                    int i = z * (_resolution + 1) + x;
                    verts[i] = new Vector3(x * cellSize - half, _heights[x, z], z * cellSize - half);
                    uvs[i] = new Vector2((float)x / _resolution, (float)z / _resolution);
                    if (x < _resolution && z < _resolution)
                    {
                        int a = i, b = i + 1, c = i + _resolution + 1, d = c + 1;
                        tris[t++] = a; tris[t++] = c; tris[t++] = b;
                        tris[t++] = b; tris[t++] = c; tris[t++] = d;
                    }
                }

            _mesh.vertices = verts;
            _mesh.triangles = tris;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();

            // prepare vertex-colors
            _vertColors = new Color[verts.Length];
            _mesh.colors = _vertColors;

            var mf = GetComponent<MeshFilter>();
            mf.mesh = _mesh;
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        private void UpdateMesh()
        {
            var v = _mesh.vertices;
            for (int z = 0; z <= _resolution; z++)
                for (int x = 0; x <= _resolution; x++)
                {
                    int i = z * (_resolution + 1) + x;
                    v[i].y = _heights[x, z];
                }
            _mesh.vertices = v;
            _mesh.RecalculateNormals();
            _mesh.UploadMeshData(false);
            GetComponent<MeshCollider>().sharedMesh = _mesh;
            ApplyVertexColors();
        }

        // только изменения – вставьте их в уже существующий MapTerrain.cs
        public void SetSurface(SurfaceType[,] src)
        {
            _surface = src;
            ApplyVertexColors();
        }

        public void ApplyVertexColors()      // ← СДЕЛАЛИ public
        {
            if (_vertColors == null || _vertColors.Length != _mesh.vertexCount)
                _vertColors = new Color[_mesh.vertexCount];

            var verts = _mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 wp = transform.TransformPoint(verts[i]);
                SurfaceType st = SurfaceAt(wp);
                _vertColors[i] = st switch
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

        #region Height Brush (он же ModifyWorld)

        public void ModifyWorld(Vector3 worldPos, float delta, float radius)
        {
            float half = _resolution * cellSize * .5f;
            int cx = Mathf.RoundToInt((worldPos.x + half) / cellSize);
            int cz = Mathf.RoundToInt((worldPos.z + half) / cellSize);
            int rad = Mathf.CeilToInt(radius / cellSize);
            for (int ix = -rad; ix <= rad; ix++)
                for (int iz = -rad; iz <= rad; iz++)
                {
                    int px = cx + ix, pz = cz + iz;
                    if (px < 0 || pz < 0 || px > _resolution || pz > _resolution) continue;
                    float fall = 1f - Mathf.Sqrt(ix * ix + iz * iz) / rad;
                    if (fall < 0) continue;
                    _heights[px, pz] += delta * fall;
                }
            UpdateMesh();
        }

        #endregion
    }
}
