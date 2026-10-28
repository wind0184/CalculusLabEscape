using UnityEngine;

namespace CalculusLabEscape
{
    /// <summary>Keeps depth-tested 3D labels bound to the live built-in font atlas.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TextMesh))]
    public sealed class WorldLabelAtlas : MonoBehaviour
    {
        public Material depthMaterial;
        TextMesh label;
        Renderer target;
        Texture atlas;
        MaterialPropertyBlock properties;

        void OnEnable() { atlas = null; Apply(); }
        void OnValidate() { atlas = null; Apply(); }
        void LateUpdate() => Apply();

        public void Apply()
        {
            if (label == null) label = GetComponent<TextMesh>();
            if (target == null) target = GetComponent<Renderer>();
            if (depthMaterial == null || label == null || label.font == null || target == null) return;
            var texture = label.font.material.mainTexture;
            if (texture == null) return;
            if (target.sharedMaterial != depthMaterial) target.sharedMaterial = depthMaterial;
            if (atlas == texture && properties != null) return;
            atlas = texture;
            if (properties == null) properties = new MaterialPropertyBlock();
            target.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex", texture);
            target.SetPropertyBlock(properties);
        }
    }
}
