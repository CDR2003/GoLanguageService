using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoFuncDecl : GoDecl
	{
		public GoCommentGroup Doc { get; set; }

		public GoFieldList Recv { get; set; }

		public GoIdent Name { get; set; }

		public GoFuncType Type { get; set; }

		public GoBlockStmt Body { get; set; }

		public override int Pos
		{
			get
			{
				return this.Type.Pos;
			}
		}

		public override int End
		{
			get
			{
				if( this.Body != null )
				{
					return this.Body.End;
				}
				return this.Type.End;
			}
		}
	}
}
