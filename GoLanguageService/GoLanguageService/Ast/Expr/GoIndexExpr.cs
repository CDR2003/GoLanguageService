using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoIndexExpr : GoExpr
	{
		public GoExpr X { get; set; }

		public int Lbrack { get; set; }

		public GoExpr Index { get; set; }

		public int Rbrack { get; set; }

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
				return this.Rbrack + 1;
			}
		}
	}
}
