namespace Cashier.Windows.Tab;
internal interface ITabPage : IWindow
{
    public string TabName { get; }
    public void Show();
}

