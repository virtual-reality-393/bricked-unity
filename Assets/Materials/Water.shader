Shader "Custom/Water"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    {

    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                float4 worldPos : TEXCOORD0;
            };

            static uint SPHERE_COUNT = 16;

            float4 _Spheres[16];



            bool CheckSphere(float3 testPoint, float4 sphere)
            {
                return distance(testPoint.xyz,sphere.xyz) < sphere.w;
            }

            // The vertex shader definition with properties defined in the Varyings
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous clip space.
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.worldPos = mul(unity_ObjectToWorld,IN.positionOS);
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.
            half4 frag(Varyings v) : SV_Target
            {
                float4 customColor = float4(0,0.8,0,1);


                for (int i = 0; i < SPHERE_COUNT; ++i)
                {
                    if (CheckSphere(v.worldPos.xyz,_Spheres[i]))
                    {
                        customColor = float4(0,0.3,1,1);
                    }
                }
                

                return customColor;
            }
            ENDHLSL
        }
    }
}