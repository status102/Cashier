using Cashier.Commons;
using Cashier.Windows.Tab;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Cashier.Windows;
public unsafe sealed class Main : IWindow
{
    private readonly Cashier _cashier;
    private readonly static Vector2 Window_Size = new(720, 640);
    private readonly List<ITabPage> TabList = [];
    public T? Get<T>() where T : ITabPage => TabList.OfType<T>().FirstOrDefault();

    private bool _visible;
    public Main(Cashier cashier)
    {
        _cashier = cashier;
        Assembly.GetAssembly(typeof(ITabPage))?.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Contains(typeof(ITabPage)))
            .ToList()
            .ForEach(x =>
            {
                var obj = (ITabPage?)Activator.CreateInstance(x, cashier);
                if (obj is null) {
                    Svc.PluginLog.Error($"Failed to create instance of {x.Name}");
                } else {
                    TabList.Add(obj);
                }
            });
    }

    public void Dispose()
    {
        foreach (var item in TabList) {
            item.Dispose();
        }
    }

    public void Show()
    {
        _visible = !_visible;
        if (_visible) {
            foreach (var item in TabList) {
                item.Show();
            }
        }
    }

    public void Draw()
    {
        if (!_visible) {
            return;
        }
        ImGui.SetNextWindowSize(Window_Size, ImGuiCond.Once);
        if (ImGui.Begin($"{Cashier.Name} By Status102##{Cashier.Name}Main", ref _visible)) {
            if (ImGui.BeginTabBar("MainTabBar", ImGuiTabBarFlags.FittingPolicyScroll)) {
                int i = 0;
#if DEBUG
                foreach (var item in TabList) {
#else
                foreach (var item in TabList.Where(i => !i.Hide)) {
#endif
                    if (ImGui.BeginTabItem(item.TabName + $"##{i}")) {
                        if (ImGui.BeginChild(item.TabName)) {
                            item.Draw();
                            ImGui.EndChild();
                        }
                        ImGui.EndTabItem();
                    }
                    i++;
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }
    }

}