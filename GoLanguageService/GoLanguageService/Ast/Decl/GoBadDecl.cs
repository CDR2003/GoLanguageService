using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoBadDecl : GoDecl
	{
		public int From { get; set; }

		public int To { get; set; }

		public override int Pos
		{
			get
			{
				return this.From;
			}
		}

		public override int End
		{
			get
			{
				return this.To;
			}
		}
	}
}
