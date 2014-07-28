using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoArrayType : GoExpr
	{
		public int Lbrack { get; set; }

		public GoExpr Len { get; set; }

		public GoExpr Elt { get; set; }

		public override int Pos
		{
			get
			{
				return this.Lbrack;
			}
		}

		public override int End
		{
			get
			{
				return this.Elt.End;
			}
		}
	}
}
