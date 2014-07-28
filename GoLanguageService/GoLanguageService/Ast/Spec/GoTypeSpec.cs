using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoTypeSpec : GoSpec
	{
		public GoCommentGroup Doc { get; set; }

		public GoIdent Name { get; set; }

		public GoExpr Type { get; set; }

		public GoCommentGroup Comment { get; set; }

		public override int Pos
		{
			get
			{
				return this.Name.Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Type.End;
			}
		}
	}
}
