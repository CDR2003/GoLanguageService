using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoUnaryExpr : GoExpr
	{
		public int OpPos { get; set; }

		public GoTokenID Op { get; set; }

		public GoExpr X { get; set; }

		public override int Pos
		{
			get
			{
				return this.OpPos;
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
