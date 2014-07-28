using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoFuncLit : GoExpr
	{
		public GoFuncType Type { get; set; }

		public GoBlockStmt Body { get; set; }

		public override int Pos
		{
			get
			{
				return this.Type.Pos;
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
