using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoBlockStmt : GoStmt
	{
		public int Lbrace { get; set; }

		public List<GoStmt> List { get; set; }

		public int Rbrace { get; set; }

		public override int Pos
		{
			get
			{
				return this.Lbrace;
			}
		}

		public override int End
		{
			get
			{
				return this.Rbrace + 1;
			}
		}
	}
}
