using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService.Ast
{
	public class GoCommentGroup : GoNode
	{
		public List<GoComment> Comments { get; set; }

		public override int Pos
		{
			get
			{
				return this.Comments.First().Pos;
			}
		}

		public override int End
		{
			get
			{
				return this.Comments.Last().End;
			}
		}

		public string Text
		{
			get
			{
				var comments = new List<string>();
				foreach( var c in this.Comments )
				{
					comments.Add( c.Text );
				}

				var lines = new List<string>();
				foreach( var c in comments )
				{
					var text = c;
					switch( c[1] )
					{
						case '/':
							text = c.Substring( 2 );
							if( text.Length > 0 && text[0] == ' ' )
							{
								text = text.Substring( 1 );
							}
							break;
						case '*':
							text = c.Substring( 2, c.Length - 2 - 2 );
							break;
					}

					var cl = text.Split( '\n' );
					foreach( var l in cl )
					{
						lines.Add( StripTrailingWhitespace( l ) );
					}
				}

				var n = 0;
				foreach( var line in lines )
				{
					if( line != "" || ( n > 0 && lines[n - 1] != "" ) )
					{
						lines[n] = line;
						n++;
					}
				}
				lines = lines.GetRange( 0, n );

				if( n > 0 && lines[n - 1] != "" )
				{
					lines.Add( "" );
				}

				return string.Join( "\n", lines );
			}
		}

		private static string StripTrailingWhitespace( string s )
		{
			var i = s.Length;
			while( i > 0 && IsWhitespace( s[i - 1] ) )
			{
				i--;
			}
			return s.Substring( 0, i );
		}

		private static bool IsWhitespace( char ch )
		{
			return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
		}
	}
}
