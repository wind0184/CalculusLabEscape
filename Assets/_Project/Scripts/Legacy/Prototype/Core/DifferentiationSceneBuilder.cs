using UnityEngine;

namespace CalculusLabEscape
{
    public sealed class DifferentiationSceneBuilder : MonoBehaviour
    {
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material cyanMaterial;
        [SerializeField] private Material magentaMaterial;

        private void Awake()
        {
            Chapter4World.Build(1);
        }

        private void BuildRoom()
        {
            CreateBlock("Floor", new Vector3(0, -.25f, 7), new Vector3(24, .5f, 28), floorMaterial);
            CreateBlock("Left Wall", new Vector3(-12, 3.5f, 7), new Vector3(.5f, 7.5f, 28), wallMaterial);
            CreateBlock("Right Wall", new Vector3(12, 3.5f, 7), new Vector3(.5f, 7.5f, 28), wallMaterial);
            CreateBlock("Back Wall", new Vector3(0, 3.5f, -7), new Vector3(24, 7.5f, .5f), wallMaterial);
            CreateBlock("Exit Wall", new Vector3(0, 3.5f, 21), new Vector3(24, 7.5f, .5f), wallMaterial);
            for (int z = -5; z <= 19; z += 4)
            {
                CreateBlock("Rail Light", new Vector3(0, .03f, z), new Vector3(20, .04f, .09f), cyanMaterial, false);
                CreateBlock("Ceiling Light", new Vector3(0, 6.6f, z), new Vector3(8, .08f, .18f), cyanMaterial, false);
            }
            RenderSettings.ambientLight = new Color(.1f, .14f, .23f);
            GameObject lightObject = new GameObject("Laboratory Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.color = new Color(.6f, .78f, 1f); light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(55, -25, 0);
        }

        private void BuildPlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1.05f, -4.5f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = .35f; controller.center = new Vector3(0, .9f, 0);
            player.AddComponent<LabPlayerController>();
            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.55f, 0);
            cameraObject.AddComponent<Camera>().fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
        }

        private void BuildPuzzle()
        {
            GameObject console = CreateBlock("Trajectory Console", new Vector3(0, 1.1f, 2.5f), new Vector3(2.2f, 2.2f, 1.4f), cyanMaterial);
            CreateBlock("Opposite Rail", new Vector3(0, 1.1f, 13f), new Vector3(18, .18f, .35f), cyanMaterial, false);
            GameObject socketObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            socketObject.name = "Moving Power Socket";
            socketObject.transform.position = new Vector3(-2.8f, 2.1f, 13f);
            socketObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            socketObject.transform.localScale = new Vector3(.65f, .25f, .65f);
            socketObject.GetComponent<Renderer>().material = magentaMaterial;
            Destroy(socketObject.GetComponent<Collider>());
            MovingPowerSocket socket = socketObject.AddComponent<MovingPowerSocket>();

            GameObject cableStart = new GameObject("Cable Launch Point");
            cableStart.transform.position = new Vector3(0, 2.1f, 3.3f);
            GameObject cableObject = new GameObject("Power Cable");
            LineRenderer cable = cableObject.AddComponent<LineRenderer>();
            cable.material = cyanMaterial; cable.startWidth = .12f; cable.endWidth = .12f;
            cable.numCapVertices = 6;

            GameObject door = CreateBlock("Exit Door", new Vector3(0, 2.1f, 20.65f), new Vector3(4, 4.2f, .3f), magentaMaterial);
            DifferentiationPrototypeGame game = gameObject.AddComponent<DifferentiationPrototypeGame>();
            game.Configure(console.transform, cableStart.transform, door.transform, socket, cable);
        }

        private GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName; block.transform.SetPositionAndRotation(position, Quaternion.identity); block.transform.localScale = scale;
            if (material != null) block.GetComponent<Renderer>().material = material;
            if (!collider) Destroy(block.GetComponent<Collider>());
            return block;
        }
    }
}
