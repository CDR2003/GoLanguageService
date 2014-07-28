using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoIdentifier : GoNode
	{
		public int NamePos { get; set; }

		public string Name { get; set; }

		public GoObject Obj { get; set; }

		public override int Pos
		{
			get
			{
				return this.NamePos;
			}
		}

		public override int End
		{
			get
			{
				return this.NamePos + this.Name.Length;
			}
		}
	}
}
