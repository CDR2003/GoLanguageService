using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoError
	{
		public GoSourcePosition Pos;

		public string Msg;

		public GoError( GoSourcePosition pos, string msg )
		{
			this.Pos = pos;
			this.Msg = msg;
		}

		public override string ToString()
		{
			if( this.Pos.Filename != "" || this.Pos.IsValid )
			{
				return this.Pos.ToString() + ": " + this.Msg;
			}
			return this.Msg;
		}
	}
}
