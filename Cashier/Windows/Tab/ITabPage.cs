﻿using Cashier.Commons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cashier.Windows.Tab;
public interface ITabPage : IWindow
{
    public string TabName { get; }
    public bool Hide { get; }

    public abstract void Show();
}

public abstract class TabConfigBase
{
    public TabConfig Config { get; set; }
}

[Serializable]
public class TabConfig : INotifyPropertyChanged
{
    #region Init and Save
    [NonSerialized]
    private string _path = string.Empty;
    //private DalamudPluginInterface? pluginInterface;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static T LoadConfig<T>(string path, string pluginName) where T : TabConfig
    {
        var _path = Path.Join(path, "TabConfig", $"{pluginName}.json");
        string configStr = "{}";

        try {
            configStr = File.ReadAllText(_path);
        } catch (FileNotFoundException) {
        } catch (DirectoryNotFoundException) {
        } catch (Exception e) {
            Svc.PluginLog.Warning(e.ToString());
        }

        var config = JsonConvert.DeserializeObject<T>(configStr);
        config ??= (T)new TabConfig();
        config._path = _path;
        config.PropertyChanged += config.Configuration_PropertyChanged;

        return config;
    }

    protected void SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value)) {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private void Configuration_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Save();
    }

    public void Save()
    {
        try {
            if (!Directory.Exists(Path.GetDirectoryName(_path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            }
            using FileStream stream = File.Open(_path, FileMode.OpenOrCreate);
            StreamWriter writer = new(stream);
            writer.WriteLine(JsonConvert.SerializeObject(this));
            writer.Flush();
        } catch (Exception e) {
            Svc.PluginLog.Warning($"保存配置失败\npath={_path}\n" + e.ToString());
        }
    }
    #endregion
}

