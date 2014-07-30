using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public struct GoSourcePosition
	{
		public string Filename;

		public int Offset;

		public int Line;

		public int Column;

		public bool IsValid
		{
			get
			{
				return this.Line > 0;
			}
		}

		public override string ToString()
		{
			var s = this.Filename;
			if( this.IsValid )
			{
				if( s != "" )
				{
					s += ":";
				}
				s += this.Line + ":" + this.Column;
			}
			if( s == "" )
			{
				s = "-";
			}
			return s;
		}
	}
}
