using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoCallExpr : GoExpr
	{
		public GoExpr Fun { get; set; }

		public int Lparen { get; set; }

		public List<GoExpr> Args { get; set; }

		public int Ellipsis { get; set; }

		public int Rparen { get; set; }

		public override int Pos
		{
			get
			{
				return this.Fun.Pos;
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
