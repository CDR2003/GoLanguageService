using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoFuncType : GoExpr
	{
		public int Func { get; set; }

		public GoFieldList Params { get; set; }

		public GoFieldList Results { get; set; }

		public override int Pos
		{
			get
			{
				if( this.Func != 0 || this.Params == null )
				{
					return this.Func;
				}
				return this.Params.Pos;
			}
		}

		public override int End
		{
			get
			{
				if( this.Results != null )
				{
					return this.Results.End;
				}
				return this.Params.End;
			}
		}
	}
}
