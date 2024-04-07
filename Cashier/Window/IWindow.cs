using System;

namespace TradeRecorder.Window
{
	public interface IWindow : IDisposable
	{
		public void Draw();
	}
}
