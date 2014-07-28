using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoBinaryExpr : GoExpr
	{
		public GoExpr X { get; set; }

		public int OpPos { get; set; }

		public GoTokenID Op { get; set; }

		public GoExpr Y { get; set; }

		public override int Pos
		{
			get
			{
				return this.X.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Y.End;
			}
		}
	}
}
