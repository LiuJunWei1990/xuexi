using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// DCC文件导入器, DCC就是Unity动画的原图文件和它的精灵
/// </summary>
/// <remarks>
/// <para>名词: [姿态]: 行走,攻击等; [方向/朝向]: 向右攻击,向左行走等; [帧]: 向右攻击的第3帧等; </para>
/// <para>1. { DCC为动画切片文件, 每个文件仅包含某个对象"一个姿态"的所有原图片(Texture2D),以及每个图片的切片信息(Sprite) } </para>
/// <para>2. { 本类导入DCC文件,将其转换为textures,sprites两个列表 }  </para>
/// <para>3. { 图片大小时4068*4068,一般一个动作方向占一张图片,如果动作方向图片大于这个大小,就会拆分成多张图片 }  </para>
/// <para>4. [播放参数] 就是播放动画时会需要调用的参数 </para>
/// <para>5. [DCC文件数据] 直接从DCC文件中读取的数据  </para>
/// </remarks>
public class DCC
{
    /// <summary>
    /// [DCC结果]图片列表
    /// </summary>
    /// <remarks>
    /// [供使用的DCC文件数据]储存所有的图片,一般每个朝向一个图片,一个图片就是一个动画,如果动作方向图片大于4068*4068,就会拆分成多张图片
    /// </remarks>
    public List<Texture2D> textures;
    /// <summary>
    /// [DCC结果]精灵列表 -- 播放参数1
    /// </summary>
    /// <remarks>
    /// [供使用的DCC文件数据]储存所有的精灵,精灵在列表中的顺序是和图片列表中图片顺序对应的
    /// </remarks>
    public List<Sprite> sprites;
    /// <summary>
    /// [DCC结果]姿态的朝向数量
    /// </summary>
    /// <remarks>
    /// 用来分割图片和精灵,总数除以朝向,就是每个朝向的图片/精灵数量
    /// </remarks>
    public int directionCount;
    /// <summary>
    /// [DCC结果]每个朝向有几帧 -- 播放参数2
    /// </summary>
    /// <remarks>
    /// 就是上面那个朝向数量计算的结果,这个是分割和播放动画的依据
    /// </remarks>
    public int framesPerDirection;

    /// <summary>
    /// 缓冲区的最大像素值数组count,避免内存溢出
    /// </summary>
    const int DCC_MAX_PB_ENTRY = 85000;

    /// <summary>
    /// DCC单元格信息
    /// </summary>
    /// <remarks>
    /// DCC的单元格,指的是动画的像素区域; 
    /// 每个单元格单位为4像素,一个单元格是4*4像素方格
    /// 这个结构体,只包含锚点和宽高信息,不包含像素信息
    /// </remarks>
    struct Cell
    {
        /// <summary>
        /// 单元格在其所在的矩阵中的锚点(左下)坐标位置
        /// </summary>
        public int x0, y0;
        /// <summary>
        /// 单元格宽度和高度
        /// </summary>
        public int w, h;
    }

    /// <summary>
    /// DCC文件头信息
    /// </summary>
    /// <remarks>
    /// 没有什么特别的,就是单独搞个类存头信息
    /// </remarks>
    class Header
    {
        /// <summary>
        /// [DCC文件数据]文件签名(貌似没用到过)
        /// </summary>
        public byte fileSignature;
        /// <summary>
        /// [DCC文件数据]DCC文件版本号(貌似没用到过)
        /// </summary>
        public byte version;
        /// <summary>
        /// [DCC文件数据]DCC文件中读取的姿态朝向总数
        /// </summary>
        public byte directionCount;
        /// <summary>
        /// [DCC文件数据]DCC文件中读取的每方向的帧数
        /// </summary>
        public int framesPerDir;
        /// <summary>
        /// [DCC文件数据]DCC文件的标签(貌似没用到过)
        /// </summary>
        public int tag;
        /// <summary>
        /// [DCC文件数据]DCC文件中读取的最终 Dc6 尺寸(DC6未实装,这个值没用到过)
        /// </summary>
        public int finalDc6Size;
        /// <summary>
        /// [DCC文件数据]DCC文件中读取的每个朝向信息的起始字节
        /// </summary>
        /// <remarks>
        /// [DCC文件数据]每个成员存一个朝向
        /// </remarks>
        public int[] dirOffset;
    }

    /// <summary>
    /// DCC文件中的朝向类
    /// </summary>
    class Direction
    {
        /// <summary>
        /// [DCC文件数据]编码数据大小(貌似没用过)
        /// </summary>
        public int outsizeCoded;
        /// <summary>
        /// [DCC文件数据]压缩标志位（0x01和0x02表示不同压缩方式）
        /// </summary>
        /// <remarks>
        /// 决定像素的解压方式,是否读取其中的三条流信息
        /// </remarks>
        public int compressionFlag;

        #region 帧的各项属性的位宽
        /// <summary>
        /// [DCC文件数据]可变参数0的位宽(未实装,只在代码中检测了一下,并未处理读取的文件数据)
        /// </summary>
        public int variable0Bits;
        /// <summary>
        /// [DCC文件数据]帧的宽度的位宽
        /// </summary>
        public int widthBits;
        /// <summary>
        /// [DCC文件数据]帧的高度的位宽
        /// </summary>
        public int heightBits;
        /// <summary>
        /// [DCC文件数据]帧的x起点偏移量的位宽
        /// </summary>
        public int xoffsetBits;
        /// <summary>
        /// [DCC文件数据]帧的y起点偏移量的位宽
        /// </summary>
        public int yoffsetBits;
        /// <summary>
        /// [DCC文件数据]帧的可选字节的位宽
        /// </summary>
        public int optionalBytesBits;
        /// <summary>
        /// [DCC文件数据]帧的编码字节的位宽
        /// </summary>
        public int codedBytesBits;
        #endregion

        /// <summary>
        /// 朝向的缓冲区矩阵
        /// </summary>
        /// <remarks>
        /// 每个帧的缓冲区矩型层叠在一起,包围这个区域的矩型就是整个朝向的缓冲区矩阵(默认初始化为0矩型)
        /// </remarks>
        public IntRect box = IntRect.zero;
        /// <summary>
        /// 帧数组
        /// </summary>
        public Frame[] frames;
        /// <summary>
        /// 像素值数组
        /// </summary>
        /// <remarks>
        /// 记录调色板256个色中,会被用到的色的序号
        /// </remarks>
        public byte[] pixel_values = new byte[256];
        /// <summary>
        /// 朝向单元格数组的像素块映射数组
        /// </summary>
        /// <remarks>
        /// 按顺序储存,没一帧的cell中的像素块,如果本cell相比上一帧没有变化,便不会收录
        /// </remarks>
        public PixelBufferEntry[] pixelBuffer;
        /// <summary>
        /// pixelBuffer的长度
        /// </summary>
        public int pb_nb_entry;
    }

    /// <summary>
    /// 朝向的缓冲区
    /// </summary>
    /// <remarks>
    /// <para>DCC文件压缩图像: </para>
    /// <para>将一个方向的所有帧层叠在一起,然后按4*4像素分割成cell,保存4个脚的像素点信息</para>
    /// <para>DCC文件图像解压: </para>
    /// <para>通过帧的信息,从cell的四个点中提取本帧的色彩; </para>
    /// <para>通过本帧的四个像素点,计算出cell的图形,进而组成图片 </para>
    /// <para>最后计算出精灵坐标</para>
    /// <para>[注]朝向缓冲区只包含宽高和cell数组,没有起始点,cell会有自己的起始点</para>
    /// </remarks>
    struct FrameBuffer
    {
        /// <summary>
        /// 单元格数组
        /// </summary>
        /// <remarks>
        /// 缓冲区的主体,这些4*4像素的单元格数组组成了整个缓冲区
        /// </remarks>
        public Cell[] cells;
        /// <summary>
        /// 缓冲区的宽度(单元格单位)
        /// </summary>
        public int nb_cell_w;
        /// <summary>
        /// 缓冲区的高度(单元格单位)
        /// </summary>
        public int nb_cell_h;
    }

    /// <summary>
    /// DCC中动画的帧信息
    /// </summary>
    class Frame
    {
        /// <summary>
        /// [DCC文件数据]帧的可变参数0
        /// </summary>
        public int variable0;
        /// <summary>
        /// [DCC文件数据]宽度(像素单位)
        /// </summary>
        public int width;
        /// <summary>
        /// [DCC文件数据]高度(像素单位)
        /// </summary>
        public int height;
        /// <summary>
        /// [DCC文件数据]x坐标起点的偏移量(像素单位)
        /// </summary>
        public int xoffset;
        /// <summary>
        /// [DCC文件数据]y坐标起点的偏移量(像素单位)
        /// </summary>
        public int yoffset;
        /// <summary>
        /// [DCC文件数据]可选字节
        /// </summary>
        public int optionalBytes;
        /// <summary>
        /// [DCC文件数据]编码字节
        /// </summary>
        public int codedBytes;
        /// <summary>
        /// [DCC文件数据]帧是否从底部向上渲染(未实装,代码跳过了这个值)
        /// </summary>
        public int bottomUp;
        /// <summary>
        /// 帧像素矩阵(像素单位)
        /// </summary>
        public IntRect box;

        /// <summary>
        /// 帧的单元格数组
        /// </summary>
        public Cell[] cells;
        /// <summary>
        /// 宽(单元格单位)
        /// </summary>
        public int nb_cell_w = 0;
        /// <summary>
        /// 高(单元格单位)
        /// </summary>
        public int nb_cell_h = 0;

        /// <summary>
        /// 帧的纹理
        /// </summary>
        public Texture2D texture;
        /// <summary>
        /// 帧的像素数组(映射像素矩阵)
        /// </summary>
        public Color32[] texturePixels;
        /// <summary>
        /// 帧的图片的x起始坐标(像素单位)
        /// </summary>
        public int textureX;
        /// <summary>
        /// 帧的图片的y起始坐标(像素单位)
        /// </summary>
        public int textureY;
    }
    /// <summary>
    /// 像素缓冲区条目
    /// </summary>
    /// <remarks>
    /// <para>由于DCC文件的图片是层叠压缩的(记录比上一帧变动的cell,没变动或者透明会在像素掩码和像素值里表现出来</para>
    /// <para>这个条目映射的是: [每一层cell] 和 [朝向层叠的cell] 的 像素块</para>
    /// <para>本条目通过映射朝向和每帧的cell对比,来解压像素</para>
    /// <para>像素块: 每个单元格最多4种颜色,代表会用到的4种颜色</para>
    /// <para>[命名] 为了方便区分,按映射对象称呼: 帧[y]层的cell[x]或者 朝向层的cell[x]</para>
    /// </remarks>
    struct PixelBufferEntry
    {
        /// <summary>
        /// 像素数组(像素块,单元格内会用到的颜色,最多4种)
        /// </summary>
        public byte[] val;
        /// <summary>
        /// 当前帧在朝向的帧数组中的下标
        /// </summary>
        public int frame;
        /// <summary>
        /// 当前单元格在朝向的单元格数组中的下标
        /// </summary>
        public int frameCellIndex;
    }

    /// <summary>
    /// [自定义流合集]一个包含5个流的类; 用来解压动画图片的像素信息
    /// </summary>
    /// <remarks>
    /// 根据不同的解压方式,选择赋值其中的某几个流,不一定全部都会赋值
    /// </remarks>
    class Streams
    {
        /// <summary>
        /// 对比单元格流,bool
        /// </summary>
        public BitReader equalCell;
        /// <summary>
        /// 像素掩码流,决定有几个像素需要解码
        /// </summary>
        public BitReader pixelMask;
        /// <summary>
        /// 编码类型流,1和0,决定使用哪种解码方式
        /// </summary>
        public BitReader encodingType;
        public BitReader rawPixel;
        public BitReader pixelCode;
    }

    /// <summary>
    /// 读取DCC文件头信息
    /// </summary>
    /// <param name="reader">二进制流</param>
    /// <param name="header">返回的头信息结果</param>
    static void ReadHeader(BinaryReader reader, Header header)
    {
        header.fileSignature = reader.ReadByte();
        header.version = reader.ReadByte();
        header.directionCount = reader.ReadByte();
        header.framesPerDir = reader.ReadInt32();
        header.tag = reader.ReadInt32();
        header.finalDc6Size = reader.ReadInt32();
        // 赋值方向的偏移量数组,数组长度为方向总数
        header.dirOffset = new int[header.directionCount];
        //遍历偏移量数组,给数组成员赋值
        for (int dir = 0; dir < header.directionCount; ++dir)
        {
            header.dirOffset[dir] = reader.ReadInt32();
        }
    }

    /// <summary>
    /// //读取朝向信息,主要标志位和帧的数据的位宽信息
    /// </summary>
    /// <param name="bitReader">比特流</param>
    /// <param name="dir">方向结果</param>
    static void ReadDirection(BitReader bitReader, Direction dir)
    {
        dir.outsizeCoded = bitReader.ReadBits(32);
        dir.compressionFlag = bitReader.ReadBits(2);
        dir.variable0Bits = bitReader.ReadBits(4);
        dir.widthBits = bitReader.ReadBits(4);
        dir.heightBits = bitReader.ReadBits(4);
        dir.xoffsetBits = bitReader.ReadBits(4);
        dir.yoffsetBits = bitReader.ReadBits(4);
        dir.optionalBytesBits = bitReader.ReadBits(4);
        dir.codedBytesBits = bitReader.ReadBits(4);
    }

    /// <summary>
    /// 读取帧信息
    /// </summary>
    /// <param name="bitReader">流</param>
    /// <param name="dir">方向类</param>
    /// <param name="frame">储存返回结果的帧帧</param>
    /// <remarks>
    /// 根据已经读了数据的dir中的帧的各属性的位宽信息,读取帧的各项属性信息
    /// </remarks>
    static void ReadFrame(BitReader bitReader, Direction dir, Frame frame)
    {
        frame.variable0 = bitReader.ReadBits(widthTable[dir.variable0Bits]);
        frame.width = bitReader.ReadBits(widthTable[dir.widthBits]);
        frame.height = bitReader.ReadBits(widthTable[dir.heightBits]);
        frame.xoffset = bitReader.ReadSigned(widthTable[dir.xoffsetBits]);
        frame.yoffset = bitReader.ReadSigned(widthTable[dir.yoffsetBits]);
        frame.optionalBytes = bitReader.ReadBits(widthTable[dir.optionalBytesBits]);
        frame.codedBytes = bitReader.ReadBits(widthTable[dir.codedBytesBits]);
        frame.bottomUp = bitReader.ReadBits(1);
        //根据上面读的信息创建帧的图形矩阵(包含起始点和宽高)
        frame.box = new IntRect(frame.xoffset, frame.yoffset, frame.width, frame.height);
    }

    /// <summary>
    /// 读取Streams类流信息
    /// </summary>
    /// <param name="bitReader"></param>
    /// <param name="dir"></param>
    /// <param name="dcc"></param>
    /// <param name="streams"></param>
    /// <remarks>
    /// <para>Streams类包含5个不同的数据流,根据DCC.Direction类的压缩标志位信息,其中的几个流会被赋值 </para>
    /// <para>Streams类的成员就是比特流,直接读取就行 </para>
    /// </remarks>
    static void ReadStreamsInfo(BitReader bitReader, Direction dir, byte[] dcc, Streams streams)
    {
        int equalCellSize = 0;
        int pixelMaskSize = 0;
        int encodingTypeSize = 0;
        int rawPixelSize = 0;

        // 检查压缩标志位的二进制10位是否为1
        if ((dir.compressionFlag & 0x02) != 0)
        {
            equalCellSize = bitReader.ReadBits(20);
        }

        pixelMaskSize = bitReader.ReadBits(20);

        //// 检查压缩标志位的二进制01位是否为1
        if ((dir.compressionFlag & 0x01) != 0)
        {
            encodingTypeSize = bitReader.ReadBits(20);
            rawPixelSize = bitReader.ReadBits(20);
        }

        // 导入需要用到的像素值数组,256个色会被用到的就加入数组中,数组成员的值代表第几个色
        for (int i = 0, idx = 0; i < 256; ++i)
        {
            if (bitReader.ReadBit() != 0)
            {
                dir.pixel_values[idx] = (byte)i;
                ++idx;
            }
        }

        // 获取当前比特索引在文件中的位置
        long offset = bitReader.offset;
        // 开始为Streams类流赋值
        if (equalCellSize != 0)
            streams.equalCell = new BitReader(dcc, offset);
        offset += equalCellSize;
        if (pixelMaskSize != 0)
            streams.pixelMask = new BitReader(dcc, offset);
        offset += pixelMaskSize;
        if (encodingTypeSize != 0)
            streams.encodingType = new BitReader(dcc, offset);
        offset += encodingTypeSize;
        if (rawPixelSize != 0)
            streams.rawPixel = new BitReader(dcc, offset);
        offset += rawPixelSize;
        streams.pixelCode = new BitReader(dcc, offset);
    }
    /// <summary>
    /// <para>创建朝向的缓冲区(所有帧的缓冲区)</para>
    /// <para>并为朝向创建单元格数组</para>
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>1. 确定,朝向cells数组的成员, </para>
    /// <para>2. 赋值,朝向cell成员赋值宽高, </para>
    /// <para>3. 未确定,朝向cell成员起始坐标</para>
    /// <para>4. 朝向的cell宽高值和cell成员数量都是由形参dir.box的宽高计算出来的</para>
    /// </remarks>
    static FrameBuffer CreateFrameBuffer(Direction dir)
    {
        //声明一个临时缓冲区
        FrameBuffer frameBuffer = new FrameBuffer();

        //计算临时缓冲区的宽高(单元格单位)
        frameBuffer.nb_cell_w = 1 + ((dir.box.width - 1) / 4); //+1是余数,-1可能是边缘1像素
        frameBuffer.nb_cell_h = 1 + ((dir.box.height - 1) / 4);
        //根据计算的宽高,创建单元格数组
        frameBuffer.cells = new Cell[frameBuffer.nb_cell_w * frameBuffer.nb_cell_h];

        //声明临时int数组,映射cells成员的宽和高
        int[] cell_w = new int[frameBuffer.nb_cell_w];
        int[] cell_h = new int[frameBuffer.nb_cell_h];

        //计算映射数组的单元格的宽高
        if (frameBuffer.nb_cell_w == 1)//1个单元格,直接赋值
            cell_w[0] = dir.box.width;
        //否则,给宽数组都赋值4,并把最后成员的值改成余数
        else
        {
            for (int i = 0; i < (frameBuffer.nb_cell_w - 1); i++)
                cell_w[i] = 4;
            //计算出末尾,不被4整除的部分,赋值给最后一个值
            cell_w[frameBuffer.nb_cell_w - 1] = dir.box.width - (4 * (frameBuffer.nb_cell_w - 1));
        }
        //赋值高和宽的算法一样
        if (frameBuffer.nb_cell_h == 1)
            cell_h[0] = dir.box.height;
        else
        {
            for (int i = 0; i < (frameBuffer.nb_cell_h - 1); i++)
                cell_h[i] = 4;
            cell_h[frameBuffer.nb_cell_h - 1] = dir.box.height - (4 * (frameBuffer.nb_cell_h - 1));
        }


        //把映射数组的宽高赋值给临时缓冲区的cell成员
        int y0 = 0;
        for (int y = 0; y < frameBuffer.nb_cell_h; y++)
        {
            int x0 = 0;
            for (int x = 0; x < frameBuffer.nb_cell_w; x++)
            {
                int index = x + (y * frameBuffer.nb_cell_w);
                frameBuffer.cells[index].w = cell_w[x];
                frameBuffer.cells[index].h = cell_h[y];
                x0 += 4;
            }
            y0 += 4;
        }

        //返回的临时缓冲区,有了缓冲区宽高和cell数组值,cell成员有宽高值,但是没有起始点值
        return frameBuffer;
    }

    /// <summary>
    /// 为 [形参2帧] 创建单元格数组,坐标对应 [形参1朝向的box]
    /// </summary>
    /// <param name="box">朝向的box</param>
    /// <param name="frame">当前帧</param>
    /// <remarks>
    /// <para>1. 确定,帧的cells数组的成员, </para>
    /// <para>2. 赋值,帧的cell成员赋值宽高, </para>
    /// <para>3. 赋值,帧的cell成员起始坐标</para>
    /// <para>4. 宽高值和cell成员数量都是由形参box的宽高计算出来的</para>
    /// <para> [注] 每个帧的cell和朝向的cell是对齐的,边缘的格子宽高可能不一样,但是中间的网格都是对齐的</para>
    /// </remarks>
    static void CreateFrameCells(IntRect box, Frame frame)
    {
        #region 计算帧box的横纵向单元格数量
        //这里计算的w是第一个单元格的宽度(最大为4像素)
        int w = 4 - ((frame.box.xMin - box.xMin) % 4);

        //初始化帧的box横纵向单元格数量
        frame.nb_cell_w = 0;
        frame.nb_cell_h = 0;

        #region  计算帧box的宽度(单元格单位)
        // 帧总宽度-第一格宽 <= 1, 说明只有1个单元格,1就是边缘1像素
        if ((frame.width - w) <= 1) 
            frame.nb_cell_w = 1;
        else
        {
            //总宽 - 第一格宽 - 边缘 = 剩余格子宽
            int tmp = frame.width - w - 1;
            //(除以4 为 格子数) + 1(第一格) + 1(余数的一格)
            frame.nb_cell_w = 2 + (tmp / 4);
            //如果没余数,那就-1,还一格
            if ((tmp % 4) == 0)
                frame.nb_cell_w--;
        }
        #endregion

        #region  计算帧box的宽度(单元格单位)
        int h = 4 - ((frame.box.yMin - box.yMin) % 4);
        if ((frame.height - h) <= 1)
            frame.nb_cell_h = 1;
        else
        {
            int tmp = frame.height - h - 1;
            frame.nb_cell_h = 2 + (tmp / 4);
            if ((tmp % 4) == 0)
                frame.nb_cell_h--;
        }
        #endregion

        #endregion

        #region 建立cell成员宽高信息的映射数组,和朝向的cell数组一样的处理
        //初始化帧的cells数组
        frame.cells = new Cell[frame.nb_cell_w * frame.nb_cell_h];
        //新建两个int数组,映射cell成员的像素单位宽高
        int[] cell_w = new int[frame.nb_cell_w];
        int[] cell_h = new int[frame.nb_cell_h];

        //如果只有一列单元格,就只赋值成员0,数值就是帧的宽度
        if (frame.nb_cell_w == 1)
            cell_w[0] = frame.width;
        //否则就继续遍历第一行每一个单元格,给映射的数组赋值
        else
        {
            cell_w[0] = w;
            for (int i = 1; i < (frame.nb_cell_w - 1); i++)
                cell_w[i] = 4;
            cell_w[frame.nb_cell_w - 1] = frame.width - w - (4 * (frame.nb_cell_w - 2));
        }
        //高同理
        if (frame.nb_cell_h == 1)
            cell_h[0] = frame.height;
        else
        {
            cell_h[0] = h;
            for (int i = 1; i < (frame.nb_cell_h - 1); i++)
                cell_h[i] = 4;
            cell_h[frame.nb_cell_h - 1] = frame.height - h - (4 * (frame.nb_cell_h - 2));
        }
        #endregion

        //y0,x0是帧box在朝向box中的相对起始坐标
        int y0 = frame.box.yMin - box.yMin;
        for (int y = 0; y < frame.nb_cell_h; y++)
        {
            int x0 = frame.box.xMin - box.xMin;
            for (int x = 0; x < frame.nb_cell_w; x++)
            {
                int index = x + (y * frame.nb_cell_w);
                //每格相对朝向box的起始坐标
                frame.cells[index].x0 = x0;
                frame.cells[index].y0 = y0;
                //映射数组中的宽高属性赋值到cell成员
                frame.cells[index].w = cell_w[x];
                frame.cells[index].h = cell_h[y];
                //递增起始坐标
                x0 += cell_w[x];
            }
            //递增起始坐标
            y0 += cell_h[y];
        }
    }

    /// <summary>
    /// 填满像素缓冲区
    /// </summary>
    /// <param name="header">dcc文件头信息</param>
    /// <param name="frameBuffer">朝向的缓冲区</param>
    /// <param name="dir">朝向类对象</param>
    /// <param name="streams">自定义流集合,用于读取解压像素的文件信息</param>
    /// <remarks>
    /// <para>开头有两个数组</para>
    /// <para>cellBuffer是临时数组,代表上一层和当前层的接替,用来判断当前单元格的上一层单元格,是否有数据,当前是否需要改动,像素掩码的值(修改几个像素)</para>
    /// <para>dir.pixelBuffer是朝向的像素缓冲区,包含每层的数组,会根据临时数组的对比,来判断需要保存的像素块(cell相比上一层无变动的不用存入缓冲区)</para>
    /// <para>按每帧每个单元格读取四种像素颜色信息</para>
    /// <para>对比当前单元格和上一层单元格的四种像素颜色信息,相同的直接略过</para>
    /// <para>不同的,按照先后存续存入dir.pixelBuffer中</para>
    /// <para>把dir.pixelBuffer中的四种像素颜色信息,转换成实际的像素值</para>
    /// </remarks>
    static void FillPixelBuffer(Header header, FrameBuffer frameBuffer, Direction dir, Streams streams)
    {
        //[注] 朝向的像素缓冲区,映射的是所有帧的所有单元格,而非朝向自己的单元格
        dir.pixelBuffer = new PixelBufferEntry[DCC_MAX_PB_ENTRY];
        //这个数组映射的才是朝向单元格
        PixelBufferEntry[] cellBuffer = new PixelBufferEntry[frameBuffer.cells.Length];
        //像素掩码,用来判断读取几个角像素(最多4个角的)
        int pixelMask = 0;
        //当前读取的像素块(4个角像素)
        int[] read_pixel = new int[4];
        //已读取朝向的像素缓冲区条目的总数
        int pb_idx = -1;


        //遍历朝向的每一帧
        for (int f = 0; f < header.framesPerDir; ++f)
        {
            #region 处理帧
            //取当前帧
            Frame frame = dir.frames[f];

            //获取当前帧的单元格[0]的(起始坐标),单元格单位
            int cell0_x = (frame.box.xMin - dir.box.xMin) / 4;
            int cell0_y = (frame.box.yMin - dir.box.yMin) / 4;
            //把当前帧的所有单元格赋值
            CreateFrameCells(dir.box, frame);

            //开始遍历单元格
            for (int y = 0; y < frame.nb_cell_h; y++)
            {
                //当前单元格在帧的缓冲区内的起始坐标y(单元格单位)
                int curr_cell_y = cell0_y + y;
                for (int x = 0; x < frame.nb_cell_w; x++)
                {
                    #region 处理单元格
                    //当前单元格在帧的缓冲区内的起始坐标x(单元格单位)
                    int curr_cell_x = cell0_x + x;
                    //计算起始点坐标对应的朝向单元格的像素映射数组索引
                    int curr_cell = curr_cell_x + (curr_cell_y * frameBuffer.nb_cell_w);
                    //跳过读取像素,直接下个单元格
                    bool nextCell = false;
                    #region 确定像素掩码
                    //如果朝向层的当前单元格有值
                    if (cellBuffer[curr_cell].val != null)
                    {
                        //读取equalCell(对比单元格流),bool类型
                        int tmp = 0;
                        if (streams.equalCell != null)
                            tmp = streams.equalCell.ReadBit();
                        //对比单元格结果为否,就读取pixelMask(像素掩码流)
                        if (tmp == 0)
                            pixelMask = streams.pixelMask.ReadBits(4);
                        //否则,对比单元格流参数为真,nextCell为真,后面代码全部跳过,直接下一单元格
                        else
                            nextCell = true;
                    }
                    //否则,如果像素块为空,像素掩码15,就是4个角像素都要读取
                    else
                        pixelMask = 0x0f;
                    #endregion

                    //当前单元格的朝向层有变动,读取像素信息
                    if (!nextCell)
                    {
                        #region 读取当前单元格的四种像素颜色信息
                        //初始化读像素数组
                        read_pixel[0] = read_pixel[1] = read_pixel[2] = read_pixel[3] = 0;
                        //当前单元格循环中,储存上一个的变量
                        int last_pixel = 0;
                        //根据像素掩码,获取读几个角像素(最多4个)
                        int nb_pix = nb_pix_table[pixelMask];

                        //读取编码类型,0和1
                        int encodingType = 0;
                        if (nb_pix != 0 && streams.encodingType != null)
                        {
                            encodingType = streams.encodingType.ReadBit();
                        }

                        //记录成功读取当前单元格成功读取的像素总数
                        int decoded_pix = 0;
                        //遍历需要解码的像素
                        for (int i = 0; i < nb_pix; i++)
                        {
                            //编码类型为1,直接读取流的数据
                            if (encodingType != 0)
                            {
                                //读原始像素流8bit
                                read_pixel[i] = streams.rawPixel.ReadBits(8);
                            }
                            //编码类型为0,最终结果是: 上一个像素值 + 读取的像素(如果读取的像素是15,就继续读取并累加到最终结果)
                            else
                            {
                                read_pixel[i] = last_pixel;
                                int pix_displ = streams.pixelCode.ReadBits(4);
                                read_pixel[i] += pix_displ;
                                while (pix_displ == 15)
                                {
                                    pix_displ = streams.pixelCode.ReadBits(4);
                                    read_pixel[i] += pix_displ;
                                }
                            }

                            //如果当前像素和上一个像素没有变化
                            if (read_pixel[i] == last_pixel)
                            {
                                //那么当前像素为0(丢弃这个像素),结束需解码像素的遍历(停止解码当前像素块)
                                read_pixel[i] = 0;
                                i = nb_pix;
                            }
                            //如果当前像素和上一个像素有变化
                            else
                            {
                                //把当前像素记录为上一个像素,成功读取的像素总数+1
                                last_pixel = read_pixel[i];
                                decoded_pix++;
                            }

                        }
                        #endregion

                        #region 核心代码,把当前cell的四种颜色信息,赋值给像素缓冲区数组
                        //当前单元格的朝向层,赋值为旧条目
                        PixelBufferEntry old_entry = cellBuffer[curr_cell];

                        //朝向中的像素缓冲区条目数组总数+1,超出就报错
                        pb_idx++;
                        Debug.Assert(pb_idx < DCC_MAX_PB_ENTRY);

                        //开一个新条目
                        var newEntry = new PixelBufferEntry();
                        newEntry.val = new byte[4];
                        //当前单元格的像素成功读取数-1 = curr_idx(当前四种像素颜色索引)
                        int curr_idx = decoded_pix - 1;

                        //遍历四种像素颜色
                        for (int i = 0; i < 4; i++)
                        {
                            //像素掩码对应4角像素,根据对应位是1或0,选择赋新的像素,还是赋之前叠加的cell朝向层像素
                            if ((pixelMask & (1 << i)) != 0)
                            {
                                //四种像素颜色索引没到头,把当前索引赋值给新条目的第i个像素,索引--
                                if (curr_idx >= 0)
                                    newEntry.val[i] = (byte)read_pixel[curr_idx--];
                                //到头了,剩下的值都是0
                                else
                                    newEntry.val[i] = 0;
                            }
                            //如果像素掩码对应位是0,就把旧条目对应角的像素赋值给新条目
                            else
                                newEntry.val[i] = old_entry.val[i];
                        }
                        #endregion

                        
                        newEntry.frame = f;
                        newEntry.frameCellIndex = x + (y * frame.nb_cell_w);
                        //只有收录比较上一层有变动的cell,pb_idx才会++
                        dir.pixelBuffer[pb_idx] = newEntry;
                        //每个cell都会覆盖上一层的的cell,即使没有变动
                        cellBuffer[curr_cell] = newEntry;

                    }
                    #endregion
                }
            }
            #endregion
        }
        

        // 把上面代码给每个像素的索引,转换为实际的256色像素,在朝向的存入像素缓冲区中
        for (int i = 0; i <= pb_idx; i++)
        {
            for (int x = 0; x < 4; x++)
            {
                int y = dir.pixelBuffer[i].val[x];
                dir.pixelBuffer[i].val[x] = dir.pixel_values[y];
            }
        }

        dir.pb_nb_entry = pb_idx + 1;
    }

    /// <summary>
    /// 生成帧
    /// </summary>
    /// <param name="header">DCC的头信息</param>
    /// <param name="dir">DCC朝向</param>
    /// <param name="frameBuffer">朝向的缓冲区</param>
    /// <param name="streams">自定义流</param>
    /// <param name="textures">结果(图片容器)</param>
    /// <param name="sprites">结果(精灵容器)</param>
    /// <remarks>
    /// 根据朝向的缓冲区的信息,生成帧的图片和精灵
    /// </remarks>
    static void MakeFrames(Header header, Direction dir, FrameBuffer frameBuffer, Streams streams, List<Texture2D> textures, List<Sprite> sprites)
    {
        // 边缘填充
        const int padding = 2;
        // 图片的宽度: 扩大至最近的2的次方数(朝向矩阵的宽度 + 边缘填充) * 朝向的数量 ; 2的次方如 : 2,4,8,16,32,64,128,256...,如果值是230,就会变成256 ; 因为图片横向排列,所以还要乘上朝向的数量
        int textureWidth = Mathf.NextPowerOfTwo((dir.box.width + padding) * header.framesPerDir);
        // 图片的高度
        int textureHeight = Mathf.NextPowerOfTwo(dir.box.height + padding);
        // 限制一下纹理宽度不能超过1024
        textureWidth = Mathf.Min(1024, textureWidth);

        // 纹理包(就是Unity切精灵的那个功能)
        var packer = new TexturePacker(textureWidth, textureHeight);
        // 当前纹理
        Texture2D texture = null;
        // 当前纹理的像素数组
        Color32[] pixels = null;

        // 重新初始化帧缓冲区的cell数组的宽高
        for (int c = 0; c < frameBuffer.cells.Length; c++)
        {
            frameBuffer.cells[c].w = -1;
            frameBuffer.cells[c].h = -1;
        }

        int pb_idx = 0;

        // 遍历当前dcc的每一帧
        for (int f = 0; f < header.framesPerDir; f++)
        {
            #region 创建帧的图片和映射的精灵初始化(给坐标和宽高)
            // 取当前帧
            Frame frame = dir.frames[f];
            // 计算当前帧的宽高(单元格单位)
            int nb_cell = frame.nb_cell_w * frame.nb_cell_h;
            //按照朝向矩阵的宽高,打包纹理包
            var pack = packer.put(dir.box.width + padding, dir.box.height + padding);
            //如果返回的结果提示打包不下就会创建一个新的纹理包
            if (pack.newTexture)
            {
                if (texture != null)
                {
                    texture.SetPixels32(pixels);
                    texture.Apply();
                }
                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
                pixels = new Color32[textureWidth * textureHeight];
                textures.Add(texture);
            }
            //把打包出来的坐标信息和新的纹理和像素数组赋值给当前帧
            frame.texture = texture;
            frame.texturePixels = pixels;
            frame.textureX = pack.x;
            frame.textureY = pack.y;

            //创建纹理的矩形
            var textureRect = new Rect(frame.textureX, frame.textureY, dir.box.width, dir.box.height);
            //计算轴心点
            var pivot = new Vector2(-dir.box.xMin / (float)dir.box.width, dir.box.yMax / (float)dir.box.height);
            //创建精灵
            Sprite sprite = Sprite.Create(texture, textureRect, pivot, Iso.pixelsPerUnit, extrude: 0, meshType: SpriteMeshType.FullRect);
            //精灵赋值给精灵数组
            sprites.Add(sprite);
            #endregion

            //遍历当前帧的单元格
            for (int c = 0; c < nb_cell; c++)
            {
                //取当前单元格
                Cell cell = frame.cells[c];

                //获取单元格坐标(单元格单位)
                int cell_x = cell.x0 / 4;
                int cell_y = cell.y0 / 4;
                //获取单元格索引(单元格单位)
                int cell_idx = cell_x + (cell_y * frameBuffer.nb_cell_w);
                //获取同位置的朝向缓冲区单元格
                Cell buff_cell = frameBuffer.cells[cell_idx];
                //按顺序取四种颜色像素块
                PixelBufferEntry pbe = dir.pixelBuffer[pb_idx];


                //看下顺序取得像素块和当前单元格是否匹配
                //不匹配就代表没有像素变化,当前单元格像素未被收入像素块数组

                #region 单元格像素未变动
                if ((pbe.frame != f) || (pbe.frameCellIndex != c))
                {
                    //当前单元格像素未变动,那么看下当前单元格和朝向单元格尺寸是否相同,相同就复制上一帧单元格像素,尺寸不相同怕有错误,不动
                    if ((cell.w == buff_cell.w) && (cell.h == buff_cell.h))
                    {
                        //取前一帧
                        Frame refFrame = dir.frames[f - 1];
                        //计算单元格在前一帧图片中的相对起始点
                        int textureY = refFrame.textureY + dir.box.height - buff_cell.y0;
                        int textureX = refFrame.textureX + buff_cell.x0;
                        //图片在前一帧像素数组中的起始下标
                        int srcOffset = refFrame.texture.width * textureY + textureX;
                        //计算单元格在当前帧图片中的相对起始点
                        textureY = frame.textureY + dir.box.height - cell.y0;
                        textureX = frame.textureX + cell.x0;
                        //图片在当前帧像素数组中的起始下标
                        int dstOffset = frame.texture.width * textureY + textureX;
                        //复制前一帧的像素到当前帧的像素数组
                        for (int y = 0; y < cell.h; y++)
                        {
                            System.Array.Copy(refFrame.texturePixels, srcOffset, frame.texturePixels, dstOffset, cell.w);
                            //由于是从下网上读的,所以要减一行的宽度
                            srcOffset -= refFrame.texture.width;
                            dstOffset -= frame.texture.width;
                        }
                    }
                }
                #endregion

                //匹配就代表像素有变动

                #region 单元格像素有变动
                else
                {
                    // fill the frame cell with pixels

                    //角0像素 == 角1像素,什么都不做
                    if (pbe.val[0] == pbe.val[1])
                    {
                        // fill FRAME cell to color val[0]
                        //clear_to_color(cell->bmp, pbe->val[0]);
                    }

                    //否则
                    else
                    {
                        int nb_bit;
                        //如果角0像素 != 角1像素,那么就看角1像素和角2像素是否相同
                        //如果相同,那么就用1位来表示像素
                        //如果不同,那么就用2位来表示像素
                        if (pbe.val[1] == pbe.val[2])
                            nb_bit = 1;
                        else
                            nb_bit = 2;

                        //计算单元格在当前帧图片中的相对起始点(左上角开始)
                        int textureY = frame.textureY + dir.box.height - cell.y0;
                        int textureX = frame.textureX + cell.x0;
                        //图片在当前帧像素数组中的起始下标(左上角开始)
                        int offset = frame.texture.width * textureY + textureX;
                        //遍历单元格的像素
                        for (int y = 0; y < cell.h; ++y)
                        {
                            for (int x = 0; x < cell.w; ++x)
                            {
                                //读像素编码
                                int pix = streams.pixelCode.ReadBits(nb_bit);
                                //读取得像素编码转换位实际颜色
                                Color32 color = Palette.palette[pbe.val[pix]];
                                //复制给帧得像素矩阵
                                frame.texturePixels[offset + x] = color;
                            }
                            //由于是从下网上读的,所以要减一行的宽度
                            offset -= frame.texture.width;
                        }
                    }

                    //四种颜色像素块数组下标+1
                    pb_idx++;
                }

                #endregion

                //当前单元格信息复制给对应方向缓冲区单元格
                frameBuffer.cells[cell_idx] = cell;
            }
        }

        //图片遍历不为空,把像素数组赋值给图片并应用一下
        if (texture != null)
        {
            texture.SetPixels32(pixels);
            texture.Apply();
        }
    }

    /// <summary>
    /// DCC文件缓存
    /// </summary>
    static Dictionary<string, DCC> cache = new Dictionary<string, DCC>();

    /// <summary>
    /// 位宽对照表
    /// </summary>
    /// <remarks>
    /// 读取帧信息时用的,由于帧信息比较长,它的位宽用二进制代替了,通过这个表转换成对应的位宽
    /// </remarks>
    static int[] widthTable = { 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 32 };

    /// <summary>
    /// 像素掩码对照表,就一个用处,解压DCC的图片信息
    /// </summary>
    /// <remarks>
    /// 成员0-15 { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 }
    /// </remarks>
    static int[] nb_pix_table = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };

    /// <summary>
    /// 1个方向的动画的对象的方向索引
    /// </summary>
    static int[] dirs1 = new int[] { 0 };
    /// <summary>
    /// 4个方向的动画的对象的方向索引
    /// </summary>
    static int[] dirs4 = new int[] { 0, 1, 2, 3 };
    /// <summary>
    /// 8个方向的动画的对象的方向索引
    /// </summary>
    static int[] dirs8 = new int[] { 4, 0, 5, 1, 6, 2, 7, 3 };
    /// <summary>
    /// 16个方向的动画的对象的方向索引
    /// </summary>
    static int[] dirs16 = new int[] { 4, 8, 0, 9, 5, 10, 1, 11, 6, 12, 2, 13, 7, 14, 3, 15 };
    
    /// <summary>
    /// 加载DCC文件
    /// </summary>
    /// <param name="filename">路径名</param>
    /// <param name="ignoreCache">是否忽略原缓存</param>
    /// <returns>
    /// [核心方法]加载DCC文件得到包含某个对象的一个姿态的DCC对象(行走,攻击,死亡等)
    /// </returns>
    static public DCC Load(string filename, bool ignoreCache = false)
    {

        //路径字符串转小写
        filename = filename.ToLower();
        //如果缓存里有就直接返回
        if (!ignoreCache && cache.ContainsKey(filename))
        {
            return cache[filename];
        }

        //没有缓存就开始加载
        Debug.Log("DCC文件加载: " + filename);
        //开启计时
        var sw = System.Diagnostics.Stopwatch.StartNew();

        //创建一个DCC对象用于返回值
        DCC dcc = new DCC();
        dcc.textures = new List<Texture2D>();
        dcc.sprites = new List<Sprite>();

        //读文件,二进制流
        byte[] bytes = File.ReadAllBytes(filename);
        var stream = new MemoryStream(bytes);
        var reader = new BinaryReader(stream);

        //读取头信息
        Header header = new Header();
        ReadHeader(reader, header);

        //根据DCC中的朝向数量,选择对应的朝向索引数组
        int[] dirs = null;
        switch (header.directionCount)
        {
            case 1: dirs = dirs1; break;
            case 4: dirs = dirs4; break;
            case 8: dirs = dirs8; break;
            case 16: dirs = dirs16; break;
        }

        //遍历每一个朝向,处理动画文件
        for (int d = 0; d < header.directionCount; ++d)
        {
            #region 处理每个朝向
            //创建比特流,并读取当前朝向的信息给临时变量
            //当前朝向数据的起点[朝向索引[遍历d]],乘以8是起始点转换为比特
            var bitReader = new BitReader(bytes, header.dirOffset[dirs[d]] * 8);
            Direction dir = new Direction();
            ReadDirection(bitReader, dir);

            //可选字节总和(未实装,加着玩的)
            int optionalBytesSum = 0;

            //初始化方向的帧数组
            dir.frames = new Frame[header.framesPerDir];
            //遍历方向的帧数组的每一帧
            for (int f = 0; f < header.framesPerDir; ++f)
            {
                #region 处理每一帧
                //临时变量,读取当前帧
                Frame frame = new Frame();
                dir.frames[f] = frame;
                ReadFrame(bitReader, dir, frame);

                //叠加可选字节(未实装,加着玩的)
                optionalBytesSum += frame.optionalBytes;
                //如果帧是从下往上渲染就跳过,(未实装,跳过这一帧)
                if (frame.bottomUp != 0)
                {
                    Debug.LogWarning("自下而上渲染图像的框架尚未实现 (" + filename + ")");
                    continue;
                }

                //计算朝向缓冲区的矩阵
                if (f == 0)
                    dir.box = frame.box;
                else
                {
                    dir.box.xMin = Mathf.Min(dir.box.xMin, frame.box.xMin);
                    dir.box.yMin = Mathf.Min(dir.box.yMin, frame.box.yMin);
                    dir.box.xMax = Mathf.Max(dir.box.xMax, frame.box.xMax);
                    dir.box.yMax = Mathf.Max(dir.box.yMax, frame.box.yMax);
                }
                #endregion
            }

            //如果有可选字节,跳过这段数据(因为未实装)
            if (optionalBytesSum != 0)
                Debug.LogWarning("可选字节总和 != 0, 未进行测试");
            bitReader.ReadBits(optionalBytesSum * 8);

            // 创建自定义流合集
            Streams streams = new Streams();
            // 根据朝向的压缩标志读取对应的几种流,用于解压像素数据
            ReadStreamsInfo(bitReader, dir, bytes, streams);

            // 创建朝向得缓冲区,在缓冲区内生成最终的图片对象和精灵对象
            FrameBuffer frameBuffer = CreateFrameBuffer(dir);
            //初始化朝向的像素块数组,像素块数组映射的是每帧的单元格数组中的像素信息,这里给所有单元格读取四种颜色像素信息
            FillPixelBuffer(header, frameBuffer, dir, streams);
            //遍历每帧,创建图片和精灵,精灵就起始坐标和宽高,图片是通过前面的四种像素颜色值来读取的
            MakeFrames(header, dir, frameBuffer, streams, dcc.textures, dcc.sprites); 
            #endregion
        }
        

        //赋值dcc的方向数
        dcc.directionCount = header.directionCount;
        //赋值dcc的每个朝向包含几帧
        dcc.framesPerDirection = header.framesPerDir;
        //如果忽略缓存,那就覆盖原来的缓存
        if (!ignoreCache)
            cache.Add(filename, dcc);

        //打印加载时间
        Debug.Log("加载时间: " + sw.Elapsed + ", 成功加载" + dcc.sprites.Count + " 个sprites");

        return dcc;
    }

    /// <summary>
    /// 将dcc文件转换成图片(仅用于测试)
    /// </summary>
    /// <param name="assetPath"></param>
    static public void ConvertToPng(string assetPath)
    {
        //加载第一幕的调色板
        Palette.LoadPalette(1);
        //加载dcc文件
        DCC dcc = Load(assetPath, ignoreCache: true);
        int i = 0;
        //遍历dcc的纹理数组
        foreach (var texture in dcc.textures)
        {
            // 将Texture2D对象编码为PNG格式的字节数组
            var pngData = texture.EncodeToPNG();
            
            // 立即销毁Unity中的Texture2D对象，释放内存
            Object.DestroyImmediate(texture);
            
            // 构造PNG文件的保存路径，格式为"原路径.序号.png"
            var pngPath = assetPath + "." + i + ".png";
            
            // 将PNG数据写入到指定路径的文件中
            File.WriteAllBytes(pngPath, pngData);
            
            // 刷新pngPath路径的Unity资源数据库，使新创建的PNG文件能够立即在编辑器中可见

            AssetDatabase.ImportAsset(pngPath);
            //处理下一个文件
            ++i;
        }
    }
}