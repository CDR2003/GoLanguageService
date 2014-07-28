using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoCompositeLit : GoExpr
	{
		public GoExpr Type { get; set; }

		public int Lbrace { get; set; }

		public List<GoExpr> Elts { get; set; }

		public int Rbrace { get; set; }

		public override int Pos
		{
			get
			{
				if( this.Type != null )
				{
					return this.Type.Pos;
				}
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
