using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoLabeledStmt : GoStmt
	{
		public GoIdent Label { get; set; }

		public int Colon { get; set; }

		public GoStmt Stmt { get; set; }

		public override int Pos
		{
			get
			{
				return this.Label.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Stmt.End;
			}
		}
	}
}
