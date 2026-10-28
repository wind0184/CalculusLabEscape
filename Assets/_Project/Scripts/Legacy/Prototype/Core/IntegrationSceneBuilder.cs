using UnityEngine;

namespace CalculusLabEscape
{
    public sealed class IntegrationSceneBuilder : MonoBehaviour
    {
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material cyanMaterial;
        [SerializeField] private Material magentaMaterial;

        private void Awake()
        {
            Chapter4World.Build(2);
        }

        private void BuildRoom()
        {
            CreateBlock("Entry Floor", new Vector3(0, -.25f, -3), new Vector3(22, .5f, 12), floorMaterial);
            CreateBlock("Exit Floor", new Vector3(0, -.25f, 20), new Vector3(22, .5f, 8), floorMaterial);
            CreateBlock("Left Wall", new Vector3(-11, 3.5f, 8), new Vector3(.5f, 7.5f, 32), wallMaterial);
            CreateBlock("Right Wall", new Vector3(11, 3.5f, 8), new Vector3(.5f, 7.5f, 32), wallMaterial);
            CreateBlock("Back Wall", new Vector3(0, 3.5f, -9), new Vector3(22, 7.5f, .5f), wallMaterial);
            CreateBlock("Exit Wall", new Vector3(0, 3.5f, 24), new Vector3(22, 7.5f, .5f), wallMaterial);
            for (int z = -7; z <= 23; z += 5)
                CreateBlock("Energy Rail", new Vector3(0, 6.5f, z), new Vector3(9, .08f, .18f), cyanMaterial, false);

            RenderSettings.ambientLight = new Color(.09f, .1f, .2f);
            GameObject lightObject = new GameObject("Integration Lab Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(.65f, .72f, 1f);
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(55, -25, 0);
        }

        private void BuildPlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1.05f, -6f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .35f;
            controller.center = new Vector3(0, .9f, 0);
            player.AddComponent<LabPlayerController>();
            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.55f, 0);
            cameraObject.AddComponent<Camera>().fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
        }

        private void BuildPuzzle()
        {
            GameObject terminal = CreateBlock("Accumulation Terminal", new Vector3(0, 1.1f, 1f), new Vector3(2.4f, 2.2f, 1.4f), cyanMaterial);
            Transform[] bridgeSegments = new Transform[6];
            for (int i = 0; i < bridgeSegments.Length; i++)
            {
                float z = 6f + i * 2.2f;
                GameObject segment = CreateBlock($"Energy Bridge {i + 1}", new Vector3(0, -2.2f, z), new Vector3(5.5f, .35f, 1.8f), cyanMaterial);
                bridgeSegments[i] = segment.transform;
            }
            GameObject door = CreateBlock("Exit Door", new Vector3(0, 2.1f, 23.65f), new Vector3(4, 4.2f, .3f), magentaMaterial);
            IntegrationPrototypeGame game = gameObject.AddComponent<IntegrationPrototypeGame>();
            game.Configure(terminal.transform, door.transform, bridgeSegments);
        }

        private GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            if (material != null) block.GetComponent<Renderer>().material = material;
            if (!collider) Destroy(block.GetComponent<Collider>());
            return block;
        }
    }
}
