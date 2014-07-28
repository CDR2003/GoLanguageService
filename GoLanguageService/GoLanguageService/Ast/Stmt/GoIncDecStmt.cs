using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoIncDecStmt : GoStmt
	{
		public GoExpr X { get; set; }

		public int TokPos { get; set; }

		public GoTokenID Tok { get; set; }

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
				return this.TokPos + 2;
			}
		}
	}
}
