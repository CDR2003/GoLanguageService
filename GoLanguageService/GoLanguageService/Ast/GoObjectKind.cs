using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public enum GoObjectKind
	{
		Bad,
		Pkg,
		Con,
		Typ,
		Var,
		Fun,
		Lbl,
	}
}
