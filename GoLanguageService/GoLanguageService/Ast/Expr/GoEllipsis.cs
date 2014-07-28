using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoEllipsis : GoExpr
	{
		public int Ellipsis { get; set; }

		public GoExpr Elt { get; set; }

		public override int Pos
		{
			get
			{
				return this.Ellipsis;
			}
		}

		public override int End
		{
			get
			{
				if( this.Elt != null )
				{
					return this.Elt.End;
				}
				return this.Ellipsis + 3;
			}
		}
	}
}
