using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoCaseClause : GoStmt
	{
		public int Case { get; set; }

		public List<GoExpr> List { get; set; }

		public int Colon { get; set; }

		public List<GoStmt> Body { get; set; }

		public override int Pos
		{
			get
			{
				return this.Case;
			}
		}

		public override int End
		{
			get
			{
				if( this.Body.Count > 0 )
				{
					return this.Body.Last().End;
				}
				return this.Colon + 1;
			}
		}
	}
}
