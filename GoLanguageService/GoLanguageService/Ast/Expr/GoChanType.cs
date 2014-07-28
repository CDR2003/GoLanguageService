using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoChanType : GoExpr
	{
		public int Begin { get; set; }

		public int Arrow { get; set; }

		public GoChanDir Dir { get; set; }

		public GoExpr Value { get; set; }

		public override int Pos
		{
			get
			{
				return this.Begin;
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
