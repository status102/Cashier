namespace Cashier.Windows.Tab;
public interface ITabPage : IWindow
{
    public string TabName { get; }
    public void Show();
}

