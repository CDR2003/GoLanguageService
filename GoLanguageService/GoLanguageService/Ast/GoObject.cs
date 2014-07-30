using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoObject
	{
		public GoObjectKind Kind { get; set; }

		public string Name { get; set; }

		public object Decl { get; set; }

		public object Data { get; set; }

		public object Type { get; set; }

		public GoObject()
			: this( GoObjectKind.Bad, "" )
		{
		}

		public GoObject( GoObjectKind kind, string name )
		{
			this.Kind = kind;
			this.Name = name;
		}

		public int Pos
		{
			get
			{
				var name = this.Name;
				var type = this.Decl.GetType();
				if( type == typeof( GoField ) )
				{
					var d = this.Decl as GoField;
					foreach( var n in d.Names )
					{
						if( n.Name == name )
						{
							return n.Pos;
						}
					}
				}
				else if( type == typeof( GoImportSpec ) )
				{
					var d = this.Decl as GoImportSpec;
					if( d.Name != null && d.Name.Name == name )
					{
						return d.Name.Pos;
					}
					return d.Path.Pos;
				}
				else if( type == typeof( GoValueSpec ) )
				{
					var d = this.Decl as GoValueSpec;
					foreach( var n in d.Names )
					{
						if( n.Name == name )
						{
							return n.Pos;
						}
					}
				}
				else if( type == typeof( GoTypeSpec ) )
				{
					var d = this.Decl as GoTypeSpec;
					if( d.Name.Name == name )
					{
						return d.Name.Pos;
					}
				}
				else if( type == typeof( GoFuncDecl ) )
				{
					var d = this.Decl as GoFuncDecl;
					if( d.Name.Name == name )
					{
						return d.Name.Pos;
					}
				}
				else if( type == typeof( GoLabeledStmt ) )
				{
					var d = this.Decl as GoLabeledStmt;
					if( d.Label.Name == name )
					{
						return d.Label.Pos;
					}
				}
				else if( type == typeof( GoAssignStmt ) )
				{
					var d = this.Decl as GoAssignStmt;
					foreach( var x in d.Lhs )
					{
						var ident = x as GoIdent;
						if( ident != null && ident.Name == name )
						{
							return ident.Pos;
						}
					}
				}
				return 0;
			}
		}
	}
}
