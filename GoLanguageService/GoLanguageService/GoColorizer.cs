using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoColorizer : Colorizer
	{
		public GoColorizer( LanguageService service, IVsTextLines textLines, IScanner scanner )
			: base( service, textLines, scanner )
		{
		}
	}
}
