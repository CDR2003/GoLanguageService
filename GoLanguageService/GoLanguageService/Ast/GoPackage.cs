using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoPackage : GoNode
	{
		public string Name { get; set; }

		public GoScope Scope { get; set; }

		public Dictionary<string, GoObject> Imports { get; set; }

		public Dictionary<string, GoFile> Files { get; set; }

		public override int Pos
		{
			get
			{
				return 0;
			}
		}

		public override int End
		{
			get
			{
				return 0;
			}
		}
	}
}
