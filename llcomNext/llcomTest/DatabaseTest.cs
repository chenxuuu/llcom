using LLCOM.Services;

namespace llcomTest;

[TestClass]
public class DatabaseTest
{
    [TestMethod]
    public void InitializeTest()
    {
        using var db = new Database("test.db");
    }
    
    [TestMethod]
    public async Task UpdateTest()
    {
        using var db = new Database("test.db");
        await db.Set("test", "test");
    }
    
    [TestMethod]
    public async Task GetTest()
    {
        using var db = new Database("test.db");
        await db.Set("GetTest","test123");
        var value = await db.Get("GetTest","default");
        Assert.AreEqual("test123", value);
    }
    
    [TestMethod]
    public async Task GetDefaultTest()
    {
        using var db = new Database("test.db");
        var value = await db.Get("GetDefaultTest","default");
        Assert.AreEqual("default", value);
    }
}