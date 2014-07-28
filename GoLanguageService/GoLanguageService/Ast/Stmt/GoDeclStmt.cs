using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoDeclStmt : GoStmt
	{
		public GoDecl Decl { get; set; }

		public override int Pos
		{
			get
			{
				return this.Decl.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Decl.End;
			}
		}
	}
}
