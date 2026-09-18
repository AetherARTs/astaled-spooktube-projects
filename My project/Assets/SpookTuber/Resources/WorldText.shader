Shader "SpookTuber/World Text"
{
    Properties { _MainTex("Font atlas",2D)="white"{} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Vertex { float4 position:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Pixel { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Pixel Vert(Vertex v) { Pixel o; o.position=TransformObjectToHClip(v.position.xyz); o.uv=v.uv; o.color=v.color; return o; }
            half4 Frag(Pixel p):SV_Target { return half4(p.color.rgb,p.color.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,p.uv).a); }
            ENDHLSL
        }
    }
}
