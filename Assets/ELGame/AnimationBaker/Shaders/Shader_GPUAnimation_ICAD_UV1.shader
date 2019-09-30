Shader "EL_Shader/Shader_GPUAnimation_ICAD_UV1"
{
	Properties
	{
		_MainTex("MainTexture", 2D) = "white" {}			//主纹理
		_SecondaryTex("SecTexture", 2D) = "black" {}		//次纹理

		_IdleTex("IdleTex", 2D) = "white" {}		//静止动画
		_IdleLen("IdleLen", float) = 0

		_ChargeTex("ChargeTex", 2D) = "white" {}	//冲锋动画
		_ChargeLen("ChargeLen", float) = 0

		_AttackTex("AttackTex", 2D) = "white" {}	//攻击动画
		_AttackLen("AttackLen", float) = 0

		_DeathTex("DeathTex", 2D) = "white" {}		//死亡动画
		_DeathLen("DeathLen", float) = 0
	}
		SubShader
		{
			Tags 
			{ 
				"RenderType" = "Opaque" 
			}

			LOD 100
			Cull Back

		Pass
		{
			CGINCLUDE
			#include "UnityCG.cginc"


			struct appdata
			{
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				//UNITY_VERTEX_INPUT_INSTANCE_ID	//片段着色器暂时不需要实例化数据
			};

			//纹理
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _SecondaryTex;
			float4 _SecondaryTex_ST;

			//多属性
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _MoveSpeed)		//移动速度
				UNITY_DEFINE_INSTANCED_PROP(float, _AttackTimer)	//攻击计时
				UNITY_DEFINE_INSTANCED_PROP(float, _IsAttack)		//是否攻击中
				UNITY_DEFINE_INSTANCED_PROP(float, _DeathTimer)		//死亡计时
				UNITY_DEFINE_INSTANCED_PROP(float, _IsDeath)		//是否死亡中
			UNITY_INSTANCING_BUFFER_END(Props)
			
			//静止
			sampler2D _IdleTex;
			float4 _IdleTex_TexelSize;
			float _IdleLen;

			//冲锋
			sampler2D _ChargeTex;
			float4 _ChargeTex_TexelSize;
			float _ChargeLen;

			//攻击
			sampler2D _AttackTex;
			float4 _AttackTex_TexelSize;
			float _AttackLen;

			//死亡
			sampler2D _DeathTex;
			float4 _DeathTex_TexelSize;
			float _DeathLen;

			float3 SampleFromLoopTex(uint vertexID, float animLength, sampler2D tex, fixed textureWidth)
			{
				//计算帧数
				float u = (vertexID + 0.5) * textureWidth;
				float v = _Time.y / animLength;
				
				return tex2Dlod(tex, float4(u, v, 0, 0)).xyz;
			}

			float3 SamplerFromClampTexBySettingTimer(uint vertexID, float animLength, sampler2D tex, fixed textureWidth, float timer)
			{
				//计算帧数
				float u = (vertexID + 0.5) * textureWidth;
				float v = timer / animLength;

				return tex2Dlod(tex, float4(u, v, 0, 0)).xyz;
			}

			float3 GetLocomotionState(float moveSpeed, uint vertexID)
			{
				float3 finalPose;

				//从静止中采样
				float3 idlePose = SampleFromLoopTex(vertexID, _IdleLen, _IdleTex, _IdleTex_TexelSize.x);

				//从冲锋状态采样
				float3 chargePose = SampleFromLoopTex(vertexID, _ChargeLen, _ChargeTex, _ChargeTex_TexelSize.x);

				//混合
				finalPose = lerp(idlePose, chargePose, moveSpeed);
				
				return finalPose;
			}

			v2f vert(appdata v, uint vertexID : SV_VertexID)
			{
				v2f o;

				//使用实例化必须在顶点着色器添加
				//确保实例化ID可以传入着色器
				//如果片段着色器不需要就不用
				UNITY_SETUP_INSTANCE_ID(v);

				//因为片段着色器无需实例化信息，因此无需使用
				//UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 finalPose;

				float moveSpeed = UNITY_ACCESS_INSTANCED_PROP(Props, _MoveSpeed);		//行走速度
				float attackTimer = UNITY_ACCESS_INSTANCED_PROP(Props, _AttackTimer);	//攻击计时器
				float isAttack = UNITY_ACCESS_INSTANCED_PROP(Props, _IsAttack);			//是否攻击中
				float deathTimer = UNITY_ACCESS_INSTANCED_PROP(Props, _DeathTimer);		//死亡计时器
				float isDeath = UNITY_ACCESS_INSTANCED_PROP(Props, _IsDeath);			//是否死亡

				//行动的动画
				float3 locomotionPose = GetLocomotionState(moveSpeed, vertexID);

				//从攻击中采样
				float3 attackPose = SamplerFromClampTexBySettingTimer(vertexID, _AttackLen, _AttackTex, _AttackTex_TexelSize.x, attackTimer);

				finalPose.xyz = lerp(locomotionPose, attackPose, isAttack);

				//从死亡动画采样				
				float3 deathPose = SamplerFromClampTexBySettingTimer(vertexID, _DeathLen, _DeathTex, _DeathTex_TexelSize.x, deathTimer);

				finalPose.xyz = lerp(finalPose.xyz, deathPose, isDeath);

				//uv转换
				o.uv.xy = v.uv;
				o.uv.z  = v.uv1.x;
				o.vertex = UnityObjectToClipPos(finalPose);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				//因为片段着色器无需实例化信息，因此无需使用
				//UNITY_SETUP_INSTANCE_ID(i);
				//从主图中采样
				fixed4 mainColor = tex2D(_MainTex, i.uv.xy);
				//从第二张贴图中采样
				fixed4 secColor = tex2D(_SecondaryTex, i.uv.xy);
				//混合一下
				return lerp(mainColor, secColor, i.uv.z);
			}

			ENDCG

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//开启gpu instancing
			#pragma multi_compile_instancing

			ENDCG
		}
	}
}
