using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	[Flags]
	public enum GoChanDir
	{
		Send = 1,
		Recv = 2,
	}
}
