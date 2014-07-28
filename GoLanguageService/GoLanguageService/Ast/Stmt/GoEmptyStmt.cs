using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoEmptyStmt : GoStmt
	{
		public int Semicolon { get; set; }

		public override int Pos
		{
			get
			{
				return this.Semicolon;
			}
		}

		public override int End
		{
			get
			{
				return this.Semicolon + 1;
			}
		}
	}
}
