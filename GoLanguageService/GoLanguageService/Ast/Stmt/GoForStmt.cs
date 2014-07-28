using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoForStmt : GoStmt
	{
		public int For { get; set; }

		public GoStmt Init { get; set; }

		public GoExpr Cond { get; set; }

		public GoStmt Post { get; set; }

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
