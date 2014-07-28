using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoStarExpr : GoExpr
	{
		public int Star { get; set; }

		public GoExpr X { get; set; }

		public override int Pos
		{
			get
			{
				return this.Star;
			}
		}

		public override int End
		{
			get
			{
				return this.X.End;
			}
		}
	}
}
