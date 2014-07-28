using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public abstract class GoNode
	{
		public abstract int Pos { get; }

		public abstract int End { get; }
	}
}
