//材质名称:Material中Shader的名称
Shader "Sprites"
{
	//材质属性,就是面板的属性
	Properties
	{
        //这里先说明一下,下面代码的几个部分的作用和来源
        //[]中的特性代表面板的一些功能[PerRendererData]代表由材质组件直接提供属性,不可修改. [MaterialToggle]代表在面板上显示为开关
        //蓝色的是变量名,在后面的Pass中会声明
        //棕色字符串是面板属性的名称
        //绿色的是属性的参数类型,2D就是2D图片参数,Color就是调色板,Float是浮点数字,Range是滑动条
        //等号后面的是默认值
        //[材质组件提供属性,不可修改] 精灵贴图纹理参数，默认白色
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		// 颜色混合参数，默认白色（不改变原色）
		_Color ("Tint", Color) = (1,1,1,1)
		// [选项为开关] 默认值为0代表关闭,1代表开启.是像素对齐开关
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		// 自发光强度参数，默认值为 1.0,就是代码中鼠标滑过发亮的那个就是调节这个参数
		_SelfIllum ("Self Illumination",Float) = 1.0
	}
	// 子着色器块（一个 Shader 可包含多个 SubShader，对应不同硬件(不同的GPU,A卡N卡手机等等)）
    SubShader  // 子着色器块（一个 Shader 可包含多个 SubShader，适配不同硬件）
    {
        Tags  // 渲染标签，控制渲染流程
        {
            "Queue" = "Transparent"         // 渲染队列为透明物体（在非透明物体之后渲染）
            "IgnoreProjector" = "True"      // 忽略投影器（避免投影影响透明物体）
            "RenderType" = "Transparent"    // 渲染类型为透明（用于替代渲染管线或后处理）
            "PreviewType" = "Plane"         // 在材质预览窗口中显示为平面
            "CanUseSpriteAtlas" = "True"    // 支持精灵图集（允许 UV 坐标自动映射图集）
        }

        Cull Off       // 关闭背面剔除（正反两面均渲染，适用于 2D 精灵）
        Lighting Off   // 关闭光照计算（2D 精灵通常无需光照）
        ZWrite Off     // 关闭深度写入（允许透明物体正确混合）
        Blend One OneMinusSrcAlpha  // 混合模式：SrcAlpha * One + DstAlpha * (1 - SrcAlpha)
                                    // 适用于预乘 Alpha 的纹理，避免边缘黑边


        Pass  // 渲染通道（指一整套渲染流程,可以用不同的渲染流程多次渲染一个模型）
        {
            //使用哪种Shader语法,这里用的是CG语法(可以使用不同的Shader语法,比如HLSL. Unity会自己适配,不同语法对应的是OpenGL,DX这些玩意)
            CGPROGRAM
            /////////////////这里#pragma就是Pass会执行的功能
            // 顶点着色器会执行vert方法,类似委托
            #pragma vertex vert
            // 片段着色器会执行frag方法
            #pragma fragment frag
            #pragma target 2.0      // 目标着色器模型为 2.0（支持基础功能）
            #pragma multi_compile _ PIXELSNAP_ON  // 编译两个变体：启用/禁用像素对齐
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA  // 编译变体支持 ETC1 分离 Alpha
            #include "UnityCG.cginc"  // 引入 Unity 内置 CG 函数库

            // 该结构用于顶点着色器输入(就是用结构打包一堆字段给下面的方法用的,注意里面的字段都是一样的名字)
            struct appdata_t
            {
                float4 vertex   : POSITION;    // 顶点位置（模型空间）
                float4 color    : COLOR;       // 顶点颜色（通常用于颜色混合）
                float2 texcoord : TEXCOORD0;   // 第一组纹理坐标
                UNITY_VERTEX_INPUT_INSTANCE_ID  // 实例化渲染 ID（用于 GPU 实例化）
            };

            // 该结构用于顶点着色器输出（传递给片段着色器的数据）
            struct v2f
            {
                float4 vertex   : SV_POSITION; // 裁剪空间顶点位置
                fixed4 color : COLOR;          // 混合后的顶点颜色
                float2 texcoord  : TEXCOORD0;   // 纹理坐标
                UNITY_VERTEX_OUTPUT_STEREO     // 立体渲染支持（如 VR 分屏）
            };

            // 材质属性变量声明(这就是上面那几个面板属性,面板输入的值就到这来了,下面代码都会用上)
            fixed4 _Color;        // 颜色混合参数（来自 Properties）
            float _SelfIllum;     // 自发光强度（来自 Properties）

            // 顶点着色器函数(刚才那个结构在这里输入了)
            v2f vert(appdata_t IN)
            {
                //这是上面的那个结构,用于输出
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);    // 初始化实例化 ID
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); // 初始化立体渲染数据
                OUT.vertex = UnityObjectToClipPos(IN.vertex); // 顶点坐标转换到裁剪空间
                OUT.texcoord = IN.texcoord;     // 传递纹理坐标
                OUT.color = IN.color * _Color;  // 混合顶点颜色与材质颜色

                // 如果启用像素对齐，将顶点坐标对齐到像素网格
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex); // Unity 内置像素对齐函数
                #endif

                return OUT;
            }

            // 纹理采样相关变量声明
            sampler2D _MainTex;    // 主纹理（精灵贴图）
            sampler2D _AlphaTex;   // 外部 Alpha 纹理（ETC1 格式专用）

            // 自定义纹理采样函数
            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv); // 采样主纹理颜色

                // 如果启用 ETC1_EXTERNAL_ALPHA，从外部纹理获取 Alpha 值
                #if ETC1_EXTERNAL_ALPHA
                color.a = tex2D(_AlphaTex, uv).r;   // 外部 Alpha 通道（单通道，存储为 R 值）
                #endif

                return color;
            }

            // 片段着色器函数(上面输出的结构,到这里来了)
            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样纹理并混合顶点颜色
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                c.rgb *= c.a;          // 预乘 Alpha（避免透明边缘黑边）
                c.rgb *= _SelfIllum;   // 应用自发光强度（增强颜色亮度）
                return c;              // 返回最终像素颜色
            }
            ENDCG  // 结束 CG 代码块
        }
    }
}
