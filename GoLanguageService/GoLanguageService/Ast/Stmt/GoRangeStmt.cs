using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoRangeStmt : GoStmt
	{
		public int For { get; set; }

		public GoExpr Key { get; set; }

		public GoExpr Value { get; set; }

		public int TokPos { get; set; }

		public GoTokenID Tok { get; set; }

		public GoExpr X { get; set; }

		public GoBlockStmt Body { get; set; }

		public override int Pos
		{
			get
			{
				return this.For;
			}
		}

		public override int End
		{
			get
			{
				return this.Body.End;
			}
		}
	}
}
