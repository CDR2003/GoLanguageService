using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoFile : GoNode
	{
		public GoCommentGroup Doc { get; set; }

		public int Package { get; set; }

		public GoIdent Name { get; set; }

		public List<GoDecl> Declarations { get; set; }

		public GoScope Scope { get; set; }

		public List<GoImportSpec> Imports { get; set; }

		public List<GoIdent> Unresolved { get; set; }

		public List<GoCommentGroup> Comments { get; set; }

		public override int Pos
		{
			get
			{
				return this.Package;
			}
		}

		public override int End
		{
			get
			{
				if( this.Declarations.Count > 0 )
				{
					return this.Declarations.Last().End;
				}
				return this.Name.End;
			}
		}
	}
}
