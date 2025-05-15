using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class MapTerrain : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;  // шаг крупной сетки (метры)

        private float[,] _heights;    // высоты в узлах
        private Mesh _mesh;
        private int _resolution;

        public void Init(int meters)
        {
            _resolution = meters;
            _heights = new float[_resolution + 1, _resolution + 1];
            GenerateMesh();
        }

        public float[,] ExportHeights() => _heights;

        public void ImportHeights(float[,] h)
        {
            _heights = h;
            _resolution = h.GetLength(0) - 1;
            UpdateMesh();
        }

        void GenerateMesh()
        {
            _mesh = new Mesh 
            { 
                name = "TerrainMesh" 
            };
            Vector3[] verts = new Vector3[(_resolution + 1) * (_resolution + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[_resolution * _resolution * 6];
            int t = 0;
            for (int z = 0; z <= _resolution; z++)
                for (int x = 0; x <= _resolution; x++)
                {
                    int i = z * (_resolution + 1) + x; 
                    float offset = _resolution * cellSize * 0.5f;
                    verts[i] = new Vector3(x * cellSize - offset, 0, z * cellSize - offset);
                    uvs[i] = new Vector2((float)x / _resolution, (float)z / _resolution);
                    if (x < _resolution && z < _resolution)
                    {
                        int a = i; int b = i + 1; 
                        int c = i + _resolution + 1; 
                        int d = c + 1;
                        tris[t++] = a; 
                        tris[t++] = c; 
                        tris[t++] = b;
                        tris[t++] = b; 
                        tris[t++] = c; 
                        tris[t++] = d;
                    }
                }
            _mesh.vertices = verts;
            _mesh.triangles = tris;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();
            GetComponent<MeshFilter>().mesh = _mesh;
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        void UpdateMesh()
        {
            var v = _mesh.vertices;
            for (int z = 0; z <= _resolution; z++)
            {
                for (int x = 0; x <= _resolution; x++)
                {
                    int i = z * (_resolution + 1) + x;
                    v[i].y = _heights[x, z];
                }
            }
            _mesh.vertices = v;
            _mesh.RecalculateNormals();
            _mesh.UploadMeshData(false);
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        public void ModifyWorld(Vector3 worldPos, float delta, float rad)
        {
            float half = _resolution * cellSize * 0.5f;
            int x = Mathf.RoundToInt((worldPos.x + half) / cellSize);
            int z = Mathf.RoundToInt((worldPos.z + half) / cellSize);
            ModifyGrid(x, z, delta, rad / cellSize);
        }

        void ModifyGrid(int gx, int gz, float delta, float radius)
        {
            int rad = Mathf.CeilToInt(radius);
            for (int ix = -rad; ix <= rad; ix++)
            {
                for (int iz = -rad; iz <= rad; iz++)
                {
                    int px = gx + ix, pz = gz + iz;
                    if (px < 0 || pz < 0 || px > _resolution || pz > _resolution)
                        continue;

                    float falloff = 1f - Mathf.Sqrt(ix * ix + iz * iz) / radius;
                    if (falloff < 0)
                        continue;

                    _heights[px, pz] += delta * falloff;
                }
            }
            UpdateMesh();
        }
    }
}
