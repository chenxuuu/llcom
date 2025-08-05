using System;
using System.IO;
using System.Threading.Tasks;
using LiteDB;

namespace LLCOM.Services;

public class Database : IDisposable
{
    private class Setting
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
    
    private readonly LiteDatabase? _db = null;
    private readonly ILiteCollection<Setting>? _collection = null;
    
    /// <summary>
    /// 初始化全局设置的数据库
    /// </summary>
    public Database(string dbFileName)
    {
        if(_db is not null)
            return;
        // Initialize the global setting
        // 指定路径的数据库
        var dbPath = Path.Combine(Utils.AppPath, dbFileName);
        _db = new LiteDatabase(dbPath);
        _collection = _db.GetCollection<Setting>("settings");
    }
    
    private async Task _update(string key, string value)
    {
        if (_db is null || _collection is null)
            throw new Exception("GlobalSetting not initialized");
        await Task.Run(() =>
        {
            var v = _collection?.FindOne(x => x.Key == key);
            if (v is null)
            {
                // Insert new setting
                v = new Setting { Key = key, Value = value };
                _collection?.Insert(v);
            }
            else
            {
                // Update existing setting
                v.Value = value;
                _collection?.Update(v);
            }
            _db.Commit();
        });
    }
    
    private async Task<string?> _get(string key)
    {
        if (_db is null || _collection is null)
            throw new Exception("GlobalSetting not initialized");
        string? value = null;
        await Task.Run(() =>
        {
            var v = _collection?.FindOne(x => x.Key == key);
            value = v?.Value;
        });
        return value;
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
            _db.Dispose();
    }
}