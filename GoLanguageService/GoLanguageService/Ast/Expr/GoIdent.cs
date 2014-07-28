using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoIdent : GoNode
	{
		public int NamePos { get; set; }

		public string Name { get; set; }

		public GoObject Obj { get; set; }

		public GoIdent( string name )
		{
			this.NamePos = 0;
			this.Name = name;
			this.Obj = null;
		}

		public static bool IsExported( string name )
		{
			return char.IsUpper( name[0] );
		}

		public bool Exported
		{
			get
			{
				return IsExported( this.Name );
			}
		}

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

		public override string ToString()
		{
			return this.Name;
		}
	}
}
