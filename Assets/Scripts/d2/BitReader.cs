
/// <summary>
/// 比特流
/// </summary>
/// <remarks>
/// [自定义流]按比特读取数据
/// </remarks>
public class BitReader
{
    /// <summary>
    /// 做为流的字节数据
    /// </summary>
    private byte[] bytes;
    /// <summary>
    /// 字节索引
    /// </summary>
    private int byteIndex = 0;
    /// <summary>
    /// 当前字节
    /// </summary>
    private int currentByte;
    /// <summary>
    /// 比特索引
    /// </summary>
    public int bitIndex = 8;

    /// <summary>
    /// 比特流构造函数
    /// </summary>
    /// <param name="bytes">文件数据</param>
    /// <param name="offset">流在文件中的起始位(默认从0开始)</param>
    /// <remarks>
    /// <para>创建成比特流</para>
    /// <para>形参1: 文件的数据(字节数组)</para>
    /// <para>形参2: 流在文件中的起始位</para>
    /// </remarks>
    public BitReader(byte[] bytes, long offset = 0)
    {
        //赋值文件数据
        this.bytes = bytes;
        //赋值字节索引
        byteIndex = (int)offset / 8;
        //赋值比特索引
        bitIndex = (int)(offset % 8);
        //当前字节
        currentByte = bytes[byteIndex++];
    }
    /// <summary>
    /// 读取一个比特
    /// </summary>
    /// <returns></returns>
    public int ReadBit()
    {
        if (bitIndex >= 8)
        {
            currentByte = bytes[byteIndex++];
            bitIndex = 0;
        }
        int result = (currentByte >> bitIndex) & 1;
        ++bitIndex;
        return result;
    }
    /// <summary>
    /// 读取多个比特
    /// </summary>
    /// <param name="count">读取的比特数量</param>
    /// <returns></returns>
    public int ReadBits(int count)
    {
        int result = 0;
        for (int i = 0; i < count; ++i)
        {
            result += ReadBit() << i;
        }
        return result;
    }

    /// <summary>
    /// 读带符号的比特
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public int ReadSigned(int count)
    {
        int result = ReadBits(count);
        if ((result & (1 << (count - 1))) != 0)
        {
            // negative : extend its sign
            result |= ~((1 << count) - 1);
        }
        return result;
    }

    /// <summary>
    /// 重置比特索引, 回到字节索引的起始位置
    /// </summary>
    public void Reset()
    {
        bitIndex = 8;
    }

    /// <summary>
    /// 计算当前字节剩余的比特数量
    /// </summary>
    public int bitsLeft
    {
        get { return 8 - bitIndex; }
    }

    /// <summary>
    /// 获取当前比特流的读取位置(以比特为单位)
    /// </summary>
    public long offset
    {
        get { return byteIndex * 8 - bitsLeft; }
    }
}
