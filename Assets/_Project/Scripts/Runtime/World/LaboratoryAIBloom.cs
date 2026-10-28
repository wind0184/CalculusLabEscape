using UnityEngine;

namespace CalculusLabEscape
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class LaboratoryAIBloom : MonoBehaviour
    {
        public Shader shader;
        [Range(0, 2)] public float intensity = .25f;
        [Range(0, 2)] public float threshold = .8f;
        Material effect;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (shader == null || !shader.isSupported) { Graphics.Blit(source, destination); return; }
            if (effect == null) effect = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var one = RenderTexture.GetTemporary(Mathf.Max(1, source.width / 3), Mathf.Max(1, source.height / 3), 0, RenderTextureFormat.ARGBHalf);
            var two = RenderTexture.GetTemporary(one.width, one.height, 0, RenderTextureFormat.ARGBHalf);
            try
            {
                effect.SetFloat("_Threshold", threshold);
                effect.SetFloat("_Intensity", intensity);
                Graphics.Blit(source, one, effect, 0);
                for (int i = 0; i < 3; i++)
                {
                    effect.SetVector("_BlurDirection", new Vector4(1.5f, 0, 0, 0)); Graphics.Blit(one, two, effect, 1);
                    effect.SetVector("_BlurDirection", new Vector4(0, 1.5f, 0, 0)); Graphics.Blit(two, one, effect, 1);
                }
                effect.SetTexture("_BloomTex", one);
                Graphics.Blit(source, destination, effect, 2);
            }
            finally { RenderTexture.ReleaseTemporary(one); RenderTexture.ReleaseTemporary(two); }
        }
        void OnDisable()
        {
            if (effect == null) return;
            if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
            effect = null;
        }
    }
}
