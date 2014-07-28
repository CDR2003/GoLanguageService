using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoParenExpr : GoExpr
	{
		public int Lparen { get; set; }

		public GoExpr X { get; set; }

		public int Rparen { get; set; }

		public override int Pos
		{
			get
			{
				return this.Lparen;
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
