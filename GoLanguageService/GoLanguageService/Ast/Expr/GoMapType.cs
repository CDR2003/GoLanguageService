using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoMapType : GoExpr
	{
		public int Map { get; set; }

		public GoExpr Key { get; set; }

		public GoExpr Value { get; set; }

		public override int Pos
		{
			get
			{
				return this.Map;
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
