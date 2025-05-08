using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// DCC 类，用于导入 DCC 格式的动画文件
/// DCC 格式是一种用于存储和加载动画数据的文件格式
/// 该类提供了导入 DCC 文件的功能，并将其转换为纹理和精灵
/// </summary>
public class DCC
{
    // 纹理列表
    public List<Texture2D> textures;
    // 精灵列表
    public List<Sprite> sprites;
    // 方向数量
    public int directionCount;
    // 每个方向的帧数
    public int framesPerDirection;

    // 最大像素缓冲区条目数
    const int DCC_MAX_PB_ENTRY = 85000;

    // 定义 Cell 结构体，表示一个单元格
    struct Cell
    {
        public int x0, y0; // 单元格的起始坐标
        public int w, h;   // 单元格的宽度和高度
    }

    // 定义 Header 类，表示 DCC 文件的头部信息
    class Header
    {
        public byte fileSignature; // 文件签名
        public byte version;      // 版本号
        public byte directionCount; // 方向数量
        public int framesPerDir;  // 每个方向的帧数
        public int tag;           // 标签
        public int finalDc6Size;  // 最终 DC6 文件大小
        public int[] dirOffset;   // 每个方向的偏移量
    }

    // 定义 Direction 类，表示一个方向的数据
    class Direction
    {
        public int outsizeCoded;      // 编码后的大小
        public int compressionFlag;   // 压缩标志
        public int variable0Bits;    // 变量0的位数
        public int widthBits;        // 宽度的位数
        public int heightBits;       // 高度的位数
        public int xoffsetBits;      // X 偏移的位数
        public int yoffsetBits;      // Y 偏移的位数
        public int optionalBytesBits; // 可选字节的位数
        public int codedBytesBits;   // 编码字节的位数

        public IntRect box = IntRect.zero; // 矩形框
        public Frame[] frames;             // 帧数组
        public byte[] pixel_values = new byte[256]; // 像素值数组
        public PixelBufferEntry[] pixelBuffer;      // 像素缓冲区
        public int pb_nb_entry;                     // 像素缓冲区条目数
    }

    // 定义 FrameBuffer 结构体，表示帧缓冲区
    struct FrameBuffer
    {
        public Cell[] cells; // 单元格数组
        public int nb_cell_w; // 单元格宽度数量
        public int nb_cell_h; // 单元格高度数量
    }

    // 定义 Frame 类，表示一帧数据
    class Frame
    {
        public int variable0;      // 变量0
        public int width;         // 宽度
        public int height;        // 高度
        public int xoffset;       // X 偏移
        public int yoffset;       // Y 偏移
        public int optionalBytes; // 可选字节
        public int codedBytes;    // 编码字节
        public int bottomUp;      // 是否从下往上绘制
        public IntRect box;       // 矩形框

        public Cell[] cells;      // 单元格数组
        public int nb_cell_w = 0; // 单元格宽度数量
        public int nb_cell_h = 0; // 单元格高度数量

        public Texture2D texture; // 纹理
        public Color32[] texturePixels; // 纹理像素
        public int textureX;      // 纹理 X 坐标
        public int textureY;      // 纹理 Y 坐标
    }

    // 定义 PixelBufferEntry 结构体，表示像素缓冲区条目
    struct PixelBufferEntry
    {
        public byte[] val;        // 像素值
        public int frame;         // 帧索引
        public int frameCellIndex; // 帧单元格索引
    }

    // 定义 Streams 类，表示各种流
    class Streams
    {
        public BitReader equalCell; // 相等单元格流
        public BitReader pixelMask; // 像素掩码流
        public BitReader encodingType; // 编码类型流
        public BitReader rawPixel; // 原始像素流
        public BitReader pixelCode; // 像素编码流
    }

    // 读取 DCC 文件头部信息
    static void ReadHeader(BinaryReader reader, Header header)
    {
        // 读取文件签名
        header.fileSignature = reader.ReadByte();
        // 读取版本号
        header.version = reader.ReadByte();
        // 读取方向数量
        header.directionCount = reader.ReadByte();
        // 读取每个方向的帧数
        header.framesPerDir = reader.ReadInt32();
        // 读取标签
        header.tag = reader.ReadInt32();
        // 读取最终 DC6 文件大小
        header.finalDc6Size = reader.ReadInt32();
        // 初始化每个方向的偏移量数组
        header.dirOffset = new int[header.directionCount];
        // 遍历每个方向，读取偏移量
        for (int dir = 0; dir < header.directionCount; ++dir)
        {
            header.dirOffset[dir] = reader.ReadInt32();
        }
    }

    // 读取方向信息
    static void ReadDirection(BitReader bitReader, Direction dir)
    {
        // 读取编码后的大小
        dir.outsizeCoded = bitReader.ReadBits(32);
        // 读取压缩标志
        dir.compressionFlag = bitReader.ReadBits(2);
        // 读取变量0的位数
        dir.variable0Bits = bitReader.ReadBits(4);
        // 读取宽度的位数
        dir.widthBits = bitReader.ReadBits(4);
        // 读取高度的位数
        dir.heightBits = bitReader.ReadBits(4);
        // 读取 X 偏移的位数
        dir.xoffsetBits = bitReader.ReadBits(4);
        // 读取 Y 偏移的位数
        dir.yoffsetBits = bitReader.ReadBits(4);
        // 读取可选字节的位数
        dir.optionalBytesBits = bitReader.ReadBits(4);
        // 读取编码字节的位数
        dir.codedBytesBits = bitReader.ReadBits(4);
    }

    // 读取帧信息
    static void ReadFrame(BitReader bitReader, Direction dir, Frame frame)
    {
        // 读取变量0
        frame.variable0 = bitReader.ReadBits(widthTable[dir.variable0Bits]);
        // 读取宽度
        frame.width = bitReader.ReadBits(widthTable[dir.widthBits]);
        // 读取高度
        frame.height = bitReader.ReadBits(widthTable[dir.heightBits]);
        // 读取 X 偏移
        frame.xoffset = bitReader.ReadSigned(widthTable[dir.xoffsetBits]);
        // 读取 Y 偏移
        frame.yoffset = bitReader.ReadSigned(widthTable[dir.yoffsetBits]);
        // 读取可选字节
        frame.optionalBytes = bitReader.ReadBits(widthTable[dir.optionalBytesBits]);
        // 读取编码字节
        frame.codedBytes = bitReader.ReadBits(widthTable[dir.codedBytesBits]);
        // 读取是否从下往上绘制
        frame.bottomUp = bitReader.ReadBits(1);
        // 创建矩形框
        frame.box = new IntRect(frame.xoffset, frame.yoffset, frame.width, frame.height);
    }

    // 读取流信息
    static void ReadStreamsInfo(BitReader bitReader, Direction dir, byte[] dcc, Streams streams)
    {
        // 初始化相等单元格流大小
        int equalCellSize = 0;
        // 初始化像素掩码流大小
        int pixelMaskSize = 0;
        // 初始化编码类型流大小
        int encodingTypeSize = 0;
        // 初始化原始像素流大小
        int rawPixelSize = 0;

        // 如果压缩标志包含 0x02，读取相等单元格流大小
        if ((dir.compressionFlag & 0x02) != 0)
        {
            equalCellSize = bitReader.ReadBits(20);
        }
        // 读取像素掩码流大小
        pixelMaskSize = bitReader.ReadBits(20);
        // 如果压缩标志包含 0x01，读取编码类型流和原始像素流大小
        if ((dir.compressionFlag & 0x01) != 0)
        {
            encodingTypeSize = bitReader.ReadBits(20);
            rawPixelSize = bitReader.ReadBits(20);
        }

        // 遍历 256 个像素值，读取有效的像素值
        for (int i = 0, idx = 0; i < 256; ++i)
        {
            // 如果当前位为 1，表示该像素值有效
            if (bitReader.ReadBit() != 0)
            {
                // 将有效的像素值存储到像素值数组中
                dir.pixel_values[idx] = (byte)i;
                // 移动到下一个索引
                ++idx;
            }
        }

        // 获取当前流的偏移量
        long offset = bitReader.offset;
        // 如果相等单元格流大小不为 0，创建相等单元格流
        if (equalCellSize != 0)
            streams.equalCell = new BitReader(dcc, offset);
        // 更新偏移量
        offset += equalCellSize;
        // 如果像素掩码流大小不为 0，创建像素掩码流
        if (pixelMaskSize != 0)
            streams.pixelMask = new BitReader(dcc, offset);
        // 更新偏移量
        offset += pixelMaskSize;
        // 如果编码类型流大小不为 0，创建编码类型流
        if (encodingTypeSize != 0)
            streams.encodingType = new BitReader(dcc, offset);
        // 更新偏移量
        offset += encodingTypeSize;
        // 如果原始像素流大小不为 0，创建原始像素流
        if (rawPixelSize != 0)
            streams.rawPixel = new BitReader(dcc, offset);
        // 更新偏移量
        offset += rawPixelSize;
        // 创建像素编码流
        streams.pixelCode = new BitReader(dcc, offset);
    }

    // 创建帧缓冲区
    static FrameBuffer CreateFrameBuffer(Direction dir)
    {
        // 初始化帧缓冲区
        FrameBuffer frameBuffer = new FrameBuffer();
        // 计算单元格的宽度数量，每个单元格宽度为4像素
        frameBuffer.nb_cell_w = 1 + ((dir.box.width - 1) / 4);
        // 计算单元格的高度数量，每个单元格高度为4像素
        frameBuffer.nb_cell_h = 1 + ((dir.box.height - 1) / 4);
        // 初始化单元格数组
        frameBuffer.cells = new Cell[frameBuffer.nb_cell_w * frameBuffer.nb_cell_h];
        // 初始化单元格宽度数组
        int[] cell_w = new int[frameBuffer.nb_cell_w];
        // 初始化单元格高度数组
        int[] cell_h = new int[frameBuffer.nb_cell_h];

        // 如果单元格宽度数量为1，直接使用整个宽度
        if (frameBuffer.nb_cell_w == 1)
            cell_w[0] = dir.box.width;
        else
        {
            // 否则，前n-1个单元格宽度为4像素
            for (int i = 0; i < (frameBuffer.nb_cell_w - 1); i++)
                cell_w[i] = 4;
            // 最后一个单元格宽度为剩余宽度
            cell_w[frameBuffer.nb_cell_w - 1] = dir.box.width - (4 * (frameBuffer.nb_cell_w - 1));
        }

        // 如果单元格高度数量为1，直接使用整个高度
        if (frameBuffer.nb_cell_h == 1)
            cell_h[0] = dir.box.height;
        else
        {
            // 否则，前n-1个单元格高度为4像素
            for (int i = 0; i < (frameBuffer.nb_cell_h - 1); i++)
                cell_h[i] = 4;
            // 最后一个单元格高度为剩余高度
            cell_h[frameBuffer.nb_cell_h - 1] = dir.box.height - (4 * (frameBuffer.nb_cell_h - 1));
        }

        // 初始化y坐标
        int y0 = 0;
        // 遍历每一行单元格
        for (int y = 0; y < frameBuffer.nb_cell_h; y++)
        {
            // 初始化x坐标
            int x0 = 0;
            // 遍历每一列单元格
            for (int x = 0; x < frameBuffer.nb_cell_w; x++)
            {
                // 计算当前单元格的索引
                int index = x + (y * frameBuffer.nb_cell_w);
                // 设置当前单元格的宽度
                frameBuffer.cells[index].w = cell_w[x];
                // 设置当前单元格的高度
                frameBuffer.cells[index].h = cell_h[y];
                // 更新x坐标
                x0 += 4;
            }
            y0 += 4;
        }

        return frameBuffer;
    }

    // 创建帧单元格
    static void CreateFrameCells(IntRect box, Frame frame)
    {
        // 计算第一个单元格的宽度，4减去帧的X坐标与矩形框X坐标的差值对4取模
        int w = 4 - ((frame.box.xMin - box.xMin) % 4);

        // 初始化单元格宽度和高度数量
        frame.nb_cell_w = 0;
        frame.nb_cell_h = 0;

        // 如果帧宽度减去第一个单元格宽度小于等于1，则单元格宽度数量为1
        if ((frame.width - w) <= 1)
            frame.nb_cell_w = 1;
        else
        {
            // 否则，计算剩余的宽度
            int tmp = frame.width - w - 1;
            // 计算单元格宽度数量，2加上剩余宽度除以4
            frame.nb_cell_w = 2 + (tmp / 4);
            // 如果剩余宽度是4的倍数，则减少一个单元格
            if ((tmp % 4) == 0)
                frame.nb_cell_w--;
        }

        // 计算第一个单元格的高度，4减去帧的Y坐标与矩形框Y坐标的差值对4取模
        int h = 4 - ((frame.box.yMin - box.yMin) % 4);
        // 如果帧高度减去第一个单元格高度小于等于1，则单元格高度数量为1
        if ((frame.height - h) <= 1)
            frame.nb_cell_h = 1;
        else
        {
            // 否则，计算剩余的高度
            int tmp = frame.height - h - 1;
            // 计算单元格高度数量，2加上剩余高度除以4
            frame.nb_cell_h = 2 + (tmp / 4);
            // 如果剩余高度是4的倍数，则减少一个单元格
            if ((tmp % 4) == 0)
                frame.nb_cell_h--;
        }

        // 初始化单元格数组
        frame.cells = new Cell[frame.nb_cell_w * frame.nb_cell_h];
        // 初始化单元格宽度数组
        int[] cell_w = new int[frame.nb_cell_w];
        // 初始化单元格高度数组
        int[] cell_h = new int[frame.nb_cell_h];
        
        // 如果单元格宽度数量为1，直接使用帧宽度
        if (frame.nb_cell_w == 1)
            cell_w[0] = frame.width;
        else
        {
            // 否则，第一个单元格宽度为w
            cell_w[0] = w;
            // 中间单元格宽度为4
            for (int i = 1; i < (frame.nb_cell_w - 1); i++)
                cell_w[i] = 4;
            // 最后一个单元格宽度为帧宽度减去第一个单元格宽度和中间单元格宽度
            cell_w[frame.nb_cell_w - 1] = frame.width - w - (4 * (frame.nb_cell_w - 2));
        }

        // 如果单元格高度数量为1，直接使用帧高度
        if (frame.nb_cell_h == 1)
            cell_h[0] = frame.height;
        else
        {
            // 否则，第一个单元格高度为h
            cell_h[0] = h;
            // 中间单元格高度为4
            for (int i = 1; i < (frame.nb_cell_h - 1); i++)
                cell_h[i] = 4;
            // 最后一个单元格高度为帧高度减去第一个单元格高度和中间单元格高度
            cell_h[frame.nb_cell_h - 1] = frame.height - h - (4 * (frame.nb_cell_h - 2));
        }

        // 初始化y坐标，帧的Y坐标减去矩形框的Y坐标
        int y0 = frame.box.yMin - box.yMin;
        // 遍历每一行单元格
        for (int y = 0; y < frame.nb_cell_h; y++)
        {
            // 初始化x坐标，帧的X坐标减去矩形框的X坐标
            int x0 = frame.box.xMin - box.xMin;
            // 遍历每一列单元格
            for (int x = 0; x < frame.nb_cell_w; x++)
            {
                // 计算当前单元格的索引
                int index = x + (y * frame.nb_cell_w);
                // 设置当前单元格的X坐标
                frame.cells[index].x0 = x0;
                // 设置当前单元格的Y坐标
                frame.cells[index].y0 = y0;
                // 设置当前单元格的宽度
                frame.cells[index].w = cell_w[x];
                // 设置当前单元格的高度
                frame.cells[index].h = cell_h[y];
                // 更新x坐标，加上当前单元格的宽度
                x0 += cell_w[x];
            }
            // 更新y坐标，加上当前单元格的高度
            y0 += cell_h[y];
        }
    }

    // 填充像素缓冲区
    static void FillPixelBuffer(Header header, FrameBuffer frameBuffer, Direction dir, Streams streams)
    {
        // 初始化像素缓冲区数组
        dir.pixelBuffer = new PixelBufferEntry[DCC_MAX_PB_ENTRY];
        // 初始化单元格缓冲区数组
        PixelBufferEntry[] cellBuffer = new PixelBufferEntry[frameBuffer.cells.Length];
        // 初始化像素掩码
        int pixelMask = 0;
        // 初始化读取的像素数组
        int[] read_pixel = new int[4];
        // 初始化像素缓冲区索引
        int pb_idx = -1;

        // 遍历每一帧
        for (int f = 0; f < header.framesPerDir; ++f)
        {
            // 获取当前帧
            Frame frame = dir.frames[f];
            // 计算当前帧的起始单元格X坐标
            int cell0_x = (frame.box.xMin - dir.box.xMin) / 4;
            // 计算当前帧的起始单元格Y坐标
            int cell0_y = (frame.box.yMin - dir.box.yMin) / 4;
            // 创建帧单元格
            CreateFrameCells(dir.box, frame);
            // 遍历每一行单元格
            for (int y = 0; y < frame.nb_cell_h; y++)
            {
                // 计算当前单元格的Y坐标
                int curr_cell_y = cell0_y + y;
                // 遍历每一列单元格
                for (int x = 0; x < frame.nb_cell_w; x++)
                {
                    // 计算当前单元格的X坐标
                    int curr_cell_x = cell0_x + x;
                    // 计算当前单元格的索引
                    int curr_cell = curr_cell_x + (curr_cell_y * frameBuffer.nb_cell_w);
                    // 初始化是否跳过当前单元格的标志
                    bool nextCell = false;
                    // 如果当前单元格的像素值不为空
                    if (cellBuffer[curr_cell].val != null)
                    {
                        // 初始化临时变量
                        int tmp = 0;
                        // 如果相等单元格流不为空，读取一个位
                        if (streams.equalCell != null)
                            tmp = streams.equalCell.ReadBit();

                        // 如果临时变量为0，读取像素掩码
                        if (tmp == 0)
                            pixelMask = streams.pixelMask.ReadBits(4);
                        // 否则，跳过当前单元格
                        else
                            nextCell = true;
                    }
                    // 否则，设置像素掩码为0x0f
                    else
                        pixelMask = 0x0f;

                    // 如果不跳过当前单元格
                    if (!nextCell)
                    {
                        // 初始化读取的像素数组
                        read_pixel[0] = read_pixel[1] = read_pixel[2] = read_pixel[3] = 0;
                        // 初始化上一个像素值
                        int last_pixel = 0;
                        // 根据像素掩码获取像素数量
                        int nb_pix = nb_pix_table[pixelMask];
                        // 初始化编码类型
                        int encodingType = 0;
                        // 如果像素数量不为0且编码类型流不为空，读取编码类型
                        if (nb_pix != 0 && streams.encodingType != null)
                        {
                            encodingType = streams.encodingType.ReadBit();
                        }

                        // 初始化解码的像素数量
                        int decoded_pix = 0;
                        // 遍历每个像素
                        for (int i = 0; i < nb_pix; i++)
                        {
                            // 如果编码类型不为0，直接从原始像素流中读取像素值
                            if (encodingType != 0)
                            {
                                read_pixel[i] = streams.rawPixel.ReadBits(8);
                            }
                            else
                            {
                                // 否则，使用上一个像素值作为当前像素值
                                read_pixel[i] = last_pixel;
                                // 读取像素位移
                                int pix_displ = streams.pixelCode.ReadBits(4);
                                // 更新当前像素值
                                read_pixel[i] += pix_displ;
                                // 如果像素位移为15，继续读取并累加
                                while (pix_displ == 15)
                                {
                                    pix_displ = streams.pixelCode.ReadBits(4);
                                    read_pixel[i] += pix_displ;
                                }
                            }

                            // 如果当前像素值与上一个像素值相同，表示结束
                            if (read_pixel[i] == last_pixel)
                            {
                                read_pixel[i] = 0;
                                i = nb_pix;
                            }
                            else
                            {
                                // 否则，更新上一个像素值并增加解码的像素数量
                                last_pixel = read_pixel[i];
                                decoded_pix++;
                            }
                        }

                        // 获取当前单元格的旧像素缓冲区条目
                        PixelBufferEntry old_entry = cellBuffer[curr_cell];
                        // 增加像素缓冲区索引
                        pb_idx++;
                        // 断言像素缓冲区索引不超过最大限制
                        Debug.Assert(pb_idx < DCC_MAX_PB_ENTRY);
                        // 创建新的像素缓冲区条目
                        var newEntry = new PixelBufferEntry();
                        // 初始化新条目的像素值数组
                        newEntry.val = new byte[4];
                        // 设置当前像素索引为解码的像素数量减1
                        int curr_idx = decoded_pix - 1;

                        // 遍历每个像素
                        for (int i = 0; i < 4; i++)
                        {
                            // 如果当前像素在像素掩码中
                            if ((pixelMask & (1 << i)) != 0)
                            {
                                // 如果当前像素索引有效，设置新条目的像素值
                                if (curr_idx >= 0)
                                    newEntry.val[i] = (byte)read_pixel[curr_idx--];
                                // 否则，设置为0
                                else
                                    newEntry.val[i] = 0;
                            }
                            // 否则，使用旧条目的像素值
                            else
                                newEntry.val[i] = old_entry.val[i];
                        }
                        // 设置新条目的帧索引
                        newEntry.frame = f;
                        // 设置新条目的帧单元格索引
                        newEntry.frameCellIndex = x + (y * frame.nb_cell_w);
                        // 将新条目添加到像素缓冲区
                        dir.pixelBuffer[pb_idx] = newEntry;
                        // 更新单元格缓冲区的条目
                        cellBuffer[curr_cell] = newEntry;
                    }
                }
            }
        }

        // 遍历每个像素缓冲区条目
        for (int i = 0; i <= pb_idx; i++)
        {
            // 遍历每个像素值
            for (int x = 0; x < 4; x++)
            {
                // 获取当前像素值
                int y = dir.pixelBuffer[i].val[x];
                // 更新像素值为调色板中的值
                dir.pixelBuffer[i].val[x] = dir.pixel_values[y];
            }
        }

        // 设置像素缓冲区的条目数量
        dir.pb_nb_entry = pb_idx + 1;
    }

    // 创建帧
    static void MakeFrames(Header header, Direction dir, FrameBuffer frameBuffer, Streams streams, List<Texture2D> textures, List<Sprite> sprites)
    {
        // 设置内边距
        const int padding = 2;
        // 计算纹理宽度，为下一个2的幂次方
        int textureWidth = Mathf.NextPowerOfTwo((dir.box.width + padding) * header.framesPerDir);
        // 计算纹理高度，为下一个2的幂次方
        int textureHeight = Mathf.NextPowerOfTwo(dir.box.height + padding);
        // 限制纹理宽度最大为1024
        textureWidth = Mathf.Min(1024, textureWidth);

        // 创建纹理打包器
        var packer = new TexturePacker(textureWidth, textureHeight);
        // 初始化纹理
        Texture2D texture = null;
        // 初始化像素数组
        Color32[] pixels = null;

        // 遍历每一单元格，初始化宽度和高度
        for (int c = 0; c < frameBuffer.cells.Length; c++)
        {
            frameBuffer.cells[c].w = -1;
            frameBuffer.cells[c].h = -1;
        }

        // 初始化像素缓冲区索引
        int pb_idx = 0;

        // 遍历每一帧
        for (int f = 0; f < header.framesPerDir; f++)
        {
            // 获取当前帧
            Frame frame = dir.frames[f];
            // 计算当前帧的单元格数量
            int nb_cell = frame.nb_cell_w * frame.nb_cell_h;

            // 将帧打包到纹理中
            var pack = packer.put(dir.box.width + padding, dir.box.height + padding);
            // 如果需要创建新纹理
            if (pack.newTexture)
            {
                // 如果已有纹理，应用像素数据
                if (texture != null)
                {
                    texture.SetPixels32(pixels);
                    texture.Apply();
                }
                // 创建新纹理
                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
                // 初始化像素数组
                pixels = new Color32[textureWidth * textureHeight];
                // 将纹理添加到纹理列表
                textures.Add(texture);
            }
            // 设置帧的纹理
            frame.texture = texture;
            // 设置帧的像素数组
            frame.texturePixels = pixels;
            // 设置帧的纹理X坐标
            frame.textureX = pack.x;
            // 设置帧的纹理Y坐标
            frame.textureY = pack.y;

            // 创建纹理矩形，用于定义精灵在纹理中的位置和大小
            var textureRect = new Rect(frame.textureX, frame.textureY, dir.box.width, dir.box.height);
            // 计算精灵的枢轴点，通常用于确定精灵的旋转和缩放中心
            var pivot = new Vector2(-dir.box.xMin / (float)dir.box.width, dir.box.yMax / (float)dir.box.height);
            // 创建精灵对象，使用纹理、纹理矩形、枢轴点、像素单位等参数
            Sprite sprite = Sprite.Create(texture, textureRect, pivot, Iso.pixelsPerUnit, extrude: 0, meshType: SpriteMeshType.FullRect);
            // 将创建的精灵添加到精灵列表中
            sprites.Add(sprite);

            // 遍历当前帧的所有单元格
            for (int c = 0; c < nb_cell; c++)
            {
                // 获取当前单元格
                Cell cell = frame.cells[c];

                // 计算当前单元格在缓冲区中的位置
                int cell_x = cell.x0 / 4;
                int cell_y = cell.y0 / 4;
                int cell_idx = cell_x + (cell_y * frameBuffer.nb_cell_w);
                // 获取缓冲区中的对应单元格
                Cell buff_cell = frameBuffer.cells[cell_idx];
                // 获取像素缓冲区条目
                PixelBufferEntry pbe = dir.pixelBuffer[pb_idx];

                // 检查是否为相等单元格
                if ((pbe.frame != f) || (pbe.frameCellIndex != c))
                {
                    // 如果当前缓冲区单元格的相等单元格标志位为1，则需要复制或清除帧单元格

                    // 如果当前单元格的宽度和高度与缓冲区单元格相同
                    if ((cell.w == buff_cell.w) && (cell.h == buff_cell.h))
                    {
                        // 获取参考帧（前一帧）
                        Frame refFrame = dir.frames[f - 1];
                        // 计算源纹理的Y坐标
                        int textureY = refFrame.textureY + dir.box.height - buff_cell.y0;
                        // 计算源纹理的X坐标
                        int textureX = refFrame.textureX + buff_cell.x0;
                        // 计算源纹理的偏移量
                        int srcOffset = refFrame.texture.width * textureY + textureX;
                        // 计算目标纹理的Y坐标
                        textureY = frame.textureY + dir.box.height - cell.y0;
                        // 计算目标纹理的X坐标
                        textureX = frame.textureX + cell.x0;
                        // 计算目标纹理的偏移量
                        int dstOffset = frame.texture.width * textureY + textureX;
                        // 遍历每一行像素，复制像素数据
                        // 遍历每一行像素，复制像素数据
                        for (int y = 0; y < cell.h; y++)
                        {
                            // 从参考帧的纹理像素数组中复制数据到当前帧的纹理像素数组
                            System.Array.Copy(refFrame.texturePixels, srcOffset, frame.texturePixels, dstOffset, cell.w);
                            // 更新源纹理偏移量，减去纹理宽度以移动到上一行
                            srcOffset -= refFrame.texture.width;
                            // 更新目标纹理偏移量，减去纹理宽度以移动到上一行
                            dstOffset -= frame.texture.width;
                        }
                    }
                }
                else
                {
                    // 填充帧单元格的像素数据

                    // 如果像素缓冲区条目的第一个和第二个像素值相同
                    if (pbe.val[0] == pbe.val[1])
                    {
                        // 使用第一个像素值填充整个帧单元格
                        //clear_to_color(cell->bmp, pbe->val[0]);
                    }
                    else
                    {
                        // 初始化读取的位数
                        int nb_bit;
                        // 如果第二个和第三个像素值相同，使用1位
                        if (pbe.val[1] == pbe.val[2])
                            nb_bit = 1;
                        // 否则使用2位
                        else
                            nb_bit = 2;

                        // 填充帧单元格的像素数据
                        // 计算纹理的Y坐标
                        int textureY = frame.textureY + dir.box.height - cell.y0;
                        // 计算纹理的X坐标
                        int textureX = frame.textureX + cell.x0;
                        // 计算纹理的偏移量
                        int offset = frame.texture.width * textureY + textureX;
                        // 遍历每一行像素
                        for (int y = 0; y < cell.h; ++y)
                        {
                            // 遍历每一列像素
                            for (int x = 0; x < cell.w; ++x)
                            {
                                // 从像素编码流中读取像素值
                                int pix = streams.pixelCode.ReadBits(nb_bit);
                                // 从调色板中获取颜色
                                Color32 color = Palette.palette[pbe.val[pix]];
                                // 设置当前像素的颜色
                                frame.texturePixels[offset + x] = color;
                            }
                            // 更新纹理偏移量，减去纹理宽度以移动到上一行
                            offset -= frame.texture.width;
                        }
                    }

                    // 移动到下一个像素缓冲区条目
                    pb_idx++;
                }

                // 对于被当前帧单元格使用的缓冲区单元格，
                // 保存当前帧单元格的宽度和高度
                // （用于后续的相等单元格测试）
                // 并保存其原点，以便在相等单元格时进行复制
                frameBuffer.cells[cell_idx] = cell;
            }
        }

        // 如果纹理不为空
        if (texture != null)
        {
            // 将像素数组应用到纹理
            texture.SetPixels32(pixels);
            // 应用纹理更改
            texture.Apply();
        }
    }

    // 静态字典，用于缓存已加载的DCC文件
    static Dictionary<string, DCC> cache = new Dictionary<string, DCC>();
    // 宽度表，用于处理不同宽度的单元格
    static int[] widthTable = { 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 32 };
    // 像素数量表，用于根据像素掩码获取像素数量
    static int[] nb_pix_table = {0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4};
    // 方向数组，用于处理不同数量的方向
    static int[] dirs1 = new int[] { 0 };
    static int[] dirs4 = new int[] { 0, 1, 2, 3 };
    static int[] dirs8 = new int[] { 4, 0, 5, 1, 6, 2, 7, 3 };
    static int[] dirs16 = new int[] { 4,  8,  0,  9,  5, 10,  1, 11, 6, 12,  2, 13,  7, 14,  3, 15};

    // 加载DCC文件的静态方法
    static public DCC Load(string filename, bool ignoreCache = false)
    {
        // 将文件名转换为小写
        filename = filename.ToLower();
        // 如果忽略缓存且缓存中包含该文件名，则返回缓存的DCC对象
        if (!ignoreCache && cache.ContainsKey(filename))
        {
            return cache[filename];
        }

        // 记录加载日志
        Debug.Log("Loading " + filename);
        // 启动计时器
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 创建新的DCC对象
        DCC dcc = new DCC();
        // 初始化纹理列表
        dcc.textures = new List<Texture2D>();
        // 初始化精灵列表
        dcc.sprites = new List<Sprite>();

        // 读取文件的字节数据
        byte[] bytes = File.ReadAllBytes(filename);
        // 创建内存流
        var stream = new MemoryStream(bytes);
        // 创建二进制读取器
        var reader = new BinaryReader(stream);

        // 创建文件头对象
        Header header = new Header();
        // 读取文件头信息
        ReadHeader(reader, header);

        // 初始化方向数组
        int[] dirs = null;
        // 根据方向数量选择方向数组
        switch(header.directionCount)
        {
            case 1: dirs = dirs1; break;
            case 4: dirs = dirs4; break;
            case 8: dirs = dirs8; break;
            case 16: dirs = dirs16; break;
        }

        // 遍历每个方向
        for (int d = 0; d < header.directionCount; ++d)
        {
            // 创建位读取器，读取当前方向的数据
            var bitReader = new BitReader(bytes, header.dirOffset[dirs[d]] * 8);
            // 创建方向对象
            Direction dir = new Direction();
            // 读取方向信息
            ReadDirection(bitReader, dir);

            // 初始化可选字节总和
            int optionalBytesSum = 0;
            // 初始化帧数组
            dir.frames = new Frame[header.framesPerDir];

            // 遍历每一帧
            for (int f = 0; f < header.framesPerDir; ++f)
            {
                // 创建帧对象
                Frame frame = new Frame();
                // 将帧对象添加到方向帧数组中
                dir.frames[f] = frame;
                // 读取帧信息
                ReadFrame(bitReader, dir, frame);

                // 累加可选字节
                optionalBytesSum += frame.optionalBytes;

                // 如果帧是从下往上绘制的，记录警告并跳过
                if (frame.bottomUp != 0)
                {
                    Debug.LogWarning("自下向上框架绘制失败 (" + filename + ")");
                    continue;
                }

                // 如果是第一帧，设置方向的矩形框
                if (f == 0)
                    dir.box = frame.box;
                else
                {
                    // 更新矩形框的最小X坐标
                    dir.box.xMin = Mathf.Min(dir.box.xMin, frame.box.xMin);
                    // 更新矩形框的最小Y坐标
                    dir.box.yMin = Mathf.Min(dir.box.yMin, frame.box.yMin);
                    // 更新矩形框的最大X坐标
                    dir.box.xMax = Mathf.Max(dir.box.xMax, frame.box.xMax);
                    // 更新矩形框的最大Y坐标
                    dir.box.yMax = Mathf.Max(dir.box.yMax, frame.box.yMax);
                }
            }

            // 如果可选字节总和不为0，记录警告
            if (optionalBytesSum != 0)
                Debug.LogWarning("可选字节总和不为0");
            // 读取可选字节的位数据
            bitReader.ReadBits(optionalBytesSum * 8);

            // 创建流对象
            Streams streams = new Streams();
            // 读取流信息
            ReadStreamsInfo(bitReader, dir, bytes, streams);

            // 创建帧缓冲区
            FrameBuffer frameBuffer = CreateFrameBuffer(dir); // dcc_prepare_buffer_cells
            // 填充像素缓冲区
            FillPixelBuffer(header, frameBuffer, dir, streams); // dcc_fill_pixel_buffer
            // 创建帧
            MakeFrames(header, dir, frameBuffer, streams, dcc.textures, dcc.sprites); // dcc_make_frames
        }

        // 设置DCC对象的方向数量
        dcc.directionCount = header.directionCount;
        // 设置DCC对象的每个方向的帧数
        dcc.framesPerDirection = header.framesPerDir;
        // 如果不忽略缓存，将DCC对象添加到缓存中
        if (!ignoreCache)
            cache.Add(filename, dcc);

        // 记录加载日志，包括加载时间和生成的精灵数量
        Debug.Log("加载时间: " + sw.Elapsed + " (" + dcc.sprites.Count + " 个精灵)");

        // 返回加载的DCC对象
        return dcc;
    }

    // 将DCC文件转换为PNG格式的静态方法
    static public void ConvertToPng(string assetPath)
    {
        // 加载调色板1
        Palette.LoadPalette(1);
        // 加载DCC文件，忽略缓存
        DCC dcc = Load(assetPath, ignoreCache: true);
        // 初始化索引
        int i = 0;
        // 遍历所有纹理
        foreach (var texture in dcc.textures)
        {
            // 将纹理编码为PNG数据
            var pngData = texture.EncodeToPNG();
            // 立即销毁纹理对象，释放内存
            Object.DestroyImmediate(texture);
            // 构建PNG文件路径，使用资产路径和索引作为文件名
            var pngPath = assetPath + "." + i + ".png";
            // 将PNG数据写入文件
            File.WriteAllBytes(pngPath, pngData);
            // 导入PNG文件到资源数据库
            AssetDatabase.ImportAsset(pngPath);
            // 增加索引，用于生成下一个PNG文件名
            ++i;
        }
    }
}