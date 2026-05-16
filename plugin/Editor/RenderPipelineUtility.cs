using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Identifies the active render pipeline type.
    /// </summary>
    public enum RenderPipelineType
    {
        BuiltIn,
        URP,
        HDRP
    }

    /// <summary>
    /// Utility for detecting the active render pipeline and providing
    /// shader compatibility information. Used by material/shader tools
    /// to select appropriate defaults and suggest alternatives.
    /// </summary>
    public static class RenderPipelineUtility
    {
        // Known shader names per pipeline for alternative suggestions
        private static readonly Dictionary<RenderPipelineType, List<string>> PipelineShaders =
            new Dictionary<RenderPipelineType, List<string>>
            {
                {
                    RenderPipelineType.BuiltIn, new List<string>
                    {
                        "Standard",
                        "Standard (Specular setup)",
                        "Unlit/Color",
                        "Unlit/Texture",
                        "Mobile/Diffuse",
                        "Legacy Shaders/Diffuse"
                    }
                },
                {
                    RenderPipelineType.URP, new List<string>
                    {
                        "Universal Render Pipeline/Lit",
                        "Universal Render Pipeline/Simple Lit",
                        "Universal Render Pipeline/Unlit",
                        "Universal Render Pipeline/Baked Lit",
                        "Universal Render Pipeline/Particles/Lit",
                        "Universal Render Pipeline/Particles/Unlit"
                    }
                },
                {
                    RenderPipelineType.HDRP, new List<string>
                    {
                        "HDRP/Lit",
                        "HDRP/Unlit",
                        "HDRP/LitTessellation",
                        "HDRP/Eye",
                        "HDRP/Hair",
                        "HDRP/Decal"
                    }
                }
            };

        /// <summary>
        /// Detects the active render pipeline by inspecting GraphicsSettings.currentRenderPipeline.
        /// Returns BuiltIn if no SRP asset is assigned, URP if the asset type name contains
        /// "Universal", or HDRP if it contains "HDRenderPipeline" or "HDRP".
        /// </summary>
        /// <returns>The detected render pipeline type.</returns>
        public static RenderPipelineType GetActiveRenderPipeline()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;

            if (pipelineAsset == null)
            {
                return RenderPipelineType.BuiltIn;
            }

            string typeName = pipelineAsset.GetType().Name;

            if (typeName.Contains("Universal"))
            {
                return RenderPipelineType.URP;
            }

            if (typeName.Contains("HDRenderPipeline") || typeName.Contains("HDRP"))
            {
                return RenderPipelineType.HDRP;
            }

            // Fallback: if we can't identify the SRP, treat as Built-in
            return RenderPipelineType.BuiltIn;
        }

        /// <summary>
        /// Returns the default lit shader name for the active render pipeline.
        /// Built-in: "Standard", URP: "Universal Render Pipeline/Lit", HDRP: "HDRP/Lit".
        /// </summary>
        /// <returns>The default shader name for the active pipeline.</returns>
        public static string GetDefaultShaderName()
        {
            return GetDefaultShaderName(GetActiveRenderPipeline());
        }

        /// <summary>
        /// Returns the default lit shader name for the specified render pipeline.
        /// </summary>
        /// <param name="pipeline">The render pipeline type.</param>
        /// <returns>The default shader name for the specified pipeline.</returns>
        public static string GetDefaultShaderName(RenderPipelineType pipeline)
        {
            switch (pipeline)
            {
                case RenderPipelineType.URP:
                    return "Universal Render Pipeline/Lit";
                case RenderPipelineType.HDRP:
                    return "HDRP/Lit";
                case RenderPipelineType.BuiltIn:
                default:
                    return "Standard";
            }
        }

        /// <summary>
        /// Returns up to 3 alternative shader names compatible with the active pipeline
        /// when the requested shader is incompatible or unavailable.
        /// The alternatives exclude the requested shader name itself.
        /// </summary>
        /// <param name="requestedShader">The shader name that was requested but is incompatible.</param>
        /// <returns>A list of up to 3 compatible alternative shader names.</returns>
        public static List<string> GetAlternativeShaders(string requestedShader)
        {
            return GetAlternativeShaders(requestedShader, GetActiveRenderPipeline());
        }

        /// <summary>
        /// Returns up to 3 alternative shader names compatible with the specified pipeline
        /// when the requested shader is incompatible or unavailable.
        /// </summary>
        /// <param name="requestedShader">The shader name that was requested but is incompatible.</param>
        /// <param name="pipeline">The render pipeline to suggest alternatives for.</param>
        /// <returns>A list of up to 3 compatible alternative shader names.</returns>
        public static List<string> GetAlternativeShaders(string requestedShader, RenderPipelineType pipeline)
        {
            var alternatives = new List<string>();

            if (!PipelineShaders.ContainsKey(pipeline))
            {
                return alternatives;
            }

            var shaders = PipelineShaders[pipeline];

            foreach (var shader in shaders)
            {
                if (alternatives.Count >= 3)
                {
                    break;
                }

                if (shader != requestedShader)
                {
                    alternatives.Add(shader);
                }
            }

            return alternatives;
        }

        /// <summary>
        /// Checks if a shader is compatible with the active render pipeline.
        /// A shader is considered compatible if it belongs to the known shader list
        /// for the active pipeline, or if it is a universal shader (e.g., "Unlit/" prefix
        /// shaders that work across pipelines).
        /// </summary>
        /// <param name="shaderName">The shader name to check.</param>
        /// <returns>True if the shader is compatible with the active pipeline, false otherwise.</returns>
        public static bool IsShaderCompatible(string shaderName)
        {
            return IsShaderCompatible(shaderName, GetActiveRenderPipeline());
        }

        /// <summary>
        /// Checks if a shader is compatible with the specified render pipeline.
        /// </summary>
        /// <param name="shaderName">The shader name to check.</param>
        /// <param name="pipeline">The render pipeline to check compatibility against.</param>
        /// <returns>True if the shader is compatible with the specified pipeline, false otherwise.</returns>
        public static bool IsShaderCompatible(string shaderName, RenderPipelineType pipeline)
        {
            if (string.IsNullOrEmpty(shaderName))
            {
                return false;
            }

            // Check if the shader is in the known list for the active pipeline
            if (PipelineShaders.ContainsKey(pipeline))
            {
                var shaders = PipelineShaders[pipeline];
                if (shaders.Contains(shaderName))
                {
                    return true;
                }
            }

            // Check pipeline-specific prefixes for compatibility
            switch (pipeline)
            {
                case RenderPipelineType.BuiltIn:
                    // Built-in pipeline shaders: Standard, Legacy, Mobile, Unlit, Particles, Nature, etc.
                    return !shaderName.StartsWith("Universal Render Pipeline/") &&
                           !shaderName.StartsWith("HDRP/");

                case RenderPipelineType.URP:
                    // URP shaders start with "Universal Render Pipeline/"
                    // Also compatible: Hidden/, Shader Graphs/, Sprites/
                    return !shaderName.StartsWith("HDRP/") &&
                           !IsBuiltInOnlyShader(shaderName);

                case RenderPipelineType.HDRP:
                    // HDRP shaders start with "HDRP/"
                    // Also compatible: Hidden/, Shader Graphs/
                    return !shaderName.StartsWith("Universal Render Pipeline/") &&
                           !IsBuiltInOnlyShader(shaderName);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a shader is exclusive to the Built-in render pipeline
        /// and would not work in URP or HDRP.
        /// </summary>
        private static bool IsBuiltInOnlyShader(string shaderName)
        {
            return shaderName == "Standard" ||
                   shaderName == "Standard (Specular setup)" ||
                   shaderName.StartsWith("Legacy Shaders/") ||
                   shaderName.StartsWith("Mobile/") ||
                   shaderName.StartsWith("Nature/");
        }
    }
}
