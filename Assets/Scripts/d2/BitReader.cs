
/// <summary>
/// 比特流
/// </summary>
/// <remarks>
/// 自定义流, 用于以比特为单位读取流; 
/// 它有两个索引, 一个是字节索引, 一个是比特索引;
/// 字节索引从文件头开始, 比特索引从当前字节的第一个比特开始;
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
    /// 构造函数
    /// </summary>
    /// <param name="bytes">赋值给流的字节数据</param>
    /// <param name="offset">流在文件中的起始位</param>
    /// <remarks>
    /// 给BitReader赋值,形参1: 文件的字节数组, 形参2: 流在文件中的起始位
    /// </remarks>
    public BitReader(byte[] bytes, long offset = 0)
    {
        //赋值字节数据
        this.bytes = bytes;
        //字节索引
        byteIndex = (int)offset / 8;
        //比特索引
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
