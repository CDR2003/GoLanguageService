using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoKeyValueExpr : GoExpr
	{
		public GoExpr Key { get; set; }

		public int Colon { get; set; }

		public GoExpr Value { get; set; }

		public override int Pos
		{
			get
			{
				return this.Key.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Value.End;
			}
		}
	}
}
