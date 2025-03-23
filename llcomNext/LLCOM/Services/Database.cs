using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;

namespace LLCOM.Services;

public class Database : IDisposable
{
    private class Setting
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }
    
    private readonly SQLiteAsyncConnection? _db = null;
    
    /// <summary>
    /// 初始化全局设置的数据库
    /// </summary>
    public Database(string dbFileName)
    {
        if(_db is not null)
            return;
        // Initialize the global setting
        // 指定路径的sqlite数据库
        var dbPath = Path.Combine(Utils.AppPath, dbFileName);
        //检查数据库是否存在
        if (!File.Exists(dbPath))
        {
            //创建数据库
            using var dbNew = new SQLiteConnection(dbPath);
            //创建表
            dbNew.CreateTable<Setting>();
        }
        //创建数据库连接
        _db = new SQLiteAsyncConnection(dbPath);
    }
    
    private async Task _update(string key, string value)
    {
        if (_db is null)
            throw new Exception("GlobalSetting not initialized");
        await _db.InsertOrReplaceAsync(new Setting
        {
            Key = key,
            Value = value
        });
    }
    
    private async Task<string?> _get(string key)
    {
        if (_db is null)
            throw new Exception("GlobalSetting not initialized");
        var setting = await _db.Table<Setting>().Where(s => s.Key == key).FirstOrDefaultAsync();
        return setting?.Value;
    }
    
    public async Task Set<T>(string key, T value) => await _update(key, value!.ToString()!);
    
    public async Task<string> Get(string key, string defaultValue = "") => await _get(key) ?? defaultValue;
    public async Task<int> Get(string key, int defaultValue = 0) => int.TryParse(await _get(key), out var result) ? result : defaultValue;
    public async Task<bool> Get(string key, bool defaultValue = false) => bool.TryParse(await _get(key), out var result) ? result : defaultValue;
    public async Task<bool?> Get(string key, bool? defaultValue = null) => bool.TryParse(await _get(key), out var result) ? result : defaultValue;
    public async Task<double> Get(string key, double defaultValue = 0) => double.TryParse(await _get(key), out var result) ? result : defaultValue;
    public async Task<long> Get(string key, long defaultValue = 0) => long.TryParse(await _get(key), out var result) ? result : defaultValue;

    public void Dispose()
    {
        if (_db is not null)
            Task.Run(async() => await _db.CloseAsync());
    }
}