using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoAssignStmt : GoStmt
	{
		public List<GoExpr> Lhs { get; set; }

		public int TokPos { get; set; }

		public GoTokenID Tok { get; set; }

		public List<GoExpr> Rhs { get; set; }

		public override int Pos
		{
			get
			{
				return this.Lhs.First().Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Rhs.Last().End;
			}
		}
	}
}
