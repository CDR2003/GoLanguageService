using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoDeferStmt : GoStmt
	{
		public int Defer { get; set; }

		public GoCallExpr Call { get; set; }

		public override int Pos
		{
			get
			{
				return this.Defer;
			}
		}

		public override int End
		{
			get
			{
				return this.Call.End;
			}
		}
	}
}
