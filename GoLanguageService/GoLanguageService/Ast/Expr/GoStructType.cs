using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoStructType : GoExpr
	{
		public int Struct { get; set; }

		public GoFieldList Fields { get; set; }

		public bool Incomplete { get; set; }

		public override int Pos
		{
			get
			{
				return this.Struct;
			}
		}

		public override int End
		{
			get
			{
				return this.Fields.End;
			}
		}
	}
}
