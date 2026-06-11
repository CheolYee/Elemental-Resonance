using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SpellsPackUrpConverter
{
    private const string BuiltinGuid    = "0000000000000000f000000000000000";
    private const string FileIdStandard = "fileID: 210,";
    private const string FileIdParticle = "fileID: 211,";
    private const string ShaderParticleUnlit = "Universal Render Pipeline/Particles/Unlit";
    private const string ShaderLit           = "Universal Render Pipeline/Lit";

    // Step 1: Built-in RP → URP 쉐이더 교체
    [MenuItem("Tools/Spells Pack/1. Convert Shaders to URP")]
    public static void ConvertShaders()
    {
        var shaderParticle = Shader.Find(ShaderParticleUnlit);
        var shaderLit      = Shader.Find(ShaderLit);
        if (shaderParticle == null || shaderLit == null)
        {
            Debug.LogError("[SpellsPackConverter] URP 쉐이더를 찾을 수 없습니다. URP 패키지 확인.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Spells Pack" });
        int converted = 0, skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string text = File.ReadAllText(path);
            if (!text.Contains(BuiltinGuid)) { skipped++; continue; }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (text.Contains(FileIdStandard))
                mat.shader = shaderLit;
            else if (text.Contains(FileIdParticle))
                mat.shader = shaderParticle;
            else
            {
                Debug.LogWarning($"[Unknown] {System.IO.Path.GetFileName(path)} — 수동 확인 필요");
                skipped++; continue;
            }

            EditorUtility.SetDirty(mat);
            Debug.Log($"[Converted] {System.IO.Path.GetFileName(path)}");
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"=== 쉐이더 교체 완료: {converted}개 변환, {skipped}개 건너뜀 ===");
        Debug.Log("다음 단계: Tools > Spells Pack > 2. Fix Blend Modes & Textures 실행");
    }

    // Step 2: 블렌드 모드, 텍스처, 색상 마이그레이션 수정
    [MenuItem("Tools/Spells Pack/2. Fix Blend Modes & Textures")]
    public static void FixBlendModesAndTextures()
    {
        var urpParticleShader = Shader.Find(ShaderParticleUnlit);
        if (urpParticleShader == null)
        {
            Debug.LogError("[SpellsPackFixer] URP Particles/Unlit 쉐이더를 찾을 수 없습니다.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Spells Pack" });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader != urpParticleShader) continue;

            string fileText = File.ReadAllText(path);
            bool changed = FixParticleMaterial(mat, fileText);

            if (changed)
            {
                EditorUtility.SetDirty(mat);
                fixedCount++;
                Debug.Log($"[Fixed] {System.IO.Path.GetFileName(path)}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"=== 블렌드 모드 수정 완료: {fixedCount}개 수정됨 ===");
    }

    private static bool FixParticleMaterial(Material mat, string fileText)
    {
        bool changed = false;

        // 1. Surface Type → Transparent (1)
        if (!Mathf.Approximately(mat.GetFloat("_Surface"), 1f))
        {
            mat.SetFloat("_Surface", 1f);
            changed = true;
        }

        // 2. 블렌드 모드 결정 (m_InvalidKeywords에 원본 키워드가 보존됨)
        int blendMode = ResolveBlendMode(fileText, mat);
        if (!Mathf.Approximately(mat.GetFloat("_Blend"), blendMode))
        {
            mat.SetFloat("_Blend", blendMode);
            changed = true;
        }

        // 3. 블렌드 팩터 적용
        changed |= ApplyBlendFactors(mat, blendMode);

        // 4. ZWrite → 0 (투명 오브젝트는 깊이 쓰기 비활성화)
        if (!Mathf.Approximately(mat.GetFloat("_ZWrite"), 0f))
        {
            mat.SetFloat("_ZWrite", 0f);
            changed = true;
        }

        // 5. 렌더 큐 → 3000 (Transparent)
        if (mat.renderQueue != 3000)
        {
            mat.renderQueue = 3000;
            changed = true;
        }

        // 6. RenderType 태그 → Transparent
        if (mat.GetTag("RenderType", false, "") != "Transparent")
        {
            mat.SetOverrideTag("RenderType", "Transparent");
            changed = true;
        }

        // 7. _MainTex → _BaseMap 텍스처 마이그레이션
        // GetTexture("_MainTex") 폴백: URP 쉐이더가 _MainTex를 expose하지 않을 경우 YAML에서 직접 읽음
        if (mat.GetTexture("_BaseMap") == null)
        {
            var mainTex = mat.GetTexture("_MainTex") ?? GetTextureFromYaml(fileText, "_MainTex");
            if (mainTex != null)
            {
                mat.SetTexture("_BaseMap", mainTex);
                changed = true;
            }
        }

        // 8. _Color → _BaseColor 색상 마이그레이션 (흰색이 아닌 경우)
        var origColor = mat.GetColor("_Color");
        var baseColor = mat.GetColor("_BaseColor");
        if (origColor != Color.white && baseColor == Color.white)
        {
            mat.SetColor("_BaseColor", origColor);
            changed = true;
        }

        // 9. ColorMode 마이그레이션: _ColorAddSubDiff → _ColorMode + _BaseColorAddSubDiff
        changed |= MigrateColorMode(mat);

        // 10. URP 키워드 활성화
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        switch (blendMode)
        {
            case 1: mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"); break;
            case 3: mat.EnableKeyword("_ALPHAMODULATE_ON"); break;
            default:
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.DisableKeyword("_ALPHAMODULATE_ON");
                break;
        }

        if (mat.HasProperty("_SoftParticlesEnabled") && mat.GetFloat("_SoftParticlesEnabled") > 0.5f)
            mat.EnableKeyword("_SOFTPARTICLES_ON");

        return changed;
    }

    // m_InvalidKeywords에 원본 Built-in RP 키워드가 보존됨 → 블렌드 모드 역추적
    private static int ResolveBlendMode(string fileText, Material mat)
    {
        if (fileText.Contains("_ALPHAMODULATE_ON")) return 3; // Multiply
        if (fileText.Contains("_ALPHAPREMULTIPLY_ON")) return 1; // Premultiply
        if (fileText.Contains("_ALPHABLEND_ON")) return 0; // Alpha

        // 폴백: _Mode 값으로 추정 (0=Opaque/Additive, 2=Fade/Alpha, 3=Transparent/Premultiply)
        if (mat.HasProperty("_Mode"))
        {
            float mode = mat.GetFloat("_Mode");
            if (mode >= 2.9f) return 1; // Mode 3 → Premultiply
            if (mode >= 1.9f) return 0; // Mode 2 (Fade) → Alpha
        }

        // 키워드도 Mode도 없으면 Additive로 간주 (기본 마법 이펙트)
        return 2;
    }

    private static bool ApplyBlendFactors(Material mat, int blendMode)
    {
        bool changed = false;
        float src, dst, srcA, dstA;

        switch (blendMode)
        {
            case 0: src = 5f; dst = 10f; srcA = 1f; dstA = 10f; break; // Alpha
            case 1: src = 1f; dst = 10f; srcA = 1f; dstA = 10f; break; // Premultiply
            case 2: src = 5f; dst = 1f;  srcA = 1f; dstA = 1f;  break; // Additive
            case 3: src = 6f; dst = 0f;  srcA = 0f; dstA = 1f;  break; // Multiply
            default: return false;
        }

        if (!Mathf.Approximately(mat.GetFloat("_SrcBlend"), src)) { mat.SetFloat("_SrcBlend", src); changed = true; }
        if (!Mathf.Approximately(mat.GetFloat("_DstBlend"), dst)) { mat.SetFloat("_DstBlend", dst); changed = true; }

        if (mat.HasProperty("_SrcBlendAlpha") && !Mathf.Approximately(mat.GetFloat("_SrcBlendAlpha"), srcA))
        { mat.SetFloat("_SrcBlendAlpha", srcA); changed = true; }
        if (mat.HasProperty("_DstBlendAlpha") && !Mathf.Approximately(mat.GetFloat("_DstBlendAlpha"), dstA))
        { mat.SetFloat("_DstBlendAlpha", dstA); changed = true; }

        return changed;
    }

    // YAML 파일 텍스트에서 직접 텍스처 GUID를 추출 (GetTexture 폴백용)
    private static Texture GetTextureFromYaml(string fileText, string propertyName)
    {
        // "- _PropertyName:\n        m_Texture: {fileID: NNN, guid: GUID, type: N}"
        var match = Regex.Match(fileText,
            $@"-\s+{Regex.Escape(propertyName)}:.*?m_Texture:\s+\{{fileID:\s+\d+,\s+guid:\s+([a-f0-9]{{32}})",
            RegexOptions.Singleline);
        if (!match.Success) return null;

        string texPath = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
        return string.IsNullOrEmpty(texPath) ? null : AssetDatabase.LoadAssetAtPath<Texture>(texPath);
    }

    // Built-in RP의 _ColorAddSubDiff를 URP의 _ColorMode + _BaseColorAddSubDiff로 마이그레이션
    private static bool MigrateColorMode(Material mat)
    {
        if (!mat.HasProperty("_ColorAddSubDiff") || !mat.HasProperty("_ColorMode")) return false;

        bool changed = false;
        var colorAddSubDiff = mat.GetColor("_ColorAddSubDiff");

        // r > 0 → Additive(1), r < 0 → Subtractive(2), otherwise Multiply(0)
        int colorMode = colorAddSubDiff.r > 0.5f ? 1 : colorAddSubDiff.r < -0.5f ? 2 : 0;

        if (!Mathf.Approximately(mat.GetFloat("_ColorMode"), colorMode))
        {
            mat.SetFloat("_ColorMode", colorMode);
            changed = true;
        }

        if (mat.HasProperty("_BaseColorAddSubDiff"))
        {
            var currentBase = mat.GetColor("_BaseColorAddSubDiff");
            if (currentBase != colorAddSubDiff)
            {
                mat.SetColor("_BaseColorAddSubDiff", colorAddSubDiff);
                changed = true;
            }
        }

        return changed;
    }
}
