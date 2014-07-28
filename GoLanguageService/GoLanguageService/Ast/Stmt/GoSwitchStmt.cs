using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoSwitchStmt : GoStmt
	{
		public int Switch { get; set; }

		public GoStmt Init { get; set; }

		public GoExpr Tag { get; set; }

		public GoBlockStmt Body { get; set; }

		public override int Pos
		{
			get
			{
				return this.Switch;
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
