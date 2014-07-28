using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoIfStmt : GoStmt
	{
		public int If { get; set; }

		public GoStmt Init { get; set; }

		public GoExpr Cond { get; set; }

		public GoBlockStmt Body { get; set; }

		public GoStmt Else { get; set; }

		public override int Pos
		{
			get
			{
				return this.If;
			}
		}

		public override int End
		{
			get
			{
				if( this.Else != null )
				{
					return this.Else.End;
				}
				return this.Body.End;
			}
		}
	}
}
