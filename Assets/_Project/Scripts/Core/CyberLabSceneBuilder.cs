using UnityEngine;

namespace CalculusLabEscape
{
    public sealed class CyberLabSceneBuilder : MonoBehaviour
    {
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material cyanMaterial;
        [SerializeField] private Material magentaMaterial;

        private void Awake()
        {
            BuildRoom();
            BuildPlayer();
            BuildTerminals();
            gameObject.AddComponent<LimitsPrototypeGame>();
        }

        private void BuildRoom()
        {
            CreateBlock("Floor", new Vector3(0, -0.25f, 8), new Vector3(24, .5f, 30), floorMaterial);
            CreateBlock("Left Wall", new Vector3(-12, 3.5f, 8), new Vector3(.5f, 7.5f, 30), wallMaterial);
            CreateBlock("Right Wall", new Vector3(12, 3.5f, 8), new Vector3(.5f, 7.5f, 30), wallMaterial);
            CreateBlock("Back Wall", new Vector3(0, 3.5f, -7), new Vector3(24, 7.5f, .5f), wallMaterial);
            CreateBlock("Exit Wall", new Vector3(0, 3.5f, 23), new Vector3(24, 7.5f, .5f), wallMaterial);
            CreateBlock("Exit Door", new Vector3(0, 2.1f, 22.65f), new Vector3(4, 4.2f, .3f), magentaMaterial);

            for (int z = -5; z <= 21; z += 4)
            {
                CreateBlock("Floor Strip", new Vector3(0, .02f, z), new Vector3(20, .03f, .08f), cyanMaterial, false);
                CreateBlock("Ceiling Light", new Vector3(0, 6.6f, z), new Vector3(8, .08f, .18f), cyanMaterial, false);
            }

            RenderSettings.ambientLight = new Color(.1f, .14f, .23f);
            var lightObject = new GameObject("Laboratory Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(.6f, .78f, 1f);
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(55, -25, 0);
        }

        private void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1.05f, -4.5f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .35f;
            controller.center = new Vector3(0, .9f, 0);
            player.AddComponent<LabPlayerController>();

            var cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.55f, 0);
            cameraObject.AddComponent<Camera>().fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
        }

        private void BuildTerminals()
        {
            Vector3[] positions =
            {
                new Vector3(-6.5f, 0, 3), new Vector3(6.5f, 0, 10), new Vector3(-5.5f, 0, 17)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                var pedestal = CreateBlock($"Terminal {i + 1}", positions[i] + Vector3.up * 1.1f,
                    new Vector3(1.8f, 2.2f, 1.2f), cyanMaterial);
                pedestal.transform.rotation = Quaternion.Euler(0, i % 2 == 0 ? 25 : -25, 0);
                var terminal = pedestal.AddComponent<CalculusTerminal>();
                terminal.questionIndex = i;
                CreateBlock("Console Glow", positions[i] + new Vector3(0, 2.35f, 0),
                    new Vector3(2.4f, .12f, 1.4f), magentaMaterial, false);
            }
        }

        private GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            if (material != null) block.GetComponent<Renderer>().material = material;
            if (!collider) Destroy(block.GetComponent<Collider>());
            return block;
        }
    }
}
