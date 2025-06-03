using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// DCC文件导入器
/// </summary>
/// <remarks>
/// 储存游戏对象的一个动作的(包含所有朝向)动画信息
/// </remarks>
public class DCC
{
    /// <summary>
    /// 
    /// </summary>
    public List<Texture2D> textures;
    /// <summary>
    /// 
    /// </summary>
    public List<Sprite> sprites;
    /// <summary>
    /// ddc的动画朝向数量
    /// </summary>
    public int directionCount;
    /// <summary>
    /// dcc的每个朝向有几帧
    /// </summary>
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
    class Header
    {
        public byte fileSignature;
        /// <summary>
        /// DCC文件版本号
        /// </summary>
        public byte version;
        /// <summary>
        /// 方向总数
        /// </summary>
        public byte directionCount;
        /// <summary>
        /// 每一方向的帧数
        /// </summary>
        public int framesPerDir;
        /// <summary>
        /// 
        /// </summary>
        public int tag;
        /// <summary>
        /// 
        /// </summary>
        public int finalDc6Size;
        /// <summary>
        /// 方向的偏移量数组
        /// </summary>
        /// <remarks>
        /// 数组长度为方向总数,应该是对应每个方向的偏移量
        /// </remarks>
        public int[] dirOffset;
    }

    class Direction
    {
        /// <summary>
        /// 编码数据大小(字节数)
        /// </summary>
        public int outsizeCoded;
        /// <summary>
        /// 压缩标志位（0x01和0x02表示不同压缩方式）
        /// </summary>
        /// <remarks>
        /// 动画切片的像素有不同压缩方式,这个代表使用哪种方式解压像素
        /// </remarks>
        public int compressionFlag;


        /// <summary>
        /// 可变参数0的位宽
        /// </summary>
        public int variable0Bits;
        /// <summary>
        /// 宽度的位宽
        /// </summary>
        public int widthBits;
        /// <summary>
        /// 高度的位宽
        /// </summary>
        public int heightBits;
        /// <summary>
        /// x偏移量的位宽
        /// </summary>
        public int xoffsetBits;
        /// <summary>
        /// y偏移量的位宽
        /// </summary>
        public int yoffsetBits;
        /// <summary>
        /// 可选字节的位宽
        /// </summary>
        public int optionalBytesBits;
        /// <summary>
        /// 编码字节的位宽
        /// </summary>
        public int codedBytesBits;

        /// <summary>
        /// 方向动画矩阵,默认值0矩阵,所有的帧矩阵叠加到一起的区域
        /// </summary>
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
        /// 当前方向的所有像素缓冲区数组
        /// </summary>
        /// <remarks>
        /// 按每帧图片分割的像素缓冲区
        /// </remarks>
        public PixelBufferEntry[] pixelBuffer;
        /// <summary>
        /// pixelBuffer的长度
        /// </summary>
        public int pb_nb_entry;
    }

    /// <summary>
    /// 帧缓冲区
    /// </summary>
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
    /// 动画帧信息
    /// </summary>
    class Frame
    {
        /// <summary>
        /// 帧的可变参数0
        /// </summary>
        public int variable0;
        /// <summary>
        /// 帧宽度(像素)
        /// </summary>
        public int width;
        /// <summary>
        /// 帧高度(像素)
        /// </summary>
        public int height;
        /// <summary>
        /// x偏移量
        /// </summary>
        public int xoffset;
        /// <summary>
        /// y偏移量
        /// </summary>
        public int yoffset;
        /// <summary>
        /// 可选字节
        /// </summary>
        public int optionalBytes;
        /// <summary>
        /// 编码字节
        /// </summary>
        public int codedBytes;
        /// <summary>
        /// 帧是否从底部向上渲染
        /// </summary>
        public int bottomUp;
        /// <summary>
        /// 帧像素矩阵
        /// </summary>
        public IntRect box;

        /// <summary>
        /// 帧的单元格数组
        /// </summary>
        public Cell[] cells;
        /// <summary>
        /// 帧的横向单元格数量
        /// </summary>
        public int nb_cell_w = 0;
        /// <summary>
        /// 帧的纵向单元格数量
        /// </summary>
        public int nb_cell_h = 0;

        /// <summary>
        /// 帧的纹理
        /// </summary>
        public Texture2D texture;
        /// <summary>
        /// 帧的像素矩阵
        /// </summary>
        public Color32[] texturePixels;
        /// <summary>
        /// 帧的纹理x坐标
        /// </summary>
        public int textureX;
        /// <summary>
        /// 帧的纹理y坐标
        /// </summary>
        public int textureY;
    }
    /// <summary>
    /// 像素缓冲区成员
    /// </summary>
    /// <remarks>
    /// 对应的就是帧缓冲区的单元格
    /// </remarks>
    struct PixelBufferEntry
    {
        /// <summary>
        /// 像素数组(单元格的4个像素)
        /// </summary>
        public byte[] val;
        /// <summary>
        /// 当前帧
        /// </summary>
        public int frame;
        /// <summary>
        /// 当前单元格
        /// </summary>
        public int frameCellIndex;
    }

    /// <summary>
    /// 自定义流,包含5个流用来解压动画图片的像素信息
    /// </summary>
    /// <remarks>
    /// 根据不同的解压方式,选择赋值其中的某几个流,不一定全部都会赋值
    /// </remarks>
    class Streams
    {
        public BitReader equalCell;
        public BitReader pixelMask;
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
    /// 读取方向信息(比特流)
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
    /// <param name="bitReader"></param>
    /// <param name="dir"></param>
    /// <param name="frame"></param>
    /// <remarks>
    /// 一个动作的其中一帧的信息
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
        //创建帧的图形矩阵(包含锚点和宽高)
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
    /// Streams自定义流类型,包含5种不同压缩方式的数据流,根据DCC.Direction类的压缩标志位信息,读取不同的流
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
    /// 方向像素矩阵单元格的赋值(方向像素矩阵叫帧缓冲区)
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    /// <remarks>
    /// 帧缓冲区是一个,方向下的所有帧重叠后,用4*4的cell分割储存的数组; 
    /// dir对应一张图片,帧对应一个切片,dir.box是把所有切片重叠在一起的矩形,这个矩形就是帧缓冲区; 
    /// 帧缓冲区的单元格就是4*4像素的格子
    /// cell只包含坐标信息,并且这个方法只创建单元格单元格的宽高,不赋值锚点
    /// </remarks>
    static FrameBuffer CreateFrameBuffer(Direction dir)
    {
        //声明一个缓冲区备用,并用初始化文件导入的dir数据赋值
        FrameBuffer frameBuffer = new FrameBuffer();
        //缓冲区的宽,高
        //因单元格单位,宽高都要除以4,由于有余数,所以要+1
        frameBuffer.nb_cell_w = 1 + ((dir.box.width - 1) / 4);
        frameBuffer.nb_cell_h = 1 + ((dir.box.height - 1) / 4);
        //根据计算的宽高,创建单元格数组
        frameBuffer.cells = new Cell[frameBuffer.nb_cell_w * frameBuffer.nb_cell_h];

        //单元格宽高度数组,例如缓冲区宽为18那么数组成员就是{4,4,4,4,2},因为单元格为4像素
        int[] cell_w = new int[frameBuffer.nb_cell_w];
        int[] cell_h = new int[frameBuffer.nb_cell_h];

        //给单元格的宽高数组赋值
        //赋值宽
        //如果等于1,证明宽小于4像素,直接赋值给数组第一个元素
        if (frameBuffer.nb_cell_w == 1)
            cell_w[0] = dir.box.width;
        //否则,给宽数组都赋值4,并把最后成员的值改成余数
        else
        {
            for (int i = 0; i < (frameBuffer.nb_cell_w - 1); i++)
                cell_w[i] = 4;
            //计算出末尾,不被4整除的部分,赋值给最后一个值
            cell_w[frameBuffer.nb_cell_w - 1] = dir.box.width - (4 * (frameBuffer.nb_cell_w - 1));
        }
        //赋值高
        if (frameBuffer.nb_cell_h == 1)
            cell_h[0] = dir.box.height;
        else
        {
            for (int i = 0; i < (frameBuffer.nb_cell_h - 1); i++)
                cell_h[i] = 4;
            cell_h[frameBuffer.nb_cell_h - 1] = dir.box.height - (4 * (frameBuffer.nb_cell_h - 1));
        }


        //根据两个临时单元格数组的宽高信息,创建缓冲区的单元格数组
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

        return frameBuffer;
    }

    /// <summary>
    /// 帧像素矩阵单元格的赋值
    /// </summary>
    /// <param name="box">方向的像素矩阵</param>
    /// <param name="frame">方向的其中一帧</param>
    static void CreateFrameCells(IntRect box, Frame frame)
    {
        #region 计算帧的横纵向单元格数量
        // width (in # of pixels) in 1st column //第一列cell中像素的宽度
        // 1. 方向像素矩阵的左边框 - 帧像素矩阵左边框 = 这两个边框中间的空白宽度
        // 2. 由于每格cell都是4像素,所以 这两个边框中间的空白宽度 % 4 = 第一个有像素的cell格的空白宽度
        // 3. 4 - 第一个有像素的cell格的空白宽度 = 第一个有像素的cell格的像素宽度
        int w = 4 - ((frame.box.xMin - box.xMin) % 4);

        //初始化帧的单元格宽高
        frame.nb_cell_w = 0;
        frame.nb_cell_h = 0;

        // 帧的像素矩阵宽度 - 第一列像素矩阵宽度 = 剩下列的宽度
        // 如果剩下的列的宽度 <= 1(每列的间隔),那就是只有1列
        if ((frame.width - w) <= 1) // if 2nd column is 0 or 1 pixel width
            frame.nb_cell_w = 1;
        else
        {
            // 剩下的列的宽度 - 1(减去1,2列之间的间隔)
            // so, we have minimum 2 pixels behind 1st column  // 至少有2像素在第一列后面,第5,6个像素
            int tmp = frame.width - w - 1; // tmp is minimum 1, can't be 0 // 因为这个else分支frame.width最少是6像素,w最高不超过4像素,所以tmp至少是1
            //tmp/4(余下列的宽度的单元格数量); +2(第一列单元格和最后一列的余数单元格)
            frame.nb_cell_w = 2 + (tmp / 4);
            //如果没余数,那就-1
            if ((tmp % 4) == 0)
                frame.nb_cell_w--;
        }

        //高同样处理一遍
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

        #region 把第一行和第一列的单元格宽高信息映射到数组中(后面的单元格都是和第一列/行一样的)
        //按照前面计算的横纵向单元格数量,给真的单元格数组赋值
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

        //(y0,x0)就是以方向矩阵左下角为原点,帧矩阵的单元格的左下角坐标
        int y0 = frame.box.yMin - box.yMin;
        for (int y = 0; y < frame.nb_cell_h; y++)
        {
            int x0 = frame.box.xMin - box.xMin;
            for (int x = 0; x < frame.nb_cell_w; x++)
            {
                int index = x + (y * frame.nb_cell_w);
                frame.cells[index].x0 = x0;
                frame.cells[index].y0 = y0;
                //映射cell的数组记录了cell内像素的长和宽
                frame.cells[index].w = cell_w[x];
                frame.cells[index].h = cell_h[y];
                //递增左下坐标
                x0 += cell_w[x];
            }
            //递增左下坐标
            y0 += cell_h[y];
        }
    }

    /// <summary>
    /// 像素填充到帧缓冲区中
    /// </summary>
    /// <param name="header">dcc头信息</param>
    /// <param name="frameBuffer">帧缓冲区(方向像素矩阵的cell数组)</param>
    /// <param name="dir">dcc方向信息</param>
    /// <param name="streams">解压像素的流</param>
    /// <remarks>
    /// 把原本重叠在一起的像素矩阵按帧分层,每帧按对应的cell分割成FrameBuffer,把分割的FrameBuffer按顺序存入dir
    /// </remarks>
    static void FillPixelBuffer(Header header, FrameBuffer frameBuffer, Direction dir, Streams streams)
    {
        //初始化方向类中的像素块数组,一块16个像素
        dir.pixelBuffer = new PixelBufferEntry[DCC_MAX_PB_ENTRY];
        //新建一个像素块数组,长度是缓冲区的单元格数量
        PixelBufferEntry[] cellBuffer = new PixelBufferEntry[frameBuffer.cells.Length];
        //像素掩码
        int pixelMask = 0;
        //cell四个角的像素点,通过这4个点来算出cell的16个像素
        int[] read_pixel = new int[4];
        int pb_idx = -1;

        //遍历方向的每一帧
        for (int f = 0; f < header.framesPerDir; ++f)
        {
            //取当前帧
            Frame frame = dir.frames[f];
            //帧矩阵的锚点(左下)坐标,单元格单位的
            int cell0_x = (frame.box.xMin - dir.box.xMin) / 4;
            int cell0_y = (frame.box.yMin - dir.box.yMin) / 4;
            //把当前帧的所有单元格赋值
            CreateFrameCells(dir.box, frame); // dcc_prepare_frame_cells // dcc准备帧的单元格

            #region 后面这段涉及到像素解码,搞不懂,大概意思就是遍历一遍单元格,按照某种解压的算法把当前帧的像素提取出来,按cell分割好,顺序放入dir.pixelBuffer中
            //开始遍历单元格
            for (int y = 0; y < frame.nb_cell_h; y++)
            {
                //当前单元格在帧缓冲区内的锚点y(左下)
                int curr_cell_y = cell0_y + y;
                for (int x = 0; x < frame.nb_cell_w; x++)
                {
                    //当前单元格在帧缓冲区内的锚点x(左下)
                    int curr_cell_x = cell0_x + x;
                    //计算锚点对应的单元格索引
                    int curr_cell = curr_cell_x + (curr_cell_y * frameBuffer.nb_cell_w);
                    //是否有下一个单元格
                    bool nextCell = false;
                    //如果当前cell映射的像素块不为空
                    if (cellBuffer[curr_cell].val != null)
                    {
                        int tmp = 0;
                        if (streams.equalCell != null)
                            tmp = streams.equalCell.ReadBit();

                        if (tmp == 0)
                            pixelMask = streams.pixelMask.ReadBits(4);
                        else
                            nextCell = true;
                    }
                    else
                        pixelMask = 0x0f;

                    if (!nextCell)
                    {
                        //初始化读像素数组
                        read_pixel[0] = read_pixel[1] = read_pixel[2] = read_pixel[3] = 0;
                        //读像素数组,读的最后一个像素?
                        int last_pixel = 0;
                        //需要解码的像素数量
                        int nb_pix = nb_pix_table[pixelMask];

                        //尝试读取编码类型流,流是空的就不会读,只读一个bit,作用基本上就是bool
                        int encodingType = 0;
                        if (nb_pix != 0 && streams.encodingType != null)
                        {
                            encodingType = streams.encodingType.ReadBit();
                        }

                        //解码后的像素数量
                        int decoded_pix = 0;
                        //遍历需要解码的像素数组
                        for (int i = 0; i < nb_pix; i++)
                        {
                            //编码类型为1
                            if (encodingType != 0)
                            {
                                //读原始像素流8bit
                                read_pixel[i] = streams.rawPixel.ReadBits(8);
                            }
                            //编码类型为0
                            else
                            {
                                //1. 赋值最后像素
                                read_pixel[i] = last_pixel;
                                int pix_displ = streams.pixelCode.ReadBits(4);
                                read_pixel[i] += pix_displ;
                                while (pix_displ == 15)
                                {
                                    pix_displ = streams.pixelCode.ReadBits(4);
                                    read_pixel[i] += pix_displ;
                                }
                            }

                            if (read_pixel[i] == last_pixel)
                            {
                                read_pixel[i] = 0; // discard this pixel
                                i = nb_pix;        // stop the decoding of pixels
                            }
                            else
                            {
                                last_pixel = read_pixel[i];
                                decoded_pix++;
                            }
                        }

                        // we have the 4 pixels code for the new entry in pixel_buffer
                        PixelBufferEntry old_entry = cellBuffer[curr_cell];

                        pb_idx++;
                        Debug.Assert(pb_idx < DCC_MAX_PB_ENTRY);

                        var newEntry = new PixelBufferEntry();
                        newEntry.val = new byte[4];
                        int curr_idx = decoded_pix - 1;

                        for (int i = 0; i < 4; i++)
                        {
                            //如果掩码的第i位为1,就把像素数组的第i个像素赋值给新的像素块
                            if ((pixelMask & (1 << i)) != 0)
                            {
                                if (curr_idx >= 0) // if stack is not empty, pop it
                                    newEntry.val[i] = (byte)read_pixel[curr_idx--];
                                else // else pop a 0
                                    newEntry.val[i] = 0;
                            }
                            else
                                newEntry.val[i] = old_entry.val[i];
                        }
                        newEntry.frame = f;
                        newEntry.frameCellIndex = x + (y * frame.nb_cell_w);
                        dir.pixelBuffer[pb_idx] = newEntry;
                        cellBuffer[curr_cell] = newEntry;
                    }
                }
            }
        }
        #endregion

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
    /// <param name="header"></param>
    /// <param name="dir"></param>
    /// <param name="frameBuffer"></param>
    /// <param name="streams"></param>
    /// <param name="textures"></param>
    /// <param name="sprites"></param>
    /// <remarks>
    /// 根据帧缓冲区的信息,生成帧
    /// </remarks>
    static void MakeFrames(Header header, Direction dir, FrameBuffer frameBuffer, Streams streams, List<Texture2D> textures, List<Sprite> sprites)
    {
        // 边缘填充
        const int padding = 2;
        // 纹理的宽度
        int textureWidth = Mathf.NextPowerOfTwo((dir.box.width + padding) * header.framesPerDir);
        // 纹理的高度
        int textureHeight = Mathf.NextPowerOfTwo(dir.box.height + padding);
        // 限制一下纹理宽度不能超过1024
        textureWidth = Mathf.Min(1024, textureWidth);

        // 纹理包
        var packer = new TexturePacker(textureWidth, textureHeight);
        // 当前纹理
        Texture2D texture = null;
        // 当前纹理的像素数组
        Color32[] pixels = null;

        // 初始化帧缓冲区的cell数组的宽高
        for (int c = 0; c < frameBuffer.cells.Length; c++)
        {
            frameBuffer.cells[c].w = -1;
            frameBuffer.cells[c].h = -1;
        }

        int pb_idx = 0;

        // 遍历当前dcc的每一帧
        for (int f = 0; f < header.framesPerDir; f++)
        {
            // 取当前帧
            Frame frame = dir.frames[f];
            // 计算当前帧的单元格数量
            int nb_cell = frame.nb_cell_w * frame.nb_cell_h;
            //打包纹理包
            var pack = packer.put(dir.box.width + padding, dir.box.height + padding);
            //打包不下就会创建一个新的纹理包
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

            //遍历当前帧的单元格
            for (int c = 0; c < nb_cell; c++)
            {
                //取当前单元格
                Cell cell = frame.cells[c];

                // buffer cell // 缓存单元格
                int cell_x = cell.x0 / 4;
                int cell_y = cell.y0 / 4;
                int cell_idx = cell_x + (cell_y * frameBuffer.nb_cell_w);
                //取帧缓冲区的单元格
                Cell buff_cell = frameBuffer.cells[cell_idx];
                //取对应的像素块
                PixelBufferEntry pbe = dir.pixelBuffer[pb_idx];

                // equal cell checks
                //取的像素块和当前的单元格的frame和frameCellIndex不同
                if ((pbe.frame != f) || (pbe.frameCellIndex != c))
                {
                    // this buffer cell have an equalcell bit set to 1
                    //    so either copy the frame cell or clear it

                    //如果取的像素块和当前像素块宽高相同
                    if ((cell.w == buff_cell.w) && (cell.h == buff_cell.h))
                    {
                        //提取前一帧
                        Frame refFrame = dir.frames[f - 1];
                        int textureY = refFrame.textureY + dir.box.height - buff_cell.y0;
                        int textureX = refFrame.textureX + buff_cell.x0;
                        int srcOffset = refFrame.texture.width * textureY + textureX;
                        textureY = frame.textureY + dir.box.height - cell.y0;
                        textureX = frame.textureX + cell.x0;
                        int dstOffset = frame.texture.width * textureY + textureX;
                        for (int y = 0; y < cell.h; y++)
                        {
                            System.Array.Copy(refFrame.texturePixels, srcOffset, frame.texturePixels, dstOffset, cell.w);
                            srcOffset -= refFrame.texture.width;
                            dstOffset -= frame.texture.width;
                        }
                    }
                }
                else
                {
                    // fill the frame cell with pixels

                    if (pbe.val[0] == pbe.val[1])
                    {
                        // fill FRAME cell to color val[0]
                        //clear_to_color(cell->bmp, pbe->val[0]);
                    }
                    else
                    {
                        int nb_bit;
                        if (pbe.val[1] == pbe.val[2])
                            nb_bit = 1;
                        else
                            nb_bit = 2;

                        // fill FRAME cell with pixels
                        int textureY = frame.textureY + dir.box.height - cell.y0;
                        int textureX = frame.textureX + cell.x0;
                        int offset = frame.texture.width * textureY + textureX;
                        for (int y = 0; y < cell.h; ++y)
                        {
                            for (int x = 0; x < cell.w; ++x)
                            {
                                int pix = streams.pixelCode.ReadBits(nb_bit);
                                Color32 color = Palette.palette[pbe.val[pix]];
                                frame.texturePixels[offset + x] = color;
                            }
                            offset -= frame.texture.width;
                        }
                    }

                    // next pixelbuffer entry
                    pb_idx++;
                }

                // for the buffer cell that was used by this frame cell,
                // save the width & size of the current frame cell
                // (needed for further tests about equalcell)
                // and save its origin, for further copy when equalcell
                frameBuffer.cells[cell_idx] = cell;
            }
        }

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
    /// <param name="filename"文件路径</param>
    /// <param name="ignoreCache">是否忽略缓存</param>
    /// <returns></returns>
    /// <remarks>
    /// 一个DCC文件包含一个角色的一个动作(包含所有的朝向)
    /// </remarks>
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

        //方向索引数组,按对象动画是几个方向的,赋值对应的数组
        int[] dirs = null;
        switch (header.directionCount)
        {
            case 1: dirs = dirs1; break;
            case 4: dirs = dirs4; break;
            case 8: dirs = dirs8; break;
            case 16: dirs = dirs16; break;
        }

        //遍历每一个方向的动画
        for (int d = 0; d < header.directionCount; ++d)
        {
            //用比特流读取方向信息
            //形参2乘以8是将字节数转为位数
            var bitReader = new BitReader(bytes, header.dirOffset[dirs[d]] * 8);
            Direction dir = new Direction();
            ReadDirection(bitReader, dir);

            //可选字节总和
            int optionalBytesSum = 0;
            //初始化方向的帧数组
            dir.frames = new Frame[header.framesPerDir];

            //遍历方向的帧数组的每一帧
            for (int f = 0; f < header.framesPerDir; ++f)
            {
                //初始化当前帧并且导入dcc文件中的相关信息
                Frame frame = new Frame();
                dir.frames[f] = frame;
                ReadFrame(bitReader, dir, frame);

                //叠加可选字节
                optionalBytesSum += frame.optionalBytes;

                //如果帧是从下往上渲染就跳过,因为还没实现
                if (frame.bottomUp != 0)
                {
                    Debug.LogWarning("自下而上渲染图像的框架尚未实现 (" + filename + ")");
                    continue;
                }

                //设定方向的动画矩阵,它要能包裹它的所有帧的动画矩阵
                //它是所有帧的动画矩阵重叠在一起的矩形区域
                if (f == 0)
                    dir.box = frame.box;
                else
                {
                    dir.box.xMin = Mathf.Min(dir.box.xMin, frame.box.xMin);
                    dir.box.yMin = Mathf.Min(dir.box.yMin, frame.box.yMin);
                    dir.box.xMax = Mathf.Max(dir.box.xMax, frame.box.xMax);
                    dir.box.yMax = Mathf.Max(dir.box.yMax, frame.box.yMax);
                }
             }

            //如果有可选字节
            if (optionalBytesSum != 0)
                Debug.LogWarning("可选字节总和 != 0, 未进行测试");
            //跳过可选字节的数据
            bitReader.ReadBits(optionalBytesSum * 8);

            // 创建自定义流
            Streams streams = new Streams();
            // 根据dir的压缩位标志决定赋值自定义流中的哪些流成员
            ReadStreamsInfo(bitReader, dir, bytes, streams);


            FrameBuffer frameBuffer = CreateFrameBuffer(dir);  // dcc准备缓冲区单元格数组(未赋值单元格锚点)
            FillPixelBuffer(header, frameBuffer, dir, streams); // dcc_fill_pixel_buffer  // dcc像素数据,填充到缓冲区
            MakeFrames(header, dir, frameBuffer, streams, dcc.textures, dcc.sprites); // dcc_make_frames // dcc创建帧
        }

        //赋值dcc的方向数
        dcc.directionCount = header.directionCount;
        //赋值dcc的每个朝向包含几帧
        dcc.framesPerDirection = header.framesPerDir;
        //如果忽略缓存,那就覆盖原来的缓存
        if (!ignoreCache)
            cache.Add(filename, dcc);

        //
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