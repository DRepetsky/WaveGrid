Shader "WaveGrid/GridOnSurface" {
	SubShader {
        Pass {
            Fog { Mode off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"

			struct Wave{
				float3 pos;
				float phase;
			};

			uniform StructuredBuffer<Wave> wavesBuffer;
			uniform float waveSpeed;
			uniform float framePeriod;
			uniform uint oscillatorsCount;
			uniform uint wavesCount;
			uniform uint curWaveIndex;


            struct v2f {
                float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float4 frag(v2f f) : COLOR {
				const float M_2PI = 3.14159265 * 2;

				float3 worldPos = f.worldPos;
				float frameRadius = waveSpeed * framePeriod;
				float maxRadius = frameRadius * (wavesCount - 1);

				float result = 0;
				for (uint i = 0; i < oscillatorsCount; i++)
				{
					uint oscRow = i * wavesCount;
					float curRadius = maxRadius - frameRadius;
					Wave prevWave = wavesBuffer[oscRow + curWaveIndex];
					//prevWave.phase = 0;
					float prevDist = curRadius - distance(worldPos, prevWave.pos);
					for (uint j = 1; j < wavesCount; j++)
					{
						curRadius -= frameRadius;
						Wave nextWave = wavesBuffer[oscRow + (curWaveIndex + j) % wavesCount];
						float nextDist = curRadius - distance(worldPos, nextWave.pos);
						if (nextDist*prevDist <= 0)
						{
							float k = abs(nextDist) / (abs(nextDist) + abs(prevDist));
							result += sin(lerp(nextWave.phase, prevWave.phase, k) * M_2PI);
						}
						prevWave = nextWave;
						prevDist = nextDist;					}
				}
				result = result / (oscillatorsCount + 1);
				float positive = sqrt((abs(result) + result) / 2.0f);
				float negative = sqrt((abs(result) - result) / 2.0f);
				float node = (0.1 - abs(result)) * 10;
				return float4(positive, node, negative, 1);

				//return float4(abs(result), pow(1-abs(result), 6), sqrt(abs(result)), 1);
				//return float4((float3)sqrt(abs(result)), 1);
            }
            ENDCG
        }
	} 
}
