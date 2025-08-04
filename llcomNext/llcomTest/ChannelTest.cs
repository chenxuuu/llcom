namespace llcomTest;

[TestClass]
public class ChannelTest
{
    [TestMethod]
    public void TestBaseChannelAdd()
    {
        var channel = new LLCOM.Models.Channels.BaseChannel(10);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = new Span<byte>(data);

        // Test adding data to the channel
        int addedLength = channel.AddData(span);
        Assert.AreEqual(5, addedLength, "Should add all 5 bytes to the buffer");

        // Test adding more data than the buffer can hold
        var moreData = new byte[] { 6, 7, 8, 9, 10, 11 };
        var moreSpan = new Span<byte>(moreData);
        addedLength = channel.AddData(moreSpan);
        Assert.AreEqual(5, addedLength, "Should only add up to the buffer size limit");
    }
    
    [TestMethod]
    public void TestBaseChannelGetData()
    {
        var channel = new LLCOM.Models.Channels.BaseChannel(10);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        channel.AddData(new Span<byte>(data));

        // Test getting data from the channel
        var retrievedData = channel.GetData();
        CollectionAssert.AreEqual(data, retrievedData, "Retrieved data should match the added data");

        // Test getting more data than available
        var emptyData = channel.GetData(20);
        Assert.AreEqual(0, emptyData.Length, "Should return an empty array when no data is available");
    }
    
    [TestMethod]
    public void TestBaseChannelUtf8CharacterHandling()
    {
        var channel = new LLCOM.Models.Channels.BaseChannel();
        var utf8Data = "你好"u8.ToArray().Take(5).ToList();//5
        utf8Data.AddRange("你好"u8.ToArray());//6
        utf8Data.AddRange("你好"u8.ToArray().Take(5).ToList());//3
        channel.AddData(new Span<byte>(utf8Data.ToArray()));

        // Test getting data with UTF-8 character handling
        var retrievedData = channel.GetData(null, true);
        Assert.AreEqual(14, retrievedData.Length, "Should retrieve the complete UTF-8 character");
    }
}