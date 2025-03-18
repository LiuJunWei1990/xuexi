//��������:Material��Shader������
Shader "Sprites"
{
	//��������,������������
	Properties
	{
        //������˵��һ��,�������ļ������ֵ����ú���Դ
        //[]�е����Դ�������һЩ����[PerRendererData]�����ɲ������ֱ���ṩ����,�����޸�. [MaterialToggle]�������������ʾΪ����
        //��ɫ���Ǳ�����,�ں����Pass�л�����
        //��ɫ�ַ�����������Ե�����
        //��ɫ�������ԵĲ�������,2D����2DͼƬ����,Color���ǵ�ɫ��,Float�Ǹ�������,Range�ǻ�����
        //�Ⱥź������Ĭ��ֵ
        //[��������ṩ����,�����޸�] ������ͼ���������Ĭ�ϰ�ɫ
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		// ��ɫ��ϲ�����Ĭ�ϰ�ɫ�����ı�ԭɫ��
		_Color ("Tint", Color) = (1,1,1,1)
		// [ѡ��Ϊ����] Ĭ��ֵΪ0����ر�,1������.�����ض��뿪��
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		// �Է���ǿ�Ȳ�����Ĭ��ֵΪ 1.0,���Ǵ�������껬���������Ǹ����ǵ����������
		_SelfIllum ("Self Illumination",Float) = 1.0
	}
	// ����ɫ���飨һ�� Shader �ɰ������ SubShader����Ӧ��ͬӲ��(��ͬ��GPU,A��N���ֻ��ȵ�)��
    SubShader  // ����ɫ���飨һ�� Shader �ɰ������ SubShader�����䲻ͬӲ����
    {
        Tags  // ��Ⱦ��ǩ��������Ⱦ����
        {
            "Queue" = "Transparent"         // ��Ⱦ����Ϊ͸�����壨�ڷ�͸������֮����Ⱦ��
            "IgnoreProjector" = "True"      // ����ͶӰ��������ͶӰӰ��͸�����壩
            "RenderType" = "Transparent"    // ��Ⱦ����Ϊ͸�������������Ⱦ���߻����
            "PreviewType" = "Plane"         // �ڲ���Ԥ����������ʾΪƽ��
            "CanUseSpriteAtlas" = "True"    // ֧�־���ͼ�������� UV �����Զ�ӳ��ͼ����
        }

        Cull Off       // �رձ����޳��������������Ⱦ�������� 2D ���飩
        Lighting Off   // �رչ��ռ��㣨2D ����ͨ��������գ�
        ZWrite Off     // �ر����д�루����͸��������ȷ��ϣ�
        Blend One OneMinusSrcAlpha  // ���ģʽ��SrcAlpha * One + DstAlpha * (1 - SrcAlpha)
                                    // ������Ԥ�� Alpha �����������Ե�ڱ�


        Pass  // ��Ⱦͨ����ָһ������Ⱦ����,�����ò�ͬ����Ⱦ���̶����Ⱦһ��ģ�ͣ�
        {
            //ʹ������Shader�﷨,�����õ���CG�﷨(����ʹ�ò�ͬ��Shader�﷨,����HLSL. Unity���Լ�����,��ͬ�﷨��Ӧ����OpenGL,DX��Щ����)
            CGPROGRAM
            /////////////////����#pragma����Pass��ִ�еĹ���
            // ������ɫ����ִ��vert����,����ί��
            #pragma vertex vert
            // Ƭ����ɫ����ִ��frag����
            #pragma fragment frag
            #pragma target 2.0      // Ŀ����ɫ��ģ��Ϊ 2.0��֧�ֻ������ܣ�
            #pragma multi_compile _ PIXELSNAP_ON  // �����������壺����/�������ض���
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA  // �������֧�� ETC1 ���� Alpha
            #include "UnityCG.cginc"  // ���� Unity ���� CG ������

            // �ýṹ���ڶ�����ɫ������(�����ýṹ���һ���ֶθ�����ķ����õ�,ע��������ֶζ���һ��������)
            struct appdata_t
            {
                float4 vertex   : POSITION;    // ����λ�ã�ģ�Ϳռ䣩
                float4 color    : COLOR;       // ������ɫ��ͨ��������ɫ��ϣ�
                float2 texcoord : TEXCOORD0;   // ��һ����������
                UNITY_VERTEX_INPUT_INSTANCE_ID  // ʵ������Ⱦ ID������ GPU ʵ������
            };

            // �ýṹ���ڶ�����ɫ����������ݸ�Ƭ����ɫ�������ݣ�
            struct v2f
            {
                float4 vertex   : SV_POSITION; // �ü��ռ䶥��λ��
                fixed4 color : COLOR;          // ��Ϻ�Ķ�����ɫ
                float2 texcoord  : TEXCOORD0;   // ��������
                UNITY_VERTEX_OUTPUT_STEREO     // ������Ⱦ֧�֣��� VR ������
            };

            // �������Ա�������(����������Ǽ����������,��������ֵ�͵�������,������붼������)
            fixed4 _Color;        // ��ɫ��ϲ��������� Properties��
            float _SelfIllum;     // �Է���ǿ�ȣ����� Properties��

            // ������ɫ������(�ղ��Ǹ��ṹ������������)
            v2f vert(appdata_t IN)
            {
                //����������Ǹ��ṹ,�������
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);    // ��ʼ��ʵ���� ID
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); // ��ʼ��������Ⱦ����
                OUT.vertex = UnityObjectToClipPos(IN.vertex); // ��������ת�����ü��ռ�
                OUT.texcoord = IN.texcoord;     // ������������
                OUT.color = IN.color * _Color;  // ��϶�����ɫ�������ɫ

                // ����������ض��룬������������뵽��������
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex); // Unity �������ض��뺯��
                #endif

                return OUT;
            }

            // ���������ر�������
            sampler2D _MainTex;    // ������������ͼ��
            sampler2D _AlphaTex;   // �ⲿ Alpha ����ETC1 ��ʽר�ã�

            // �Զ��������������
            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv); // ������������ɫ

                // ������� ETC1_EXTERNAL_ALPHA�����ⲿ�����ȡ Alpha ֵ
                #if ETC1_EXTERNAL_ALPHA
                color.a = tex2D(_AlphaTex, uv).r;   // �ⲿ Alpha ͨ������ͨ�����洢Ϊ R ֵ��
                #endif

                return color;
            }

            // Ƭ����ɫ������(��������Ľṹ,����������)
            fixed4 frag(v2f IN) : SV_Target
            {
                // ����������϶�����ɫ
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                c.rgb *= c.a;          // Ԥ�� Alpha������͸����Ե�ڱߣ�
                c.rgb *= _SelfIllum;   // Ӧ���Է���ǿ�ȣ���ǿ��ɫ���ȣ�
                return c;              // ��������������ɫ
            }
            ENDCG  // ���� CG �����
        }
    }
}
