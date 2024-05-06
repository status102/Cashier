using Cashier.Windows.Tab;
using ImGuiNET;
using System.Numerics;

namespace Cashier.Windows;
public unsafe sealed class Main : IWindow
{
    private readonly Cashier _cashier;
    private readonly static Vector2 Window_Size = new(720, 640);
    private readonly ITabPage[] TabList;
    public SendMoney SendMoney { get; private init; }

    private bool _visible;
    public Main(Cashier cashier)
    {
        _cashier = cashier;
        SendMoney = new(cashier);
        TabList = [SendMoney];
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
                foreach (var item in TabList) {
                    if (ImGui.BeginTabItem(item.TabName + i)) {
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