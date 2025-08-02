using UnityEngine;
using UnityEngine.Rendering;

public class OceanWaves : MonoBehaviour {

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Wave {
        public Vector2 direction;
        public float amplitude;
        public float waveLength;
        public float speed; 
        public float phaseOffset; 
    };

    public Camera cam;
    public Shader oceanShader;
    public Mesh oceanMesh;
    public Material oceanMaterial;

    public float edgeLength = 1.0f;
    public float edgeResolution = 1.0f;

    public int numWaves = 1;
    public int randomSeed = 0;
    public float speedFactor = 1.0f;
    public float baseWaveAmplitude = 1.0f;
    public float baseWaveAmplDamp = 0.7f;
    public float baseWaveLength = 0.5f;
    public float baseWaveLengthDampMin = 0.4f;
    public float baseWaveLengthDampMax = 0.8f;

    public Vector3 sunDir = new Vector3(-0.3f, -1f, -0.2f);
    public Vector4 sunColor = new Color(0.1f, 0.3f, 0.75f, 1.0f);

    public Vector4 specColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public float specPower = 256.0f;

    public float fresnelPower = 5.0f;

    public Vector4 shallowColor = new Color(0.2f, 0.5f, 0.7f, 1.0f);
    public Vector4 deepColor = new Color(0.0f, 0.1f, 0.3f, 1.0f);
    public float depthFadeDistance = 100.0f;

    public float foamSharpness = 4.0f;
    public float foamIntensity = 0.5f;

    public bool updateShader = false;

    private Wave[] waves;
    private GraphicsBuffer wavesBuffer;

    void Start() {
        GeneratePlane();
        GenerateMaterial();
    }

    void Update() {
        if (updateShader) {
            GenerateMaterial();
            updateShader = false;
        }
    }

    void GeneratePlane() {
        int numQuadsPerEdge = (int) (edgeLength / edgeResolution);
        int numVerticesPerEdge = 1 + numQuadsPerEdge; 
        int numQuads = numQuadsPerEdge * numQuadsPerEdge;
        int numTriangles = numQuads * 2;
        Vector3 startPos = gameObject.transform.position - new Vector3(edgeLength / 2, 0.0f, edgeLength / 2);

        Vector3[] vertices = new Vector3[numVerticesPerEdge * numVerticesPerEdge];
        for (int rowIdx = 0; rowIdx < numVerticesPerEdge; rowIdx++) {
            for (int colIdx = 0; colIdx < numVerticesPerEdge; colIdx++) {
                float xOff = startPos.x + edgeResolution * colIdx;
                float zOff = startPos.z + edgeResolution * rowIdx;
                int vertexIdx = rowIdx * numVerticesPerEdge + colIdx;
                vertices[vertexIdx] = new Vector3(xOff, startPos.y, zOff);
            }
        }

        int[] indices = new int[numTriangles * 3];
        for (int rowIdx = 0; rowIdx < numQuadsPerEdge; rowIdx++) {
            for (int colIdx = 0; colIdx < numQuadsPerEdge; colIdx++) {
                int bottomLeft = rowIdx * numVerticesPerEdge + colIdx;
                int bottomRight = rowIdx * numVerticesPerEdge + colIdx + 1;
                int topLeft = (rowIdx + 1) * numVerticesPerEdge + colIdx;
                int topRight = (rowIdx + 1) * numVerticesPerEdge + colIdx + 1;

                int quadIdx = (rowIdx * numQuadsPerEdge + colIdx) * 6;
                indices[quadIdx] = bottomLeft;
                indices[quadIdx + 1] = topLeft;
                indices[quadIdx + 2] = bottomRight;

                indices[quadIdx + 3] = topLeft;
                indices[quadIdx + 4] = topRight;
                indices[quadIdx + 5] = bottomRight;
            }
        }

        oceanMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = oceanMesh;
        oceanMesh.name = "OceanWater";
        oceanMesh.indexFormat = IndexFormat.UInt32;
        oceanMesh.vertices = vertices;
        oceanMesh.triangles = indices;
    }

    void GenerateMaterial() {
        if (waves != null) {
            wavesBuffer.Release();
        }

        waves = new Wave[numWaves];
        wavesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numWaves, sizeof(float) * 6);

        Random.seed = randomSeed;
        for (int i = 0; i < waves.Length; i++) {
            float angleRad = Random.Range(0.0f, 0.5f * Mathf.PI);
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;

            float waveLength = Random.Range(
                baseWaveLength * Mathf.Pow(baseWaveLengthDampMin, i),
                baseWaveLength * Mathf.Pow(baseWaveLengthDampMax, i)
            );
            float k = 2f * Mathf.PI / waveLength;
            float amplitude = baseWaveAmplitude * Mathf.Pow(baseWaveAmplDamp, i) * (waveLength / baseWaveLength);
            float speed = Mathf.Sqrt(9.8f * k) * speedFactor;
            float phaseOffset = Random.Range(0f, 2f * Mathf.PI);

            Wave wave;
            wave.direction = dir;
            wave.amplitude = amplitude;
            wave.waveLength = waveLength;
            wave.speed = speed;
            wave.phaseOffset = phaseOffset;

            waves[i] = wave;
        }
        wavesBuffer.SetData(waves);

        oceanMaterial = new Material(oceanShader);
        oceanMaterial.SetInt("_NumWaves", numWaves);
        oceanMaterial.SetBuffer("_Waves", wavesBuffer);
        oceanMaterial.SetVector("_SunDir", sunDir);
        oceanMaterial.SetColor("_SunColor", sunColor);
        oceanMaterial.SetColor("_SpecularColor", specColor);
        oceanMaterial.SetFloat("_SpecularPower", specPower);
        oceanMaterial.SetFloat("_FresnelPower", fresnelPower);
        Cubemap skyCubemap = RenderSettings.skybox?.GetTexture("_Tex") as Cubemap;
        if (skyCubemap != null) {
            oceanMaterial.SetTexture("_SkyboxTex", skyCubemap);
        }
        oceanMaterial.SetColor("_ShallowColor", shallowColor);
        oceanMaterial.SetColor("_DeepColor", deepColor);
        oceanMaterial.SetFloat("_DepthFadeDistance", depthFadeDistance);
        oceanMaterial.SetFloat("_FoamSharpness", foamSharpness);
        oceanMaterial.SetFloat("_FoamIntensity", foamIntensity);
        GetComponent<MeshRenderer>().material = oceanMaterial;
    }

    void OnDestroy() {
        wavesBuffer.Release();
    }
}
