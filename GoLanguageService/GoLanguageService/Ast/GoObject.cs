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

		public object Declaration { get; set; }

		public object Data { get; set; }

		public object Type { get; set; }

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
				var type = this.Declaration.GetType();
				if( type == typeof( GoField ) )
				{
					var d = this.Declaration as GoField;
					foreach( var n in d.Names )
					{
						if( n.Name == name )
						{
							return n.Pos;
						}
					}
				}
				else if( type == typeof( GoImport ) )
				{
					var d = this.Declaration as GoImport;
					if( d.Name != null && d.Name.Name == name )
					{
						return d.Name.Pos;
					}
					return d.Path.Pos;
				}
				else if( type == typeof( GoValue ) )
				{
					var d = this.Declaration as GoValue;
					foreach( var n in d.Names )
					{
						if( n.Name == name )
						{
							return n.Pos;
						}
					}
				}
				else if( type == typeof( GoType ) )
				{
					var d = this.Declaration as GoType;
					if( d.Name.Name == name )
					{
						return d.Name.Pos;
					}
				}
				else if( type == typeof( GoFunctionDeclaration ) )
				{
					var d = this.Declaration as GoFunctionDeclaration;
					if( d.Name.Name == name )
					{
						return d.Name.Pos;
					}
				}
				else if( type == typeof( GoLabeledStatement ) )
				{
					var d = this.Declaration as GoLabeledStatement;
					if( d.Label.Name == name )
					{
						return d.Label.Pos;
					}
				}
				else if( type == typeof( GoAssignStatement ) )
				{
					var d = this.Declaration as GoAssignStatement;
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
