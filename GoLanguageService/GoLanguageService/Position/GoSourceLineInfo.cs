using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoSourceLineInfo
	{
		public int Offset;

		public string Filename;

		public int Line;

		public GoSourceLineInfo()
			: this( 0, "", 0 )
		{
		}

		public GoSourceLineInfo( int offset, string filename, int line )
		{
			this.Offset = offset;
			this.Filename = filename;
			this.Line = line;
		}
	}
}
