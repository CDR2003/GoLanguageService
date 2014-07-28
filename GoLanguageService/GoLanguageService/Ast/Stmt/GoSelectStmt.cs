using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoSelectStmt : GoStmt
	{
		public int Select { get; set; }

		public GoBlockStmt Body { get; set; }

		public override int Pos
		{
			get
			{
				return this.Select;
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
