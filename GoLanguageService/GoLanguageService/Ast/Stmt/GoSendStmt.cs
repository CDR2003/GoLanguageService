using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoSendStmt : GoStmt
	{
		public GoExpr Chan { get; set; }

		public int Arrow { get; set; }

		public GoExpr Value { get; set; }

		public override int Pos
		{
			get
			{
				return this.Chan.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Value.End;
			}
		}
	}
}
