﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoSelectorExpr : GoExpr
	{
		public GoExpr X { get; set; }

		public GoIdent Sel { get; set; }

		public override int Pos
		{
			get
			{
				return this.X.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Sel.End;
			}
		}
	}
}
