Shader "Hidden/IdleBlade/RigSprite" {
 Properties { _MainTex("Texture",2D)="white"{} }
 SubShader { Tags {"Queue"="Transparent"} Cull Off ZWrite Off ZTest Always
 Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _BoneClip;float2 _BoneRange;
 struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
 Output vert(Input i){Output o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
 fixed4 frag(Output i):SV_Target{float length2=dot(_BoneClip.zw,_BoneClip.zw);if(length2>0.000001){float t=dot(i.uv-_BoneClip.xy,_BoneClip.zw)/length2;clip(t-_BoneRange.x);clip(_BoneRange.y-t);}return tex2D(_MainTex,i.uv);}
 ENDCG }
 }
}
