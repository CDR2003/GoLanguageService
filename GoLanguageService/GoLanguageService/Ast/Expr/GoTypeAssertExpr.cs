using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoTypeAssertExpr : GoExpr
	{
		public GoExpr X { get; set; }

		public int Lparen { get; set; }

		public GoExpr Type { get; set; }

		public int Rparen { get; set; }

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
				return this.Rparen + 1;
			}
		}
	}
}
