using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoReturnStmt : GoStmt
	{
		public int Return { get; set; }

		public List<GoExpr> Results { get; set; }

		public override int Pos
		{
			get
			{
				return this.Return;
			}
		}

		public override int End
		{
			get
			{
				if( this.Results.Count > 0 )
				{
					return this.Results.Last().End;
				}
				return this.Return + 6;
			}
		}
	}
}
