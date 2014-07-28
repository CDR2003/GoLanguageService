using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoInterfaceType : GoExpr
	{
		public int Interface { get; set; }

		public GoFieldList Methods { get; set; }

		public bool Incomplete { get; set; }

		public override int Pos
		{
			get
			{
				return this.Interface;
			}
		}

		public override int End
		{
			get
			{
				return this.Methods.End;
			}
		}
	}
}
