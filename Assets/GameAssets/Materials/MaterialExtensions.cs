using UnityEngine;

public static class MaterialExtensions
{
    public static void SetTransparentMode(this Material material)
    {
        // Äëÿ URP
        material.SetFloat("_Surface", 1f); // 1 = Transparent, 0 = Opaque
        material.SetFloat("_Blend", 0f);   // 0 = Alpha, 1 = Premultiply
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}