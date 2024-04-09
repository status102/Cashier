using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashier.Commons
{
	public class Hook : IDisposable
	{
		public Hook(Cashier cashier)
		{
			Svc.GameInteropProvider.InitializeFromAttributes(this);
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
