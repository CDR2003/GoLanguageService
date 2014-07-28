using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoBasicLit : GoExpr
	{
		public int ValuePos { get; set; }

		public GoTokenID Kind { get; set; }

		public string Value { get; set; }

		public override int Pos
		{
			get
			{
				return this.ValuePos;
			}
		}

		public override int End
		{
			get
			{
				return this.ValuePos + this.Value.Length;
			}
		}
	}
}
