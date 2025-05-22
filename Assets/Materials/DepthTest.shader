Shader "Unlit/ShowDepthMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            Texture2DArray_half _PreprocessedEnvironmentDepthTexture;
            SamplerState sampler_PreprocessedEnvironmentDepthTexture;
            Texture2DArray_half _EnvironmentDepthTexture;
            SamplerState sampler_EnvironmentDepthTexture;
            uniform float4 _EnvironmentDepthZBufferParams;
            
            struct Attribures
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;                
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;            

            Interpolators vert (Attribures v)
            {
                Interpolators o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(Interpolators, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);                
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 
                return o;
            }            
            
            fixed4 frag (Interpolators i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 uv = float3(i.uv, 0);//just show one texture so we don't get eye strain
                // fixed4 col = _PreprocessedEnvironmentDepthTexture.Sample(sampler_PreprocessedEnvironmentDepthTexture, uv);
                //Uncomment the line below and comment the line above to change between the preprocessed depth texture and the raw depth texture
                
                float col = _EnvironmentDepthTexture.Sample(sampler_EnvironmentDepthTexture, uv).r;

                const float inputDepthNdc = col * 2.0 - 1.0;
                const float linearDepth = (1.0f / (inputDepthNdc + _EnvironmentDepthZBufferParams.y)) * _EnvironmentDepthZBufferParams.x;
                
                return fixed4(linearDepth,linearDepth,linearDepth,1)/10;
            }
            ENDCG
        }
    }
}